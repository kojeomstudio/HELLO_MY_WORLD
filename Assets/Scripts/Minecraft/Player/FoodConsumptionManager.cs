using System;
using System.Collections.Generic;
using UnityEngine;
using SharedProtocol;
using Minecraft.Core;

namespace Minecraft.Player
{
    /// <summary>
    /// Client-side food consumption manager
    /// Handles eating animations, timing, and server communication
    /// </summary>
    public class FoodConsumptionManager : MonoBehaviour
    {
        [Header("Food Consumption Settings")]
        [SerializeField] private float eatingDuration = 1.6f; // Seconds to eat food
        [SerializeField] private KeyCode eatKey = KeyCode.E;
        
        private MinecraftGameClient _gameClient;
        private PlayerUI _playerUI;
        private MinecraftPlayerController _playerController;
        
        private bool _isEating;
        private float _eatingStartTime;
        private ItemInfo _currentFood;
        
        // Food properties (in real game, this would be loaded from config)
        private readonly Dictionary<int, FoodProperties> _foodProperties = new()
        {
            { 1, new FoodProperties { ItemId = 1, ItemName = "apple", DisplayName = "Apple", Nutrition = 4, Saturation = 2.4f } },
            { 2, new FoodProperties { ItemId = 2, ItemName = "bread", DisplayName = "Bread", Nutrition = 5, Saturation = 6.0f } },
            { 3, new FoodProperties { ItemId = 3, ItemName = "cooked_beef", DisplayName = "Cooked Beef", Nutrition = 8, Saturation = 12.8f } },
            { 4, new FoodProperties { ItemId = 4, ItemName = "cooked_chicken", DisplayName = "Cooked Chicken", Nutrition = 6, Saturation = 7.2f } },
            { 5, new FoodProperties { ItemId = 5, ItemName = "cooked_pork", DisplayName = "Cooked Pork", Nutrition = 7, Saturation = 8.4f } },
            { 6, new FoodProperties { ItemId = 6, ItemName = "carrot", DisplayName = "Carrot", Nutrition = 3, Saturation = 3.6f } },
            { 7, new FoodProperties { ItemId = 7, ItemName = "potato", DisplayName = "Potato", Nutrition = 2, Saturation = 2.4f } },
            { 8, new FoodProperties { ItemId = 8, ItemName = "golden_apple", DisplayName = "Golden Apple", Nutrition = 4, Saturation = 9.6f } }
        };
        
        public bool IsEating => _isEating;
        public float EatingProgress => _isEating ? (Time.time - _eatingStartTime) / eatingDuration : 0f;
        
        private void Awake()
        {
            _gameClient = FindObjectOfType<MinecraftGameClient>();
            _playerUI = FindObjectOfType<PlayerUI>();
            _playerController = GetComponent<MinecraftPlayerController>();
        }
        
        private void Update()
        {
            HandleFoodConsumptionInput();
            UpdateEatingAnimation();
        }
        
        private void HandleFoodConsumptionInput()
        {
            if (Input.GetKeyDown(eatKey))
            {
                TryStartEating();
            }
            else if (Input.GetKeyUp(eatKey))
            {
                // Cancel eating if key is released early
                if (_isEating && (Time.time - _eatingStartTime) < eatingDuration)
                {
                    CancelEating();
                }
            }
            else if (_isEating && (Time.time - _eatingStartTime) >= eatingDuration)
            {
                // Complete eating
                CompleteEating();
            }
        }
        
        private void TryStartEating()
        {
            if (_isEating)
            {
                return; // Already eating
            }
            
            var selectedItem = _playerController?.SelectedItem;
            if (selectedItem == null || selectedItem.Type != ItemType.Food)
            {
                return; // No food item selected
            }
            
            // Check if we can eat this food (hunger not full)
            if (IsHungerFull())
            {
                ShowMessage("You are not hungry!");
                return;
            }
            
            _currentFood = selectedItem;
            _isEating = true;
            _eatingStartTime = Time.time;
            
            // Show eating progress in UI
            _playerUI?.ShowEatingProgress(_currentFood.DisplayName);
            
            Debug.Log($"Started eating {_currentFood.DisplayName}");
        }
        
        private void CancelEating()
        {
            if (!_isEating) return;
            
            _isEating = false;
            _currentFood = null;
            
            // Hide eating progress
            _playerUI?.HideEatingProgress();
            
            Debug.Log("Cancelled eating");
        }
        
