namespace Golem.Character.FSM.States
{
    public class LookingState : ICharacterState
    {
        public CharacterStateId Id => CharacterStateId.Looking;

        public void Enter(CharacterStateContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetBool("LookingDown", true);
        }

        public void Update(CharacterStateContext ctx) { }

        public void Exit(CharacterStateContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetBool("LookingDown", false);

            if (ctx.NavAgent != null)
                ctx.NavAgent.enabled = true;

            ctx.RestoreDisabledCollider();
        }

        public bool CanTransitionTo(CharacterStateId target) => true;
    }
}
