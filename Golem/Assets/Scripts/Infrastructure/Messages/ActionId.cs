namespace Golem.Infrastructure.Messages
{
    public enum ActionId
    {
        None = 0,

        // System lifecycle (from Managers Update/LateUpdate/FixedUpdate)
        System_Update = 100,
        System_LateUpdate = 101,
        System_FixedUpdate = 102,

        // Agent connection events (from CFConnector)
        Agent_Connected = 200,
        Agent_Disconnected = 201,
        Agent_StateChanged = 202,
        Agent_Error = 203,

        // Emote events (from CFConnector)
        Agent_VoiceEmote = 300,
        Agent_AnimatedEmote = 301,
        Agent_FacialExpression = 302,

        // Character actions (from CFConnector.OnCharacterAction)
        Character_MoveToLocation = 400,
        Character_SitAtChair = 401,
        Character_StandUp = 402,
        Character_ExamineMenu = 403,
        Character_PlayArcade = 404,
        Character_ChangeCameraAngle = 405,
        Character_Idle = 406,

        // Camera
        Camera_ChangeState = 500,
    }
}
