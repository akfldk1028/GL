namespace Golem.Character.Modules
{
    /// <summary>
    /// Abstract base for behavior modules. Reduces boilerplate.
    /// </summary>
    public abstract class BaseBehaviorModule : IBehaviorModule
    {
        public abstract string ModuleId { get; }
        public bool IsActive { get; private set; } = true;

        protected BehaviorModuleContext Context { get; private set; }

        public virtual void Initialize(BehaviorModuleContext ctx)
        {
            Context = ctx;
        }

        public virtual void Dispose() { }

        public virtual void OnUpdate(float deltaTime) { }

        public virtual void OnLateUpdate(float deltaTime) { }

        public void SetActive(bool active)
        {
            IsActive = active;
        }
    }
}
