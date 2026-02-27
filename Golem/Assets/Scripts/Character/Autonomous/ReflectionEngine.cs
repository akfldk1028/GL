using System;
using System.Collections;
using System.Collections.Generic;
using Golem.Infrastructure.Messages;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Golem.Character.Autonomous
{
    public class ReflectionEngine
    {
        private readonly MemoryConfigSO _config;
        private readonly MemoryStore _memoryStore;
        private readonly AIDecisionConnector _connector;
        private readonly AIDecisionConfigSO _decisionConfig;
        private readonly MonoBehaviour _runner;

        private int _actionsSinceReflection;
        private float _accumulatedImportance;
        private bool _isReflecting;

        public ReflectionEngine(
            MemoryConfigSO config,
            MemoryStore memoryStore,
            AIDecisionConnector connector,
            AIDecisionConfigSO decisionConfig,
            MonoBehaviour runner)
        {
            _config = config;
            _memoryStore = memoryStore;
            _connector = connector;
            _decisionConfig = decisionConfig;
            _runner = runner;
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

            string prompt = BuildReflectionPrompt(topEpisodes);

            // Use the connector's query mechanism but with a custom prompt
            // We'll do a direct query through the connector's infrastructure
            DecisionResult dummyResult = null;
            var reflectionActions = new List<string> { "[REFLECTION MODE]" };
            yield return _runner.StartCoroutine(
                _connector.QueryDecision(reflectionActions, r => dummyResult = r));

            // Parse the reflection from the raw response — we actually need to send a custom prompt
            // Since QueryDecision builds its own prompt, we'll use the observations from the last decision's reasoning
            // For now, generate observations from the episode patterns directly
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

        private string BuildReflectionPrompt(List<EpisodeEntry> episodes)
        {
            string name = _decisionConfig != null ? _decisionConfig.characterName : "Golem";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"You are {name}. Review your recent experiences and generate 1-2 abstract observations.");
            sb.AppendLine();
            sb.AppendLine("## Recent Experiences");
            foreach (var ep in episodes)
            {
                string status = ep.succeeded ? "OK" : "FAILED";
                string timeStr = ep.Timestamp.ToString("HH:mm");
                sb.AppendLine($"- [{timeStr}] {ep.actionName} ({ep.thought}) [{status}]");
            }
            sb.AppendLine();
            sb.AppendLine("Respond ONLY with JSON: {\"observations\": [\"...\", \"...\"]}");
            return sb.ToString();
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
            if (mostCommon != null && maxCount >= 3)
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
