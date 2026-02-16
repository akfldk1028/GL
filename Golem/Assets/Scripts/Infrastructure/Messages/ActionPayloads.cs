using System.Collections.Generic;
using UnityEngine;

namespace Golem.Infrastructure.Messages
{
    // ── AGENT META PAYLOADS ─────────────────────────────────────

    public class AgentStatePayload : IActionPayload
    {
        public string Status { get; set; }
        public string LastActionType { get; set; }
        public bool RoutinesRunning { get; set; }
    }

    public class AgentErrorPayload : IActionPayload
    {
        public string ErrorMessage { get; set; }
        public string Exception { get; set; }
    }

    /// <summary>
    /// Shared payload for Agent_ActionStarted / ActionCompleted / ActionFailed.
    /// </summary>
    public class ActionLifecyclePayload : IActionPayload
    {
        public ActionId SourceAction { get; set; }
        public string ActionName { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public float Duration { get; set; }
    }

    // ── EXPRESSION / EMOTE PAYLOADS ─────────────────────────────

    public class VoiceEmotePayload : IActionPayload
    {
        public string Type { get; set; }
        public string AudioBase64 { get; set; }
    }

    public class AnimatedEmotePayload : IActionPayload
    {
        public string Type { get; set; }
        public string AudioBase64 { get; set; }
        public string AnimationName { get; set; }
        public float AnimationDuration { get; set; }
    }

    public class FacialExpressionPayload : IActionPayload
    {
        public string Expression { get; set; }
        public float Intensity { get; set; }
    }

    /// <summary>
    /// BML gesture payload — name + dominant hand.
    /// </summary>
    public class GesturePayload : IActionPayload
    {
        public string Name { get; set; }
        public string Hand { get; set; } // "left", "right", "both"
    }

    /// <summary>
    /// BML gaze payload — target name or world position.
    /// </summary>
    public class GazePayload : IActionPayload
    {
        public string Target { get; set; }
        public Vector3 Position { get; set; }
    }

    // ── LOCOMOTION PAYLOADS ─────────────────────────────────────

    public class MoveToLocationPayload : IActionPayload
    {
        public string Location { get; set; }
        public Vector3 Destination { get; set; }
    }

    // ── POSTURE PAYLOADS ────────────────────────────────────────

    public class SitAtChairPayload : IActionPayload
    {
        public int ChairNumber { get; set; }
    }

    public class IdlePayload : IActionPayload
    {
        public string IdleType { get; set; } // "standing", "sitting", "leaning"
    }

    // ── INTERACTION PAYLOADS ────────────────────────────────────

    public class ExamineMenuPayload : IActionPayload
    {
        public string Focus { get; set; }
    }

    public class PlayArcadePayload : IActionPayload
    {
        public string Game { get; set; }
    }

    // ── CAMERA PAYLOADS ─────────────────────────────────────────

    public class ChangeCameraAnglePayload : IActionPayload
    {
        public string Angle { get; set; }
        public string Transition { get; set; }
    }

    // ── GENERIC / FALLBACK ──────────────────────────────────────

    public class CharacterActionPayload : IActionPayload
    {
        public string ActionType { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

        public CharacterActionPayload()
        {
            Parameters = new Dictionary<string, object>();
        }
    }

    // ── COMPOSITE PAYLOADS ── BML sync + Generative Agents ──────

    /// <summary>
    /// Composite_MultiAction — parallel execution of sub-actions.
    /// </summary>
    public class MultiActionPayload : IActionPayload
    {
        public List<SubAction> Actions { get; set; } = new List<SubAction>();
    }

    /// <summary>
    /// Composite_Sequence — sequential execution of sub-actions.
    /// </summary>
    public class SequencePayload : IActionPayload
    {
        public List<SubAction> Actions { get; set; } = new List<SubAction>();
    }

    /// <summary>
    /// Lightweight sub-action descriptor used inside Composite payloads.
    /// </summary>
    public class SubAction
    {
        public string Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

        public SubAction()
        {
            Parameters = new Dictionary<string, object>();
        }
    }

    // ── FEEDBACK PAYLOADS ───────────────────────────────────────

    public class ActionResultPayload : IActionPayload
    {
        public ActionId SourceAction { get; set; }
        public string ActionName { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}
