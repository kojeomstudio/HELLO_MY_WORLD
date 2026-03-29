using System;
using System.Collections.Generic;
using System.Text;
using Minecraft.Core;
using SharedProtocol;
using UnityEngine;

namespace Minecraft.UI
{
    /// <summary>
    /// Spawns lightweight world-space popups when combat damage events arrive from the server.
    /// </summary>
    [AddComponentMenu("Minecraft/UI/Combat Damage Popup Controller")]
    public sealed class CombatDamagePopupController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MinecraftGameClient gameClient;
        [SerializeField] private Camera worldCamera;

        [Header("Popup Settings")]
        [SerializeField] private float popupLifetime = 1.5f;
        [SerializeField] private float riseSpeed = 1.4f;
        [SerializeField] private float spawnHeightOffset = 2.0f;
        [SerializeField] private float horizontalJitter = 0.18f;
        [SerializeField] private int fontSize = 48;
        [SerializeField] private float characterSize = 0.18f;
        [SerializeField] private float fadeOutExponent = 1.35f;

        [Header("Color Settings")]
        [SerializeField] private Color defaultColor = new(1f, 0.85f, 0.3f);
        [SerializeField] private Color criticalColor = new(1f, 0.35f, 0.35f);
        [SerializeField] private Color blockedColor = new(0.65f, 0.85f, 1f);

        private readonly List<PopupInstance> _activePopups = new();
        private readonly Stack<PopupInstance> _pool = new();

        private void Awake()
        {
            if (gameClient == null)
            {
                gameClient = FindObjectOfType<MinecraftGameClient>();
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            if (gameClient != null)
            {
                gameClient.CombatEventReceived += HandleCombatEvent;
            }
        }

        private void OnDisable()
        {
            if (gameClient != null)
            {
                gameClient.CombatEventReceived -= HandleCombatEvent;
            }

            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                ReturnToPool(_activePopups[i]);
            }

            _activePopups.Clear();
        }

        private void Update()
        {
            if (_activePopups.Count == 0)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                var popup = _activePopups[i];
                popup.Age += deltaTime;
                popup.Position += popup.Velocity * deltaTime;
                popup.Transform.position = popup.Position;

                if (worldCamera != null)
                {
                    var direction = popup.Transform.position - worldCamera.transform.position;
                    if (direction.sqrMagnitude > 0.0001f)
                    {
                        popup.Transform.rotation = Quaternion.LookRotation(direction);
                        popup.Transform.Rotate(0f, 180f, 0f);
                    }
                }

                var normalized = Mathf.Clamp01(popup.Age / Mathf.Max(0.01f, popup.Lifetime));
                var alpha = Mathf.Pow(1f - normalized, fadeOutExponent);
                var color = popup.BaseColor;
                color.a = alpha;
                popup.Text.color = color;

                if (popup.Age >= popup.Lifetime)
                {
                    ReturnToPool(popup);
                    _activePopups.RemoveAt(i);
                }
            }
        }

        private void HandleCombatEvent(CombatEventMessage message)
        {
            if (message == null || gameClient == null)
            {
                return;
            }

            if (!TryResolveWorldPosition(message, out var anchorPosition))
            {
                return;
            }

            SpawnPopup(anchorPosition, message);
        }

        private void SpawnPopup(Vector3 anchorPosition, CombatEventMessage message)
        {
            var popup = RentPopup();
            popup.Lifetime = Mathf.Max(0.25f, popupLifetime);
            popup.Age = 0f;
            popup.BaseColor = ResolveColor(message);
            popup.Position = anchorPosition + new Vector3(0f, spawnHeightOffset, 0f);

            if (horizontalJitter > 0f)
            {
                popup.Position += new Vector3(
                    UnityEngine.Random.Range(-horizontalJitter, horizontalJitter),
                    0f,
                    UnityEngine.Random.Range(-horizontalJitter, horizontalJitter));
            }

            popup.Velocity = new Vector3(0f, Mathf.Max(0f, riseSpeed), 0f);
            popup.Transform.position = popup.Position;
            popup.Transform.localScale = Vector3.one;

            popup.Text.text = FormatDamageText(message);
            popup.Text.fontSize = Mathf.Max(1, fontSize);
            popup.Text.characterSize = Mathf.Max(0.01f, characterSize);
            popup.Text.color = popup.BaseColor;

            popup.Root.SetActive(true);
            _activePopups.Add(popup);
        }

        private PopupInstance RentPopup()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }

            var root = new GameObject("DamagePopup");
            root.transform.SetParent(transform, false);

            var text = root.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.richText = true;

            return new PopupInstance(root, text);
        }

        private void ReturnToPool(PopupInstance popup)
        {
            if (popup == null)
            {
                return;
            }

            popup.Root.SetActive(false);
            popup.Age = 0f;
            popup.Velocity = Vector3.zero;
            popup.Lifetime = 0f;
            popup.Transform.SetParent(transform, false);
            _pool.Push(popup);
        }

        private bool TryResolveWorldPosition(CombatEventMessage message, out Vector3 position)
        {
            position = Vector3.zero;
            if (message == null || gameClient == null || string.IsNullOrWhiteSpace(message.TargetName))
            {
                return false;
            }

            if (gameClient.PlayerState != null &&
                string.Equals(gameClient.PlayerState.Username, message.TargetName, StringComparison.OrdinalIgnoreCase) &&
                gameClient.PlayerState.Position != null)
            {
                var source = gameClient.PlayerState.Position;
                position = new Vector3((float)source.X, (float)source.Y, (float)source.Z);
                return true;
            }

            if (gameClient.TryGetEntity(message.TargetName, out var entity) && entity.Position != null)
            {
                position = new Vector3((float)entity.Position.X, (float)entity.Position.Y, (float)entity.Position.Z);
                return true;
            }

            return false;
        }

        private string FormatDamageText(CombatEventMessage message)
        {
            if (message == null)
            {
                return "-0";
            }

            var builder = new StringBuilder();
            var amount = Mathf.Max(0f, message.FinalDamage);
            builder.Append('-').Append(amount.ToString("0.#"));

            if (message.IsCritical)
            {
                builder.Append('!');
            }

            if (message.IsBlocked)
            {
                builder.Append(" (BLOCK)");
            }

            return builder.ToString();
        }

        private Color ResolveColor(CombatEventMessage message)
        {
            if (message == null)
            {
                return defaultColor;
            }

            if (message.IsCritical)
            {
                return criticalColor;
            }

            if (message.IsBlocked)
            {
                return blockedColor;
            }

            return defaultColor;
        }

        private sealed class PopupInstance
        {
            public PopupInstance(GameObject root, TextMesh text)
            {
                Root = root;
                Text = text;
                Transform = root.transform;
            }

            public GameObject Root { get; }
            public Transform Transform { get; }
            public TextMesh Text { get; }
            public Vector3 Position;
            public Vector3 Velocity;
            public float Lifetime;
            public float Age;
            public Color BaseColor;
        }
    }
}
