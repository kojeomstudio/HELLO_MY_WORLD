# AI System Implementation Guide

## Overview

HELLO_MY_WORLD 프로젝트의 AI 시스템은 **Behavior Tree + State Machine** 하이브리드 아키텍처를 사용합니다.

### 핵심 구성 요소

1. **Enhanced BlackBoard** - AI 의사결정을 위한 통합 데이터 저장소
2. **Perception System** - 시야/청각 기반 엔티티 감지
3. **Combat BT Nodes** - 전투 관련 행동 트리 노드
4. **Monster AI** - 3가지 타입의 샘플 몬스터 AI
5. **AI LOD Manager** - 거리 기반 성능 최적화

---

## 1. Enhanced BlackBoard

### 기능

BlackBoard는 AI가 의사결정에 필요한 모든 정보를 저장하는 중앙 저장소입니다.

#### 주요 데이터 카테고리:

```csharp
// Navigation (경로 탐색)
Stack<PathNode3D> PathList
Vector3 PathFidningTargetPoint
bool IsPathFindingActive

// Perception (인지)
Dictionary<GameObject, PerceivedEntity> PerceivedEntities
List<GameObject> VisibleEntities
GameObject MostThreateningEntity

// Combat (전투)
CombatMode CurrentCombatMode  // Passive, Defensive, Aggressive, Fleeing
GameObject CurrentTarget
Dictionary<GameObject, float> AggroList
float AttackRange, AttackCooldown, DetectionRange

// Memory (기억)
AIMemory Memory  // LastAttacker, LastDamagedPosition, PatrolPoints

// Status (상태)
float HealthRatio, StaminaRatio
List<string> ActiveBuffs, ActiveDebuffs
bool IsStunned
```

### 사용 예시

```csharp
BlackBoard bb = BehaviorTreeInstance.GetBlackBoard();

// 어그로 추가
bb.AddAggro(playerObject, 50f);

// 가장 위협적인 타겟 가져오기
GameObject threat = bb.MostThreateningEntity;

// 전투 상태 확인
if (bb.IsInCombat && bb.CanAttack)
{
    // 공격 실행
}

// 체력 비율 확인
if (bb.HealthRatio < 0.3f)
{
    // 도주
}
```

---

## 2. Perception System

### 개요

PerceptionSystem은 MonoBehaviour 컴포넌트로, AI가 주변 환경을 인지하는 시스템입니다.

### 기능

- **시야(Sight)**: FOV 기반 엔티티 감지, 레이캐스트로 장애물 가림 처리
- **청각(Hearing)**: 소리 기반 감지
- **자동 BlackBoard 연동**: 감지된 정보 자동 저장

### Inspector 설정

```
Sight Settings:
├─ Sight Range: 20.0f (시야 거리)
├─ Sight Angle: 120.0f (시야각, 도)
├─ Sight Update Interval: 0.2f (업데이트 주기, 초)
├─ Detectable Layers: Player, Monster, NPC (감지할 레이어)
└─ Obstacle Layers: Terrain (장애물 레이어)

Hearing Settings:
├─ Hearing Range: 15.0f (청각 범위)
└─ Hearing Update Interval: 0.5f (업데이트 주기)

Debug:
└─ Show Debug Gizmos: true (Gizmo 표시)
```

### 사용 방법

```csharp
// 1. AI GameObject에 PerceptionSystem 컴포넌트 추가
PerceptionSystem perception = actorController.gameObject.AddComponent<PerceptionSystem>();

// 2. 설정
perception.SightRange = 20f;
perception.SightAngle = 120f;
perception.DetectableLayers = LayerMask.GetMask("Player", "NPC");
perception.ObstacleLayers = LayerMask.GetMask("Terrain");

// 3. BlackBoard 연결
perception.Initialize(blackBoard);

// 4. 데미지 이벤트 연동 (외부 호출)
perception.OnDamageReceived(attackerObject, damagePosition, damageAmount);

// 5. 소리 이벤트 연동 (외부 호출)
perception.OnSoundHeard(soundPosition, soundIntensity);
```

