using UnityEngine;

namespace Golem.Character.Autonomous
{
    /// <summary>
    /// Tuning values for autonomous idle behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "IdleSchedulerConfig", menuName = "Golem/IdleSchedulerConfig")]
    public class IdleSchedulerConfigSO : ScriptableObject
    {
        [Header("Timing")]
        [Tooltip("Seconds of idle before autonomous behavior starts")]
        public float idleDelayBeforeAutonomous = 10f;
        public float idleDelayVariance = 5f;

        [Header("Behavior Weights (must sum to 1.0)")]
        [Range(0, 1)] public float wanderWeight = 0.40f;
        [Range(0, 1)] public float lookAroundWeight = 0.20f;
        [Range(0, 1)] public float sitWeight = 0.15f;
        [Range(0, 1)] public float gestureWeight = 0.15f;
        [Range(0, 1)] public float playGameWeight = 0.10f;

        [Header("Wander")]
        public float wanderRadius = 5f;
        public float wanderDuration = 8f;

        [Header("Sit Duration")]
        public float sitDurationMin = 10f;
        public float sitDurationMax = 30f;

        private void OnValidate()
        {
            float sum = wanderWeight + lookAroundWeight + sitWeight + gestureWeight + playGameWeight;
            if (Mathf.Abs(sum - 1f) > 0.01f)
                Debug.LogWarning($"[IdleSchedulerConfig] Behavior weights sum to {sum:F2}, expected 1.0");
        }
    }
}
