using UnityEngine;

namespace Golem.Character.Modules.Impl
{
    /// <summary>
    /// Adds subtle breathing motion via spine bone rotation.
    /// Breathing rate increases during walking.
    /// Applied in LateUpdate (after animation).
    /// </summary>
    public class BreathingModule : BaseBehaviorModule
    {
        public override string ModuleId => "breathing";

        private float _phase;

        public override void OnLateUpdate(float deltaTime)
        {
            if (Context?.SpineBone == null || Context?.Config == null) return;

            // Breathing rate — faster when moving
            float rate = Context.Config.breathingRate;
            if (Context.NavAgent != null && Context.NavAgent.enabled &&
                Context.NavAgent.velocity.magnitude > 0.1f)
            {
                rate *= Context.Config.breathingSpeedMultiplier;
            }

            _phase += rate * deltaTime * Mathf.PI * 2f;
            if (_phase > Mathf.PI * 2f) _phase -= Mathf.PI * 2f;

            float breathOffset = Mathf.Sin(_phase) * Context.Config.breathingAmplitude;

            // Multiply onto post-animation rotation (Animator writes before LateUpdate)
            Context.SpineBone.localRotation *= Quaternion.Euler(breathOffset, 0f, 0f);
        }
    }
}
