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
    /// Displays a rolling feed of death and respawn notifications sourced from the MinecraftGameClient.
    /// </summary>
    public class DeathFeedUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MinecraftGameClient gameClient;
        [SerializeField] private Text feedText;

        [Header("Display Settings")]
        [SerializeField] private int maxEntries = 6;
        [SerializeField] private float messageLifetimeSeconds = 12f;

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
        }

        private void OnEnable()
        {
            if (gameClient == null)
            {
                return;
            }

            gameClient.PlayerDeathNotified += HandlePlayerDeath;
            gameClient.PlayerRespawned += HandlePlayerRespawn;
        }

        private void OnDisable()
        {
            if (gameClient != null)
            {
                gameClient.PlayerDeathNotified -= HandlePlayerDeath;
                gameClient.PlayerRespawned -= HandlePlayerRespawn;
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
                RefreshFeedText();
            }
        }

        private void HandlePlayerDeath(PlayerDeathMessage message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.PlayerName))
            {
                return;
            }

            var template = string.IsNullOrWhiteSpace(message.DeathMessage)
                ? $"{message.PlayerName} died."
                : $"{message.PlayerName} {message.DeathMessage}";

            AddEntry(template);
        }

        private void HandlePlayerRespawn(PlayerRespawnBroadcast message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.PlayerName))
            {
                return;
            }

            AddEntry($"{message.PlayerName} respawned.");
        }

        private void AddEntry(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var lifetime = Mathf.Max(1f, messageLifetimeSeconds);
            _entries.Add(new FeedEntry(text.Trim(), Time.unscaledTime + lifetime));

            if (maxEntries > 0 && _entries.Count > maxEntries)
            {
                var overflow = _entries.Count - maxEntries;
                _entries.RemoveRange(0, overflow);
            }

            RefreshFeedText();
        }

        private void RefreshFeedText()
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
