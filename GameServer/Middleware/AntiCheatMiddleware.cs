using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SharedProtocol;

namespace GameServerApp.Middleware
{
    /// <summary>
    /// 안티치트 미들웨어 - 속도 핵, 비행 핵, 리치 핵 감지
    /// </summary>
    public class AntiCheatMiddleware
    {
        // 치팅 임계값
        private const float MaxLegitimateSpeed = 12f; // m/s (달리기 속도)
        private const float MaxLegitimateSpeedSprint = 15f; // m/s (스프린트)
        private const float MaxLegitimateReach = 6f; // 블록 상호작용 최대 거리
        private const float MaxLegitimateY벨로시티 = 10f; // 점프 속도
        private const int MaxViolationsBeforeKick = 10; // 킥 전 최대 위반 횟수
        private const int ViolationDecaySeconds = 60; // 위반 기록 유지 시간

        // 플레이어별 치팅 감지 데이터
        private readonly ConcurrentDictionary<string, PlayerAntiCheatData> _playerData = new();

        /// <summary>
        /// 플레이어별 안티치트 데이터
        /// </summary>
        private class PlayerAntiCheatData
        {
            public Queue<MovementSample> MovementHistory { get; set; } = new();
            public List<ViolationRecord> Violations { get; set; } = new();
            public Vector3 LastPosition { get; set; } = new Vector3();
            public DateTime LastPositionUpdate { get; set; } = DateTime.UtcNow;
            public float MaxYVelocityObserved { get; set; }
            public int ConsecutiveFlightTicks { get; set; }
            public bool IsAllowedToFly { get; set; } // Creative mode
        }

        /// <summary>
        /// 이동 샘플
        /// </summary>
        private class MovementSample
        {
            public Vector3 Position { get; set; } = new Vector3();
            public DateTime Timestamp { get; set; }
            public float Speed { get; set; }
        }

        /// <summary>
        /// 위반 기록
        /// </summary>
        public class ViolationRecord
        {
            public ViolationType Type { get; set; }
            public DateTime Timestamp { get; set; }
            public string Details { get; set; } = string.Empty;
            public float Severity { get; set; } // 0.0 ~ 1.0
        }

        public enum ViolationType
        {
            SpeedHack,
            FlightHack,
            ReachHack,
            NoClip,
            AbnormalMovement,
            TeleportHack
        }

        /// <summary>
        /// 검증 결과
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public ViolationType? ViolationType { get; set; }
            public string Reason { get; set; } = string.Empty;
            public bool ShouldKick { get; set; }
            public int TotalViolations { get; set; }
        }

        /// <summary>
        /// 이동 검증 (속도 핵, 비행 핵 감지)
        /// </summary>
        public ValidationResult ValidateMovement(string playerName, Vector3 newPosition, bool isOnGround, bool isSprinting = false)
        {
            var data = _playerData.GetOrAdd(playerName, _ => new PlayerAntiCheatData());

            var result = new ValidationResult { IsValid = true };

            var now = DateTime.UtcNow;
            var deltaTime = (float)(now - data.LastPositionUpdate).TotalSeconds;

            if (deltaTime < 0.001f) // 너무 빠른 업데이트 방지
            {
                return result;
            }

            // 1. 속도 검증
            var distance = GetDistance(data.LastPosition, newPosition);
            var speed = distance / deltaTime;

            var maxAllowedSpeed = data.IsAllowedToFly ? 50f : (isSprinting ? MaxLegitimateSpeedSprint : MaxLegitimateSpeed);

            if (speed > maxAllowedSpeed)
            {
                result.IsValid = false;
                result.ViolationType = ViolationType.SpeedHack;
                result.Reason = $"Speed {speed:F2} m/s exceeds maximum {maxAllowedSpeed:F2} m/s";

                RecordViolation(playerName, ViolationType.SpeedHack, result.Reason,
                    Math.Min(1.0f, (speed - maxAllowedSpeed) / maxAllowedSpeed));
            }

            // 2. Y축 속도 검증 (비정상적인 상승)
            var yVelocity = (newPosition.Y - data.LastPosition.Y) / deltaTime;

            if (!data.IsAllowedToFly && !isOnGround && yVelocity > MaxLegitimateY벨로시티)
            {
                result.IsValid = false;
                result.ViolationType = ViolationType.FlightHack;
                result.Reason = $"Y velocity {yVelocity:F2} m/s exceeds jump velocity";

                RecordViolation(playerName, ViolationType.FlightHack, result.Reason, 0.8f);
            }

            // 3. 비행 감지 (연속으로 공중에 떠 있음)
            if (!data.IsAllowedToFly && !isOnGround)
            {
                data.ConsecutiveFlightTicks++;

                // 10초 이상 연속으로 공중에 있으면 비행 핵 의심
                if (data.ConsecutiveFlightTicks > 200) // 20 ticks/s * 10s
                {
                    result.IsValid = false;
                    result.ViolationType = ViolationType.FlightHack;
                    result.Reason = $"Airborne for {data.ConsecutiveFlightTicks / 20f:F1} seconds";

                    RecordViolation(playerName, ViolationType.FlightHack, result.Reason, 0.9f);
                }
            }
            else
            {
                data.ConsecutiveFlightTicks = 0;
            }

            // 4. 텔레포트 감지 (순간 이동)
            if (distance > 50f && deltaTime < 1f)
            {
                result.IsValid = false;
                result.ViolationType = ViolationType.TeleportHack;
                result.Reason = $"Teleported {distance:F2} blocks in {deltaTime:F3} seconds";

                RecordViolation(playerName, ViolationType.TeleportHack, result.Reason, 1.0f);
            }

            // 5. 이동 히스토리 업데이트
            data.MovementHistory.Enqueue(new MovementSample
            {
                Position = newPosition,
                Timestamp = now,
                Speed = speed
            });

            // 최근 100개 샘플만 유지
            while (data.MovementHistory.Count > 100)
            {
                data.MovementHistory.Dequeue();
            }

            data.LastPosition = newPosition;
            data.LastPositionUpdate = now;

            // 6. 위반 누적 확인
            CleanupOldViolations(playerName);
            var recentViolations = data.Violations.Count(v => (now - v.Timestamp).TotalSeconds < ViolationDecaySeconds);

            result.TotalViolations = recentViolations;
            result.ShouldKick = recentViolations >= MaxViolationsBeforeKick;

            return result;
        }

