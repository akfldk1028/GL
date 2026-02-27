using System.Collections;
using System.Collections.Generic;
using Golem.Character.FSM;
using Golem.Infrastructure.Messages;
using UnityEngine;
using UnityEngine.AI;

namespace Golem.Character.Autonomous
{
    public class IdleScheduler
    {
        private readonly CharacterBehaviorFSM _fsm;
        private readonly IdleSchedulerConfigSO _config;
        private readonly MonoBehaviour _runner;
        private readonly Transform _characterTransform;

        // Tier 1: LLM decision connector (null = weighted random only)
        private AIDecisionConnector _decisionConnector;
        private AIDecisionConfigSO _decisionConfig;

        private Coroutine _schedulerCoroutine;
        private Coroutine _autonomousCoroutine;
        private bool _isRunning;
        private bool _isPerformingAutonomousAction;
        private bool _publishingAutonomous;

        // Recent action tracking for LLM context (last 5)
        private readonly List<string> _recentActions = new List<string>();
        private const int MaxRecentActions = 5;

        public bool IsPerformingAutonomousAction => _isPerformingAutonomousAction;

        /// <summary>True during the synchronous PublishAction call from an autonomous action.
        /// CharacterCommandRouter checks this to avoid cancelling the action we just started.</summary>
        public bool IsPublishingAutonomous => _publishingAutonomous;

        public IdleScheduler(
            CharacterBehaviorFSM fsm,
            IdleSchedulerConfigSO config,
            MonoBehaviour runner,
            Transform characterTransform)
        {
            _fsm = fsm;
            _config = config;
            _runner = runner;
            _characterTransform = characterTransform;
        }

        /// <summary>
        /// Initialize the LLM decision connector for Tier 1 autonomous behavior.
        /// If not called, falls back to weighted random.
        /// </summary>
        public void SetDecisionConnector(AIDecisionConfigSO decisionConfig)
        {
            _decisionConfig = decisionConfig;
            if (decisionConfig != null && decisionConfig.useLLM)
            {
                _decisionConnector = new AIDecisionConnector(decisionConfig, _fsm, _characterTransform, _runner);
                Debug.Log($"[IdleScheduler] LLM decision enabled: {decisionConfig.apiType} ({decisionConfig.modelName})");
            }
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _schedulerCoroutine = _runner.StartCoroutine(SchedulerLoop());
        }

        public void Stop()
        {
            _isRunning = false;
            CancelCurrentAction();
            if (_schedulerCoroutine != null)
            {
                _runner.StopCoroutine(_schedulerCoroutine);
                _schedulerCoroutine = null;
            }
        }

        public void CancelCurrentAction()
        {
            _isPerformingAutonomousAction = false;
            _publishingAutonomous = false;
            if (_autonomousCoroutine != null)
            {
                _runner.StopCoroutine(_autonomousCoroutine);
                _autonomousCoroutine = null;
            }
        }

        private IEnumerator SchedulerLoop()
        {
            while (_isRunning)
            {
                while (!_fsm.IsInState(CharacterStateId.Idle) || _isPerformingAutonomousAction)
                    yield return null;

                float delay = _config.idleDelayBeforeAutonomous
                    + Random.Range(-_config.idleDelayVariance, _config.idleDelayVariance);
                delay = Mathf.Max(3f, delay);

                float waited = 0f;
                while (waited < delay)
                {
                    if (!_fsm.IsInState(CharacterStateId.Idle))
                        break;
                    waited += Time.deltaTime;
                    yield return null;
                }

                if (!_fsm.IsInState(CharacterStateId.Idle))
                    continue;

                // Tier 1: Try LLM decision, fallback to weighted random
                if (_decisionConnector != null && _decisionConfig != null && _decisionConfig.useLLM)
                {
                    yield return _runner.StartCoroutine(PickActionViaLLM());
                }
                else
                {
                    var action = PickRandomAction();
                    if (action != null)
                        _autonomousCoroutine = _runner.StartCoroutine(ExecuteAutonomousAction(action));
                }
            }
        }

        /// <summary>
        /// Query LLM for action decision. Falls back to weighted random on failure.
        /// </summary>
        private IEnumerator PickActionViaLLM()
        {
            DecisionResult result = null;
            yield return _runner.StartCoroutine(
                _decisionConnector.QueryDecision(_recentActions, r => result = r));

            AutonomousAction action;

            if (result == null)
            {
                // HTTP failure or parse error → fallback
                Debug.Log("[IdleScheduler] LLM failed, falling back to weighted random");
                action = PickRandomAction();
            }
            else if (result.Confidence < _decisionConfig.minConfidence)
            {
                // Low confidence → fallback
                Debug.Log($"[IdleScheduler] LLM confidence too low ({result.Confidence:F2}), falling back to weighted random");
                action = PickRandomAction();
            }
            else
            {
                // LLM decision accepted
                action = CreateActionFromDecision(result);
                if (action == null)
                {
                    Debug.LogWarning($"[IdleScheduler] Could not create action from LLM decision: {result.ActionName}");
                    action = PickRandomAction();
                }
            }

            if (action != null && _fsm.IsInState(CharacterStateId.Idle))
                _autonomousCoroutine = _runner.StartCoroutine(ExecuteAutonomousAction(action));
        }

