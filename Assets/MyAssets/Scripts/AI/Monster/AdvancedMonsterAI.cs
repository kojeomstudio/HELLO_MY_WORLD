using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss Monster AI
///
/// 특징:
/// - 높은 체력과 공격력
/// - 도주하지 않음 (체력 낮아도 계속 싸움)
/// - 넓은 감지 범위 (30m)
/// - 특수 공격 패턴 (일정 간격으로 특수 스킬)
/// - 넓은 시야각 (180°)
/// </summary>
public class BossMonsterAI : BehaviorTree
{
    // ============================================================
    // NODES
    // ============================================================
    private Selector SelectorMain = new Selector();
    private Sequence SeqCombat = new Sequence();
    private Selector SelectorAttackType = new Selector();
    private Sequence SeqSpecialAttack = new Sequence();
    private Sequence SeqNormalAttack = new Sequence();

    private BTNodeCheckDead NodeCheckDead;
    private BTNodeDeadProcess NodeDeadProcess;
    private BTNodeSelectTarget NodeSelectTarget;
    private BTNodeCheckTargetValid NodeCheckTargetValid;
    private BTNodeCheckTargetInRange NodeCheckTargetInRange;
    private BTNodeAttack NodeAttack;
    private BTNodeChaseTarget NodeChaseTarget;
    private BTNodeWandering NodeWandering;
    private BTNodeTimer NodeSpecialAttackTimer;

    public override void Initialize(ActorController actorController)
    {
        BlackBoardInstance = new BlackBoard();
        ActorControllerInstance = actorController;

        // Boss 설정
        BlackBoardInstance.AttackRange = 5.0f; // 넓은 공격 범위
        BlackBoardInstance.AttackCooldown = 1.5f; // 빠른 공격
        BlackBoardInstance.DetectionRange = 30.0f; // 넓은 감지 범위
        BlackBoardInstance.FleeHealthThreshold = 0.0f; // 도주하지 않음

        // Perception System
        PerceptionSystem perception = actorController.gameObject.AddComponent<PerceptionSystem>();
        perception.SightRange = 30f;
        perception.SightAngle = 180f; // 넓은 시야
        perception.HearingRange = 25f;
        perception.DetectableLayers = LayerMask.GetMask("Player", "NPC");
        perception.ObstacleLayers = LayerMask.GetMask("Terrain");
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

        // Special Attack Timer (10초마다)
        NodeSpecialAttackTimer = new BTNodeTimer(this, actorController);
        NodeSpecialAttackTimer.SetWakeupTime(10f);
        NodeSpecialAttackTimer.SetCallbackAfterTimer(() => {
            KojeomLogger.DebugLog($"[Boss AI] {actorController.name} uses special attack!");
            // TODO: 특수 공격 로직
        });

        // Attack selector
        SeqNormalAttack.AddChild(NodeAttack);
        SeqNormalAttack.AddChild(NodeCheckTargetInRange);

        SelectorAttackType.AddChild(NodeChaseTarget);
        SelectorAttackType.AddChild(SeqNormalAttack);

        // Combat sequence
        SeqCombat.AddChild(SelectorAttackType);
        SeqCombat.AddChild(NodeCheckTargetValid);
        SeqCombat.AddChild(NodeSelectTarget);

        // Main selector
        SelectorMain.AddChild(NodeWandering);
        SelectorMain.AddChild(SeqCombat);
        SelectorMain.AddChild(NodeSpecialAttackTimer);

        RootNode.AddChild(SelectorMain);

        KojeomLogger.DebugLog($"[AI] BossMonsterAI initialized for {actorController.name}");
    }
}

/// <summary>
/// Flying Monster AI (비행 몬스터)
///
/// 특징:
/// - 공중 이동 (중력 무시)
/// - 원거리 공격 선호
/// - 높은 이동 속도
/// - 플레이어 위에서 공격
/// </summary>
public class FlyingMonsterAI : BehaviorTree
{
    private Selector SelectorMain = new Selector();
    private Sequence SeqCombat = new Sequence();
    private Selector SelectorAttackOrMove = new Selector();
    private Sequence SeqRangedAttack = new Sequence();

