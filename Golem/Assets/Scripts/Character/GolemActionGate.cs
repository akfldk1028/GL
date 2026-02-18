using UnityEngine;

/// <summary>
/// Disables the legacy CharacterActionController (CelesteActionController) at runtime
/// to prevent dual-path conflicts with the new GolemCharacterController.
///
/// Uses Awake() with [DefaultExecutionOrder(-50)] to guarantee execution before
/// CelesteActionController.Start() — which is where CFConnector event subscriptions happen.
/// Disabling before Start() prevents the subscription from ever occurring.
///
/// Additionally clears CFConnector.OnCharacterActionUnity listeners as a safety net
/// (e.g., hot-reload scenarios where Start() may have already run).
/// </summary>
[DefaultExecutionOrder(-50)]
public class GolemActionGate : MonoBehaviour
{
    [Tooltip("If true, disables legacy controller on Awake. Set false to keep both active for debugging.")]
    [SerializeField] private bool disableLegacy = true;

    private void Awake()
    {
        if (!disableLegacy) return;

        var legacyControllers = FindObjectsOfType<CharacterActionController>();
        foreach (var legacy in legacyControllers)
        {
            // Disabling before Start() prevents CelesteActionController.Start() from running,
            // so CFConnector event subscriptions never happen in normal flow.
            // For hot-reload safety, also clear the UnityEvent listeners.
            TryCleanupCFConnectorEvents(legacy);

            legacy.enabled = false;
            Debug.Log($"[GolemActionGate] Disabled legacy CharacterActionController on '{legacy.gameObject.name}'.");
        }

        if (legacyControllers.Length == 0)
        {
            Debug.Log("[GolemActionGate] No legacy CharacterActionController found.");
        }
    }

    private void TryCleanupCFConnectorEvents(CharacterActionController legacy)
    {
        // Resolve the CFConnector reference (CelesteActionController.Awake sets this)
        var connector = legacy.connector;
        if (connector == null)
            connector = CFConnector.instance;

        if (connector == null) return;

        // Clear UnityEvent listeners (safe even if none registered)
        try { connector.OnCharacterActionUnity.RemoveAllListeners(); }
        catch (System.Exception) { }

        // C# event (OnCharacterAction): HandleCelesteActionProxy is private,
        // so we can't unsubscribe externally. However, since Awake(-50) runs
        // before CelesteActionController.Start(), the subscription never happens.
        // If hot-reload is a concern, CelesteActionController.OnDestroy handles
        // its own cleanup when the component is eventually destroyed.
    }
}
