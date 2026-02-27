using System;
using System.Collections;
using System.Collections.Generic;
using Golem.Infrastructure.Messages;
using UnityEngine;

namespace Golem.Character.Autonomous
{
    public class ReflectionEngine
    {
        private readonly MemoryConfigSO _config;
        private readonly MemoryStore _memoryStore;

        private int _actionsSinceReflection;
        private float _accumulatedImportance;
        private bool _isReflecting;

        public ReflectionEngine(MemoryConfigSO config, MemoryStore memoryStore)
        {
            _config = config;
            _memoryStore = memoryStore;
        }

        public void TrackAction(float importance)
        {
            _actionsSinceReflection++;
            _accumulatedImportance += importance;
        }

        public bool ShouldReflect()
        {
            if (_isReflecting) return false;
            return _actionsSinceReflection >= _config.reflectionInterval
                || _accumulatedImportance >= _config.reflectionImportanceThreshold;
        }

        public IEnumerator ExecuteReflection()
        {
            if (_isReflecting) yield break;
            _isReflecting = true;
            _actionsSinceReflection = 0;
            _accumulatedImportance = 0f;

            Debug.Log("[Reflection] Starting reflection...");

            var topEpisodes = _memoryStore.Episodic.RetrieveByImportance(10);
            if (topEpisodes.Count == 0)
            {
                _isReflecting = false;
                yield break;
            }

            // Generate observations from episode patterns (local analysis, no LLM call)
            var observations = GenerateLocalObservations(topEpisodes);

            foreach (string obs in observations)
            {
                var entry = new EpisodeEntry
                {
                    timestampTicks = DateTime.UtcNow.Ticks,
                    actionId = (int)ActionId.Agent_ReflectionTriggered,
                    actionName = "Reflection",
                    target = null,
                    thought = obs,
                    importance = 1.0f,
                    succeeded = true,
                    posX = 0f, posY = 0f, posZ = 0f,
                    contextHash = "reflection",
                    reasoning = "Periodic reflection on recent experiences"
                };
                _memoryStore.Episodic.AddEpisode(entry);
                Debug.Log($"[Reflection] Observation: {obs}");
            }

            _memoryStore.Skills.Prune();
            _memoryStore.OnEpisodeAdded();
            _isReflecting = false;

            Debug.Log($"[Reflection] Complete. Generated {observations.Count} observations.");
        }

        private List<string> GenerateLocalObservations(List<EpisodeEntry> episodes)
        {
            var observations = new List<string>();

            // Pattern: track failure rates
            int failCount = 0;
            int totalCount = episodes.Count;
            var actionCounts = new Dictionary<string, int>();
            var failedActions = new Dictionary<string, int>();

            foreach (var ep in episodes)
            {
                if (!ep.succeeded) failCount++;
                string key = ep.actionName ?? "unknown";
                actionCounts[key] = actionCounts.TryGetValue(key, out int c) ? c + 1 : 1;
                if (!ep.succeeded)
                    failedActions[key] = failedActions.TryGetValue(key, out int f) ? f + 1 : 1;
            }

            // Observation 1: most common action
            string mostCommon = null;
            int maxCount = 0;
            foreach (var kv in actionCounts)
            {
                if (kv.Value > maxCount) { maxCount = kv.Value; mostCommon = kv.Key; }
            }
            if (mostCommon != null && maxCount >= _config.frequentActionThreshold)
            {
                observations.Add($"I tend to {mostCommon} frequently ({maxCount} times recently). I should try more variety.");
            }

            // Observation 2: failure pattern
            if (failCount > 0)
            {
                string worstAction = null;
                int worstFails = 0;
                foreach (var kv in failedActions)
                {
                    if (kv.Value > worstFails) { worstFails = kv.Value; worstAction = kv.Key; }
                }
                if (worstAction != null)
                {
                    observations.Add($"{worstAction} has failed {worstFails} times. I should be more cautious with this action or try alternatives.");
                }
            }

            // Fallback: generic observation
            if (observations.Count == 0)
            {
                observations.Add("My recent actions have been going well. I should continue exploring my environment.");
            }

            return observations;
        }
    }
}
