namespace Golem.Character.FSM.States
{
    /// <summary>
    /// Character is moving to a destination with no interaction pending.
    /// On arrival → Idle.
    /// </summary>
    public class WalkingState : ICharacterState
    {
        public CharacterStateId Id => CharacterStateId.Walking;

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
                ctx.FSM.ForceTransition(CharacterStateId.Idle);
            }
        }

        public bool CanTransitionTo(CharacterStateId target) => true;
    }
}
