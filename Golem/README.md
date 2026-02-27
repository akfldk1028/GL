# Golem — Virtual AI Agent Character System

Unity 6000.3.3f1 기반 가상 AI 에이전트 캐릭터 시스템.
AI 서버(WebSocket)가 보내는 명령으로 3D 캐릭터가 자연스럽게 행동하고,
명령이 없을 때는 자율적으로 돌아다니며 생활하는 프레임워크.

## Core Logic — 어떻게 작동하는가

**현재 (Phase 1-9 + Tier 2 완료)**:
캐릭터는 10-state FSM이 모든 행동을 제어한다. AI 서버가 WebSocket으로 `"sit"` 같은 명령을 보내면,
`ActionTypeRegistry`가 ActionId로 매핑 → `ActionMessageBus`가 Pub/Sub으로 배포 → `CharacterCommandRouter`가 FSM 상태 전환 → Animator가 애니메이션 재생. AI 서버 명령이 없으면 `IdleScheduler`가 **LLM + Memory 기반 자율 판단**으로 행동을 선택한다.

**Tier 1 (LLM Decision Brain)**: FSM 상태, 위치, 주변 오브젝트, 최근 5개 행동, 성격을 LLM에 쿼리. confidence ≥ 0.3이면 실행, 아니면 가중 랜덤 폴백.

**Tier 2 (Memory System)**: 과거 경험을 기억하는 4종 메모리 시스템. Episodic Memory로 행동 이력 저장, Skill Library(Voyager 패턴)로 성공 패턴 캐시, Reflection Engine으로 주기적 자기성찰, ReAct 패턴으로 실패 시 재시도.

## Research Basis — 참조 논문

자율 판단 시스템 설계의 학술적 근거. 자세한 분석: [`docs/golem-specs/gl-2025-agent-research.md`](../docs/golem-specs/gl-2025-agent-research.md)

