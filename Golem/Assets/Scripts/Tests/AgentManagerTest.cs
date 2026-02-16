using UnityEngine;

public class AgentManagerTest : MonoBehaviour
{
    void Start()
    {
        // Test 1: Register agent
        var agent1 = new GameObject("TestAgent1");
        agent1.AddComponent<CharacterActionController>();
        agent1.AddComponent<PointClickController>();
        agent1.AddComponent<EmotePlayer>();
        agent1.AddComponent<Animator>();

        var instance1 = Managers.Agent.Register("agent1", agent1);
        Debug.Log($"Test 1: instance1 != null: {instance1 != null}");
        Debug.Log($"Test 1: Controller found: {instance1.Controller != null}");

        // Test 2: Retrieve agent
        var retrieved = Managers.Agent.GetAgent("agent1");
        Debug.Log($"Test 2: Retrieved == instance1: {retrieved == instance1}");

        // Test 3: HasAgent
        Debug.Log($"Test 3: HasAgent('agent1'): {Managers.Agent.HasAgent("agent1")}");
        Debug.Log($"Test 3: HasAgent('nonexistent'): {Managers.Agent.HasAgent("nonexistent")}");

        // Test 4: Duplicate registration
        var instance2 = Managers.Agent.Register("agent1", agent1);
        Debug.Log($"Test 4: Duplicate registered (check warning above)");

        // Test 5: Count
        Debug.Log($"Test 5: Count: {Managers.Agent.Count}");

        // Test 6: Unregister
        Managers.Agent.Unregister("agent1");
        Debug.Log($"Test 6: After unregister, HasAgent: {Managers.Agent.HasAgent("agent1")}");
        Debug.Log($"Test 6: Count after unregister: {Managers.Agent.Count}");
    }
}
