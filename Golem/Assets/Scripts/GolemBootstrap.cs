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

        // Step 4: Register initial states
        RegisterStates();
        Debug.Log("[GolemBootstrap] Step 4: States registered.");

        // Step 5: Set initial state
        Managers.SetState(StateId.Boot);
        Debug.Log("[GolemBootstrap] Step 5: Initial state set to Boot.");

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

    private void RegisterStates()
    {
        // Register basic states for the state machine
        Managers.RegisterState(new SimpleState(StateId.Boot));
        Managers.RegisterState(new SimpleState(StateId.Initializing));
        Managers.RegisterState(new SimpleState(StateId.Connected));
        Managers.RegisterState(new SimpleState(StateId.Disconnected));
        Managers.RegisterState(new SimpleState(StateId.Active));
        Managers.RegisterState(new SimpleState(StateId.Idle));
        Managers.RegisterState(new SimpleState(StateId.Performing));
    }

    /// <summary>
    /// Simple state implementation for initial registration.
    /// Logs Enter/Exit transitions. Can be replaced with proper state classes later.
    /// </summary>
    private class SimpleState : IState
    {
        public StateId Id { get; }

        public SimpleState(StateId id) { Id = id; }

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
            return actionId == Golem.Infrastructure.Messages.ActionId.Agent_Connected ||
                   actionId == Golem.Infrastructure.Messages.ActionId.Agent_Disconnected;
        }

        public void Handle(Golem.Infrastructure.Messages.ActionMessage message)
        {
            if (message.Id == Golem.Infrastructure.Messages.ActionId.Agent_Connected)
            {
                Managers.SetState(StateId.Connected);
            }
            else if (message.Id == Golem.Infrastructure.Messages.ActionId.Agent_Disconnected)
            {
                Managers.SetState(StateId.Disconnected);
            }
        }
    }
}
