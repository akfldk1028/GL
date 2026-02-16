namespace Golem.Infrastructure.State
{
    public enum StateId
    {
        None = 0,
        Boot,
        Initializing,
        Connected,
        Disconnected,
        Active,
        Idle,
        Performing
    }
}
