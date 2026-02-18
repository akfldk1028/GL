# Golem — Virtual AI Agent Character System

Unity 6000.3.3f1 기반 가상 AI 에이전트 캐릭터 시스템.
AI 서버(WebSocket)와 3D 캐릭터의 자율 행동을 연결하는 프레임워크.

## Architecture

```
Layer 4: AI Server Commands     CFConnector → ActionBus → CharacterCommandRouter
Layer 3: Autonomous Behavior    IdleScheduler (AI 명령 없을 때 자율 행동)
Layer 2: Behavior Modules       호흡, 시선, ThinkTime, Idle변형, 가감속 (항상 작동)
Layer 1: Character FSM          10-state FSM (상태 전환 + 애니메이션 제어)
Layer 0: PointClickController   NavMesh + Animator (실행만 담당하는 thin layer)
```

### Data Flow

```
[AI Server] → WebSocket → CFConnector → AINetworkManager → ActionMessageBus
  → CharacterCommandRouter (ActionId 구독)
    → CharacterBehaviorFSM (상태 전환)
      → PointClickController (NavMesh 이동/애니메이션)
  → CharacterActionCompletionTracker (완료 감지)
    → Agent_ActionCompleted → AINetworkManager → [AI Server]
```

### Pub/Sub API

```csharp
// 구독 (returns IDisposable)
Managers.Subscribe(ActionId.Character_MoveToLocation, (ActionMessage msg) => { ... });

// 발행
Managers.PublishAction(ActionId.Character_Stop);
Managers.PublishAction(ActionId.Character_SitAtChair, new SitAtChairPayload { ChairNumber = 2 });
```

## Project Structure

