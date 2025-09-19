using System.Collections.Generic;
using System.Text;
using SharedProtocol;
using UnityEngine;
using UnityEngine.UI;

namespace Minecraft.Crafting
{
    /// <summary>
    /// Lightweight runtime UI overlay that lists crafting recipes and results.
    /// </summary>
    public class CraftingOverlay : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _panel;
        private Text _titleText;
        private Text _recipeListText;
        private Text _detailsText;
        private Text _statusText;

        private readonly Vector2 _panelSize = new Vector2(420f, 360f);

        public void Initialize()
        {
            BuildCanvas();
            BuildLayout();
            SetVisible(false);
        }

        private void BuildCanvas()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5000;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            gameObject.AddComponent<GraphicRaycaster>();
        }

        private void BuildLayout()
        {
            var panelObj = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(_canvas.transform, false);
            _panel = panelObj.GetComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0f, 0f);
            _panel.anchorMax = new Vector2(0f, 0f);
            _panel.pivot = new Vector2(0f, 0f);
            _panel.sizeDelta = _panelSize;
            _panel.anchoredPosition = new Vector2(20f, 20f);

            var image = panelObj.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.7f);

            _titleText = CreateText("Title", new Vector2(12f, -16f), 26, FontStyle.Bold);
            _titleText.text = "Crafting";

            _recipeListText = CreateText("RecipeList", new Vector2(12f, -52f), 20, FontStyle.Normal);
            _recipeListText.rectTransform.sizeDelta = new Vector2(_panelSize.x - 24f, 160f);
            _recipeListText.alignment = TextAnchor.UpperLeft;

            _detailsText = CreateText("RecipeDetails", new Vector2(12f, -220f), 18, FontStyle.Normal);
            _detailsText.rectTransform.sizeDelta = new Vector2(_panelSize.x - 24f, 90f);
            _detailsText.alignment = TextAnchor.UpperLeft;

            _statusText = CreateText("Status", new Vector2(12f, -320f), 18, FontStyle.Normal);
            _statusText.rectTransform.sizeDelta = new Vector2(_panelSize.x - 24f, 24f);
            _statusText.alignment = TextAnchor.MiddleLeft;
        }

        private Text CreateText(string name, Vector2 anchoredPosition, int fontSize, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_panel, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(_panelSize.x - 24f, 32f);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.text = string.Empty;
            return text;
        }

        public void SetVisible(bool visible)
        {
            if (_canvas != null)
            {
                _canvas.enabled = visible;
            }
        }

        public void ShowRecipes(IReadOnlyList<RecipeData> recipes, int selectedIndex)
        {
            if (recipes == null || recipes.Count == 0)
            {
                _recipeListText.text = "No recipes available.";
                _detailsText.text = string.Empty;
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, recipes.Count - 1);

            var listBuilder = new StringBuilder();
            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                var prefix = i == selectedIndex ? "> " : "  ";
                listBuilder.Append(prefix);
                listBuilder.Append(recipe.Name);
                if (!string.IsNullOrEmpty(recipe.RecipeId))
                {
                    listBuilder.Append(" (" + recipe.RecipeId + ")");
                }
                listBuilder.Append('\n');
            }

            _recipeListText.text = listBuilder.ToString();

            var selectedRecipe = recipes[selectedIndex];
            var detailsBuilder = new StringBuilder();
            detailsBuilder.AppendLine("Ingredients:");
            if (selectedRecipe.Ingredients.Count == 0)
            {
                detailsBuilder.AppendLine("  (none)");
            }
            else
            {
                foreach (var ingredient in selectedRecipe.Ingredients)
                {
                    detailsBuilder.AppendLine($"  - {ingredient.Amount}x {ingredient.ItemId}");
                }
            }

            detailsBuilder.AppendLine();
            detailsBuilder.AppendLine("Results:");
            if (selectedRecipe.Results.Count == 0)
            {
                detailsBuilder.AppendLine("  (none)");
            }
            else
            {
                foreach (var result in selectedRecipe.Results)
                {
                    detailsBuilder.AppendLine($"  - {result.Amount}x {result.ItemId}");
                }
            }

            detailsBuilder.AppendLine();
            detailsBuilder.AppendLine($"Type: {(CraftingType)selectedRecipe.CraftingType}");
            if (selectedRecipe.CraftingTime > 0)
            {
                detailsBuilder.AppendLine($"Time: {selectedRecipe.CraftingTime} ms");
            }

            _detailsText.text = detailsBuilder.ToString();
        }

        public void ShowStatus(string message, Color color)
        {
            _statusText.text = message;
            _statusText.color = color;
        }
    }
}
