using UnityEngine;

namespace Golem.Character.FSM.States
{
    /// <summary>
    /// Character is approaching an interaction spot (Look, Lean, PlayClaw, PlayArcade).
    /// On arrival: snap to spot → transition to pending interaction state.
    /// </summary>
    public class ArrivingState : ICharacterState
    {
        public CharacterStateId Id => CharacterStateId.Arriving;

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

                // Snap to interaction spot
                if (ctx.InteractionSpot != null)
                    ctx.PointClick.SnapToTransform(ctx.InteractionSpot);

                // Transition to the pending interaction state
                var target = ctx.PendingInteractionState;
                if (target != CharacterStateId.None)
                {
                    ctx.FSM.ForceTransition(target);
                }
                else
                {
                    Debug.LogWarning("[ArrivingState] No pending interaction state set. Falling back to Idle.");
                    ctx.FSM.ForceTransition(CharacterStateId.Idle);
                }
            }
        }

        public bool CanTransitionTo(CharacterStateId target) => true;
    }
}
