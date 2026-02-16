namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// Taxonomy of action message types in the Golem system.
    /// Based on VirtualHome (MIT), BML/SAIBA, Generative Agents (Stanford),
    /// BEHAVIOR-1K, SmartBody (USC) research.
    ///
    /// 8-Layer Pattern:
    ///   System(100) → AgentMeta(200) → Expression(300) → Locomotion(400)
    ///   → Posture(450) → Interaction(500) → Social(600) → Camera(700)
    ///   → Composite(800) → Feedback(900)
    ///
    /// Numbering: category/100, subgroup/10 → expansion room per range.
    /// </summary>
    public enum ActionId
    {
        None = 0,

        // ── SYSTEM (100-199) ──────────────────────────────────────
        System_Update           = 100,
        System_LateUpdate       = 101,
        System_FixedUpdate      = 102,

        // ── AGENT META (200-299) ──────────────────────────────────
        //   Connection (200-209)
        Agent_Connected         = 200,
        Agent_Disconnected      = 201,
        Agent_StateChanged      = 202,
        Agent_Error             = 203,
        //   Action lifecycle (210-219)
        Agent_ActionStarted     = 210,
        Agent_ActionCompleted   = 211,
        Agent_ActionFailed      = 212,
        //   Cognition (220-229) — Generative Agents pattern
        Agent_PlanUpdated       = 220,   // FUTURE
        Agent_ReflectionTriggered = 221, // FUTURE

        // ── EXPRESSION / EMOTE (300-399) ── BML-based ─────────────
        //   Voice (300-309)
        Agent_VoiceEmote        = 300,
        Agent_TTSStart          = 301,
        Agent_TTSEnd            = 302,
        //   Animation (310-319)
        Agent_AnimatedEmote     = 310,
        //   Facial (320-329) — BML faceLexeme
        Agent_FacialExpression  = 320,
        //   Gesture (330-339) — BML gesture
        Agent_Gesture           = 330,
        //   Gaze (340-349) — BML gaze
        Agent_GazeAt            = 340,
        Agent_GazeAway          = 341,

        // ── LOCOMOTION (400-449) ── VirtualHome WALK/RUN + BML ────
        Character_MoveToLocation = 400,
        Character_WalkTo        = 401,
        Character_RunTo         = 402,
        Character_Stop          = 403,
        Character_TurnTo        = 404,
        Character_Jump          = 405,

        // ── POSTURE (450-499) ── VirtualHome SIT/STANDUP + BML ────
        Character_SitAtChair    = 450,
        Character_StandUp       = 451,
        Character_Idle          = 452,
        Character_Lean          = 453,

        // ── INTERACTION (500-599) ── VirtualHome + BEHAVIOR-1K ────
        //   Observation (500-519)
        Character_ExamineMenu   = 500,
        Character_LookAt        = 501,
        //   Activity (550-579) — domain-specific
        Character_PlayArcade    = 550,
        Character_PlayClaw      = 551,

        // ── SOCIAL (600-699) ── BML speech/gesture + VirtualHome ──
        Social_Greet            = 600,
        Social_Wave             = 601,
        Social_Nod              = 602,
        Social_HeadShake        = 603,
        Social_Point            = 604,

        // ── CAMERA (700-799) ─────────────────────────────────────
        Camera_ChangeAngle      = 700,

        // ── COMPOSITE (800-899) ── BML sync + Generative Agents ──
        Composite_MultiAction   = 800,
        Composite_Sequence      = 801,

        // ── FEEDBACK (900-999) ───────────────────────────────────
        Feedback_ActionResult   = 900,
    }
}
