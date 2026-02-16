using System;
using System.Collections.Generic;

namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// Command pattern dispatcher that routes ActionMessages to registered IAction handlers
    /// Automatically subscribes to the ActionMessageBus and dispatches to registered handlers
    /// </summary>
    public sealed class ActionDispatcher : IDisposable
    {
        private readonly ActionMessageBus m_Bus;
        private readonly Dictionary<ActionId, List<IAction>> m_Registry = new Dictionary<ActionId, List<IAction>>();
        private readonly IDisposable m_Subscription;

        /// <summary>
        /// Creates a new action dispatcher
        /// </summary>
        /// <param name="bus">The ActionMessageBus to subscribe to</param>
        public ActionDispatcher(ActionMessageBus bus)
        {
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            m_Bus = bus;
            m_Subscription = m_Bus.Subscribe(OnAction);
        }

        /// <summary>
        /// Registers an action handler
        /// Multiple handlers can be registered for the same ActionId
        /// </summary>
        /// <param name="action">The action handler to register</param>
        public void Register(IAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (!m_Registry.TryGetValue(action.Id, out var list))
            {
                list = new List<IAction>();
                m_Registry[action.Id] = list;
            }

            if (!list.Contains(action))
            {
                list.Add(action);
            }
        }

        /// <summary>
        /// Unregisters an action handler
        /// </summary>
        /// <param name="action">The action handler to unregister</param>
        public void Unregister(IAction action)
        {
            if (action == null)
                return;

            if (m_Registry.TryGetValue(action.Id, out var list))
            {
                list.Remove(action);
                if (list.Count == 0)
                {
                    m_Registry.Remove(action.Id);
                }
            }
        }

        /// <summary>
        /// Routes an incoming action message to all registered handlers
        /// </summary>
        /// <param name="message">The action message to route</param>
        private void OnAction(ActionMessage message)
        {
            if (!m_Registry.TryGetValue(message.Id, out var list))
                return;

            // Execute all handlers for this ActionId
            for (int i = 0; i < list.Count; i++)
            {
                list[i].Execute(message);
            }
        }

        /// <summary>
        /// Disposes the dispatcher and clears all registrations
        /// </summary>
        public void Dispose()
        {
            m_Subscription?.Dispose();
            m_Registry.Clear();
        }
    }
}
