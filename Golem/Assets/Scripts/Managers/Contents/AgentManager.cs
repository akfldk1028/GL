using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages multiple AI agent character instances. Each agent has a unique ID
/// and its own set of components (controller, point-click, emotes, animator).
/// </summary>
public class AgentManager
{
    private readonly Dictionary<string, AgentInstance> _agents = new Dictionary<string, AgentInstance>();
    private Transform _agentsRoot;

    private Transform AgentsRoot
    {
        get
        {
            if (_agentsRoot == null)
            {
                var go = GameObject.Find("@Agents");
                if (go == null)
                {
                    go = new GameObject("@Agents");
                    Object.DontDestroyOnLoad(go);
                }
                _agentsRoot = go.transform;
            }
            return _agentsRoot;
        }
    }

    public int Count => _agents.Count;

    public AgentInstance Register(string agentId, GameObject root)
    {
        if (string.IsNullOrEmpty(agentId))
        {
            Debug.LogWarning("[AgentManager] Cannot register agent with empty ID.");
            return null;
        }

        if (_agents.ContainsKey(agentId))
        {
            Debug.LogWarning($"[AgentManager] Agent '{agentId}' already registered. Updating.");
            _agents.Remove(agentId);
        }

        // Parent under @Agents root
        if (root != null)
        {
            root.transform.SetParent(AgentsRoot);
        }

        var instance = new AgentInstance(agentId, root);
        _agents[agentId] = instance;
        Debug.Log($"[AgentManager] Registered agent: {agentId}");
        return instance;
    }

    public void Unregister(string agentId)
    {
        if (_agents.TryGetValue(agentId, out var instance))
        {
            _agents.Remove(agentId);
            Debug.Log($"[AgentManager] Unregistered agent: {agentId}");
        }
    }

    public AgentInstance GetAgent(string agentId)
    {
        _agents.TryGetValue(agentId, out var instance);
        return instance;
    }

    public bool HasAgent(string agentId) => _agents.ContainsKey(agentId);

    public IReadOnlyDictionary<string, AgentInstance> GetAllAgents() => _agents;

    public void Clear()
    {
        _agents.Clear();
    }
}
