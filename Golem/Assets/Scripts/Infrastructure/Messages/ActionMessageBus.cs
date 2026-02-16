using System;
using System.Collections.Generic;
using UnityEngine;

namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// Sealed wrapper around MessageChannel&lt;ActionMessage&gt; with filtered Subscribe overloads.
    /// Ported from MB_N2N ActionMessageBus.
    /// </summary>
    public sealed class ActionMessageBus : IMessageChannel<ActionMessage>
    {
        private readonly MessageChannel<ActionMessage> _channel = new MessageChannel<ActionMessage>();

        public bool IsDisposed => _channel.IsDisposed;

        public void Publish(ActionMessage message)
        {
            if (IsDisposed)
            {
                Debug.LogWarning("[ActionMessageBus] Tried to publish after disposal.");
                return;
            }

            _channel.Publish(message);
        }

        /// <summary>Subscribe to ALL messages.</summary>
        public IDisposable Subscribe(Action<ActionMessage> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return _channel.Subscribe(handler);
        }

        /// <summary>Subscribe to a specific ActionId with parameterless handler.</summary>
        public IDisposable Subscribe(ActionId actionId, Action handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return Subscribe(actionId, _ => handler());
        }

        /// <summary>Subscribe to a specific ActionId.</summary>
        public IDisposable Subscribe(ActionId actionId, Action<ActionMessage> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return _channel.Subscribe(message =>
            {
                if (message.Id == actionId)
                {
                    handler(message);
                }
            });
        }

        /// <summary>Subscribe to multiple ActionIds.</summary>
        public IDisposable Subscribe(Action<ActionMessage> handler, params ActionId[] actionIds) =>
            Subscribe((IEnumerable<ActionId>)actionIds, handler);

        public IDisposable Subscribe(IEnumerable<ActionId> actionIds, Action<ActionMessage> handler)
        {
            if (actionIds == null) throw new ArgumentNullException(nameof(actionIds));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var filter = new HashSet<ActionId>(actionIds);
            if (filter.Count == 0)
                throw new ArgumentException("At least one ActionId is required.", nameof(actionIds));

            return _channel.Subscribe(message =>
            {
                if (filter.Contains(message.Id))
                {
                    handler(message);
                }
            });
        }

        public void Unsubscribe(Action<ActionMessage> handler)
        {
            if (handler == null) return;
            _channel.Unsubscribe(handler);
        }

        public void Dispose() => _channel.Dispose();
    }
}
