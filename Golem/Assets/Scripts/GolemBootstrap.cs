using System.Collections.Generic;
using Golem.Infrastructure.State;
using UnityEngine;

/// <summary>
/// Entry point for the Golem AI Agent system.
/// Initializes all infrastructure in the correct order:
/// 1. Managers (ActionBus, ActionDispatcher, StateMachine, ResourceManager, PoolManager, DataManager)
/// 2. AINetworkManager (CFConnector -> ActionBus bridge)
/// 3. Debug Overlay (development builds only)
/// 4. State Machine initial states
///
/// Place this script on a GameObject in your startup scene, or it will auto-create itself.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GolemBootstrap : MonoBehaviour
{
    private static bool _bootstrapped = false;

    private void Awake()
    {
        if (_bootstrapped)
        {
            Debug.Log("[GolemBootstrap] Already bootstrapped. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        _bootstrapped = true;

        Debug.Log("[GolemBootstrap] === Golem AI Agent System Initializing ===");

        // Step 1: Initialize Managers (creates @Managers GameObject)
        Managers.Init();
        Debug.Log("[GolemBootstrap] Step 1: Managers initialized.");

        // Step 2: Create AINetworkManager (bridges CFConnector -> ActionBus)
        CreateAINetworkManager();
        Debug.Log("[GolemBootstrap] Step 2: AINetworkManager created.");

        // Step 3: Create Debug Overlay (development builds only)
        CreateDebugOverlay();

        // Step 4: Create CompositeActionExecutor (BML sync patterns)
        CreateCompositeExecutor();
        Debug.Log("[GolemBootstrap] Step 4: CompositeActionExecutor created.");

        // Step 5: Create GolemActionGate (disable legacy controller)
        CreateActionGate();
        Debug.Log("[GolemBootstrap] Step 5: GolemActionGate created.");

        // Step 6: Register initial states
        RegisterStates();
        Debug.Log("[GolemBootstrap] Step 6: States registered.");

        // Step 7: Set initial state
        Managers.SetState(StateId.Boot);
        Debug.Log("[GolemBootstrap] Step 7: Initial state set to Boot.");

        Debug.Log("[GolemBootstrap] === Golem AI Agent System Ready ===");
    }

    private void CreateAINetworkManager()
    {
        if (AINetworkManager.Instance != null)
        {
            Debug.Log("[GolemBootstrap] AINetworkManager already exists.");
            return;
        }

        var go = new GameObject("@AINetworkManager");
        DontDestroyOnLoad(go);
        go.AddComponent<AINetworkManager>();
    }

    private void CreateDebugOverlay()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var go = new GameObject("@GolemDebugOverlay");
        DontDestroyOnLoad(go);
        go.AddComponent<GolemDebugOverlay>();
        Debug.Log("[GolemBootstrap] Step 3: Debug overlay created (F12 to toggle).");
#else
        Debug.Log("[GolemBootstrap] Step 3: Debug overlay skipped (release build).");
#endif
    }

    private void CreateCompositeExecutor()
    {
        var go = new GameObject("@CompositeActionExecutor");
        DontDestroyOnLoad(go);
        go.AddComponent<CompositeActionExecutor>();
    }

    private void CreateActionGate()
    {
        var go = new GameObject("@GolemActionGate");
        DontDestroyOnLoad(go);
        go.AddComponent<GolemActionGate>();
    }

    private void RegisterStates()
    {
        // States that transition to Connected on Agent_Connected
        var onConnected = new Dictionary<Golem.Infrastructure.Messages.ActionId, StateId>
        {
            { Golem.Infrastructure.Messages.ActionId.Agent_Connected, StateId.Connected }
        };

        // States that transition to Disconnected on Agent_Disconnected
        var onDisconnected = new Dictionary<Golem.Infrastructure.Messages.ActionId, StateId>
        {
            { Golem.Infrastructure.Messages.ActionId.Agent_Disconnected, StateId.Disconnected }
        };

        Managers.RegisterState(new SimpleState(StateId.Boot, onConnected));
        Managers.RegisterState(new SimpleState(StateId.Initializing, onConnected));
        Managers.RegisterState(new SimpleState(StateId.Disconnected, onConnected));
        Managers.RegisterState(new SimpleState(StateId.Connected, onDisconnected));
        Managers.RegisterState(new SimpleState(StateId.Active, onDisconnected));
        Managers.RegisterState(new SimpleState(StateId.Idle, onDisconnected));
        Managers.RegisterState(new SimpleState(StateId.Performing, onDisconnected));
    }

    /// <summary>
    /// Simple state with configurable transitions.
    /// Each state only handles the ActionIds specified in its transitions dictionary.
    /// </summary>
    private class SimpleState : IState
    {
        public StateId Id { get; }
        private readonly Dictionary<Golem.Infrastructure.Messages.ActionId, StateId> _transitions;

        public SimpleState(StateId id, Dictionary<Golem.Infrastructure.Messages.ActionId, StateId> transitions)
        {
            Id = id;
            _transitions = transitions;
        }

        public void Enter()
        {
            Debug.Log($"[StateMachine] Entered state: {Id}");
        }

        public void Exit()
        {
            Debug.Log($"[StateMachine] Exited state: {Id}");
        }

        public bool CanHandle(Golem.Infrastructure.Messages.ActionId actionId)
        {
            return _transitions.ContainsKey(actionId);
        }

        public void Handle(Golem.Infrastructure.Messages.ActionMessage message)
        {
            if (_transitions.TryGetValue(message.Id, out var targetState))
            {
                Managers.SetState(targetState);
            }
        }
    }
}
