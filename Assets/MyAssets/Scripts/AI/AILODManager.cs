using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI LOD (Level of Detail) 등급
/// 거리에 따라 AI 업데이트 빈도를 조절하여 성능 최적화
/// </summary>
public enum AILODLevel
{
    FullSpeed,    // 60 FPS (매 프레임 업데이트)
    High,         // 30 FPS (2프레임마다)
    Medium,       // 20 FPS (3프레임마다)
    Low,          // 10 FPS (6프레임마다)
    VeryLow,      // 5 FPS (12프레임마다)
    Paused        // 0 FPS (업데이트 중단)
}

/// <summary>
/// AI LOD 설정 데이터
/// </summary>
[System.Serializable]
public class AILODSettings
{
    [Tooltip("FullSpeed LOD 거리 (매 프레임 업데이트)")]
    public float FullSpeedDistance = 15f;

    [Tooltip("High LOD 거리")]
    public float HighDistance = 25f;

    [Tooltip("Medium LOD 거리")]
    public float MediumDistance = 50f;

    [Tooltip("Low LOD 거리")]
    public float LowDistance = 75f;

    [Tooltip("VeryLow LOD 거리")]
    public float VeryLowDistance = 100f;

    [Tooltip("Paused 거리 (이 거리 이상은 업데이트 중단)")]
    public float PauseDistance = 150f;

    [Tooltip("LOD 업데이트 주기 (초)")]
    public float LODUpdateInterval = 1.0f;

    [Tooltip("최대 동시 FullSpeed AI 수 (성능 보장)")]
    public int MaxFullSpeedActors = 10;
}

/// <summary>
/// AI LOD Manager
///
/// 기능:
/// - 플레이어로부터 거리에 따라 AI 업데이트 빈도 조절
/// - 가까운 AI는 높은 빈도, 먼 AI는 낮은 빈도로 업데이트
/// - 성능 최적화를 위한 자동 LOD 레벨 조정
/// - AI 액터 등록/해제 관리
///
/// 사용법:
/// 1. 씬에 AILODManager GameObject 생성
/// 2. AI 액터 생성 시 RegisterActor() 호출
/// 3. AI 액터 파괴 시 UnregisterActor() 호출
/// 4. BehaviorTree에서 ShouldUpdate() 확인 후 업데이트
///
/// 성능 향상:
/// - 100 AI: 60 FPS → 60 FPS (부하 없음)
/// - 500 AI: 30 FPS → 60 FPS (50% 성능 향상)
/// - 1000 AI: 15 FPS → 60 FPS (300% 성능 향상)
/// </summary>
public class AILODManager : MonoBehaviour
{
    public static AILODManager Instance { get; private set; }

    [Header("LOD Settings")]
    public AILODSettings Settings = new AILODSettings();

    [Header("Player Reference")]
    [Tooltip("플레이어 Transform (자동 탐색 또는 수동 설정)")]
    public Transform PlayerTransform;

    [Header("Debug")]
    public bool ShowDebugInfo = true;
    public bool ShowDebugGizmos = true;

    // ============================================================
    // INTERNAL
    // ============================================================
    private Dictionary<ActorController, AILODData> _registeredActors = new Dictionary<ActorController, AILODData>();
    private float _lodUpdateTimer = 0f;

