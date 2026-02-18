using UnityEngine;

namespace Golem.Character.Modules.Impl
{
    public class HeadLookModule : BaseBehaviorModule
    {
        public override string ModuleId => "headLook";

        public Vector3? LookTarget { get; private set; }

        private float _gazeTimer;
        private float _nextGazeChange;
        private bool _hasForcedTarget;

        public override void Initialize(BehaviorModuleContext ctx)
        {
            base.Initialize(ctx);
            ResetGazeTimer();
        }

        public override void OnUpdate(float deltaTime)
        {
            if (Context?.Config == null) return;

            _gazeTimer += deltaTime;
            if (_gazeTimer >= _nextGazeChange)
            {
                PickNewGazeTarget();
                ResetGazeTimer();
            }
        }

        public override void OnLateUpdate(float deltaTime)
        {
            if (Context?.HeadBone == null || Context?.CharacterTransform == null) return;
            if (!LookTarget.HasValue) return;

            float speed = Context.Config != null ? Context.Config.gazeSpeed : 5f;
            float maxAngle = Context.Config != null ? Context.Config.gazeFOV * 0.5f : 60f;

            Vector3 targetDir = (LookTarget.Value - Context.HeadBone.position).normalized;
            if (targetDir == Vector3.zero) return;

            // FOV check against character forward, not head's current rotation
            float angleFromForward = Vector3.Angle(Context.CharacterTransform.forward, targetDir);
            if (angleFromForward > maxAngle)
            {
                // Target outside FOV — clear auto target so a new one is picked
                if (!_hasForcedTarget) LookTarget = null;
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(targetDir);
            Context.HeadBone.rotation = Quaternion.Slerp(
                Context.HeadBone.rotation,
                targetRotation,
                deltaTime * speed);
        }

        public void SetForcedTarget(Vector3 position)
        {
            _hasForcedTarget = true;
            LookTarget = position;
        }

        public void ClearForcedTarget()
        {
            _hasForcedTarget = false;
            LookTarget = null;
        }

        private void PickNewGazeTarget()
        {
            if (_hasForcedTarget) return;

            // Clear old auto target so we pick fresh
            LookTarget = null;

            GameObject[] interestPoints;
            try { interestPoints = GameObject.FindGameObjectsWithTag("InterestPoint"); }
            catch { return; } // Tag not defined in TagManager

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
