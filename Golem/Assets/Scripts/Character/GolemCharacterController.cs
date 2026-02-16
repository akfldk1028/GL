using System;
using System.Collections.Generic;
using Golem.Infrastructure.Messages;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ActionBus-based character controller. Subscribes to Character_* and Camera_* ActionIds
/// and drives PointClickController directly (no SendMessage).
/// </summary>
public class GolemCharacterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PointClickController pointClick;
    [SerializeField] private CameraStateMachine cameraStateMachine;
    [SerializeField] private Animator animator;

    private NavMeshAgent _navAgent;
    private List<IDisposable> _subscriptions = new List<IDisposable>();

    private void Start()
    {
        // Auto-find references if not assigned
        if (pointClick == null)
            pointClick = GetComponent<PointClickController>() ?? FindObjectOfType<PointClickController>();

        if (cameraStateMachine == null)
        {
            var cam = Camera.main;
            if (cam != null)
                cameraStateMachine = cam.GetComponent<CameraStateMachine>();
        }

        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        _navAgent = GetComponent<NavMeshAgent>();

        // ── Locomotion ──
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_MoveToLocation, OnMoveToLocation));
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_WalkTo, OnMoveToLocation)); // same handler
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_RunTo, OnMoveToLocation));   // same handler
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_Stop, OnStop));
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_TurnTo, OnTurnTo));

        // ── Posture ──
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_SitAtChair, OnSitAtChair));
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_StandUp, OnStandUp));
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_Idle, OnIdle));
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_Lean, OnLean));

        // ── Interaction ──
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_ExamineMenu, OnExamineMenu));
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_LookAt, OnLookAt));
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_PlayArcade, OnPlayArcade));
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_PlayClaw, OnPlayClaw));

        // ── Camera ──
        _subscriptions.Add(Managers.Subscribe(ActionId.Camera_ChangeAngle, OnChangeCameraAngle));

        Debug.Log("[GolemCharacterController] Subscribed to ActionBus character events.");
    }

    private void OnDestroy()
    {
        foreach (var sub in _subscriptions)
            sub?.Dispose();
        _subscriptions.Clear();
    }

    // ── Locomotion Handlers ────────────────────────────────────

    private void OnMoveToLocation(ActionMessage msg)
    {
        if (pointClick == null) return;
        if (msg.TryGetPayload<MoveToLocationPayload>(out var payload))
        {
            if (payload.Destination != Vector3.zero)
            {
                pointClick.MoveToPointPublic(payload.Destination);
                return;
            }

            var target = FindTransformByNameContains(payload.Location);
            Vector3 dest = target != null ? target.position : transform.position;
            pointClick.MoveToPointPublic(dest);
        }
    }

    private void OnStop(ActionMessage msg)
    {
        if (_navAgent != null && _navAgent.enabled)
            _navAgent.ResetPath();
    }

    private void OnTurnTo(ActionMessage msg)
    {
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
    }

    // ── Posture Handlers ───────────────────────────────────────

    private void OnSitAtChair(ActionMessage msg)
    {
        if (pointClick == null) return;
        if (msg.TryGetPayload<SitAtChairPayload>(out var payload))
        {
            GameObject[] chairs = null;
            try { chairs = GameObject.FindGameObjectsWithTag("Caffee Chair"); } catch { }
            if (chairs != null && chairs.Length > 0)
            {
                int idx = Mathf.Clamp(payload.ChairNumber - 1, 0, chairs.Length - 1);
                var chosen = chairs[idx];
                var interaction = chosen.transform.Find("InteractionSpot");
                pointClick.SitAtInteractionSpot(interaction != null ? interaction : chosen.transform);
            }
            else
            {
                Debug.LogWarning("[GolemCharacterController] No chairs found with tag 'Caffee Chair'.");
            }
        }
    }

    private void OnStandUp(ActionMessage msg)
    {
        if (pointClick == null) return;
        pointClick.ForceStandUp();
    }

    private void OnIdle(ActionMessage msg)
    {
        if (pointClick == null) return;
        if (msg.TryGetPayload<IdlePayload>(out var payload))
        {
            string idleType = payload.IdleType ?? "standing";
            if (idleType == "standing")
                pointClick.ForceStandUp();
            else if (idleType == "sitting")
                OnSitAtChair(ActionMessage.From(ActionId.Character_SitAtChair, new SitAtChairPayload { ChairNumber = 1 }));
            else if (idleType == "leaning")
                OnLean(msg);
        }
    }

    private void OnLean(ActionMessage msg)
    {
        if (pointClick == null) return;
        GameObject[] chairs = null;
        try { chairs = GameObject.FindGameObjectsWithTag("Slot Machine Chair"); } catch { }
        if (chairs != null && chairs.Length > 0)
        {
            var chosen = chairs[0];
            var interaction = chosen.transform.Find("InteractionSpot");
            pointClick.SitAtInteractionSpot(interaction != null ? interaction : chosen.transform);
        }
        else
        {
            Debug.LogWarning("[GolemCharacterController] No slot machine chairs found.");
        }
    }

    // ── Interaction Handlers ───────────────────────────────────

    private void OnExamineMenu(ActionMessage msg)
    {
        if (pointClick == null) return;
        GameObject[] ads = null;
        try { ads = GameObject.FindGameObjectsWithTag("Cafe Ad Display"); } catch { }
        if (ads != null && ads.Length > 0)
        {
            var chosen = ads[0];
            var interaction = chosen.transform.Find("InteractionSpot");
            pointClick.ExamineAtInteractionSpot(interaction != null ? interaction : chosen.transform);
        }
        else
        {
            Debug.LogWarning("[GolemCharacterController] No ad displays found with tag 'Cafe Ad Display'.");
        }
    }

    private void OnLookAt(ActionMessage msg)
    {
        if (msg.TryGetPayload<GazePayload>(out var payload))
        {
            Vector3 target = payload.Position;
            if (target == Vector3.zero && !string.IsNullOrEmpty(payload.Target))
            {
                var t = FindTransformByNameContains(payload.Target);
                if (t != null) target = t.position;
            }
            if (target != Vector3.zero)
                transform.LookAt(target);
        }
    }

    private void OnPlayArcade(ActionMessage msg)
    {
        if (pointClick == null) return;
        GameObject[] arcades = null;
        try { arcades = GameObject.FindGameObjectsWithTag("Arcade"); } catch { }
        if (arcades != null && arcades.Length > 0)
        {
            var chosen = arcades[0];
            var interaction = chosen.transform.Find("InteractionSpot");
            pointClick.PlayArcadeAtSpot(interaction != null ? interaction : chosen.transform);
        }
        else
        {
            Debug.LogWarning("[GolemCharacterController] No arcades found with tag 'Arcade'.");
        }
    }

    private void OnPlayClaw(ActionMessage msg)
    {
        if (pointClick == null) return;
        GameObject[] claws = null;
        try { claws = GameObject.FindGameObjectsWithTag("Claw Machine"); } catch { }
        if (claws != null && claws.Length > 0)
        {
            var chosen = claws[0];
            var interaction = chosen.transform.Find("InteractionSpot");
            pointClick.PlayClawAtSpot(interaction != null ? interaction : chosen.transform);
        }
        else
        {
            Debug.LogWarning("[GolemCharacterController] No claw machines found.");
        }
    }

    // ── Camera Handler ─────────────────────────────────────────

    private void OnChangeCameraAngle(ActionMessage msg)
    {
        if (cameraStateMachine == null) return;
        if (msg.TryGetPayload<ChangeCameraAnglePayload>(out var payload))
        {
            var stateSO = Resources.Load<CameraStateSO>($"CameraStates/{payload.Angle}");
            if (stateSO != null)
            {
                cameraStateMachine.ChangeState(stateSO);
            }
            else
            {
                Debug.LogWarning($"[GolemCharacterController] CameraStateSO not found: CameraStates/{payload.Angle}");
            }
        }
    }

    // ── Helpers ─────────────────────────────────────────────────

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
