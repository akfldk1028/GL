using System;
using Golem.Infrastructure.Messages;
using UnityEngine;

namespace Golem.Character.Autonomous
{
    public class ActionOutcomeTracker : IDisposable
    {
        private readonly MemoryStore _memoryStore;
        private IDisposable _completedSub;
        private IDisposable _failedSub;

        // Pending action being tracked
        private bool _hasPending;
        private int _pendingActionId;
        private string _pendingActionName;
        private string _pendingTarget;
        private string _pendingThought;
        private string _pendingReasoning;
        private string _pendingContextHash;
        private Vector3 _pendingPosition;
        private long _pendingTimestamp;

        /// <summary>True while an action outcome is being tracked (between BeginTracking and RecordOutcome).</summary>
        public bool HasPending => _hasPending;

        public event Action<bool> OnOutcomeRecorded;

        public ActionOutcomeTracker(MemoryStore memoryStore)
        {
            _memoryStore = memoryStore;

            _completedSub = Managers.Subscribe(ActionId.Agent_ActionCompleted, OnActionCompleted);
            _failedSub = Managers.Subscribe(ActionId.Agent_ActionFailed, OnActionFailed);
        }

        public void BeginTracking(AutonomousAction action, DecisionResult decision, string contextHash, Vector3 position)
        {
            _hasPending = true;
            _pendingActionId = (int)action.ActionId;
            _pendingActionName = action.ActionId.ToString();
            _pendingTarget = decision?.Target;
            _pendingThought = decision?.Thought ?? "";
            _pendingReasoning = decision?.Reasoning ?? "";
            _pendingContextHash = contextHash;
            _pendingPosition = position;
            _pendingTimestamp = DateTime.UtcNow.Ticks;
        }

        public void CompleteTracking(bool succeeded)
        {
            if (!_hasPending) return;
            RecordOutcome(succeeded);
        }

        private void OnActionCompleted(ActionMessage msg)
        {
            if (!_hasPending) return;
            if (!msg.TryGetPayload<ActionLifecyclePayload>(out var payload)) return;
            if ((int)payload.SourceAction != _pendingActionId) return;

            RecordOutcome(payload.Success);
        }

        private void OnActionFailed(ActionMessage msg)
        {
            if (!_hasPending) return;
            if (!msg.TryGetPayload<ActionLifecyclePayload>(out var payload)) return;
            if ((int)payload.SourceAction != _pendingActionId) return;

            RecordOutcome(false);
        }

        private void RecordOutcome(bool succeeded)
        {
            _hasPending = false;

            var episode = new EpisodeEntry
            {
                timestampTicks = _pendingTimestamp,
                actionId = _pendingActionId,
                actionName = _pendingActionName,
                target = _pendingTarget,
                thought = _pendingThought,
                importance = 0f, // calculated by EpisodicMemory
                succeeded = succeeded,
                posX = _pendingPosition.x,
                posY = _pendingPosition.y,
                posZ = _pendingPosition.z,
                contextHash = _pendingContextHash,
                reasoning = _pendingReasoning
            };

            _memoryStore.Episodic.AddEpisode(episode);
            _memoryStore.Skills.RecordOutcome(
                _pendingContextHash,
                _pendingActionId,
                _pendingActionName,
                _pendingTarget,
                succeeded);

            _memoryStore.OnEpisodeAdded();

            // Log after AddEpisode which calculates importance
            Debug.Log($"[OutcomeTracker] Recorded: {_pendingActionName} → {(succeeded ? "SUCCESS" : "FAIL")} (importance={episode.importance:F2})");
            OnOutcomeRecorded?.Invoke(succeeded);
        }

        public void Dispose()
        {
            _completedSub?.Dispose();
            _failedSub?.Dispose();
        }
    }
}
