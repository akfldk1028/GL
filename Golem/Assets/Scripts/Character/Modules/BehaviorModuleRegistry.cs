using System;
using System.Collections.Generic;

namespace Golem.Character.Modules
{
    /// <summary>
    /// Manages lifecycle of all behavior modules.
    /// Register → Initialize → Update/LateUpdate → Dispose.
    /// </summary>
    public sealed class BehaviorModuleRegistry : IDisposable
    {
        private readonly List<IBehaviorModule> _modules = new();
        private BehaviorModuleContext _context;

        public void Initialize(BehaviorModuleContext context)
        {
            _context = context;
            foreach (var module in _modules)
                module.Initialize(context);
        }

        public void Register(IBehaviorModule module)
        {
            _modules.Add(module);
            if (_context != null)
                module.Initialize(_context);
        }

        public T Get<T>() where T : IBehaviorModule
        {
            foreach (var module in _modules)
                if (module is T typed) return typed;
            return default;
        }

        public void UpdateAll(float deltaTime)
        {
            foreach (var module in _modules)
                if (module.IsActive) module.OnUpdate(deltaTime);
        }

        public void LateUpdateAll(float deltaTime)
        {
            foreach (var module in _modules)
                if (module.IsActive) module.OnLateUpdate(deltaTime);
        }

        public void Dispose()
        {
            foreach (var module in _modules)
                module.Dispose();
            _modules.Clear();
        }
    }
}
