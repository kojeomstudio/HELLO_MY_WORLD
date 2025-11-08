using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI 인지 시스템 (Perception System)
///
/// 기능:
/// - 시야(Sight) 기반 엔티티 감지
/// - 청각(Hearing) 기반 소리 감지
/// - BlackBoard와 자동 연동
/// - FOV (Field of View) 기반 시야 체크
/// - 레이캐스트로 장애물 가림 처리
///
/// 사용법:
/// 1. AI Actor GameObject에 PerceptionSystem 컴포넌트 추가
/// 2. BehaviorTree에서 PerceptionSystem 참조
/// 3. BlackBoard에 감지된 정보 자동 저장됨
/// </summary>
public class PerceptionSystem : MonoBehaviour
{
    // ============================================================
    // SIGHT (시야)
    // ============================================================
    [Header("Sight Settings")]
    [Tooltip("시야 거리")]
    public float SightRange = 20.0f;

    [Tooltip("시야각 (도, 180 = 반구, 360 = 전방위)")]
    [Range(0f, 360f)]
    public float SightAngle = 120.0f;

    [Tooltip("시야 업데이트 주기 (초)")]
    public float SightUpdateInterval = 0.2f;

    [Tooltip("감지할 레이어 (Player, Monster, NPC 등)")]
    public LayerMask DetectableLayers;

    [Tooltip("장애물 레이어 (시야 가림 판정)")]
    public LayerMask ObstacleLayers;

    // ============================================================
    // HEARING (청각)
    // ============================================================
    [Header("Hearing Settings")]
    [Tooltip("청각 범위")]
    public float HearingRange = 15.0f;

    [Tooltip("청각 업데이트 주기 (초)")]
    public float HearingUpdateInterval = 0.5f;

    // ============================================================
    // DEBUG
    // ============================================================
    [Header("Debug")]
    [Tooltip("Gizmo 표시 여부")]
    public bool ShowDebugGizmos = true;

    // ============================================================
    // INTERNAL
    // ============================================================
    private BlackBoard _blackBoard;
    private float _sightUpdateTimer = 0f;
    private float _hearingUpdateTimer = 0f;
    private Transform _transform;

    private List<GameObject> _detectedEntities = new List<GameObject>();

    /// <summary>
    /// BlackBoard 연결
    /// </summary>
    public void Initialize(BlackBoard blackBoard)
    {
        _blackBoard = blackBoard;
        _transform = transform;

        if (_blackBoard != null)
        {
            _blackBoard.DetectionRange = SightRange;
        }
    }

    void Update()
    {
        if (_blackBoard == null)
        {
            return;
        }

        // 시야 업데이트
        _sightUpdateTimer += Time.deltaTime;
        if (_sightUpdateTimer >= SightUpdateInterval)
        {
            _sightUpdateTimer = 0f;
            UpdateSight();
        }

        // 청각 업데이트
        _hearingUpdateTimer += Time.deltaTime;
        if (_hearingUpdateTimer >= HearingUpdateInterval)
        {
            _hearingUpdateTimer = 0f;
            UpdateHearing();
        }

        // 오래된 인지 데이터 정리 (10초 이상)
        _blackBoard.CleanupOldPerceptionData(10f);
    }

