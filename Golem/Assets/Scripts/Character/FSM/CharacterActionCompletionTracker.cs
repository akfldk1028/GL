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
            // Arriving → interaction state completion is handled by ArrivingState itself
            // SitTransition → Sitting is a natural flow, completion when seated
            // StandTransition → Idle/Walking completion when stand animation done

            // Publish completion for specific transitions that indicate action end
            switch (previous)
            {
                case CharacterStateId.StandTransition when current == CharacterStateId.Idle:
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
