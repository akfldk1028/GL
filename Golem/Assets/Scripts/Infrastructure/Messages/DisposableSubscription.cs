using System;

namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// RAII wrapper for automatic subscription cleanup
    /// Automatically unsubscribes when disposed (e.g., via using statement)
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    public sealed class DisposableSubscription<T> : IDisposable
    {
        private ISubscriber<T> m_Channel;
        private Action<T> m_Handler;
        private bool m_IsDisposed;

        /// <summary>
        /// Creates a new disposable subscription
        /// </summary>
        /// <param name="channel">The channel to unsubscribe from</param>
        /// <param name="handler">The handler to unsubscribe</param>
        public DisposableSubscription(ISubscriber<T> channel, Action<T> handler)
        {
            m_Channel = channel;
            m_Handler = handler;
            m_IsDisposed = false;
        }

        /// <summary>
        /// Disposes the subscription and unsubscribes from the channel
        /// Safe to call multiple times (idempotent)
        /// </summary>
        public void Dispose()
        {
            if (m_IsDisposed)
                return;

            if (m_Channel != null && m_Handler != null)
            {
                m_Channel.Unsubscribe(m_Handler);
            }

            m_Channel = null;
            m_Handler = null;
            m_IsDisposed = true;
        }
    }
}
