using System;

namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// Message channel that buffers the last published message
    /// Late subscribers immediately receive the buffered message upon subscription
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    public class BufferedMessageChannel<T> : IBufferedMessageChannel<T>
    {
        private readonly MessageChannel<T> m_InnerChannel = new MessageChannel<T>();
        private T m_BufferedMessage;
        private bool m_HasBufferedMessage;

        /// <summary>
        /// Gets whether a message has been buffered
        /// </summary>
        public bool HasBufferedMessage
        {
            get { return m_HasBufferedMessage; }
        }

        /// <summary>
        /// Gets the last buffered message (only valid if HasBufferedMessage is true)
        /// </summary>
        public T BufferedMessage
        {
            get { return m_BufferedMessage; }
        }

        /// <summary>
        /// Gets whether this channel has been disposed
        /// </summary>
        public bool IsDisposed
        {
            get { return m_InnerChannel.IsDisposed; }
        }

        /// <summary>
        /// Publishes a message to all subscribers and stores it in the buffer
        /// </summary>
        /// <param name="message">The message to publish and buffer</param>
        public void Publish(T message)
        {
            m_BufferedMessage = message;
            m_HasBufferedMessage = true;
            m_InnerChannel.Publish(message);
        }

        /// <summary>
        /// Subscribes a handler to receive messages
        /// If a message has been buffered, the handler is immediately invoked with it
        /// </summary>
        /// <param name="handler">The callback to invoke when messages are published</param>
        /// <returns>A disposable subscription that unsubscribes when disposed</returns>
        public IDisposable Subscribe(Action<T> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            // Immediately replay buffered message to new subscriber
            if (m_HasBufferedMessage)
            {
                handler.Invoke(m_BufferedMessage);
            }

            return m_InnerChannel.Subscribe(handler);
        }

        /// <summary>
        /// Unsubscribes a handler from receiving messages
        /// </summary>
        /// <param name="handler">The handler to unsubscribe</param>
        public void Unsubscribe(Action<T> handler)
        {
            m_InnerChannel.Unsubscribe(handler);
        }

        /// <summary>
        /// Disposes the channel and clears the buffer
        /// </summary>
        public void Dispose()
        {
            m_InnerChannel.Dispose();
            m_BufferedMessage = default(T);
            m_HasBufferedMessage = false;
        }
    }
}
