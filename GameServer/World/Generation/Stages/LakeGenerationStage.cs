using GameServerApp.World;
using GameServerApp.World.Generation;

namespace GameServerApp.World.Generation.Stages
{
    internal sealed class LakeGenerationStage : ITerrainGenerationStage
    {
        private readonly WorldManager _owner;
        public LakeGenerationStage(WorldManager owner) => _owner = owner;
        public string Name => "lakes";
        public void Execute(TerrainGenerationContext context) => _owner.GenerateLakesInternal(context);
    }
}
