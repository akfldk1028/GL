using UnityEngine;

namespace Golem.Character.FSM.States
{
    /// <summary>
    /// Character is playing stand-up animation (replaces isStandingUp boolean).
    /// Monitors Animator state for SitToStand completion, then transitions to Idle or Walking.
    /// </summary>
    public class StandTransitionState : ICharacterState
    {
        public CharacterStateId Id => CharacterStateId.StandTransition;

        private bool _enteredStandState;

        public void Enter(CharacterStateContext ctx)
        {
            _enteredStandState = false;

            if (ctx.Animator != null)
                ctx.Animator.SetTrigger("ToStand");
        }

        public void Exit(CharacterStateContext ctx) { }

        public void Update(CharacterStateContext ctx)
        {
            if (ctx.Animator == null) return;

            AnimatorStateInfo stateInfo = ctx.Animator.GetCurrentAnimatorStateInfo(0);
            bool inTransition = ctx.Animator.IsInTransition(0);

            // Wait until we've entered the SitToStand state
            if (!_enteredStandState && stateInfo.IsName("SitToStand"))
            {
                _enteredStandState = true;
            }

            // Check for exit after confirmed entry, wait for transition to complete
            if (_enteredStandState && !stateInfo.IsName("SitToStand") && !inTransition)
            {
                // Re-enable NavAgent
                if (ctx.NavAgent != null)
                    ctx.NavAgent.enabled = true;

                // If there's a pending destination (clicked while sitting), go there
                if (ctx.PendingDestination != Vector3.zero)
                {
                    ctx.PointClick.MoveToPoint(ctx.PendingDestination);
                    ctx.PendingDestination = Vector3.zero;
                    ctx.FSM.ForceTransition(CharacterStateId.Walking);
                }
                else
                {
                    ctx.FSM.ForceTransition(CharacterStateId.Idle);
                }
            }
        }

        public bool CanTransitionTo(CharacterStateId target)
        {
            return target == CharacterStateId.Idle
                || target == CharacterStateId.Walking;
        }
    }
}
