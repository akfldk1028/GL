using System;
using System.Collections.Generic;
using UnityEngine;

namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// Generic pub/sub message channel. Ported from MB_N2N.
    /// Uses pending handler dictionary to safely handle subscribe/unsubscribe during publish.
    /// </summary>
    public class MessageChannel<T> : IMessageChannel<T>
    {
        readonly List<Action<T>> m_MessageHandlers = new List<Action<T>>();
        readonly Dictionary<Action<T>, bool> m_PendingHandlers = new Dictionary<Action<T>, bool>();

        public bool IsDisposed { get; private set; } = false;

        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                m_MessageHandlers.Clear();
                m_PendingHandlers.Clear();
            }
        }

        public virtual void Publish(T message)
        {
            // Apply pending subscribe/unsubscribe operations
            foreach (var handler in m_PendingHandlers.Keys)
            {
                if (m_PendingHandlers[handler])
                    m_MessageHandlers.Add(handler);
                else
                    m_MessageHandlers.Remove(handler);
            }
            m_PendingHandlers.Clear();

            // Invoke all active handlers
            foreach (var messageHandler in m_MessageHandlers)
            {
                if (messageHandler != null)
                    messageHandler.Invoke(message);
            }
        }

        public virtual IDisposable Subscribe(Action<T> handler)
        {
            if (m_PendingHandlers.ContainsKey(handler))
            {
                if (!m_PendingHandlers[handler])
                    m_PendingHandlers.Remove(handler);
            }
            else
            {
                m_PendingHandlers[handler] = true;
            }

            return new DisposableSubscription<T>(this, handler);
        }

        public void Unsubscribe(Action<T> handler)
        {
            if (IsSubscribed(handler))
            {
                if (m_PendingHandlers.ContainsKey(handler))
                {
                    if (m_PendingHandlers[handler])
                        m_PendingHandlers.Remove(handler);
                }
                else
                {
                    m_PendingHandlers[handler] = false;
                }
            }
        }

        bool IsSubscribed(Action<T> handler)
        {
            var isPendingRemoval = m_PendingHandlers.ContainsKey(handler) && !m_PendingHandlers[handler];
            var isPendingAdding = m_PendingHandlers.ContainsKey(handler) && m_PendingHandlers[handler];
            return m_MessageHandlers.Contains(handler) && !isPendingRemoval || isPendingAdding;
        }
    }
}
