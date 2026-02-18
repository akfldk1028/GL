using System;
using System.Collections.Generic;
using UnityEngine;

namespace Golem.Character.FSM
{
    /// <summary>
    /// Character behavior finite state machine.
    /// Manages state registration, transitions, and per-frame updates.
    /// Ported from MB_N2N StateMachine pattern, adapted for character behavior.
    /// </summary>
    public sealed class CharacterBehaviorFSM
    {
        private readonly Dictionary<CharacterStateId, ICharacterState> _states = new();
        private readonly CharacterStateContext _context;
        private ICharacterState _current;

        public CharacterStateId CurrentStateId => _current?.Id ?? CharacterStateId.None;
        public CharacterStateId PreviousStateId { get; private set; }

        /// <summary>Fires (previousState, newState) on every successful transition.</summary>
        public event Action<CharacterStateId, CharacterStateId> OnStateChanged;

        public CharacterBehaviorFSM(CharacterStateContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.FSM = this;
        }

        public void RegisterState(ICharacterState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            _states[state.Id] = state;
        }

        /// <summary>
        /// Requests a transition. Respects CanTransitionTo check.
        /// Returns false if the current state disallows the transition.
        /// </summary>
        public bool RequestTransition(CharacterStateId target)
        {
            if (_current != null && !_current.CanTransitionTo(target))
            {
                Debug.Log($"[CharacterFSM] Transition denied: {CurrentStateId} → {target}");
                return false;
            }
            return ForceTransition(target);
        }

        /// <summary>
        /// Forces a transition regardless of CanTransitionTo.
        /// Used for interrupts (AI commands, user input).
        /// </summary>
        public bool ForceTransition(CharacterStateId target)
        {
            if (!_states.TryGetValue(target, out var next))
            {
                Debug.LogWarning($"[CharacterFSM] State {target} not registered.");
                return false;
            }

            if (_current != null && _current.Id == target)
            {
                // Re-enter same state (e.g., new Walking destination)
                _current.Exit(_context);
                _current.Enter(_context);
                return true;
            }

            PreviousStateId = _current?.Id ?? CharacterStateId.None;
            _current?.Exit(_context);
            _current = next;
            _current.Enter(_context);

            OnStateChanged?.Invoke(PreviousStateId, target);
            return true;
        }

        public void Update()
        {
            _current?.Update(_context);
        }

        public bool IsInState(CharacterStateId id) => CurrentStateId == id;

        public bool IsInAnyState(params CharacterStateId[] ids)
        {
            foreach (var id in ids)
                if (CurrentStateId == id) return true;
            return false;
        }
    }
}
