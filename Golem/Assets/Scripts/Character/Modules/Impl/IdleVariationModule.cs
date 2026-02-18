using Golem.Character.FSM;
using UnityEngine;

namespace Golem.Character.Modules.Impl
{
    public class IdleVariationModule : BaseBehaviorModule
    {
        public override string ModuleId => "idleVariation";

        private float _weightShiftTimer;
        private float _nextWeightShift;
        private float _gestureTimer;
        private float _nextGesture;

        private static readonly int IdleVariationHash = Animator.StringToHash("IdleVariation");

        public override void Initialize(BehaviorModuleContext ctx)
        {
            base.Initialize(ctx);
            ResetWeightShiftTimer();
            ResetGestureTimer();
        }

        public override void OnUpdate(float deltaTime)
        {
            if (Context?.Animator == null || Context?.FSM == null) return;
            if (!Context.FSM.IsInAnyState(CharacterStateId.Idle, CharacterStateId.Sitting)) return;

            // Weight shift
            _weightShiftTimer += deltaTime;
            if (_weightShiftTimer >= _nextWeightShift)
            {
                ApplyWeightShift();
                ResetWeightShiftTimer();
            }

            // Micro gesture
            _gestureTimer += deltaTime;
            if (_gestureTimer >= _nextGesture)
            {
                Context.Animator.SetTrigger(IdleVariationHash);
                ResetGestureTimer();
            }
        }

        public override void OnLateUpdate(float deltaTime)
        {
            // Hip bone micro-shift applied in LateUpdate
            if (Context?.HipBone == null || Context?.Config == null) return;
            if (Context.FSM == null || !Context.FSM.IsInState(CharacterStateId.Idle)) return;

            float shift = Mathf.Sin(Time.time * 0.3f) * Context.Config.hipShiftAmount;
            Context.HipBone.localPosition += new Vector3(shift, 0f, 0f);
        }

        private void ApplyWeightShift()
        {
            // Handled by hip bone LateUpdate oscillation
        }

        private void ResetWeightShiftTimer()
        {
            _weightShiftTimer = 0f;
            float interval = Context?.Config != null ? Context.Config.weightShiftInterval : 12f;
            float variance = Context?.Config != null ? Context.Config.weightShiftVariance : 4f;
            _nextWeightShift = interval + Random.Range(-variance, variance);
        }

        private void ResetGestureTimer()
        {
            _gestureTimer = 0f;
            float interval = Context?.Config != null ? Context.Config.microGestureInterval : 45f;
            float variance = Context?.Config != null ? Context.Config.microGestureVariance : 15f;
            _nextGesture = interval + Random.Range(-variance, variance);
        }
    }
}
