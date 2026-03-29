using UnityEngine;
using System.Collections;

/// <summary>
/// Enhanced Player Controller for Minecraft-like gameplay
/// Handles player movement, block interaction, and inventory management
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4.5f;
    public float runSpeed = 7.0f;
    public float jumpForce = 7.0f;
    public float gravity = 20.0f;
    public float mouseSensitivity = 2.0f;
    
    [Header("Block Interaction")]
    public float blockReachDistance = 5.0f;
    public float blockBreakTime = 1.0f;
    public LayerMask blockLayer;
    
    [Header("Player Stats")]
    public float maxHealth = 100.0f;
    public float maxHunger = 100.0f;
    public float currentHealth = 100.0f;
    public float currentHunger = 100.0f;
    
    private CharacterController characterController;
    private Camera playerCamera;
    private ModifyWorldManager worldManager;
    private GamePlayerManager playerManager;
    
    private Vector3 moveDirection = Vector3.zero;
    private float verticalRotation = 0.0f;
    private bool isGrounded = false;
    private bool isBreakingBlock = false;
    private float currentBlockBreakTime = 0.0f;
    private Vector3 currentBlockPosition;
    private byte currentBlockType;
    
    // Inventory system
    private InventoryManager inventoryManager;
    private int selectedHotbarSlot = 0;
    private byte selectedBlockType = 1; // Default to stone
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        worldManager = FindObjectOfType<ModifyWorldManager>();
        playerManager = GamePlayerManager.Instance;
        inventoryManager = GetComponent<InventoryManager>();
        
        if (playerCamera == null)
        {
            Debug.LogError("PlayerController: No camera found!");
        }
        
        // Lock cursor for first-person control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleBlockInteraction();
        HandleInventoryInput();
        HandleHotbarSelection();
        
        // Update hunger/health system
        UpdateHungerSystem();
    }
    
    void HandleMouseLook()
    {
        if (playerCamera == null) return;
        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Rotate player around y-axis
        transform.Rotate(Vector3.up * mouseX);
        
        // Rotate camera around x-axis (with clamping)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }
    
    void HandleMovement()
    {
        isGrounded = characterController.isGrounded;
        
        if (isGrounded)
        {
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = transform.TransformDirection(moveDirection);
            
            // Check if running
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            moveDirection *= currentSpeed;
            
            // Jump
            if (Input.GetButton("Jump"))
            {
                moveDirection.y = jumpForce;
            }
        }
        else
        {
            // Apply gravity
            moveDirection.y -= gravity * Time.deltaTime;
        }
        
        characterController.Move(moveDirection * Time.deltaTime);
    }
    
    void HandleBlockInteraction()
    {
        if (worldManager == null) return;
        
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        
        // Block breaking (left mouse button)
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out hit, blockReachDistance, blockLayer))
            {
                currentBlockPosition = hit.point;
                currentBlockType = GetBlockTypeAtPosition(hit.point);
                isBreakingBlock = true;
                currentBlockBreakTime = 0.0f;
            }
        }
        
        if (Input.GetMouseButton(0) && isBreakingBlock)
        {
            currentBlockBreakTime += Time.deltaTime;
            
            if (currentBlockBreakTime >= blockBreakTime)
            {
                // Break block
                worldManager.DeleteBlockByInput(ray, currentBlockPosition, currentBlockType);
                isBreakingBlock = false;
                
                // Add block to inventory
                if (inventoryManager != null)
                {
                    inventoryManager.AddItem(currentBlockType, 1);
                }
            }
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            isBreakingBlock = false;
            currentBlockBreakTime = 0.0f;
        }
        
        // Block placing (right mouse button)
        if (Input.GetMouseButtonDown(1))
        {
            if (Physics.Raycast(ray, out hit, blockReachDistance, blockLayer))
            {
                // Place block on face we're looking at
                Vector3 placePosition = hit.point + hit.normal * 0.5f;
                worldManager.AddBlockByInput(ray, placePosition, selectedBlockType);
                
                // Remove block from inventory
                if (inventoryManager != null)
                {
                    inventoryManager.RemoveItem(selectedBlockType, 1);
                }
            }
        }
    }
    
    void HandleInventoryInput()
    {
        // Toggle inventory with 'I' key
        if (Input.GetKeyDown(KeyCode.I))
        {
            // TODO: Show/hide inventory UI
            Debug.Log("Inventory toggle");
        }
        
        // Drop item with 'Q' key
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // TODO: Drop currently selected item
            Debug.Log("Drop item");
        }
    }
    
    void HandleHotbarSelection()
    {
        // Number keys 1-9 for hotbar selection
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                selectedHotbarSlot = i - 1;
                UpdateSelectedBlockType();
            }
        }
        
        // Mouse wheel for hotbar scrolling
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (scroll > 0)
            {
                selectedHotbarSlot = (selectedHotbarSlot + 1) % 9;
            }
            else
            {
                selectedHotbarSlot = (selectedHotbarSlot - 1 + 9) % 9;
            }
            UpdateSelectedBlockType();
        }
    }
    
    void UpdateSelectedBlockType()
    {
        if (inventoryManager != null)
        {
            selectedBlockType = inventoryManager.GetHotbarItem(selectedHotbarSlot);
        }
    }
    
    byte GetBlockTypeAtPosition(Vector3 position)
    {
        // TODO: Get actual block type from world data
        return 1; // Default to stone
    }
    
    void UpdateHungerSystem()
    {
        // Decrease hunger over time
        if (currentHunger > 0)
        {
            currentHunger -= Time.deltaTime * 0.5f; // Adjust rate as needed
            
            // Damage player if hunger is depleted
            if (currentHunger <= 0)
            {
                currentHealth -= Time.deltaTime * 2.0f; // Damage rate when starving
                currentHealth = Mathf.Max(0, currentHealth);
            }
        }
        
        // Regenerate health if hunger is sufficient
        if (currentHunger > 80 && currentHealth < maxHealth)
        {
            currentHealth += Time.deltaTime * 1.0f; // Health regeneration rate
            currentHealth = Mathf.Min(maxHealth, currentHealth);
        }
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        if (currentHealth <= 0)
        {
            // Handle player death
            Debug.Log("Player died!");
            // TODO: Respawn logic
        }
    }
    
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
    }
    
    public void Eat(float nutrition)
    {
        currentHunger += nutrition;
        currentHunger = Mathf.Min(maxHunger, currentHunger);
    }
    
    // Getters for UI
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetHungerPercentage() => currentHunger / maxHunger;
    public int GetSelectedHotbarSlot() => selectedHotbarSlot;
    public byte GetSelectedBlockType() => selectedBlockType;
    
    // Block breaking progress for UI
    public float GetBlockBreakProgress() => isBreakingBlock ? currentBlockBreakTime / blockBreakTime : 0f;
}
using System.Collections;

