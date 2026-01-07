using Minecraft.Core;

namespace Minecraft.World
{
    /// <summary>
    /// Compatibility wrapper that reuses the hydrology-aware EnhancedTerrainGenerator logic.
    /// Kept for callers instantiating the legacy type.
    /// </summary>
    public class ImprovedTerrainGenerator : EnhancedTerrainGenerator
    {
    }
}
