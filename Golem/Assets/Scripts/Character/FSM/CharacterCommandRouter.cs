using System;
using System.Collections;
using System.Collections.Generic;
using Golem.Character.Autonomous;
using Golem.Character.Modules;
using Golem.Character.Modules.Impl;
using Golem.Infrastructure.Messages;
using UnityEngine;

namespace Golem.Character.FSM
{
    public class CharacterCommandRouter : IDisposable
    {
        private readonly CharacterBehaviorFSM _fsm;
        private readonly CharacterStateContext _stateContext;
        private readonly PointClickController _pointClick;
        private readonly BehaviorModuleRegistry _modules;
        private readonly MonoBehaviour _coroutineRunner;
        private readonly List<IDisposable> _subscriptions = new();
        private IdleScheduler _idleScheduler;
        private Coroutine _moveCompletionCoroutine;
        private Coroutine _delayedMoveCoroutine;

        public CharacterCommandRouter(
            CharacterBehaviorFSM fsm,
            CharacterStateContext stateContext,
            PointClickController pointClick,
            BehaviorModuleRegistry modules,
            MonoBehaviour coroutineRunner)
        {
            _fsm = fsm;
            _stateContext = stateContext;
            _pointClick = pointClick;
            _modules = modules;
            _coroutineRunner = coroutineRunner;

            _subscriptions.Add(Managers.Subscribe(ActionId.Character_MoveToLocation, OnMoveToLocation));
            _subscriptions.Add(Managers.Subscribe(ActionId.Character_WalkTo, OnMoveToLocation));
            _subscriptions.Add(Managers.Subscribe(ActionId.Character_RunTo, OnMoveToLocation));
            _subscriptions.Add(Managers.Subscribe(ActionId.Character_Stop, OnStop));
            _subscriptions.Add(Managers.Subscribe(ActionId.Character_TurnTo, OnTurnTo));
            _subscriptions.Add(Managers.Subscribe(ActionId.Character_SitAtChair, OnSitAtChair));
            _subscriptions.Add(Managers.Subscribe(ActionId.Character_StandUp, OnStandUp));
            _subscriptions.Add(Managers.Subscribe(ActionId.Character_Idle, OnIdle));
            _subscriptions.Add(Managers.Subscribe(ActionId.Character_Lean, OnLean));
            _subscriptions.Add(Managers.Subscribe(ActionId.Character_ExamineMenu, OnExamineMenu));
            _subscriptions.Add(Managers.Subscribe(ActionId.Character_LookAt, OnLookAt));
            _subscriptions.Add(Managers.Subscribe(ActionId.Character_PlayArcade, OnPlayArcade));
            _subscriptions.Add(Managers.Subscribe(ActionId.Character_PlayClaw, OnPlayClaw));
        }

        public void SetIdleScheduler(IdleScheduler scheduler) => _idleScheduler = scheduler;

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();
        }

        /// <summary>Cancel any running autonomous action if this command is NOT from IdleScheduler itself.</summary>
        private void InterruptAutonomous()
        {
            if (_idleScheduler != null && !_idleScheduler.IsPublishingAutonomous)
                _idleScheduler.CancelCurrentAction();
        }

        private void PublishCompleted(ActionId source, string name)
        {
            Managers.PublishAction(ActionId.Agent_ActionCompleted, new ActionLifecyclePayload
            {
                SourceAction = source,
                ActionName = name,
                Success = true
            });
        }

        private void PublishFailed(ActionId source, string name, string error)
        {
            Debug.LogWarning($"[CommandRouter] Action failed: {name} — {error}");
            Managers.PublishAction(ActionId.Agent_ActionFailed, new ActionLifecyclePayload
            {
                SourceAction = source,
                ActionName = name,
                Success = false,
                Error = error
            });
        }

        private void StartMoveWatch(ActionId source, string name)
        {
            if (_moveCompletionCoroutine != null)
                _coroutineRunner.StopCoroutine(_moveCompletionCoroutine);
            _moveCompletionCoroutine = _coroutineRunner.StartCoroutine(WaitForMove(source, name));
        }