```
Assets/Scripts/                              70 files
├── GolemBootstrap.cs                        시스템 초기화 진입점 (Exec Order -100)
│
├── Character/
│   ├── GolemCharacterController.cs          Slim orchestrator (~130 lines)
│   ├── PointClickController.cs              NavMesh 이동 + 마우스 입력 + thin API
│   ├── CelesteActionController.cs           Legacy action controller
│   ├── EmotePlayer.cs                       Audio/voice playback
│   ├── GolemActionGate.cs                   Action gating (connected 상태 체크)
│   ├── GolemEmoteHandler.cs                 Emote ActionBus handler
│   ├── GolemSocialHandler.cs                Social ActionBus handler
│   │
│   ├── FSM/
│   │   ├── CharacterStateId.cs              enum: None + 10 states
│   │   ├── ICharacterState.cs               Id, Enter, Exit, Update, CanTransitionTo
│   │   ├── CharacterStateContext.cs          Shared blackboard (refs + interaction data)
│   │   ├── CharacterBehaviorFSM.cs          RequestTransition / ForceTransition / Update
│   │   ├── CharacterCommandRouter.cs        ActionId 구독 → FSM 전환 매핑 (13 commands)
│   │   ├── CharacterActionCompletionTracker.cs  FSM.OnStateChanged → ActionCompleted 발행
│   │   ├── InteractionSpotFinder.cs         Tag 기반 오브젝트 검색 유틸리티
│   │   └── States/
│   │       ├── IdleState.cs                 NavAgent 활성화, path reset
│   │       ├── WalkingState.cs              Speed param → Animator, 도착 감지
│   │       ├── ArrivingState.cs             InteractionSpot 접근 + snap
│   │       ├── SitTransitionState.cs        의자로 이동 중 → ToSit
│   │       ├── SittingState.cs              NavAgent 비활성, 착석
│   │       ├── StandTransitionState.cs      ToStand 트리거, 애니메이션 완료 대기
│   │       ├── LookingState.cs              LookingDown bool
│   │       ├── LeaningState.cs              Leaning bool
│   │       ├── PlayingArcadeState.cs        ToPlayArcade 트리거
│   │       └── PlayingClawState.cs          PlayingClawMachine bool
│   │
│   ├── Modules/
│   │   ├── IBehaviorModule.cs               ModuleId, IsActive, OnUpdate, OnLateUpdate
│   │   ├── BaseBehaviorModule.cs            Abstract base (boilerplate 감소)
│   │   ├── BehaviorModuleContext.cs          Animator, NavAgent, FSM, Bones, Config
│   │   ├── BehaviorModuleRegistry.cs        Register / Get<T> / UpdateAll / Dispose
│   │   ├── BehaviorConfigSO.cs              ScriptableObject 튜닝값 전체
│   │   └── Impl/
│   │       ├── BreathingModule.cs           Spine bone 사인파 호흡
│   │       ├── HeadLookModule.cs            InterestPoint 태그 시선 추적
│   │       ├── ThinkTimeModule.cs           Idle→Walking 전환 시 0.3~2s 멈춤
│   │       ├── IdleVariationModule.cs       체중 이동(12s) + 미세 제스처(45s)
│   │       └── AccelerationCurveModule.cs   AnimationCurve 기반 S-curve 가감속
│   │
│   └── Autonomous/
│       ├── AutonomousAction.cs              Action value object (ActionId + Payload)
│       ├── IdleScheduler.cs                 가중 랜덤 자율 행동 스케줄러
│       └── IdleSchedulerConfigSO.cs         딜레이, 가중치, wanderRadius SO
│
├── Infrastructure/
│   ├── Messages/
│   │   ├── ActionId.cs                      100+ action IDs (8-layer pattern)
│   │   ├── ActionMessage.cs                 readonly struct + TryGetPayload<T>
│   │   ├── ActionMessageBus.cs              Filtered pub/sub
│   │   ├── ActionPayloads.cs                MoveToLocation, SitAtChair, Gaze 등
│   │   ├── ActionDispatcher.cs              StateMachine 연동 디스패처
│   │   ├── ActionTypeRegistry.cs            Action type 등록
│   │   ├── CompositeActionExecutor.cs       BML 시퀀스 실행
│   │   ├── BufferedMessageChannel.cs        버퍼링 채널
│   │   ├── MessageChannel.cs / IMessageChannel.cs
│   │   ├── DisposableSubscription.cs
│   │   └── ParamHelper.cs
│   └── State/
│       ├── IState.cs / StateId.cs           글로벌 상태 인터페이스
│       └── StateMachine.cs                  Boot→Connected→Active 전환
│
├── Managers/
│   ├── Managers.cs                          Singleton service locator (ActionBus, State 등)
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
│       ├── CFConnector.cs                   WebSocket 클라이언트
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
                    ┌─────────────────────────────────────────┐
                    │              Idle (default)              │
                    └─────┬────────┬────────┬────────┬────────┘
                          │        │        │        │
                     Walking  SitTransition Arriving Arriving
                          │        │        │        │
                          ▼        ▼        ▼        ▼
                       (도착)   Sitting   Looking  Leaning
                                   │      PlayingArcade
                          StandTransition  PlayingClaw
                                   │
                                 Idle
```

| State | Animator Param | NavAgent | CanTransitionTo |
|-------|---------------|----------|-----------------|
| Idle | Speed=0 | enabled | Walking, Arriving, SitTransition |
| Walking | Speed>0 | enabled | all |
| Arriving | — | disabled on snap | all |
| SitTransition | — | enabled→disabled | Sitting, Idle, Walking |
| Sitting | ToSit trigger | disabled | StandTransition only |
| StandTransition | ToStand trigger | disabled→enabled | Idle, Walking |
| Looking | LookingDown=true | disabled | all (interrupt OK) |
| Leaning | Leaning=true | disabled | all |
| PlayingArcade | ToPlayArcade trigger | disabled | all |
| PlayingClaw | PlayingClawMachine=true | disabled | all |

## Behavior Modules

항상 작동하며 FSM 상태와 독립적으로 "살아있는" 느낌 추가:

| Module | Update | Effect | Config Fields |
|--------|--------|--------|---------------|
| Breathing | LateUpdate | Spine bone X축 사인파 회전 | breathingRate, breathingAmplitude, breathingSpeedMultiplier |
| HeadLook | Update+Late | Head bone → InterestPoint 태그 추적 | gazeSpeed, gazeChangeInterval, gazeChangeVariance, gazeFOV |
| ThinkTime | (passive) | Idle→Walking 전 0.3~2s 멈춤 | thinkTimeMin, thinkTimeMax |
| IdleVariation | Update+Late | Hip bone 이동 + IdleVariation 트리거 | weightShiftInterval, microGestureInterval, hipShiftAmount |
| AccelerationCurve | Update | NavAgent.speed S-curve 가감속 | accelerationCurve, accelerationTime, decelerationTime |

