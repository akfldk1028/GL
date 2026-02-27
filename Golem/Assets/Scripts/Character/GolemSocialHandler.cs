using System;
using System.Collections;
using System.Collections.Generic;
using Golem.Infrastructure.Messages;
using UnityEngine;

/// <summary>
/// Social action handler — Greet, Wave, Nod, HeadShake, Point.
/// Based on BML speech/gesture + VirtualHome GREET + SmartBody head controller.
/// Drives Animator triggers and optional IK targeting.
/// </summary>
public class GolemSocialHandler : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private List<IDisposable> _subscriptions = new List<IDisposable>();

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        _subscriptions.Add(Managers.Subscribe(ActionId.Social_Greet, OnGreet));
        _subscriptions.Add(Managers.Subscribe(ActionId.Social_Wave, OnWave));
        _subscriptions.Add(Managers.Subscribe(ActionId.Social_Nod, OnNod));
        _subscriptions.Add(Managers.Subscribe(ActionId.Social_HeadShake, OnHeadShake));
        _subscriptions.Add(Managers.Subscribe(ActionId.Social_Point, OnPoint));

        Debug.Log("[GolemSocialHandler] Subscribed to ActionBus social events.");
    }

    private void OnDestroy()
    {
        foreach (var sub in _subscriptions)
            sub?.Dispose();
        _subscriptions.Clear();
    }

    private void OnGreet(ActionMessage msg)
    {
        if (animator == null) return;
        animator.SetTrigger("Greet");
        Debug.Log("[GolemSocialHandler] Greet");
        StartCoroutine(DelayedCompletion(ActionId.Social_Greet, "greet", 2f));
    }

    private void OnWave(ActionMessage msg)
    {
        if (animator == null) return;
        animator.SetTrigger("Wave");
        Debug.Log("[GolemSocialHandler] Wave");
        StartCoroutine(DelayedCompletion(ActionId.Social_Wave, "wave", 2f));
    }

    private void OnNod(ActionMessage msg)
    {
        if (animator == null) return;
        animator.SetTrigger("Nod");
        Debug.Log("[GolemSocialHandler] Nod");
        StartCoroutine(DelayedCompletion(ActionId.Social_Nod, "nod", 1.5f));
    }

    private void OnHeadShake(ActionMessage msg)
    {
        if (animator == null) return;
        animator.SetTrigger("HeadShake");
        Debug.Log("[GolemSocialHandler] HeadShake");
        StartCoroutine(DelayedCompletion(ActionId.Social_HeadShake, "headShake", 1.5f));
    }

    private void OnPoint(ActionMessage msg)
    {
        if (animator == null) return;

        // Optional: look toward the target
        if (msg.TryGetPayload<GazePayload>(out var payload))
        {
            Vector3 target = payload.Position;
            if (target == Vector3.zero && !string.IsNullOrEmpty(payload.Target))
            {
                var t = FindTransformByNameContains(payload.Target);
                if (t != null) target = t.position;
            }
            if (target != Vector3.zero)
            {
                Vector3 dir = (target - transform.position);
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        animator.SetTrigger("Point");
        Debug.Log("[GolemSocialHandler] Point");
        StartCoroutine(DelayedCompletion(ActionId.Social_Point, "point", 2f));
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

    private Transform FindTransformByNameContains(string namePart)
    {
        if (string.IsNullOrEmpty(namePart)) return null;
        var all = FindObjectsOfType<Transform>();
        namePart = namePart.ToLower();
        foreach (var t in all)
        {
            if (t == null || t.gameObject == null) continue;
            if (t.name != null && t.name.ToLower().Contains(namePart))
                return t;
        }
        return null;
    }
}