    /// <summary>
    /// 시야 업데이트 (FOV + 레이캐스트)
    /// </summary>
    private void UpdateSight()
    {
        _detectedEntities.Clear();
        _blackBoard.VisibleEntities.Clear();

        // 범위 내 모든 콜라이더 검사
        Collider[] colliders = Physics.OverlapSphere(_transform.position, SightRange, DetectableLayers);

        foreach (Collider col in colliders)
        {
            // 자기 자신 제외
            if (col.gameObject == gameObject)
                continue;

            Vector3 targetPosition = col.bounds.center;
            Vector3 directionToTarget = (targetPosition - _transform.position).normalized;
            float distanceToTarget = Vector3.Distance(_transform.position, targetPosition);

            // FOV (Field of View) 체크
            float angleToTarget = Vector3.Angle(_transform.forward, directionToTarget);
            if (angleToTarget > SightAngle / 2f)
            {
                // 시야각 밖
                continue;
            }

            // 레이캐스트로 장애물 가림 체크
            RaycastHit hit;
            if (Physics.Raycast(_transform.position, directionToTarget, out hit, distanceToTarget, ObstacleLayers | DetectableLayers))
            {
                // 레이캐스트가 타겟이 아닌 장애물에 먼저 맞으면 가려진 것
                if (hit.collider.gameObject != col.gameObject)
                {
                    // 장애물에 가려짐
                    UpdatePerceivedEntityAsHidden(col.gameObject, targetPosition, distanceToTarget);
                    continue;
                }
            }

            // 시야에 보임
            _detectedEntities.Add(col.gameObject);
            _blackBoard.VisibleEntities.Add(col.gameObject);
            _blackBoard.AddOrUpdatePerceivedEntity(col.gameObject, targetPosition, distanceToTarget, true);

            // 위협 레벨 계산 (거리 기반)
            if (_blackBoard.PerceivedEntities.ContainsKey(col.gameObject))
            {
                float threatLevel = CalculateThreatLevel(col.gameObject, distanceToTarget);
                _blackBoard.PerceivedEntities[col.gameObject].ThreatLevel = threatLevel;
            }
        }
    }

    /// <summary>
    /// 가려진 엔티티를 BlackBoard에 업데이트 (보이지 않지만 기억은 유지)
    /// </summary>
    private void UpdatePerceivedEntityAsHidden(GameObject entity, Vector3 lastPosition, float distance)
    {
        if (_blackBoard.PerceivedEntities.ContainsKey(entity))
        {
            var perceived = _blackBoard.PerceivedEntities[entity];
            perceived.IsVisible = false;
            perceived.LastKnownPosition = lastPosition;
            perceived.Distance = distance;
            // LastSeenTime은 업데이트하지 않음 (마지막으로 본 시간 유지)
        }
    }

    /// <summary>
    /// 위협 레벨 계산
    ///
    /// 기준:
    /// - 거리가 가까울수록 위협도 증가
    /// - 플레이어는 기본 위협도 높음
    /// - 공격받은 적은 위협도 최대
    /// </summary>
    private float CalculateThreatLevel(GameObject entity, float distance)
    {
        float baseThreat = 0.3f;

        // 플레이어인 경우 위협도 증가
        if (entity.CompareTag("Player"))
        {
            baseThreat = 0.7f;
        }

        // 최근 공격자인 경우 위협도 최대
        if (_blackBoard.Memory.LastAttacker == entity)
        {
            baseThreat = 1.0f;
        }

        // 거리 기반 위협도 증가 (가까울수록 위험)
        float distanceFactor = 1.0f - Mathf.Clamp01(distance / SightRange);
        float finalThreat = Mathf.Clamp01(baseThreat + distanceFactor * 0.3f);

        return finalThreat;
    }

    /// <summary>
    /// 청각 업데이트 (소리 감지)
    ///
    /// TODO: 향후 SoundManager와 연동하여 실제 소리 이벤트 처리
    /// 현재는 범위 내 엔티티의 움직임을 "소리"로 간주
    /// </summary>
    private void UpdateHearing()
    {
        // TODO: SoundManager에서 발생한 소리 이벤트 리스닝
        // 현재는 간단한 거리 기반 감지만 구현

        Collider[] colliders = Physics.OverlapSphere(_transform.position, HearingRange, DetectableLayers);

        foreach (Collider col in colliders)
        {
            if (col.gameObject == gameObject)
                continue;

            // 청각으로 감지 (시야에 없어도 감지)
            if (!_blackBoard.VisibleEntities.Contains(col.gameObject))
            {
                Vector3 soundPosition = col.transform.position;
                float distance = Vector3.Distance(_transform.position, soundPosition);

                // BlackBoard에 소리 정보 저장
                _blackBoard.LastHeardSoundPosition = soundPosition;
                _blackBoard.LastHeardSoundTime = Time.time;

                // 인지 목록에 추가 (보이진 않지만 소리로 감지)
                _blackBoard.AddOrUpdatePerceivedEntity(col.gameObject, soundPosition, distance, false);
            }
        }
    }