        /// <summary>
        /// Convert LLM DecisionResult into an AutonomousAction with appropriate payload.
        /// </summary>
        private AutonomousAction CreateActionFromDecision(DecisionResult decision)
        {
            switch (decision.Action)
            {
                case ActionId.Character_MoveToLocation:
                    // Try to find target by name, otherwise wander randomly
                    Vector3 dest;
                    if (!string.IsNullOrEmpty(decision.Target) && decision.Target != "null")
                    {
                        var targetObj = InteractionSpotFinder.FindByNameContains(decision.Target);
                        dest = targetObj != null ? targetObj.position : GetRandomWanderPosition();
                    }
                    else
                    {
                        dest = GetRandomWanderPosition();
                    }
                    return new AutonomousAction
                    {
                        ActionId = ActionId.Character_MoveToLocation,
                        Payload = new MoveToLocationPayload { Destination = dest },
                        ExpectedDuration = _config.wanderDuration,
                        Description = $"LLM: move ({decision.Thought})"
                    };

                case ActionId.Character_TurnTo:
                    Vector3 lookPos = _characterTransform.position
                        + Quaternion.Euler(0, Random.Range(-180f, 180f), 0) * Vector3.forward * 5f;
                    return new AutonomousAction
                    {
                        ActionId = ActionId.Character_TurnTo,
                        Payload = new GazePayload { Position = lookPos },
                        ExpectedDuration = 3f,
                        Description = $"LLM: turn ({decision.Thought})"
                    };

                case ActionId.Character_SitAtChair:
                    int chair = Random.Range(1, 5);
                    return new AutonomousAction
                    {
                        ActionId = ActionId.Character_SitAtChair,
                        Payload = new SitAtChairPayload { ChairNumber = chair },
                        ExpectedDuration = Random.Range(_config.sitDurationMin, _config.sitDurationMax),
                        Description = $"LLM: sit ({decision.Thought})"
                    };

                case ActionId.Character_StandUp:
                    return new AutonomousAction
                    {
                        ActionId = ActionId.Character_StandUp,
                        Payload = null,
                        ExpectedDuration = 3f,
                        Description = $"LLM: stand up ({decision.Thought})"
                    };

                case ActionId.Character_Idle:
                    return new AutonomousAction
                    {
                        ActionId = ActionId.Character_Idle,
                        Payload = null,
                        ExpectedDuration = 5f,
                        Description = $"LLM: idle ({decision.Thought})"
                    };

                case ActionId.Character_LookAt:
                    return new AutonomousAction
                    {
                        ActionId = ActionId.Character_LookAt,
                        Payload = new GazePayload { Target = decision.Target },
                        ExpectedDuration = 5f,
                        Description = $"LLM: look at ({decision.Thought})"
                    };

                case ActionId.Character_Lean:
                    return new AutonomousAction
                    {
                        ActionId = ActionId.Character_Lean,
                        Payload = null,
                        ExpectedDuration = Random.Range(10f, 20f),
                        Description = $"LLM: lean ({decision.Thought})"
                    };

                case ActionId.Character_ExamineMenu:
                    return new AutonomousAction
                    {
                        ActionId = ActionId.Character_ExamineMenu,
                        Payload = null,
                        ExpectedDuration = Random.Range(8f, 15f),
                        Description = $"LLM: examine menu ({decision.Thought})"
                    };

                case ActionId.Character_PlayArcade:
                    return new AutonomousAction
                    {
                        ActionId = ActionId.Character_PlayArcade,
                        Payload = null,
                        ExpectedDuration = Random.Range(15f, 30f),
                        Description = $"LLM: play arcade ({decision.Thought})"
                    };

                case ActionId.Character_PlayClaw:
                    return new AutonomousAction
                    {
                        ActionId = ActionId.Character_PlayClaw,
                        Payload = null,
                        ExpectedDuration = Random.Range(15f, 30f),
                        Description = $"LLM: play claw ({decision.Thought})"
                    };

                case ActionId.Social_Wave:
                    return new AutonomousAction
                    {
                        ActionId = ActionId.Social_Wave,
                        Payload = null,
                        ExpectedDuration = 3f,
                        Description = $"LLM: wave ({decision.Thought})"
                    };

                default:
                    return null;
            }
        }

