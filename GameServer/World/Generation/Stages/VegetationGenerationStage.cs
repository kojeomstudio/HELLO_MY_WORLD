using GameServerApp.World;
using GameServerApp.World.Generation;

namespace GameServerApp.World.Generation.Stages
{
    internal sealed class VegetationGenerationStage : ITerrainGenerationStage
    {
        private readonly WorldManager _owner;
        public VegetationGenerationStage(WorldManager owner) => _owner = owner;
        public string Name => "vegetation";
        public void Execute(TerrainGenerationContext context) => _owner.GenerateVegetationInternal(context);
    }
}