/// <summary>
/// Enhanced Player Controller for Minecraft-like gameplay
/// Handles player movement, block interaction, and inventory management
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4.5f;
    public float runSpeed = 7.0f;
    public float jumpForce = 7.0f;
    public float gravity = 20.0f;
    public float mouseSensitivity = 2.0f;
    
    [Header("Block Interaction")]
    public float blockReachDistance = 5.0f;
    public float blockBreakTime = 1.0f;
    public LayerMask blockLayer;
    
    [Header("Player Stats")]
    public float maxHealth = 100.0f;
    public float maxHunger = 100.0f;
    public float currentHealth = 100.0f;
    public float currentHunger = 100.0f;
    
    private CharacterController characterController;
    private Camera playerCamera;
    private ModifyWorldManager worldManager;
    private GamePlayerManager playerManager;
    
    private Vector3 moveDirection = Vector3.zero;
    private float verticalRotation = 0.0f;
    private bool isGrounded = false;
    private bool isBreakingBlock = false;
    private float currentBlockBreakTime = 0.0f;
    private Vector3 currentBlockPosition;
    private byte currentBlockType;
    
    // Inventory system
    private InventoryManager inventoryManager;
    private int selectedHotbarSlot = 0;
    private byte selectedBlockType = 1; // Default to stone
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        worldManager = FindObjectOfType<ModifyWorldManager>();
        playerManager = GamePlayerManager.Instance;
        inventoryManager = GetComponent<InventoryManager>();
        
        if (playerCamera == null)
        {
            Debug.LogError("PlayerController: No camera found!");
        }
        
        // Lock cursor for first-person control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleBlockInteraction();
        HandleInventoryInput();
        HandleHotbarSelection();
        
        // Update hunger/health system
        UpdateHungerSystem();
    }
    
    void HandleMouseLook()
    {
        if (playerCamera == null) return;
        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Rotate player around y-axis
        transform.Rotate(Vector3.up * mouseX);
        
        // Rotate camera around x-axis (with clamping)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }
    
    void HandleMovement()
    {
        isGrounded = characterController.isGrounded;
        
        if (isGrounded)
        {
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = transform.TransformDirection(moveDirection);
            
            // Check if running
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            moveDirection *= currentSpeed;
            
            // Jump
            if (Input.GetButton("Jump"))
            {
                moveDirection.y = jumpForce;
            }
        }
        else
        {
            // Apply gravity
            moveDirection.y -= gravity * Time.deltaTime;
        }
        
        characterController.Move(moveDirection * Time.deltaTime);
    }
    
    void HandleBlockInteraction()
    {
        if (worldManager == null) return;
        
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        
        // Block breaking (left mouse button)
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out hit, blockReachDistance, blockLayer))
            {
                currentBlockPosition = hit.point;
                currentBlockType = GetBlockTypeAtPosition(hit.point);
                isBreakingBlock = true;
                currentBlockBreakTime = 0.0f;
            }
        }
        
        if (Input.GetMouseButton(0) && isBreakingBlock)
        {
            currentBlockBreakTime += Time.deltaTime;
            
            if (currentBlockBreakTime >= blockBreakTime)
            {
                // Break block
                worldManager.DeleteBlockByInput(ray, currentBlockPosition, currentBlockType);
                isBreakingBlock = false;
                
                // Add block to inventory
                if (inventoryManager != null)
                {
                    inventoryManager.AddItem(currentBlockType, 1);
                }
            }
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            isBreakingBlock = false;
            currentBlockBreakTime = 0.0f;
        }
        
        // Block placing (right mouse button)
        if (Input.GetMouseButtonDown(1))
        {
            if (Physics.Raycast(ray, out hit, blockReachDistance, blockLayer))
            {
                // Place block on face we're looking at
                Vector3 placePosition = hit.point + hit.normal * 0.5f;
                worldManager.AddBlockByInput(ray, placePosition, selectedBlockType);
                
                // Remove block from inventory
                if (inventoryManager != null)
                {
                    inventoryManager.RemoveItem(selectedBlockType, 1);
                }
            }
        }
    }
    
    void HandleInventoryInput()
    {
        // Toggle inventory with 'I' key
        if (Input.GetKeyDown(KeyCode.I))
        {
            // TODO: Show/hide inventory UI
            Debug.Log("Inventory toggle");
        }
        
        // Drop item with 'Q' key
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // TODO: Drop currently selected item
            Debug.Log("Drop item");
        }
    }
    
    void HandleHotbarSelection()
    {
        // Number keys 1-9 for hotbar selection
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                selectedHotbarSlot = i - 1;
                UpdateSelectedBlockType();
            }
        }
        
        // Mouse wheel for hotbar scrolling
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (scroll > 0)
            {
                selectedHotbarSlot = (selectedHotbarSlot + 1) % 9;
            }
            else
            {
                selectedHotbarSlot = (selectedHotbarSlot - 1 + 9) % 9;
            }
            UpdateSelectedBlockType();
        }
    }
    
    void UpdateSelectedBlockType()
    {
        if (inventoryManager != null)
        {
            selectedBlockType = inventoryManager.GetHotbarItem(selectedHotbarSlot);
        }
    }
    
    byte GetBlockTypeAtPosition(Vector3 position)
    {
        // TODO: Get actual block type from world data
        return 1; // Default to stone
    }
    
    void UpdateHungerSystem()
    {
        // Decrease hunger over time
        if (currentHunger > 0)
        {
            currentHunger -= Time.deltaTime * 0.5f; // Adjust rate as needed
            
            // Damage player if hunger is depleted
            if (currentHunger <= 0)
            {
                currentHealth -= Time.deltaTime * 2.0f; // Damage rate when starving
                currentHealth = Mathf.Max(0, currentHealth);
            }
        }
        
        // Regenerate health if hunger is sufficient
        if (currentHunger > 80 && currentHealth < maxHealth)
        {
            currentHealth += Time.deltaTime * 1.0f; // Health regeneration rate
            currentHealth = Mathf.Min(maxHealth, currentHealth);
        }
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        if (currentHealth <= 0)
        {
            // Handle player death
            Debug.Log("Player died!");
            // TODO: Respawn logic
        }
    }
    
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
    }
    
    public void Eat(float nutrition)
    {
        currentHunger += nutrition;
        currentHunger = Mathf.Min(maxHunger, currentHunger);
    }
    
    // Getters for UI
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetHungerPercentage() => currentHunger / maxHunger;
    public int GetSelectedHotbarSlot() => selectedHotbarSlot;
    public byte GetSelectedBlockType() => selectedBlockType;
    
    // Block breaking progress for UI
    public float GetBlockBreakProgress() => isBreakingBlock ? currentBlockBreakTime / blockBreakTime : 0f;
}
}
/// <summary>
/// Enhanced Player Controller for Minecraft-like gameplay
/// Handles player movement, block interaction, and inventory management
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4.5f;
    public float runSpeed = 7.0f;
    public float jumpForce = 7.0f;
    public float gravity = 20.0f;
    public float mouseSensitivity = 2.0f;
    
    [Header("Block Interaction")]
    public float blockReachDistance = 5.0f;
    public float blockBreakTime = 1.0f;
    public LayerMask blockLayer;
    
    [Header("Player Stats")]
    public float maxHealth = 100.0f;
    public float maxHunger = 100.0f;
    public float currentHealth = 100.0f;
    public float currentHunger = 100.0f;
    
    private CharacterController characterController;
    private Camera playerCamera;
    private ModifyWorldManager worldManager;
    private GamePlayerManager playerManager;
    
    private Vector3 moveDirection = Vector3.zero;
    private float verticalRotation = 0.0f;
    private bool isGrounded = false;
    private bool isBreakingBlock = false;
    private float currentBlockBreakTime = 0.0f;
    private Vector3 currentBlockPosition;
    private byte currentBlockType;
    
    // Inventory system
    private InventoryManager inventoryManager;
    private int selectedHotbarSlot = 0;
    private byte selectedBlockType = 1; // Default to stone
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        worldManager = FindObjectOfType<ModifyWorldManager>();
        playerManager = GamePlayerManager.Instance;
        inventoryManager = GetComponent<InventoryManager>();
        
        if (playerCamera == null)
        {
            Debug.LogError("PlayerController: No camera found!");
        }
        
        // Lock cursor for first-person control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleBlockInteraction();
        HandleInventoryInput();
        HandleHotbarSelection();
        
        // Update hunger/health system
        UpdateHungerSystem();
    }
    
    void HandleMouseLook()
    {
        if (playerCamera == null) return;
        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Rotate player around y-axis
        transform.Rotate(Vector3.up * mouseX);
        
        // Rotate camera around x-axis (with clamping)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }
    
    void HandleMovement()
    {
        isGrounded = characterController.isGrounded;
        
        if (isGrounded)
        {
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = transform.TransformDirection(moveDirection);
            
            // Check if running
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            moveDirection *= currentSpeed;
            
            // Jump
            if (Input.GetButton("Jump"))
            {
                moveDirection.y = jumpForce;
            }
        }
        else
        {
            // Apply gravity
            moveDirection.y -= gravity * Time.deltaTime;
        }
        
        characterController.Move(moveDirection * Time.deltaTime);
    }
    
    void HandleBlockInteraction()
    {
        if (worldManager == null) return;
        
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        
        // Block breaking (left mouse button)
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out hit, blockReachDistance, blockLayer))
            {
                currentBlockPosition = hit.point;
                currentBlockType = GetBlockTypeAtPosition(hit.point);
                isBreakingBlock = true;
                currentBlockBreakTime = 0.0f;
            }
        }
        
        if (Input.GetMouseButton(0) && isBreakingBlock)
        {
            currentBlockBreakTime += Time.deltaTime;
            
            if (currentBlockBreakTime >= blockBreakTime)
            {
                // Break the block
                worldManager.DeleteBlockByInput(ray, currentBlockPosition, currentBlockType);
                isBreakingBlock = false;
                
                // Add block to inventory
                if (inventoryManager != null)
                {
                    inventoryManager.AddItem(currentBlockType, 1);
                }
            }
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            isBreakingBlock = false;
            currentBlockBreakTime = 0.0f;
        }
        
        // Block placing (right mouse button)
        if (Input.GetMouseButtonDown(1))
        {
            if (Physics.Raycast(ray, out hit, blockReachDistance, blockLayer))
            {
                // Place block on the face we're looking at
                Vector3 placePosition = hit.point + hit.normal * 0.5f;
                worldManager.AddBlockByInput(ray, placePosition, selectedBlockType);
                
                // Remove block from inventory
                if (inventoryManager != null)
                {
                    inventoryManager.RemoveItem(selectedBlockType, 1);
                }
            }
        }
    }
    
    void HandleInventoryInput()
    {
        // Toggle inventory with 'I' key
        if (Input.GetKeyDown(KeyCode.I))
        {
            // TODO: Show/hide inventory UI
            Debug.Log("Inventory toggle");
        }
        
        // Drop item with 'Q' key
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // TODO: Drop currently selected item
            Debug.Log("Drop item");
        }
    }
    
    void HandleHotbarSelection()
    {
        // Number keys 1-9 for hotbar selection
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                selectedHotbarSlot = i - 1;
                UpdateSelectedBlockType();
            }
        }
        
        // Mouse wheel for hotbar scrolling
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (scroll > 0)
            {
                selectedHotbarSlot = (selectedHotbarSlot + 1) % 9;
            }
            else
            {
                selectedHotbarSlot = (selectedHotbarSlot - 1 + 9) % 9;
            }
            UpdateSelectedBlockType();
        }
    }
    
    void UpdateSelectedBlockType()
    {
        if (inventoryManager != null)
        {
            selectedBlockType = inventoryManager.GetHotbarItem(selectedHotbarSlot);
        }
    }
    
    byte GetBlockTypeAtPosition(Vector3 position)
    {
        // TODO: Get actual block type from world data
        return 1; // Default to stone
    }
    
    void UpdateHungerSystem()
    {
        // Decrease hunger over time
        if (currentHunger > 0)
        {
            currentHunger -= Time.deltaTime * 0.5f; // Adjust rate as needed
            
            // Damage player if hunger is depleted
            if (currentHunger <= 0)
            {
                currentHealth -= Time.deltaTime * 2.0f; // Damage rate when starving
                currentHealth = Mathf.Max(0, currentHealth);
            }
        }
        
        // Regenerate health if hunger is sufficient
        if (currentHunger > 80 && currentHealth < maxHealth)
        {
            currentHealth += Time.deltaTime * 1.0f; // Health regeneration rate
            currentHealth = Mathf.Min(maxHealth, currentHealth);
        }
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        if (currentHealth <= 0)
        {
            // Handle player death
            Debug.Log("Player died!");
            // TODO: Respawn logic
        }
    }
    
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
    }
    
    public void Eat(float nutrition)
    {
        currentHunger += nutrition;
        currentHunger = Mathf.Min(maxHunger, currentHunger);
    }
    
    // Getters for UI
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetHungerPercentage() => currentHunger / maxHunger;
    public int GetSelectedHotbarSlot() => selectedHotbarSlot;
    public byte GetSelectedBlockType() => selectedBlockType;
    
    // Block breaking progress for UI
    public float GetBlockBreakProgress() => isBreakingBlock ? currentBlockBreakTime / blockBreakTime : 0f;
}
}
