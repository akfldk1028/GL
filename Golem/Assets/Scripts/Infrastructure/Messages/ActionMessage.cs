using System;

namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// Marker interface for action message payloads
    /// All payload classes must implement this interface
    /// </summary>
    public interface IActionPayload
    {
    }

    /// <summary>
    /// Interface for command pattern action handlers
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// Gets the ActionId this handler responds to
        /// </summary>
        ActionId Id { get; }

        /// <summary>
        /// Executes the action with the given message
        /// </summary>
        /// <param name="message">The action message containing the payload</param>
        void Execute(ActionMessage message);
    }

    /// <summary>
    /// Immutable message structure containing an ActionId and optional typed payload
    /// Provides type-safe payload extraction via TryGetPayload()
    /// </summary>
    public readonly struct ActionMessage
    {
        /// <summary>
        /// Gets the action identifier
        /// </summary>
        public ActionId Id { get; }

        /// <summary>
        /// Gets the payload (may be null for messages without data)
        /// </summary>
        public IActionPayload Payload { get; }

        /// <summary>
        /// Private constructor (use factory methods From() instead)
        /// </summary>
        /// <param name="id">The action identifier</param>
        /// <param name="payload">The optional payload</param>
        private ActionMessage(ActionId id, IActionPayload payload)
        {
            Id = id;
            Payload = payload;
        }

        /// <summary>
        /// Creates an action message without a payload
        /// </summary>
        /// <param name="id">The action identifier</param>
        /// <returns>An action message with no payload</returns>
        public static ActionMessage From(ActionId id)
        {
            return new ActionMessage(id, null);
        }

        /// <summary>
        /// Creates an action message with a typed payload
        /// </summary>
        /// <param name="id">The action identifier</param>
        /// <param name="payload">The payload data</param>
        /// <returns>An action message with the specified payload</returns>
        public static ActionMessage From(ActionId id, IActionPayload payload)
        {
            return new ActionMessage(id, payload);
        }

        /// <summary>
        /// Attempts to extract the payload as a specific type
        /// </summary>
        /// <typeparam name="T">The expected payload type (must be a class implementing IActionPayload)</typeparam>
        /// <param name="payload">The extracted payload if successful, null otherwise</param>
        /// <returns>True if the payload was successfully extracted, false otherwise</returns>
        public bool TryGetPayload<T>(out T payload) where T : class, IActionPayload
        {
            if (Payload is T typed)
            {
                payload = typed;
                return true;
            }

            payload = null;
            return false;
        }
    }
}
