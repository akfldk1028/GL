using UnityEngine;

namespace Golem.Character.Modules
{
    [CreateAssetMenu(fileName = "BehaviorConfig", menuName = "Golem/BehaviorConfig")]
    public class BehaviorConfigSO : ScriptableObject
    {
        [Header("Breathing")]
        public float breathingRate = 0.25f;
        public float breathingAmplitude = 1.5f;
        public float breathingSpeedMultiplier = 1.5f;

        [Header("Head Look")]
        public float gazeSpeed = 2f;
        public float gazeChangeInterval = 5f;
        public float gazeChangeVariance = 2f;
        public float gazeFOV = 120f;

        [Header("Think Time")]
        public float thinkTimeMin = 0.3f;
        public float thinkTimeMax = 2.0f;

        [Header("Idle Variation")]
        public float weightShiftInterval = 12f;
        public float weightShiftVariance = 4f;
        public float microGestureInterval = 45f;
        public float microGestureVariance = 15f;
        public float hipShiftAmount = 0.02f;

        [Header("Acceleration")]
        public AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float accelerationTime = 0.5f;
        public float decelerationTime = 0.4f;
    }
}
