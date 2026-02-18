namespace Golem.Character.Modules
{
    /// <summary>
    /// Interface for composable behavior modules (N8N node pattern).
    /// Each module adds a "layer of life" to the character.
    /// </summary>
    public interface IBehaviorModule
    {
        string ModuleId { get; }
        bool IsActive { get; }
        void Initialize(BehaviorModuleContext ctx);
        void Dispose();
        void OnUpdate(float deltaTime);
        void OnLateUpdate(float deltaTime);
        void SetActive(bool active);
    }
}
