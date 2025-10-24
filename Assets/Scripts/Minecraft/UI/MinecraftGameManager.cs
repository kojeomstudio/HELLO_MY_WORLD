using System;
using UnityEngine;
using UnityEngine.UI;
using Minecraft.Core;
using Minecraft.Containers;
using Minecraft.World;
using Minecraft.Player;
using SharedProtocol;
using System.Threading.Tasks;
using Minecraft.Crafting;
using Minecraft.Multiplayer;

namespace Minecraft.UI
{
    /// <summary>
    /// Main game manager that coordinates all Minecraft systems
    /// Handles login, connection, and game state management
    /// </summary>
    public class MinecraftGameManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button connectButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Text connectionStatusText;
        [SerializeField] private Text serverStatusText;
        [SerializeField] private Text timeOfDayText;
        [SerializeField] private Text weatherStatusText;
        [SerializeField] private Button refreshStatusButton;

        [Header("Game Components")]
        [SerializeField] private MinecraftGameClient gameClient;
        [SerializeField] private ChunkManager chunkManager;
        [SerializeField] private MinecraftPlayerController playerController;

        [Header("Game Settings")]
        [SerializeField] private string defaultUsername = "TestPlayer";
        [SerializeField] private string defaultPassword = "password123";

        private bool _isLoggedIn;
        private bool _isConnected;
        private TimeUpdateMessage _lastTimeUpdate;
        private WeatherChangeMessage _lastWeatherUpdate;

        private void Start()
        {
            InitializeUI();
            InitializeGameSystems();
            SetupEventHandlers();

            if (usernameInput != null)
            {
                usernameInput.text = defaultUsername;
            }

            if (passwordInput != null)
            {
                passwordInput.text = defaultPassword;
            }

            UpdateUI();
        }

        private void InitializeUI()
        {
            if (loginButton != null)
            {
                loginButton.onClick.AddListener(OnLoginButtonClicked);
            }

            if (connectButton != null)
            {
                connectButton.onClick.AddListener(OnConnectButtonClicked);
            }

            if (refreshStatusButton != null)
            {
                refreshStatusButton.onClick.AddListener(OnRefreshStatusClicked);
            }

            if (loginPanel != null)
            {
                loginPanel.SetActive(true);
            }

            UpdateStatusText("Ready to connect", Color.white);
            UpdateServerStatusText(null);
            RefreshTimeWeatherUI();
        }

        private void InitializeGameSystems()
        {
            if (gameClient == null)
            {
                gameClient = FindObjectOfType<MinecraftGameClient>();
            }

            if (chunkManager == null)
            {
                chunkManager = FindObjectOfType<ChunkManager>();
            }

            if (playerController == null)
            {
                playerController = FindObjectOfType<MinecraftPlayerController>();
            }

            if (FindObjectOfType<CraftingManager>() == null)
            {
                gameObject.AddComponent<CraftingManager>();
            }

            if (FindObjectOfType<RoomBrowserManager>() == null)
            {
                gameObject.AddComponent<RoomBrowserManager>();
            }

            if (FindObjectOfType<ContainerManager>() == null)
            {
                gameObject.AddComponent<ContainerManager>();
            }

            if (playerController != null)
            {
                playerController.enabled = false;
            }
        }

        private void SetupEventHandlers()
        {
            if (gameClient == null)
            {
                return;
            }

            gameClient.ConnectionStatusChanged += OnConnectionStatusChanged;
            gameClient.ErrorOccurred += OnErrorOccurred;
            gameClient.PlayerStateUpdated += OnPlayerStateUpdated;
            gameClient.ChunkLoaded += OnChunkLoaded;
            gameClient.BlockChanged += OnBlockChanged;
            gameClient.ChatMessageReceived += OnChatMessageReceived;
            gameClient.ServerStatusReceived += OnServerStatusReceived;
            gameClient.TimeUpdated += OnTimeUpdated;
            gameClient.WeatherChanged += OnWeatherChanged;

            if (gameClient.TryGetLastTimeSnapshot(out var timeSnapshot) && timeSnapshot != null)
            {
                _lastTimeUpdate = timeSnapshot;
            }

            if (gameClient.TryGetLastWeatherSnapshot(out var weatherSnapshot) && weatherSnapshot != null)
            {
                _lastWeatherUpdate = weatherSnapshot;
            }

            RefreshTimeWeatherUI();
        }

        private async void OnConnectButtonClicked()
        {
            if (!_isConnected)
            {
                UpdateStatusText("Connecting to server...", Color.yellow);

                if (connectButton != null)
                {
                    connectButton.interactable = false;
                }

                bool connected = await gameClient.ConnectAsync();

                if (connectButton != null)
                {
                    connectButton.interactable = true;
                }

                if (connected)
                {
                    UpdateStatusText("Connected! Ready to login.", Color.green);
                }
                else
                {
                    UpdateStatusText("Failed to connect to server", Color.red);
                }
            }
            else
            {
                await gameClient.DisconnectAsync();
            }
        }