    /// <summary>
    /// 특정 위치에서 소리 발생 (외부 호출용)
    ///
    /// 예: 폭발, 총소리, 발소리 등
    /// </summary>
    public void OnSoundHeard(Vector3 soundPosition, float soundIntensity)
    {
        if (_blackBoard == null)
            return;

        float distance = Vector3.Distance(_transform.position, soundPosition);

        // 청각 범위 내이고, 소리 강도가 충분한 경우
        if (distance <= HearingRange * soundIntensity)
        {
            _blackBoard.LastHeardSoundPosition = soundPosition;
            _blackBoard.LastHeardSoundTime = Time.time;

            // 소리 방향으로 관심 표시 (향후 "조사하러 가기" 행동 트리거)
            Debug.Log($"[Perception] Heard sound at {soundPosition}, distance: {distance:F2}m");
        }
    }

    /// <summary>
    /// 데미지를 받았을 때 호출 (외부 호출용)
    /// </summary>
    public void OnDamageReceived(GameObject attacker, Vector3 damagePosition, float damageAmount)
    {
        if (_blackBoard == null || attacker == null)
            return;

        // 메모리에 기록
        _blackBoard.Memory.LastAttacker = attacker;
        _blackBoard.Memory.LastDamagedPosition = damagePosition;
        _blackBoard.Memory.LastDamagedTime = Time.time;

        // 어그로 추가 (데미지 비례)
        _blackBoard.AddAggro(attacker, damageAmount * 2f);

        // 인지 목록에 추가
        float distance = Vector3.Distance(_transform.position, attacker.transform.position);
        _blackBoard.AddOrUpdatePerceivedEntity(attacker, attacker.transform.position, distance, true);

        Debug.Log($"[Perception] Received {damageAmount} damage from {attacker.name}, aggro: {_blackBoard.AggroList[attacker]}");
    }

    // ============================================================
    // DEBUG GIZMOS
    // ============================================================
    void OnDrawGizmos()
    {
        if (!ShowDebugGizmos)
            return;

        // 시야 범위 (녹색 반투명 원)
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, SightRange);

        // 청각 범위 (파란색 반투명 원)
        Gizmos.color = new Color(0, 0, 1, 0.1f);
        Gizmos.DrawSphere(transform.position, HearingRange);

        // 시야각 (노란색 선)
        Gizmos.color = Color.yellow;
        Vector3 leftBoundary = Quaternion.Euler(0, -SightAngle / 2f, 0) * transform.forward * SightRange;
        Vector3 rightBoundary = Quaternion.Euler(0, SightAngle / 2f, 0) * transform.forward * SightRange;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

        if (_blackBoard != null)
        {
            // 감지된 엔티티 (빨간색 선)
            Gizmos.color = Color.red;
            foreach (var entity in _blackBoard.VisibleEntities)
            {
                if (entity != null)
                {
                    Gizmos.DrawLine(transform.position, entity.transform.position);
                }
            }

            // 현재 타겟 (굵은 빨간색 선)
            if (_blackBoard.CurrentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _blackBoard.CurrentTarget.transform.position);
                Gizmos.DrawSphere(_blackBoard.CurrentTarget.transform.position, 0.5f);
            }

            // 마지막으로 들은 소리 위치 (파란색 구)
            if (Time.time - _blackBoard.LastHeardSoundTime < 2f)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(_blackBoard.LastHeardSoundPosition, 0.3f);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!ShowDebugGizmos || _blackBoard == null)
            return;

        // 인지된 모든 엔티티 상세 정보
        foreach (var kvp in _blackBoard.PerceivedEntities)
        {
            if (kvp.Key == null)
                continue;

            PerceivedEntity perceived = kvp.Value;

            // 위협 레벨에 따라 색상 변경 (녹색 → 노란색 → 빨간색)
            Gizmos.color = Color.Lerp(Color.green, Color.red, perceived.ThreatLevel);

            // 마지막 알려진 위치
            Gizmos.DrawSphere(perceived.LastKnownPosition, 0.3f);

            // 정보 레이블 (Unity Editor에서만 표시)
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                perceived.LastKnownPosition + Vector3.up * 2f,
                $"{kvp.Key.name}\nDist: {perceived.Distance:F1}m\nThreat: {perceived.ThreatLevel:F2}\nVisible: {perceived.IsVisible}"
            );
            #endif
        }
    }
}
