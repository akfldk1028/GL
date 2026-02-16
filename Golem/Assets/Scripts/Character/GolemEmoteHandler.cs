using System;
using System.Collections.Generic;
using Golem.Infrastructure.Messages;
using UnityEngine;

/// <summary>
/// ActionBus-based emote handler. Subscribes to Agent_VoiceEmote, Agent_AnimatedEmote,
/// and Agent_FacialExpression. Works in parallel with existing EmotePlayer.
/// </summary>
public class GolemEmoteHandler : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private List<IDisposable> _subscriptions = new List<IDisposable>();

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        _subscriptions.Add(Managers.Subscribe(ActionId.Agent_VoiceEmote, OnVoiceEmote));
        _subscriptions.Add(Managers.Subscribe(ActionId.Agent_AnimatedEmote, OnAnimatedEmote));
        _subscriptions.Add(Managers.Subscribe(ActionId.Agent_FacialExpression, OnFacialExpression));

        Debug.Log("[GolemEmoteHandler] Subscribed to ActionBus emote events.");
    }

    private void OnDestroy()
    {
        foreach (var sub in _subscriptions)
            sub?.Dispose();
        _subscriptions.Clear();
    }

    private void OnVoiceEmote(ActionMessage msg)
    {
        if (msg.TryGetPayload<VoiceEmotePayload>(out var payload))
        {
            Debug.Log($"[GolemEmoteHandler] Voice emote received: {payload.Type}");
        }
    }

    private void OnAnimatedEmote(ActionMessage msg)
    {
        if (msg.TryGetPayload<AnimatedEmotePayload>(out var payload))
        {
            Debug.Log($"[GolemEmoteHandler] Animated emote: {payload.AnimationName}");
            if (animator != null && !string.IsNullOrEmpty(payload.AnimationName))
            {
                animator.SetTrigger(payload.AnimationName);
            }
        }
    }

    private void OnFacialExpression(ActionMessage msg)
    {
        if (msg.TryGetPayload<FacialExpressionPayload>(out var payload))
        {
            Debug.Log($"[GolemEmoteHandler] Facial expression: {payload.Expression} (intensity: {payload.Intensity})");
        }
    }
}