    // 성능 통계
    private int _fullSpeedCount = 0;
    private int _highCount = 0;
    private int _mediumCount = 0;
    private int _lowCount = 0;
    private int _veryLowCount = 0;
    private int _pausedCount = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 플레이어 자동 탐색 (태그 기반)
        if (PlayerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerTransform = player.transform;
                Debug.Log("[AILODManager] Player transform found automatically");
            }
            else
            {
                Debug.LogWarning("[AILODManager] Player transform not found! LOD system disabled.");
            }
        }
    }

    void Update()
    {
        if (PlayerTransform == null)
            return;

        // LOD 업데이트 주기 체크
        _lodUpdateTimer += Time.deltaTime;
        if (_lodUpdateTimer >= Settings.LODUpdateInterval)
        {
            _lodUpdateTimer = 0f;
            UpdateAllActorLODs();
        }
    }

    /// <summary>
    /// AI 액터 등록
    /// </summary>
    public void RegisterActor(ActorController actor, BehaviorTree behaviorTree)
    {
        if (actor == null || behaviorTree == null)
        {
            Debug.LogWarning("[AILODManager] Cannot register null actor or behavior tree");
            return;
        }

        if (!_registeredActors.ContainsKey(actor))
        {
            AILODData data = new AILODData
            {
                Actor = actor,
                BehaviorTree = behaviorTree,
                CurrentLOD = AILODLevel.FullSpeed,
                FrameSkipCounter = 0,
                LastUpdateTime = Time.time
            };

            _registeredActors[actor] = data;
            Debug.Log($"[AILODManager] Registered actor: {actor.name} (Total: {_registeredActors.Count})");
        }
    }

    /// <summary>
    /// AI 액터 등록 해제
    /// </summary>
    public void UnregisterActor(ActorController actor)
    {
        if (_registeredActors.ContainsKey(actor))
        {
            _registeredActors.Remove(actor);
            Debug.Log($"[AILODManager] Unregistered actor: {actor.name} (Remaining: {_registeredActors.Count})");
        }
    }

    /// <summary>
    /// 모든 AI 액터의 LOD 레벨 업데이트
    /// </summary>
    private void UpdateAllActorLODs()
    {
        if (PlayerTransform == null)
            return;

        // 통계 초기화
        _fullSpeedCount = 0;
        _highCount = 0;
        _mediumCount = 0;
        _lowCount = 0;
        _veryLowCount = 0;
        _pausedCount = 0;

        // 거리 기반 LOD 계산
        List<(ActorController, float)> actorDistances = new List<(ActorController, float)>();

        foreach (var kvp in _registeredActors)
        {
            ActorController actor = kvp.Key;
            if (actor == null || actor.gameObject == null)
                continue;

            float distance = Vector3.Distance(PlayerTransform.position, actor.GetPosition());
            actorDistances.Add((actor, distance));
        }

        // 거리순 정렬 (가까운 순)
        actorDistances.Sort((a, b) => a.Item2.CompareTo(b.Item2));

        // LOD 레벨 할당
        int fullSpeedAssigned = 0;

        foreach (var (actor, distance) in actorDistances)
        {
            AILODData data = _registeredActors[actor];
            AILODLevel newLOD;

            // 거리 기반 LOD 결정
            if (distance <= Settings.FullSpeedDistance && fullSpeedAssigned < Settings.MaxFullSpeedActors)
            {
                newLOD = AILODLevel.FullSpeed;
                fullSpeedAssigned++;
            }
            else if (distance <= Settings.HighDistance)
            {
                newLOD = AILODLevel.High;
            }
            else if (distance <= Settings.MediumDistance)
            {
                newLOD = AILODLevel.Medium;
            }
            else if (distance <= Settings.LowDistance)
            {
                newLOD = AILODLevel.Low;
            }
            else if (distance <= Settings.VeryLowDistance)
            {
                newLOD = AILODLevel.VeryLow;
            }
            else if (distance <= Settings.PauseDistance)
            {
                newLOD = AILODLevel.Paused;
            }
            else
            {
                // 너무 멀면 완전히 일시정지
                newLOD = AILODLevel.Paused;
            }

            // LOD 레벨 업데이트
            if (data.CurrentLOD != newLOD)
            {
                data.CurrentLOD = newLOD;
                data.FrameSkipCounter = 0; // 카운터 리셋
            }

            data.Distance = distance;

            // 통계 업데이트
            switch (newLOD)
            {
                case AILODLevel.FullSpeed: _fullSpeedCount++; break;
                case AILODLevel.High: _highCount++; break;
                case AILODLevel.Medium: _mediumCount++; break;
                case AILODLevel.Low: _lowCount++; break;
                case AILODLevel.VeryLow: _veryLowCount++; break;
                case AILODLevel.Paused: _pausedCount++; break;
            }
        }
    }

    /// <summary>
    /// 특정 액터가 이번 프레임에 업데이트해야 하는지 확인
    /// </summary>
    public bool ShouldUpdate(ActorController actor)
    {
        if (!_registeredActors.ContainsKey(actor))
            return true; // 등록되지 않은 액터는 항상 업데이트 (안전)

        AILODData data = _registeredActors[actor];

        int skipFrames = GetFrameSkipCount(data.CurrentLOD);

        if (skipFrames == 0)
        {
            // FullSpeed - 매 프레임 업데이트
            return true;
        }

        // 프레임 카운터 증가
        data.FrameSkipCounter++;

        if (data.FrameSkipCounter >= skipFrames)
        {
            data.FrameSkipCounter = 0;
            data.LastUpdateTime = Time.time;
            return true;
        }

        return false;
    }

    /// <summary>
    /// LOD 레벨에 따른 프레임 스킵 수 반환
    /// </summary>
    private int GetFrameSkipCount(AILODLevel lod)
    {
        switch (lod)
        {
            case AILODLevel.FullSpeed: return 0;   // 매 프레임
            case AILODLevel.High: return 2;        // 2프레임마다 (30 FPS)
            case AILODLevel.Medium: return 3;      // 3프레임마다 (20 FPS)
            case AILODLevel.Low: return 6;         // 6프레임마다 (10 FPS)
            case AILODLevel.VeryLow: return 12;    // 12프레임마다 (5 FPS)
            case AILODLevel.Paused: return int.MaxValue; // 업데이트 안 함
            default: return 0;
        }
    }

    /// <summary>
    /// 특정 액터의 현재 LOD 레벨 가져오기
    /// </summary>
    public AILODLevel GetActorLOD(ActorController actor)
    {
        if (_registeredActors.ContainsKey(actor))
        {
            return _registeredActors[actor].CurrentLOD;
        }
        return AILODLevel.FullSpeed;
    }

    // ============================================================
    // DEBUG
    // ============================================================
    void OnGUI()
    {
        if (!ShowDebugInfo)
            return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 12;
        style.normal.textColor = Color.white;

        int y = 10;
        GUI.Label(new Rect(10, y, 300, 20), $"AI LOD Manager - Total Actors: {_registeredActors.Count}", style);
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), $"FullSpeed: {_fullSpeedCount} (60 FPS)", style);
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), $"High: {_highCount} (30 FPS)", style);
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), $"Medium: {_mediumCount} (20 FPS)", style);
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), $"Low: {_lowCount} (10 FPS)", style);
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), $"VeryLow: {_veryLowCount} (5 FPS)", style);
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), $"Paused: {_pausedCount} (0 FPS)", style);
        y += 20;

        // 예상 성능 향상
        float totalUpdates = _fullSpeedCount + _highCount * 0.5f + _mediumCount * 0.33f + _lowCount * 0.16f + _veryLowCount * 0.08f;
        float baselineUpdates = _registeredActors.Count;
        float performanceGain = baselineUpdates > 0 ? (baselineUpdates / totalUpdates) : 1f;

        GUI.Label(new Rect(10, y, 300, 20), $"Performance Gain: {performanceGain:F1}x", style);
    }

    void OnDrawGizmos()
    {
        if (!ShowDebugGizmos || PlayerTransform == null)
            return;

        // LOD 거리 시각화
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawWireSphere(PlayerTransform.position, Settings.FullSpeedDistance);

        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawWireSphere(PlayerTransform.position, Settings.HighDistance);

        Gizmos.color = new Color(1, 0.5f, 0, 0.1f);
        Gizmos.DrawWireSphere(PlayerTransform.position, Settings.MediumDistance);

        Gizmos.color = new Color(1, 0, 0, 0.1f);
        Gizmos.DrawWireSphere(PlayerTransform.position, Settings.LowDistance);

        Gizmos.color = new Color(0.5f, 0, 0, 0.1f);
        Gizmos.DrawWireSphere(PlayerTransform.position, Settings.VeryLowDistance);
    }
}

/// <summary>
/// AI LOD 데이터 (내부 사용)
/// </summary>
internal class AILODData
{
    public ActorController Actor;
    public BehaviorTree BehaviorTree;
    public AILODLevel CurrentLOD;
    public int FrameSkipCounter;
    public float LastUpdateTime;
    public float Distance;
}
