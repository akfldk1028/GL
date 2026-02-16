using System;
using Golem.Infrastructure.Messages;
using Golem.Infrastructure.State;
using UnityEngine;

public class Managers : MonoBehaviour
{
    public static bool Initialized { get; private set; } = false;

    private static Managers s_instance;
    private static Managers Instance
    {
        get
        {
            if (s_instance == null)
                Init();
            return s_instance;
        }
    }

    #region Core
    private readonly ActionMessageBus _actionBus = new ActionMessageBus();
    private ActionDispatcher _actionDispatcher;
    private StateMachine _stateMachine;

    private DataManager _data = new DataManager();
    private PoolManager _pool = new PoolManager();
    private ResourceManager _resource = new ResourceManager();
    private AgentManager _agent = new AgentManager();

    public static ActionMessageBus ActionBus => Instance?._actionBus;
    public static StateMachine StateMachine => Instance?._stateMachine;
    public static DataManager Data => Instance?._data;
    public static PoolManager Pool => Instance?._pool;
    public static ResourceManager Resource => Instance?._resource;
    public static AgentManager Agent => Instance?._agent;
    #endregion

    public static void Init()
    {
        if (s_instance == null && !Initialized)
        {
            Initialized = true;

            GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }

            DontDestroyOnLoad(go);
            s_instance = go.GetComponent<Managers>();
        }
    }

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);

        // Infrastructure init
        Debug.Log("[Managers] Initializing Infrastructure...");
        if (_actionDispatcher == null)
        {
            _actionDispatcher = new ActionDispatcher(_actionBus);
            Debug.Log("[Managers] ActionDispatcher created");
        }

        if (_stateMachine == null)
        {
            _stateMachine = new StateMachine(_actionBus);
            Debug.Log("[Managers] StateMachine created");
        }
    }

    private void Update() => PublishAction(ActionId.System_Update);
    private void LateUpdate() => PublishAction(ActionId.System_LateUpdate);
    private void FixedUpdate() => PublishAction(ActionId.System_FixedUpdate);

    private void OnDestroy()
    {
        _stateMachine?.Dispose();
        _actionDispatcher?.Dispose();
        _actionBus?.Dispose();
    }

    #region Static Helpers
    public static IDisposable Subscribe(ActionId actionId, Action handler)
        => ActionBus?.Subscribe(actionId, handler);

    public static IDisposable Subscribe(ActionId actionId, Action<ActionMessage> handler)
        => ActionBus?.Subscribe(actionId, handler);

    public static IDisposable SubscribeMultiple(Action<ActionMessage> handler, params ActionId[] actionIds)
        => ActionBus?.Subscribe(handler, actionIds);

    public static void RegisterAction(IAction action)
        => Instance?._actionDispatcher?.Register(action);

    public static void UnregisterAction(IAction action)
        => Instance?._actionDispatcher?.Unregister(action);

    public static void PublishAction(ActionId actionId)
        => ActionBus?.Publish(ActionMessage.From(actionId));

    public static void PublishAction(ActionId actionId, IActionPayload payload)
        => ActionBus?.Publish(ActionMessage.From(actionId, payload));

    public static void RegisterState(IState state)
        => Instance?._stateMachine?.RegisterState(state);

    public static void SetState(StateId stateId)
        => Instance?._stateMachine?.SetState(stateId);

    public static StateId CurrentStateId
        => Instance?._stateMachine?.CurrentId ?? StateId.None;
    #endregion
}
