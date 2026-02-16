using UnityEngine;

/// <summary>
/// Singleton service locator providing global access to manager systems.
/// </summary>
public class Managers : MonoBehaviour
{
    private static Managers _instance;
    private static bool _applicationIsQuitting = false;

    public static Managers Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                return null;
            }

            if (_instance == null)
            {
                _instance = FindObjectOfType<Managers>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("@Managers");
                    _instance = go.AddComponent<Managers>();
                    DontDestroyOnLoad(go);
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _applicationIsQuitting = true;
        }
    }

    #region Core
    private AgentManager _agent = new AgentManager();

    public static AgentManager Agent => Instance?._agent;
    #endregion
}
