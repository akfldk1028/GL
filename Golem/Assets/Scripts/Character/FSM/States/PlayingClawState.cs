namespace Golem.Character.FSM.States
{
    public class PlayingClawState : ICharacterState
    {
        public CharacterStateId Id => CharacterStateId.PlayingClaw;

        public void Enter(CharacterStateContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetBool("PlayingClawMachine", true);
        }

        public void Update(CharacterStateContext ctx) { }

        public void Exit(CharacterStateContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetBool("PlayingClawMachine", false);

            if (ctx.NavAgent != null)
                ctx.NavAgent.enabled = true;

            ctx.RestoreDisabledCollider();
        }

        public bool CanTransitionTo(CharacterStateId target) => true;
    }
}
