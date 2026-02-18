namespace Golem.Character.FSM.States
{
    public class LeaningState : ICharacterState
    {
        public CharacterStateId Id => CharacterStateId.Leaning;

        public void Enter(CharacterStateContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetBool("Leaning", true);
        }

        public void Update(CharacterStateContext ctx) { }

        public void Exit(CharacterStateContext ctx)
        {
            if (ctx.Animator != null)
                ctx.Animator.SetBool("Leaning", false);

            if (ctx.NavAgent != null)
                ctx.NavAgent.enabled = true;

            ctx.RestoreDisabledCollider();
        }

        public bool CanTransitionTo(CharacterStateId target) => true;
    }
}
