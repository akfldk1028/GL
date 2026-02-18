namespace Golem.Character.FSM.States
{
    /// <summary>
    /// Character is walking to a chair (replaces goingToSit boolean).
    /// On arrival: snap to chair interaction spot, trigger ToSit, transition to Sitting.
    /// </summary>
    public class SitTransitionState : ICharacterState
    {
        public CharacterStateId Id => CharacterStateId.SitTransition;

        public void Enter(CharacterStateContext ctx)
        {
            if (ctx.NavAgent != null && !ctx.NavAgent.enabled)
                ctx.NavAgent.enabled = true;
        }

        public void Exit(CharacterStateContext ctx) { }

        public void Update(CharacterStateContext ctx)
        {
            if (ctx.PointClick == null || ctx.NavAgent == null) return;
            if (!ctx.NavAgent.enabled) return;

            if (ctx.PointClick.HasArrived)
            {
                // Disable NavAgent before snap to prevent repositioning
                if (ctx.NavAgent != null)
                    ctx.NavAgent.enabled = false;

                // Snap to chair interaction spot
                if (ctx.InteractionSpot != null)
                    ctx.PointClick.SnapToTransform(ctx.InteractionSpot);

                // Trigger sit animation and transition to Sitting
                if (ctx.Animator != null)
                    ctx.Animator.SetTrigger("ToSit");

                ctx.FSM.ForceTransition(CharacterStateId.Sitting);
            }
        }

        public bool CanTransitionTo(CharacterStateId target)
        {
            return target == CharacterStateId.Sitting
                || target == CharacterStateId.Idle
                || target == CharacterStateId.Walking;
        }
    }
}
