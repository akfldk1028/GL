using UnityEngine;
using Unity.Profiling;

/// <summary>
/// ProfilerMarker wrappers for behavior module performance tracking.
/// Only active in Development Builds.
/// </summary>
public static class BehaviorModuleProfiler
{
    public static readonly ProfilerMarker FSMUpdate = new("Golem.FSM.Update");
    public static readonly ProfilerMarker ModulesUpdate = new("Golem.Modules.Update");
    public static readonly ProfilerMarker ModulesLateUpdate = new("Golem.Modules.LateUpdate");
    public static readonly ProfilerMarker BreathingModule = new("Golem.Module.Breathing");
    public static readonly ProfilerMarker HeadLookModule = new("Golem.Module.HeadLook");
    public static readonly ProfilerMarker IdleVariationModule = new("Golem.Module.IdleVariation");
    public static readonly ProfilerMarker AccelerationModule = new("Golem.Module.Acceleration");
}
