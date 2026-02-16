namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// Taxonomy of action message types in the Golem system
    /// Organized into categories by numeric ranges:
    /// - 100s: System lifecycle events
    /// - 200s: Agent state events
    /// - 300s: Facial expression and emote events
    /// - 400s: Character action events
    /// - 500s: Camera control events
    /// </summary>
    public enum ActionId
    {
        // ===== SYSTEM LIFECYCLE (100s) =====

        /// <summary>
        /// Fired every frame during Unity Update()
        /// </summary>
        System_Update = 100,

        /// <summary>
        /// Fired every frame during Unity LateUpdate()
        /// </summary>
        System_LateUpdate = 101,

        /// <summary>
        /// Fired at fixed intervals during Unity FixedUpdate()
        /// </summary>
        System_FixedUpdate = 102,

        // ===== AGENT STATE (200s) =====

        /// <summary>
        /// Agent state changed (from CFConnector)
        /// </summary>
        Agent_StateChanged = 200,

        /// <summary>
        /// Agent error occurred
        /// </summary>
        Agent_Error = 201,

        /// <summary>
        /// Voice emote received (from CFConnector)
        /// </summary>
        Agent_VoiceEmote = 202,

        /// <summary>
        /// Animated emote received (from CFConnector)
        /// </summary>
        Agent_AnimatedEmote = 203,

        // ===== FACIAL EXPRESSIONS AND EMOTES (300s) =====

        /// <summary>
        /// Facial expression change (from CFConnector)
        /// </summary>
        Emote_FacialExpression = 300,

        /// <summary>
        /// Idle emote/expression
        /// </summary>
        Emote_Idle = 301,

        /// <summary>
        /// Custom emote
        /// </summary>
        Emote_Custom = 302,

        // ===== CHARACTER ACTIONS (400s) =====

        /// <summary>
        /// Move character to a named location
        /// </summary>
        Character_MoveToLocation = 400,

        /// <summary>
        /// Sit at a specific chair
        /// </summary>
        Character_SitAtChair = 401,

        /// <summary>
        /// Examine/interact with menu
        /// </summary>
        Character_ExamineMenu = 402,

        /// <summary>
        /// Play arcade game
        /// </summary>
        Character_PlayArcade = 403,

        /// <summary>
        /// Walk to a position
        /// </summary>
        Character_WalkTo = 404,

        /// <summary>
        /// Run to a position
        /// </summary>
        Character_RunTo = 405,

        /// <summary>
        /// Jump action
        /// </summary>
        Character_Jump = 406,

        // ===== CAMERA (500s) =====

        /// <summary>
        /// Change camera angle/view
        /// </summary>
        Camera_ChangeAngle = 500
    }
}