        /// <summary>
        /// 블록 상호작용 검증 (리치 핵 감지)
        /// </summary>
        public ValidationResult ValidateBlockInteraction(string playerName, Vector3 playerPosition, Vector3 blockPosition)
        {
            var result = new ValidationResult { IsValid = true };

            var distance = GetDistance(playerPosition, blockPosition);

            if (distance > MaxLegitimateReach)
            {
                result.IsValid = false;
                result.ViolationType = ViolationType.ReachHack;
                result.Reason = $"Block reach {distance:F2} exceeds maximum {MaxLegitimateReach:F2}";

                RecordViolation(playerName, ViolationType.ReachHack, result.Reason,
                    Math.Min(1.0f, (distance - MaxLegitimateReach) / MaxLegitimateReach));

                var data = _playerData.GetOrAdd(playerName, _ => new PlayerAntiCheatData());
                CleanupOldViolations(playerName);
                var recentViolations = data.Violations.Count(v =>
                    (DateTime.UtcNow - v.Timestamp).TotalSeconds < ViolationDecaySeconds);

                result.TotalViolations = recentViolations;
                result.ShouldKick = recentViolations >= MaxViolationsBeforeKick;
            }

            return result;
        }

        /// <summary>
        /// Creative 모드 비행 허용 설정
        /// </summary>
        public void SetFlightAllowed(string playerName, bool allowed)
        {
            var data = _playerData.GetOrAdd(playerName, _ => new PlayerAntiCheatData());
            data.IsAllowedToFly = allowed;
            data.ConsecutiveFlightTicks = 0;
        }

        /// <summary>
        /// 플레이어 위반 기록 조회
        /// </summary>
        public List<ViolationRecord> GetViolations(string playerName)
        {
            if (_playerData.TryGetValue(playerName, out var data))
            {
                CleanupOldViolations(playerName);
                return new List<ViolationRecord>(data.Violations);
            }
            return new List<ViolationRecord>();
        }

        /// <summary>
        /// 플레이어 데이터 초기화 (로그아웃 시)
        /// </summary>
        public void ClearPlayerData(string playerName)
        {
            _playerData.TryRemove(playerName, out _);
        }

        /// <summary>
        /// 위반 기록 저장
        /// </summary>
        private void RecordViolation(string playerName, ViolationType type, string details, float severity)
        {
            var data = _playerData.GetOrAdd(playerName, _ => new PlayerAntiCheatData());

            data.Violations.Add(new ViolationRecord
            {
                Type = type,
                Timestamp = DateTime.UtcNow,
                Details = details,
                Severity = severity
            });

            Console.WriteLine($"[AntiCheat] {playerName} - {type}: {details} (Severity: {severity:F2})");
        }

        /// <summary>
        /// 오래된 위반 기록 제거
        /// </summary>
        private void CleanupOldViolations(string playerName)
        {
            if (_playerData.TryGetValue(playerName, out var data))
            {
                var cutoff = DateTime.UtcNow.AddSeconds(-ViolationDecaySeconds);
                data.Violations.RemoveAll(v => v.Timestamp < cutoff);
            }
        }

        /// <summary>
        /// 두 위치 간 거리 계산
        /// </summary>
        private float GetDistance(Vector3 a, Vector3 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// 통계 정보 조회
        /// </summary>
        public AntiCheatStatistics GetStatistics()
        {
            var stats = new AntiCheatStatistics();

            foreach (var kvp in _playerData)
            {
                var data = kvp.Value;
                CleanupOldViolations(kvp.Key);

                stats.TotalPlayers++;
                stats.TotalViolations += data.Violations.Count;

                foreach (var violation in data.Violations)
                {
                    if (!stats.ViolationsByType.ContainsKey(violation.Type))
                    {
                        stats.ViolationsByType[violation.Type] = 0;
                    }
                    stats.ViolationsByType[violation.Type]++;
                }

                if (data.Violations.Count >= MaxViolationsBeforeKick)
                {
                    stats.PlayersAboveKickThreshold++;
                }
            }

            return stats;
        }

        public class AntiCheatStatistics
        {
            public int TotalPlayers { get; set; }
            public int TotalViolations { get; set; }
            public int PlayersAboveKickThreshold { get; set; }
            public Dictionary<ViolationType, int> ViolationsByType { get; set; } = new();
        }
    }
}
