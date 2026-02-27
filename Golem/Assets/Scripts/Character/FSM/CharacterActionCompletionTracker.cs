using System;
using Golem.Infrastructure.Messages;

namespace Golem.Character.FSM
{
    /// <summary>
    /// Subscribes to FSM.OnStateChanged and publishes Agent_ActionCompleted
    /// at the appropriate times. Replaces coroutine-based completion detection.
    /// </summary>
    public class CharacterActionCompletionTracker : IDisposable
    {
        private readonly CharacterBehaviorFSM _fsm;

        public CharacterActionCompletionTracker(CharacterBehaviorFSM fsm)
        {
            _fsm = fsm;
            _fsm.OnStateChanged += OnStateChanged;
        }

        public void Dispose()
        {
            if (_fsm != null)
                _fsm.OnStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(CharacterStateId previous, CharacterStateId current)
        {
            switch (previous)
            {
                // Arriving → interaction state = character reached destination and entered interaction
                case CharacterStateId.Arriving when current == CharacterStateId.Looking:
                    PublishCompleted(ActionId.Character_ExamineMenu, "examineMenu");
                    break;
                case CharacterStateId.Arriving when current == CharacterStateId.Leaning:
                    PublishCompleted(ActionId.Character_Lean, "lean");
                    break;
                case CharacterStateId.Arriving when current == CharacterStateId.PlayingArcade:
                    PublishCompleted(ActionId.Character_PlayArcade, "playArcade");
                    break;
                case CharacterStateId.Arriving when current == CharacterStateId.PlayingClaw:
                    PublishCompleted(ActionId.Character_PlayClaw, "playClaw");
                    break;

                // SitTransition → Sitting = sit animation finished
                case CharacterStateId.SitTransition when current == CharacterStateId.Sitting:
                    PublishCompleted(ActionId.Character_SitAtChair, "sitAtChair");
                    break;

                // StandTransition → Idle or Walking = stand animation done
                case CharacterStateId.StandTransition when current == CharacterStateId.Idle:
                    PublishCompleted(ActionId.Character_StandUp, "standUp");
                    break;
                case CharacterStateId.StandTransition when current == CharacterStateId.Walking:
                    PublishCompleted(ActionId.Character_StandUp, "standUp");
                    break;
            }
        }

        private void PublishCompleted(ActionId source, string name)
        {
            Managers.PublishAction(ActionId.Agent_ActionCompleted, new ActionLifecyclePayload
            {
                SourceAction = source,
                ActionName = name,
                Success = true
            });
        }
    }
}
