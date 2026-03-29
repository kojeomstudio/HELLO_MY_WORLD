using GameServerApp.World;
using GameServerApp.World.Generation;

namespace GameServerApp.World.Generation.Stages
{
    internal sealed class ImprovedCaveGenerationStage : ITerrainGenerationStage
    {
        private readonly WorldManager _owner;

        public ImprovedCaveGenerationStage(WorldManager owner)
        {
            _owner = owner;
        }

        public string Name => "improved-caves";

        public void Execute(TerrainGenerationContext context)
        {
            _owner.GenerateImprovedCavesInternal(context);
        }
    }
}
