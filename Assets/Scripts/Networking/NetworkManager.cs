using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Networking.Core;
using GameProtocol;

namespace Networking
{
    /// <summary>
    /// Manager class that handles network connection and game features in Unity
    /// Provides features such as login, movement, chat, and block changes integrated with UI.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        [Header("Network Client")]
        [SerializeField] private ProtobufNetworkClient networkClient;
        
        [Header("UI References")]
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private Button loginButton;
        [SerializeField] private Text statusText;
        [SerializeField] private InputField chatInput;
        [SerializeField] private Button chatSendButton;
        [SerializeField] private Text chatDisplay;
        
        [Header("Player Settings")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float moveSpeed = 5f;
        
        private bool isLoggedIn = false;
        private string currentUsername;
        private Vector3 lastSentPosition;
        private float nextPingTime = 0f;
        private const float PING_INTERVAL = 5f; // Send ping every 5 seconds

        private void Start()
        {
            InitializeNetworkClient();
            SetupUI();
            
            // Set default values
            if (usernameInput != null) usernameInput.text = "test";
            if (passwordInput != null) passwordInput.text = "password";
        }

        private void Update()
        {
            // Send periodic pings
            if (networkClient.IsConnected && Time.time >= nextPingTime)
            {
                networkClient.SendPing();
                nextPingTime = Time.time + PING_INTERVAL;
            }
            
            // Detect and send player position changes
            CheckPlayerMovement();
        }

        private void InitializeNetworkClient()
        {
            if (networkClient == null)
            {
                var clientGO = new GameObject("NetworkClient");
                clientGO.transform.SetParent(transform);
                networkClient = clientGO.AddComponent<ProtobufNetworkClient>();
            }
            
            // Register event handlers
            networkClient.ConnectionStatusChanged += OnConnectionStatusChanged;
            networkClient.ConnectionError += OnConnectionError;
            networkClient.LoginResponseReceived += OnLoginResponse;
            networkClient.MoveResponseReceived += OnMoveResponse;
            networkClient.ChatMessageReceived += OnChatMessage;
            networkClient.BlockChangeBroadcastReceived += OnBlockChangeBroadcast;
            networkClient.PingResponseReceived += OnPingResponse;
        }

        private void SetupUI()
        {
            // Connect button events
            if (connectButton != null)
                connectButton.onClick.AddListener(OnConnectButtonClicked);
                
            if (disconnectButton != null)
                disconnectButton.onClick.AddListener(OnDisconnectButtonClicked);
                
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginButtonClicked);
                
            if (chatSendButton != null)
                chatSendButton.onClick.AddListener(OnChatSendButtonClicked);
                
            // Handle enter key in chat input field
            if (chatInput != null)
            {
                chatInput.onEndEdit.AddListener(OnChatInputEndEdit);
            }
            
            UpdateUI();
        }

