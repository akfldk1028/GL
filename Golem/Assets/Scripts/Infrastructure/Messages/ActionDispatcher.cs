using System;
using System.Collections.Generic;

namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// Routes ActionMessages to registered IAction handlers by ActionId.
    /// Ported from MB_N2N ActionDispatcher.
    /// </summary>
    public sealed class ActionDispatcher : IDisposable
    {
        private readonly ActionMessageBus _bus;
        private readonly Dictionary<ActionId, List<IAction>> _registry = new Dictionary<ActionId, List<IAction>>();
        private readonly IDisposable _subscription;

        public ActionDispatcher(ActionMessageBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _subscription = _bus.Subscribe(OnAction);
        }

        public void Register(IAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (!_registry.TryGetValue(action.Id, out var list))
            {
                list = new List<IAction>();
                _registry[action.Id] = list;
            }

            if (!list.Contains(action))
            {
                list.Add(action);
            }
        }

        public void Unregister(IAction action)
        {
            if (action == null) return;

            if (_registry.TryGetValue(action.Id, out var list))
            {
                list.Remove(action);
                if (list.Count == 0)
                {
                    _registry.Remove(action.Id);
                }
            }
        }

        private void OnAction(ActionMessage message)
        {
            if (_registry.TryGetValue(message.Id, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].Execute(message);
                }
            }
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _registry.Clear();
        }
    }
}
