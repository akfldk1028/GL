using Golem.Infrastructure.Messages;

namespace Golem.Character.Autonomous
{
    /// <summary>
    /// Value object representing an autonomous action to perform.
    /// </summary>
    public class AutonomousAction
    {
        public ActionId ActionId { get; set; }
        public IActionPayload Payload { get; set; }
        public float ExpectedDuration { get; set; }
        public string Description { get; set; }
    }
}
