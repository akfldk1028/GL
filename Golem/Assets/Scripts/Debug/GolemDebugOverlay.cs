#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Golem.Infrastructure.Messages;
using Golem.Infrastructure.State;
using UnityEngine;

public class GolemDebugOverlay : MonoBehaviour
{
    private bool _visible = false;
    private readonly List<string> _messageLog = new List<string>();
    private const int MaxLogEntries = 20;
    private Vector2 _scrollPos;

    // Message rate tracking
    private int _messageCount;
    private float _lastRateCheck;
    private float _messagesPerSecond;

    private IDisposable _subscription;

    private void Start()
    {
        // Subscribe to ALL ActionBus messages for logging
        if (Managers.ActionBus != null)
        {
            _subscription = Managers.ActionBus.Subscribe(OnActionMessage);
        }
    }

    private void OnDestroy()
    {
        _subscription?.Dispose();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
            _visible = !_visible;

        // Calculate message rate every second
        if (Time.realtimeSinceStartup - _lastRateCheck >= 1f)
        {
            _messagesPerSecond = _messageCount;
            _messageCount = 0;
            _lastRateCheck = Time.realtimeSinceStartup;
        }
    }

    private void OnActionMessage(ActionMessage msg)
    {
        _messageCount++;

        // Skip System_Update/LateUpdate/FixedUpdate from log (too noisy)
        if (msg.Id == ActionId.System_Update || msg.Id == ActionId.System_LateUpdate || msg.Id == ActionId.System_FixedUpdate)
            return;

        string entry = $"[{Time.realtimeSinceStartup:F1}] {msg.Id}";
        if (msg.Payload != null)
            entry += $" ({msg.Payload.GetType().Name})";

        _messageLog.Add(entry);
        if (_messageLog.Count > MaxLogEntries)
            _messageLog.RemoveAt(0);
    }

    private void OnGUI()
    {
        if (!_visible) return;

        float w = 400;
        float h = 500;
        float x = Screen.width - w - 10;
        float y = 10;

        GUI.Box(new Rect(x, y, w, h), "Golem Debug [F12]");

        GUILayout.BeginArea(new Rect(x + 10, y + 25, w - 20, h - 35));

        // Connection Status
        bool connected = AINetworkManager.IsConnected;
        GUI.color = connected ? Color.green : Color.red;
        GUILayout.Label($"Connection: {(connected ? "CONNECTED" : "DISCONNECTED")}");
        GUI.color = Color.white;

        // StateMachine State
        GUILayout.Label($"State: {Managers.CurrentStateId}");

        // Message Rate
        GUILayout.Label($"Messages/sec: {_messagesPerSecond:F0}");

        // Agent Count
        if (Managers.Agent != null)
        {
            GUILayout.Label($"Agents: {Managers.Agent.Count}");
            var agents = Managers.Agent.GetAllAgents();
            foreach (var kvp in agents)
            {
                GUILayout.Label($"  - {kvp.Key}: {(kvp.Value.Root != null ? "Active" : "Null")}");
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("--- Action Log ---");

        // Scrollable message log
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(280));
        for (int i = _messageLog.Count - 1; i >= 0; i--)
        {
            GUILayout.Label(_messageLog[i]);
        }
        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }
}
#endif
