using UnityEngine;

namespace Golem.Character.FSM.States
{
    public class IdleState : ICharacterState
    {
        public CharacterStateId Id => CharacterStateId.Idle;

        public void Enter(CharacterStateContext ctx)
        {
            if (ctx.NavAgent != null && !ctx.NavAgent.enabled)
                ctx.NavAgent.enabled = true;

            if (ctx.NavAgent != null && ctx.NavAgent.enabled)
                ctx.NavAgent.ResetPath();

            ctx.ClearInteraction();
        }

        public void Exit(CharacterStateContext ctx) { }

        public void Update(CharacterStateContext ctx) { }

        public bool CanTransitionTo(CharacterStateId target)
        {
            return target != CharacterStateId.StandTransition
                && target != CharacterStateId.Sitting;
        }
    }
}
