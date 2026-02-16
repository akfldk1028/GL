using UnityEngine;

/// <summary>
/// Disables the legacy CharacterActionController (CelesteActionController) at runtime
/// to prevent dual-path conflicts with the new GolemCharacterController.
///
/// Place this on the same GameObject as CharacterActionController, or on the GolemBootstrap.
/// It finds and disables all CharacterActionController instances in the scene.
/// </summary>
public class GolemActionGate : MonoBehaviour
{
    [Tooltip("If true, disables legacy controller on Start. Set false to keep both active for debugging.")]
    [SerializeField] private bool disableLegacy = true;

    private void Start()
    {
        if (!disableLegacy) return;

        var legacyControllers = FindObjectsOfType<CharacterActionController>();
        foreach (var legacy in legacyControllers)
        {
            legacy.enabled = false;
            Debug.Log($"[GolemActionGate] Disabled legacy CharacterActionController on '{legacy.gameObject.name}'.");
        }

        if (legacyControllers.Length == 0)
        {
            Debug.Log("[GolemActionGate] No legacy CharacterActionController found.");
        }
    }
}