    private BTNodeCheckDead NodeCheckDead;
    private BTNodeDeadProcess NodeDeadProcess;
    private BTNodeSelectTarget NodeSelectTarget;
    private BTNodeCheckTargetValid NodeCheckTargetValid;
    private BTNodeAttack NodeAttack; // 원거리 공격
    private BTNodeChaseTarget NodeChaseTarget;
    private BTNodeWandering NodeWandering;
    private BTNodeCheckHealthLow NodeCheckHealthLow;
    private BTNodeFlee NodeFlee;

    public override void Initialize(ActorController actorController)
    {
        BlackBoardInstance = new BlackBoard();
        ActorControllerInstance = actorController;

        // Flying 설정
        BlackBoardInstance.AttackRange = 10.0f; // 원거리 공격
        BlackBoardInstance.AttackCooldown = 2.5f;
        BlackBoardInstance.DetectionRange = 25.0f;
        BlackBoardInstance.FleeHealthThreshold = 0.4f; // 40% 이하 시 도주

        // Perception System (공중에서 더 잘 보임)
        PerceptionSystem perception = actorController.gameObject.AddComponent<PerceptionSystem>();
        perception.SightRange = 25f;
        perception.SightAngle = 150f;
        perception.HearingRange = 20f;
        perception.Initialize(BlackBoardInstance);

        // 중력 비활성화 (비행)
        Rigidbody rb = actorController.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
        }

        // Build Tree
        NodeCheckDead = new BTNodeCheckDead(this, actorController);
        NodeDeadProcess = new BTNodeDeadProcess(this, actorController);
        NodeSelectTarget = new BTNodeSelectTarget(this, actorController);
        NodeCheckTargetValid = new BTNodeCheckTargetValid(this, actorController);
        NodeAttack = new BTNodeAttack(this, actorController);
        NodeChaseTarget = new BTNodeChaseTarget(this, actorController);
        NodeWandering = new BTNodeWandering(this, actorController);
        NodeCheckHealthLow = new BTNodeCheckHealthLow(this, actorController);
        NodeFlee = new BTNodeFlee(this, actorController);

        // Ranged attack (원거리)
        SeqRangedAttack.AddChild(NodeAttack);

        // Attack or move
        SelectorAttackOrMove.AddChild(NodeChaseTarget);
        SelectorAttackOrMove.AddChild(SeqRangedAttack);

        // Combat
        SeqCombat.AddChild(SelectorAttackOrMove);
        SeqCombat.AddChild(NodeCheckTargetValid);
        SeqCombat.AddChild(NodeSelectTarget);

        // Flee if low health
        Sequence SeqFlee = new Sequence();
        SeqFlee.AddChild(NodeFlee);
        SeqFlee.AddChild(NodeCheckHealthLow);

        // Main selector
        SelectorMain.AddChild(NodeWandering);
        SelectorMain.AddChild(SeqFlee);
        SelectorMain.AddChild(SeqCombat);

        RootNode.AddChild(SelectorMain);

        KojeomLogger.DebugLog($"[AI] FlyingMonsterAI initialized for {actorController.name}");
    }
}

/// <summary>
/// Ranged Monster AI (원거리 공격 몬스터)
///
/// 특징:
/// - 원거리 공격 선호 (활, 마법 등)
/// - 플레이어와 거리 유지
/// - 도망가면서 공격
/// - 중거리 전투 (10-15m)
/// </summary>
public class RangedMonsterAI : BehaviorTree
{
    private Selector SelectorMain = new Selector();
    private Sequence SeqCombat = new Sequence();
    private Selector SelectorDistanceBased = new Selector();
    private Sequence SeqMaintainDistance = new Sequence();
    private Sequence SeqRangedAttack = new Sequence();