| # | Paper | Year | 가져온 것 |
|---|-------|------|-----------|
| 1 | [Park et al. — *Generative Agents*](https://arxiv.org/abs/2304.03442) | 2023 | **기반 아키텍처**: Memory Stream, Reflection, Persona 기반 행동 |
| 2 | [Wang et al. — *Voyager*](https://arxiv.org/abs/2305.16291) | 2023 | **Skill Library**: 성공한 행동 시퀀스 캐시 + 자기검증 |
| 3 | [Altera — *Project Sid*](https://arxiv.org/abs/2411.00114) | 2024 | **PIANO 아키텍처**: 1000 에이전트 규모 검증, LLM 판단 스케일 확인 |
| 4 | [DeepMind — *SIMA 2*](https://arxiv.org/abs/2512.04797) | 2025 | **Confidence 점수 + 자기개선 루프**: Evaluate→Adapt 사이클 |
| 5 | [Xi et al. — *LLM Agent Survey*](https://arxiv.org/abs/2309.07864) | 2025 | **4종 메모리 분류**: Working, Episodic, Semantic, Procedural |
| 6 | [Wang et al. — *Autonomous Agent Survey*](https://arxiv.org/abs/2308.11432) | 2024 | **추론 패턴**: CoT, ReAct, Reflexion |
| 7 | [Sumers et al. — *CoALA*](https://arxiv.org/abs/2309.02427) | 2024 | **인지 아키텍처 프레임워크**: 모듈형 메모리 + 액션 공간 |
| 8 | Brohan et al. — *RT-2* | 2023 | VLA 방식 **기각 근거** (연속 모터 제어 ≠ 이산 행동 선택) |
| 9 | [Piao et al. — *AgentSociety*](https://arxiv.org/abs/2502.08691) | 2025 | **10,000 에이전트 자율 행동**: Mind-Behavior Coupling, 대규모 자율 검증 |

## Architecture Overview

```
Layer 5 ─ Decision Brain ─────────── LLM 기반 자율 판단 + 4종 메모리 시스템
                                        Tier 1: AIDecisionConnector (LLM 쿼리)
                                        Tier 2: MemoryStore + SkillLibrary + ReflectionEngine

Layer 4 ─ AI Server Commands ──── CFConnector → AINetworkManager → ActionBus
                                    → CharacterCommandRouter (13 commands 구독)

Layer 3 ─ Autonomous Behavior ─── IdleScheduler (AI 명령 없을 때 자율 행동)
                                    → Skill Check → Memory Retrieve → LLM → Track → Reflect

Layer 2 ─ Behavior Modules ────── 5개 모듈 (항상 작동, FSM 독립)
                                    호흡 │ 시선 │ ThinkTime │ Idle변형 │ 가감속

Layer 1 ─ Character FSM ─────────  10-state FSM (상태 전환 + 애니메이션 제어)
                                    CharacterBehaviorFSM + CharacterStateContext

Layer 0 ─ PointClickController ── NavMesh 이동 + Animator Speed feed
                                    FSM 모드 시 thin API만 노출
```

### End-to-End Data Flow

```
[AI Server]
    │ WebSocket (System.Net.WebSockets)
    ▼
CFConnector.ReceiveLoop()
    │ HandleMessage() → "character_action" 식별
    ▼
AINetworkManager.HandleCharacterAction()
    │ ActionTypeRegistry.MapToActionId("move" → ActionId.Character_MoveToLocation)
    ▼
Managers.PublishAction(actionId, payload)        ← Pub/Sub 중심점
    │
    ├──▶ CharacterCommandRouter (ActionId별 구독)
    │       → FSM 상태 전환 + PointClickController 명령
    │
    ├──▶ GolemSocialHandler (Social 액션)
    │       → Wave, Nod, Greet 등
    │
    └──▶ GolemEmoteHandler (Emote 액션)
            → 표정, 음성

[캐릭터 행동 완료]
    │
    ▼
CharacterActionCompletionTracker (FSM.OnStateChanged 감지)
    │ Managers.PublishAction(Agent_ActionCompleted)
    ▼
AINetworkManager → CFConnector.Send() → [AI Server]
```

### Pub/Sub API (Managers)

```csharp
// 구독 — returns IDisposable
IDisposable sub = Managers.Subscribe(ActionId.Character_MoveToLocation, (ActionMessage msg) =>
{
    if (msg.TryGetPayload<MoveToLocationPayload>(out var p))
        Debug.Log($"Moving to {p.Destination}");
});

// 발행 — payload 없이
Managers.PublishAction(ActionId.Character_Stop);

// 발행 — payload 포함
Managers.PublishAction(ActionId.Character_SitAtChair, new SitAtChairPayload { ChairNumber = 2 });

// 해제
sub.Dispose();
```

**Managers 전체 API:**

| Method | Description |
|--------|-------------|
| `Managers.Init()` | 전체 서브시스템 초기화 (GolemBootstrap이 호출) |
| `Managers.Subscribe(ActionId, Action<ActionMessage>)` | 액션 구독 (IDisposable 반환) |
| `Managers.PublishAction(ActionId)` | payload 없는 액션 발행 |
| `Managers.PublishAction(ActionId, IActionPayload)` | payload 포함 액션 발행 |
| `Managers.RegisterState(IState)` | StateMachine에 상태 등록 |
| `Managers.SetState(StateId)` | StateMachine 상태 전환 |
| `Managers.ActionBus` | ActionMessageBus 직접 접근 |
| `Managers.State` | StateMachine 직접 접근 |
| `Managers.Agent` | AgentManager 접근 |

## Project Structure

```
Assets/Scripts/                              ~78 files
├── GolemBootstrap.cs                        시스템 초기화 (Exec Order -100)
│
├── Character/
│   ├── GolemCharacterController.cs          Slim orchestrator (~130 lines)
│   │                                          FSM + Modules + Router + IdleScheduler 생성
│   ├── PointClickController.cs              NavMesh 이동 + Speed animator feed
│   │                                          fsmActive=true 시 legacy 로직 스킵
│   ├── CelesteActionController.cs           Legacy action controller
│   ├── EmotePlayer.cs                       Audio/voice playback
│   ├── GolemActionGate.cs                   Action gating (Awake -50)
│   ├── GolemEmoteHandler.cs                 Emote ActionBus handler
│   ├── GolemSocialHandler.cs                Social ActionBus handler
│   │
│   ├── FSM/                                 ── 상태 기계 ──
│   │   ├── CharacterStateId.cs              enum: None + 10 states
│   │   ├── ICharacterState.cs               Enter/Exit/Update/CanTransitionTo
│   │   ├── CharacterStateContext.cs          Shared blackboard (9 fields)
│   │   ├── CharacterBehaviorFSM.cs          RegisterState/ForceTransition/RequestTransition
│   │   ├── CharacterCommandRouter.cs        ActionId→FSM 매핑 (13 commands)
│   │   ├── CharacterActionCompletionTracker.cs  완료 감지 → ActionCompleted 발행
│   │   ├── InteractionSpotFinder.cs         Tag 기반 오브젝트 검색
│   │   └── States/
│   │       ├── IdleState.cs                 NavAgent 활성화, path reset
│   │       ├── WalkingState.cs              도착 감지 → Idle/Arriving 전환
│   │       ├── ArrivingState.cs             InteractionSpot snap + 상호작용 전환
│   │       ├── SitTransitionState.cs        의자로 이동 → snap → ToSit
│   │       ├── SittingState.cs              NavAgent 비활성, 착석
│   │       ├── StandTransitionState.cs      ToStand 트리거, 애니메이션 완료 대기
│   │       ├── LookingState.cs              LookingDown bool 제어
│   │       ├── LeaningState.cs              Leaning bool 제어
│   │       ├── PlayingArcadeState.cs        ToPlayArcade 트리거
│   │       └── PlayingClawState.cs          PlayingClawMachine bool 제어
│   │
│   ├── Modules/                             ── 행동 모듈 ──
│   │   ├── IBehaviorModule.cs               ModuleId, IsActive, OnUpdate, OnLateUpdate
│   │   ├── BaseBehaviorModule.cs            Abstract base
│   │   ├── BehaviorModuleContext.cs          Animator, NavAgent, FSM, Bones, Config
│   │   ├── BehaviorModuleRegistry.cs        Register/Get<T>/UpdateAll/LateUpdateAll
│   │   ├── BehaviorConfigSO.cs              ScriptableObject (17 tuning fields)
│   │   └── Impl/
│   │       ├── BreathingModule.cs           Spine bone 사인파 호흡 (LateUpdate)
│   │       ├── HeadLookModule.cs            InterestPoint 태그 시선 추적
│   │       ├── ThinkTimeModule.cs           Idle→Walking 전환 전 자연스러운 멈춤
│   │       ├── IdleVariationModule.cs       Hip bone 체중 이동 + 제스처 변형
│   │       └── AccelerationCurveModule.cs   S-curve 가속 (NavAgent.speed 제어)
│   │
│   └── Autonomous/                          ── 자율 행동 ──
│       ├── AutonomousAction.cs              ActionId + Payload + ExpectedDuration
│       ├── AIDecisionConfigSO.cs            LLM 엔드포인트 설정 (Tier 1)
│       ├── AIDecisionConnector.cs           HTTP POST → LLM 쿼리 + 메모리 프롬프트 (Tier 1+2)
│       ├── IdleScheduler.cs                 자율 행동 스케줄러 (Skill→Memory→LLM→Track→Reflect)
│       ├── IdleSchedulerConfigSO.cs         ScriptableObject 설정
│       ├── EpisodeEntry.cs                  Episodic Memory 데이터 모델 (Tier 2)
│       ├── SkillEntry.cs                    Skill Library 데이터 모델 (Tier 2)
│       ├── MemoryConfigSO.cs               메모리 시스템 설정 ScriptableObject (Tier 2)
│       ├── EpisodicMemory.cs               점수 기반 에피소드 검색 + FIFO 관리 (Tier 2)
│       ├── SkillLibrary.cs                  상황 패턴 매칭 + 성공 패턴 캐시 (Tier 2)
│       ├── MemoryStore.cs                   메모리 파사드 + JSON 영속화 (Tier 2)
│       ├── ActionOutcomeTracker.cs          행동 결과 추적 + 메모리 기록 (Tier 2)
│       └── ReflectionEngine.cs              주기적 자기성찰 + 관찰 생성 (Tier 2)
│
├── Infrastructure/
│   ├── Messages/
│   │   ├── ActionId.cs                      42 action IDs (10 categories)
│   │   ├── ActionMessage.cs                 readonly struct + TryGetPayload<T>
│   │   ├── ActionMessageBus.cs              Filtered pub/sub
│   │   ├── ActionPayloads.cs                17 payload classes
│   │   ├── ActionTypeRegistry.cs            string→ActionId 매핑
│   │   ├── CompositeActionExecutor.cs       BML 시퀀스 실행
│   │   ├── BufferedMessageChannel.cs        버퍼링 채널
│   │   ├── IMessageChannel.cs               메시지 채널 인터페이스
│   │   ├── MessageChannel.cs                IMessageChannel 구현
│   │   ├── DisposableSubscription.cs        IDisposable 구독 래퍼
│   │   └── ParamHelper.cs                   JSON 파라미터 추출
│   └── State/
│       ├── IState.cs / StateId.cs           글로벌 상태 (Boot/Connected/Active)
│       └── StateMachine.cs                  상태 전환 + OnStateChanged 이벤트
│
├── Managers/
│   ├── Managers.cs                          Singleton service locator
│   ├── Core/
│   │   ├── ResourceManager.cs
│   │   ├── PoolManager.cs
│   │   └── DataManager.cs
│   └── Contents/
│       ├── AgentManager.cs
│       └── AgentInstance.cs
│
├── Systems/
│   ├── Camera/
│   │   ├── CameraStateMachine.cs            SO 기반 카메라 상태 전환
│   │   ├── CameraStateSO.cs / CameraStateTransitionSO.cs
│   │   ├── CameraSystemController.cs
│   │   ├── CameraStateTester.cs
│   │   └── SmoothRotationCamTarget.cs
│   └── Networking/
│       ├── CFConnector.cs                   WebSocket 클라이언트 (System.Net.WebSockets)
│       └── AINetworkManager.cs              WS → ActionBus 브릿지
│
├── Debug/
│   ├── GolemDebugOverlay.cs                 F12 — 연결, State, ActionBus 로그
│   ├── CharacterBehaviorDebugUI.cs          F11 — FSM 상태, 모듈 활성
│   └── BehaviorModuleProfiler.cs            Unity Profiler 마커
│
├── Tests/
│   └── AgentManagerTest.cs
└── Utils/
    └── WavUtility.cs
```

## FSM States & Transitions

```
                         ┌──────────────────────────────────────────┐
                         │              Idle (default)               │
                         └──┬──────────┬──────────┬──────────┬──────┘
                            │          │          │          │
                       Walking    SitTransition  Arriving  Arriving
                            │          │          │          │
                            ▼          ▼          ▼          ▼
                         (도착)     Sitting    Looking    Leaning
                          →Idle        │       PlayingArcade
                                       │       PlayingClaw
                              StandTransition
                                       │
                                     Idle
```

| State | Animator Param | NavAgent | CanTransitionTo |
|-------|---------------|----------|-----------------|
| Idle | Speed=0 | enabled | Walking, Arriving, SitTransition |
| Walking | Speed>0 | enabled | all |
| Arriving | — | enabled→**disabled** before snap | all |
| SitTransition | — | enabled→**disabled** before snap | Sitting, Idle, Walking |
| Sitting | ToSit trigger | disabled | StandTransition only |
| StandTransition | ToStand trigger | disabled→enabled | Idle, Walking |
| Looking | LookingDown=true | disabled | all |
| Leaning | Leaning=true | disabled | all |
| PlayingArcade | ToPlayArcade | disabled | all |
| PlayingClaw | PlayingClawMachine=true | disabled | all |

**핵심 설계**: Arriving/SitTransition에서 `SnapToTransform()` 호출 전 반드시 `NavAgent.enabled = false` 처리.
NavAgent가 활성 상태에서 위치를 직접 변경하면 슬라이딩 현상 발생.

### CharacterStateContext (Blackboard)

```csharp
public class CharacterStateContext
{
    // 주입 참조
    public PointClickController PointClick;
    public Animator Animator;
    public NavMeshAgent NavAgent;
    public Transform CharacterTransform;
    public CharacterBehaviorFSM FSM;

    // 인터랙션 데이터 (런타임)
    public Transform InteractionSpot;
    public Collider DisabledCollider;
    public Vector3 PendingDestination;
    public CharacterStateId PendingInteractionState;

    // 유틸리티
    public void ClearInteraction();           // InteractionSpot + Pending 초기화
    public void RestoreDisabledCollider();    // 비활성 콜라이더 복원
}
```

## Behavior Modules

FSM 상태와 독립적으로 항상 작동하며 "살아있는" 느낌을 추가하는 모듈 레이어.

| Module | Timing | Effect | Config Fields |
|--------|--------|--------|---------------|
| **Breathing** | LateUpdate | Spine bone X축 사인파 회전 (`*=` 적용) | breathingRate (0.15), breathingAmplitude (0.3), breathingSpeedMultiplier (1.5) |
| **HeadLook** | Update+LateUpdate | Head bone → InterestPoint 태그 대상 추적 | gazeSpeed (2.0), gazeChangeInterval (5.0), gazeChangeVariance (2.0), gazeFOV (120) |
| **ThinkTime** | passive | Idle→Walking 전환 전 자연스러운 멈춤 삽입 | thinkTimeMin (0.3), thinkTimeMax (2.0) |
| **IdleVariation** | Update+LateUpdate | Hip bone 미세 이동 + IdleVariation 트리거 | weightShiftInterval (12), microGestureInterval (45), hipShiftAmount (0.02), ..2 more |
| **AccelerationCurve** | Update | NavAgent.speed를 AnimationCurve로 S-curve 가속 | accelerationCurve, accelerationTime (0.5) |

### Module Design Notes

**BreathingModule**: Animator가 bone rotation을 매 프레임 덮어쓰므로, `LateUpdate`에서 `*=` (multiply) 연산으로 기존 회전 위에 호흡을 합성. `=` 대입은 Animator 결과를 무시하게 됨.

**HeadLookModule**: `SetForcedTarget(pos)` / `ClearForcedTarget()`로 FSM이 시선 강제 지정 가능. 자동 시선은 `InterestPoint` 태그 오브젝트 중 캐릭터 전방 120° 범위 내에서 선택. FOV 체크는 head bone이 아닌 `CharacterTransform.forward` 기준.

**AccelerationCurve**: 가속만 적용 (Walking 진입 시 0→baseSpeed). 감속은 NavAgent 기본 감속에 위임. `BehaviorConfigSO.decelerationTime` 필드는 현재 사용되지 않음.

## CommandRouter Action Mapping

CharacterCommandRouter가 ActionBus에서 13개 명령을 구독하여 FSM 전환으로 변환:

| ActionId | Handler | FSM Result |
|----------|---------|------------|
| Character_MoveToLocation / WalkTo / RunTo | OnMoveToLocation | → Walking (ThinkTime pause 적용) |
| Character_Stop | OnStop | → Idle (진행 중 DelayedMove 취소) |
| Character_TurnTo | OnTurnTo | 제자리 회전 (상태 유지) |
| Character_SitAtChair | OnSitAtChair | → SitTransition → Sitting |
| Character_StandUp | OnStandUp | Sitting → StandTransition; 그 외 → Idle |
| Character_Idle | OnIdle | → Idle / delegate to sit/lean |
| Character_Lean | OnLean | → Arriving → Leaning |
| Character_ExamineMenu | OnExamineMenu | → Arriving → Looking |
| Character_LookAt | OnLookAt | 제자리 회전 (상태 유지) |
| Character_PlayArcade | OnPlayArcade | → Arriving → PlayingArcade |
| Character_PlayClaw | OnPlayClaw | → Arriving → PlayingClaw |
| Camera_ChangeAngle | (GolemCharacterController) | CameraStateMachine 전환 |

**완료 보고**: 각 명령 완료 시 `Agent_ActionCompleted` 발행.
StandUp(Sitting→StandTransition 경로)만 CompletionTracker가 애니메이션 완료를 감지하여 발행.

## Autonomous Behavior (IdleScheduler + AIDecisionConnector + Memory)

AI 서버 명령 없이 Idle 상태가 N초 지속되면 자율 행동 시작.

### Decision Flow (Tier 1+2)

```
Timer → [1] BuildContextHash (fsmState|nearbyTags|timeBucket)
       → [2] SkillLibrary.Match → ShouldUseSkill?
             ├─ Yes → 캐시된 행동 실행 (LLM 스킵) ─┐
             └─ No / Explore                        │
       → [3] EpisodicMemory.RetrieveTopK            │
       → [4] LLM 쿼리 (memories + failureContext)   │
       → [5] BeginTracking ←────────────────────────┘
       → [6] ExecuteAutonomousAction
       → [7] 완료 시:
             → EpisodicMemory.AddEpisode
             → SkillLibrary.RecordOutcome
             → 실패 + enableRetry → ReAct 재시도 (1회)
       → [8] ShouldReflect? → ExecuteReflection
       → Loop
```

### Tier 1 (LLM Decision Brain)
`AIDecisionConfigSO`가 할당되고 `useLLM=true`이면 LLM에 쿼리.
FSM 상태, 위치, 주변 오브젝트, 최근 5개 행동, 성격 + **최근 메모리**를 프롬프트로 전송.
confidence ≥ 0.3이면 LLM 판단 실행, 그 외 가중 랜덤 폴백.

### Tier 2 (Memory System)
`MemoryConfigSO`가 할당되면 4종 메모리 활성화:

| Component | Purpose | Key Method |
|-----------|---------|------------|
| **EpisodicMemory** | 행동 이력 저장 + 점수 기반 검색 | `RetrieveTopK(contextHash)` |
| **SkillLibrary** | 성공 패턴 캐시 (Voyager) | `Match(pattern) → ShouldUseSkill()` |
| **ActionOutcomeTracker** | 완료/실패 추적 → 메모리 기록 | `BeginTracking() → CompleteTracking()` |
| **ReflectionEngine** | 주기적 자기성찰 → 관찰 생성 | `ShouldReflect() → ExecuteReflection()` |

**Episodic Scoring**: `recency(exp decay) × 0.4 + importance × 0.3 + relevance(context match) × 0.3`

**Persistence**: `{persistentDataPath}/GolemMemory/{characterName}_memory.json`

### Fallback Safety Chain
```
memoryConfig == null  → Tier 1 (LLM + recent actions only)
decisionConnector == null → Tier 0 (weighted random)
MemoryStore.Load() fail → empty memory (fresh start)
SkillLibrary.Match() null → query LLM
RetrieveTopK() empty → prompt에 memories 섹션 없음
Reflection fail → swallow error, continue
File save fail → log warning, in-memory only
```

### 폴백 (가중 랜덤)
LLM 미설정 또는 실패 시 아래 가중치로 랜덤 선택.

| Action | Default Weight | ActionId | Description |
|--------|---------------|----------|-------------|
| Wander | 3.0 | Character_MoveToLocation | NavMesh 랜덤 위치로 이동 |
| Look Around | 2.0 | Character_TurnTo | 랜덤 방향 회전 |
| Sit | 1.5 | Character_SitAtChair | 랜덤 의자에 앉기 |
| Gesture | 1.5 | Character_Idle | standing idle 변형 |
| Play Game | 1.0 | (placeholder → wander) | 추후 구현 예정 |

**우선순위**: AI 서버 명령 수신 시 `CancelCurrentAction()` 즉시 중단.
자율 행동은 기존 ActionBus 파이프라인으로 발행되므로 CharacterCommandRouter가 동일하게 처리.

## ActionId Categories (42 IDs)

```csharp
System     = 100  // Update, LateUpdate, FixedUpdate                     (3)
Agent      = 200  // Connected, Disconnected, StateChanged, Error, ...   (9)
Expression = 300  // VoiceEmote, TTS, AnimatedEmote, Facial, Gesture, Gaze  (8)
Locomotion = 400  // MoveToLocation, WalkTo, RunTo, Stop, TurnTo        (5)
Posture    = 450  // SitAtChair, StandUp, Idle, Lean                    (4)
Interaction = 500 // ExamineMenu, LookAt, PlayArcade, PlayClaw          (4)
Social     = 600  // Greet, Wave, Nod, HeadShake, Point                 (5)
Camera     = 700  // ChangeAngle                                        (1)
Composite  = 800  // MultiAction, Sequence                              (2)
Feedback   = 900  // ActionResult                                       (1)
```

## PointClickController Dual-Mode

PointClickController는 두 가지 모드로 동작:

| Mode | `fsmActive` | Behavior |
|------|-------------|----------|
| **Legacy** | false | 마우스 클릭 → 11개 boolean 플래그 → 상태 전환 (원래 로직) |
| **FSM** | true | 마우스/boolean 로직 스킵, Speed animator feed만 실행. FSM이 모든 상태 제어 |

GolemCharacterController가 Start()에서 `pointClick.fsmActive = true` 설정.
GolemCharacterController가 없는 씬에서는 legacy 모드로 단독 동작 가능.

### Thin API (FSM 모드에서 사용)

```csharp
pointClick.MoveToPoint(Vector3 dest);      // NavMesh 이동 시작
pointClick.StopMovement();                  // 즉시 정지
pointClick.SnapToTransform(Transform t);    // 위치+회전 즉시 변경
bool arrived = pointClick.HasArrived;       // 도착 여부 (hasPath 체크 포함)
bool moving  = pointClick.IsMoving;         // 이동 중 여부
```

## Setup

### 1. Scene 구성

1. `GolemBootstrap` 오브젝트 배치 (Script Execution Order: -100)
2. 캐릭터 GameObject에 컴포넌트:
   - `GolemCharacterController`
   - `PointClickController`
   - `NavMeshAgent`
   - `Animator`
3. Inspector 할당:
   - `BehaviorConfigSO` — Assets > Create > Golem > BehaviorConfig
   - `IdleSchedulerConfigSO` — Assets > Create > Golem > IdleSchedulerConfig
   - Bone 참조: `spineBone` (호흡), `headBone` (시선), `hipBone` (체중 이동)

### 2. Scene 오브젝트 태그

| Tag | 용도 | 필수 |
|-----|------|------|
| `Caffee Chair` | 앉을 수 있는 카페 의자 | InteractionSpot 자식 필요 |
| `Cafe Ad Display` | 광고판 (examineMenu) | InteractionSpot 자식 필요 |
| `Slot Machine Chair` | 슬롯머신 의자 (leaning) | InteractionSpot 자식 필요 |
| `Arcade` | 아케이드 기기 | InteractionSpot 자식 필요 |
| `Claw Machine` | 인형뽑기 | InteractionSpot 자식 필요 |
| `InterestPoint` | HeadLook 시선 대상 | 위치만 사용 |

각 인터랙션 오브젝트는 `InteractionSpot` 이름의 자식 Transform을 가져야 합니다.
캐릭터가 도착 후 해당 Transform의 position/rotation으로 snap됩니다.

### 3. Animator Parameters

| Parameter | Type | Used By |
|-----------|------|---------|
| Speed | float | PointClickController (이동 속도) |
| ToSit | trigger | SitTransitionState |
| ToStand | trigger | StandTransitionState |
| ToPlayArcade | trigger | PlayingArcadeState |
| ToStopArcade | trigger | PointClickController (legacy) |
| LookingDown | bool | LookingState |
| Leaning | bool | LeaningState |
| PlayingClawMachine | bool | PlayingClawState |
| IdleVariation | trigger | IdleVariationModule |

### 4. AI Server 연결

CFConnector Inspector에서 서버 URL 설정.
`AINetworkManager`가 자동으로 WebSocket 이벤트를 ActionBus로 변환.

## Debug

| Key | UI | Content |
|-----|-----|---------|
| **F12** | GolemDebugOverlay | 연결 상태, StateMachine, Character FSM, 메시지율, ActionBus 로그 |
| **F11** | CharacterBehaviorDebugUI | FSM 현재/이전 상태, 5개 모듈 활성 상태 |

Unity Profiler 마커:
- `Golem.FSM.Update`
- `Golem.Modules.Update` / `Golem.Modules.LateUpdate`
- `Golem.Module.Breathing` / `Golem.Module.HeadLook` / etc.

## Implementation Status

| Phase | Status | Content |
|-------|--------|---------|
| 1. Character FSM | **Done** | 10 states, FSM engine, context blackboard |
| 2. Module Framework + Breathing | **Done** | IBehaviorModule, Registry, BreathingModule |
| 3. HeadLook + ThinkTime + IdleVar + Accel | **Done** | 4 behavior modules |
| 4. Command Router + Completion Tracker | **Done** | 13 commands, action lifecycle |
| 5. Autonomous Idle | **Done** | IdleScheduler, weighted random |
| 6. Debug & Polish | **Done** | F11/F12 overlays, ProfilerMarker |
| 7. Multi-Channel Behavior | Not started | 상체/하체 분리, 걸으면서 제스처 |
| 8. Animation Rigging | Not started | IK 기반 시선/호흡 (optional) |
| 9. LLM Decision Brain (Tier 1) | **Done** | AIDecisionConnector (Ollama/OpenAI), CoT 프롬프트, confidence 폴백 |
| 10. Memory + Skills (Tier 2) | **Done** | EpisodicMemory, SkillLibrary, MemoryStore, ActionOutcomeTracker, ReflectionEngine, ReAct retry |

## Dependencies

- **Unity 6000.3.3f1**
- NavMesh (built-in)
- Newtonsoft.Json (JSON.NET, Unity 내장)
- System.Net.WebSockets (.NET 내장)
- 외부 패키지 불필요

## Next Steps

### Tier 1 — LLM Decision Brain (Phase 9) ✅ 완료

| 파일 | 역할 |
|------|------|
| `AIDecisionConfigSO.cs` | ScriptableObject — endpoint URL, model, personality, 스캔 반경 |
| `AIDecisionConnector.cs` | HTTP POST (Ollama/OpenAI), 프롬프트 빌더, JSON 파서, ActionId 매핑 |
| `IdleScheduler.cs` | LLM 쿼리 경로 + 가중 랜덤 폴백 + 최근 행동 5개 추적 |

**사용법**: `Assets > Create > Golem > AIDecisionConfig` → Inspector에서 설정 → GolemCharacterController에 할당

### Tier 2 — Memory + Skills (Phase 10) ✅ 완료

| 파일 | 역할 |
|------|------|
| `EpisodeEntry.cs` | Episodic Memory 데이터 모델 — timestampTicks, actionId, importance, succeeded, contextHash |
| `SkillEntry.cs` | Skill Library 데이터 모델 — situationPattern, successRate, useCount |
| `MemoryConfigSO.cs` | ScriptableObject — episodic/skill/reflection/persistence/ReAct 설정 |
| `EpisodicMemory.cs` | 점수 기반 검색 (recency×0.4 + importance×0.3 + relevance×0.3), FIFO eviction |
| `SkillLibrary.cs` | Voyager 패턴 — 상황 패턴 매칭, 성공률 기반 캐시, exploration rate, pruning |
| `MemoryStore.cs` | 파사드 — EpisodicMemory + SkillLibrary + JSON 영속화 |
| `ActionOutcomeTracker.cs` | Agent_ActionCompleted/Failed 구독 → 에피소드 + 스킬 기록 |
| `ReflectionEngine.cs` | N 액션마다 자기성찰 → 추상 관찰 생성 → 에피소드에 저장 |

**사용법**: `Assets > Create > Golem > MemoryConfig` → Inspector에서 설정 → GolemCharacterController에 할당

### Tier 3 — Self-Improvement (Future)

1. 자기 평가 루프 (SIMA 2 패턴) — N번 결정마다 LLM이 자기 판단 리뷰
2. 로컬 모델 최적화 (Ollama, Qwen2.5-3B / Llama 3.2 3B)

자세한 스펙: [`docs/golem-specs/gl-autonomous-decision-system.md`](../docs/golem-specs/gl-autonomous-decision-system.md)

## Documentation

| Document | Path | Purpose |
|----------|------|---------|
| **이 파일** | `Golem/README.md` | 프로젝트 개요, 구조, 설정 가이드 |
| 자율 판단 시스템 스펙 | `docs/golem-specs/gl-autonomous-decision-system.md` | Decision Brain 설계 — Tier 1/2/3 구현 계획 |
| 2025 에이전트 연구 | `docs/golem-specs/gl-2025-agent-research.md` | SIMA 2, Voyager, Project Sid 등 연구 레퍼런스 |
| 자연스러운 행동 연구 | `docs/research/natural-ai-character-behavior.md` | 모션, idle, 전환 등 행동 자연스러움 기법 |
| Phase 스펙 (1-8) | `docs/golem-specs/phase{1-8}.txt` | 각 Phase 구현 상세 스펙 |

