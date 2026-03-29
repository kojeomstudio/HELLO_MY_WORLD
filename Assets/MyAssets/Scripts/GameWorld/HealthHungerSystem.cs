using UnityEngine;
using System.Collections;

/// <summary>
/// Health and Hunger system for player survival mechanics
/// Handles health regeneration, hunger depletion, and food effects
/// </summary>
public class HealthHungerSystem : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100.0f;
    public float currentHealth = 100.0f;
    public float healthRegenerationRate = 1.0f; // Health per second when hunger is sufficient
    public float starvationDamageRate = 2.0f; // Damage per second when starving
    
    [Header("Hunger Settings")]
    public float maxHunger = 100.0f;
    public float currentHunger = 100.0f;
    public float hungerDepletionRate = 0.5f; // Hunger per second
    public float healthRegenerationHungerThreshold = 80.0f; // Minimum hunger for health regen
    
    [Header("Effects Settings")]
    public float poisonDamageRate = 1.0f;
    public float regenerationRate = 2.0f;
    public float fireDamageRate = 3.0f;
    
    // Status effects
    private bool isPoisoned = false;
    private bool isRegenerating = false;
    private bool isOnFire = false;
    private float effectDuration = 0.0f;
    
    // Events
    public delegate void HealthHungerUpdateHandler();
    public event HealthHungerUpdateHandler OnHealthChanged;
    public event HealthHungerUpdateHandler OnHungerChanged;
    public event HealthHungerUpdateHandler OnStatusEffectChanged;
    
    void Start()
    {
        // Initialize with full health and hunger
        currentHealth = maxHealth;
        currentHunger = maxHunger;
    }
    
    void Update()
    {
        UpdateHunger();
        UpdateHealth();
        UpdateStatusEffects();
    }
    
    void UpdateHunger()
    {
        if (currentHunger > 0)
        {
            // Deplete hunger over time
            currentHunger -= hungerDepletionRate * Time.deltaTime;
            currentHunger = Mathf.Max(0, currentHunger);
            
            OnHungerChanged?.Invoke();
        }
    }
    
    void UpdateHealth()
    {
        // Health regeneration when hunger is sufficient
        if (currentHunger >= healthRegenerationHungerThreshold && currentHealth < maxHealth)
        {
            currentHealth += healthRegenerationRate * Time.deltaTime;
            currentHealth = Mathf.Min(maxHealth, currentHealth);
            OnHealthChanged?.Invoke();
        }
        
        // Damage from starvation
        if (currentHunger <= 0 && currentHealth > 0)
        {
            currentHealth -= starvationDamageRate * Time.deltaTime;
            currentHealth = Mathf.Max(0, currentHealth);
            OnHealthChanged?.Invoke();
            
            if (currentHealth <= 0)
            {
                HandlePlayerDeath();
            }
        }
    }
    
    void UpdateStatusEffects()
    {
        if (isPoisoned)
        {
            ApplyPoisonDamage();
        }
        
        if (isRegenerating)
        {
            ApplyRegeneration();
        }
        
        if (isOnFire)
        {
            ApplyFireDamage();
        }
        
        // Update effect duration
        if (effectDuration > 0)
        {
            effectDuration -= Time.deltaTime;
            if (effectDuration <= 0)
            {
                ClearAllEffects();
            }
        }
    }
    
    void ApplyPoisonDamage()
    {
        currentHealth -= poisonDamageRate * Time.deltaTime;
        currentHealth = Mathf.Max(0, currentHealth);
        OnHealthChanged?.Invoke();
        
        if (currentHealth <= 0)
        {
            HandlePlayerDeath();
        }
    }
    
    void ApplyRegeneration()
    {
        currentHealth += regenerationRate * Time.deltaTime;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        OnHealthChanged?.Invoke();
    }
    
    void ApplyFireDamage()
    {
        currentHealth -= fireDamageRate * Time.deltaTime;
        currentHealth = Mathf.Max(0, currentHealth);
        OnHealthChanged?.Invoke();
        
        if (currentHealth <= 0)
        {
            HandlePlayerDeath();
        }
    }
    
    void HandlePlayerDeath()
    {
        Debug.Log("Player died!");
        // TODO: Implement death handling (respawn, drop items, etc.)
        
        // Reset status effects
        ClearAllEffects();
        
        // Trigger death event
        // OnPlayerDied?.Invoke();
    }
    
    public void TakeDamage(float damage, DamageType damageType = DamageType.Generic)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        OnHealthChanged?.Invoke();
        
        Debug.Log($"Player took {damage} damage from {damageType}");
        
        if (currentHealth <= 0)
        {
            HandlePlayerDeath();
        }
    }
    
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        OnHealthChanged?.Invoke();
        
        Debug.Log($"Player healed for {amount}");
    }
    
    public void Feed(float nutrition, float saturation = 0.0f)
    {
        currentHunger += nutrition;
        currentHunger = Mathf.Min(maxHunger, currentHunger);
        OnHungerChanged?.Invoke();
        
        Debug.Log($"Player fed with {nutrition} nutrition");
        
        // Apply saturation effects if available
        if (saturation > 0)
        {
            // TODO: Implement saturation system
            Debug.Log($"Player received {saturation} saturation");
        }
    }
    
    public void ApplyStatusEffect(StatusEffect effect, float duration)
    {
        effectDuration = duration;
        
        switch (effect)
        {
            case StatusEffect.Poison:
                isPoisoned = true;
                break;
            case StatusEffect.Regeneration:
                isRegenerating = true;
                break;
            case StatusEffect.Fire:
                isOnFire = true;
                break;
        }
        
        OnStatusEffectChanged?.Invoke();
        Debug.Log($"Applied {effect} effect for {duration} seconds");
    }
    
    public void ClearStatusEffect(StatusEffect effect)
    {
        switch (effect)
        {
            case StatusEffect.Poison:
                isPoisoned = false;
                break;
            case StatusEffect.Regeneration:
                isRegenerating = false;
                break;
            case StatusEffect.Fire:
                isOnFire = false;
                break;
        }
        
        OnStatusEffectChanged?.Invoke();
        Debug.Log($"Cleared {effect} effect");
    }
    
    public void ClearAllEffects()
    {
        isPoisoned = false;
        isRegenerating = false;
        isOnFire = false;
        effectDuration = 0.0f;
        
        OnStatusEffectChanged?.Invoke();
        Debug.Log("Cleared all status effects");
    }
    
    // Getters for UI
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetHungerPercentage() => currentHunger / maxHunger;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetCurrentHunger() => currentHunger;
    public float GetMaxHunger() => maxHunger;
    
    // Status effect getters
    public bool IsPoisoned() => isPoisoned;
    public bool IsRegenerating() => isRegenerating;
    public bool IsOnFire() => isOnFire;
    public float GetEffectDuration() => effectDuration;
    
    // Setters for initialization
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke();
    }
    
    public void SetMaxHunger(float newMaxHunger)
    {
        maxHunger = newMaxHunger;
        currentHunger = Mathf.Min(currentHunger, maxHunger);
        OnHungerChanged?.Invoke();
    }
    
    public void SetCurrentHealth(float newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        OnHealthChanged?.Invoke();
    }
    
    public void SetCurrentHunger(float newHunger)
    {
        currentHunger = Mathf.Clamp(newHunger, 0, maxHunger);
        OnHungerChanged?.Invoke();
    }
    
    // Save/Load functionality
    public string SaveHealthHungerData()
    {
        HealthHungerSaveData saveData = new HealthHungerSaveData
        {
            currentHealth = this.currentHealth,
            maxHealth = this.maxHealth,
            currentHunger = this.currentHunger,
            maxHunger = this.maxHunger,
            isPoisoned = this.isPoisoned,
            isRegenerating = this.isRegenerating,
            isOnFire = this.isOnFire,
            effectDuration = this.effectDuration
        };
        
        return JsonUtility.ToJson(saveData);
    }
    
    public void LoadHealthHungerData(string jsonData)
    {
        try
        {
            HealthHungerSaveData saveData = JsonUtility.FromJson<HealthHungerSaveData>(jsonData);
            this.currentHealth = saveData.currentHealth;
            this.maxHealth = saveData.maxHealth;
            this.currentHunger = saveData.currentHunger;
            this.maxHunger = saveData.maxHunger;
            this.isPoisoned = saveData.isPoisoned;
            this.isRegenerating = saveData.isRegenerating;
            this.isOnFire = saveData.isOnFire;
            this.effectDuration = saveData.effectDuration;
            
            OnHealthChanged?.Invoke();
            OnHungerChanged?.Invoke();
            OnStatusEffectChanged?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load health/hunger data: {e.Message}");
        }
    }
}

