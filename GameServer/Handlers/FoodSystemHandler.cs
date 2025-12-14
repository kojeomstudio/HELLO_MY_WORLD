using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GameServerApp.Systems;
using SharedProtocol;
using ProtoBuf;

namespace GameServerApp.Handlers
{
    /// <summary>
    /// Handles food consumption requests from clients
    /// </summary>
    public class FoodSystemHandler : IMessageHandler
    {
        private readonly HealthAndHungerSystem _healthSystem;
        private readonly SessionManager _sessionManager;
        private readonly InventorySystem _inventorySystem;

        public FoodSystemHandler(
            HealthAndHungerSystem healthSystem,
            SessionManager sessionManager,
            InventorySystem inventorySystem)
        {
            _healthSystem = healthSystem;
            _sessionManager = sessionManager;
            _inventorySystem = inventorySystem;
        }

        public MessageType Type => MessageType.HealthActionRequest;

        public async Task HandleAsync(Session session, object message)
        {
            if (message is not HealthActionRequest request)
            {
                Console.WriteLine("Invalid message type for FoodSystemHandler");
                return;
            }

            if (string.IsNullOrEmpty(session.UserName))
            {
                await SendErrorResponse(session, "Not authenticated");
                return;
            }

            // Check if this is a food consumption request
            if (request.ActionType != 2) // 2 = Feed action
            {
                return; // Not a food consumption request
            }

            try
            {
                var result = await ProcessFoodConsumption(session, request);
                await SendResponse(session, result.Success, result.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing food consumption: {ex.Message}");
                await SendErrorResponse(session, "Server error while processing food consumption");
            }
        }

        private async Task<(bool Success, string Message)> ProcessFoodConsumption(
            Session session, 
            HealthActionRequest request)
        {
            var userName = session.UserName;
            var playerState = _sessionManager.GetPlayerState(userName);
            
            if (playerState == null)
            {
                return (false, "Player state not found");
            }

            // Check if player can eat (not already at max hunger)
            var healthData = await _healthSystem.GetPlayerHealthAsync(userName);
            if (healthData.Hunger >= healthData.MaxHunger)
            {
                return (false, "You are not hungry");
            }

            // Get food item data from request
            var foodItemId = request.WeaponItemId; // Reusing this field for food item ID
            if (foodItemId <= 0)
            {
                return (false, "Invalid food item");
            }

            // Check if player has food item in inventory
            var hasFood = await _inventorySystem.HasItemAsync(userName, foodItemId, 1);
            if (!hasFood)
            {
                return (false, "You don't have this food item");
            }

            // Get food properties from config
            var foodProperties = GetFoodProperties(foodItemId);
            if (foodProperties == null)
            {
                return (false, "This item is not edible");
            }

            // Remove food item from inventory
            var removed = await _inventorySystem.RemoveItemAsync(userName, foodItemId, 1);
            if (!removed)
            {
                return (false, "Failed to remove food item from inventory");
            }

            // Apply food effects
            var fed = await _healthSystem.FeedPlayerAsync(
                userName, 
                foodProperties.Nutrition, 
                foodProperties.Saturation);

            if (!fed)
            {
                // Return item if feeding failed
                await _inventorySystem.AddItemAsync(userName, foodItemId, 1);
                return (false, "Failed to consume food");
            }

            // Apply any additional effects
            if (foodProperties.Effects?.Count > 0)
            {
                await ApplyFoodEffects(userName, foodProperties.Effects);
            }

            Console.WriteLine($"Player {userName} consumed {foodProperties.DisplayName} " +
                             $"(+{foodProperties.Nutrition} hunger, +{foodProperties.Saturation:F1} saturation)");

            return (true, $"Consumed {foodProperties.DisplayName}");
        }

        private FoodProperties? GetFoodProperties(int foodItemId)
        {
            // In a real implementation, this would load from config/items.json
            // For now, using a simple hardcoded mapping
            return foodItemId switch
            {
                1 => new FoodProperties { ItemId = 1, ItemName = "apple", DisplayName = "Apple", Nutrition = 4, Saturation = 2.4f },
                2 => new FoodProperties { ItemId = 2, ItemName = "bread", DisplayName = "Bread", Nutrition = 5, Saturation = 6.0f },
                3 => new FoodProperties { ItemId = 3, ItemName = "cooked_beef", DisplayName = "Cooked Beef", Nutrition = 8, Saturation = 12.8f },
                4 => new FoodProperties { ItemId = 4, ItemName = "cooked_chicken", DisplayName = "Cooked Chicken", Nutrition = 6, Saturation = 7.2f },
                5 => new FoodProperties { ItemId = 5, ItemName = "cooked_pork", DisplayName = "Cooked Pork", Nutrition = 7, Saturation = 8.4f },
                6 => new FoodProperties { ItemId = 6, ItemName = "carrot", DisplayName = "Carrot", Nutrition = 3, Saturation = 3.6f },
                7 => new FoodProperties { ItemId = 7, ItemName = "potato", DisplayName = "Potato", Nutrition = 2, Saturation = 2.4f },
                8 => new FoodProperties { ItemId = 8, ItemName = "golden_apple", DisplayName = "Golden Apple", Nutrition = 4, Saturation = 9.6f },
                _ => null
            };
        }

        private async Task ApplyFoodEffects(string userName, List<FoodEffect> effects)
        {
            foreach (var effect in effects)
            {
                // Apply special food effects (like golden apple effects)
                switch (effect.Type)
                {
                    case "regeneration":
                        // TODO: Apply regeneration effect
                        Console.WriteLine($"Applied regeneration effect to {userName} for {effect.Duration}s");
                        break;
                    case "absorption":
                        // TODO: Apply absorption effect (extra health)
                        Console.WriteLine($"Applied absorption effect to {userName} for {effect.Duration}s");
                        break;
                    case "fire_resistance":
                        // TODO: Apply fire resistance effect
                        Console.WriteLine($"Applied fire resistance effect to {userName} for {effect.Duration}s");
                        break;
                }
            }
        }

        private async Task SendResponse(Session session, bool success, string message)
        {
            var response = new HealthActionResponse
            {
                Success = success,
                Message = message,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.HealthActionResponse, response);
        }

        private async Task SendErrorResponse(Session session, string errorMessage)
        {
            var response = new HealthActionResponse
            {
                Success = false,
                Message = errorMessage,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.HealthActionResponse, response);
        }
    }