## CommandRouter Action Mapping

| ActionId | Handler | FSM Transition |
|----------|---------|---------------|
| Character_MoveToLocation/WalkTo/RunTo | OnMoveToLocation | → Walking (ThinkTime 적용) |
| Character_Stop | OnStop | → Idle |
| Character_TurnTo | OnTurnTo | (회전만, 상태 유지) |
| Character_SitAtChair | OnSitAtChair | → SitTransition |
| Character_StandUp | OnStandUp | → StandTransition or Idle |
| Character_Idle | OnIdle | → Idle / delegate |
| Character_Lean | OnLean | → Arriving → Leaning |
| Character_ExamineMenu | OnExamineMenu | → Arriving → Looking |
| Character_LookAt | OnLookAt | (회전만, 상태 유지) |
| Character_PlayArcade | OnPlayArcade | → Arriving → PlayingArcade |
| Character_PlayClaw | OnPlayClaw | → Arriving → PlayingClaw |
| Camera_ChangeAngle | OnChangeCameraAngle | (CameraStateMachine) |

## Autonomous Behavior (IdleScheduler)

AI 서버 명령 없이 N초 Idle 유지 시 자율 행동 시작:

| Action | Weight | ActionId |
|--------|--------|----------|
| Wander | 40% | Character_MoveToLocation (NavMesh 랜덤) |
| Look Around | 20% | Character_TurnTo (랜덤 방향) |
| Sit | 15% | Character_SitAtChair (랜덤 의자) |
| Gesture | 15% | Character_Idle (standing) |
| Play Game | 10% | (placeholder → wander fallback) |

AI 서버 명령 수신 시 `CancelCurrentAction()` 즉시 중단.

## Setup

1. Scene에 `GolemBootstrap` 오브젝트 배치 (또는 Managers.Init() 시 자동 생성)
2. 캐릭터 GameObject에 컴포넌트 추가:
   - `GolemCharacterController`
   - `PointClickController`
   - `NavMeshAgent`
   - `Animator`
3. Inspector에서 할당:
   - `BehaviorConfigSO` (Assets > Create > Golem > BehaviorConfig)
   - `IdleSchedulerConfigSO` (Assets > Create > Golem > IdleSchedulerConfig)
   - Bone 참조: spineBone, headBone, hipBone
4. Scene 환경 태그:
   - `Caffee Chair` — 카페 의자
   - `Cafe Ad Display` — 광고판
   - `Slot Machine Chair` — 슬롯머신 의자
   - `Arcade` — 아케이드 기기
   - `Claw Machine` — 인형뽑기
   - `InterestPoint` — HeadLook 시선 대상

## Debug

| Key | UI | Content |
|-----|-----|---------|
| F12 | GolemDebugOverlay | 연결 상태, StateMachine, Character FSM, 메시지율, ActionBus 로그 |
| F11 | CharacterBehaviorDebugUI | FSM 현재/이전 상태, 5개 모듈 활성 상태 |

Unity Profiler 마커: `Golem.FSM.Update`, `Golem.Modules.Update`, `Golem.Module.Breathing` 등

## Implementation Status

| Phase | Status | Content |
|-------|--------|---------|
| 1. Character FSM | Done | 10 states, FSM engine, context blackboard |
| 2. Module Framework + Breathing | Done | IBehaviorModule, Registry, BreathingModule |
| 3. HeadLook + ThinkTime + IdleVar + Accel | Done | 4 behavior modules |
| 4. Command Router + Completion Tracker | Done | 13 commands routed, action lifecycle |
| 5. Autonomous Idle | Done | IdleScheduler, weighted random |
| 6. Debug & Polish | Done | F11/F12 overlays, ProfilerMarker |
| 7. Multi-Channel Behavior | Not started | 상체/하체 분리, 걸으면서 제스처 |
| 8. Animation Rigging | Not started | IK 기반 시선/호흡 (optional) |

## Dependencies

- Unity 6000.3.3f1
- NavMesh components (built-in)
- No external packages required
