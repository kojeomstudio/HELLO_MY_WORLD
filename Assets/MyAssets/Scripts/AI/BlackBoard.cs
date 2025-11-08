using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인지된 엔티티 정보 (Perception System 데이터)
/// </summary>
public class PerceivedEntity
{
    public GameObject Entity;
    public Vector3 LastKnownPosition;
    public float LastSeenTime;
    public float ThreatLevel; // 0.0 ~ 1.0
    public bool IsVisible;
    public float Distance;

    public PerceivedEntity(GameObject entity, Vector3 position, float time)
    {
        Entity = entity;
        LastKnownPosition = position;
        LastSeenTime = time;
        ThreatLevel = 0.0f;
        IsVisible = true;
        Distance = 0.0f;
    }
}

/// <summary>
/// AI 전투 상태
/// </summary>
public enum CombatMode
{
    Passive,    // 비전투 상태
    Defensive,  // 방어 모드 (공격받으면 반격)
    Aggressive, // 공격적 (적극적으로 타겟 추적)
    Fleeing     // 도주 중
}

/// <summary>
/// AI 메모리 (최근 행동 기록)
/// </summary>
public class AIMemory
{
    public Vector3 LastDamagedPosition = Vector3.zero;
    public float LastDamagedTime = 0f;
    public GameObject LastAttacker = null;
    public List<Vector3> PatrolPoints = new List<Vector3>();
    public int CurrentPatrolIndex = 0;
}

/// <summary>
/// Enhanced BlackBoard - AI 의사결정을 위한 통합 데이터 저장소
///
/// 기능:
/// - Navigation: 경로 탐색 및 이동 목표
/// - Perception: 인지된 엔티티 및 위협 정보
/// - Combat: 전투 상태, 타겟, 어그로 관리
/// - Memory: 과거 경험 및 행동 기록
/// - Status: 체력, 스태미나, 버프/디버프
/// </summary>
public class BlackBoard
{
    // ============================================================
    // NAVIGATION (기존 유지)
    // ============================================================
    public Stack<PathNode3D> PathList = new Stack<PathNode3D>();
    public Vector3 PathFidningTargetPoint = Vector3.zero;
    public bool IsPathFindingActive = false;
    public float PathRecalculationTimer = 0f;

    // ============================================================
    // PERCEPTION (인지 시스템)
    // ============================================================
    /// <summary>인지된 모든 엔티티 (플레이어, 몬스터, NPC 등)</summary>
    public Dictionary<GameObject, PerceivedEntity> PerceivedEntities = new Dictionary<GameObject, PerceivedEntity>();

    /// <summary>현재 시야에 있는 엔티티 목록</summary>
    public List<GameObject> VisibleEntities = new List<GameObject>();

    /// <summary>청각으로 감지된 소리 소스 위치</summary>
    public Vector3 LastHeardSoundPosition = Vector3.zero;
    public float LastHeardSoundTime = 0f;

    /// <summary>가장 위협적인 엔티티 (자동 계산됨)</summary>
    public GameObject MostThreateningEntity
    {
        get
        {
            GameObject mostThreatening = null;
            float highestThreat = 0f;

            foreach (var kvp in PerceivedEntities)
            {
                if (kvp.Value.IsVisible && kvp.Value.ThreatLevel > highestThreat)
                {
                    highestThreat = kvp.Value.ThreatLevel;
                    mostThreatening = kvp.Key;
                }
            }

            return mostThreatening;
        }
    }

    // ============================================================
    // COMBAT (전투 시스템)
    // ============================================================
    public CombatMode CurrentCombatMode = CombatMode.Passive;

    /// <summary>현재 공격 타겟</summary>
    public GameObject CurrentTarget = null;

    /// <summary>어그로 리스트 (타겟별 어그로 수치)</summary>
    public Dictionary<GameObject, float> AggroList = new Dictionary<GameObject, float>();

    /// <summary>마지막 공격 시간</summary>
    public float LastAttackTime = 0f;

    /// <summary>공격 쿨다운</summary>
    public float AttackCooldown = 2.0f;

    /// <summary>공격 범위</summary>
    public float AttackRange = 3.0f;

    /// <summary>감지 범위 (시야 거리)</summary>
    public float DetectionRange = 20.0f;

    /// <summary>도주 체력 임계값 (0.0 ~ 1.0)</summary>
    public float FleeHealthThreshold = 0.3f;

    /// <summary>전투 중인가?</summary>
    public bool IsInCombat => CurrentTarget != null && CurrentCombatMode != CombatMode.Passive;

