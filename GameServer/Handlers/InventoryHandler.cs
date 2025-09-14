using GameServerApp.Database;
using SharedProtocol;

namespace GameServerApp.Handlers;

/// <summary>
/// 인벤토리 관리를 처리하는 핸들러
/// </summary>
public class InventoryHandler : MessageHandler<InventoryRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;
    private readonly InventorySystem _inventorySystem;

    public InventoryHandler(DatabaseHelper database, SessionManager sessions)
        : base(MessageType.InventoryRequest)
    {
        _database = database;
        _sessions = sessions;
        _inventorySystem = new InventorySystem(database);
    }

    protected override async Task HandleAsync(Session session, InventoryRequest message)
    {
        try
        {
            if (string.IsNullOrEmpty(session.SessionToken) || string.IsNullOrEmpty(session.UserName))
            {
                await SendFailureResponse(session, "인증되지 않은 세션입니다.");
                return;
            }

            var playerInventory = await _inventorySystem.GetPlayerInventoryAsync(session.UserName);
            if (playerInventory == null)
            {
                await SendFailureResponse(session, "플레이어 인벤토리를 찾을 수 없습니다.");
                return;
            }

            bool success = false;
            string resultMessage = "";
            var updatedSlots = new List<InventorySlotData>();

            switch (message.Action)
            {
                case 0: // Move
                    success = await ProcessMoveAction(playerInventory, message, updatedSlots);
                    resultMessage = success ? "아이템 이동 완료" : "아이템 이동 실패";
                    break;

                case 1: // Swap
                    success = await ProcessSwapAction(playerInventory, message, updatedSlots);
                    resultMessage = success ? "아이템 교환 완료" : "아이템 교환 실패";
                    break;

                case 2: // Split
                    success = await ProcessSplitAction(playerInventory, message, updatedSlots);
                    resultMessage = success ? "아이템 분할 완료" : "아이템 분할 실패";
                    break;

                case 3: // Drop
                    success = await ProcessDropAction(playerInventory, message, updatedSlots);
                    resultMessage = success ? "아이템 드랍 완료" : "아이템 드랍 실패";
                    break;

                default:
                    await SendFailureResponse(session, "알 수 없는 액션입니다.");
                    return;
            }

            if (success)
            {
                await _inventorySystem.SavePlayerInventoryAsync(session.UserName, playerInventory);
            }

            var response = new InventoryResponse
            {
                Success = success,
                Message = resultMessage,
                UpdatedSlots = updatedSlots,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.InventoryResponse, response);

            Console.WriteLine($"Inventory action {message.Action} processed for {session.UserName}: {success}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Inventory error for user '{session.UserName}': {ex.Message}");
            await SendFailureResponse(session, "인벤토리 처리 중 오류가 발생했습니다.");
        }
    }

    private async Task<bool> ProcessMoveAction(PlayerInventory inventory, InventoryRequest message, List<InventorySlotData> updatedSlots)
    {
        var sourceSlot = inventory.GetSlot(message.SourceSlot);
        var targetSlot = inventory.GetSlot(message.TargetSlot);

        if (sourceSlot == null || targetSlot == null || sourceSlot.IsEmpty())
            return false;

        int moveAmount = message.Amount > 0 ? Math.Min(message.Amount, sourceSlot.Amount) : sourceSlot.Amount;

        if (targetSlot.IsEmpty())
        {
            // 빈 슬롯으로 이동
            targetSlot.SetItem(sourceSlot.ItemId, moveAmount, sourceSlot.ItemData);
            sourceSlot.Amount -= moveAmount;
            
            if (sourceSlot.Amount <= 0)
                sourceSlot.Clear();
        }
        else if (targetSlot.CanStackWith(sourceSlot))
        {
            // 같은 아이템끼리 스택
            int maxStack = GetMaxStackSize(targetSlot.ItemId);
            int canAdd = Math.Min(moveAmount, maxStack - targetSlot.Amount);
            
            if (canAdd > 0)
            {
                targetSlot.Amount += canAdd;
                sourceSlot.Amount -= canAdd;
                
                if (sourceSlot.Amount <= 0)
                    sourceSlot.Clear();
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        updatedSlots.Add(ConvertToSlotData(sourceSlot));
        updatedSlots.Add(ConvertToSlotData(targetSlot));

        return true;
    }

    private async Task<bool> ProcessSwapAction(PlayerInventory inventory, InventoryRequest message, List<InventorySlotData> updatedSlots)
    {
        var sourceSlot = inventory.GetSlot(message.SourceSlot);
        var targetSlot = inventory.GetSlot(message.TargetSlot);

        if (sourceSlot == null || targetSlot == null)
            return false;

        // 슬롯 내용 교환
        var tempItemId = sourceSlot.ItemId;
        var tempAmount = sourceSlot.Amount;
        var tempData = sourceSlot.ItemData;

        sourceSlot.SetItem(targetSlot.ItemId, targetSlot.Amount, targetSlot.ItemData);
        targetSlot.SetItem(tempItemId, tempAmount, tempData);

        updatedSlots.Add(ConvertToSlotData(sourceSlot));
        updatedSlots.Add(ConvertToSlotData(targetSlot));

        return true;
    }

    private async Task<bool> ProcessSplitAction(PlayerInventory inventory, InventoryRequest message, List<InventorySlotData> updatedSlots)
    {
        var sourceSlot = inventory.GetSlot(message.SourceSlot);
        var targetSlot = inventory.GetSlot(message.TargetSlot);

        if (sourceSlot == null || targetSlot == null || sourceSlot.IsEmpty() || !targetSlot.IsEmpty())
            return false;

        if (sourceSlot.Amount <= 1)
            return false;

        int splitAmount = message.Amount > 0 ? Math.Min(message.Amount, sourceSlot.Amount / 2) : sourceSlot.Amount / 2;
        
        targetSlot.SetItem(sourceSlot.ItemId, splitAmount, sourceSlot.ItemData);
        sourceSlot.Amount -= splitAmount;

        updatedSlots.Add(ConvertToSlotData(sourceSlot));
        updatedSlots.Add(ConvertToSlotData(targetSlot));

        return true;
    }

    private async Task<bool> ProcessDropAction(PlayerInventory inventory, InventoryRequest message, List<InventorySlotData> updatedSlots)
    {
        var sourceSlot = inventory.GetSlot(message.SourceSlot);

        if (sourceSlot == null || sourceSlot.IsEmpty())
            return false;

        int dropAmount = message.Amount > 0 ? Math.Min(message.Amount, sourceSlot.Amount) : sourceSlot.Amount;
        
        sourceSlot.Amount -= dropAmount;
        
        if (sourceSlot.Amount <= 0)
            sourceSlot.Clear();

        updatedSlots.Add(ConvertToSlotData(sourceSlot));

        // TODO: 실제 월드에 드랍된 아이템 엔티티 생성
        Console.WriteLine($"Dropped {dropAmount} of {sourceSlot.ItemId} from slot {message.SourceSlot}");

        return true;
    }

    private InventorySlotData ConvertToSlotData(InventorySlot slot)
    {
        return new InventorySlotData
        {
            SlotIndex = slot.SlotIndex,
            ItemId = slot.ItemId,
            Amount = slot.Amount,
            ItemData = slot.ItemData
        };
    }

    private int GetMaxStackSize(string itemId)
    {
        return itemId switch
        {
            var item when item.Contains("sword") => 1,
            var item when item.Contains("pickaxe") => 1,
            var item when item.Contains("axe") => 1,
            var item when item.Contains("shovel") => 1,
            var item when item.Contains("hoe") => 1,
            var item when item.Contains("helmet") => 1,
            var item when item.Contains("chestplate") => 1,
            var item when item.Contains("leggings") => 1,
            var item when item.Contains("boots") => 1,
            _ => 64
        };
    }

    private async Task SendFailureResponse(Session session, string errorMessage)
    {
        var response = new InventoryResponse
        {
            Success = false,
            Message = errorMessage,
            UpdatedSlots = new List<InventorySlotData>(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await session.SendAsync(MessageType.InventoryResponse, response);
    }
}