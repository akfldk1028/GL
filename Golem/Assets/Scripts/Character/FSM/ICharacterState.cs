namespace Golem.Character.FSM
{
    public interface ICharacterState
    {
        CharacterStateId Id { get; }
        void Enter(CharacterStateContext ctx);
        void Exit(CharacterStateContext ctx);
        void Update(CharacterStateContext ctx);
        bool CanTransitionTo(CharacterStateId target);
    }
}
