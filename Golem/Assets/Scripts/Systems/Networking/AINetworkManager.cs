using System;
using System.Collections.Generic;
using Golem.Infrastructure.Messages;
using UnityEngine;

public class AINetworkManager : MonoBehaviour
{
    public static AINetworkManager Instance { get; private set; }

    private CFConnector _connector;
    private bool _isConnected;

    public static bool IsConnected => Instance != null && Instance._isConnected;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _connector = CFConnector.instance;
        if (_connector == null)
        {
            Debug.LogWarning("[AINetworkManager] CFConnector.instance is null. Will retry on Update.");
            return;
        }
        SubscribeToEvents();
    }

    private void Update()
    {
        if (_connector == null)
        {
            _connector = CFConnector.instance;
            if (_connector != null)
                SubscribeToEvents();
        }
    }

    private void SubscribeToEvents()
    {
        Debug.Log("[AINetworkManager] Subscribing to CFConnector events...");

        _connector.OnOpen += HandleOpen;
        _connector.OnClose += HandleClose;
        _connector.OnError += HandleError;
        _connector.OnAgentState += HandleAgentState;
        _connector.OnVoiceEmote += HandleVoiceEmote;
        _connector.OnAnimatedEmote += HandleAnimatedEmote;
        _connector.OnFacialExpression += HandleFacialExpression;
        _connector.OnCharacterAction += HandleCharacterAction;

        Debug.Log("[AINetworkManager] Subscribed to all CFConnector events.");
    }

    private void OnDestroy()
    {
        if (_connector != null)
        {
            try { _connector.OnOpen -= HandleOpen; } catch { }
            try { _connector.OnClose -= HandleClose; } catch { }
            try { _connector.OnError -= HandleError; } catch { }
            try { _connector.OnAgentState -= HandleAgentState; } catch { }
            try { _connector.OnVoiceEmote -= HandleVoiceEmote; } catch { }
            try { _connector.OnAnimatedEmote -= HandleAnimatedEmote; } catch { }
            try { _connector.OnFacialExpression -= HandleFacialExpression; } catch { }
            try { _connector.OnCharacterAction -= HandleCharacterAction; } catch { }
        }
    }

    // --- Event Handlers ---

    private void HandleOpen()
    {
        _isConnected = true;
        Debug.Log("[AINetworkManager] Connected to AI server");
        Managers.PublishAction(ActionId.Agent_Connected);
    }

    private void HandleClose()
    {
        _isConnected = false;
        Debug.Log("[AINetworkManager] Disconnected from AI server");
        Managers.PublishAction(ActionId.Agent_Disconnected);
    }

    private void HandleError(Exception ex)
    {
        Debug.LogError($"[AINetworkManager] Error: {ex.Message}");
        Managers.PublishAction(ActionId.Agent_Error, new AgentErrorPayload
        {
            ErrorMessage = ex.Message,
            Exception = ex.ToString()
        });
    }

    private void HandleAgentState(CFConnector.AgentState state)
    {
        if (state == null) return;
        Managers.PublishAction(ActionId.Agent_StateChanged, new AgentStatePayload
        {
            Status = state.state?.status ?? "unknown",
            LastActionType = state.lastActionType ?? "",
            RoutinesRunning = state.routinesRunning
        });
    }

    private void HandleVoiceEmote(CFConnector.VoiceEmoteData data)
    {
        if (data == null) return;
        Managers.PublishAction(ActionId.Agent_VoiceEmote, new VoiceEmotePayload
        {
            Type = data.type,
            AudioBase64 = data.audioBase64
        });
    }

    private void HandleAnimatedEmote(CFConnector.AnimatedEmoteData data)
    {
        if (data == null) return;
        Managers.PublishAction(ActionId.Agent_AnimatedEmote, new AnimatedEmotePayload
        {
            Type = data.type,
            AudioBase64 = data.audioBase64,
            AnimationName = data.animation?.name ?? "",
            AnimationDuration = data.animation?.duration ?? 0f
        });
    }

    private void HandleFacialExpression(CFConnector.FacialExpressionData data)
    {
        if (data == null) return;
        Managers.PublishAction(ActionId.Agent_FacialExpression, new FacialExpressionPayload
        {
            Expression = data.expression,
            Intensity = data.intensity
        });
    }

    private void HandleCharacterAction(CFConnector.CharacterActionData data)
    {
        if (data == null || data.action == null) return;

        string actionType = data.action.type ?? "";
        var parameters = data.action.parameters;
        var registry = Managers.Registry;

        if (registry == null)
        {
            Debug.LogWarning("[AINetworkManager] ActionTypeRegistry not initialized.");
            return;
        }

        var actionId = registry.MapToActionId(actionType);

        if (actionId == ActionId.None)
        {
            Debug.LogWarning($"[AINetworkManager] Unknown action type: {actionType}");
            Managers.PublishAction(ActionId.None, new CharacterActionPayload
            {
                ActionType = actionType,
                Parameters = parameters
            });
            return;
        }

        var payload = registry.CreatePayload(actionId, parameters);
        Managers.PublishAction(actionId, payload);

        Debug.Log($"[AINetworkManager] Character action published: {actionType} → {actionId}");
    }
}
