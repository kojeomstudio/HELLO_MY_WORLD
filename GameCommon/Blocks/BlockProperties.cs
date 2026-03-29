using System.Collections.Generic;

namespace GameCommon.Blocks
{
    /// <summary>
    /// 블록 속성 정의
    /// config/blocks.json에서 로드됨
    /// </summary>
    public class BlockProperties
    {
        public BlockType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 블록 경도 (파괴 시간 계산)
        /// </summary>
        public float Hardness { get; set; }

        /// <summary>
        /// 폭발 저항
        /// </summary>
        public float Resistance { get; set; }

        /// <summary>
        /// 투명도 (빛 통과)
        /// </summary>
        public bool IsTransparent { get; set; }

        /// <summary>
        /// 유체 여부
        /// </summary>
        public bool IsFluid { get; set; }

        /// <summary>
        /// 중력 영향 여부 (모래, 자갈)
        /// </summary>
        public bool AffectedByGravity { get; set; }

        /// <summary>
        /// 필요한 도구 타입
        /// </summary>
        public string? RequiredTool { get; set; }

        /// <summary>
        /// 필요한 도구 레벨 (0=나무, 1=돌, 2=철, 3=다이아)
        /// </summary>
        public int RequiredToolLevel { get; set; }

        /// <summary>
        /// 빛 방출 레벨 (0-15)
        /// </summary>
        public int LightLevel { get; set; }

        /// <summary>
        /// 드롭 아이템 목록
        /// </summary>
        public List<BlockDrop> Drops { get; set; } = new();

        /// <summary>
        /// 레드스톤 전도 여부
        /// </summary>
        public bool ConductsRedstone { get; set; }

        /// <summary>
        /// 레드스톤 전원 공급 여부
        /// </summary>
        public bool IsPowerSource { get; set; }
    }

    /// <summary>
    /// 블록 드롭 정의
    /// </summary>
    public class BlockDrop
    {
        public string ItemId { get; set; } = string.Empty;
        public float Chance { get; set; } = 1.0f;
        public int MinCount { get; set; } = 1;
        public int MaxCount { get; set; } = 1;

        /// <summary>
        /// 도구 인챈트 조건
        /// </summary>
        public string? RequiredEnchantment { get; set; }
    }
}