    /// <summary>
    /// Represents food item properties
    /// </summary>
    public class FoodProperties
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Nutrition { get; set; } // Hunger points restored
        public float Saturation { get; set; } // Saturation points restored
        public List<FoodEffect>? Effects { get; set; }
    }

    /// <summary>
    /// Represents a special effect from food
    /// </summary>
    public class FoodEffect
    {
        public string Type { get; set; } = string.Empty;
        public int Duration { get; set; } // Duration in seconds
        public int Amplifier { get; set; } // Effect level
    }
}using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GameServerApp.Systems;
using SharedProtocol;
using ProtoBuf;

namespace GameServerApp.Handlers
{
    /// <summary>
    /// Handles food consumption requests from clients
    /// </summary>
    public class FoodSystemHandler : IMessageHandler
    {
        private readonly HealthAndHungerSystem _healthSystem;
        private readonly SessionManager _sessionManager;
        private readonly InventorySystem _inventorySystem;

        public FoodSystemHandler(
            HealthAndHungerSystem healthSystem,
            SessionManager sessionManager,
            InventorySystem inventorySystem)
        {
            _healthSystem = healthSystem;
            _sessionManager = sessionManager;
            _inventorySystem = inventorySystem;
        }

        public MessageType Type => MessageType.HealthActionRequest;

