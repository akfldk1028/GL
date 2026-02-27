using UnityEngine;

namespace Golem.Character.Autonomous
{
    [CreateAssetMenu(fileName = "MemoryConfig", menuName = "Golem/MemoryConfig")]
    public class MemoryConfigSO : ScriptableObject
    {
        [Header("Episodic Memory")]
        [Tooltip("Maximum stored episodes before FIFO eviction")]
        public int maxEpisodes = 200;
        [Tooltip("Number of episodes to retrieve for LLM context")]
        public int topKEpisodes = 5;
        [Tooltip("Half-life in seconds for recency decay")]
        public float recencyHalfLife = 600f;
        [Tooltip("Recency weight in scoring")]
        [Range(0f, 1f)] public float recencyWeight = 0.4f;
        [Tooltip("Importance weight in scoring")]
        [Range(0f, 1f)] public float importanceWeight = 0.3f;
        [Tooltip("Relevance weight in scoring")]
        [Range(0f, 1f)] public float relevanceWeight = 0.3f;

        [Header("Skill Library")]
        [Tooltip("Maximum cached skill patterns")]
        public int maxSkills = 50;
        [Tooltip("Minimum uses before trusting a skill")]
        public int minSkillUses = 3;
        [Tooltip("Required success rate to use a skill")]
        [Range(0f, 1f)] public float skillConfidenceThreshold = 0.7f;
        [Tooltip("Success rate below which skills are pruned")]
        [Range(0f, 1f)] public float skillPruneThreshold = 0.3f;
        [Tooltip("Chance to explore (skip skill) even when match exists")]
        [Range(0f, 1f)] public float explorationRate = 0.2f;
        [Tooltip("If existing skill success rate is below this, replace with new successful action")]
        [Range(0f, 1f)] public float skillReplacementThreshold = 0.5f;

        [Header("Reflection")]
        [Tooltip("Trigger reflection every N actions")]
        public int reflectionInterval = 20;
        [Tooltip("Accumulated importance threshold to trigger reflection")]
        public float reflectionImportanceThreshold = 5.0f;

        [Header("Persistence")]
        [Tooltip("Save to disk every N episodes added")]
        public int saveInterval = 10;
        [Tooltip("Enable JSON file persistence")]
        public bool enablePersistence = true;

        [Header("Importance")]
        [Tooltip("Default base importance for any action (novelty/failure bonuses added on top)")]
        [Range(0f, 1f)] public float defaultBaseImportance = 0.3f;
        [Tooltip("Bonus importance for actions not seen in recent episodes")]
        [Range(0f, 1f)] public float noveltyBonus = 0.2f;
        [Tooltip("Bonus importance for failed actions")]
        [Range(0f, 1f)] public float failureBonus = 0.3f;

        [Header("Reflection Thresholds")]
        [Tooltip("Minimum repetition count to generate 'too frequent' observation")]
        public int frequentActionThreshold = 3;

        [Header("ReAct")]
        [Tooltip("Re-query LLM once on action failure")]
        public bool enableFailureRetry = true;
    }
}
