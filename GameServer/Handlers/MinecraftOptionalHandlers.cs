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

    public sealed class MinecraftMultiBlockChangeHandler : MinecraftMessageHandlerBase<MultiBlockChangeRequestMessage>
    {
        public override async Task HandleAsync(Session session, MultiBlockChangeRequestMessage message)
        {
            int changeCount = message?.Changes?.Count ?? 0;
            var response = new MultiBlockChangeResponseMessage
            {
                AllSuccess = true
            };

            if (message?.Changes != null)
            {
                foreach (var change in message.Changes)
                {
                    response.Results.Add(new BlockChangeResultEntry
                    {
                        Success = true,
                        Message = "MultiBlockChange optional packet acknowledged.",
                        Position = change?.Position ?? new Vector3I(),
                        ActualBlockId = change?.NewBlockId ?? 0,
                        Sequence = change?.Sequence ?? 0
                    });
                }
            }

            Console.WriteLine($"[Minecraft][Optional] MultiBlockChange received ({changeCount} changes) and acknowledged.");
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, response);
            await session.SendAsync((int)MinecraftMessageType.MultiBlockChange, stream.ToArray());
        }
    }

    public sealed class MinecraftItemPickupHandler : MinecraftMessageHandlerBase<ItemPickupRequestMessage>
    {
        public override async Task HandleAsync(Session session, ItemPickupRequestMessage message)
        {
            var response = new ItemPickupResponseMessage
            {
                Success = true,
                Message = "ItemPickup optional packet acknowledged.",
                EntityId = message?.EntityId ?? string.Empty,
                PickedItem = new InventoryItemInfo
                {
                    ItemId = 0,
                    ItemName = "unknown",
                    Quantity = Math.Max(0, message?.RequestedQuantity ?? 0)
                },
                RemainingQuantity = 0,
                Sequence = message?.Sequence ?? 0
            };

            Console.WriteLine($"[Minecraft][Optional] ItemPickup received for entity '{response.EntityId}' and acknowledged.");
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, response);
            await session.SendAsync((int)MinecraftMessageType.ItemPickup, stream.ToArray());
        }
    }

    public sealed class MinecraftEntityInteractHandler : MinecraftMessageHandlerBase<EntityInteractRequestMessage>
    {
        public override async Task HandleAsync(Session session, EntityInteractRequestMessage message)
        {
            var response = new EntityInteractResponseMessage
            {
                Success = true,
                Message = "EntityInteract optional packet acknowledged.",
                TargetEntityId = message?.TargetEntityId ?? string.Empty,
                Sequence = message?.Sequence ?? 0
            };

            Console.WriteLine(
                $"[Minecraft][Optional] EntityInteract received for '{response.TargetEntityId}' (action: {message?.InteractionType}) and acknowledged.");
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, response);
            await session.SendAsync((int)MinecraftMessageType.EntityInteract, stream.ToArray());
        }
    }
}
