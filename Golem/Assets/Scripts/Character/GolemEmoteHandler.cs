using System;
using System.Collections;
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
            // Voice audio playback is handled by existing EmotePlayer via CFConnector.OnVoiceEmote
            // This handler can be extended for additional voice-related logic (lip sync, etc.)
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
            float duration = payload.AnimationDuration > 0f ? payload.AnimationDuration : 2f;
            StartCoroutine(DelayedCompletion(ActionId.Agent_AnimatedEmote, "animatedEmote", duration));
        }
    }

    private void OnFacialExpression(ActionMessage msg)
    {
        if (msg.TryGetPayload<FacialExpressionPayload>(out var payload))
        {
            Debug.Log($"[GolemEmoteHandler] Facial expression: {payload.Expression} (intensity: {payload.Intensity})");
        }
        // Facial expressions are instant — publish completion immediately
        Managers.PublishAction(ActionId.Agent_ActionCompleted, new ActionLifecyclePayload
        {
            SourceAction = ActionId.Agent_FacialExpression,
            ActionName = "facialExpression",
            Success = true
        });
    }

    private IEnumerator DelayedCompletion(ActionId source, string name, float delay)
    {
        yield return new WaitForSeconds(delay);
        Managers.PublishAction(ActionId.Agent_ActionCompleted, new ActionLifecyclePayload
        {
            SourceAction = source,
            ActionName = name,
            Success = true
        });
    }
}