        private Vector3 GetRandomWanderPosition()
        {
            Vector3 randomDir = Random.insideUnitSphere * _config.wanderRadius;
            randomDir.y = 0;
            Vector3 dest = _characterTransform.position + randomDir;
            if (NavMesh.SamplePosition(dest, out NavMeshHit hit, _config.wanderRadius, NavMesh.AllAreas))
                dest = hit.position;
            return dest;
        }

        private void TrackRecentAction(string actionDescription)
        {
            _recentActions.Add(actionDescription);
            if (_recentActions.Count > MaxRecentActions)
                _recentActions.RemoveAt(0);
        }

        // ── Weighted Random Fallback ──────────────────────────────────

        private AutonomousAction PickRandomAction()
        {
            float total = _config.wanderWeight + _config.lookAroundWeight
                + _config.sitWeight + _config.gestureWeight + _config.playGameWeight;
            if (total <= 0f) return CreateWanderAction();

            float roll = Random.value * total;
            float cumulative = 0f;

            cumulative += _config.wanderWeight;
            if (roll < cumulative) return CreateWanderAction();

            cumulative += _config.lookAroundWeight;
            if (roll < cumulative) return CreateLookAroundAction();

            cumulative += _config.sitWeight;
            if (roll < cumulative) return CreateSitAction();

            cumulative += _config.gestureWeight;
            if (roll < cumulative) return CreateGestureAction();

            cumulative += _config.playGameWeight;
            if (roll < cumulative) return CreatePlayGameAction();

            return CreateWanderAction();
        }

        private AutonomousAction CreateWanderAction()
        {
            return new AutonomousAction
            {
                ActionId = ActionId.Character_MoveToLocation,
                Payload = new MoveToLocationPayload { Destination = GetRandomWanderPosition() },
                ExpectedDuration = _config.wanderDuration,
                Description = "autonomous wander"
            };
        }

        private AutonomousAction CreateLookAroundAction()
        {
            return new AutonomousAction
            {
                ActionId = ActionId.Character_TurnTo,
                Payload = new GazePayload
                {
                    Position = _characterTransform.position
                        + Quaternion.Euler(0, Random.Range(-180f, 180f), 0) * Vector3.forward * 5f
                },
                ExpectedDuration = 3f,
                Description = "autonomous look around"
            };
        }

        private AutonomousAction CreateSitAction()
        {
            return new AutonomousAction
            {
                ActionId = ActionId.Character_SitAtChair,
                Payload = new SitAtChairPayload { ChairNumber = Random.Range(1, 5) },
                ExpectedDuration = Random.Range(_config.sitDurationMin, _config.sitDurationMax),
                Description = "autonomous sit"
            };
        }

        private AutonomousAction CreateGestureAction()
        {
            ActionId[] gestures = { ActionId.Social_Wave, ActionId.Social_Nod, ActionId.Social_Greet };
            var chosen = gestures[Random.Range(0, gestures.Length)];
            return new AutonomousAction
            {
                ActionId = chosen,
                Payload = null,
                ExpectedDuration = 3f,
                Description = $"autonomous gesture ({chosen})"
            };
        }

        private AutonomousAction CreatePlayGameAction()
        {
            bool playArcade = Random.value > 0.5f;
            return new AutonomousAction
            {
                ActionId = playArcade ? ActionId.Character_PlayArcade : ActionId.Character_PlayClaw,
                Payload = null,
                ExpectedDuration = Random.Range(15f, 30f),
                Description = playArcade ? "autonomous play arcade" : "autonomous play claw"
            };
        }

        // ── Action Execution ──────────────────────────────────────────

        private IEnumerator ExecuteAutonomousAction(AutonomousAction action)
        {
            _isPerformingAutonomousAction = true;
            _publishingAutonomous = true;
            Debug.Log($"[IdleScheduler] Starting: {action.Description}");

            // Track for LLM context
            TrackRecentAction(action.Description);

            if (action.Payload != null)
                Managers.PublishAction(action.ActionId, action.Payload);
            else
                Managers.PublishAction(action.ActionId);

            float elapsed = 0f;
            while (elapsed < action.ExpectedDuration && _isPerformingAutonomousAction)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_isPerformingAutonomousAction && _fsm.IsInState(CharacterStateId.Sitting))
            {
                Managers.PublishAction(ActionId.Character_StandUp);
            }

            _publishingAutonomous = false;
            _isPerformingAutonomousAction = false;
            _autonomousCoroutine = null;
        }
    }
}
