using GameServerApp.World;
using GameServerApp.World.Generation;

namespace GameServerApp.World.Generation.Stages
{
    internal sealed class ImprovedLakeGenerationStage : ITerrainGenerationStage
    {
        private readonly WorldManager _owner;

        public ImprovedLakeGenerationStage(WorldManager owner)
        {
            _owner = owner;
        }

        public string Name => "improved-lakes";

        public void Execute(TerrainGenerationContext context)
        {
            _owner.GenerateImprovedLakesInternal(context);
        }
    }
}
