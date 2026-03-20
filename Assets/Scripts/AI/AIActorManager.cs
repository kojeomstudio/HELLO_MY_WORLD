using System.Collections.Generic;
using UnityEngine;
using GameProtocol;

/// <summary>
/// Unity Client-side AI Actor Manager
///
/// Manages visual representation of server-authoritative AI actors.
/// Receives AI state updates from GameServer and renders them in Unity.
///
/// Key Responsibilities:
/// - Create/destroy AI actor GameObjects based on server messages
/// - Update AI positions with interpolation for smooth movement
/// - Trigger animations based on AI state
/// - Display health bars
/// - Handle AI death effects
///
/// Note: This is CLIENT-SIDE ONLY - no AI logic, only rendering!
/// All AI logic runs on GameServer via ServerAIManager.
/// </summary>
public class AIActorManager : MonoBehaviour
{
    [Header("AI Actor Settings")]
    [Tooltip("Prefabs for different AI types")]
    public GameObject AggressiveAIPrefab;
    public GameObject DefensiveAIPrefab;
    public GameObject CowardAIPrefab;
    public GameObject BossAIPrefab;
    public GameObject FlyingAIPrefab;
    public GameObject RangedAIPrefab;

    [Header("Performance Settings")]
    [Tooltip("Position interpolation speed (higher = snappier)")]
    [Range(1f, 20f)]
    public float InterpolationSpeed = 10f;

    [Tooltip("Maximum distance to render AI actors")]
    public float MaxRenderDistance = 100f;

    [Header("Debug")]
    public bool ShowDebugLogs = false;
    public bool ShowDebugGizmos = false;

    // Dictionary of active AI actors (ActorId -> ActorInstance)
    private Dictionary<int, AIActorInstance> _activeActors = new Dictionary<int, AIActorInstance>();

    // Player reference for distance culling
    private Transform _playerTransform;

    // Network client reference
    private Networking.Core.ProtobufNetworkClient _networkClient;

    /// <summary>
    /// AI Actor instance data (client-side representation)
    /// </summary>
    private class AIActorInstance
    {
        public int ActorId;
        public GameObject GameObject;
        public ActorController ActorController;
        public ActorAnimationController AnimationController;
        public UnityEngine.Vector3 TargetPosition;
        public AIState CurrentState;
        public int Health;
        public int MaxHealth;
    }

    void Awake()
    {
        // Find player transform
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }

        // Find network client and subscribe to AI events
        _networkClient = FindObjectOfType<Networking.Core.ProtobufNetworkClient>();
        if (_networkClient != null)
        {
            _networkClient.AIStateSyncReceived += OnAIStateSyncReceived;
            _networkClient.AIAttackEventReceived += OnAIAttackEventReceived;
            _networkClient.AIDeathEventReceived += OnAIDeathEventReceived;
            _networkClient.AISpawnResponseReceived += OnAISpawnResponseReceived;

            if (ShowDebugLogs)
            {
                Debug.Log("[AIActorManager] Subscribed to ProtobufNetworkClient AI events");
            }
        }
        else
        {
            Debug.LogWarning("[AIActorManager] ProtobufNetworkClient not found! AI actors will not be rendered.");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (_networkClient != null)
        {
            _networkClient.AIStateSyncReceived -= OnAIStateSyncReceived;
            _networkClient.AIAttackEventReceived -= OnAIAttackEventReceived;
            _networkClient.AIDeathEventReceived -= OnAIDeathEventReceived;
            _networkClient.AISpawnResponseReceived -= OnAISpawnResponseReceived;
        }
    }

