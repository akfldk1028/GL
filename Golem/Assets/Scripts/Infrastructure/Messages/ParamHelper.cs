using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// Safe extraction utilities for Dictionary&lt;string, object&gt; parameters
    /// received from the AI server JSON.
    ///
    /// Newtonsoft.Json deserializes nested objects as JObject/JArray/JValue,
    /// NOT as Dictionary/List/primitive. Every getter must handle both paths.
    /// </summary>
    public static class ParamHelper
    {
        public static string GetString(Dictionary<string, object> p, string key, string fallback = null)
        {
            if (p == null || !p.TryGetValue(key, out var val) || val == null) return fallback;
            if (val is JValue jval) return jval.Value<string>() ?? fallback;
            return val.ToString();
        }

        public static int GetInt(Dictionary<string, object> p, string key, int fallback = 0)
        {
            if (p == null || !p.TryGetValue(key, out var val) || val == null) return fallback;
            if (val is JValue jval) return jval.Value<int>();
            if (val is long l) return (int)l;
            if (val is int i) return i;
            if (val is double d) return (int)d;
            if (int.TryParse(val.ToString(), out int parsed)) return parsed;
            return fallback;
        }

        public static float GetFloat(Dictionary<string, object> p, string key, float fallback = 0f)
        {
            if (p == null || !p.TryGetValue(key, out var val) || val == null) return fallback;
            if (val is JValue jval) return jval.Value<float>();
            if (val is double d) return (float)d;
            if (val is float f) return f;
            if (val is long l) return l;
            if (val is int i) return i;
            if (float.TryParse(val.ToString(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float parsed)) return parsed;
            return fallback;
        }

        public static bool GetBool(Dictionary<string, object> p, string key, bool fallback = false)
        {
            if (p == null || !p.TryGetValue(key, out var val) || val == null) return fallback;
            if (val is JValue jval) return jval.Value<bool>();
            if (val is bool b) return b;
            if (bool.TryParse(val.ToString(), out bool parsed)) return parsed;
            return fallback;
        }

        public static Vector3 GetVector3(Dictionary<string, object> p, string key, Vector3 fallback = default)
        {
            if (p == null || !p.TryGetValue(key, out var val) || val == null) return fallback;

            // Newtonsoft deserializes nested JSON objects as JObject
            if (val is JObject jobj)
            {
                float x = jobj["x"]?.Value<float>() ?? 0f;
                float y = jobj["y"]?.Value<float>() ?? 0f;
                float z = jobj["z"]?.Value<float>() ?? 0f;
                return new Vector3(x, y, z);
            }

            // Fallback: already-converted Dictionary (e.g. manual construction)
            if (val is Dictionary<string, object> dict)
            {
                float x = GetFloat(dict, "x");
                float y = GetFloat(dict, "y");
                float z = GetFloat(dict, "z");
                return new Vector3(x, y, z);
            }
            return fallback;
        }

        /// <summary>
        /// Extract a list of sub-action dictionaries from a "actions" parameter.
        /// </summary>
        public static List<Dictionary<string, object>> GetActionList(Dictionary<string, object> p, string key = "actions")
        {
            var result = new List<Dictionary<string, object>>();
            if (p == null || !p.TryGetValue(key, out var val) || val == null) return result;

            // Newtonsoft deserializes JSON arrays as JArray
            if (val is JArray jarr)
            {
                foreach (var item in jarr)
                {
                    if (item is JObject jo)
                        result.Add(jo.ToObject<Dictionary<string, object>>());
                }
                return result;
            }

            // Fallback: already-converted List (e.g. manual construction)
            if (val is List<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dict)
                        result.Add(dict);
                }
            }
            return result;
        }
    }
}
