using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공격적인 몬스터 AI (Aggressive Monster)
///
/// 행동 패턴:
/// 1. Idle: 배회하며 플레이어 탐색
/// 2. Detection: 플레이어 발견 시 어그로 획득
/// 3. Chase: 타겟 추적
/// 4. Attack: 사거리 내 공격
/// 5. Flee: 체력 낮으면 도주
/// 6. Death: 사망 처리
///
/// Behavior Tree 구조:
/// Root (Sequence)
///   ├─ CheckDead → DeadProcess
///   └─ Selector (전투 또는 평화)
///       ├─ Combat Sequence
///       │   ├─ SelectTarget
///       │   ├─ CheckTargetValid
///       │   └─ Selector (체력에 따라)
///       │       ├─ Flee Sequence (체력 낮음)
///       │       │   ├─ CheckHealthLow
///       │       │   └─ Flee
///       │       └─ Combat Sequence (정상)
///       │           ├─ Selector (거리에 따라)
///       │           │   ├─ Attack Sequence (근거리)
///       │           │   │   ├─ CheckTargetInRange
///       │           │   │   └─ Attack
///       │           │   └─ ChaseTarget (원거리)
///       │           └─ ExitCombat (타겟 잃음)
///       └─ Wandering (평화 상태)
/// </summary>
public class AggressiveMonsterAI : BehaviorTree
{
    // ============================================================
    // NODES
    // ============================================================
    // Root
    private Sequence SeqRoot = new Sequence();

    // Death
    private BTNodeCheckDead NodeCheckDead;
    private BTNodeDeadProcess NodeDeadProcess;

    // Combat
    private Selector SelectorCombatOrPeace = new Selector();
    private Sequence SeqCombat = new Sequence();
    private BTNodeSelectTarget NodeSelectTarget;
    private BTNodeCheckTargetValid NodeCheckTargetValid;

    // Health-based behavior
    private Selector SelectorHealthBased = new Selector();

    // Flee
    private Sequence SeqFlee = new Sequence();
    private BTNodeCheckHealthLow NodeCheckHealthLow;
    private BTNodeFlee NodeFlee;

    // Normal Combat
    private Sequence SeqNormalCombat = new Sequence();
    private Selector SelectorDistanceBased = new Selector();

    // Attack
    private Sequence SeqAttack = new Sequence();
    private BTNodeCheckTargetInRange NodeCheckTargetInRange;
    private BTNodeAttack NodeAttack;

    // Chase
    private BTNodeChaseTarget NodeChaseTarget;

    // Exit Combat
    private BTNodeExitCombat NodeExitCombat;

    // Peace (Wandering)
    private BTNodeWandering NodeWandering;

