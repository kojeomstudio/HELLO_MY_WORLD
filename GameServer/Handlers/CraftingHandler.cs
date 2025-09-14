using GameServerApp.Database;
using SharedProtocol;
using System.Text.Json;

namespace GameServerApp.Handlers;

/// <summary>
/// 제작 요청을 처리하는 핸들러
/// 레시피 검증, 재료 소모, 결과물 생성을 담당합니다.
/// </summary>
public class CraftingHandler : MessageHandler<CraftingRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;
    private readonly CraftingSystem _craftingSystem;

    public CraftingHandler(DatabaseHelper database, SessionManager sessions, CraftingSystem craftingSystem)
        : base(MessageType.CraftingRequest)
    {
        _database = database;
        _sessions = sessions;
        _craftingSystem = craftingSystem;
    }

    protected override async Task HandleAsync(Session session, CraftingRequest message)
    {
        try
        {
            if (string.IsNullOrEmpty(session.SessionToken) || string.IsNullOrEmpty(session.UserName))
            {
                await SendFailureResponse(session, "인증되지 않은 세션입니다.");
                return;
            }

            var playerState = _sessions.GetPlayerState(session.UserName);
            if (playerState == null)
            {
                await SendFailureResponse(session, "플레이어 상태를 찾을 수 없습니다.");
                return;
            }

            if (string.IsNullOrEmpty(message.RecipeId))
            {
                await SendFailureResponse(session, "잘못된 레시피 ID입니다.");
                return;
            }

            var recipe = _craftingSystem.GetRecipe(message.RecipeId);
            if (recipe == null)
            {
                await SendFailureResponse(session, "존재하지 않는 레시피입니다.");
                return;
            }

            var craftingResult = await _craftingSystem.ProcessCraftingAsync(session.UserName, message.RecipeId, message.CraftingAmount);
            
            if (!craftingResult.Success)
            {
                await SendFailureResponse(session, craftingResult.ErrorMessage);
                return;
            }

            var response = new CraftingResponse
            {
                Success = true,
                Message = "제작이 완료되었습니다.",
                RecipeId = message.RecipeId,
                CraftedItems = ConvertToCraftedItemData(craftingResult.CraftedItems),
                UpdatedInventory = JsonSerializer.Serialize(craftingResult.UpdatedInventory),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.CraftingResponse, response);

            Console.WriteLine($"Crafting completed by {session.UserName}: Recipe {message.RecipeId} x{message.CraftingAmount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Crafting error for user '{session.UserName}': {ex.Message}");
            await SendFailureResponse(session, "제작 처리 중 오류가 발생했습니다.");
        }
    }

    private async Task SendFailureResponse(Session session, string errorMessage)
    {
        var response = new CraftingResponse
        {
            Success = false,
            Message = errorMessage,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await session.SendAsync(MessageType.CraftingResponse, response);
    }

    private List<CraftedItemData> ConvertToCraftedItemData(List<CraftedItem> craftedItems)
    {
        return craftedItems.Select(item => new CraftedItemData
        {
            ItemId = item.ItemId,
            Amount = item.Amount,
            SlotIndex = item.SlotIndex
        }).ToList();
    }
}

/// <summary>
/// 제작 시스템 코어 클래스
/// </summary>
public class CraftingSystem
{
    private readonly Dictionary<string, CraftingRecipe> _recipes;
    private readonly InventorySystem _inventorySystem;

    public CraftingSystem(InventorySystem inventorySystem)
    {
        _inventorySystem = inventorySystem;
        _recipes = new Dictionary<string, CraftingRecipe>();
        InitializeRecipes();
    }

    public CraftingRecipe? GetRecipe(string recipeId)
    {
        _recipes.TryGetValue(recipeId, out var recipe);
        return recipe;
    }

