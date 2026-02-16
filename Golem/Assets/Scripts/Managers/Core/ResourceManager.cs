using System.Collections.Generic;
using UnityEngine;

public class ResourceManager
{
    private Dictionary<string, UnityEngine.Object> _resources = new Dictionary<string, UnityEngine.Object>();

    public T Load<T>(string key) where T : UnityEngine.Object
    {
        if (_resources.TryGetValue(key, out var resource))
            return resource as T;

        // Cache miss - try Resources.Load
        T loaded = Resources.Load<T>(key);
        if (loaded != null)
        {
            _resources[key] = loaded;
            return loaded;
        }

        // Sprite fallback
        if (typeof(T) == typeof(Sprite) && !key.Contains(".sprite"))
        {
            if (_resources.TryGetValue($"{key}.sprite", out resource))
                return resource as T;
        }

        return null;
    }

    public GameObject Instantiate(string key, Transform parent = null, bool pooling = false)
    {
        GameObject prefab = Load<GameObject>(key);
        if (prefab == null)
        {
            Debug.LogError($"[ResourceManager] Failed to load prefab: {key}");
            return null;
        }

        if (pooling)
            return Managers.Pool.Pop(prefab);

        GameObject go = UnityEngine.Object.Instantiate(prefab, parent);
        go.name = prefab.name;
        return go;
    }

    public void Destroy(GameObject go)
    {
        if (go == null) return;
        if (Managers.Pool.Push(go)) return;
        UnityEngine.Object.Destroy(go);
    }

    public void Clear()
    {
        _resources.Clear();
    }
}
