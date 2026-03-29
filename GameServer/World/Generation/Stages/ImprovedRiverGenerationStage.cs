using GameServerApp.World;
using GameServerApp.World.Generation;

namespace GameServerApp.World.Generation.Stages
{
    internal sealed class ImprovedRiverGenerationStage : ITerrainGenerationStage
    {
        private readonly WorldManager _owner;

        public ImprovedRiverGenerationStage(WorldManager owner)
        {
            _owner = owner;
        }

        public string Name => "improved-rivers";

        public void Execute(TerrainGenerationContext context)
        {
            _owner.GenerateImprovedRiversInternal(context);
        }
    }
}
