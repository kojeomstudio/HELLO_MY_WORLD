using GameServerApp.World;
using GameServerApp.World.Generation;

namespace GameServerApp.World.Generation.Stages
{
    internal sealed class BaseTerrainStage : ITerrainGenerationStage
    {
        private readonly WorldManager _owner;
        public BaseTerrainStage(WorldManager owner) => _owner = owner;
        public string Name => "base-terrain";
        public void Execute(TerrainGenerationContext context) => _owner.GenerateBaseTerrainInternal(context);
    }
}