### Debug Gizmos

- **녹색 반투명 원**: 시야 범위
- **파란색 반투명 원**: 청각 범위
- **노란색 선**: 시야각 경계
- **빨간색 선**: 감지된 엔티티로의 연결선
- **파란색 구**: 마지막으로 들은 소리 위치

---

## 3. Combat BT Nodes

### 새로운 노드 목록

| 노드 | 설명 | 반환값 |
|------|------|--------|
| `BTNodeSelectTarget` | 어그로 리스트에서 타겟 선택 | true: 타겟 선택됨<br>false: 타겟 없음 |
| `BTNodeCheckTargetInRange` | 타겟이 공격 범위 내인지 확인 | true: 범위 내<br>false: 범위 밖 |
| `BTNodeAttack` | 타겟 공격 실행 | true: 공격 성공<br>false: 실패 |
| `BTNodeChaseTarget` | 타겟 추적 | true: 추적 시작<br>false: 타겟 없음 |
| `BTNodeFlee` | 도주 실행 | true: 도주 시작 |
| `BTNodeCheckHealthLow` | 체력 낮은지 확인 | true: 체력 낮음<br>false: 정상 |
| `BTNodeCheckTargetValid` | 타겟 유효성 확인 | true: 유효<br>false: 무효 |
| `BTNodeExitCombat` | 전투 종료 | true: 종료됨 |
| `BTNodeCheckDead` | 사망 확인 | true: 사망<br>false: 생존 |
| `BTNodeDeadProcess` | 사망 처리 | true: 처리 완료 |

### 전투 BT 구조 예시

```
Root (Sequence)
├─ CheckDead → DeadProcess (사망 체크)
└─ Selector
    ├─ Combat Sequence (전투)
    │   ├─ SelectTarget
    │   ├─ CheckTargetValid
    │   └─ Selector
    │       ├─ Flee (체력 낮음)
    │       └─ Attack or Chase (정상)
    └─ Wandering (평화 상태)
```

---

## 4. Monster AI 구현

### AggressiveMonsterAI (공격적 몬스터)

**특징**:
- 플레이어를 발견하면 즉시 공격
- 체력 30% 이하 시 도주
- 감지 범위: 20m, 시야각: 120°

**사용법**:
```csharp
public override void Initialize(ActorController actorController)
{
    AIGroup[(int)AITypes.Common] = new AggressiveMonsterAI();
    AIGroup[(int)AITypes.Common].Initialize(actorController);
    AIGroup[(int)AITypes.Common].StartBT();
}
```

### DefensiveMonsterAI (방어적 몬스터)

**특징**:
- 공격받을 때만 반격
- 먼저 공격하지 않음
- 감지 범위: 15m, 시야각: 90°

### CowardMonsterAI (비겁한 몬스터)

**특징**:
- 플레이어를 발견하면 즉시 도망
- 공격하지 않음
- 감지 범위: 25m, 시야각: 180° (경계심 많음)

---

## 5. AI LOD Manager

### 개요

거리에 따라 AI 업데이트 빈도를 자동 조절하여 성능을 최적화하는 시스템입니다.

### LOD 레벨

| 레벨 | 거리 (기본값) | 업데이트 빈도 | FPS |
|------|---------------|---------------|-----|
| **FullSpeed** | 0-15m | 매 프레임 | 60 |
| **High** | 15-25m | 2프레임마다 | 30 |
| **Medium** | 25-50m | 3프레임마다 | 20 |
| **Low** | 50-75m | 6프레임마다 | 10 |
| **VeryLow** | 75-100m | 12프레임마다 | 5 |
| **Paused** | 100m+ | 업데이트 중단 | 0 |

### 설정

