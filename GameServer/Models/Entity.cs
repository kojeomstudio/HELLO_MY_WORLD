using System;

namespace GameServerApp.Models
{
    /// <summary>
    /// 엔티티 모델 - 게임 내 동적 객체 (플레이어, 몹, 아이템 등)를 표현
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// 엔티티 고유 ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 엔티티 타입 (EntityType enum 값)
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// X 좌표
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y 좌표
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Z 좌표
        /// </summary>
        public double Z { get; set; }

        /// <summary>
        /// X축 회전 (피치)
        /// </summary>
        public double RotationX { get; set; }

        /// <summary>
        /// Y축 회전 (요)
        /// </summary>
        public double RotationY { get; set; }

        /// <summary>
        /// Z축 회전 (롤)
        /// </summary>
        public double RotationZ { get; set; }

        /// <summary>
        /// X축 속도
        /// </summary>
        public double VelocityX { get; set; }

        /// <summary>
        /// Y축 속도
        /// </summary>
        public double VelocityY { get; set; }

        /// <summary>
        /// Z축 속도
        /// </summary>
        public double VelocityZ { get; set; }

        /// <summary>
        /// 현재 체력
        /// </summary>
        public float Health { get; set; } = 20.0f;

        /// <summary>
        /// 최대 체력
        /// </summary>
        public float MaxHealth { get; set; } = 20.0f;

        /// <summary>
        /// 엔티티별 추가 데이터 (JSON 형태)
        /// </summary>
        public string? Data { get; set; }

        /// <summary>
        /// 엔티티가 속한 청크 X 좌표
        /// </summary>
        public int ChunkX => (int)Math.Floor(X / 16.0);

        /// <summary>
        /// 엔티티가 속한 청크 Z 좌표
        /// </summary>
        public int ChunkZ => (int)Math.Floor(Z / 16.0);

        /// <summary>
        /// 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 마지막 업데이트 시간
        /// </summary>
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 엔티티가 활성화된 상태인지
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 엔티티의 소유자 (플레이어 ID, 아이템 엔티티 등에 사용)
        /// </summary>
        public string? OwnerId { get; set; }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public Entity() { }

        /// <summary>
        /// 타입과 위치를 지정하는 생성자
        /// </summary>
        public Entity(string id, int type, double x, double y, double z)
        {
            Id = id;
            Type = type;
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// 위치 정보를 문자열로 반환
        /// </summary>
        public string GetPositionString() => $"[{X:F2}, {Y:F2}, {Z:F2}]";

        /// <summary>
        /// 청크 좌표를 문자열로 반환
        /// </summary>
        public string GetChunkString() => $"[{ChunkX}, {ChunkZ}]";

        /// <summary>
        /// 두 엔티티 간의 거리 계산
        /// </summary>
        public double DistanceTo(Entity other)
        {
            if (other == null) return double.MaxValue;
            
            double dx = X - other.X;
            double dy = Y - other.Y;
            double dz = Z - other.Z;
            
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// 지정된 위치로부터의 거리 계산
        /// </summary>
        public double DistanceTo(double x, double y, double z)
        {
            double dx = X - x;
            double dy = Y - y;
            double dz = Z - z;
            
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// 엔티티가 죽었는지 확인
        /// </summary>
        public bool IsDead() => Health <= 0;

        /// <summary>
        /// 엔티티가 움직이고 있는지 확인
        /// </summary>
        public bool IsMoving()
        {
            const double threshold = 0.001;
            return Math.Abs(VelocityX) > threshold || 
                   Math.Abs(VelocityY) > threshold || 
                   Math.Abs(VelocityZ) > threshold;
        }

        /// <summary>
        /// 속도를 설정
        /// </summary>
        public void SetVelocity(double vx, double vy, double vz)
        {
            VelocityX = vx;
            VelocityY = vy;
            VelocityZ = vz;
            LastUpdate = DateTime.UtcNow;
        }

        /// <summary>
        /// 위치를 설정
        /// </summary>
        public void SetPosition(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
            LastUpdate = DateTime.UtcNow;
        }

        /// <summary>
        /// 회전을 설정
        /// </summary>
        public void SetRotation(double rx, double ry, double rz)
        {
            RotationX = rx;
            RotationY = ry;
            RotationZ = rz;
            LastUpdate = DateTime.UtcNow;
        }

        /// <summary>
        /// 데미지 적용
        /// </summary>
        public void TakeDamage(float damage)
        {
            Health = Math.Max(0, Health - damage);
            LastUpdate = DateTime.UtcNow;
        }

        /// <summary>
        /// 체력 회복
        /// </summary>
        public void Heal(float amount)
        {
            Health = Math.Min(MaxHealth, Health + amount);
            LastUpdate = DateTime.UtcNow;
        }

        /// <summary>
        /// 엔티티 정보를 문자열로 반환
        /// </summary>
        public override string ToString()
        {
            return $"Entity {Id} (Type: {Type}) at {GetPositionString()}, Health: {Health}/{MaxHealth}";
        }

        /// <summary>
        /// 해시 코드 생성 (ID 기반)
        /// </summary>
        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// 동등성 비교 (ID 기반)
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is Entity other && Id == other.Id;
        }
    }
}