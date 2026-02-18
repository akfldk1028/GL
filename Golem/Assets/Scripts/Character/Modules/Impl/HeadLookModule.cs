using Golem.Character.FSM;
using UnityEngine;

namespace Golem.Character.Modules.Impl
{
    public class HeadLookModule : BaseBehaviorModule
    {
        public override string ModuleId => "headLook";

        public Vector3? LookTarget { get; set; }

        private float _gazeTimer;
        private float _nextGazeChange;

        public override void Initialize(BehaviorModuleContext ctx)
        {
            base.Initialize(ctx);
            ResetGazeTimer();
        }

        public override void OnUpdate(float deltaTime)
        {
            if (Context?.Config == null) return;

            // Auto-pick interest points periodically
            _gazeTimer += deltaTime;
            if (_gazeTimer >= _nextGazeChange)
            {
                PickNewGazeTarget();
                ResetGazeTimer();
            }
        }

        public override void OnLateUpdate(float deltaTime)
        {
            if (Context?.HeadBone == null) return;
            if (!LookTarget.HasValue) return;

            float speed = Context.Config != null ? Context.Config.gazeSpeed : 5f;
            float maxAngle = Context.Config != null ? Context.Config.gazeFOV * 0.5f : 60f;

            Vector3 targetDir = (LookTarget.Value - Context.HeadBone.position).normalized;
            if (targetDir == Vector3.zero) return;

            Quaternion targetRotation = Quaternion.LookRotation(targetDir);
            float angle = Quaternion.Angle(Context.HeadBone.rotation, targetRotation);
            if (angle > maxAngle) return;

            Context.HeadBone.rotation = Quaternion.Slerp(
                Context.HeadBone.rotation,
                targetRotation,
                deltaTime * speed);
        }

        public void SetForcedTarget(Vector3 position) => LookTarget = position;
        public void ClearForcedTarget() => LookTarget = null;

        private void PickNewGazeTarget()
        {
            if (LookTarget.HasValue) return; // Don't override forced target

            var interestPoints = GameObject.FindGameObjectsWithTag("InterestPoint");
            if (interestPoints == null || interestPoints.Length == 0) return;

            var chosen = interestPoints[Random.Range(0, interestPoints.Length)];
            if (chosen != null && Context?.CharacterTransform != null)
            {
                float dist = Vector3.Distance(chosen.transform.position, Context.CharacterTransform.position);
                if (dist < 15f)
                    LookTarget = chosen.transform.position;
            }
        }

        private void ResetGazeTimer()
        {
            _gazeTimer = 0f;
            float interval = Context?.Config != null ? Context.Config.gazeChangeInterval : 5f;
            float variance = Context?.Config != null ? Context.Config.gazeChangeVariance : 2f;
            _nextGazeChange = interval + Random.Range(-variance, variance);
        }
    }
}
