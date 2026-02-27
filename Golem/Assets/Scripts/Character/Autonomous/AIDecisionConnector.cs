using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Golem.Character.FSM;
using Golem.Infrastructure.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Golem.Character.Autonomous
{
    /// <summary>
    /// Tier 1: HTTP connector that queries an LLM for autonomous action decisions.
    /// Supports Ollama (/api/generate) and OpenAI-compatible (/v1/chat/completions) endpoints.
    /// </summary>
    public class AIDecisionConnector
    {
        private readonly AIDecisionConfigSO _config;
        private readonly Transform _characterTransform;
        private readonly CharacterBehaviorFSM _fsm;
        private readonly MonoBehaviour _runner;

        // Valid actions the LLM can choose from (matches prompt template)
        private static readonly Dictionary<string, ActionId> ActionNameMap = new Dictionary<string, ActionId>(StringComparer.OrdinalIgnoreCase)
        {
            { "Idle",            ActionId.Character_Idle },
            { "MoveToLocation",  ActionId.Character_MoveToLocation },
            { "TurnTo",          ActionId.Character_TurnTo },
            { "SitAtChair",      ActionId.Character_SitAtChair },
            { "StandUp",         ActionId.Character_StandUp },
            { "LookAt",          ActionId.Character_LookAt },
            { "Lean",            ActionId.Character_Lean },
            { "ExamineMenu",     ActionId.Character_ExamineMenu },
            { "PlayArcade",      ActionId.Character_PlayArcade },
            { "PlayClaw",        ActionId.Character_PlayClaw },
            { "Wave",            ActionId.Social_Wave },
        };

        public AIDecisionConnector(
            AIDecisionConfigSO config,
            CharacterBehaviorFSM fsm,
            Transform characterTransform,
            MonoBehaviour runner)
        {
            _config = config;
            _fsm = fsm;
            _characterTransform = characterTransform;
            _runner = runner;
        }

        /// <summary>
        /// Query the LLM for an action decision. Returns null on failure.
        /// Use via: _runner.StartCoroutine(connector.QueryDecision(recentActions, callback))
        /// </summary>
        public IEnumerator QueryDecision(List<string> recentActions, Action<DecisionResult> onComplete)
        {
            string prompt = BuildPrompt(recentActions);
            string requestJson = BuildRequestBody(prompt);

            using var request = new UnityWebRequest(_config.endpointUrl, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = (int)_config.timeoutSeconds;

            if (_config.apiType == LLMApiType.OpenAI && !string.IsNullOrEmpty(_config.apiKey))
                request.SetRequestHeader("Authorization", $"Bearer {_config.apiKey}");

            Debug.Log($"[AIDecision] Querying {_config.apiType} ({_config.modelName})...");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[AIDecision] HTTP failed: {request.error}");
                onComplete?.Invoke(null);
                yield break;
            }

            var result = ParseResponse(request.downloadHandler.text);
            if (result != null)
                Debug.Log($"[AIDecision] Decision: {result.Action} (confidence={result.Confidence:F2}) — {result.Thought}");
            else
                Debug.LogWarning("[AIDecision] Failed to parse LLM response");

            onComplete?.Invoke(result);
        }

        private string BuildPrompt(List<string> recentActions)
        {
            var pos = _characterTransform.position;
            string nearbyObjects = ScanNearbyObjects();
            string recentStr = recentActions.Count > 0 ? string.Join(", ", recentActions) : "none";

            return $@"You are {_config.characterName}, a character in a virtual world.

## Current State
- FSM state: {_fsm.CurrentStateId}
- Position: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})
- Nearby objects: {nearbyObjects}
- Recent actions (last 5): {recentStr}

## Personality
{_config.personalityJson}

## Rules
1. Think step by step about what you want to do and why.
2. Do NOT repeat the same action 3 times in a row.
3. Choose actions that fit your personality and current context.
4. If you just sat for a long time, consider standing up and walking.

## Valid Actions
Idle, MoveToLocation, TurnTo, SitAtChair, StandUp, LookAt, Lean, ExamineMenu, PlayArcade, PlayClaw, Wave

Respond ONLY with JSON (no markdown, no explanation):
{{
  ""reasoning"": ""<2-3 sentences: why this action>"",
  ""action"": ""<ActionId from valid list>"",
  ""target"": ""<object_name or null>"",
  ""thought"": ""<one sentence: character's inner thought>"",
  ""confidence"": <0.0-1.0>
}}";
        }

        private string BuildRequestBody(string prompt)
        {
            if (_config.apiType == LLMApiType.Ollama)
            {
                var body = new
                {
                    model = _config.modelName,
                    prompt = prompt,
                    stream = false,
                    format = "json",
                    options = new { temperature = _config.temperature }
                };
                return JsonConvert.SerializeObject(body);
            }
            else // OpenAI
            {
                var body = new
                {
                    model = _config.modelName,
                    messages = new[] { new { role = "user", content = prompt } },
                    temperature = _config.temperature,
                    response_format = new { type = "json_object" }
                };
                return JsonConvert.SerializeObject(body);
            }
        }

        private DecisionResult ParseResponse(string responseBody)
        {
            try
            {
                string jsonContent;

                if (_config.apiType == LLMApiType.Ollama)
                {
                    var obj = JObject.Parse(responseBody);
                    jsonContent = obj["response"]?.ToString();
                }
                else // OpenAI
                {
                    var obj = JObject.Parse(responseBody);
                    jsonContent = obj["choices"]?[0]?["message"]?["content"]?.ToString();
                }

                if (string.IsNullOrEmpty(jsonContent))
                    return null;

                var decision = JObject.Parse(jsonContent);
                string actionName = decision["action"]?.ToString();
                if (string.IsNullOrEmpty(actionName) || !ActionNameMap.TryGetValue(actionName, out var actionId))
                {
                    Debug.LogWarning($"[AIDecision] Unknown action: {actionName}");
                    return null;
                }

                return new DecisionResult
                {
                    Action = actionId,
                    ActionName = actionName,
                    Target = decision["target"]?.ToString(),
                    Thought = decision["thought"]?.ToString() ?? "",
                    Confidence = decision["confidence"]?.Value<float>() ?? 0.5f,
                    Reasoning = decision["reasoning"]?.ToString() ?? ""
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AIDecision] Parse error: {e.Message}");
                return null;
            }
        }

        private string ScanNearbyObjects()
        {
            var found = new List<string>();
            float radius = _config.nearbyObjectRadius;
            Vector3 pos = _characterTransform.position;

            foreach (string tag in _config.nearbyObjectTags)
            {
                GameObject[] objs;
                try { objs = GameObject.FindGameObjectsWithTag(tag); } catch { continue; }
                if (objs == null) continue;

                foreach (var obj in objs)
                {
                    if (Vector3.Distance(obj.transform.position, pos) <= radius)
                        found.Add($"{obj.name} ({tag})");
                }
            }

            return found.Count > 0 ? string.Join(", ", found) : "nothing nearby";
        }
    }

    /// <summary>
    /// Parsed LLM decision response.
    /// </summary>
    public class DecisionResult
    {
        public ActionId Action;
        public string ActionName;
        public string Target;
        public string Thought;
        public float Confidence;
        public string Reasoning;
    }
}
