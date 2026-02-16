using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Object pool manager using UnityEngine.Pool.ObjectPool.
/// Ported from MB_N2N PoolManager pattern.
/// </summary>
public class PoolManager
{
    private class Pool
    {
        private GameObject _prefab;
        private ObjectPool<GameObject> _pool;
        private Transform _root;

        public Pool(GameObject prefab, Transform root)
        {
            _prefab = prefab;
            _root = root;
            _pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var go = Object.Instantiate(_prefab, _root);
                    go.name = _prefab.name;
                    return go;
                },
                actionOnGet: go => go.SetActive(true),
                actionOnRelease: go => go.SetActive(false),
                actionOnDestroy: go => Object.Destroy(go),
                defaultCapacity: 5,
                maxSize: 50
            );
        }

        public GameObject Pop() => _pool.Get();

        public void Push(GameObject go) => _pool.Release(go);

        public void Clear() => _pool.Clear();
    }

    private readonly Dictionary<string, Pool> _pools = new Dictionary<string, Pool>();
    private Transform _poolRoot;

    private Transform PoolRoot
    {
        get
        {
            if (_poolRoot == null)
            {
                var go = GameObject.Find("@Pool");
                if (go == null)
                {
                    go = new GameObject("@Pool");
                    Object.DontDestroyOnLoad(go);
                }
                _poolRoot = go.transform;
            }
            return _poolRoot;
        }
    }

    public GameObject Pop(GameObject prefab)
    {
        if (prefab == null) return null;
        string key = prefab.name;

        if (!_pools.TryGetValue(key, out var pool))
        {
            pool = new Pool(prefab, PoolRoot);
            _pools[key] = pool;
        }

        return pool.Pop();
    }

    public bool Push(GameObject go)
    {
        if (go == null) return false;
        string key = go.name;

        if (_pools.TryGetValue(key, out var pool))
        {
            pool.Push(go);
            return true;
        }

        return false;
    }

    public void Clear()
    {
        foreach (var pool in _pools.Values)
            pool.Clear();
        _pools.Clear();
    }
}
