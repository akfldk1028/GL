using Golem.Character.FSM;
using Golem.Character.FSM.States;
using Golem.Character.Modules;
using Golem.Character.Modules.Impl;
using Golem.Infrastructure.Messages;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Slim orchestrator: creates FSM, registers modules, delegates command handling to Router.
/// </summary>
public class GolemCharacterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PointClickController pointClick;
    [SerializeField] private CameraStateMachine cameraStateMachine;
    [SerializeField] private Animator animator;

    [Header("Behavior Modules")]
    [SerializeField] private BehaviorConfigSO behaviorConfig;
    [SerializeField] private Transform spineBone;
    [SerializeField] private Transform headBone;
    [SerializeField] private Transform hipBone;

    [Header("Autonomous")]
    [SerializeField] private Golem.Character.Autonomous.IdleSchedulerConfigSO idleSchedulerConfig;

    private NavMeshAgent _navAgent;
    private CharacterBehaviorFSM _fsm;
    private CharacterStateContext _stateContext;
    private BehaviorModuleRegistry _moduleRegistry;
    private CharacterCommandRouter _router;
    private CharacterActionCompletionTracker _tracker;
    private Golem.Character.Autonomous.IdleScheduler _idleScheduler;
    private System.IDisposable _cameraSub;

    private void Start()
    {
        if (pointClick == null)
            pointClick = GetComponent<PointClickController>() ?? FindObjectOfType<PointClickController>();
        if (cameraStateMachine == null)
        {
            var cam = Camera.main;
            if (cam != null) cameraStateMachine = cam.GetComponent<CameraStateMachine>();
        }
        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        _navAgent = GetComponent<NavMeshAgent>();

        // Disable legacy mouse+boolean logic in PointClickController
        if (pointClick != null)
            pointClick.fsmActive = true;

        // FSM
        _stateContext = new CharacterStateContext
        {
            PointClick = pointClick, Animator = animator,
            NavAgent = _navAgent, CharacterTransform = transform
        };
        _fsm = new CharacterBehaviorFSM(_stateContext);
        _fsm.RegisterState(new IdleState());          _fsm.RegisterState(new WalkingState());
        _fsm.RegisterState(new ArrivingState());       _fsm.RegisterState(new SitTransitionState());
        _fsm.RegisterState(new SittingState());        _fsm.RegisterState(new StandTransitionState());
        _fsm.RegisterState(new LookingState());        _fsm.RegisterState(new LeaningState());
        _fsm.RegisterState(new PlayingArcadeState());  _fsm.RegisterState(new PlayingClawState());
        _fsm.ForceTransition(CharacterStateId.Idle);

        // Modules
        _moduleRegistry = new BehaviorModuleRegistry();
        _moduleRegistry.Initialize(new BehaviorModuleContext
        {
            Animator = animator, NavAgent = _navAgent,
            CharacterTransform = transform, FSM = _fsm,
            SpineBone = spineBone, HeadBone = headBone, HipBone = hipBone,
            Config = behaviorConfig
        });
        _moduleRegistry.Register(new BreathingModule());
        _moduleRegistry.Register(new HeadLookModule());
        _moduleRegistry.Register(new ThinkTimeModule());
        _moduleRegistry.Register(new IdleVariationModule());
        _moduleRegistry.Register(new AccelerationCurveModule());

        // Router + Tracker
        _router = new CharacterCommandRouter(_fsm, _stateContext, pointClick, _moduleRegistry, this);
        _tracker = new CharacterActionCompletionTracker(_fsm);

        // Camera
        _cameraSub = Managers.Subscribe(ActionId.Camera_ChangeAngle, OnChangeCameraAngle);

        // Autonomous idle
        if (idleSchedulerConfig != null)
        {
            _idleScheduler = new Golem.Character.Autonomous.IdleScheduler(_fsm, idleSchedulerConfig, this, transform);
            _router.SetIdleScheduler(_idleScheduler);
            _idleScheduler.Start();
        }

        Debug.Log("[GolemCharacterController] FSM + Modules + Router + IdleScheduler initialized.");
    }

    private void Update()
    {
        _fsm?.Update();
        _moduleRegistry?.UpdateAll(Time.deltaTime);
    }

    private void LateUpdate()
    {
        _moduleRegistry?.LateUpdateAll(Time.deltaTime);
    }

    private void OnDestroy()
    {
        _idleScheduler?.Stop();
        _router?.Dispose();
        _tracker?.Dispose();
        _cameraSub?.Dispose();
        _moduleRegistry?.Dispose();
    }

    private void OnChangeCameraAngle(ActionMessage msg)
    {
        if (cameraStateMachine == null) return;
        if (msg.TryGetPayload<ChangeCameraAnglePayload>(out var p))
        {
            var stateSO = Resources.Load<CameraStateSO>($"CameraStates/{p.Angle}");
            if (stateSO != null) cameraStateMachine.ChangeState(stateSO);
        }
        Managers.PublishAction(ActionId.Agent_ActionCompleted, new ActionLifecyclePayload
        {
            SourceAction = ActionId.Camera_ChangeAngle,
            ActionName = "changeCameraAngle",
            Success = true
        });
    }

    // Public accessors for debug UI
    public CharacterBehaviorFSM FSM => _fsm;
    public BehaviorModuleRegistry ModuleRegistry => _moduleRegistry;
}