```
LOD Settings:
├─ Full Speed Distance: 15m
├─ High Distance: 25m
├─ Medium Distance: 50m
├─ Low Distance: 75m
├─ Very Low Distance: 100m
├─ Pause Distance: 150m
├─ LOD Update Interval: 1.0s
└─ Max Full Speed Actors: 10 (최대 60 FPS AI 수)
```

### 성능 향상

| AI 수 | LOD 미사용 | LOD 사용 | 성능 향상 |
|-------|------------|----------|-----------|
| 100 | 60 FPS | 60 FPS | 1.0× |
| 500 | 30 FPS | 60 FPS | **2.0×** |
| 1000 | 15 FPS | 60 FPS | **4.0×** |

### 자동 등록/해제

BehaviorTree 시작 시 자동으로 AILODManager에 등록되고, 종료 시 자동 해제됩니다.

```csharp
// BehaviorTree.cs에 자동 통합됨
protected IEnumerator BehaviorProcess()
{
    // 자동 등록
    if (AILODManager.Instance != null)
    {
        AILODManager.Instance.RegisterActor(ActorControllerInstance, this);
    }

    while(bRunningBT)
    {
        // LOD 체크
        bool shouldUpdate = AILODManager.Instance.ShouldUpdate(ActorControllerInstance);
        if (shouldUpdate)
        {
            RootNode.Invoke(Time.deltaTime);
        }
        yield return null;
    }

    // 자동 해제
    AILODManager.Instance.UnregisterActor(ActorControllerInstance);
}
```

---

## 6. 새로운 Monster AI 만들기

### Step 1: BehaviorTree 클래스 생성

```csharp
public class MyCustomMonsterAI : BehaviorTree
{
    // 노드 선언
    private Sequence SeqRoot = new Sequence();
    private BTNodeWandering NodeWandering;
    private BTNodeSelectTarget NodeSelectTarget;
    // ... 기타 노드

    public override void Initialize(ActorController actorController)
    {
        BlackBoardInstance = new BlackBoard();
        ActorControllerInstance = actorController;

        // BlackBoard 설정
        BlackBoardInstance.AttackRange = 5.0f;
        BlackBoardInstance.AttackCooldown = 1.5f;
        BlackBoardInstance.DetectionRange = 30.0f;

        // Perception System 설정
        PerceptionSystem perception = actorController.gameObject.AddComponent<PerceptionSystem>();
        perception.SightRange = 30f;
        perception.SightAngle = 180f;
        perception.Initialize(BlackBoardInstance);

        // 노드 생성
        NodeWandering = new BTNodeWandering(this, actorController);
        NodeSelectTarget = new BTNodeSelectTarget(this, actorController);

        // BT 구조 구성
        SeqRoot.AddChild(NodeWandering);
        RootNode.AddChild(SeqRoot);
    }
}
```

### Step 2: ActorController에 적용

```csharp
public class MonsterController : ActorController
{
    public override void Init(SubWorld world, Actor instance)
    {
        // ...초기화 코드

        // AI 설정
        AIGroup[(int)AITypes.Common] = new MyCustomMonsterAI();
        AIGroup[(int)AITypes.Common].Initialize(this);
        AIGroup[(int)AITypes.Common].StartBT();
    }
}
```

### Step 3: 씬에 배치

1. Monster GameObject 생성
2. MonsterController 컴포넌트 추가
3. BoxCollider, Rigidbody 추가
4. 레이어를 "Actor_Monster"로 설정
5. Prefab으로 저장

---

## 7. Server-Side AI 통합 (향후 구현)

### 현재 상태

- **Client-Side AI**: 모든 AI 로직이 클라이언트에서 실행
- **문제**: 치팅 가능, 동기화 이슈

### 향후 개선안

#### Server-Authoritative AI 구조

