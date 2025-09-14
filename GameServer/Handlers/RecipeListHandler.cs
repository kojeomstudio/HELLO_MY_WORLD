using GameServerApp.Database;
using SharedProtocol;

namespace GameServerApp.Handlers;

/// <summary>
/// 제작 레시피 목록 요청을 처리하는 핸들러
/// </summary>
public class RecipeListHandler : MessageHandler<RecipeListRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;
    private readonly CraftingSystem _craftingSystem;

    public RecipeListHandler(DatabaseHelper database, SessionManager sessions, CraftingSystem craftingSystem)
        : base(MessageType.RecipeListRequest)
    {
        _database = database;
        _sessions = sessions;
        _craftingSystem = craftingSystem;
    }

    protected override async Task HandleAsync(Session session, RecipeListRequest message)
    {
        try
        {
            if (string.IsNullOrEmpty(session.SessionToken) || string.IsNullOrEmpty(session.UserName))
            {
                await SendFailureResponse(session, "인증되지 않은 세션입니다.");
                return;
            }

            var craftingType = (CraftingType)message.CraftingType;
            if (message.CraftingType == -1)
            {
                // 모든 레시피 반환
                var allRecipes = GetAllRecipesAsData();
                var response = new RecipeListResponse
                {
                    Success = true,
                    Recipes = allRecipes,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                await session.SendAsync(MessageType.RecipeListResponse, response);
            }
            else
            {
                // 특정 제작 타입의 레시피만 반환
                var recipes = _craftingSystem.GetAvailableRecipes(session.UserName, craftingType);
                var recipeData = ConvertToRecipeData(recipes);
                
                var response = new RecipeListResponse
                {
                    Success = true,
                    Recipes = recipeData,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                await session.SendAsync(MessageType.RecipeListResponse, response);
            }

            Console.WriteLine($"Recipe list sent to {session.UserName}: Type={message.CraftingType}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Recipe list error for user '{session.UserName}': {ex.Message}");
            await SendFailureResponse(session, "레시피 목록 조회 중 오류가 발생했습니다.");
        }
    }

    private async Task SendFailureResponse(Session session, string errorMessage)
    {
        var response = new RecipeListResponse
        {
            Success = false,
            Recipes = new List<RecipeData>(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await session.SendAsync(MessageType.RecipeListResponse, response);
    }

    private List<RecipeData> ConvertToRecipeData(List<CraftingRecipe> recipes)
    {
        var recipeData = new List<RecipeData>();

        foreach (var recipe in recipes)
        {
            var data = new RecipeData
            {
                RecipeId = recipe.Id,
                Name = recipe.Name,
                CraftingType = (int)recipe.CraftingType,
                CraftingTime = recipe.CraftingTime
            };

            // 재료 변환
            foreach (var ingredient in recipe.Ingredients)
            {
                data.Ingredients.Add(new CraftingIngredientData
                {
                    ItemId = ingredient.ItemId,
                    Amount = ingredient.Amount
                });
            }

            // 결과물 변환
            foreach (var result in recipe.Results)
            {
                data.Results.Add(new CraftingResultData
                {
                    ItemId = result.ItemId,
                    Amount = result.Amount
                });
            }

            recipeData.Add(data);
        }

        return recipeData;
    }

    private List<RecipeData> GetAllRecipesAsData()
    {
        var allRecipes = new List<CraftingRecipe>();
        
        // 모든 제작 타입의 레시피를 수집
        foreach (CraftingType craftingType in Enum.GetValues<CraftingType>())
        {
            var recipes = _craftingSystem.GetAvailableRecipes("", craftingType);
            allRecipes.AddRange(recipes);
        }

        return ConvertToRecipeData(allRecipes);
    }
}