    public async Task<CraftingProcessResult> ProcessCraftingAsync(string userName, string recipeId, int amount = 1)
    {
        var recipe = GetRecipe(recipeId);
        if (recipe == null)
        {
            return new CraftingProcessResult { Success = false, ErrorMessage = "존재하지 않는 레시피입니다." };
        }

        var playerInventory = await _inventorySystem.GetPlayerInventoryAsync(userName);
        if (playerInventory == null)
        {
            return new CraftingProcessResult { Success = false, ErrorMessage = "인벤토리를 찾을 수 없습니다." };
        }

        if (!HasRequiredMaterials(playerInventory, recipe, amount))
        {
            return new CraftingProcessResult { Success = false, ErrorMessage = "재료가 부족합니다." };
        }

        ConsumeMaterials(playerInventory, recipe, amount);
        var craftedItems = AddCraftedItems(playerInventory, recipe, amount);

        await _inventorySystem.SavePlayerInventoryAsync(userName, playerInventory);

        return new CraftingProcessResult
        {
            Success = true,
            CraftedItems = craftedItems,
            UpdatedInventory = playerInventory
        };
    }

    private bool HasRequiredMaterials(PlayerInventory inventory, CraftingRecipe recipe, int craftAmount)
    {
        foreach (var ingredient in recipe.Ingredients)
        {
            int requiredAmount = ingredient.Amount * craftAmount;
            int availableAmount = 0;

            foreach (var slot in inventory.GetAllSlots())
            {
                if (slot.ItemId == ingredient.ItemId)
                {
                    availableAmount += slot.Amount;
                }
            }

            if (availableAmount < requiredAmount)
            {
                return false;
            }
        }
        return true;
    }

    private void ConsumeMaterials(PlayerInventory inventory, CraftingRecipe recipe, int craftAmount)
    {
        foreach (var ingredient in recipe.Ingredients)
        {
            int remainingToConsume = ingredient.Amount * craftAmount;

            foreach (var slot in inventory.GetAllSlots())
            {
                if (slot.ItemId == ingredient.ItemId && remainingToConsume > 0)
                {
                    int consumed = Math.Min(slot.Amount, remainingToConsume);
                    slot.Amount -= consumed;
                    remainingToConsume -= consumed;

                    if (slot.Amount <= 0)
                    {
                        slot.Clear();
                    }
                }
            }
        }
    }

    private List<CraftedItem> AddCraftedItems(PlayerInventory inventory, CraftingRecipe recipe, int craftAmount)
    {
        var craftedItems = new List<CraftedItem>();

        foreach (var result in recipe.Results)
        {
            int totalAmount = result.Amount * craftAmount;
            int remainingToAdd = totalAmount;

            while (remainingToAdd > 0)
            {
                var emptySlot = inventory.FindEmptySlot();
                if (emptySlot == null)
                {
                    break;
                }

                int maxStackSize = GetMaxStackSize(result.ItemId);
                int amountToAdd = Math.Min(remainingToAdd, maxStackSize);

                emptySlot.SetItem(result.ItemId, amountToAdd);
                remainingToAdd -= amountToAdd;

                craftedItems.Add(new CraftedItem
                {
                    ItemId = result.ItemId,
                    Amount = amountToAdd,
                    SlotIndex = emptySlot.SlotIndex
                });
            }
        }

        return craftedItems;
    }

    private int GetMaxStackSize(string itemId)
    {
        return itemId switch
        {
            "minecraft:diamond_sword" => 1,
            "minecraft:iron_sword" => 1,
            "minecraft:wooden_sword" => 1,
            "minecraft:diamond_pickaxe" => 1,
            "minecraft:iron_pickaxe" => 1,
            "minecraft:wooden_pickaxe" => 1,
            _ => 64
        };
    }

    private void InitializeRecipes()
    {
        // 기본 도구 제작 레시피
        _recipes["wooden_pickaxe"] = new CraftingRecipe
        {
            Id = "wooden_pickaxe",
            Name = "나무 곡괭이",
            Ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { ItemId = "minecraft:oak_planks", Amount = 3 },
                new CraftingIngredient { ItemId = "minecraft:stick", Amount = 2 }
            },
            Results = new List<CraftingResultItem> 
            { 
                new CraftingResultItem { ItemId = "minecraft:wooden_pickaxe", Amount = 1 }
            },
            CraftingType = CraftingType.Workbench
        };

