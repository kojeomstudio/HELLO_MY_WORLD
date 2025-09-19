using System.Collections.Generic;
using System.Text;
using SharedProtocol;
using UnityEngine;
using UnityEngine.UI;

namespace Minecraft.Multiplayer
{
    /// <summary>
    /// Lightweight overlay that lists available rooms and shows join status.
    /// </summary>
    public class RoomBrowserOverlay : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _panel;
        private Text _titleText;
        private Text _roomListText;
        private Text _statusText;

        private readonly Vector2 _panelSize = new Vector2(420f, 220f);

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
            _canvas.sortingOrder = 4500;

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
            _panel.anchorMin = new Vector2(1f, 0f);
            _panel.anchorMax = new Vector2(1f, 0f);
            _panel.pivot = new Vector2(1f, 0f);
            _panel.sizeDelta = _panelSize;
            _panel.anchoredPosition = new Vector2(-20f, 20f);

            var image = panelObj.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.65f);

            _titleText = CreateText("Title", new Vector2(-12f, -16f), 24, FontStyle.Bold);
            _titleText.alignment = TextAnchor.UpperRight;
            _titleText.text = "Rooms";

            _roomListText = CreateText("RoomList", new Vector2(-12f, -52f), 18, FontStyle.Normal);
            _roomListText.rectTransform.sizeDelta = new Vector2(_panelSize.x - 24f, 140f);
            _roomListText.alignment = TextAnchor.UpperRight;

            _statusText = CreateText("Status", new Vector2(-12f, -200f), 16, FontStyle.Normal);
            _statusText.rectTransform.sizeDelta = new Vector2(_panelSize.x - 24f, 20f);
            _statusText.alignment = TextAnchor.UpperRight;
        }

        private Text CreateText(string name, Vector2 anchoredPosition, int fontSize, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_panel, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(_panelSize.x - 24f, 28f);

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

        public void ShowRooms(IReadOnlyList<RoomInfo> rooms, int selectedIndex)
        {
            if (rooms == null || rooms.Count == 0)
            {
                _roomListText.text = "No rooms available.";
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, rooms.Count - 1);
            var builder = new StringBuilder();
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                var prefix = i == selectedIndex ? "> " : "  ";
                builder.Append(prefix);
                builder.Append(room.DisplayName);
                builder.Append(" [");
                builder.Append(room.PlayerCount);
                builder.Append('/');
                builder.Append(room.Capacity <= 0 ? "∞" : room.Capacity.ToString());
                builder.Append("] (#");
                builder.Append(room.RoomId);
                builder.Append(")\n");
            }

            _roomListText.text = builder.ToString();
        }

        public void ShowStatus(string message, Color color)
        {
            _statusText.text = message;
            _statusText.color = color;
        }
    }
}
