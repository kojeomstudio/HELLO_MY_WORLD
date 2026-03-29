using System;
using System.Collections.Generic;
using System.Text;
using Minecraft.Core;
using SharedProtocol;
using UnityEngine;
using UnityEngine.UI;

namespace Minecraft.UI
{
    /// <summary>
    /// Displays recent combat events as a lightweight textual feed with color-coded damage hints.
    /// </summary>
    public class CombatFeedbackUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MinecraftGameClient gameClient;
        [SerializeField] private Text feedText;

        [Header("Display Settings")]
        [SerializeField] private int maxEntries = 5;
        [SerializeField] private float entryLifetimeSeconds = 3.5f;
        [SerializeField] private Color defaultColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.35f, 0.35f);
        [SerializeField] private Color blockedColor = new Color(0.65f, 0.85f, 1f);

        private readonly List<FeedEntry> _entries = new();

        private void Awake()
        {
            if (gameClient == null)
            {
                gameClient = FindObjectOfType<MinecraftGameClient>();
            }

            if (feedText == null)
            {
                feedText = GetComponent<Text>();
            }

            if (feedText != null)
            {
                feedText.supportRichText = true;
            }
        }

        private void OnEnable()
        {
            if (gameClient == null)
            {
                return;
            }

            gameClient.CombatEventReceived += HandleCombatEvent;
        }

        private void OnDisable()
        {
            if (gameClient != null)
            {
                gameClient.CombatEventReceived -= HandleCombatEvent;
            }
        }

        private void Update()
        {
            if (_entries.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            var changed = false;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].ExpiresAt <= now)
                {
                    _entries.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                RefreshFeed();
            }
        }

        private void HandleCombatEvent(CombatEventMessage message)
        {
            if (message == null)
            {
                return;
            }

            var attacker = !string.IsNullOrWhiteSpace(message.AttackerName) ? message.AttackerName : "Environment";
            var target = !string.IsNullOrWhiteSpace(message.TargetName) ? message.TargetName : "Unknown";
            var builder = new StringBuilder();
            builder.Append(attacker).Append(" -> ").Append(target).Append(": ");
            builder.Append($"-{message.FinalDamage:0.#} HP");

            if (message.IsCritical)
            {
                builder.Append(" (CRIT)");
            }

            if (message.IsBlocked)
            {
                builder.Append(" (BLOCKED)");
            }

            builder.Append($" | {Mathf.Max(0f, message.TargetRemainingHealth):0.#} HP left");

            if (!string.IsNullOrWhiteSpace(message.WeaponName))
            {
                builder.Append($" [{message.WeaponName}]");
            }

            AddEntry(builder.ToString(), ResolveColor(message));
        }

        private void AddEntry(string text, Color color)
        {
            if (feedText == null || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var lifetime = Mathf.Max(1f, entryLifetimeSeconds);
            _entries.Add(new FeedEntry(ToRichText(text.Trim(), color), Time.unscaledTime + lifetime));

            if (maxEntries > 0 && _entries.Count > maxEntries)
            {
                var overflow = _entries.Count - maxEntries;
                _entries.RemoveRange(0, overflow);
            }

            RefreshFeed();
        }

        private void RefreshFeed()
        {
            if (feedText == null)
            {
                return;
            }

            if (_entries.Count == 0)
            {
                feedText.text = string.Empty;
                return;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < _entries.Count; i++)
            {
                if (i > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(_entries[i].Text);
            }

            feedText.text = builder.ToString();
        }

        private string ToRichText(string text, Color color)
        {
            var hex = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hex}>{text}</color>";
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

        [Serializable]
        private struct FeedEntry
        {
            public FeedEntry(string text, float expiresAt)
            {
                Text = text;
                ExpiresAt = expiresAt;
            }

            public string Text { get; }
            public float ExpiresAt { get; }
        }
    }
}
