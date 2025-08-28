using GameServerApp.Database;
using SharedProtocol;

namespace GameServerApp.Handlers;

/// <summary>
/// Handles inventory management and item interactions.
/// </summary>
public class InventoryHandler : MessageHandler<InventoryActionRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;

    public InventoryHandler(DatabaseHelper database, SessionManager sessions)
        : base(MessageType.InventoryActionRequest)
    {
        _database = database;
        _sessions = sessions;
    }

    protected override async Task HandleAsync(Session session, InventoryActionRequest message)
    {
        try
        {
            if (!_sessions.ValidateSession(session))
            {
                await SendErrorResponse(session, "Invalid session");
                return;
            }

            var character = await _database.GetPlayerByNameAsync(session.UserName!);
            if (character == null)
            {
                await SendErrorResponse(session, "Player not found");
                return;
            }

            bool success = false;
            string resultMessage = "";

            switch (message.ActionType)
            {
                case InventoryActionType.AddItem:
                    success = await AddItemToInventory(character, message);
                    resultMessage = success ? "Item added successfully" : "Failed to add item";
                    break;

                case InventoryActionType.RemoveItem:
                    success = await RemoveItemFromInventory(character, message);
                    resultMessage = success ? "Item removed successfully" : "Failed to remove item";
                    break;

                case InventoryActionType.MoveItem:
                    success = await MoveItemInInventory(character, message);
                    resultMessage = success ? "Item moved successfully" : "Failed to move item";
                    break;

                case InventoryActionType.UseItem:
                    success = await UseItem(character, message);
                    resultMessage = success ? "Item used successfully" : "Failed to use item";
                    break;

                default:
                    resultMessage = "Unknown action type";
                    break;
            }

            if (success)
            {
                await _database.SavePlayerAsync(character);
            }

            var response = new InventoryActionResponse
            {
                Success = success,
                Message = resultMessage,
                ActionType = message.ActionType,
                UpdatedInventory = ConvertInventory(character.Inventory),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.InventoryActionResponse, response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling inventory action for {session.UserName}: {ex.Message}");
            await SendErrorResponse(session, "Internal server error");
        }
    }

    private async Task<bool> AddItemToInventory(Models.Character character, InventoryActionRequest message)
    {
        if (message.ItemId <= 0 || message.Quantity <= 0)
            return false;

        character.AddItem(message.ItemId, message.ItemName ?? $"Item_{message.ItemId}", message.Quantity);
        return true;
    }

    private async Task<bool> RemoveItemFromInventory(Models.Character character, InventoryActionRequest message)
    {
        if (message.ItemId <= 0 || message.Quantity <= 0)
            return false;

        return character.RemoveItem(message.ItemId, message.Quantity);
    }

    private async Task<bool> MoveItemInInventory(Models.Character character, InventoryActionRequest message)
    {
        return true;
    }

    private async Task<bool> UseItem(Models.Character character, InventoryActionRequest message)
    {
        var item = character.Inventory.FirstOrDefault(i => i.Id == message.ItemId);
        if (item == null || item.Quantity <= 0)
            return false;

        switch (item.Id)
        {
            case 1001:
                character.Health = Math.Min(character.MaxHealth, character.Health + 20);
                character.RemoveItem(message.ItemId, 1);
                return true;

            case 1002:
                character.Health = Math.Min(character.MaxHealth, character.Health + 50);
                character.RemoveItem(message.ItemId, 1);
                return true;

            default:
                return false;
        }
    }

    private List<InventoryItem> ConvertInventory(List<Models.Item> inventory)
    {
        return inventory.Select(item => new InventoryItem
        {
            ItemId = item.Id,
            ItemName = item.Name,
            Quantity = item.Quantity,
            Slot = 0
        }).ToList();
    }

    private async Task SendErrorResponse(Session session, string errorMessage)
    {
        var response = new InventoryActionResponse
        {
            Success = false,
            Message = errorMessage,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await session.SendAsync(MessageType.InventoryActionResponse, response);
    }
}

/// <summary>
/// Handles player interaction requests like opening chests, doors, etc.
/// </summary>
public class InteractionHandler : MessageHandler<PlayerInteractionRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;

    public InteractionHandler(DatabaseHelper database, SessionManager sessions)
        : base(MessageType.PlayerInteractionRequest)
    {
        _database = database;
        _sessions = sessions;
    }

    protected override async Task HandleAsync(Session session, PlayerInteractionRequest message)
    {
        try
        {
            if (!_sessions.ValidateSession(session))
            {
                await SendErrorResponse(session, "Invalid session");
                return;
            }

            var playerState = _sessions.GetPlayerState(session.UserName!);
            if (playerState == null)
            {
                await SendErrorResponse(session, "Player state not found");
                return;
            }

            bool success = await ProcessInteraction(session, message, playerState);
            string resultMessage = success ? "Interaction successful" : "Interaction failed";

            var response = new PlayerInteractionResponse
            {
                Success = success,
                Message = resultMessage,
                InteractionType = message.InteractionType,
                TargetPosition = message.TargetPosition,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.PlayerInteractionResponse, response);

            if (success)
            {
                await BroadcastInteraction(session, message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling interaction for {session.UserName}: {ex.Message}");
            await SendErrorResponse(session, "Internal server error");
        }
    }

    private async Task<bool> ProcessInteraction(Session session, PlayerInteractionRequest message, PlayerState playerState)
    {
        if (message.TargetPosition == null)
            return false;

        var distance = Math.Sqrt(
            Math.Pow(message.TargetPosition.X - playerState.Position.X, 2) +
            Math.Pow(message.TargetPosition.Y - playerState.Position.Y, 2) +
            Math.Pow(message.TargetPosition.Z - playerState.Position.Z, 2));

        if (distance > 5.0)
            return false;

        switch (message.InteractionType)
        {
            case InteractionType.OpenContainer:
                return await HandleContainerInteraction(session, message);

            case InteractionType.UseBlock:
                return await HandleBlockUse(session, message);

            case InteractionType.AttackEntity:
                return await HandleEntityAttack(session, message);

            default:
                return false;
        }
    }

    private async Task<bool> HandleContainerInteraction(Session session, PlayerInteractionRequest message)
    {
        Console.WriteLine($"Player {session.UserName} opened container at {message.TargetPosition?.X}, {message.TargetPosition?.Y}, {message.TargetPosition?.Z}");
        return true;
    }

    private async Task<bool> HandleBlockUse(Session session, PlayerInteractionRequest message)
    {
        Console.WriteLine($"Player {session.UserName} used block at {message.TargetPosition?.X}, {message.TargetPosition?.Y}, {message.TargetPosition?.Z}");
        return true;
    }

    private async Task<bool> HandleEntityAttack(Session session, PlayerInteractionRequest message)
    {
        Console.WriteLine($"Player {session.UserName} attacked entity at {message.TargetPosition?.X}, {message.TargetPosition?.Y}, {message.TargetPosition?.Z}");
        return true;
    }

    private async Task BroadcastInteraction(Session session, PlayerInteractionRequest message)
    {
        var playerState = _sessions.GetPlayerState(session.UserName!);
        if (playerState == null) return;

        var nearbyPlayers = _sessions.GetPlayersInRange(playerState.CurrentWorldId,
            playerState.Position, 20.0);

        var broadcast = new PlayerInteractionBroadcast
        {
            PlayerName = session.UserName!,
            InteractionType = message.InteractionType,
            TargetPosition = message.TargetPosition,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var tasks = new List<Task>();
        foreach (var playerName in nearbyPlayers)
        {
            if (playerName != session.UserName)
            {
                var playerSession = _sessions.GetSession(playerName);
                if (playerSession != null)
                {
                    tasks.Add(playerSession.SendAsync(MessageType.PlayerInteractionBroadcast, broadcast));
                }
            }
        }

        await Task.WhenAll(tasks);
    }

    private async Task SendErrorResponse(Session session, string errorMessage)
    {
        var response = new PlayerInteractionResponse
        {
            Success = false,
            Message = errorMessage,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await session.SendAsync(MessageType.PlayerInteractionResponse, response);
    }
}