        private void OnLoginButtonClicked()
        {
            if (!_isConnected)
            {
                UpdateStatusText("Please connect to server first", Color.red);
                return;
            }

            if (_isLoggedIn)
            {
                UpdateStatusText("Already logged in", Color.yellow);
                return;
            }

            string username = usernameInput != null ? usernameInput.text : defaultUsername;
            string password = passwordInput != null ? passwordInput.text : defaultPassword;

            if (string.IsNullOrWhiteSpace(username))
            {
                UpdateStatusText("Please enter a username", Color.red);
                return;
            }

            UpdateStatusText($"Logging in as {username}...", Color.yellow);
            gameClient.SendLogin(username, password);

            if (loginButton != null)
            {
                loginButton.interactable = false;
            }
        }

        #region Event Handlers

        private void OnConnectionStatusChanged(bool isConnected)
        {
            _isConnected = isConnected;

            if (connectionStatusText != null)
            {
                connectionStatusText.text = isConnected ? "Connected" : "Disconnected";
                connectionStatusText.color = isConnected ? Color.green : Color.red;
            }

            if (connectButton != null)
            {
                var label = connectButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = isConnected ? "Disconnect" : "Connect";
                }
            }

            if (!isConnected)
            {
                _isLoggedIn = false;
                UpdateStatusText("Disconnected from server", Color.red);
                UpdateServerStatusText(null);
                _lastTimeUpdate = null;
                _lastWeatherUpdate = null;
                RefreshTimeWeatherUI();

                if (loginPanel != null)
                {
                    loginPanel.SetActive(true);
                }

                if (playerController != null)
                {
                    playerController.enabled = false;
                }
            }

