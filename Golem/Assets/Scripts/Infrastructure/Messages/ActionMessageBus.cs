using System;
using System.Collections.Generic;

namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// High-level message bus for ActionMessage with filtered subscription support
    /// Wraps MessageChannel{ActionMessage} and provides convenience methods for filtering by ActionId
    /// </summary>
    public sealed class ActionMessageBus : IMessageChannel<ActionMessage>
    {
        private readonly MessageChannel<ActionMessage> m_Channel = new MessageChannel<ActionMessage>();

        /// <summary>
        /// Gets whether this bus has been disposed
        /// </summary>
        public bool IsDisposed
        {
            get { return m_Channel.IsDisposed; }
        }

        /// <summary>
        /// Publishes an action message to all subscribers
        /// </summary>
        /// <param name="message">The action message to publish</param>
        public void Publish(ActionMessage message)
        {
            m_Channel.Publish(message);
        }

        /// <summary>
        /// Subscribes to ALL action messages (no filtering)
        /// </summary>
        /// <param name="handler">The callback to invoke for all messages</param>
        /// <returns>A disposable subscription</returns>
        public IDisposable Subscribe(Action<ActionMessage> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return m_Channel.Subscribe(handler);
        }

        /// <summary>
        /// Subscribes to messages with a specific ActionId
        /// </summary>
        /// <param name="actionId">The ActionId to filter by</param>
        /// <param name="handler">The callback to invoke for matching messages</param>
        /// <returns>A disposable subscription</returns>
        public IDisposable Subscribe(ActionId actionId, Action<ActionMessage> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return m_Channel.Subscribe(message =>
            {
                if (message.Id == actionId)
                {
                    handler(message);
                }
            });
        }

        /// <summary>
        /// Subscribes to messages with a specific ActionId (parameterless handler)
        /// Useful when you don't need the message payload
        /// </summary>
        /// <param name="actionId">The ActionId to filter by</param>
        /// <param name="handler">The parameterless callback to invoke</param>
        /// <returns>A disposable subscription</returns>
        public IDisposable Subscribe(ActionId actionId, Action handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return Subscribe(actionId, _ => handler());
        }

        /// <summary>
        /// Subscribes to messages matching any of the specified ActionIds
        /// </summary>
        /// <param name="handler">The callback to invoke for matching messages</param>
        /// <param name="actionIds">The ActionIds to filter by (params array)</param>
        /// <returns>A disposable subscription</returns>
        public IDisposable Subscribe(Action<ActionMessage> handler, params ActionId[] actionIds)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (actionIds == null || actionIds.Length == 0)
                throw new ArgumentException("At least one ActionId is required", nameof(actionIds));

            // Use HashSet for O(1) lookup
            var filter = new HashSet<ActionId>(actionIds);

            return m_Channel.Subscribe(message =>
            {
                if (filter.Contains(message.Id))
                {
                    handler(message);
                }
            });
        }

        /// <summary>
        /// Unsubscribes a handler from receiving messages
        /// </summary>
        /// <param name="handler">The handler to unsubscribe</param>
        public void Unsubscribe(Action<ActionMessage> handler)
        {
            m_Channel.Unsubscribe(handler);
        }

        /// <summary>
        /// Disposes the bus and clears all subscriptions
        /// </summary>
        public void Dispose()
        {
            m_Channel.Dispose();
        }
    }
}
