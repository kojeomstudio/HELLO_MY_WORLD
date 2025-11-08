using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Actor와 관련된 행동트리 노드들이 정의되어 있는 파일. 
 */

public class BTNodeMoveForTarget : Node
{
    public CustomAstar3D PathFinderInstance { get; private set; } = new CustomAstar3D();
    //
    private float ReCalcPathfindingTimeSec = 5.0f;
    public BTNodeMoveForTarget(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
    }
   
    public override bool Invoke(float DeltaTime)
    {
        ElapsedTimeSec += DeltaTime;
        bool bRecalcPathFinding = BehaviorTreeInstance.GetBlackBoard().PathList.Count == 0 && ElapsedTimeSec >= ReCalcPathfindingTimeSec;
        if (bRecalcPathFinding == true)
        {
            ElapsedTimeSec = 0.0f;
            AsyncPathFinding(BehaviorTreeInstance.GetBlackBoard().PathFidningTargetPoint);
        }

        if (BehaviorTreeInstance.GetBlackBoard().PathList.Count > 0)
        {
            // test code.
            PathNode3D node = BehaviorTreeInstance.GetBlackBoard().PathList.Pop();
            Controller.StartRun(node.GetWorldPosition());
        }
        return true;
    }
    public void AsyncPathFinding(Vector3 goalWorldPosition)
    {
        // 예외처리.
        switch (Controller.GetContainedWorldState())
        {
            case SubWorldRealTimeStatus.Loading:
            case SubWorldRealTimeStatus.Release:
            case SubWorldRealTimeStatus.ReleaseFinish:
                return;
        }
        // init
        PathFinderSettings needData = new PathFinderSettings(Controller.GetContainedWorldBlockData(),
                                                             Controller.GetContainedSubWorldOffset(),
                                                             Controller.GetContainedWorldAreaOffset());
        PathFinderInstance.Init(needData, new SimpleVector3(Controller.GetActorTransform().position));
        PathFinderInstance.OnFinishAsyncPathFinding += OnFinishAsyncPathFinding;
        // async start.
        PathFinderInstance.AsyncPathFinding(goalWorldPosition);
    }
    private void OnFinishAsyncPathFinding(Stack<PathNode3D> resultPath)
    {
        BehaviorTreeInstance.GetBlackBoard().PathList = resultPath;
    }
}

// 이곳저곳 배회하는 노드.
public class BTNodeWandering : Node
{
    private readonly float WakeupTimeSec = 3.0f;
    public BTNodeWandering(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
    }
    public override bool Invoke(float DeltaTime)
    {
        ElapsedTimeSec += DeltaTime;
        if(ElapsedTimeSec >= WakeupTimeSec)
        {
            Vector3 targetPos;
            if(AIUtils.GetRandomWorldPositionFromActorPos(out targetPos, Controller) == true)
            {
                if(Controller.GetCurrentState() != ActorStateType.Run)
                {
                    //KojeomLogger.DebugLog(string.Format("Wandering start!"));
                    Controller.StartRun(targetPos);
                }
            }
            ElapsedTimeSec = 0.0f;
        }
        return true;
    }

}

public class BTNodeTimer : Node
{
    private float WakeupTimeSec = 2.0f;
    public delegate void OnAfterTimer();
    private OnAfterTimer CallBack;

    public BTNodeTimer(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
    }
    public override bool Invoke(float DeltaTime)
    {
        if(ElapsedTimeSec <= WakeupTimeSec)
        {
            ElapsedTimeSec += DeltaTime;
        }
        else
        {
            ElapsedTimeSec = 0.0f;
            CallBack();
        }
        return true;
    }

    public void SetCallbackAfterTimer(OnAfterTimer callback)
    {
        CallBack = callback;
    }

    public void SetWakeupTime(float sec)
    {
        WakeupTimeSec = sec;
    }
}

// ============================================================
// COMBAT NODES (전투 관련 노드)
// ============================================================

/// <summary>
/// 타겟 선택 노드
/// BlackBoard의 어그로 리스트에서 가장 위협적인 타겟 선택
/// </summary>
public class BTNodeSelectTarget : Node
{
    public BTNodeSelectTarget(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
    }

    public override bool Invoke(float DeltaTime)
    {
        BlackBoard bb = BehaviorTreeInstance.GetBlackBoard();

        // 1. 어그로 리스트에서 가장 높은 타겟 선택
        GameObject highestAggroTarget = bb.GetHighestAggroTarget();
        if (highestAggroTarget != null)
        {
            bb.CurrentTarget = highestAggroTarget;
            bb.CurrentCombatMode = CombatMode.Aggressive;
            return true;
        }

        // 2. 어그로가 없으면 가장 위협적인 엔티티 선택
        GameObject mostThreatening = bb.MostThreateningEntity;
        if (mostThreatening != null)
        {
            bb.CurrentTarget = mostThreatening;
            bb.CurrentCombatMode = CombatMode.Defensive;
            return true;
        }

        // 3. 타겟이 없으면 실패
        bb.CurrentTarget = null;
        bb.CurrentCombatMode = CombatMode.Passive;
        return false;
    }
}