    // ============================================================
    // INITIALIZATION
    // ============================================================
    public override void Initialize(ActorController actorController)
    {
        BlackBoardInstance = new BlackBoard();
        ActorControllerInstance = actorController;

        // BlackBoard 설정 (공격적인 몬스터)
        BlackBoardInstance.AttackRange = 3.0f;
        BlackBoardInstance.AttackCooldown = 2.0f;
        BlackBoardInstance.DetectionRange = 20.0f;
        BlackBoardInstance.FleeHealthThreshold = 0.3f; // 30% 이하 시 도주

        // PerceptionSystem 추가 (GameObject에 컴포넌트로 추가되어야 함)
        PerceptionSystem perception = actorController.gameObject.GetComponent<PerceptionSystem>();
        if (perception == null)
        {
            perception = actorController.gameObject.AddComponent<PerceptionSystem>();
            perception.SightRange = BlackBoardInstance.DetectionRange;
            perception.SightAngle = 120f; // 120도 시야각
            perception.HearingRange = 15f;
            perception.DetectableLayers = LayerMask.GetMask("Player", "NPC"); // 플레이어와 NPC 감지
            perception.ObstacleLayers = LayerMask.GetMask("Terrain"); // 지형에 가림
        }
        perception.Initialize(BlackBoardInstance);

        // ============================================================
        // BUILD BEHAVIOR TREE
        // ============================================================

        // Death Check (최우선)
        NodeCheckDead = new BTNodeCheckDead(this, actorController);
        NodeDeadProcess = new BTNodeDeadProcess(this, actorController);

        // Combat
        NodeSelectTarget = new BTNodeSelectTarget(this, actorController);
        NodeCheckTargetValid = new BTNodeCheckTargetValid(this, actorController);

        // Flee (체력 낮음)
        NodeCheckHealthLow = new BTNodeCheckHealthLow(this, actorController);
        NodeFlee = new BTNodeFlee(this, actorController);
        SeqFlee.AddChild(NodeFlee);
        SeqFlee.AddChild(NodeCheckHealthLow);

        // Attack (근거리)
        NodeCheckTargetInRange = new BTNodeCheckTargetInRange(this, actorController);
        NodeAttack = new BTNodeAttack(this, actorController);
        SeqAttack.AddChild(NodeAttack);
        SeqAttack.AddChild(NodeCheckTargetInRange);

        // Chase (원거리)
        NodeChaseTarget = new BTNodeChaseTarget(this, actorController);

        // Distance-based selector (공격 or 추적)
        SelectorDistanceBased.AddChild(NodeChaseTarget);
        SelectorDistanceBased.AddChild(SeqAttack);

        // Normal Combat Sequence
        SeqNormalCombat.AddChild(SelectorDistanceBased);

        // Health-based selector (도주 or 전투)
        SelectorHealthBased.AddChild(SeqNormalCombat);
        SelectorHealthBased.AddChild(SeqFlee);

        // Combat Sequence
        SeqCombat.AddChild(SelectorHealthBased);
        SeqCombat.AddChild(NodeCheckTargetValid);
        SeqCombat.AddChild(NodeSelectTarget);

        // Peace (Wandering)
        NodeWandering = new BTNodeWandering(this, actorController);

        // Combat or Peace Selector
        SelectorCombatOrPeace.AddChild(NodeWandering);
        SelectorCombatOrPeace.AddChild(SeqCombat);

        // Root Sequence
        RootNode.AddChild(SelectorCombatOrPeace);

        KojeomLogger.DebugLog($"[AI] AggressiveMonsterAI initialized for {actorController.name}");
    }
}

/// <summary>
/// 방어적인 몬스터 AI (Defensive Monster)
///
/// 공격받을 때만 반격하는 몬스터
/// 먼저 공격하지 않음
/// </summary>
public class DefensiveMonsterAI : BehaviorTree
{
    private Selector SelectorMain = new Selector();
    private Sequence SeqCombat = new Sequence();
    private Sequence SeqAttack = new Sequence();

    private BTNodeCheckDead NodeCheckDead;
    private BTNodeDeadProcess NodeDeadProcess;
    private BTNodeSelectTarget NodeSelectTarget;
    private BTNodeCheckTargetValid NodeCheckTargetValid;
    private BTNodeCheckTargetInRange NodeCheckTargetInRange;
    private BTNodeAttack NodeAttack;
    private BTNodeChaseTarget NodeChaseTarget;
    private BTNodeWandering NodeWandering;

