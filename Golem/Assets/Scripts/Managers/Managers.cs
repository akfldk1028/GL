using System;
using Golem.Infrastructure.Messages;
using Golem.Infrastructure.State;
using UnityEngine;

/// <summary>
/// Singleton service locator providing global access to manager systems.
/// Owns ActionMessageBus, ActionDispatcher, StateMachine, and ActionTypeRegistry.
/// </summary>
public class Managers : MonoBehaviour
{
    private static Managers _instance;
    private static bool _applicationIsQuitting;

    // ── Sub-systems ────────────────────────────────────────────
    private ActionMessageBus _actionBus;
    private ActionDispatcher _actionDispatcher;
    private StateMachine _stateMachine;
    private ActionTypeRegistry _actionTypeRegistry;
    private AgentManager _agent;

    // ── Public accessors ───────────────────────────────────────
    public static Managers Instance
    {
        get
        {
            if (_applicationIsQuitting) return null;

            if (_instance == null)
            {
                _instance = FindObjectOfType<Managers>();
                if (_instance == null)
                {
                    var go = new GameObject("@Managers");
                    _instance = go.AddComponent<Managers>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    public static ActionMessageBus ActionBus => Instance?._actionBus;
    public static ActionDispatcher Dispatcher => Instance?._actionDispatcher;
    public static StateMachine State => Instance?._stateMachine;
    public static ActionTypeRegistry Registry => Instance?._actionTypeRegistry;
    public static AgentManager Agent => Instance?._agent;
    public static StateId CurrentStateId => Instance?._stateMachine?.CurrentId ?? StateId.None;

    // ── Lifecycle ──────────────────────────────────────────────

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Explicit initializer called by GolemBootstrap.
    /// Creates all sub-systems in the correct order.
    /// </summary>
    public static void Init()
    {
        var m = Instance;
        if (m == null) return;

        m._actionBus = new ActionMessageBus();
        m._actionDispatcher = new ActionDispatcher(m._actionBus);
        m._stateMachine = new StateMachine(m._actionBus);
        m._actionTypeRegistry = new ActionTypeRegistry();
        m._agent = new AgentManager();

        Debug.Log("[Managers] All sub-systems initialized.");
    }

    // ── Static convenience methods ─────────────────────────────

    public static void PublishAction(ActionId id)
    {
        Instance?._actionBus?.Publish(ActionMessage.From(id));
    }

    public static void PublishAction(ActionId id, IActionPayload payload)
    {
        Instance?._actionBus?.Publish(ActionMessage.From(id, payload));
    }

    public static IDisposable Subscribe(ActionId id, Action<ActionMessage> handler)
    {
        return Instance?._actionBus?.Subscribe(id, handler);
    }

    public static IDisposable Subscribe(ActionId id, Action handler)
    {
        return Instance?._actionBus?.Subscribe(id, handler);
    }

    public static void RegisterState(IState state)
    {
        Instance?._stateMachine?.RegisterState(state);
    }

    public static void SetState(StateId id)
    {
        Instance?._stateMachine?.SetState(id);
    }

    // ── Frame pumps ────────────────────────────────────────────

    private void Update()
    {
        _actionBus?.Publish(ActionMessage.From(ActionId.System_Update));
    }

    private void LateUpdate()
    {
        _actionBus?.Publish(ActionMessage.From(ActionId.System_LateUpdate));
    }

    private void FixedUpdate()
    {
        _actionBus?.Publish(ActionMessage.From(ActionId.System_FixedUpdate));
    }

    // ── Cleanup ────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _actionDispatcher?.Dispose();
            _stateMachine?.Dispose();
            _actionBus?.Dispose();
            _applicationIsQuitting = true;
        }
    }
}
