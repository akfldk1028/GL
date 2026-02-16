using System.Collections.Generic;

namespace Golem.Infrastructure.Messages
{
    // For Character_MoveToLocation
    public class MoveToLocationPayload : IActionPayload
    {
        public string Location { get; set; }
        public UnityEngine.Vector3 Destination { get; set; }
    }

    // For Character_SitAtChair
    public class SitAtChairPayload : IActionPayload
    {
        public int ChairNumber { get; set; }
    }

    // For Character_ExamineMenu
    public class ExamineMenuPayload : IActionPayload
    {
        public string Focus { get; set; }
    }

    // For Character_PlayArcade
    public class PlayArcadePayload : IActionPayload
    {
        public string Game { get; set; }
    }

    // For Character_ChangeCameraAngle
    public class ChangeCameraAnglePayload : IActionPayload
    {
        public string Angle { get; set; }
        public string Transition { get; set; }
    }

    // For Character_Idle
    public class IdlePayload : IActionPayload
    {
        public string IdleType { get; set; }
    }

    // For Agent_VoiceEmote (wraps CFConnector.VoiceEmoteData)
    public class VoiceEmotePayload : IActionPayload
    {
        public string Type { get; set; }
        public string AudioBase64 { get; set; }
    }

    // For Agent_AnimatedEmote (wraps CFConnector.AnimatedEmoteData)
    public class AnimatedEmotePayload : IActionPayload
    {
        public string Type { get; set; }
        public string AudioBase64 { get; set; }
        public string AnimationName { get; set; }
        public float AnimationDuration { get; set; }
    }

    // For Agent_FacialExpression (wraps CFConnector.FacialExpressionData)
    public class FacialExpressionPayload : IActionPayload
    {
        public string Expression { get; set; }
        public float Intensity { get; set; }
    }

    // For Agent_StateChanged (wraps CFConnector.AgentState)
    public class AgentStatePayload : IActionPayload
    {
        public string Status { get; set; }
        public string LastActionType { get; set; }
        public bool RoutinesRunning { get; set; }
    }

    // For Agent_Error
    public class AgentErrorPayload : IActionPayload
    {
        public string ErrorMessage { get; set; }
        public System.Exception Exception { get; set; }
    }

    // Generic payload wrapping a raw CharacterActionData for extensibility
    public class CharacterActionPayload : IActionPayload
    {
        public string ActionType { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}
