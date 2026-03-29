using System;
using SharedProtocol;

namespace GameServerApp.Systems
{
    /// <summary>
    /// 서버 측 물리 시스템 - 중력, 충돌, 낙하 데미지 처리
    /// </summary>
    public class PhysicsSystem
    {
        // 물리 상수
        private const float Gravity = 9.8f; // m/s^2 (현실적인 중력)
        private const float MinecraftGravity = 32f; // Minecraft의 중력 (더 빠른 낙하)
        private const float TerminalVelocity = 78.4f; // 최대 낙하 속도 (m/s)
        private const float FallDamageThreshold = 3.0f; // 낙하 데미지 시작 높이
        private const float FallDamagePerBlock = 1.0f; // 블록당 데미지

        // AABB 충돌 박스 설정
        private const float PlayerWidth = 0.6f;
        private const float PlayerHeight = 1.8f;
        private const float PlayerEyeHeight = 1.62f;

        /// <summary>
        /// 플레이어 물리 상태
        /// </summary>
        public class PlayerPhysicsState
        {
            public Vector3 Position { get; set; } = new Vector3();
            public Vector3 Velocity { get; set; } = new Vector3();
            public bool IsOnGround { get; set; }
            public float LastGroundY { get; set; }
            public DateTime LastPhysicsUpdate { get; set; } = DateTime.UtcNow;
            public bool IsFlying { get; set; } // Creative mode
            public bool IsInWater { get; set; }
            public bool IsInLava { get; set; }
        }

        /// <summary>
        /// 물리 업데이트 적용 (서버 틱마다 호출)
        /// </summary>
        /// <param name="state">플레이어 물리 상태</param>
        /// <param name="deltaTime">델타 타임 (초)</param>
        /// <param name="worldData">월드 데이터 (블록 충돌 검사용)</param>
        /// <returns>업데이트된 물리 상태</returns>
        public PlayerPhysicsState ApplyPhysics(PlayerPhysicsState state, float deltaTime, IWorldDataProvider worldData)
        {
            if (state.IsFlying)
            {
                // Creative mode에서는 물리 법칙 무시
                state.Velocity = new Vector3 { X = 0, Y = 0, Z = 0 };
                state.IsOnGround = false;
                return state;
            }

            // 1. 중력 적용
            if (!state.IsOnGround && !state.IsInWater)
            {
                double gravityForce = state.IsInLava ? MinecraftGravity * 0.5f : MinecraftGravity;
                state.Velocity.Y -= gravityForce * deltaTime;

                // 종단 속도 제한
                if (state.Velocity.Y < -TerminalVelocity)
                {
                    state.Velocity.Y = -TerminalVelocity;
                }
            }
            else if (state.IsInWater)
            {
                // 물 속에서는 부력과 저항 적용
                state.Velocity.Y += 2.0f * deltaTime; // 부력
                state.Velocity.Y *= 0.8f; // 저항
            }

            // 2. 속도 적용하여 새 위치 계산
            var newPosition = new Vector3 {
                X = state.Position.X + state.Velocity.X * deltaTime,
                Y = state.Position.Y + state.Velocity.Y * deltaTime,
                Z = state.Position.Z + state.Velocity.Z * deltaTime
            };

            // 3. 충돌 감지 및 처리
            var collision = CheckCollision(newPosition, worldData);

            if (collision.HasCollision)
            {
                // 충돌 시 위치 보정
                if (collision.CollidesY)
                {
                    if (state.Velocity.Y < 0) // 바닥 충돌
                    {
                        state.IsOnGround = true;
                        state.LastGroundY = (float)newPosition.Y;
                        newPosition.Y = collision.CorrectedY;
                    }
                    state.Velocity.Y = 0;
                }

                if (collision.CollidesX)
                {
                    state.Velocity.X = 0;
                    newPosition.X = collision.CorrectedX;
                }

                if (collision.CollidesZ)
                {
                    state.Velocity.Z = 0;
                    newPosition.Z = collision.CorrectedZ;
                }
            }
            else
            {
                state.IsOnGround = false;
            }

            state.Position = newPosition;
            state.LastPhysicsUpdate = DateTime.UtcNow;

            return state;
        }