/// <summary>
/// 타겟이 공격 범위 내에 있는지 체크
/// </summary>
public class BTNodeCheckTargetInRange : Node
{
    public BTNodeCheckTargetInRange(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
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

        return distance <= bb.AttackRange;
    }
}

/// <summary>
/// 공격 실행 노드
/// 타겟을 공격하고 쿨다운 적용
/// </summary>
public class BTNodeAttack : Node
{
    private float _attackAnimationDuration = 1.0f;

    public BTNodeAttack(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
    }

    public override bool Invoke(float DeltaTime)
    {
        BlackBoard bb = BehaviorTreeInstance.GetBlackBoard();

        // 타겟이 없으면 실패
        if (bb.CurrentTarget == null)
            return false;

        // 쿨다운 체크
        if (!bb.CanAttack)
            return false;

        // 스턴 상태면 공격 불가
        if (bb.IsStunned)
            return false;

        // 공격 실행
        PerformAttack(bb.CurrentTarget);

        // 쿨다운 설정
        bb.LastAttackTime = Time.time;

        // 공격 애니메이션 재생
        PlayAttackAnimation();

        KojeomLogger.DebugLog($"[AI] {Controller.name} attacks {bb.CurrentTarget.name}");

        return true;
    }

    private void PlayAttackAnimation()
    {
        // ActorAnimationController 사용
        ActorAnimationController animController = Controller.GetComponent<ActorAnimationController>();
        if (animController != null)
        {
            animController.PlayAnimation(ActorAnimationType.MeleeAttack);
        }
        else
        {
            // Fallback: 기존 방식
            Controller.PlayAnimation("Attack");
        }
    }

    private void PerformAttack(GameObject target)
    {
        // 타겟의 Actor 컴포넌트 가져오기
        ActorController targetController = target.GetComponent<ActorController>();
        if (targetController != null)
        {
            Actor targetActor = targetController.GetActorInstance();
            Actor attackerActor = Controller.GetActorInstance();

            if (targetActor != null && attackerActor != null)
            {
                // 데미지 계산 (공격자의 AttackPoint 사용)
                int damage = CalculateDamage(attackerActor, targetActor);

                // 타겟에게 데미지 적용
                targetActor.TakeDamage(damage, attackerActor);

                KojeomLogger.DebugLog($"[Combat] {attackerActor.name} dealt {damage} damage to {targetActor.name}");

                // TODO: 서버로 공격 메시지 전송 (향후 구현)
                // NetworkManager.SendAttackMessage(attackerActor.GetNetID(), targetActor.GetNetID(), damage);
            }
            else
            {
                KojeomLogger.DebugLog($"[Combat] Failed to get Actor components for combat");
            }
        }
    }

    /// <summary>
    /// 데미지 계산 (공격력, 방어력 등 고려)
    /// </summary>
    private int CalculateDamage(Actor attacker, Actor target)
    {
        // 기본 데미지 = 공격자의 공격력
        int baseDamage = attacker.AttackPoint;

        // 랜덤 변동 (±20%)
        float randomFactor = Random.Range(0.8f, 1.2f);
        int finalDamage = Mathf.RoundToInt(baseDamage * randomFactor);

        // 최소 데미지 보장
        finalDamage = Mathf.Max(1, finalDamage);

        return finalDamage;
    }
}

/// <summary>
/// 타겟 추적 노드
/// 타겟을 향해 이동
/// </summary>
public class BTNodeChaseTarget : Node
{
    public BTNodeChaseTarget(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
    }

    public override bool Invoke(float DeltaTime)
    {
        BlackBoard bb = BehaviorTreeInstance.GetBlackBoard();

        if (bb.CurrentTarget == null)
            return false;

        // 타겟 위치로 이동
        Vector3 targetPos = bb.CurrentTarget.transform.position;
        bb.PathFidningTargetPoint = targetPos;
        bb.IsChasing = true;
        bb.IsWandering = false;

        // 이동 시작
        if (Controller.GetCurrentState() != ActorStateType.Run)
        {
            Controller.StartRun(targetPos);
        }

        return true;
    }
}

/// <summary>
/// 도주 노드
/// 타겟으로부터 반대 방향으로 도망
/// </summary>
public class BTNodeFlee : Node
{
    private float _fleeDistance = 15.0f;

    public BTNodeFlee(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
    }

