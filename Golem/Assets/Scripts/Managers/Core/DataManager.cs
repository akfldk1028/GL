using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MakeDict();
}

public class DataManager
{
    public void Init()
    {
        Debug.Log("[DataManager] Initialized");
    }

    public T LoadJson<T>(string path) where T : class
    {
        TextAsset textAsset = Managers.Resource.Load<TextAsset>(path);
        if (textAsset == null)
        {
            Debug.LogWarning($"[DataManager] Failed to load JSON: {path}");
            return null;
        }
        return JsonConvert.DeserializeObject<T>(textAsset.text);
    }
}