    void Update()
    {
        // Update all AI actor positions with interpolation
        foreach (var kvp in _activeActors)
        {
            var actor = kvp.Value;
            if (actor.GameObject != null)
            {
                // Interpolate toward server position
                actor.GameObject.transform.position = Vector3.Lerp(
                    actor.GameObject.transform.position,
                    actor.TargetPosition,
                    Time.deltaTime * InterpolationSpeed
                );

                // Distance culling
                if (_playerTransform != null)
                {
                    float distance = Vector3.Distance(actor.GameObject.transform.position, _playerTransform.position);
                    bool shouldRender = distance <= MaxRenderDistance;

                    if (actor.GameObject.activeSelf != shouldRender)
                    {
                        actor.GameObject.SetActive(shouldRender);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Called when AI state sync broadcast is received from server (10Hz)
    /// </summary>
    public void OnAIStateSyncReceived(AIStateSyncBroadcast broadcast)
    {
        if (broadcast == null || broadcast.Actors == null)
            return;

        foreach (var actorInfo in broadcast.Actors)
        {
            if (_activeActors.ContainsKey(actorInfo.ActorId))
            {
                // Update existing actor
                UpdateAIActor(actorInfo);
            }
            else
            {
                // Create new actor
                CreateAIActor(actorInfo);
            }
        }

        // Remove actors that are no longer in the broadcast
        var receivedIds = new HashSet<int>();
        foreach (var actorInfo in broadcast.Actors)
        {
            receivedIds.Add(actorInfo.ActorId);
        }

        var toRemove = new List<int>();
        foreach (var kvp in _activeActors)
        {
            if (!receivedIds.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var id in toRemove)
        {
            RemoveAIActor(id);
        }
    }

    /// <summary>
    /// Called when AI attack event is received
    /// </summary>
    public void OnAIAttackEventReceived(AIAttackEventBroadcast attackEvent)
    {
        if (attackEvent == null)
            return;

        // Find attacker and play attack animation
        if (_activeActors.TryGetValue(attackEvent.AttackerId, out var attacker))
        {
            if (attacker.AnimationController != null)
            {
                attacker.AnimationController.PlayAnimation(ActorAnimationType.Attack);
            }
        }

        // Show damage effect on target
        // TODO: Implement damage visual effects

        if (ShowDebugLogs)
        {
            Debug.Log($"[AIActorManager] AI {attackEvent.AttackerId} attacked {attackEvent.TargetId} for {attackEvent.Damage} damage");
        }
    }

    /// <summary>
    /// Called when AI death event is received
    /// </summary>
    public void OnAIDeathEventReceived(AIDeathEventBroadcast deathEvent)
    {
        if (deathEvent == null)
            return;

        if (_activeActors.TryGetValue(deathEvent.ActorId, out var actor))
        {
            // Play death animation
            if (actor.AnimationController != null)
            {
                actor.AnimationController.PlayAnimation(ActorAnimationType.Death);
            }

            // Remove actor after delay (for death animation)
            StartCoroutine(RemoveActorAfterDelay(deathEvent.ActorId, 3f));
        }

        if (ShowDebugLogs)
        {
            Debug.Log($"[AIActorManager] AI {deathEvent.ActorId} died (killed by {deathEvent.KillerId})");
        }
    }

    /// <summary>
    /// Called when AI spawn response is received
    /// </summary>
    public void OnAISpawnResponseReceived(AISpawnResponse response)
    {
        if (response == null)
            return;

        if (response.Success)
        {
            Debug.Log($"[AIActorManager] AI spawned successfully: {response.Message} (ID: {response.SpawnedActorId})");
        }
        else
        {
            Debug.LogWarning($"[AIActorManager] AI spawn failed: {response.Message}");
        }
    }

    /// <summary>
    /// Create a new AI actor GameObject
    /// </summary>
    private void CreateAIActor(AIActorInfo actorInfo)
    {
        GameObject prefab = GetPrefabForAIType(actorInfo.ActorName);
        if (prefab == null)
        {
            Debug.LogWarning($"[AIActorManager] No prefab found for AI type: {actorInfo.ActorName}");
            return;
        }

        var actorGO = Instantiate(prefab, new Vector3(actorInfo.Position.X, actorInfo.Position.Y, actorInfo.Position.Z), Quaternion.identity);
        actorGO.name = $"AI_{actorInfo.ActorName}_{actorInfo.ActorId}";

        var actorController = actorGO.GetComponent<ActorController>();
        var animationController = actorGO.GetComponent<ActorAnimationController>();

        // Disable client-side AI (server controls everything)
        if (actorController != null)
        {
            actorController.enabled = false; // Don't run BehaviorTree on client
        }

        var instance = new AIActorInstance
        {
            ActorId = actorInfo.ActorId,
            GameObject = actorGO,
            ActorController = actorController,
            AnimationController = animationController,
            TargetPosition = new Vector3(actorInfo.Position.X, actorInfo.Position.Y, actorInfo.Position.Z),
            CurrentState = actorInfo.State,
            Health = actorInfo.Health,
            MaxHealth = actorInfo.MaxHealth
        };

        _activeActors[actorInfo.ActorId] = instance;

        if (ShowDebugLogs)
        {
            Debug.Log($"[AIActorManager] Created AI actor: {actorInfo.ActorName} (ID: {actorInfo.ActorId})");
        }
    }

    /// <summary>
    /// Update existing AI actor from server state
    /// </summary>
    private void UpdateAIActor(AIActorInfo actorInfo)
    {
        if (!_activeActors.TryGetValue(actorInfo.ActorId, out var actor))
            return;

        // Update target position (will be interpolated in Update())
        actor.TargetPosition = new Vector3(actorInfo.Position.X, actorInfo.Position.Y, actorInfo.Position.Z);
        actor.Health = actorInfo.Health;
        actor.MaxHealth = actorInfo.MaxHealth;

        // Update animation if state changed
        if (actor.CurrentState != actorInfo.State)
        {
            actor.CurrentState = actorInfo.State;

            if (actor.AnimationController != null)
            {
                switch (actorInfo.State)
                {
                    case AIState.AiIdle:
                        actor.AnimationController.PlayAnimation(ActorAnimationType.Idle);
                        break;
                    case AIState.AiWander:
                        actor.AnimationController.PlayAnimation(ActorAnimationType.Walk);
                        break;
                    case AIState.AiChase:
                        actor.AnimationController.PlayAnimation(ActorAnimationType.Run);
                        break;
                    case AIState.AiAttack:
                        actor.AnimationController.PlayAnimation(ActorAnimationType.Attack);
                        break;
                    case AIState.AiFlee:
                        actor.AnimationController.PlayAnimation(ActorAnimationType.Flee);
                        break;
                    case AIState.AiDead:
                        actor.AnimationController.PlayAnimation(ActorAnimationType.Death);
                        break;
                }
            }
        }

        // Update health bar
        // TODO: Implement health bar UI
    }

    /// <summary>
    /// Remove AI actor
    /// </summary>
    private void RemoveAIActor(int actorId)
    {
        if (_activeActors.TryGetValue(actorId, out var actor))
        {
            if (actor.GameObject != null)
            {
                Destroy(actor.GameObject);
            }
            _activeActors.Remove(actorId);

            if (ShowDebugLogs)
            {
                Debug.Log($"[AIActorManager] Removed AI actor: {actorId}");
            }
        }
    }

    /// <summary>
    /// Remove actor after delay (for death animation)
    /// </summary>
    private System.Collections.IEnumerator RemoveActorAfterDelay(int actorId, float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveAIActor(actorId);
    }

    /// <summary>
    /// Get prefab for AI type based on actor name
    /// </summary>
    private GameObject GetPrefabForAIType(string actorName)
    {
        if (actorName.Contains("Aggressive"))
            return AggressiveAIPrefab;
        else if (actorName.Contains("Defensive"))
            return DefensiveAIPrefab;
        else if (actorName.Contains("Coward"))
            return CowardAIPrefab;
        else if (actorName.Contains("Boss"))
            return BossAIPrefab;
        else if (actorName.Contains("Flying"))
            return FlyingAIPrefab;
        else if (actorName.Contains("Ranged"))
            return RangedAIPrefab;
        else
            return AggressiveAIPrefab; // Default
    }

    /// <summary>
    /// Get current actor count
    /// </summary>
    public int GetActiveActorCount()
    {
        return _activeActors.Count;
    }

    void OnDrawGizmos()
    {
        if (!ShowDebugGizmos || _activeActors == null)
            return;

        foreach (var kvp in _activeActors)
        {
            var actor = kvp.Value;
            if (actor.GameObject != null)
            {
                // Draw sphere at actor position
                Gizmos.color = GetColorForState(actor.CurrentState);
                Gizmos.DrawWireSphere(actor.GameObject.transform.position, 1f);

                // Draw line to target position
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(actor.GameObject.transform.position, actor.TargetPosition);
            }
        }
    }

    private Color GetColorForState(AIState state)
    {
        switch (state)
        {
            case AIState.AiIdle: return Color.green;
            case AIState.AiWander: return Color.cyan;
            case AIState.AiChase: return Color.yellow;
            case AIState.AiAttack: return Color.red;
            case AIState.AiFlee: return Color.magenta;
            case AIState.AiDead: return Color.gray;
            default: return Color.white;
        }
    }
}
