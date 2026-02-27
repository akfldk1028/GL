using System;
using System.Collections;
using System.Collections.Generic;
using Golem.Infrastructure.Messages;
using UnityEngine;

/// <summary>
/// Handles Composite_MultiAction (parallel) and Composite_Sequence (sequential) execution.
/// Based on BML synchronization patterns and Generative Agents plan decomposition.
///
/// MultiAction: publishes all sub-actions immediately (parallel).
/// Sequence:    publishes one sub-action at a time, waits for Agent_ActionCompleted
///              before publishing the next.
/// </summary>
public class CompositeActionExecutor : MonoBehaviour
{
    private List<IDisposable> _subscriptions = new List<IDisposable>();

    private void Start()
    {
        _subscriptions.Add(Managers.Subscribe(ActionId.Composite_MultiAction, OnMultiAction));
        _subscriptions.Add(Managers.Subscribe(ActionId.Composite_Sequence, OnSequence));

        Debug.Log("[CompositeActionExecutor] Subscribed to composite action events.");
    }

    private void OnDestroy()
    {
        foreach (var sub in _subscriptions)
            sub?.Dispose();
        _subscriptions.Clear();
        StopAllCoroutines();
    }

    private void OnMultiAction(ActionMessage msg)
    {
        if (!msg.TryGetPayload<MultiActionPayload>(out var payload)) return;

        foreach (var sub in payload.Actions)
        {
            DispatchSubAction(sub);
        }

        Debug.Log($"[CompositeActionExecutor] MultiAction dispatched {payload.Actions.Count} sub-actions in parallel.");
    }

    private void OnSequence(ActionMessage msg)
    {
        if (!msg.TryGetPayload<SequencePayload>(out var payload)) return;
        if (payload.Actions.Count == 0) return;

        StartCoroutine(ExecuteSequence(payload.Actions));
    }

    private IEnumerator ExecuteSequence(List<SubAction> actions)
    {
        for (int i = 0; i < actions.Count; i++)
        {
            var sub = actions[i];
            bool completed = false;
            bool dispatched = false;

            // Resolve the ActionId for correlation matching
            var registry = Managers.Registry;
            var expectedActionId = registry != null ? registry.MapToActionId(sub.Type) : ActionId.None;

            // Subscribe to ActionCompleted — only match AFTER dispatch to avoid stale completions
            IDisposable completionSub = Managers.Subscribe(ActionId.Agent_ActionCompleted, (ActionMessage m) =>
            {
                if (!dispatched) return;

                if (m.TryGetPayload<ActionLifecyclePayload>(out var lifecycle))
                {
                    if (lifecycle.SourceAction == expectedActionId ||
                        string.Equals(lifecycle.ActionName, sub.Type, System.StringComparison.OrdinalIgnoreCase))
                    {
                        completed = true;
                    }
                }
                else
                {
                    completed = true;
                }
            });

            DispatchSubAction(sub);
            dispatched = true;
            Debug.Log($"[CompositeActionExecutor] Sequence step {i + 1}/{actions.Count}: {sub.Type}");

            // Wait for completion or timeout (30 seconds max per step)
            float elapsed = 0f;
            while (!completed && elapsed < 30f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            completionSub?.Dispose();

            if (!completed)
            {
                Debug.LogWarning($"[CompositeActionExecutor] Sequence step {i + 1} timed out: {sub.Type}. Aborting sequence.");
                Managers.PublishAction(ActionId.Agent_ActionFailed, new ActionLifecyclePayload
                {
                    SourceAction = expectedActionId,
                    ActionName = sub.Type,
                    Success = false
                });
                yield break;
            }
        }

        Debug.Log("[CompositeActionExecutor] Sequence complete.");
    }

    private void DispatchSubAction(SubAction sub)
    {
        if (sub == null || string.IsNullOrEmpty(sub.Type)) return;

        var registry = Managers.Registry;
        if (registry == null) return;

        var actionId = registry.MapToActionId(sub.Type);
        if (actionId == ActionId.None)
        {
            Debug.LogWarning($"[CompositeActionExecutor] Unknown sub-action type: {sub.Type}");
            return;
        }

        var payload = registry.CreatePayload(actionId, sub.Parameters);
        Managers.PublishAction(actionId, payload);
    }
}
