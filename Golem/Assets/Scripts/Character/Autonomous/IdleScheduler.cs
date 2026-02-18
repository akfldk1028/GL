using System.Collections;
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

        private Coroutine _schedulerCoroutine;
        private Coroutine _autonomousCoroutine;
        private bool _isRunning;
        private bool _isPerformingAutonomousAction;

        public bool IsPerformingAutonomousAction => _isPerformingAutonomousAction;

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

                var action = PickRandomAction();
                if (action != null)
                    _autonomousCoroutine = _runner.StartCoroutine(ExecuteAutonomousAction(action));
            }
        }

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
            if (roll < cumulative) return CreateWanderAction(); // placeholder for game

            return CreateWanderAction();
        }

        private AutonomousAction CreateWanderAction()
        {
            Vector3 randomDir = Random.insideUnitSphere * _config.wanderRadius;
            randomDir.y = 0;
            Vector3 dest = _characterTransform.position + randomDir;

            if (NavMesh.SamplePosition(dest, out NavMeshHit hit, _config.wanderRadius, NavMesh.AllAreas))
                dest = hit.position;

            return new AutonomousAction
            {
                ActionId = ActionId.Character_MoveToLocation,
                Payload = new MoveToLocationPayload { Destination = dest },
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
            return new AutonomousAction
            {
                ActionId = ActionId.Character_Idle,
                Payload = new IdlePayload { IdleType = "standing" },
                ExpectedDuration = 5f,
                Description = "autonomous gesture"
            };
        }

        private IEnumerator ExecuteAutonomousAction(AutonomousAction action)
        {
            _isPerformingAutonomousAction = true;
            Debug.Log($"[IdleScheduler] Starting: {action.Description}");

            Managers.PublishAction(action.ActionId, action.Payload);

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

            _isPerformingAutonomousAction = false;
            _autonomousCoroutine = null;
        }
    }
}