    public override bool Invoke(float DeltaTime)
    {
        BlackBoard bb = BehaviorTreeInstance.GetBlackBoard();

        // 도주 모드 설정
        bb.CurrentCombatMode = CombatMode.Fleeing;
        bb.IsFleeing = true;
        bb.IsChasing = false;

        // 위협으로부터 반대 방향 계산
        Vector3 fleeDirection = Vector3.zero;

        if (bb.CurrentTarget != null)
        {
            // 타겟의 반대 방향
            fleeDirection = (Controller.GetPosition() - bb.CurrentTarget.transform.position).normalized;
        }
        else if (bb.Memory.LastAttacker != null)
        {
            // 마지막 공격자의 반대 방향
            fleeDirection = (Controller.GetPosition() - bb.Memory.LastAttacker.transform.position).normalized;
        }
        else
        {
            // 랜덤 방향
            fleeDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        }

        // 도주 목표 위치 계산
        Vector3 fleeTarget = Controller.GetPosition() + fleeDirection * _fleeDistance;
        bb.PathFidningTargetPoint = fleeTarget;

        // 도주 시작
        Controller.StartRun(fleeTarget);

        KojeomLogger.DebugLog($"[AI] {Controller.name} is fleeing!");

        return true;
    }
}

/// <summary>
/// 체력 체크 노드
/// 체력이 임계값 이하인지 확인
/// </summary>
public class BTNodeCheckHealthLow : Node
{
    public BTNodeCheckHealthLow(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
    }

    public override bool Invoke(float DeltaTime)
    {
        BlackBoard bb = BehaviorTreeInstance.GetBlackBoard();
        return bb.HealthRatio <= bb.FleeHealthThreshold;
    }
}

/// <summary>
/// 타겟 유효성 체크
/// 타겟이 여전히 유효한지 확인 (null, 너무 멀리 등)
/// </summary>
public class BTNodeCheckTargetValid : Node
{
    private float _maxChaseDistance = 50.0f;

    public BTNodeCheckTargetValid(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
    }

    public override bool Invoke(float DeltaTime)
    {
        BlackBoard bb = BehaviorTreeInstance.GetBlackBoard();

        if (bb.CurrentTarget == null)
            return false;

        // 타겟이 파괴되었는지 확인
        if (!bb.CurrentTarget.activeInHierarchy)
        {
            bb.CurrentTarget = null;
            return false;
        }

        // 타겟이 너무 멀어졌는지 확인
        float distance = Vector3.Distance(Controller.GetPosition(), bb.CurrentTarget.transform.position);
        if (distance > _maxChaseDistance)
        {
            KojeomLogger.DebugLog($"[AI] Target too far ({distance:F1}m), giving up");
            bb.CurrentTarget = null;
            bb.AggroList.Clear();
            return false;
        }

        return true;
    }
}

/// <summary>
/// 전투 종료 노드
/// 전투 상태를 초기화
/// </summary>
public class BTNodeExitCombat : Node
{
    public BTNodeExitCombat(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
    }

    public override bool Invoke(float DeltaTime)
    {
        BlackBoard bb = BehaviorTreeInstance.GetBlackBoard();

        bb.CurrentTarget = null;
        bb.CurrentCombatMode = CombatMode.Passive;
        bb.IsChasing = false;
        bb.IsFleeing = false;
        bb.AggroList.Clear();

        // Idle 상태로 전환
        Controller.StartIdle();

        KojeomLogger.DebugLog($"[AI] {Controller.name} exited combat");

        return true;
    }
}

// ============================================================
// LEGACY NODES (기존 노드 유지 - 하위 호환성)
// ============================================================

public class BTNodeStartAttack : Node
{
    public BTNodeStartAttack(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
    }
    public override bool Invoke(float DeltaTime)
    {
        // Legacy - BTNodeAttack 사용 권장
        return true;
    }
}

public class BTNodeStopAttack : Node
{
    public BTNodeStopAttack(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
    }
    public override bool Invoke(float DeltaTime)
    {
        // Legacy - BTNodeExitCombat 사용 권장
        return true;
    }
}

public class BTNodeDeadProcess : Node
{
    public BTNodeDeadProcess(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
    }
    public override bool Invoke(float DeltaTime)
    {
        BlackBoard bb = BehaviorTreeInstance.GetBlackBoard();

        // 사망 처리
        if (bb.HealthRatio <= 0f)
        {
            // BT 중지
            BehaviorTreeInstance.StopBT();

            // TODO: 사망 애니메이션, 아이템 드롭, 경험치 등
            KojeomLogger.DebugLog($"[AI] {Controller.name} is dead");

            return true;
        }

        return false;
    }
}

public class BTNodeCheckDead : Node
{
    public BTNodeCheckDead(BehaviorTree behaviorTreeInstance, ActorController actorController)
    {
        Controller = actorController;
        BehaviorTreeInstance = behaviorTreeInstance;
    }
    public override bool Invoke(float DeltaTime)
    {
        BlackBoard bb = BehaviorTreeInstance.GetBlackBoard();
        return bb.HealthRatio <= 0f;
    }
}
