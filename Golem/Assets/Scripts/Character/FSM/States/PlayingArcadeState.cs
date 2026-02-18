namespace Golem.Character.FSM.States
{
    public class PlayingArcadeState : ICharacterState
    {
        public CharacterStateId Id => CharacterStateId.PlayingArcade;

        public void Enter(CharacterStateContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetTrigger("ToPlayArcade");
        }

        public void Update(CharacterStateContext ctx) { }

        public void Exit(CharacterStateContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetTrigger("ToStopArcade");

            if (ctx.NavAgent != null)
                ctx.NavAgent.enabled = true;

            ctx.RestoreDisabledCollider();
        }

        public bool CanTransitionTo(CharacterStateId target) => true;
    }
}