    /// <summary>공격 가능한가?</summary>
    public bool CanAttack => Time.time - LastAttackTime >= AttackCooldown;

    // ============================================================
    // MEMORY (AI 기억)
    // ============================================================
    public AIMemory Memory = new AIMemory();

    // ============================================================
    // STATUS (상태 정보)
    // ============================================================
    /// <summary>현재 체력 비율 (0.0 ~ 1.0)</summary>
    public float HealthRatio = 1.0f;

    /// <summary>현재 스태미나 비율 (0.0 ~ 1.0)</summary>
    public float StaminaRatio = 1.0f;

    /// <summary>현재 적용된 버프 목록</summary>
    public List<string> ActiveBuffs = new List<string>();

    /// <summary>현재 적용된 디버프 목록</summary>
    public List<string> ActiveDebuffs = new List<string>();

    /// <summary>스턴 상태</summary>
    public bool IsStunned = false;

    /// <summary>슬로우 배율 (1.0 = 정상, 0.5 = 50% 감속)</summary>
    public float SlowMultiplier = 1.0f;

    // ============================================================
    // BEHAVIOR FLAGS (행동 플래그)
    // ============================================================
    /// <summary>배회 중</summary>
    public bool IsWandering = false;

    /// <summary>순찰 중</summary>
    public bool IsPatrolling = false;

    /// <summary>추적 중</summary>
    public bool IsChasing = false;

    /// <summary>도주 중</summary>
    public bool IsFleeing = false;

    // ============================================================
    // UTILITY METHODS
    // ============================================================

    /// <summary>
    /// 엔티티를 인지 목록에 추가/업데이트
    /// </summary>
    public void AddOrUpdatePerceivedEntity(GameObject entity, Vector3 position, float distance, bool isVisible)
    {
        if (PerceivedEntities.ContainsKey(entity))
        {
            var perceived = PerceivedEntities[entity];
            perceived.LastKnownPosition = position;
            perceived.LastSeenTime = Time.time;
            perceived.IsVisible = isVisible;
            perceived.Distance = distance;
        }
        else
        {
            PerceivedEntities[entity] = new PerceivedEntity(entity, position, Time.time);
            PerceivedEntities[entity].Distance = distance;
            PerceivedEntities[entity].IsVisible = isVisible;
        }
    }

    /// <summary>
    /// 어그로 추가
    /// </summary>
    public void AddAggro(GameObject target, float amount)
    {
        if (AggroList.ContainsKey(target))
        {
            AggroList[target] += amount;
        }
        else
        {
            AggroList[target] = amount;
        }

        // 어그로가 추가되면 자동으로 인지 목록에도 추가
        if (target != null)
        {
            AddOrUpdatePerceivedEntity(target, target.transform.position,
                Vector3.Distance(PathFidningTargetPoint, target.transform.position), true);

            // 위협 레벨도 업데이트
            if (PerceivedEntities.ContainsKey(target))
            {
                PerceivedEntities[target].ThreatLevel = Mathf.Clamp01(AggroList[target] / 100f);
            }
        }
    }

    /// <summary>
    /// 가장 높은 어그로를 가진 타겟 가져오기
    /// </summary>
    public GameObject GetHighestAggroTarget()
    {
        GameObject highestTarget = null;
        float highestAggro = 0f;

        foreach (var kvp in AggroList)
        {
            if (kvp.Key != null && kvp.Value > highestAggro)
            {
                highestAggro = kvp.Value;
                highestTarget = kvp.Key;
            }
        }

        return highestTarget;
    }

    /// <summary>
    /// 오래된 인지 데이터 정리 (메모리 관리)
    /// </summary>
    public void CleanupOldPerceptionData(float maxAge = 10f)
    {
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var kvp in PerceivedEntities)
        {
            if (Time.time - kvp.Value.LastSeenTime > maxAge)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var entity in toRemove)
        {
            PerceivedEntities.Remove(entity);
        }
    }

    /// <summary>
    /// BlackBoard 초기화
    /// </summary>
    public void Reset()
    {
        PathList.Clear();
        PathFidningTargetPoint = Vector3.zero;
        IsPathFindingActive = false;

        PerceivedEntities.Clear();
        VisibleEntities.Clear();

        CurrentTarget = null;
        AggroList.Clear();

        CurrentCombatMode = CombatMode.Passive;
        IsWandering = false;
        IsPatrolling = false;
        IsChasing = false;
        IsFleeing = false;
    }
}