        _recipes["stone_pickaxe"] = new CraftingRecipe
        {
            Id = "stone_pickaxe",
            Name = "돌 곡괭이",
            Ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { ItemId = "minecraft:cobblestone", Amount = 3 },
                new CraftingIngredient { ItemId = "minecraft:stick", Amount = 2 }
            },
            Results = new List<CraftingResultItem> 
            { 
                new CraftingResultItem { ItemId = "minecraft:stone_pickaxe", Amount = 1 }
            },
            CraftingType = CraftingType.Workbench
        };

        _recipes["iron_pickaxe"] = new CraftingRecipe
        {
            Id = "iron_pickaxe",
            Name = "철 곡괭이",
            Ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { ItemId = "minecraft:iron_ingot", Amount = 3 },
                new CraftingIngredient { ItemId = "minecraft:stick", Amount = 2 }
            },
            Results = new List<CraftingResultItem> 
            { 
                new CraftingResultItem { ItemId = "minecraft:iron_pickaxe", Amount = 1 }
            },
            CraftingType = CraftingType.Workbench
        };

        // 검 제작 레시피
        _recipes["wooden_sword"] = new CraftingRecipe
        {
            Id = "wooden_sword",
            Name = "나무 검",
            Ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { ItemId = "minecraft:oak_planks", Amount = 2 },
                new CraftingIngredient { ItemId = "minecraft:stick", Amount = 1 }
            },
            Results = new List<CraftingResultItem> 
            { 
                new CraftingResultItem { ItemId = "minecraft:wooden_sword", Amount = 1 }
            },
            CraftingType = CraftingType.Workbench
        };

        // 기본 블록 제작 레시피
        _recipes["oak_planks"] = new CraftingRecipe
        {
            Id = "oak_planks",
            Name = "참나무 판자",
            Ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { ItemId = "minecraft:oak_log", Amount = 1 }
            },
            Results = new List<CraftingResultItem> 
            { 
                new CraftingResultItem { ItemId = "minecraft:oak_planks", Amount = 4 }
            },
            CraftingType = CraftingType.Hand
        };

        _recipes["stick"] = new CraftingRecipe
        {
            Id = "stick",
            Name = "막대기",
            Ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { ItemId = "minecraft:oak_planks", Amount = 2 }
            },
            Results = new List<CraftingResultItem> 
            { 
                new CraftingResultItem { ItemId = "minecraft:stick", Amount = 4 }
            },
            CraftingType = CraftingType.Hand
        };

        // 제련 레시피
        _recipes["iron_ingot"] = new CraftingRecipe
        {
            Id = "iron_ingot",
            Name = "철괴",
            Ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { ItemId = "minecraft:raw_iron", Amount = 1 },
                new CraftingIngredient { ItemId = "minecraft:coal", Amount = 1 }
            },
            Results = new List<CraftingResultItem> 
            { 
                new CraftingResultItem { ItemId = "minecraft:iron_ingot", Amount = 1 }
            },
            CraftingType = CraftingType.Furnace
        };
    }

    public List<CraftingRecipe> GetAvailableRecipes(string userName, CraftingType craftingType)
    {
        return _recipes.Values
            .Where(recipe => recipe.CraftingType == craftingType)
            .ToList();
    }
}

/// <summary>
/// 제작 레시피 정보
/// </summary>
public class CraftingRecipe
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<CraftingIngredient> Ingredients { get; set; } = new();
    public List<CraftingResultItem> Results { get; set; } = new();
    public CraftingType CraftingType { get; set; }
    public int CraftingTime { get; set; } = 0; // 제작 시간 (밀리초)
}

/// <summary>
/// 제작 재료
/// </summary>
public class CraftingIngredient
{
    public string ItemId { get; set; } = string.Empty;
    public int Amount { get; set; }
}

/// <summary>
/// 제작 결과물
/// </summary>
public class CraftingResultItem
{
    public string ItemId { get; set; } = string.Empty;
    public int Amount { get; set; }
}

/// <summary>
/// 제작 완료된 아이템
/// </summary>
public class CraftedItem
{
    public string ItemId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public int SlotIndex { get; set; }
}

/// <summary>
/// 제작 처리 결과
/// </summary>
public class CraftingProcessResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public List<CraftedItem> CraftedItems { get; set; } = new();
    public PlayerInventory? UpdatedInventory { get; set; }
}

/// <summary>
/// 제작 타입
/// </summary>
public enum CraftingType
{
    Hand = 0,        // 손 제작
    Workbench = 1,   // 작업대
    Furnace = 2,     // 화로
    Anvil = 3,       // 모루
    EnchantingTable = 4 // 마법부여대
}