        public async Task HandleAsync(Session session, object message)
        {
            if (message is not HealthActionRequest request)
            {
                Console.WriteLine("Invalid message type for FoodSystemHandler");
                return;
            }

            if (string.IsNullOrEmpty(session.UserName))
            {
                await SendErrorResponse(session, "Not authenticated");
                return;
            }

            // Check if this is a food consumption request
            if (request.ActionType != 2) // 2 = Feed action
            {
                return; // Not a food consumption request
            }

            try
            {
                var result = await ProcessFoodConsumption(session, request);
                await SendResponse(session, result.Success, result.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing food consumption: {ex.Message}");
                await SendErrorResponse(session, "Server error while processing food consumption");
            }
        }

        private async Task<(bool Success, string Message)> ProcessFoodConsumption(
            Session session, 
            HealthActionRequest request)
        {
            var userName = session.UserName;
            var playerState = _sessionManager.GetPlayerState(userName);
            
            if (playerState == null)
            {
                return (false, "Player state not found");
            }

            // Check if player can eat (not already at max hunger)
            var healthData = await _healthSystem.GetPlayerHealthAsync(userName);
            if (healthData.Hunger >= healthData.MaxHunger)
            {
                return (false, "You are not hungry");
            }

            // Get food item data from request
            var foodItemId = request.WeaponItemId; // Reusing this field for food item ID
            if (foodItemId <= 0)
            {
                return (false, "Invalid food item");
            }

            // Check if player has food item in inventory
            var hasFood = await _inventorySystem.HasItemAsync(userName, foodItemId, 1);
            if (!hasFood)
            {
                return (false, "You don't have this food item");
            }

            // Get food properties from config
            var foodProperties = GetFoodProperties(foodItemId);
            if (foodProperties == null)
            {
                return (false, "This item is not edible");
            }

            // Remove food item from inventory
            var removed = await _inventorySystem.RemoveItemAsync(userName, foodItemId, 1);
            if (!removed)
            {
                return (false, "Failed to remove food item from inventory");
            }

            // Apply food effects
            var fed = await _healthSystem.FeedPlayerAsync(
                userName, 
                foodProperties.Nutrition, 
                foodProperties.Saturation);

            if (!fed)
            {
                // Return item if feeding failed
                await _inventorySystem.AddItemAsync(userName, foodItemId, 1);
                return (false, "Failed to consume food");
            }

            // Apply any additional effects
            if (foodProperties.Effects?.Count > 0)
            {
                await ApplyFoodEffects(userName, foodProperties.Effects);
            }

            Console.WriteLine($"Player {userName} consumed {foodProperties.DisplayName} " +
                             $"(+{foodProperties.Nutrition} hunger, +{foodProperties.Saturation:F1} saturation)");

            return (true, $"Consumed {foodProperties.DisplayName}");
        }

        private FoodProperties? GetFoodProperties(int foodItemId)
        {
            // In a real implementation, this would load from config/items.json
            // For now, using a simple hardcoded mapping
            return foodItemId switch
            {
                1 => new FoodProperties { ItemId = 1, ItemName = "apple", DisplayName = "Apple", Nutrition = 4, Saturation = 2.4f },
                2 => new FoodProperties { ItemId = 2, ItemName = "bread", DisplayName = "Bread", Nutrition = 5, Saturation = 6.0f },
                3 => new FoodProperties { ItemId = 3, ItemName = "cooked_beef", DisplayName = "Cooked Beef", Nutrition = 8, Saturation = 12.8f },
                4 => new FoodProperties { ItemId = 4, ItemName = "cooked_chicken", DisplayName = "Cooked Chicken", Nutrition = 6, Saturation = 7.2f },
                5 => new FoodProperties { ItemId = 5, ItemName = "cooked_pork", DisplayName = "Cooked Pork", Nutrition = 7, Saturation = 8.4f },
                6 => new FoodProperties { ItemId = 6, ItemName = "carrot", DisplayName = "Carrot", Nutrition = 3, Saturation = 3.6f },
                7 => new FoodProperties { ItemId = 7, ItemName = "potato", DisplayName = "Potato", Nutrition = 2, Saturation = 2.4f },
                8 => new FoodProperties { ItemId = 8, ItemName = "golden_apple", DisplayName = "Golden Apple", Nutrition = 4, Saturation = 9.6f },
                _ => null
            };
        }

        private async Task ApplyFoodEffects(string userName, List<FoodEffect> effects)
        {
            foreach (var effect in effects)
            {
                // Apply special food effects (like golden apple effects)
                switch (effect.Type)
                {
                    case "regeneration":
                        // TODO: Apply regeneration effect
                        Console.WriteLine($"Applied regeneration effect to {userName} for {effect.Duration}s");
                        break;
                    case "absorption":
                        // TODO: Apply absorption effect (extra health)
                        Console.WriteLine($"Applied absorption effect to {userName} for {effect.Duration}s");
                        break;
                    case "fire_resistance":
                        // TODO: Apply fire resistance effect
                        Console.WriteLine($"Applied fire resistance effect to {userName} for {effect.Duration}s");
                        break;
                }
            }
        }

        private async Task SendResponse(Session session, bool success, string message)
        {
            var response = new HealthActionResponse
            {
                Success = success,
                Message = message,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.HealthActionResponse, response);
        }

        private async Task SendErrorResponse(Session session, string errorMessage)
        {
            var response = new HealthActionResponse
            {
                Success = false,
                Message = errorMessage,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.HealthActionResponse, response);
        }
    }

    /// <summary>
    /// Represents food item properties
    /// </summary>
    public class FoodProperties
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Nutrition { get; set; } // Hunger points restored
        public float Saturation { get; set; } // Saturation points restored
        public List<FoodEffect>? Effects { get; set; }
    }

    /// <summary>
    /// Represents a special effect from food
    /// </summary>
    public class FoodEffect
    {
        public string Type { get; set; } = string.Empty;
        public int Duration { get; set; } // Duration in seconds
        public int Amplifier { get; set; } // Effect level
    }
}
