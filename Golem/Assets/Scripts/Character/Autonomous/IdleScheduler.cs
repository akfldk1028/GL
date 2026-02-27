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

        // Tier 2: Memory system (null = Tier 1 only)
        private MemoryStore _memoryStore;
        private ActionOutcomeTracker _outcomeTracker;
        private ReflectionEngine _reflectionEngine;
        private MemoryConfigSO _memoryConfig;

        private Coroutine _schedulerCoroutine;
        private Coroutine _autonomousCoroutine;
        private bool _isRunning;
        private bool _isPerformingAutonomousAction;
        private bool _publishingAutonomous;

        // Recent action tracking for LLM context (last 5)
        private readonly List<string> _recentActions = new List<string>();
        private const int MaxRecentActions = 5;

        // Tier 2: current context for outcome tracking
        private string _currentContextHash;
        private DecisionResult _currentDecision;
        private bool _retryPending;

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

        /// <summary>
        /// Initialize the Tier 2 memory system. If not called, operates as Tier 1 only.
        /// </summary>
        public void SetMemoryStore(MemoryConfigSO memoryConfig)
        {
            if (memoryConfig == null) return;
            _memoryConfig = memoryConfig;

            string charName = _decisionConfig != null ? _decisionConfig.characterName : "Golem";
            _memoryStore = new MemoryStore(memoryConfig, charName);
            _memoryStore.Load();

            _outcomeTracker = new ActionOutcomeTracker(_memoryStore, memoryConfig);
            _outcomeTracker.OnOutcomeRecorded += OnOutcomeRecorded;

            if (_decisionConnector != null)
            {
                _reflectionEngine = new ReflectionEngine(memoryConfig, _memoryStore, _decisionConnector, _decisionConfig, _runner);
            }

            Debug.Log($"[IdleScheduler] Tier 2 memory enabled (episodes={_memoryStore.Episodic.Episodes.Count}, skills={_memoryStore.Skills.Skills.Count})");
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
            _outcomeTracker?.Dispose();
            _memoryStore?.Dispose();
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

                // Tier 1+2: Try LLM decision (with memory), fallback to weighted random
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
        /// Query LLM for action decision with Tier 2 memory integration.
        /// Falls back to weighted random on failure.
        /// </summary>
        private IEnumerator PickActionViaLLM(string failureContext = null)
        {
            // Step 1: Build context hash
            string contextHash = BuildCurrentContextHash();
            _currentContextHash = contextHash;

            // Step 2: Check skill library for cached action
            if (_memoryStore != null && failureContext == null)
            {
                var skill = _memoryStore.Skills.Match(contextHash);
                if (skill != null && _memoryStore.Skills.ShouldUseSkill(skill))
                {
                    Debug.Log($"[IdleScheduler] Using cached skill: {skill.actionName} (rate={skill.SuccessRate:F2}, uses={skill.useCount})");
                    var skillAction = CreateActionFromSkill(skill);
                    if (skillAction != null && _fsm.IsInState(CharacterStateId.Idle))
                    {
                        _currentDecision = new DecisionResult
                        {
                            Action = (ActionId)skill.recommendedActionId,
                            ActionName = skill.actionName,
                            Target = skill.target,
                            Thought = "cached skill",
                            Confidence = skill.SuccessRate,
                            Reasoning = $"Skill cache: {skill.situationPattern}"
                        };
                        BeginTrackingAndExecute(skillAction);
                        yield break;
                    }
                }
            }

            // Step 3: Retrieve episodic memories for LLM context
            List<EpisodeEntry> memories = null;
            if (_memoryStore != null)
            {
                memories = _memoryStore.Episodic.RetrieveTopK(contextHash);
                if (memories.Count > 0)
                    Debug.Log($"[IdleScheduler] Retrieved {memories.Count} memories for LLM context");
            }

            // Step 4: Query LLM with memories
            DecisionResult result = null;
            yield return _runner.StartCoroutine(
                _decisionConnector.QueryDecision(_recentActions, r => result = r, memories, failureContext));

            AutonomousAction action;

            if (result == null)
            {
                Debug.Log("[IdleScheduler] LLM failed, falling back to weighted random");
                action = PickRandomAction();
                _currentDecision = null;
            }
            else if (result.Confidence < _decisionConfig.minConfidence)
            {
                Debug.Log($"[IdleScheduler] LLM confidence too low ({result.Confidence:F2}), falling back to weighted random");
                action = PickRandomAction();
                _currentDecision = null;
            }
            else
            {
                action = CreateActionFromDecision(result);
                if (action == null)
                {
                    Debug.LogWarning($"[IdleScheduler] Could not create action from LLM decision: {result.ActionName}");
                    action = PickRandomAction();
                    _currentDecision = null;
                }
                else
                {
                    _currentDecision = result;
                }
            }

            if (action != null && _fsm.IsInState(CharacterStateId.Idle))
                BeginTrackingAndExecute(action);
        }

        private void BeginTrackingAndExecute(AutonomousAction action)
        {
            // Begin outcome tracking (Tier 2)
            if (_outcomeTracker != null && _currentContextHash != null)
            {
                _outcomeTracker.BeginTracking(action, _currentDecision, _currentContextHash, _characterTransform.position);
            }

            _autonomousCoroutine = _runner.StartCoroutine(ExecuteAutonomousAction(action));
        }

        private void OnOutcomeRecorded(bool succeeded)
        {
            // Track for reflection engine
            _reflectionEngine?.TrackAction(_memoryStore?.Episodic.Episodes.Count > 0
                ? _memoryStore.Episodic.Episodes[_memoryStore.Episodic.Episodes.Count - 1].importance
                : 0.2f);

            // ReAct: retry once on failure
            if (!succeeded && _memoryConfig != null && _memoryConfig.enableFailureRetry && !_retryPending)
            {
                string failCtx = _currentDecision != null
                    ? $"Your previous action '{_currentDecision.ActionName}' targeting '{_currentDecision.Target}' FAILED. Reason: {_currentDecision.Reasoning}. Choose a different action."
                    : "Your previous action FAILED. Choose a different action.";
                _retryPending = true;
                _runner.StartCoroutine(RetryAfterFailure(failCtx));
                return;
            }

            // Check reflection
            if (_reflectionEngine != null && _reflectionEngine.ShouldReflect())
            {
                _runner.StartCoroutine(_reflectionEngine.ExecuteReflection());
            }
        }

        private IEnumerator RetryAfterFailure(string failureContext)
        {
            // Wait for current action to finish cleanup
            yield return null;
            _retryPending = false;

            if (!_fsm.IsInState(CharacterStateId.Idle) || _isPerformingAutonomousAction)
                yield break;

            Debug.Log("[IdleScheduler] ReAct: retrying after failure...");
            yield return _runner.StartCoroutine(PickActionViaLLM(failureContext));
        }

        private string BuildCurrentContextHash()
        {
            if (_decisionConnector == null) return "unknown";
            string fsmState = _fsm.CurrentStateId.ToString();
            string[] nearbyTags = _decisionConnector.GetNearbyTags();
            float gameHour = GetGameHour();
            return EpisodicMemory.BuildContextHash(fsmState, nearbyTags, gameHour);
        }

        private float GetGameHour()
        {
            // 24-hour cycle based on Time.time (1 game hour = 60 real seconds by default)
            return (Time.time / 60f) % 24f;
        }

        private AutonomousAction CreateActionFromSkill(SkillEntry skill)
        {
            var fakeDecision = new DecisionResult
            {
                Action = (ActionId)skill.recommendedActionId,
                ActionName = skill.actionName,
                Target = skill.target,
                Thought = "cached skill",
                Confidence = skill.SuccessRate,
                Reasoning = "skill cache"
            };
            return CreateActionFromDecision(fakeDecision);
        }

        /// <summary>
        /// Convert LLM DecisionResult into an AutonomousAction with appropriate payload.
        /// </summary>
        private AutonomousAction CreateActionFromDecision(DecisionResult decision)
        {
            switch (decision.Action)
            {
                case ActionId.Character_MoveToLocation:
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

            bool completed = _isPerformingAutonomousAction;

            if (_isPerformingAutonomousAction && _fsm.IsInState(CharacterStateId.Sitting))
            {
                Managers.PublishAction(ActionId.Character_StandUp);
            }

            _publishingAutonomous = false;
            _isPerformingAutonomousAction = false;
            _autonomousCoroutine = null;

            // Tier 2: Complete tracking if not already handled by ActionMessage
            if (completed)
                _outcomeTracker?.CompleteTracking(true);
        }
    }
}
