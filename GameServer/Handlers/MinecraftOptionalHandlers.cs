using System;
using System.IO;
using System.Threading.Tasks;
using ProtoBuf;
using SharedProtocol;

namespace GameServerApp.Handlers
{
    /// <summary>
    /// Optional Minecraft packet handlers used to keep probe traffic observable
    /// even when full gameplay wiring is not enabled for these message types.
    /// </summary>
    public sealed class MinecraftInventoryUpdateHandler : MinecraftMessageHandlerBase<InventoryUpdateBroadcast>
    {
        public override Task HandleAsync(Session session, InventoryUpdateBroadcast message)
        {
            string playerId = string.IsNullOrWhiteSpace(message.PlayerId) ? "unknown" : message.PlayerId;
            Console.WriteLine($"[Minecraft][Optional] InventoryUpdate received from '{playerId}' and ignored (server authoritative inventory).");
            return Task.CompletedTask;
        }
    }

    public sealed class MinecraftEntityUpdateHandler : MinecraftMessageHandlerBase<EntityUpdateMessage>
    {
        public override Task HandleAsync(Session session, EntityUpdateMessage message)
        {
            string entityId = string.IsNullOrWhiteSpace(message.EntityId) ? "unknown" : message.EntityId;
            Console.WriteLine($"[Minecraft][Optional] EntityUpdate received for '{entityId}' and ignored (server authoritative entities).");
            return Task.CompletedTask;
        }
    }

    public sealed class MinecraftItemUseHandler : MinecraftMessageHandlerBase<PlayerActionRequestMessage>
    {
        public override async Task HandleAsync(Session session, PlayerActionRequestMessage message)
        {
            if (message.Action != PlayerActionType.UseItem)
            {
                message.Action = PlayerActionType.UseItem;
            }

            var response = new PlayerActionResponseMessage
            {
                Success = true,
                Message = "ItemUse optional packet acknowledged.",
                Sequence = message.Sequence
            };

            using var stream = new MemoryStream();
            Serializer.Serialize(stream, response);
            await session.SendAsync((int)MinecraftMessageType.PlayerActionResponse, stream.ToArray());
        }
    }

    public sealed class MinecraftItemDropHandler : MinecraftMessageHandlerBase<PlayerActionRequestMessage>
    {
        public override async Task HandleAsync(Session session, PlayerActionRequestMessage message)
        {
            if (message.Action != PlayerActionType.DropItem)
            {
                message.Action = PlayerActionType.DropItem;
            }

            var response = new PlayerActionResponseMessage
            {
                Success = true,
                Message = "ItemDrop optional packet acknowledged.",
                Sequence = message.Sequence
            };

            using var stream = new MemoryStream();
            Serializer.Serialize(stream, response);
            await session.SendAsync((int)MinecraftMessageType.PlayerActionResponse, stream.ToArray());
        }
    }
}
