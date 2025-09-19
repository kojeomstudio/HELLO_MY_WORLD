using UnityEngine;
using UnityEngine.UI;
using Minecraft.Core;
using Minecraft.World;
using Minecraft.Player;
using SharedProtocol;
using System.Threading.Tasks;

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
        
        [Header("Game Components")]
        [SerializeField] private MinecraftGameClient gameClient;
        [SerializeField] private ChunkManager chunkManager;
        [SerializeField] private MinecraftPlayerController playerController;
        
        [Header("Game Settings")]
        [SerializeField] private string defaultUsername = "TestPlayer";
        [SerializeField] private string defaultPassword = "password123";
        
        private bool _isLoggedIn = false;
        private bool _isConnected = false;
        
        private void Start()
        {
            InitializeUI();
            InitializeGameSystems();
            SetupEventHandlers();
            
            // Set default values
            if (usernameInput != null) usernameInput.text = defaultUsername;
            if (passwordInput != null) passwordInput.text = defaultPassword;
            
            UpdateUI();
        }
        
        private void InitializeUI()
        {
            if (loginButton != null) loginButton.onClick.AddListener(OnLoginButtonClicked);
            if (connectButton != null) connectButton.onClick.AddListener(OnConnectButtonClicked);
            
            if (loginPanel != null) loginPanel.SetActive(true);
            
            UpdateStatusText("Ready to connect", Color.white);
        }
        
        private void InitializeGameSystems()
        {
            // Find game components if not assigned
            if (gameClient == null) gameClient = FindObjectOfType<MinecraftGameClient>();
            if (chunkManager == null) chunkManager = FindObjectOfType<ChunkManager>();
            if (playerController == null) playerController = FindObjectOfType<MinecraftPlayerController>();
            
            // Disable player controller until logged in
            if (playerController != null) playerController.enabled = false;
        }
        
        private void SetupEventHandlers()
        {
            if (gameClient != null)
            {
                gameClient.ConnectionStatusChanged += OnConnectionStatusChanged;
                gameClient.ErrorOccurred += OnErrorOccurred;
                gameClient.PlayerStateUpdated += OnPlayerStateUpdated;
                gameClient.ChunkLoaded += OnChunkLoaded;
                gameClient.BlockChanged += OnBlockChanged;
                gameClient.ChatMessageReceived += OnChatMessageReceived;
            }
        }
        
        private async void OnConnectButtonClicked()
        {
            if (!_isConnected)
            {
                UpdateStatusText("Connecting to server...", Color.yellow);
                
                if (connectButton != null) connectButton.interactable = false;
                
                bool connected = await gameClient.ConnectAsync();
                
                if (connectButton != null) connectButton.interactable = true;
                
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
            
            string username = usernameInput?.text ?? defaultUsername;
            string password = passwordInput?.text ?? defaultPassword;
            
            if (string.IsNullOrEmpty(username))
            {
                UpdateStatusText("Please enter a username", Color.red);
                return;
            }
            
            UpdateStatusText($"Logging in as {username}...", Color.yellow);
            
            gameClient.SendLogin(username, password);
            
            if (loginButton != null) loginButton.interactable = false;
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
                connectButton.GetComponentInChildren<Text>().text = isConnected ? "Disconnect" : "Connect";
            }
            
            if (!isConnected)
            {
                _isLoggedIn = false;
                UpdateStatusText("Disconnected from server", Color.red);
                
                if (loginPanel != null) loginPanel.SetActive(true);
                if (playerController != null) playerController.enabled = false;
            }
            
            UpdateUI();
        }
        
        private void OnErrorOccurred(string error)
        {
            UpdateStatusText($"Error: {error}", Color.red);
            
            if (loginButton != null) loginButton.interactable = true;
        }
        
        private void OnPlayerStateUpdated(PlayerStateInfo playerState)
        {
            if (!_isLoggedIn)
            {
                _isLoggedIn = true;
                UpdateStatusText($"Logged in as {playerState.PlayerId}", Color.green);
                
                // Hide login panel and enable game
                if (loginPanel != null) loginPanel.SetActive(false);
                if (playerController != null) playerController.enabled = true;
                
                // Position player at spawn
                if (playerState.Position != null && playerController != null)
                {
                    var spawnPos = new Vector3(\n                        (float)playerState.Position.X,\n                        (float)playerState.Position.Y,\n                        (float)playerState.Position.Z\n                    );\n                    playerController.Teleport(spawnPos);\n                }\n                \n                Debug.Log($\"Player logged in: {playerState.PlayerId} at level {playerState.Level}\");\n            }\n        }\n        \n        private void OnChunkLoaded(ChunkSnapshot chunkData)
        {
            if (chunkManager != null)
            {
                chunkManager.LoadChunk(chunkData);
                Debug.Log($"Loaded chunk ({chunkData.ChunkX}, {chunkData.ChunkZ})");
            }
        }\n        \n        private void OnBlockChanged(Vector3Int position, int oldBlockId, int newBlockId)\n        {\n            if (chunkManager != null)\n            {\n                chunkManager.ChangeBlock(position, oldBlockId, newBlockId);\n                Debug.Log($\"Block changed at {position}: {oldBlockId} -> {newBlockId}\");\n            }\n        }\n        \n        private void OnChatMessageReceived(ChatMessage chatMessage)
        {
            var sender = string.IsNullOrEmpty(chatMessage.SenderName) ? chatMessage.SenderId : chatMessage.SenderName;
            Debug.Log($"[Chat] {sender}: {chatMessage.Message}");
        }

        \n        #endregion\n        \n        private void UpdateStatusText(string message, Color color)\n        {\n            if (statusText != null)\n            {\n                statusText.text = message;\n                statusText.color = color;\n            }\n            \n            Debug.Log($\"[GameManager] {message}\");\n        }\n        \n        private void UpdateUI()\n        {\n            if (loginButton != null) loginButton.interactable = _isConnected && !_isLoggedIn;\n            if (connectButton != null) connectButton.interactable = true;\n        }\n        \n        #region Unity Lifecycle\n        \n        private void Update()\n        {\n            // Handle developer shortcuts\n            if (Input.GetKeyDown(KeyCode.F1) && !_isConnected)\n            {\n                OnConnectButtonClicked();\n            }\n            \n            if (Input.GetKeyDown(KeyCode.F2) && _isConnected && !_isLoggedIn)\n            {\n                OnLoginButtonClicked();\n            }\n            \n            // Handle chat input\n            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))\n            {\n                if (_isLoggedIn && Input.inputString.Length > 1)\n                {\n                    gameClient?.SendChatMessage(Input.inputString.Trim());\n                }\n            }\n        }\n        \n        private void OnDestroy()\n        {\n            if (gameClient != null)\n            {\n                gameClient.ConnectionStatusChanged -= OnConnectionStatusChanged;\n                gameClient.ErrorOccurred -= OnErrorOccurred;\n                gameClient.PlayerStateUpdated -= OnPlayerStateUpdated;\n                gameClient.ChunkLoaded -= OnChunkLoaded;\n                gameClient.BlockChanged -= OnBlockChanged;\n                gameClient.ChatMessageReceived -= OnChatMessageReceived;\n            }\n        }\n        \n        #endregion\n    }\n}