using GameServerApp.World;
using GameServerApp.World.Generation;

namespace GameServerApp.World.Generation.Stages
{
    internal sealed class RiverGenerationStage : ITerrainGenerationStage
    {
        private readonly WorldManager _owner;
        public RiverGenerationStage(WorldManager owner) => _owner = owner;
        public string Name => "rivers";
        public void Execute(TerrainGenerationContext context) => _owner.GenerateRiversInternal(context);
    }
}
