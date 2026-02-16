namespace Golem.Infrastructure.Messages
{
    public interface IActionPayload { }

    public readonly struct ActionMessage
    {
        public ActionId Id { get; }
        public IActionPayload Payload { get; }

        private ActionMessage(ActionId id, IActionPayload payload = null)
        {
            Id = id;
            Payload = payload;
        }

        public static ActionMessage From(ActionId id) => new ActionMessage(id);
        public static ActionMessage From(ActionId id, IActionPayload payload) => new ActionMessage(id, payload);

        public bool TryGetPayload<T>(out T payload) where T : class, IActionPayload
        {
            payload = Payload as T;
            return payload != null;
        }
    }

    public interface IAction
    {
        ActionId Id { get; }
        void Execute(ActionMessage message);
    }
}
