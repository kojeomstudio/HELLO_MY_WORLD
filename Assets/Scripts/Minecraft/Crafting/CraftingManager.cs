using System.Collections.Generic;
using System.Linq;
using SharedProtocol;
using UnityEngine;
using Minecraft.Core;

namespace Minecraft.Crafting
{
    /// <summary>
    /// Bridges the MinecraftGameClient crafting API with a basic keyboard-driven overlay.
    /// </summary>
    public class CraftingManager : MonoBehaviour
    {
        [Header("Key Bindings")]
        [SerializeField] private KeyCode toggleOverlayKey = KeyCode.C;
        [SerializeField] private KeyCode craftKey = KeyCode.Return;
        [SerializeField] private KeyCode refreshKey = KeyCode.R;
        [SerializeField] private KeyCode increaseAmountKey = KeyCode.Equals; // '=' key
        [SerializeField] private KeyCode decreaseAmountKey = KeyCode.Minus;  // '-' key

        [Header("Crafting Settings")]
        [SerializeField] private CraftingType defaultCraftingType = CraftingType.Workbench;
        [SerializeField] private int maxCraftAmount = 16;

        private MinecraftGameClient _client;
        private CraftingOverlay _overlay;
        private readonly List<RecipeData> _currentRecipes = new();
        private int _selectedRecipeIndex;
        private bool _overlayVisible;
        private int _craftAmount = 1;

        private void Awake()
        {
            _client = FindObjectOfType<MinecraftGameClient>();
            if (_client == null)
            {
                Debug.LogWarning("CraftingManager disabled: MinecraftGameClient not found in scene.");
                enabled = false;
                return;
            }

            var overlayObj = new GameObject("CraftingOverlay");
            DontDestroyOnLoad(overlayObj);
            _overlay = overlayObj.AddComponent<CraftingOverlay>();
            _overlay.Initialize();

            _client.RecipeListReceived += OnRecipeListReceived;
            _client.CraftingCompleted += OnCraftingCompleted;
        }

        private void OnDestroy()
        {
            if (_client != null)
            {
                _client.RecipeListReceived -= OnRecipeListReceived;
                _client.CraftingCompleted -= OnCraftingCompleted;
            }

            if (_overlay != null)
            {
                Destroy(_overlay.gameObject);
            }
        }

        private void Update()
        {
            if (_client == null || !_client.IsConnected || string.IsNullOrEmpty(_client.SessionToken))
            {
                return;
            }

            if (Input.GetKeyDown(toggleOverlayKey))
            {
                ToggleOverlay();
            }

            if (!_overlayVisible)
            {
                return;
            }

            HandleNavigationInput();
            HandleCraftingInput();
        }

        private void ToggleOverlay()
        {
            _overlayVisible = !_overlayVisible;
            _overlay.SetVisible(_overlayVisible);

            if (_overlayVisible)
            {
                if (_currentRecipes.Count == 0)
                {
                    RequestRecipes();
                    _overlay.ShowStatus("Requesting recipes...", new Color(0.9f, 0.8f, 0.3f));
                }
                else
                {
                    RefreshOverlay();
                }
            }
        }

        private void RequestRecipes()
        {
            _client.RequestRecipes(defaultCraftingType);
        }

        private void HandleNavigationInput()
        {
            if (_currentRecipes.Count == 0)
            {
                return;
            }

            bool dirty = false;

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _selectedRecipeIndex = (_selectedRecipeIndex - 1 + _currentRecipes.Count) % _currentRecipes.Count;
                dirty = true;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _selectedRecipeIndex = (_selectedRecipeIndex + 1) % _currentRecipes.Count;
                dirty = true;
            }

            if (Input.GetKeyDown(increaseAmountKey))
            {
                _craftAmount = Mathf.Clamp(_craftAmount + 1, 1, maxCraftAmount);
                dirty = true;
            }
            else if (Input.GetKeyDown(decreaseAmountKey))
            {
                _craftAmount = Mathf.Clamp(_craftAmount - 1, 1, maxCraftAmount);
                dirty = true;
            }

            if (dirty)
            {
                RefreshOverlay();
            }
        }

        private void HandleCraftingInput()
        {
            if (Input.GetKeyDown(refreshKey))
            {
                RequestRecipes();
                _overlay.ShowStatus("Refreshing recipes...", new Color(0.9f, 0.8f, 0.3f));
            }

            if (Input.GetKeyDown(craftKey))
            {
                if (_currentRecipes.Count == 0)
                {
                    _overlay.ShowStatus("No recipe selected.", Color.red);
                    return;
                }

                var recipe = _currentRecipes[_selectedRecipeIndex];
                var type = (CraftingType)recipe.CraftingType;
                var amount = Mathf.Clamp(_craftAmount, 1, maxCraftAmount);
                _overlay.ShowStatus($"Crafting {amount}x {recipe.Name}...", new Color(0.7f, 0.9f, 0.7f));
                _client.SendCraftingRequest(recipe.RecipeId, amount, type);
            }
        }

        private void OnRecipeListReceived(IReadOnlyList<RecipeData> recipes)
        {
            _currentRecipes.Clear();
            if (recipes != null)
            {
                _currentRecipes.AddRange(recipes.Where(r => !string.IsNullOrEmpty(r.RecipeId)));
            }

            _selectedRecipeIndex = Mathf.Clamp(_selectedRecipeIndex, 0, Mathf.Max(0, _currentRecipes.Count - 1));

            if (_overlayVisible)
            {
                RefreshOverlay();
                _overlay.ShowStatus($"Loaded {_currentRecipes.Count} recipes.", Color.cyan);
            }
        }

        private void OnCraftingCompleted(CraftingResponse response)
        {
            if (!_overlayVisible)
            {
                return;
            }

            if (response.Success)
            {
                var craftedSummary = response.CraftedItems?.Count > 0
                    ? string.Join(", ", response.CraftedItems.Select(item => $"{item.Amount}x {item.ItemId}"))
                    : "Nothing";
                _overlay.ShowStatus($"Crafting complete: {craftedSummary}", Color.green);
            }
            else
            {
                var message = string.IsNullOrEmpty(response.Message) ? "Crafting failed." : response.Message;
                _overlay.ShowStatus(message, Color.red);
            }
        }

        private void RefreshOverlay()
        {
            if (_currentRecipes.Count == 0)
            {
                _overlay.ShowRecipes(_currentRecipes, _selectedRecipeIndex);
                _overlay.ShowStatus("No recipes returned.", Color.yellow);
                return;
            }

            _overlay.ShowRecipes(_currentRecipes, _selectedRecipeIndex);
            var info = _currentRecipes[_selectedRecipeIndex];
            _overlay.ShowStatus($"Selected: {info.Name} (x{_craftAmount})", Color.white);
        }
    }
}
