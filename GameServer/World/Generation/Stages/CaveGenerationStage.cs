using GameServerApp.World;
using GameServerApp.World.Generation;

namespace GameServerApp.World.Generation.Stages
{
    internal sealed class CaveGenerationStage : ITerrainGenerationStage
    {
        private readonly WorldManager _owner;
        public CaveGenerationStage(WorldManager owner) => _owner = owner;
        public string Name => "caves";
        public void Execute(TerrainGenerationContext context) => _owner.GenerateCavesInternal(context);
    }
}