            UpdateUI();
        }

        private void OnErrorOccurred(string error)
        {
            UpdateStatusText($"Error: {error}", Color.red);

            if (loginButton != null)
            {
                loginButton.interactable = true;
            }
        }

        private void OnPlayerStateUpdated(PlayerStateInfo playerState)
        {
            if (!_isLoggedIn)
            {
                _isLoggedIn = true;
                UpdateStatusText($"Logged in as {playerState.PlayerId}", Color.green);

                if (loginPanel != null)
                {
                    loginPanel.SetActive(false);
                }

                if (playerController != null)
                {
                    playerController.enabled = true;
                }

                if (playerState.Position != null && playerController != null)
                {
                    var spawnPos = new Vector3(
                        (float)playerState.Position.X,
                        (float)playerState.Position.Y,
                        (float)playerState.Position.Z
                    );

                    playerController.Teleport(spawnPos);
                }

                Debug.Log($"Player logged in: {playerState.PlayerId} at level {playerState.Level}");
            }

            UpdateUI();
        }

        private void OnChunkLoaded(ChunkSnapshot chunkData)
        {
            if (chunkManager == null)
            {
                return;
            }

            chunkManager.LoadChunk(chunkData);
            Debug.Log($"Loaded chunk ({chunkData.ChunkX}, {chunkData.ChunkZ})");
        }

        private void OnBlockChanged(Vector3Int position, int oldBlockId, int newBlockId)
        {
            if (chunkManager == null)
            {
                return;
            }

            chunkManager.ChangeBlock(position, oldBlockId, newBlockId);
            Debug.Log($"Block changed at {position}: {oldBlockId} -> {newBlockId}");
        }

        private void OnChatMessageReceived(ChatMessage chatMessage)
        {
            var sender = string.IsNullOrEmpty(chatMessage.SenderName) ? chatMessage.SenderId : chatMessage.SenderName;
            Debug.Log($"[Chat] {sender}: {chatMessage.Message}");
        }

        private void OnTimeUpdated(TimeUpdateMessage message)
        {
            _lastTimeUpdate = message;
            RefreshTimeWeatherUI();
        }

        private void OnWeatherChanged(WeatherChangeMessage message)
        {
            _lastWeatherUpdate = message;
            RefreshTimeWeatherUI();
        }

        private void OnServerStatusReceived(ServerStatusResponse status)
        {
            UpdateServerStatusText(status);
        }

        #endregion

        private void UpdateStatusText(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }

            Debug.Log($"[GameManager] {message}");
        }

        private void UpdateServerStatusText(ServerStatusResponse status)
        {
            if (serverStatusText == null)
            {
                return;
            }

            if (status == null)
            {
                serverStatusText.text = "Server status: --";
                return;
            }

            var uptime = TimeSpan.FromMilliseconds(Math.Max(0, status.ServerUptime));
            string residencyPart;
            if (status.ActiveChunkResidencyPlayers > 0)
            {
                residencyPart =
                    $"Residency: {status.TotalTrackedChunks} chunks/{status.ActiveChunkResidencyPlayers} players";
                if (!string.IsNullOrEmpty(status.BusiestChunkPlayer) && status.PeakChunksPerPlayer > 0)
                {
                    residencyPart +=
                        $" (top {status.BusiestChunkPlayer}: {status.PeakChunksPerPlayer})";
                }
            }
            else
            {
                residencyPart = "Residency: --";
            }

            serverStatusText.text =
                $"Server v{status.ServerVersion} | Players: {status.OnlinePlayers} | {residencyPart} | Hash mismatches: {status.ContainerHashMismatches} | Uptime: {FormatUptime(uptime)}";
        }

        private void RefreshTimeWeatherUI()
        {
            UpdateTimeText();
            UpdateWeatherText();
        }

        private void UpdateTimeText()
        {
            if (timeOfDayText == null)
            {
                return;
            }

            if (_lastTimeUpdate == null)
            {
                timeOfDayText.text = "Time: --:--";
                return;
            }

            var formatted = FormatDayTime(_lastTimeUpdate.WorldTime, _lastTimeUpdate.DayTime);
            timeOfDayText.text = $"Time: {formatted}";
        }

        private void UpdateWeatherText()
        {
            if (weatherStatusText == null)
            {
                return;
            }

            if (_lastWeatherUpdate == null)
            {
                weatherStatusText.text = "Weather: --";
                return;
            }

            weatherStatusText.text = $"Weather: {FormatWeather(_lastWeatherUpdate)}";
        }

        private static string FormatDayTime(long worldTime, long dayTime)
        {
            var dayIndex = worldTime >= 0 ? worldTime / 24000 : 0;
            var totalHours = ((dayTime / 1000.0) + 6.0) % 24.0;
            if (totalHours < 0)
            {
                totalHours += 24.0;
            }

            var hour = (int)Math.Floor(totalHours);
            var minute = (int)Math.Round((totalHours - hour) * 60.0);
            if (minute >= 60)
            {
                minute -= 60;
                hour = (hour + 1) % 24;
            }

            return $"Day {dayIndex + 1} {hour:D2}:{minute:D2}";
        }

        private static string FormatWeather(WeatherChangeMessage weather)
        {
            if (weather == null)
            {
                return "Clear";
            }

            string label = weather.WeatherType switch
            {
                WeatherType.Clear => "Clear",
                WeatherType.Rain => "Rain",
                WeatherType.Thunderstorm => "Storm",
                WeatherType.Snow => "Snow",
                _ => weather.WeatherType.ToString()
            };

            if (weather.WeatherType == WeatherType.Clear)
            {
                return label;
            }

            int intensityPercent = Mathf.RoundToInt(Mathf.Clamp01(weather.Intensity) * 100f);
            string durationPart = weather.Duration > 0 ? $" - {weather.Duration}s" : string.Empty;

            return $"{label} {intensityPercent}%{durationPart}";
        }

        private string FormatUptime(TimeSpan uptime)
        {
            if (uptime.TotalDays >= 1)
            {
                return $"{(int)uptime.TotalDays}d {uptime.Hours}h";
            }

            if (uptime.TotalHours >= 1)
            {
                return $"{(int)uptime.TotalHours}h {uptime.Minutes}m";
            }

            if (uptime.TotalMinutes >= 1)
            {
                return $"{uptime.Minutes}m {uptime.Seconds}s";
            }

            var seconds = Math.Max(0, (int)uptime.TotalSeconds);
            return $"{seconds}s";
        }

        private void OnRefreshStatusClicked()
        {
            if (gameClient == null)
            {
                return;
            }

            if (!gameClient.IsConnected || string.IsNullOrEmpty(gameClient.SessionToken))
            {
                UpdateStatusText("Connect and log in to refresh server status", Color.yellow);
                UpdateServerStatusText(null);
                return;
            }

            gameClient.RequestServerStatus();
        }

        private void UpdateUI()
        {
            if (loginButton != null)
            {
                loginButton.interactable = _isConnected && !_isLoggedIn;
            }

            if (connectButton != null)
            {
                connectButton.interactable = true;
            }

            if (refreshStatusButton != null)
            {
                bool canRefresh = _isConnected && gameClient != null && !string.IsNullOrEmpty(gameClient.SessionToken);
                refreshStatusButton.interactable = canRefresh;
            }
        }

        #region Unity Lifecycle

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && !_isConnected)
            {
                OnConnectButtonClicked();
            }

            if (Input.GetKeyDown(KeyCode.F2) && _isConnected && !_isLoggedIn)
            {
                OnLoginButtonClicked();
            }

            if (_isLoggedIn && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                if (!string.IsNullOrWhiteSpace(Input.inputString))
                {
                    var message = Input.inputString.Trim();
                    if (!string.IsNullOrEmpty(message))
                    {
                        gameClient?.SendChatMessage(message);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (gameClient == null)
            {
                return;
            }

            gameClient.ConnectionStatusChanged -= OnConnectionStatusChanged;
            gameClient.ErrorOccurred -= OnErrorOccurred;
            gameClient.PlayerStateUpdated -= OnPlayerStateUpdated;
            gameClient.ChunkLoaded -= OnChunkLoaded;
            gameClient.BlockChanged -= OnBlockChanged;
            gameClient.ChatMessageReceived -= OnChatMessageReceived;
            gameClient.ServerStatusReceived -= OnServerStatusReceived;
            gameClient.TimeUpdated -= OnTimeUpdated;
            gameClient.WeatherChanged -= OnWeatherChanged;
        }

        #endregion
    }
}
