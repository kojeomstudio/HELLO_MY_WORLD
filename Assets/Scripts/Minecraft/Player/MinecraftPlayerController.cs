using UnityEngine;
using SharedProtocol;
using Minecraft.Core;
using Minecraft.World;

namespace Minecraft.Player
{
    /// <summary>
    /// Minecraft-style player controller
    /// Handles first-person view, block interaction, inventory management, etc.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class MinecraftPlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float sneakSpeed = 2f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundCheckDistance = 0.2f;
        
        [Header("Camera Settings")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float maxLookAngle = 90f;
        
        [Header("Block Interaction")]
        [SerializeField] private float blockReach = 5f;
        [SerializeField] private LayerMask blockLayerMask = 1;
        [SerializeField] private GameObject blockHighlight;
        
        [Header("UI References")]
        [SerializeField] private PlayerUI playerUI;
        
        private CharacterController _characterController;
        private MinecraftGameClient _gameClient;
        private ChunkManager _chunkManager;
        
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _isSprinting;
        private bool _isSneaking;
        private bool _isFlying;
        private GameModeType _gameMode = GameModeType.Survival;
        
        private float _verticalRotation;
        private Vector3 _lastSentPosition;
        private float _lastSentTime;
        
        private Vector3Int _targetBlockPosition;
        private Vector3Int _placeBlockPosition;
        private bool _hasTargetBlock;
        private float _blockBreakProgress;
        private float _blockBreakTime;
        private int _selectedHotbarSlot = 0;
        
        private PlayerStateInfo _playerInfo;
        private ItemInfo[] _hotbar = new ItemInfo[9];
        
        private bool _leftMousePressed;
        private bool _rightMousePressed;
        private bool _leftMouseHeld;
        
        public Vector3Int TargetBlockPosition => _targetBlockPosition;
        public bool HasTargetBlock => _hasTargetBlock;
        public int SelectedHotbarSlot => _selectedHotbarSlot;
        public ItemInfo SelectedItem => _hotbar[_selectedHotbarSlot];
        
        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
            
            Cursor.lockState = CursorLockMode.Locked;
            
            _gameClient = FindObjectOfType<MinecraftGameClient>();
            _chunkManager = FindObjectOfType<ChunkManager>();
        }
        
        private void Start()
        {
            if (_gameClient != null)
            {
                _gameClient.PlayerStateUpdated += OnPlayerStateUpdated;
                _gameClient.BlockChanged += OnBlockChanged;
            }
            
            InitializeHotbar();
        }
        