// Enums and data structures
public enum DamageType
{
    Generic,
    Fall,
    Fire,
    Drowning,
    Poison,
    Starvation,
    Explosion,
    Melee,
    Ranged
}

public enum StatusEffect
{
    None,
    Poison,
    Regeneration,
    Fire,
    Speed,
    Slowness,
    Haste,
    MiningFatigue,
    Strength,
    Weakness,
    JumpBoost,
    Nausea,
    Blindness,
    NightVision,
    Invisibility
}

[System.Serializable]
public class HealthHungerSaveData
{
    public float currentHealth;
    public float maxHealth;
    public float currentHunger;
    public float maxHunger;
    public bool isPoisoned;
    public bool isRegenerating;
    public bool isOnFire;
    public float effectDuration;
}using System.Collections;

/// <summary>
/// Health and Hunger system for player survival mechanics
/// Handles health regeneration, hunger depletion, and food effects
/// </summary>
public class HealthHungerSystem : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100.0f;
    public float currentHealth = 100.0f;
    public float healthRegenerationRate = 1.0f; // Health per second when hunger is sufficient
    public float starvationDamageRate = 2.0f; // Damage per second when starving
    
    [Header("Hunger Settings")]
    public float maxHunger = 100.0f;
    public float currentHunger = 100.0f;
    public float hungerDepletionRate = 0.5f; // Hunger per second
    public float healthRegenerationHungerThreshold = 80.0f; // Minimum hunger for health regen
    
    [Header("Effects Settings")]
    public float poisonDamageRate = 1.0f;
    public float regenerationRate = 2.0f;
    public float fireDamageRate = 3.0f;
    
    // Status effects
    private bool isPoisoned = false;
    private bool isRegenerating = false;
    private bool isOnFire = false;
    private float effectDuration = 0.0f;
    
    // Events
    public delegate void HealthHungerUpdateHandler();
    public event HealthHungerUpdateHandler OnHealthChanged;
    public event HealthHungerUpdateHandler OnHungerChanged;
    public event HealthHungerUpdateHandler OnStatusEffectChanged;
    
    void Start()
    {
        // Initialize with full health and hunger
        currentHealth = maxHealth;
        currentHunger = maxHunger;
    }
    
    void Update()
    {
        UpdateHunger();
        UpdateHealth();
        UpdateStatusEffects();
    }
    
    void UpdateHunger()
    {
        if (currentHunger > 0)
        {
            // Deplete hunger over time
            currentHunger -= hungerDepletionRate * Time.deltaTime;
            currentHunger = Mathf.Max(0, currentHunger);
            
            OnHungerChanged?.Invoke();
        }
    }
    
    void UpdateHealth()
    {
        // Health regeneration when hunger is sufficient
        if (currentHunger >= healthRegenerationHungerThreshold && currentHealth < maxHealth)
        {
            currentHealth += healthRegenerationRate * Time.deltaTime;
            currentHealth = Mathf.Min(maxHealth, currentHealth);
            OnHealthChanged?.Invoke();
        }
        
        // Damage from starvation
        if (currentHunger <= 0 && currentHealth > 0)
        {
            currentHealth -= starvationDamageRate * Time.deltaTime;
            currentHealth = Mathf.Max(0, currentHealth);
            OnHealthChanged?.Invoke();
            
            if (currentHealth <= 0)
            {
                HandlePlayerDeath();
            }
        }
    }
    
    void UpdateStatusEffects()
    {
        if (isPoisoned)
        {
            ApplyPoisonDamage();
        }
        
        if (isRegenerating)
        {
            ApplyRegeneration();
        }
        
        if (isOnFire)
        {
            ApplyFireDamage();
        }
        
        // Update effect duration
        if (effectDuration > 0)
        {
            effectDuration -= Time.deltaTime;
            if (effectDuration <= 0)
            {
                ClearAllEffects();
            }
        }
    }
    
    void ApplyPoisonDamage()
    {
        currentHealth -= poisonDamageRate * Time.deltaTime;
        currentHealth = Mathf.Max(0, currentHealth);
        OnHealthChanged?.Invoke();
        
        if (currentHealth <= 0)
        {
            HandlePlayerDeath();
        }
    }
    
    void ApplyRegeneration()
    {
        currentHealth += regenerationRate * Time.deltaTime;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        OnHealthChanged?.Invoke();
    }
    
    void ApplyFireDamage()
    {
        currentHealth -= fireDamageRate * Time.deltaTime;
        currentHealth = Mathf.Max(0, currentHealth);
        OnHealthChanged?.Invoke();
        
        if (currentHealth <= 0)
        {
            HandlePlayerDeath();
        }
    }
    
    void HandlePlayerDeath()
    {
        Debug.Log("Player died!");
        // TODO: Implement death handling (respawn, drop items, etc.)
        
        // Reset status effects
        ClearAllEffects();
        
        // Trigger death event
        // OnPlayerDied?.Invoke();
    }
    
    public void TakeDamage(float damage, DamageType damageType = DamageType.Generic)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        OnHealthChanged?.Invoke();
        
        Debug.Log($"Player took {damage} damage from {damageType}");
        
        if (currentHealth <= 0)
        {
            HandlePlayerDeath();
        }
    }
    
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        OnHealthChanged?.Invoke();
        
        Debug.Log($"Player healed for {amount}");
    }
    
    public void Feed(float nutrition, float saturation = 0.0f)
    {
        currentHunger += nutrition;
        currentHunger = Mathf.Min(maxHunger, currentHunger);
        OnHungerChanged?.Invoke();
        
        Debug.Log($"Player fed with {nutrition} nutrition");
        
        // Apply saturation effects if available
        if (saturation > 0)
        {
            // TODO: Implement saturation system
            Debug.Log($"Player received {saturation} saturation");
        }
    }
    
    public void ApplyStatusEffect(StatusEffect effect, float duration)
    {
        effectDuration = duration;
        
        switch (effect)
        {
            case StatusEffect.Poison:
                isPoisoned = true;
                break;
            case StatusEffect.Regeneration:
                isRegenerating = true;
                break;
            case StatusEffect.Fire:
                isOnFire = true;
                break;
        }
        
        OnStatusEffectChanged?.Invoke();
        Debug.Log($"Applied {effect} effect for {duration} seconds");
    }
    
    public void ClearStatusEffect(StatusEffect effect)
    {
        switch (effect)
        {
            case StatusEffect.Poison:
                isPoisoned = false;
                break;
            case StatusEffect.Regeneration:
                isRegenerating = false;
                break;
            case StatusEffect.Fire:
                isOnFire = false;
                break;
        }
        
        OnStatusEffectChanged?.Invoke();
        Debug.Log($"Cleared {effect} effect");
    }
    
    public void ClearAllEffects()
    {
        isPoisoned = false;
        isRegenerating = false;
        isOnFire = false;
        effectDuration = 0.0f;
        
        OnStatusEffectChanged?.Invoke();
        Debug.Log("Cleared all status effects");
    }
    
    // Getters for UI
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetHungerPercentage() => currentHunger / maxHunger;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetCurrentHunger() => currentHunger;
    public float GetMaxHunger() => maxHunger;
    
    // Status effect getters
    public bool IsPoisoned() => isPoisoned;
    public bool IsRegenerating() => isRegenerating;
    public bool IsOnFire() => isOnFire;
    public float GetEffectDuration() => effectDuration;
    
    // Setters for initialization
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke();
    }
    
    public void SetMaxHunger(float newMaxHunger)
    {
        maxHunger = newMaxHunger;
        currentHunger = Mathf.Min(currentHunger, maxHunger);
        OnHungerChanged?.Invoke();
    }
    
    public void SetCurrentHealth(float newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        OnHealthChanged?.Invoke();
    }
    
    public void SetCurrentHunger(float newHunger)
    {
        currentHunger = Mathf.Clamp(newHunger, 0, maxHunger);
        OnHungerChanged?.Invoke();
    }
    
    // Save/Load functionality
    public string SaveHealthHungerData()
    {
        HealthHungerSaveData saveData = new HealthHungerSaveData
        {
            currentHealth = this.currentHealth,
            maxHealth = this.maxHealth,
            currentHunger = this.currentHunger,
            maxHunger = this.maxHunger,
            isPoisoned = this.isPoisoned,
            isRegenerating = this.isRegenerating,
            isOnFire = this.isOnFire,
            effectDuration = this.effectDuration
        };
        
        return JsonUtility.ToJson(saveData);
    }
    
    public void LoadHealthHungerData(string jsonData)
    {
        try
        {
            HealthHungerSaveData saveData = JsonUtility.FromJson<HealthHungerSaveData>(jsonData);
            this.currentHealth = saveData.currentHealth;
            this.maxHealth = saveData.maxHealth;
            this.currentHunger = saveData.currentHunger;
            this.maxHunger = saveData.maxHunger;
            this.isPoisoned = saveData.isPoisoned;
            this.isRegenerating = saveData.isRegenerating;
            this.isOnFire = saveData.isOnFire;
            this.effectDuration = saveData.effectDuration;
            
            OnHealthChanged?.Invoke();
            OnHungerChanged?.Invoke();
            OnStatusEffectChanged?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load health/hunger data: {e.Message}");
        }
    }
}

// Enums and data structures
public enum DamageType
{
    Generic,
    Fall,
    Fire,
    Drowning,
    Poison,
    Starvation,
    Explosion,
    Melee,
    Ranged
}

public enum StatusEffect
{
    None,
    Poison,
    Regeneration,
    Fire,
    Speed,
    Slowness,
    Haste,
    MiningFatigue,
    Strength,
    Weakness,
    JumpBoost,
    Nausea,
    Blindness,
    NightVision,
    Invisibility
}

[System.Serializable]
public class HealthHungerSaveData
{
    public float currentHealth;
    public float maxHealth;
    public float currentHunger;
    public float maxHunger;
    public bool isPoisoned;
    public bool isRegenerating;
    public bool isOnFire;
    public float effectDuration;
}
}
