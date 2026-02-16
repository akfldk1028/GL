using System;
using System.Collections.Generic;
using Golem.Infrastructure.Messages;
using UnityEngine;

namespace Golem.Infrastructure.State
{
    public sealed class StateMachine : IDisposable
    {
        private readonly Dictionary<StateId, IState> _states = new Dictionary<StateId, IState>();
        private readonly IMessageChannel<ActionMessage> _channel;
        private readonly IDisposable _subscription;
        private IState _current;

        public StateMachine(IMessageChannel<ActionMessage> channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _subscription = _channel.Subscribe(OnAction);
        }

        public StateId CurrentId => _current?.Id ?? StateId.None;

        public void RegisterState(IState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            _states[state.Id] = state;
        }

        public void SetState(StateId id)
        {
            if (_states.TryGetValue(id, out var next))
            {
                if (_current == next) return;
                _current?.Exit();
                _current = next;
                _current.Enter();
            }
            else
            {
                Debug.LogWarning($"[StateMachine] State {id} not registered.");
            }
        }

        private void OnAction(ActionMessage message)
        {
            if (_current != null && _current.CanHandle(message.Id))
            {
                _current.Handle(message);
            }
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _states.Clear();
            _current = null;
        }
    }
}