        private void Update()
        {
            HandleInput();
            UpdateMovement();
            UpdateCamera();
            UpdateBlockInteraction();
            UpdateNetworkSync();
            UpdateUI();
        }
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
            }
            
            _isSprinting = Input.GetKey(KeyCode.LeftShift) && !_isSneaking;
            _isSneaking = Input.GetKey(KeyCode.LeftControl);
            
            if (Input.GetButtonDown("Jump") && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            
            if (_gameMode == GameModeType.Creative && Input.GetKeyDown(KeyCode.F))
            {
                _isFlying = !_isFlying;
            }
            
            for (int i = 1; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    _selectedHotbarSlot = i - 1;
                    break;
                }
            }
            
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.1f)
            {
                _selectedHotbarSlot = (int)Mathf.Repeat(_selectedHotbarSlot - Mathf.Sign(scroll), 9);
            }
            
            _leftMousePressed = Input.GetMouseButtonDown(0);
            _rightMousePressed = Input.GetMouseButtonDown(1);
            _leftMouseHeld = Input.GetMouseButton(0);
        }
        
        private void UpdateMovement()
        {
            _isGrounded = Physics.CheckSphere(transform.position, groundCheckDistance, blockLayerMask);
            
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            Vector3 direction = transform.right * horizontal + transform.forward * vertical;
            direction.Normalize();
            
            float currentSpeed = _isSprinting ? sprintSpeed : (_isSneaking ? sneakSpeed : walkSpeed);
            
            if (_isFlying && _gameMode == GameModeType.Creative)
            {
                if (Input.GetKey(KeyCode.Space))
                    direction.y = 1f;
                else if (Input.GetKey(KeyCode.LeftShift))
                    direction.y = -1f;
                
                _velocity = direction * currentSpeed;
            }
            else
            {
                Vector3 move = direction * currentSpeed;
                
                if (_isGrounded && _velocity.y < 0)
                    _velocity.y = -2f;
                
                _velocity.y += gravity * Time.deltaTime;
                move.y = _velocity.y;
                
                _velocity = move;
            }
            
            _characterController.Move(_velocity * Time.deltaTime);
            
            if (_isSneaking && _isGrounded)
            {
                PreventFalling();
            }
        }
        
        private void PreventFalling()
        {
            Vector3 futurePosition = transform.position + _characterController.velocity * Time.deltaTime;
            
            if (!Physics.CheckSphere(futurePosition + Vector3.down * 1.5f, 0.3f, blockLayerMask))
            {
                Vector3 limitedVelocity = _characterController.velocity;
                limitedVelocity.x = 0;
                limitedVelocity.z = 0;
            }
        }
        
        private void UpdateCamera()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;
            
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            _verticalRotation -= mouseY;
            _verticalRotation = Mathf.Clamp(_verticalRotation, -maxLookAngle, maxLookAngle);
            playerCamera.transform.localRotation = Quaternion.Euler(_verticalRotation, 0, 0);
            
            transform.Rotate(Vector3.up * mouseX);
        }
        
        private void UpdateBlockInteraction()
        {
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            
            if (Physics.Raycast(ray, out RaycastHit hit, blockReach, blockLayerMask))
            {
                Vector3Int blockPos = Vector3Int.FloorToInt(hit.point - hit.normal * 0.5f);
                Vector3Int placePos = Vector3Int.FloorToInt(hit.point + hit.normal * 0.5f);
                
                _targetBlockPosition = blockPos;
                _placeBlockPosition = placePos;
                _hasTargetBlock = true;
                
                if (blockHighlight != null)
                {
                    blockHighlight.SetActive(true);
                    blockHighlight.transform.position = blockPos + Vector3.one * 0.5f;
                }
                
                if (_leftMousePressed)
                {
                    StartBlockBreaking();
                }
                else if (_leftMouseHeld)
                {
                    ContinueBlockBreaking();
                }
                else
                {
                    StopBlockBreaking();
                }
                
                if (_rightMousePressed && SelectedItem != null && SelectedItem.Type == ItemType.Block)
                {
                    PlaceBlock();
                }
            }
            else
            {
                _hasTargetBlock = false;
                _blockBreakProgress = 0;
                
                if (blockHighlight != null)
                    blockHighlight.SetActive(false);
                    
                if (_leftMouseHeld)
                    StopBlockBreaking();
            }
        }
        
        private void StartBlockBreaking()
        {
            if (!_hasTargetBlock) return;
            
            var blockId = _chunkManager?.GetBlockAt(_targetBlockPosition) ?? 0;
            if (blockId == 0) return;
            
            var blockType = _chunkManager?.GetBlockType(blockId);
            if (blockType == null) return;
            
            _blockBreakTime = blockType.Hardness;
            _blockBreakProgress = 0f;
            
            _gameClient?.SendPlayerAction(PlayerActionType.StartDestroyBlock, _targetBlockPosition, 0, Vector3.zero, SelectedItem);
            
            Debug.Log($"Started breaking block at {_targetBlockPosition} (hardness: {blockType.Hardness})");
        }
        
        private void ContinueBlockBreaking()
        {
            if (!_hasTargetBlock || _blockBreakTime <= 0) return;
            
            _blockBreakProgress += Time.deltaTime / _blockBreakTime;
            
            if (_blockBreakProgress >= 1f)
            {
                _gameClient?.SendPlayerAction(PlayerActionType.StopDestroyBlock, _targetBlockPosition, 0, Vector3.zero, SelectedItem);
                _blockBreakProgress = 0f;
                
                Debug.Log($"Finished breaking block at {_targetBlockPosition}");
            }
        }
        
        private void StopBlockBreaking()
        {
            if (_blockBreakProgress > 0 && _blockBreakProgress < 1f)
            {
                _gameClient?.SendPlayerAction(PlayerActionType.AbortDestroyBlock, _targetBlockPosition, 0, Vector3.zero, SelectedItem);
                Debug.Log($"Aborted breaking block at {_targetBlockPosition}");
            }
            
            _blockBreakProgress = 0f;
        }
        
        private void PlaceBlock()
        {
            if (!_hasTargetBlock || SelectedItem == null) return;
            
            Bounds playerBounds = _characterController.bounds;
            Bounds blockBounds = new Bounds(_placeBlockPosition + Vector3.one * 0.5f, Vector3.one);
            
            if (playerBounds.Intersects(blockBounds))
            {
                Debug.Log("Cannot place block: would intersect with player");
                return;
            }
            
            _gameClient?.SendPlayerAction(PlayerActionType.PlaceBlock, _placeBlockPosition, 0, Vector3.zero, SelectedItem);
            
            if (_gameMode == GameModeType.Survival)
            {
                ConsumeSelectedItem(1);
            }
            
            Debug.Log($"Placed block {SelectedItem.ItemName} at {_placeBlockPosition}");
        }
        
        private void ConsumeSelectedItem(int quantity)
        {
            var item = _hotbar[_selectedHotbarSlot];
            if (item == null || item.Quantity < quantity) return;
            
            item.Quantity -= quantity;
            if (item.Quantity <= 0)
            {
                _hotbar[_selectedHotbarSlot] = null;
            }
            
            // TODO: Implement inventory update with new protocol
        }
        
        private void UpdateNetworkSync()
        {
            if (_gameClient == null || !_gameClient.IsConnected) return;
            
            float distance = Vector3.Distance(transform.position, _lastSentPosition);
            float timeSinceLastSent = Time.time - _lastSentTime;
            
            if (distance > 0.1f || timeSinceLastSent > 0.05f)
            {
                Vector3 rotation = new Vector3(_verticalRotation, transform.eulerAngles.y, 0);
                
                _gameClient.SendPlayerStateUpdate(
                    transform.position,
                    rotation,
                    _isGrounded,
                    _isSneaking,
                    _isSprinting,
                    _isFlying
                );
                
                _lastSentPosition = transform.position;
                _lastSentTime = Time.time;
            }
        }
        
        private void UpdateUI()
        {
            if (playerUI != null)
            {
                playerUI.UpdateHotbar(_hotbar, _selectedHotbarSlot);
                playerUI.UpdateBlockBreakProgress(_blockBreakProgress);
                playerUI.UpdateCrosshair(_hasTargetBlock);
                
                if (_playerInfo != null)
                {
                    playerUI.UpdateHealth(_playerInfo.Health, _playerInfo.MaxHealth);
                    playerUI.UpdateHunger(_playerInfo.Hunger, _playerInfo.MaxHunger);
                    playerUI.UpdateExperience(_playerInfo.Level, _playerInfo.Experience);
                }
            }
        }
        
        private void InitializeHotbar()
        {
            _hotbar[0] = new ItemInfo { Id = 1, Name = "Stone", Quantity = 64, Type = ItemType.Block };
            _hotbar[1] = new ItemInfo { Id = 2, Name = "Grass", Quantity = 64, Type = ItemType.Block };
            _hotbar[2] = new ItemInfo { Id = 3, Name = "Dirt", Quantity = 64, Type = ItemType.Block };
        }
        
        private void OnPlayerStateUpdated(PlayerStateInfo playerState)
        {
            _playerInfo = playerState;
            
            if (playerState.Position != null)
            {
                var serverPos = new Vector3(
                    (float)playerState.Position.X,
                    (float)playerState.Position.Y,
                    (float)playerState.Position.Z
                );
                
                if (Vector3.Distance(transform.position, serverPos) > 1f)
                {
                    transform.position = serverPos;
                    Debug.Log($"Position corrected by server: {serverPos}");
                }
            }
            
            _gameMode = playerState.GameMode;
            
            if (playerState.Inventory != null)
            {
                UpdateHotbarFromInventory(playerState.Inventory);
            }
        }
        
        private void OnBlockChanged(Vector3Int position, int oldBlockId, int newBlockId)
        {
            Debug.Log($"Block changed at {position}: {oldBlockId} -> {newBlockId}");
        }
        
        private void UpdateHotbarFromInventory(System.Collections.Generic.IList<ItemInfo> inventory)
        {
            for (int i = 0; i < 9 && i < inventory.Count; i++)
            {
                _hotbar[i] = inventory[i];
            }
        }
        
        public void Teleport(Vector3 position)
        {
            _characterController.enabled = false;
            transform.position = position;
            _characterController.enabled = true;
            
            _velocity = Vector3.zero;
            _lastSentPosition = position;
        }
        
        public void SetGameMode(GameModeType gameMode)
        {
            _gameMode = gameMode;
            
            if (gameMode == GameModeType.Creative)
            {
                _isFlying = true;
            }
            else
            {
                _isFlying = false;
            }
        }
        
        private void OnDestroy()
        {
            if (_gameClient != null)
            {
                _gameClient.PlayerStateUpdated -= OnPlayerStateUpdated;
                _gameClient.BlockChanged -= OnBlockChanged;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (playerCamera != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * blockReach);
            }
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, groundCheckDistance);
            
            if (_hasTargetBlock)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(_targetBlockPosition + Vector3.one * 0.5f, Vector3.one);
                
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(_placeBlockPosition + Vector3.one * 0.5f, Vector3.one);
            }
        }
    }
}