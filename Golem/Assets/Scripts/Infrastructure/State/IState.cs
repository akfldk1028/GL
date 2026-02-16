using Golem.Infrastructure.Messages;

namespace Golem.Infrastructure.State
{
    public interface IState
    {
        StateId Id { get; }
        void Enter();
        void Exit();
        bool CanHandle(ActionId actionId);
        void Handle(ActionMessage message);
    }
}
