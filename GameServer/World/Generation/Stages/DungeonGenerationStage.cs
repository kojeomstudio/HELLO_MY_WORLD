using GameServerApp.World;
using GameServerApp.World.Generation;

namespace GameServerApp.World.Generation.Stages
{
    internal sealed class DungeonGenerationStage : ITerrainGenerationStage
    {
        private readonly WorldManager _owner;
        public DungeonGenerationStage(WorldManager owner) => _owner = owner;
        public string Name => "dungeons";
        public void Execute(TerrainGenerationContext context) => _owner.GenerateDungeonsInternal(context);
    }
}
