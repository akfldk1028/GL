using System;
using System.Collections.Generic;

namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// Generic pub/sub message channel with thread-safe concurrent modification handling
    /// Uses a pending queue to defer subscription changes during message publication
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    public class MessageChannel<T> : IMessageChannel<T>
    {
        private readonly List<Action<T>> m_MessageHandlers = new List<Action<T>>();

        /// <summary>
        /// Pending subscription/unsubscription operations
        /// Key: handler, Value: true = subscribe, false = unsubscribe
        /// Prevents ConcurrentModificationException during Publish()
        /// </summary>
        private readonly Dictionary<Action<T>, bool> m_PendingHandlers = new Dictionary<Action<T>, bool>();

        /// <summary>
        /// Gets whether this channel has been disposed
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Disposes the channel and clears all handlers
        /// </summary>
        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                m_MessageHandlers.Clear();
                m_PendingHandlers.Clear();
            }
        }

        /// <summary>
        /// Publishes a message to all subscribers
        /// Processes pending subscription changes before publishing
        /// </summary>
        /// <param name="message">The message to publish</param>
        public virtual void Publish(T message)
        {
            // Process pending subscription changes
            foreach (var kvp in m_PendingHandlers)
            {
                if (kvp.Value) // Subscribe
                {
                    m_MessageHandlers.Add(kvp.Key);
                }
                else // Unsubscribe
                {
                    m_MessageHandlers.Remove(kvp.Key);
                }
            }
            m_PendingHandlers.Clear();

            // Publish to all handlers
            foreach (var handler in m_MessageHandlers)
            {
                if (handler != null)
                {
                    handler.Invoke(message);
                }
            }
        }

        /// <summary>
        /// Subscribes a handler to receive messages
        /// </summary>
        /// <param name="handler">The callback to invoke when messages are published</param>
        /// <returns>A disposable subscription that unsubscribes when disposed</returns>
        public virtual IDisposable Subscribe(Action<T> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            // If already subscribed, don't add again
            if (IsSubscribed(handler))
                return new DisposableSubscription<T>(this, handler);

            // If pending unsubscribe, cancel it
            if (m_PendingHandlers.ContainsKey(handler))
            {
                if (!m_PendingHandlers[handler])
                {
                    m_PendingHandlers.Remove(handler);
                }
            }
            else
            {
                // Add to pending subscriptions
                m_PendingHandlers[handler] = true;
            }

            return new DisposableSubscription<T>(this, handler);
        }

        /// <summary>
        /// Unsubscribes a handler from receiving messages
        /// </summary>
        /// <param name="handler">The handler to unsubscribe</param>
        public void Unsubscribe(Action<T> handler)
        {
            if (handler == null)
                return;

            if (IsSubscribed(handler))
            {
                // If pending subscribe, cancel it
                if (m_PendingHandlers.ContainsKey(handler) && m_PendingHandlers[handler])
                {
                    m_PendingHandlers.Remove(handler);
                }
                else
                {
                    // Add to pending unsubscriptions
                    m_PendingHandlers[handler] = false;
                }
            }
        }

        /// <summary>
        /// Checks if a handler is currently subscribed or pending subscription
        /// </summary>
        /// <param name="handler">The handler to check</param>
        /// <returns>True if subscribed or pending subscription, false otherwise</returns>
        private bool IsSubscribed(Action<T> handler)
        {
            bool isPendingRemoval = m_PendingHandlers.ContainsKey(handler) && !m_PendingHandlers[handler];
            bool isPendingAdding = m_PendingHandlers.ContainsKey(handler) && m_PendingHandlers[handler];
            return (m_MessageHandlers.Contains(handler) && !isPendingRemoval) || isPendingAdding;
        }
    }
}
