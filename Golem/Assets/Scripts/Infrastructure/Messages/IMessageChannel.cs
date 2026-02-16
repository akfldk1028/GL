using System;

namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// Interface for publishing messages to subscribers
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    public interface IPublisher<T>
    {
        /// <summary>
        /// Publishes a message to all subscribers
        /// </summary>
        /// <param name="message">The message to publish</param>
        void Publish(T message);
    }

    /// <summary>
    /// Interface for subscribing to messages
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    public interface ISubscriber<T>
    {
        /// <summary>
        /// Subscribes to messages with a handler callback
        /// </summary>
        /// <param name="handler">The callback to invoke when messages are published</param>
        /// <returns>A disposable subscription that unsubscribes when disposed</returns>
        IDisposable Subscribe(Action<T> handler);

        /// <summary>
        /// Unsubscribes a handler from receiving messages
        /// </summary>
        /// <param name="handler">The handler to unsubscribe</param>
        void Unsubscribe(Action<T> handler);
    }

    /// <summary>
    /// Combined publisher/subscriber message channel
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    public interface IMessageChannel<T> : IPublisher<T>, ISubscriber<T>, IDisposable
    {
        /// <summary>
        /// Gets whether this channel has been disposed
        /// </summary>
        bool IsDisposed { get; }
    }

    /// <summary>
    /// Message channel that buffers the last published message for replay to late subscribers
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    public interface IBufferedMessageChannel<T> : IMessageChannel<T>
    {
        /// <summary>
        /// Gets whether a message has been buffered
        /// </summary>
        bool HasBufferedMessage { get; }

        /// <summary>
        /// Gets the last buffered message (only valid if HasBufferedMessage is true)
        /// </summary>
        T BufferedMessage { get; }
    }
}
