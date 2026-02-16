using UnityEngine;

/// <summary>
/// Holds all component references for a single AI agent character instance.
/// </summary>
public class AgentInstance
{
    public string AgentId { get; private set; }
    public GameObject Root { get; private set; }
    public CharacterActionController Controller { get; private set; }
    public PointClickController PointClick { get; private set; }
    public EmotePlayer EmoteHandler { get; private set; }
    public Animator Animator { get; private set; }

    public AgentInstance(string agentId, GameObject root)
    {
        AgentId = agentId;
        Root = root;

        // Auto-discover components
        if (root != null)
        {
            Controller = root.GetComponent<CharacterActionController>() ?? root.GetComponentInChildren<CharacterActionController>();
            PointClick = root.GetComponent<PointClickController>() ?? root.GetComponentInChildren<PointClickController>();
            EmoteHandler = root.GetComponent<EmotePlayer>() ?? root.GetComponentInChildren<EmotePlayer>();
            Animator = root.GetComponent<Animator>() ?? root.GetComponentInChildren<Animator>();
        }
    }
}
