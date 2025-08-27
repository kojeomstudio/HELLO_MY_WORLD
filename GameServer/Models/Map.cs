using System;

namespace GameServerApp.Models
{
    /// <summary>
    /// 게임 맵/월드 모델
    /// 맵의 기본 정보와 설정을 관리합니다.
    /// </summary>
    public class Map
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public int MaxPlayers { get; set; } = 100;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        
        // 맵 크기 설정 (선택사항)
        public int SizeX { get; set; } = 1000;
        public int SizeY { get; set; } = 1000;
        public int SizeZ { get; set; } = 256;

        public Map(int id, string name)
        {
            if (id < 0)
                throw new ArgumentException("Map ID cannot be negative", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Map name cannot be empty", nameof(name));
                
            Id = id;
            Name = name;
        }
        
        /// <summary>
        /// 맵이 최대 인원에 도달했는지 확인합니다.
        /// </summary>
        public bool IsAtMaxCapacity(int currentPlayerCount)
        {
            return currentPlayerCount >= MaxPlayers;
        }
        
        /// <summary>
        /// 좌표가 맵 범위 내에 있는지 확인합니다.
        /// </summary>
        public bool IsWithinBounds(double x, double y, double z)
        {
            return x >= 0 && x < SizeX &&
                   y >= 0 && y < SizeY &&
                   z >= 0 && z < SizeZ;
        }
        
        public override string ToString()
        {
            return $"Map[{Id}]: {Name} ({MaxPlayers} players max)";
        }
        
        public override bool Equals(object obj)
        {
            return obj is Map other && Id == other.Id;
        }
        
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
