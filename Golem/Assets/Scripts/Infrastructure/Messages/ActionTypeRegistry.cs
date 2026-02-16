using System;
using System.Collections.Generic;
using UnityEngine;

namespace Golem.Infrastructure.Messages
{
    /// <summary>
    /// Bidirectional mapping between AI server action type strings and ActionId enum values.
    /// Replaces hardcoded switch statements with a runtime-extensible registry.
    /// Also provides a PayloadFactory for creating typed payloads from raw parameter dictionaries.
    /// </summary>
    public sealed class ActionTypeRegistry
    {
        private readonly Dictionary<string, ActionId> _stringToId = new Dictionary<string, ActionId>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<ActionId, string> _idToString = new Dictionary<ActionId, string>();
        private readonly Dictionary<ActionId, Func<Dictionary<string, object>, IActionPayload>> _payloadFactories
            = new Dictionary<ActionId, Func<Dictionary<string, object>, IActionPayload>>();

        public ActionTypeRegistry()
        {
            RegisterDefaults();
        }

        /// <summary>
        /// Register an action type mapping with optional payload factory.
        /// </summary>
        public void Register(string serverName, ActionId id, Func<Dictionary<string, object>, IActionPayload> factory = null)
        {
            _stringToId[serverName] = id;
            _idToString[id] = serverName;
            if (factory != null)
                _payloadFactories[id] = factory;
        }

        /// <summary>
        /// Map AI server string → ActionId. Returns ActionId.None if unknown.
        /// </summary>
        public ActionId MapToActionId(string serverType)
        {
            if (string.IsNullOrEmpty(serverType)) return ActionId.None;
            return _stringToId.TryGetValue(serverType, out var id) ? id : ActionId.None;
        }

        /// <summary>
        /// Map ActionId → AI server string. Returns null if unknown.
        /// </summary>
        public string MapToString(ActionId id)
        {
            return _idToString.TryGetValue(id, out var s) ? s : null;
        }

        /// <summary>
        /// Create a typed payload from raw parameters using the registered factory.
        /// Falls back to CharacterActionPayload if no factory is registered.
        /// </summary>
        public IActionPayload CreatePayload(ActionId id, Dictionary<string, object> parameters)
        {
            if (_payloadFactories.TryGetValue(id, out var factory))
                return factory(parameters);

            return null;
        }

        /// <summary>
        /// Create payload from server type string.
        /// </summary>
        public IActionPayload CreatePayload(string serverType, Dictionary<string, object> parameters)
        {
            var id = MapToActionId(serverType);
            return CreatePayload(id, parameters);
        }

        public bool HasMapping(string serverType) => _stringToId.ContainsKey(serverType);

        // ── Default Registrations ──────────────────────────────────

        private void RegisterDefaults()
        {
            // Locomotion
            Register("moveToLocation", ActionId.Character_MoveToLocation, p => new MoveToLocationPayload
            {
                Location = ParamHelper.GetString(p, "location", "cafe"),
                Destination = ParamHelper.GetVector3(p, "destination")
            });
            Register("walkTo", ActionId.Character_WalkTo, p => new MoveToLocationPayload
            {
                Location = ParamHelper.GetString(p, "location"),
                Destination = ParamHelper.GetVector3(p, "destination")
            });
            Register("runTo", ActionId.Character_RunTo, p => new MoveToLocationPayload
            {
                Location = ParamHelper.GetString(p, "location"),
                Destination = ParamHelper.GetVector3(p, "destination")
            });
            Register("stop", ActionId.Character_Stop);
            Register("turnTo", ActionId.Character_TurnTo, p => new GazePayload
            {
                Target = ParamHelper.GetString(p, "target"),
                Position = ParamHelper.GetVector3(p, "position")
            });

            // Posture
            Register("sitAtChair", ActionId.Character_SitAtChair, p => new SitAtChairPayload
            {
                ChairNumber = ParamHelper.GetInt(p, "chairNumber", 1)
            });
            Register("standUp", ActionId.Character_StandUp);
            Register("idle", ActionId.Character_Idle, p => new IdlePayload
            {
                IdleType = ParamHelper.GetString(p, "idleType", "standing")
            });
            Register("lean", ActionId.Character_Lean);

            // Interaction
            Register("examineMenu", ActionId.Character_ExamineMenu, p => new ExamineMenuPayload
            {
                Focus = ParamHelper.GetString(p, "focus")
            });
            Register("lookAt", ActionId.Character_LookAt, p => new GazePayload
            {
                Target = ParamHelper.GetString(p, "target"),
                Position = ParamHelper.GetVector3(p, "position")
            });
            Register("playArcadeGame", ActionId.Character_PlayArcade, p => new PlayArcadePayload
            {
                Game = ParamHelper.GetString(p, "game")
            });
            Register("playClaw", ActionId.Character_PlayClaw);

            // Camera
            Register("changeCameraAngle", ActionId.Camera_ChangeAngle, p => new ChangeCameraAnglePayload
            {
                Angle = ParamHelper.GetString(p, "angle"),
                Transition = ParamHelper.GetString(p, "transition")
            });

            // Expression / Emote
            Register("facial_expression", ActionId.Agent_FacialExpression, p => new FacialExpressionPayload
            {
                Expression = ParamHelper.GetString(p, "expression"),
                Intensity = ParamHelper.GetFloat(p, "intensity", 1f)
            });
            Register("gesture", ActionId.Agent_Gesture, p => new GesturePayload
            {
                Name = ParamHelper.GetString(p, "name"),
                Hand = ParamHelper.GetString(p, "hand", "right")
            });
            Register("gazeAt", ActionId.Agent_GazeAt, p => new GazePayload
            {
                Target = ParamHelper.GetString(p, "target"),
                Position = ParamHelper.GetVector3(p, "position")
            });
            Register("gazeAway", ActionId.Agent_GazeAway);

            // Social
            Register("greet", ActionId.Social_Greet);
            Register("wave", ActionId.Social_Wave);
            Register("nod", ActionId.Social_Nod);
            Register("headShake", ActionId.Social_HeadShake);
            Register("point", ActionId.Social_Point, p => new GazePayload
            {
                Target = ParamHelper.GetString(p, "target"),
                Position = ParamHelper.GetVector3(p, "position")
            });

            // Composite
            Register("multi", ActionId.Composite_MultiAction, CreateCompositePayload);
            Register("sequence", ActionId.Composite_Sequence, CreateSequencePayload);
        }

        private IActionPayload CreateCompositePayload(Dictionary<string, object> p)
        {
            var payload = new MultiActionPayload();
            var actions = ParamHelper.GetActionList(p, "actions");
            foreach (var a in actions)
            {
                payload.Actions.Add(new SubAction
                {
                    Type = ParamHelper.GetString(a, "type"),
                    Parameters = a.TryGetValue("parameters", out var pObj) && pObj is Dictionary<string, object> pDict
                        ? pDict : new Dictionary<string, object>()
                });
            }
            return payload;
        }

        private IActionPayload CreateSequencePayload(Dictionary<string, object> p)
        {
            var payload = new SequencePayload();
            var actions = ParamHelper.GetActionList(p, "actions");
            foreach (var a in actions)
            {
                payload.Actions.Add(new SubAction
                {
                    Type = ParamHelper.GetString(a, "type"),
                    Parameters = a.TryGetValue("parameters", out var pObj) && pObj is Dictionary<string, object> pDict
                        ? pDict : new Dictionary<string, object>()
                });
            }
            return payload;
        }
    }
}
