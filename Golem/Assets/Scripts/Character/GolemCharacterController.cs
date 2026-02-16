using System;
using System.Collections.Generic;
using Golem.Infrastructure.Messages;
using UnityEngine;

/// <summary>
/// ActionBus-based character controller. Subscribes to Character_* ActionIds
/// and drives PointClickController directly (no SendMessage).
/// Works in parallel with existing CharacterActionController.
/// </summary>
public class GolemCharacterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PointClickController pointClick;
    [SerializeField] private CameraStateMachine cameraStateMachine;

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

        // Subscribe to ActionBus
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_MoveToLocation, OnMoveToLocation));
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_SitAtChair, OnSitAtChair));
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_StandUp, OnStandUp));
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_ExamineMenu, OnExamineMenu));
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_PlayArcade, OnPlayArcade));
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_ChangeCameraAngle, OnChangeCameraAngle));
        _subscriptions.Add(Managers.Subscribe(ActionId.Character_Idle, OnIdle));

        Debug.Log("[GolemCharacterController] Subscribed to ActionBus character events.");
    }

    private void OnDestroy()
    {
        foreach (var sub in _subscriptions)
            sub?.Dispose();
        _subscriptions.Clear();
    }

    // --- Action Handlers ---

    private void OnMoveToLocation(ActionMessage msg)
    {
        if (pointClick == null) return;
        if (msg.TryGetPayload<MoveToLocationPayload>(out var payload))
        {
            // If destination is set (non-zero), use it directly
            if (payload.Destination != Vector3.zero)
            {
                pointClick.MoveToPointPublic(payload.Destination);
                return;
            }

            // Otherwise find by location name
            var target = FindTransformByNameContains(payload.Location);
            Vector3 dest = target != null ? target.position : transform.position;
            pointClick.MoveToPointPublic(dest);
        }
    }

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

    private void OnChangeCameraAngle(ActionMessage msg)
    {
        if (cameraStateMachine == null) return;
        if (msg.TryGetPayload<ChangeCameraAnglePayload>(out var payload))
        {
            // Load CameraStateSO by name from Resources
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
        }
    }

    // --- Helpers ---

    private Transform FindTransformByNameContains(string namePart)
    {
        if (string.IsNullOrEmpty(namePart)) return null;
        var all = GameObject.FindObjectsOfType<Transform>();
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