        private void CompleteEating()
        {
            if (!_isEating || _currentFood == null) return;
            
            // Send food consumption request to server
            SendFoodConsumptionRequest(_currentFood);
            
            // Reset eating state
            _isEating = false;
            _currentFood = null;
            
            // Hide eating progress
            _playerUI?.HideEatingProgress();
            
            Debug.Log($"Completed eating {_currentFood.DisplayName}");
        }
        
        private void SendFoodConsumptionRequest(ItemInfo foodItem)
        {
            if (_gameClient == null) return;
            
            // Create health action request for food consumption
            var request = new HealthActionRequest
            {
                ActionType = 2, // 2 = Feed action
                Amount = 1, // Consuming 1 item
                WeaponItemId = foodItem.Id, // Reusing this field for food item ID
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            
            // Send to server
            _gameClient.SendMessage(MessageType.HealthActionRequest, request);
            
            Debug.Log($"Sent food consumption request for {foodItem.Name}");
        }
        
        private void UpdateEatingAnimation()
        {
            // TODO: Implement eating animation (hand raising/lowering)
            // This would involve:
            // 1. Raising the hand item when eating starts
            // 2. Lowering the hand when eating completes/cancels
            // 3. Playing eating sounds at appropriate intervals
        }
        
        private bool IsHungerFull()
        {
            // In a real implementation, this would check player's current hunger
            // For now, we'll assume we can always eat (server will validate)
            return false;
        }
        
        private void ShowMessage(string message)
        {
            // TODO: Display message in chat or UI
            Debug.Log(message);
        }
        
        /// <summary>
        /// Called when server responds to food consumption request
        /// </summary>
        public void OnFoodConsumptionResponse(HealthActionResponse response)
        {
            if (response.Success)
            {
                ShowMessage(response.Message);
                Debug.Log($"Food consumption successful: {response.Message}");
            }
            else
            {
                ShowMessage($"Failed to eat: {response.Message}");
                Debug.Log($"Food consumption failed: {response.Message}");
                
                // Return item to inventory if consumption failed
                // This would be handled by the server, but we can show feedback
            }
        }
        
        /// <summary>
        /// Get food properties for an item
        /// </summary>
        public FoodProperties? GetFoodProperties(int itemId)
        {
            return _foodProperties.TryGetValue(itemId, out var properties) ? properties : null;
        }
    }
    
    /// <summary>
    /// Client-side food properties
    /// </summary>
    public class FoodProperties
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Nutrition { get; set; } // Hunger points restored
        public float Saturation { get; set; } // Saturation points restored
    }
}using System.Collections.Generic;
using UnityEngine;
using SharedProtocol;
using Minecraft.Core;

namespace Minecraft.Player
{
    /// <summary>
    /// Client-side food consumption manager
    /// Handles eating animations, timing, and server communication
    /// </summary>
    public class FoodConsumptionManager : MonoBehaviour
    {
        [Header("Food Consumption Settings")]
        [SerializeField] private float eatingDuration = 1.6f; // Seconds to eat food
        [SerializeField] private KeyCode eatKey = KeyCode.E;
        
        private MinecraftGameClient _gameClient;
        private PlayerUI _playerUI;
        private MinecraftPlayerController _playerController;
        
        private bool _isEating;
        private float _eatingStartTime;
        private ItemInfo _currentFood;
        
        // Food properties (in real game, this would be loaded from config)
        private readonly Dictionary<int, FoodProperties> _foodProperties = new()
        {
            { 1, new FoodProperties { ItemId = 1, ItemName = "apple", DisplayName = "Apple", Nutrition = 4, Saturation = 2.4f } },
            { 2, new FoodProperties { ItemId = 2, ItemName = "bread", DisplayName = "Bread", Nutrition = 5, Saturation = 6.0f } },
            { 3, new FoodProperties { ItemId = 3, ItemName = "cooked_beef", DisplayName = "Cooked Beef", Nutrition = 8, Saturation = 12.8f } },
            { 4, new FoodProperties { ItemId = 4, ItemName = "cooked_chicken", DisplayName = "Cooked Chicken", Nutrition = 6, Saturation = 7.2f } },
            { 5, new FoodProperties { ItemId = 5, ItemName = "cooked_pork", DisplayName = "Cooked Pork", Nutrition = 7, Saturation = 8.4f } },
            { 6, new FoodProperties { ItemId = 6, ItemName = "carrot", DisplayName = "Carrot", Nutrition = 3, Saturation = 3.6f } },
            { 7, new FoodProperties { ItemId = 7, ItemName = "potato", DisplayName = "Potato", Nutrition = 2, Saturation = 2.4f } },
            { 8, new FoodProperties { ItemId = 8, ItemName = "golden_apple", DisplayName = "Golden Apple", Nutrition = 4, Saturation = 9.6f } }
        };
        
