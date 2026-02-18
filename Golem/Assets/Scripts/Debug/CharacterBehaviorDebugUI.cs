using Golem.Character.FSM;
using Golem.Character.Modules;
using UnityEngine;

public class CharacterBehaviorDebugUI : MonoBehaviour
{
    [SerializeField] private bool showOnStart = false;

    private bool _visible;
    private CharacterBehaviorFSM _fsm;
    private BehaviorModuleRegistry _modules;

    public void Initialize(CharacterBehaviorFSM fsm, BehaviorModuleRegistry modules)
    {
        _fsm = fsm;
        _modules = modules;
        _visible = showOnStart;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
            _visible = !_visible;

        // Lazy auto-find if not initialized (Bootstrap creates us before GCC.Start runs)
        if (_fsm == null)
        {
            var gcc = FindObjectOfType<GolemCharacterController>();
            if (gcc != null && gcc.FSM != null)
                Initialize(gcc.FSM, gcc.ModuleRegistry);
        }
    }

    private void OnGUI()
    {
        if (!_visible || _fsm == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.BeginVertical("box");

        GUILayout.Label("<b>Character Behavior Debug [F11]</b>");
        GUILayout.Space(5);

        GUILayout.Label($"FSM State: <color=yellow>{_fsm.CurrentStateId}</color>");
        GUILayout.Label($"Previous: {_fsm.PreviousStateId}");
        GUILayout.Space(5);

        GUILayout.Label("<b>Modules:</b>");
        ShowModule<Golem.Character.Modules.Impl.BreathingModule>("Breathing");
        ShowModule<Golem.Character.Modules.Impl.HeadLookModule>("HeadLook");
        ShowModule<Golem.Character.Modules.Impl.ThinkTimeModule>("ThinkTime");
        ShowModule<Golem.Character.Modules.Impl.IdleVariationModule>("IdleVariation");
        ShowModule<Golem.Character.Modules.Impl.AccelerationCurveModule>("Acceleration");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void ShowModule<T>(string name) where T : IBehaviorModule
    {
        var m = _modules?.Get<T>();
        if (m != null)
            GUILayout.Label($"  {name}: {(m.IsActive ? "<color=green>Active</color>" : "<color=red>Inactive</color>")}");
    }
}