    public override void Initialize(ActorController actorController)
    {
        BlackBoardInstance = new BlackBoard();
        ActorControllerInstance = actorController;

        // 방어적 설정
        BlackBoardInstance.CurrentCombatMode = CombatMode.Defensive;
        BlackBoardInstance.AttackRange = 3.0f;
        BlackBoardInstance.DetectionRange = 15.0f; // 공격적 몬스터보다 짧음

        // Perception System
        PerceptionSystem perception = actorController.gameObject.GetComponent<PerceptionSystem>();
        if (perception == null)
        {
            perception = actorController.gameObject.AddComponent<PerceptionSystem>();
            perception.SightRange = BlackBoardInstance.DetectionRange;
            perception.SightAngle = 90f; // 좁은 시야각
            perception.HearingRange = 10f;
        }
        perception.Initialize(BlackBoardInstance);

        // Build Tree
        NodeCheckDead = new BTNodeCheckDead(this, actorController);
        NodeDeadProcess = new BTNodeDeadProcess(this, actorController);
        NodeSelectTarget = new BTNodeSelectTarget(this, actorController);
        NodeCheckTargetValid = new BTNodeCheckTargetValid(this, actorController);
        NodeCheckTargetInRange = new BTNodeCheckTargetInRange(this, actorController);
        NodeAttack = new BTNodeAttack(this, actorController);
        NodeChaseTarget = new BTNodeChaseTarget(this, actorController);
        NodeWandering = new BTNodeWandering(this, actorController);

        // Combat (어그로가 있을 때만)
        SeqAttack.AddChild(NodeAttack);
        SeqAttack.AddChild(NodeCheckTargetInRange);

        SeqCombat.AddChild(NodeChaseTarget); // 기본 추적
        SeqCombat.AddChild(SeqAttack); // 범위 내 공격
        SeqCombat.AddChild(NodeCheckTargetValid);
        SeqCombat.AddChild(NodeSelectTarget);

        // Main Selector
        SelectorMain.AddChild(NodeWandering); // 평화 상태 (우선순위 낮음)
        SelectorMain.AddChild(SeqCombat); // 전투 (어그로 있을 때만)

        RootNode.AddChild(SelectorMain);

        KojeomLogger.DebugLog($"[AI] DefensiveMonsterAI initialized for {actorController.name}");
    }
}

/// <summary>
/// 비겁한 몬스터 AI (Coward Monster)
///
/// 플레이어를 발견하면 도망감
/// 공격하지 않고 항상 도주
/// </summary>
public class CowardMonsterAI : BehaviorTree
{
    private Selector SelectorMain = new Selector();
    private Sequence SeqFlee = new Sequence();

    private BTNodeCheckDead NodeCheckDead;
    private BTNodeDeadProcess NodeDeadProcess;
    private BTNodeSelectTarget NodeSelectTarget;
    private BTNodeFlee NodeFlee;
    private BTNodeWandering NodeWandering;

    public override void Initialize(ActorController actorController)
    {
        BlackBoardInstance = new BlackBoard();
        ActorControllerInstance = actorController;

        // 비겁한 설정
        BlackBoardInstance.CurrentCombatMode = CombatMode.Fleeing;
        BlackBoardInstance.DetectionRange = 25.0f; // 멀리서 감지
        BlackBoardInstance.FleeHealthThreshold = 1.0f; // 항상 도주

        // Perception System
        PerceptionSystem perception = actorController.gameObject.GetComponent<PerceptionSystem>();
        if (perception == null)
        {
            perception = actorController.gameObject.AddComponent<PerceptionSystem>();
            perception.SightRange = BlackBoardInstance.DetectionRange;
            perception.SightAngle = 180f; // 넓은 시야각 (경계심 많음)
            perception.HearingRange = 20f;
        }
        perception.Initialize(BlackBoardInstance);

        // Build Tree
        NodeCheckDead = new BTNodeCheckDead(this, actorController);
        NodeDeadProcess = new BTNodeDeadProcess(this, actorController);
        NodeSelectTarget = new BTNodeSelectTarget(this, actorController);
        NodeFlee = new BTNodeFlee(this, actorController);
        NodeWandering = new BTNodeWandering(this, actorController);

        // Flee (타겟 발견 시)
        SeqFlee.AddChild(NodeFlee);
        SeqFlee.AddChild(NodeSelectTarget);

        // Main Selector
        SelectorMain.AddChild(NodeWandering); // 평화 상태
        SelectorMain.AddChild(SeqFlee); // 타겟 발견 시 도주

        RootNode.AddChild(SelectorMain);

        KojeomLogger.DebugLog($"[AI] CowardMonsterAI initialized for {actorController.name}");
    }
}
