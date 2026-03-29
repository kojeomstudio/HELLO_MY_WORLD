using GameServerApp.World;
using GameServerApp.World.Generation;

namespace GameServerApp.World.Generation.Stages
{
    internal sealed class CloudGenerationStage : ITerrainGenerationStage
    {
        private readonly WorldManager _owner;
        public CloudGenerationStage(WorldManager owner) => _owner = owner;
        public string Name => "clouds";
        public void Execute(TerrainGenerationContext context) => _owner.GenerateCloudsInternal(context);
    }
}
