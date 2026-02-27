using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Golem.Character.Autonomous
{
    public class EpisodicMemory
    {
        private readonly List<EpisodeEntry> _episodes = new List<EpisodeEntry>();
        private readonly MemoryConfigSO _config;

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
            // All actions start with the same base importance (no hardcoded per-action table).
            // Differentiation comes from novelty and failure — the agent learns what matters.
            float baseImp = _config.defaultBaseImportance;

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

        public static string BuildContextHash(string fsmState, string[] nearbyTags)
        {
            string sortedTags = "none";
            if (nearbyTags != null && nearbyTags.Length > 0)
            {
                var sorted = nearbyTags.OrderBy(t => t).ToArray();
                sortedTags = string.Join(",", sorted);
            }

            return $"{fsmState}|{sortedTags}";
        }

        /// <summary>
        /// Component-based partial relevance scoring instead of binary exact match.
        /// fsmState match = 0.5, nearbyTags overlap ratio = 0.5
        /// </summary>
        public static float CalculateRelevance(string hashA, string hashB)
        {
            if (hashA == hashB) return 1f;
            if (string.IsNullOrEmpty(hashA) || string.IsNullOrEmpty(hashB)) return 0f;

            var partsA = hashA.Split('|');
            var partsB = hashB.Split('|');
            if (partsA.Length < 2 || partsB.Length < 2) return 0f;

            float score = 0f;

            // FSM state match (0.5)
            if (partsA[0] == partsB[0]) score += 0.5f;

            // Nearby tags overlap ratio (0.5)
            if (partsA[1] == partsB[1])
            {
                score += 0.5f;
            }
            else if (partsA[1] != "none" && partsB[1] != "none")
            {
                var tagsA = new HashSet<string>(partsA[1].Split(','));
                var tagsB = new HashSet<string>(partsB[1].Split(','));
                int union = 0;
                int intersection = 0;
                foreach (var t in tagsA) { union++; if (tagsB.Contains(t)) intersection++; }
                foreach (var t in tagsB) { if (!tagsA.Contains(t)) union++; }
                if (union > 0) score += 0.5f * intersection / union; // Jaccard similarity
            }

            return score;
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

                // Relevance: component-based partial match
                float relevance = CalculateRelevance(ep.contextHash, currentContextHash);

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