    private BTNodeCheckDead NodeCheckDead;
    private BTNodeDeadProcess NodeDeadProcess;
    private BTNodeSelectTarget NodeSelectTarget;
    private BTNodeCheckTargetValid NodeCheckTargetValid;
    private BTNodeAttack NodeAttack;
    private BTNodeFlee NodeFlee; // 거리 유지용
    private BTNodeChaseTarget NodeChaseTarget;
    private BTNodeWandering NodeWandering;
    private BTNodeCheckHealthLow NodeCheckHealthLow;

    public override void Initialize(ActorController actorController)
    {
        BlackBoardInstance = new BlackBoard();
        ActorControllerInstance = actorController;

        // Ranged 설정
        BlackBoardInstance.AttackRange = 15.0f; // 원거리 공격 범위
        BlackBoardInstance.AttackCooldown = 2.0f;
        BlackBoardInstance.DetectionRange = 20.0f;
        BlackBoardInstance.FleeHealthThreshold = 0.3f;

        // Perception System
        PerceptionSystem perception = actorController.gameObject.AddComponent<PerceptionSystem>();
        perception.SightRange = 20f;
        perception.SightAngle = 100f;
        perception.HearingRange = 15f;
        perception.Initialize(BlackBoardInstance);

        // Build Tree
        NodeCheckDead = new BTNodeCheckDead(this, actorController);
        NodeDeadProcess = new BTNodeDeadProcess(this, actorController);
        NodeSelectTarget = new BTNodeSelectTarget(this, actorController);
        NodeCheckTargetValid = new BTNodeCheckTargetValid(this, actorController);
        NodeAttack = new BTNodeAttack(this, actorController);
        NodeFlee = new BTNodeFlee(this, actorController);
        NodeChaseTarget = new BTNodeChaseTarget(this, actorController);
        NodeWandering = new BTNodeWandering(this, actorController);
        NodeCheckHealthLow = new BTNodeCheckHealthLow(this, actorController);

        // Ranged attack (원거리에서 공격)
        SeqRangedAttack.AddChild(NodeAttack);

        // Maintain distance (너무 가까우면 도망)
        BTNodeCheckTooClose checkTooClose = new BTNodeCheckTooClose(this, actorController, 5.0f);
        SeqMaintainDistance.AddChild(NodeFlee);
        SeqMaintainDistance.AddChild(checkTooClose);

        // Distance-based behavior
        SelectorDistanceBased.AddChild(NodeChaseTarget); // 너무 멀면 추적
        SelectorDistanceBased.AddChild(SeqRangedAttack); // 적당한 거리에서 공격
        SelectorDistanceBased.AddChild(SeqMaintainDistance); // 너무 가까우면 후퇴

        // Combat
        SeqCombat.AddChild(SelectorDistanceBased);
        SeqCombat.AddChild(NodeCheckTargetValid);
        SeqCombat.AddChild(NodeSelectTarget);

        // Flee if low health
        Sequence SeqFlee = new Sequence();
        SeqFlee.AddChild(NodeFlee);
        SeqFlee.AddChild(NodeCheckHealthLow);

        // Main selector
        SelectorMain.AddChild(NodeWandering);
        SelectorMain.AddChild(SeqFlee);
        SelectorMain.AddChild(SeqCombat);

        RootNode.AddChild(SelectorMain);

        KojeomLogger.DebugLog($"[AI] RangedMonsterAI initialized for {actorController.name}");
    }
}

/// <summary>
/// 거리 체크 노드 (너무 가까운지 확인)
/// </summary>
public class BTNodeCheckTooClose : Node
{
    private float _minDistance;

    public BTNodeCheckTooClose(BehaviorTree behaviorTreeInstance, ActorController actorController, float minDistance)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
        _minDistance = minDistance;
    }

    public override bool Invoke(float DeltaTime)
    {
        BlackBoard bb = BehaviorTreeInstance.GetBlackBoard();

        if (bb.CurrentTarget == null)
            return false;

        float distance = Vector3.Distance(
            Controller.GetPosition(),
            bb.CurrentTarget.transform.position
        );

        return distance < _minDistance;
    }
}
