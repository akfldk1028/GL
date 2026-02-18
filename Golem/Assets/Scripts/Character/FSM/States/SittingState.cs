namespace Golem.Character.FSM.States
{
    /// <summary>
    /// Character is sitting (replaces isSitting boolean).
    /// NavAgent disabled. Can only transition to StandTransition.
    /// </summary>
    public class SittingState : ICharacterState
    {
        public CharacterStateId Id => CharacterStateId.Sitting;

        public void Enter(CharacterStateContext ctx)
        {
            // NavAgent already disabled by SnapToTransform
        }

        public void Exit(CharacterStateContext ctx)
        {
            ctx.RestoreDisabledCollider();
        }

        public void Update(CharacterStateContext ctx) { }

        public bool CanTransitionTo(CharacterStateId target)
        {
            return target == CharacterStateId.StandTransition;
        }
    }
}
