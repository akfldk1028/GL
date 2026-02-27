using UnityEngine;

namespace Golem.Character.Autonomous
{
    public enum LLMApiType { Ollama, OpenAI }

    /// <summary>
    /// Configuration for LLM-based autonomous decision making (Tier 1).
    /// Create via: Assets > Create > Golem > AIDecisionConfig
    /// </summary>
    [CreateAssetMenu(fileName = "AIDecisionConfig", menuName = "Golem/AIDecisionConfig")]
    public class AIDecisionConfigSO : ScriptableObject
    {
        [Header("Endpoint")]
        public LLMApiType apiType = LLMApiType.Ollama;
        [Tooltip("Ollama: http://localhost:11434/api/generate\nOpenAI: https://api.openai.com/v1/chat/completions")]
        public string endpointUrl = "http://localhost:11434/api/generate";
        public string modelName = "qwen2.5:3b";
        [Tooltip("OpenAI API key (only for OpenAI apiType)")]
        public string apiKey = "";

        [Header("Request")]
        [Range(0f, 2f)] public float temperature = 0.7f;
        [Range(3f, 30f)] public float timeoutSeconds = 10f;
        [Range(0f, 1f)] public float minConfidence = 0.3f;

        [Header("Character")]
        public string characterName = "Golem";
        [TextArea(2, 4)]
        public string personalityJson = "{\"traits\":[\"curious\",\"calm\",\"observant\"],\"preferences\":{\"favorite_spot\":\"garden_bench\",\"dislikes\":\"standing still too long\"}}";

        [Header("Context")]
        [Tooltip("Radius to scan for nearby objects")]
        public float nearbyObjectRadius = 15f;
        [Tooltip("Tags to scan for nearby objects")]
        public string[] nearbyObjectTags = { "Caffee Chair", "Arcade", "Claw Machine", "Slot Machine Chair", "Cafe Ad Display", "InterestPoint" };

        [Header("Mode")]
        [Tooltip("true = LLM query, false = weighted random fallback")]
        public bool useLLM = true;
    }
}
