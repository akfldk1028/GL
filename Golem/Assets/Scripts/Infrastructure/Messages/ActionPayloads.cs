using System.Collections.Generic;
using UnityEngine;

namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// Payload for Character_MoveToLocation action
    /// Maps to character movement commands
    /// </summary>
    public class MoveToLocationPayload : IActionPayload
    {
        /// <summary>
        /// The named location to move to (e.g., "bar", "chair_1")
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// The destination position in world space
        /// </summary>
        public Vector3 Destination { get; set; }

        public MoveToLocationPayload(string location, Vector3 destination)
        {
            Location = location;
            Destination = destination;
        }
    }

    /// <summary>
    /// Payload for Character_SitAtChair action
    /// </summary>
    public class SitAtChairPayload : IActionPayload
    {
        /// <summary>
        /// The chair number to sit at
        /// </summary>
        public int ChairNumber { get; set; }

        public SitAtChairPayload(int chairNumber)
        {
            ChairNumber = chairNumber;
        }
    }

    /// <summary>
    /// Payload for Agent_VoiceEmote action
    /// Maps CFConnector.VoiceEmoteData
    /// </summary>
    public class VoiceEmotePayload : IActionPayload
    {
        /// <summary>
        /// The emote type (e.g., "voice")
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Base64-encoded audio data
        /// </summary>
        public string AudioBase64 { get; set; }

        public VoiceEmotePayload(string type, string audioBase64)
        {
            Type = type;
            AudioBase64 = audioBase64;
        }
    }

    /// <summary>
    /// Payload for Agent_AnimatedEmote action
    /// Maps CFConnector.AnimatedEmoteData
    /// </summary>
    public class AnimatedEmotePayload : IActionPayload
    {
        /// <summary>
        /// The emote type (e.g., "animated")
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Base64-encoded audio data
        /// </summary>
        public string AudioBase64 { get; set; }

        /// <summary>
        /// The animation name to play
        /// </summary>
        public string AnimationName { get; set; }

        /// <summary>
        /// Duration of the animation in seconds
        /// </summary>
        public float AnimationDuration { get; set; }

        public AnimatedEmotePayload(string type, string audioBase64, string animationName, float animationDuration)
        {
            Type = type;
            AudioBase64 = audioBase64;
            AnimationName = animationName;
            AnimationDuration = animationDuration;
        }
    }

    /// <summary>
    /// Payload for Emote_FacialExpression action
    /// Maps CFConnector.FacialExpressionData
    /// </summary>
    public class FacialExpressionPayload : IActionPayload
    {
        /// <summary>
        /// The facial expression (e.g., "happy", "sad", "angry")
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Expression intensity (0.0 to 1.0)
        /// </summary>
        public float Intensity { get; set; }

        public FacialExpressionPayload(string expression, float intensity)
        {
            Expression = expression;
            Intensity = intensity;
        }
    }

    /// <summary>
    /// Payload for Agent_StateChanged action
    /// Maps CFConnector.AgentState
    /// </summary>
    public class AgentStatePayload : IActionPayload
    {
        /// <summary>
        /// The agent status (from AgentState.state.status)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Last action type the agent performed
        /// </summary>
        public string LastActionType { get; set; }

        /// <summary>
        /// Whether routines are currently running
        /// </summary>
        public bool RoutinesRunning { get; set; }

        public AgentStatePayload(string status, string lastActionType, bool routinesRunning)
        {
            Status = status;
            LastActionType = lastActionType;
            RoutinesRunning = routinesRunning;
        }
    }

    /// <summary>
    /// Payload for Agent_Error action
    /// </summary>
    public class AgentErrorPayload : IActionPayload
    {
        /// <summary>
        /// The error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Exception details (if available)
        /// </summary>
        public string Exception { get; set; }

        public AgentErrorPayload(string errorMessage, string exception = null)
        {
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }

    /// <summary>
    /// Payload for generic character actions
    /// Maps CFConnector.CharacterActionData
    /// </summary>
    public class CharacterActionPayload : IActionPayload
    {
        /// <summary>
        /// The action type (e.g., "move", "sit", "examine")
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// Extensible parameters dictionary
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }

        public CharacterActionPayload(string actionType, Dictionary<string, object> parameters = null)
        {
            ActionType = actionType;
            Parameters = parameters ?? new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Payload for Character_ExamineMenu action
    /// </summary>
    public class ExamineMenuPayload : IActionPayload
    {
        /// <summary>
        /// The menu ID to examine
        /// </summary>
        public string MenuId { get; set; }

        public ExamineMenuPayload(string menuId)
        {
            MenuId = menuId;
        }
    }

    /// <summary>
    /// Payload for Character_PlayArcade action
    /// </summary>
    public class PlayArcadePayload : IActionPayload
    {
        /// <summary>
        /// The arcade game ID to play
        /// </summary>
        public string ArcadeId { get; set; }

        public PlayArcadePayload(string arcadeId)
        {
            ArcadeId = arcadeId;
        }
    }

    /// <summary>
    /// Payload for Camera_ChangeAngle action
    /// </summary>
    public class ChangeCameraAnglePayload : IActionPayload
    {
        /// <summary>
        /// The camera angle identifier
        /// </summary>
        public int AngleId { get; set; }

        public ChangeCameraAnglePayload(int angleId)
        {
            AngleId = angleId;
        }
    }

    /// <summary>
    /// Payload for Emote_Idle action (marker payload, no data)
    /// </summary>
    public class IdlePayload : IActionPayload
    {
        /// <summary>
        /// Singleton instance for idle payload
        /// </summary>
        public static readonly IdlePayload Instance = new IdlePayload();

        private IdlePayload()
        {
        }
    }
}
