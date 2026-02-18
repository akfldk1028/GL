using Golem.Character.FSM;
using UnityEngine;

namespace Golem.Character.Modules.Impl
{
    public class AccelerationCurveModule : BaseBehaviorModule
    {
        public override string ModuleId => "accelerationCurve";

        private float _baseSpeed;
        private float _rampTime;
        private bool _isRamping;

        public override void Initialize(BehaviorModuleContext ctx)
        {
            base.Initialize(ctx);
            if (ctx?.NavAgent != null)
                _baseSpeed = ctx.NavAgent.speed;
        }

        public override void OnUpdate(float deltaTime)
        {
            if (Context?.NavAgent == null || Context?.FSM == null) return;

            bool isWalking = Context.FSM.IsInState(CharacterStateId.Walking);

            if (isWalking && !_isRamping)
            {
                _rampTime = 0f;
                _isRamping = true;
            }
            else if (!isWalking && _isRamping)
            {
                _isRamping = false;
                _rampTime = 0f;
                if (Context.NavAgent.enabled)
                    Context.NavAgent.speed = _baseSpeed;
            }

            if (_isRamping)
            {
                float duration = Context.Config != null ? Context.Config.accelerationTime : 0.5f;
                if (duration <= 0f)
                {
                    _isRamping = false;
                    return;
                }

                _rampTime += deltaTime;
                float t = Mathf.Clamp01(_rampTime / duration);

                float curveValue = t;
                if (Context.Config?.accelerationCurve != null)
                    curveValue = Context.Config.accelerationCurve.Evaluate(t);

                if (Context.NavAgent.enabled)
                    Context.NavAgent.speed = _baseSpeed * curveValue;

                if (t >= 1f)
                    _isRamping = false;
            }
        }

        public override void Dispose()
        {
            if (Context?.NavAgent != null && Context.NavAgent.enabled)
                Context.NavAgent.speed = _baseSpeed;
        }
    }
}
