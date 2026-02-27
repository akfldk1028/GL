using System;
using System.Collections.Generic;
using System.Linq;
using Golem.Infrastructure.Messages;
using UnityEngine;

namespace Golem.Character.Autonomous
{
    public class EpisodicMemory
    {
        private readonly List<EpisodeEntry> _episodes = new List<EpisodeEntry>();
        private readonly MemoryConfigSO _config;

        private static readonly Dictionary<int, float> BaseImportance = new Dictionary<int, float>
        {
            { (int)ActionId.Character_Idle,           0.1f },
            { (int)ActionId.Character_MoveToLocation, 0.2f },
            { (int)ActionId.Character_TurnTo,         0.2f },
            { (int)ActionId.Character_SitAtChair,     0.3f },
            { (int)ActionId.Character_StandUp,        0.2f },
            { (int)ActionId.Character_LookAt,         0.3f },
            { (int)ActionId.Character_Lean,           0.3f },
            { (int)ActionId.Character_ExamineMenu,    0.4f },
            { (int)ActionId.Character_PlayArcade,     0.5f },
            { (int)ActionId.Character_PlayClaw,       0.5f },
            { (int)ActionId.Social_Wave,              0.4f },
            { (int)ActionId.Agent_ReflectionTriggered, 1.0f },
        };

        public List<EpisodeEntry> Episodes => _episodes;

        public EpisodicMemory(MemoryConfigSO config)
        {
            _config = config;
        }

        public void LoadFrom(List<EpisodeEntry> saved)
        {
            _episodes.Clear();
            if (saved != null)
                _episodes.AddRange(saved);
        }

        public void AddEpisode(EpisodeEntry entry)
        {
            if (entry.importance <= 0f)
                entry.importance = CalculateImportance(entry);

            _episodes.Add(entry);

            // FIFO eviction
            while (_episodes.Count > _config.maxEpisodes)
                _episodes.RemoveAt(0);
        }

        public float CalculateImportance(EpisodeEntry entry)
        {
            float baseImp = BaseImportance.TryGetValue(entry.actionId, out float b) ? b : 0.2f;

            // Novelty bonus: action not seen in last 10 episodes
            float noveltyBonus = 0f;
            int lookback = Mathf.Min(10, _episodes.Count);
            bool seenRecently = false;
            for (int i = _episodes.Count - 1; i >= _episodes.Count - lookback && i >= 0; i--)
            {
                if (_episodes[i].actionId == entry.actionId)
                {
                    seenRecently = true;
                    break;
                }
            }
            if (!seenRecently) noveltyBonus = 0.2f;

            // Failure bonus
            float failureBonus = entry.succeeded ? 0f : 0.3f;

            return Mathf.Clamp01(baseImp + noveltyBonus + failureBonus);
        }

        public static string BuildContextHash(string fsmState, string[] nearbyTags, float gameHour)
        {
            string timeBucket;
            if (gameHour < 6f) timeBucket = "night";
            else if (gameHour < 12f) timeBucket = "morning";
            else if (gameHour < 18f) timeBucket = "afternoon";
            else timeBucket = "evening";

            string sortedTags = "none";
            if (nearbyTags != null && nearbyTags.Length > 0)
            {
                var sorted = nearbyTags.OrderBy(t => t).ToArray();
                sortedTags = string.Join(",", sorted);
            }

            return $"{fsmState}|{sortedTags}|{timeBucket}";
        }

        public List<EpisodeEntry> RetrieveTopK(string currentContextHash, int k = -1)
        {
            if (_episodes.Count == 0) return new List<EpisodeEntry>();
            if (k < 0) k = _config.topKEpisodes;

            long nowTicks = DateTime.UtcNow.Ticks;
            double halfLifeTicks = (double)_config.recencyHalfLife * TimeSpan.TicksPerSecond;

            var scored = new List<(EpisodeEntry entry, float score)>();
            for (int i = 0; i < _episodes.Count; i++)
            {
                var ep = _episodes[i];
                // Recency: exponential decay
                double ageTicks = nowTicks - ep.timestampTicks;
                float recency = Mathf.Exp((float)(-0.693 * ageTicks / halfLifeTicks)); // ln(2) ≈ 0.693

                // Importance: stored value
                float importance = ep.importance;

                // Relevance: contextHash match
                float relevance = (ep.contextHash == currentContextHash) ? 1f : 0f;

                float score = recency * _config.recencyWeight
                            + importance * _config.importanceWeight
                            + relevance * _config.relevanceWeight;
                scored.Add((ep, score));
            }

            scored.Sort((a, b) => b.score.CompareTo(a.score));
            int count = Mathf.Min(k, scored.Count);
            var result = new List<EpisodeEntry>(count);
            for (int i = 0; i < count; i++)
                result.Add(scored[i].entry);
            return result;
        }

        public List<EpisodeEntry> RetrieveByImportance(int count)
        {
            if (_episodes.Count == 0) return new List<EpisodeEntry>();
            var sorted = _episodes.OrderByDescending(e => e.importance).Take(count).ToList();
            return sorted;
        }
    }
}