```
GameServer (C# .NET 6.0)
├─ AIManager
│   ├─ MonsterAI (서버 버전)
│   │   ├─ BehaviorTree (간소화)
│   │   ├─ BlackBoard (최소 데이터)
│   │   └─ Combat Logic (권한)
│   └─ AIUpdateScheduler
│       └─ LOD 기반 업데이트
└─ NetworkSync
    └─ AI State Sync (클라이언트로)

Client (Unity)
├─ AIVisualization
│   ├─ Animation
│   ├─ VFX
│   └─ SFX
└─ Prediction (부드러운 움직임)
```

#### 구현 단계

1. **Phase 1**: 서버에 간소화된 BT 구현 (8시간)
2. **Phase 2**: AI 상태 동기화 프로토콜 추가 (4시간)
3. **Phase 3**: 클라이언트 예측 시스템 (4시간)

---

## 8. 성능 최적화 팁

### 1. LOD 설정 조정

```csharp
// 성능이 좋은 경우 (고사양 PC)
Settings.FullSpeedDistance = 30f;
Settings.MaxFullSpeedActors = 20;

// 성능이 낮은 경우 (저사양 PC)
Settings.FullSpeedDistance = 10f;
Settings.MaxFullSpeedActors = 5;
```

### 2. Perception 업데이트 간격 조정

```csharp
// 고성능 설정
perception.SightUpdateInterval = 0.1f;
perception.HearingUpdateInterval = 0.2f;

// 저성능 설정
perception.SightUpdateInterval = 0.5f;
perception.HearingUpdateInterval = 1.0f;
```

### 3. 불필요한 AI 비활성화

```csharp
// 플레이어로부터 매우 먼 AI는 완전히 비활성화
if (distance > 200f)
{
    behaviorTree.StopBT();
    actor.gameObject.SetActive(false);
}
```

---

## 9. 디버깅

### Gizmos 활용

```csharp
// PerceptionSystem Gizmos
perception.ShowDebugGizmos = true;  // 시야/청각 범위 표시

// AILODManager Gizmos
lodManager.ShowDebugGizmos = true;  // LOD 범위 표시

// AILODManager UI
lodManager.ShowDebugInfo = true;    // 화면에 통계 표시
```

### 로그 활용

```csharp
// AI 행동 로그
KojeomLogger.DebugLog($"[AI] {actor.name} selected target: {target.name}");
KojeomLogger.DebugLog($"[AI] {actor.name} is fleeing! Health: {bb.HealthRatio:P0}");
```

### Unity Profiler

- **Behavior Tree** CPU 사용량 확인
- **Physics.OverlapSphere** (Perception) 병목 확인
- **Garbage Collection** 최소화 (Dictionary 재사용)

---

## 10. 체크리스트

### AI 구현 전 확인사항

- [ ] AILODManager GameObject가 씬에 존재하는가?
- [ ] Player GameObject가 "Player" 태그를 가지고 있는가?
- [ ] Monster GameObject가 올바른 레이어에 있는가?
- [ ] Terrain GameObject가 "Terrain" 레이어에 있는가?
- [ ] ActorController에 BoxCollider와 Rigidbody가 있는가?

### 성능 최적화 체크리스트

- [ ] AI LOD Manager 활성화됨
- [ ] Max Full Speed Actors 제한 설정됨 (10~20)
- [ ] Perception 업데이트 간격이 적절한가 (0.2s~0.5s)
- [ ] 불필요한 AI가 비활성화되는가 (200m+)

---

## 11. 참고 자료

- **COMPREHENSIVE_ARCHITECTURE_ANALYSIS.md**: 전체 아키텍처 분석
- **ROOM_BASED_ARCHITECTURE.md**: 룸 기반 아키텍처
- **Unity Documentation**: Behavior Tree, State Machine
- **Game AI Pro**: Behavior Tree 패턴

---

## 개선 이력

| 버전 | 날짜 | 내용 |
|------|------|------|
| 1.0.0 | 2025-01-XX | Enhanced BlackBoard, Perception System, Combat Nodes, LOD Manager 구현 |

---

**문의**: 코드 관련 질문은 Issue 트래커에 등록해주세요.