        /// <summary>
        /// 낙하 데미지 계산
        /// </summary>
        /// <param name="fallDistance">낙하 거리 (블록 단위)</param>
        /// <returns>데미지 양</returns>
        public float CalculateFallDamage(float fallDistance)
        {
            if (fallDistance <= FallDamageThreshold)
            {
                return 0f;
            }

            float damageDistance = fallDistance - FallDamageThreshold;
            return damageDistance * FallDamagePerBlock;
        }

        /// <summary>
        /// 낙하 거리 계산
        /// </summary>
        public float GetFallDistance(PlayerPhysicsState state)
        {
            if (state.IsOnGround)
            {
                return 0f;
            }

            return MathF.Max(0f, state.LastGroundY - (float)state.Position.Y);
        }

        /// <summary>
        /// AABB 충돌 감지
        /// </summary>
        private CollisionResult CheckCollision(Vector3 position, IWorldDataProvider worldData)
        {
            var result = new CollisionResult
            {
                CorrectedX = position.X,
                CorrectedY = position.Y,
                CorrectedZ = position.Z
            };

            // 플레이어 AABB 바운딩 박스
            double minX = position.X - PlayerWidth / 2;
            double maxX = position.X + PlayerWidth / 2;
            double minY = position.Y;
            double maxY = position.Y + PlayerHeight;
            double minZ = position.Z - PlayerWidth / 2;
            double maxZ = position.Z + PlayerWidth / 2;

            // 주변 블록 검사 범위
            int startX = (int)Math.Floor(minX);
            int endX = (int)Math.Ceiling(maxX);
            int startY = (int)Math.Floor(minY);
            int endY = (int)Math.Ceiling(maxY);
            int startZ = (int)Math.Floor(minZ);
            int endZ = (int)Math.Ceiling(maxZ);

            // 주변 블록 순회하여 충돌 검사
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    for (int z = startZ; z <= endZ; z++)
                    {
                        if (worldData.IsBlockSolid(x, y, z))
                        {
                            // 블록과 충돌 발생
                            result.HasCollision = true;

                            // Y축 충돌 (바닥/천장)
                            if (position.Y < y + 1 && position.Y + PlayerHeight > y)
                            {
                                result.CollidesY = true;
                                if (position.Y < y)
                                {
                                    result.CorrectedY = y - PlayerHeight; // 천장 충돌
                                }
                                else
                                {
                                    result.CorrectedY = y + 1; // 바닥 충돌
                                }
                            }

                            // X축 충돌 (벽)
                            if (minX < x + 1 && maxX > x)
                            {
                                result.CollidesX = true;
                                if (position.X < x + 0.5f)
                                {
                                    result.CorrectedX = x - PlayerWidth / 2;
                                }
                                else
                                {
                                    result.CorrectedX = x + 1 + PlayerWidth / 2;
                                }
                            }

                            // Z축 충돌 (벽)
                            if (minZ < z + 1 && maxZ > z)
                            {
                                result.CollidesZ = true;
                                if (position.Z < z + 0.5f)
                                {
                                    result.CorrectedZ = z - PlayerWidth / 2;
                                }
                                else
                                {
                                    result.CorrectedZ = z + 1 + PlayerWidth / 2;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 충돌 결과
        /// </summary>
        private class CollisionResult
        {
            public bool HasCollision { get; set; }
            public bool CollidesX { get; set; }
            public bool CollidesY { get; set; }
            public bool CollidesZ { get; set; }
            public double CorrectedX { get; set; }
            public double CorrectedY { get; set; }
            public double CorrectedZ { get; set; }
        }
    }

    /// <summary>
    /// 월드 데이터 제공자 인터페이스
    /// </summary>
    public interface IWorldDataProvider
    {
        /// <summary>
        /// 지정한 좌표의 블록이 고체인지 확인
        /// </summary>
        bool IsBlockSolid(int x, int y, int z);

        /// <summary>
        /// 지정한 좌표의 블록이 물인지 확인
        /// </summary>
        bool IsBlockWater(int x, int y, int z);

        /// <summary>
        /// 지정한 좌표의 블록이 용암인지 확인
        /// </summary>
        bool IsBlockLava(int x, int y, int z);
    }
}