        private IEnumerator WaitForMove(ActionId source, string name)
        {
            yield return null;
            float elapsed = 0f;
            var nav = _stateContext.NavAgent;
            bool arrived = false;
            while (elapsed < 60f)
            {
                if (nav == null || !nav.enabled) break;
                if (!nav.pathPending)
                {
                    // Check for invalid path
                    if (nav.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
                    {
                        _moveCompletionCoroutine = null;
                        PublishFailed(source, name, "NavMesh path invalid — destination unreachable");
                        yield break;
                    }
                    if (nav.remainingDistance <= nav.stoppingDistance + 0.1f)
                    {
                        arrived = true;
                        break;
                    }
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            _moveCompletionCoroutine = null;
            if (arrived)
                PublishCompleted(source, name);
            else
                PublishFailed(source, name, "Move timed out after 60s");
        }

        private void SetupInteraction(Transform chosen, CharacterStateId targetState, bool disableCollider = false)
        {
            var interaction = chosen.Find("InteractionSpot");
            _stateContext.InteractionSpot = interaction != null ? interaction : chosen;

            if (disableCollider)
            {
                _stateContext.RestoreDisabledCollider();
                _stateContext.DisabledCollider = chosen.GetComponent<Collider>();
                if (_stateContext.DisabledCollider != null)
                    _stateContext.DisabledCollider.enabled = false;
            }

            _stateContext.PendingInteractionState = targetState;
            _pointClick.MoveToPoint(_stateContext.InteractionSpot.position);
        }

        // ── Handlers ──

        private void OnMoveToLocation(ActionMessage msg)
        {
            InterruptAutonomous();
            if (_pointClick == null) return;
            if (msg.TryGetPayload<MoveToLocationPayload>(out var p))
            {
                Vector3 dest = p.Destination;
                if (dest == Vector3.zero)
                {
                    var t = InteractionSpotFinder.FindByNameContains(p.Location);
                    dest = t != null ? t.position : _stateContext.CharacterTransform.position;
                }

                var thinkTime = _modules?.Get<ThinkTimeModule>();
                if (thinkTime != null && _fsm.IsInState(CharacterStateId.Idle))
                {
                    float pause = thinkTime.GetPauseDuration(CharacterStateId.Idle, CharacterStateId.Walking);
                    if (pause > 0f)
                    {
                        CancelDelayedMove();
                        _delayedMoveCoroutine = _coroutineRunner.StartCoroutine(DelayedMove(dest, pause, msg.Id));
                        return;
                    }
                }

                _pointClick.MoveToPoint(dest);
                _fsm.ForceTransition(CharacterStateId.Walking);
                StartMoveWatch(msg.Id, "moveToLocation");
            }
        }

        private IEnumerator DelayedMove(Vector3 dest, float delay, ActionId source)
        {
            yield return new WaitForSeconds(delay);
            _delayedMoveCoroutine = null;
            _pointClick.MoveToPoint(dest);
            _fsm.ForceTransition(CharacterStateId.Walking);
            StartMoveWatch(source, "moveToLocation");
        }

        private void CancelDelayedMove()
        {
            if (_delayedMoveCoroutine != null)
            {
                _coroutineRunner.StopCoroutine(_delayedMoveCoroutine);
                _delayedMoveCoroutine = null;
            }
        }

        private void OnStop(ActionMessage msg)
        {
            InterruptAutonomous();
            CancelDelayedMove();
            if (_moveCompletionCoroutine != null)
            {
                _coroutineRunner.StopCoroutine(_moveCompletionCoroutine);
                _moveCompletionCoroutine = null;
            }
            _pointClick.StopMovement();
            _fsm.ForceTransition(CharacterStateId.Idle);
            PublishCompleted(ActionId.Character_Stop, "stop");
        }

        private void OnTurnTo(ActionMessage msg)
        {
            InterruptAutonomous();
            if (msg.TryGetPayload<GazePayload>(out var p))
            {
                Vector3 target = p.Position;
                if (target == Vector3.zero && !string.IsNullOrEmpty(p.Target))
                {
                    var t = InteractionSpotFinder.FindByNameContains(p.Target);
                    if (t != null) target = t.position;
                }
                if (target != Vector3.zero)
                {
                    Vector3 dir = target - _stateContext.CharacterTransform.position;
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.01f)
                        _stateContext.CharacterTransform.rotation = Quaternion.LookRotation(dir);
                }
            }
            PublishCompleted(ActionId.Character_TurnTo, "turnTo");
        }

        private void OnSitAtChair(ActionMessage msg)
        {
            InterruptAutonomous();
            if (_pointClick == null) return;
            if (msg.TryGetPayload<SitAtChairPayload>(out var p))
            {
                var chair = InteractionSpotFinder.FindChair(p.ChairNumber);
                if (chair != null)
                {
                    SetupInteraction(chair, CharacterStateId.Sitting, true);
                    _fsm.ForceTransition(CharacterStateId.SitTransition);
                    // Completion handled by CompletionTracker: SitTransition→Sitting
                }
                else
                {
                    PublishFailed(ActionId.Character_SitAtChair, "sitAtChair", $"Chair #{p.ChairNumber} not found in scene");
                }
            }
        }

        private void OnStandUp(ActionMessage msg)
        {
            InterruptAutonomous();
            if (_fsm.IsInState(CharacterStateId.Sitting))
            {
                // StandTransition is async (animation) — CompletionTracker publishes when done
                _fsm.ForceTransition(CharacterStateId.StandTransition);
            }
            else if (_fsm.IsInAnyState(CharacterStateId.Looking, CharacterStateId.Leaning,
                CharacterStateId.PlayingArcade, CharacterStateId.PlayingClaw))
            {
                _fsm.ForceTransition(CharacterStateId.Idle);
                PublishCompleted(ActionId.Character_StandUp, "standUp");
            }
        }

        private void OnIdle(ActionMessage msg)
        {
            InterruptAutonomous();
            if (msg.TryGetPayload<IdlePayload>(out var p))
            {
                string type = p.IdleType ?? "standing";
                if (type == "standing")
                {
                    _fsm.ForceTransition(CharacterStateId.Idle);
                    PublishCompleted(ActionId.Character_Idle, "idle");
                }
                else if (type == "sitting")
                    OnSitAtChair(ActionMessage.From(ActionId.Character_SitAtChair, new SitAtChairPayload { ChairNumber = 1 }));
                else if (type == "leaning")
                    OnLean(msg);
            }
        }

        private void OnLean(ActionMessage msg)
        {
            InterruptAutonomous();
            var chair = InteractionSpotFinder.FindSlotMachineChair();
            if (chair != null)
            {
                SetupInteraction(chair, CharacterStateId.Leaning, true);
                _fsm.ForceTransition(CharacterStateId.Arriving);
                // Completion handled by CompletionTracker: Arriving→Leaning
            }
            else
            {
                PublishFailed(ActionId.Character_Lean, "lean", "Slot machine chair not found in scene");
            }
        }

        private void OnExamineMenu(ActionMessage msg)
        {
            InterruptAutonomous();
            var ad = InteractionSpotFinder.FindAdDisplay();
            if (ad != null)
            {
                SetupInteraction(ad, CharacterStateId.Looking);
                _fsm.ForceTransition(CharacterStateId.Arriving);
                // Completion handled by CompletionTracker: Arriving→Looking
            }
            else
            {
                PublishFailed(ActionId.Character_ExamineMenu, "examineMenu", "Ad display not found in scene");
            }
        }

        private void OnLookAt(ActionMessage msg)
        {
            InterruptAutonomous();
            if (msg.TryGetPayload<GazePayload>(out var p))
            {
                Vector3 target = p.Position;
                if (target == Vector3.zero && !string.IsNullOrEmpty(p.Target))
                {
                    var t = InteractionSpotFinder.FindByNameContains(p.Target);
                    if (t != null) target = t.position;
                }
                if (target != Vector3.zero)
                    _stateContext.CharacterTransform.LookAt(target);
            }
            PublishCompleted(ActionId.Character_LookAt, "lookAt");
        }

        private void OnPlayArcade(ActionMessage msg)
        {
            InterruptAutonomous();
            var arcade = InteractionSpotFinder.FindArcade();
            if (arcade != null)
            {
                SetupInteraction(arcade, CharacterStateId.PlayingArcade, true);
                _fsm.ForceTransition(CharacterStateId.Arriving);
                // Completion handled by CompletionTracker: Arriving→PlayingArcade
            }
            else
            {
                PublishFailed(ActionId.Character_PlayArcade, "playArcade", "Arcade machine not found in scene");
            }
        }

        private void OnPlayClaw(ActionMessage msg)
        {
            InterruptAutonomous();
            var claw = InteractionSpotFinder.FindClawMachine();
            if (claw != null)
            {
                SetupInteraction(claw, CharacterStateId.PlayingClaw, true);
                _fsm.ForceTransition(CharacterStateId.Arriving);
                // Completion handled by CompletionTracker: Arriving→PlayingClaw
            }
            else
            {
                PublishFailed(ActionId.Character_PlayClaw, "playClaw", "Claw machine not found in scene");
            }
        }
    }
}
