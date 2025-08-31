using System;

namespace GameServerApp.Models
{
    /// <summary>
    /// 블록 데이터 모델 - 블록의 위치, 타입, 메타데이터 등을 저장
    /// </summary>
    public class BlockData
    {
        /// <summary>
        /// 블록 ID (블록 타입)
        /// </summary>
        public int BlockId { get; set; }
        
        /// <summary>
        /// X 좌표
        /// </summary>
        public int X { get; set; }
        
        /// <summary>
        /// Y 좌표
        /// </summary>
        public int Y { get; set; }
        
        /// <summary>
        /// Z 좌표
        /// </summary>
        public int Z { get; set; }
        
        /// <summary>
        /// 블록 메타데이터 (방향, 상태 등)
        /// </summary>
        public int Metadata { get; set; }
        
        /// <summary>
        /// 블록을 설치한 플레이어 이름
        /// </summary>
        public string PlacedBy { get; set; } = string.Empty;
        
        /// <summary>
        /// 블록 설치 시간
        /// </summary>
        public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 블록 엔티티 데이터 (상자, 화로 등의 추가 데이터)
        /// </summary>
        public string? BlockEntityData { get; set; }
        
        /// <summary>
        /// 마지막 업데이트 시간
        /// </summary>
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public BlockData() { }

        /// <summary>
        /// 위치와 블록 ID를 지정하는 생성자
        /// </summary>
        public BlockData(int x, int y, int z, int blockId)
        {
            X = x;
            Y = y;
            Z = z;
            BlockId = blockId;
        }

        /// <summary>
        /// 위치 문자열 반환
        /// </summary>
        public string GetPositionString() => $"[{X}, {Y}, {Z}]";

        /// <summary>
        /// 블록이 공기인지 확인
        /// </summary>
        public bool IsAir() => BlockId == 0;

        /// <summary>
        /// 두 블록의 위치가 같은지 비교
        /// </summary>
        public bool SamePosition(BlockData other)
        {
            return other != null && X == other.X && Y == other.Y && Z == other.Z;
        }

        /// <summary>
        /// 블록 정보를 문자열로 반환
        /// </summary>
        public override string ToString()
        {
            return $"Block {BlockId} at {GetPositionString()}, Metadata: {Metadata}, PlacedBy: {PlacedBy}";
        }

        /// <summary>
        /// 해시 코드 생성 (위치 기반)
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        /// <summary>
        /// 동등성 비교 (위치 기반)
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is BlockData other && SamePosition(other);
        }
    }
}