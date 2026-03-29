using GameServerApp.World;
using GameServerApp.World.Generation;

namespace GameServerApp.World.Generation.Stages
{
    internal sealed class OreGenerationStage : ITerrainGenerationStage
    {
        private readonly WorldManager _owner;
        public OreGenerationStage(WorldManager owner) => _owner = owner;
        public string Name => "ores";
        public void Execute(TerrainGenerationContext context) => _owner.GenerateOresInternal(context);
    }
}
