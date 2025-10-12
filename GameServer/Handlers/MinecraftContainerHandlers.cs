using System.Threading.Tasks;
using GameServerApp.Systems;
using SharedProtocol;

namespace GameServerApp.Handlers
{
    public sealed class MinecraftContainerOpenHandler : MinecraftMessageHandlerBase<ContainerOpenRequestMessage>
    {
        private readonly ContainerSystem _containerSystem;

        public MinecraftContainerOpenHandler(ContainerSystem containerSystem)
        {
            _containerSystem = containerSystem;
        }

        public override Task HandleAsync(Session session, ContainerOpenRequestMessage message)
        {
            return _containerSystem.HandleOpenAsync(session, message);
        }
    }

    public sealed class MinecraftContainerCloseHandler : MinecraftMessageHandlerBase<ContainerCloseRequestMessage>
    {
        private readonly ContainerSystem _containerSystem;

        public MinecraftContainerCloseHandler(ContainerSystem containerSystem)
        {
            _containerSystem = containerSystem;
        }

        public override Task HandleAsync(Session session, ContainerCloseRequestMessage message)
        {
            return _containerSystem.HandleCloseAsync(session, message);
        }
    }

    public sealed class MinecraftContainerUpdateHandler : MinecraftMessageHandlerBase<ContainerUpdateRequestMessage>
    {
        private readonly ContainerSystem _containerSystem;

        public MinecraftContainerUpdateHandler(ContainerSystem containerSystem)
        {
            _containerSystem = containerSystem;
        }

        public override Task HandleAsync(Session session, ContainerUpdateRequestMessage message)
        {
            return _containerSystem.HandleUpdateAsync(session, message);
        }
    }
}