        public bool IsEating => _isEating;
        public float EatingProgress => _isEating ? (Time.time - _eatingStartTime) / eatingDuration : 0f;
        
        private void Awake()
        {
            _gameClient = FindObjectOfType<MinecraftGameClient>();
            _playerUI = FindObjectOfType<PlayerUI>();
            _playerController = GetComponent<MinecraftPlayerController>();
        }
        
        private void Update()
        {
            HandleFoodConsumptionInput();
            UpdateEatingAnimation();
        }
        
        private void HandleFoodConsumptionInput()
        {
            if (Input.GetKeyDown(eatKey))
            {
                TryStartEating();
            }
            else if (Input.GetKeyUp(eatKey))
            {
                // Cancel eating if key is released early
                if (_isEating && (Time.time - _eatingStartTime) < eatingDuration)
                {
                    CancelEating();
                }
            }
            else if (_isEating && (Time.time - _eatingStartTime) >= eatingDuration)
            {
                // Complete eating
                CompleteEating();
            }
        }
        
        private void TryStartEating()
        {
            if (_isEating)
            {
                return; // Already eating
            }
            
            var selectedItem = _playerController?.SelectedItem;
            if (selectedItem == null || selectedItem.Type != ItemType.Food)
            {
                return; // No food item selected
            }
            
            // Check if we can eat this food (hunger not full)
            if (IsHungerFull())
            {
                ShowMessage("You are not hungry!");
                return;
            }
            
            _currentFood = selectedItem;
            _isEating = true;
            _eatingStartTime = Time.time;
            
            // Show eating progress in UI
            _playerUI?.ShowEatingProgress(_currentFood.DisplayName);
            
            Debug.Log($"Started eating {_currentFood.DisplayName}");
        }
        
        private void CancelEating()
        {
            if (!_isEating) return;
            
            _isEating = false;
            _currentFood = null;
            
            // Hide eating progress
            _playerUI?.HideEatingProgress();
            
            Debug.Log("Cancelled eating");
        }
        
        private void CompleteEating()
        {
            if (!_isEating || _currentFood == null) return;
            
            // Send food consumption request to server
            SendFoodConsumptionRequest(_currentFood);
            
            // Reset eating state
            _isEating = false;
            _currentFood = null;
            
            // Hide eating progress
            _playerUI?.HideEatingProgress();
            
            Debug.Log($"Completed eating {_currentFood.DisplayName}");
        }
        
        private void SendFoodConsumptionRequest(ItemInfo foodItem)
        {
            if (_gameClient == null) return;
            
            // Create health action request for food consumption
            var request = new HealthActionRequest
            {
                ActionType = 2, // 2 = Feed action
                Amount = 1, // Consuming 1 item
                WeaponItemId = foodItem.Id, // Reusing this field for food item ID
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            
            // Send to server
            _gameClient.SendMessage(MessageType.HealthActionRequest, request);
            
            Debug.Log($"Sent food consumption request for {foodItem.Name}");
        }
        
        private void UpdateEatingAnimation()
        {
            // TODO: Implement eating animation (hand raising/lowering)
            // This would involve:
            // 1. Raising the hand item when eating starts
            // 2. Lowering the hand when eating completes/cancels
            // 3. Playing eating sounds at appropriate intervals
        }
        
        private bool IsHungerFull()
        {
            // In a real implementation, this would check player's current hunger
            // For now, we'll assume we can always eat (server will validate)
            return false;
        }
        
        private void ShowMessage(string message)
        {
            // TODO: Display message in chat or UI
            Debug.Log(message);
        }
        
        /// <summary>
        /// Called when server responds to food consumption request
        /// </summary>
        public void OnFoodConsumptionResponse(HealthActionResponse response)
        {
            if (response.Success)
            {
                ShowMessage(response.Message);
                Debug.Log($"Food consumption successful: {response.Message}");
            }
            else
            {
                ShowMessage($"Failed to eat: {response.Message}");
                Debug.Log($"Food consumption failed: {response.Message}");
                
                // Return item to inventory if consumption failed
                // This would be handled by the server, but we can show feedback
            }
        }
        
        /// <summary>
        /// Get food properties for an item
        /// </summary>
        public FoodProperties? GetFoodProperties(int itemId)
        {
            return _foodProperties.TryGetValue(itemId, out var properties) ? properties : null;
        }
    }
    
    /// <summary>
    /// Client-side food properties
    /// </summary>
    public class FoodProperties
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Nutrition { get; set; } // Hunger points restored
        public float Saturation { get; set; } // Saturation points restored
    }
}
}