        private void UpdateUI()
        {
            bool isConnected = networkClient?.IsConnected ?? false;
            
            if (connectButton != null)
                connectButton.interactable = !isConnected;
                
            if (disconnectButton != null)
                disconnectButton.interactable = isConnected;
                
            if (loginButton != null)
                loginButton.interactable = isConnected && !isLoggedIn;
                
            if (chatSendButton != null)
                chatSendButton.interactable = isConnected && isLoggedIn;
                
            if (chatInput != null)
                chatInput.interactable = isConnected && isLoggedIn;
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            }
            Debug.Log($"Status: {message}");
        }

        private void AddChatMessage(string message)
        {
            if (chatDisplay != null)
            {
                chatDisplay.text += $"\n[{System.DateTime.Now:HH:mm:ss}] {message}";
                
                // Scroll chat window to bottom
                var scrollRect = chatDisplay.GetComponentInParent<ScrollRect>();
                if (scrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    scrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }

        private void CheckPlayerMovement()
        {
            if (!isLoggedIn || playerTransform == null) return;
            
            var currentPosition = playerTransform.position;
            var distance = Vector3.Distance(currentPosition, lastSentPosition);
            
            // Only send to server when moved more than 1 unit
            if (distance >= 1.0f)
            {
                networkClient.SendMoveRequest(currentPosition, moveSpeed);
                lastSentPosition = currentPosition;
            }
        }

        // UI event handlers
        private async void OnConnectButtonClicked()
        {
            UpdateStatusText("Connecting to server...");
            
            bool success = await networkClient.ConnectAsync();
            if (success)
            {
                UpdateStatusText("Connected to server");
            }
        }

        private async void OnDisconnectButtonClicked()
        {
            UpdateStatusText("Disconnecting from server...");
            await networkClient.DisconnectAsync();
            
            isLoggedIn = false;
            currentUsername = null;
            UpdateStatusText("Disconnected from server");
        }

        private void OnLoginButtonClicked()
        {
            if (usernameInput == null || passwordInput == null) return;
            
            var username = usernameInput.text.Trim();
            var password = passwordInput.text.Trim();
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                UpdateStatusText("Please enter username and password");
                return;
            }
            
            networkClient.SendLogin(username, password);
            UpdateStatusText($"Logging in as {username}...");
        }

        private void OnChatSendButtonClicked()
        {
            SendChatMessage();
        }

        private void OnChatInputEndEdit(string text)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SendChatMessage();
            }
        }

        private void SendChatMessage()
        {
            if (chatInput == null || string.IsNullOrWhiteSpace(chatInput.text)) return;
            
            var message = chatInput.text.Trim();
            networkClient.SendChatMessage(message, ChatType.Global);
            chatInput.text = "";
            chatInput.ActivateInputField();
        }

        // Network event handlers
        private void OnConnectionStatusChanged(bool isConnected)
        {
            if (!isConnected)
            {
                isLoggedIn = false;
                currentUsername = null;
            }
            
            UpdateUI();
            UpdateStatusText(isConnected ? "Connected" : "Disconnected");
        }

        private void OnConnectionError(string error)
        {
            UpdateStatusText($"Connection error: {error}");
        }

        private void OnLoginResponse(LoginResponse response)
        {
            if (response.Success)
            {
                isLoggedIn = true;
                currentUsername = response.PlayerInfo?.Username ?? usernameInput.text;
                
                if (playerTransform != null && response.PlayerInfo?.Position != null)
                {
                    var pos = response.PlayerInfo.Position;
                    playerTransform.position = new Vector3(pos.X, pos.Y, pos.Z);
                    lastSentPosition = playerTransform.position;
                }
                
                UpdateStatusText($"Successfully logged in as {currentUsername}");
                AddChatMessage($"Welcome, {currentUsername}!");
            }
            else
            {
                isLoggedIn = false;
                UpdateStatusText($"Login failed: {response.Message}");
            }
            
            UpdateUI();
        }

        private void OnMoveResponse(MoveResponse response)
        {
            if (response.Success && response.NewPosition != null && playerTransform != null)
            {
                var pos = response.NewPosition;
                var newPosition = new Vector3(pos.X, pos.Y, pos.Z);
                
                // Move player to server-approved position
                playerTransform.position = newPosition;
                lastSentPosition = newPosition;
                
                Debug.Log($"Player position updated: ({pos.X:F2}, {pos.Y:F2}, {pos.Z:F2})");
            }
        }

        private void OnChatMessage(ChatMessage message)
        {
            var chatType = (ChatType)message.Type;
            var prefix = chatType switch
            {
                ChatType.Global => "[Global]",
                ChatType.Local => "[Local]",
                ChatType.Whisper => "[Whisper]",
                ChatType.System => "[System]",
                _ => "[Unknown]"
            };
            
            AddChatMessage($"{prefix} {message.SenderName}: {message.Message}");
        }

        private void OnBlockChangeBroadcast(WorldBlockChangeBroadcast broadcast)
        {
            if (broadcast.BlockPosition != null)
            {
                var pos = broadcast.BlockPosition;
                Debug.Log($"Block changed by {broadcast.PlayerId}: ({pos.X}, {pos.Y}, {pos.Z}) -> Type {broadcast.BlockType}");
                
                // TODO: Handle actual block changes (integrate with world system)
                AddChatMessage($"Block changed at ({pos.X}, {pos.Y}, {pos.Z}) by {broadcast.PlayerId}");
            }
        }

        private void OnPingResponse(PingResponse response)
        {
            var latency = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - response.ClientTimestamp;
            Debug.Log($"Ping: {latency}ms");
        }

        // Public methods (can be called from other scripts)
        public void SendBlockChange(string areaId, string subworldId, Vector3Int blockPosition, int blockType, int chunkType)
        {
            if (isLoggedIn)
            {
                networkClient.SendBlockChangeRequest(areaId, subworldId, blockPosition, blockType, chunkType);
            }
        }

        public void SendWhisperMessage(string targetPlayer, string message)
        {
            if (isLoggedIn)
            {
                networkClient.SendChatMessage(message, ChatType.Whisper, targetPlayer);
            }
        }

        private void OnDestroy()
        {
            if (networkClient != null)
            {
                networkClient.ConnectionStatusChanged -= OnConnectionStatusChanged;
                networkClient.ConnectionError -= OnConnectionError;
                networkClient.LoginResponseReceived -= OnLoginResponse;
                networkClient.MoveResponseReceived -= OnMoveResponse;
                networkClient.ChatMessageReceived -= OnChatMessage;
                networkClient.BlockChangeBroadcastReceived -= OnBlockChangeBroadcast;
                networkClient.PingResponseReceived -= OnPingResponse;
            }
        }
    }
}