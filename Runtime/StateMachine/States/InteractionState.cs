using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extensions.LeanTween;
using Xprees.Npc.Extensions;

namespace Xprees.Npc.StateMachine.States
{
    public class InteractionState : NpcStateBase
    {
        private readonly TimeoutController _timeoutController = new();

        private CancellationToken _stateToken;
        private CancellationToken _controllerToken;
        private bool _dialogActive;

        /// Timeout after which the NPC will go back to idle state if no interaction is detected
        public float InteractionTimeout { get; set; } = 5;

        /// Seconds the NPC keeps facing the player after the dialog ended, before it goes back to idle
        public float PostDialogDelay { get; set; } = 2.5f;

        public InteractionState(NpcController controller) : base(controller)
        {
        }

        public override async UniTask EnterState(CancellationToken cancellationToken = default)
        {
            _controllerToken = cancellationToken;
            _stateToken = BeginState(cancellationToken);

            agent.Pause();
            LookAtPlayer();

            if (controller.IsInDialog)
            {
                OnDialogStarted();
                return;
            }

            await WaitForInteractionOrGoBackToIdle();
        }

        private async UniTask WaitForInteractionOrGoBackToIdle()
        {
            try
            {
                await UniTask.WaitUntil(
                    predicate: () => InteractedWithPlayer() || _dialogActive,
                    cancellationToken: _timeoutController.Timeout(TimeSpan.FromSeconds(InteractionTimeout))
                ).AttachExternalCancellation(_stateToken);
            }
            catch (OperationCanceledException)
            {
                var timedOut = _timeoutController.IsTimeout() && !_stateToken.IsCancellationRequested;
                if (timedOut && IsCurrentState && !InteractedWithPlayer() && !_dialogActive)
                {
                    await controller.SwitchState(new IdleState(controller, controller.CurrentWaypointIndex), _controllerToken);
                }
            }
        }

        /// The dialog with this NPC opened: it can't be interrupted (scenario moves wait) and doesn't time out or end by distance.
        public void OnDialogStarted()
        {
            _dialogActive = true;
            CanBeInterrupted = false;
            WaitUntilCanBeInterrupted = true;
        }

        /// The dialog with this NPC closed: face the player a moment longer, then go back to idle.
        public void OnDialogStopped()
        {
            _dialogActive = false;
            CanBeInterrupted = true;
            ResumeAfterDelay().Forget();
        }

        private async UniTask ResumeAfterDelay()
        {
            var cancelled = await UniTask.Delay(TimeSpan.FromSeconds(PostDialogDelay), cancellationToken: _stateToken)
                .SuppressCancellationThrow();
            if (cancelled || !IsCurrentState || _dialogActive) return;

            await controller.SwitchState(new IdleState(controller, controller.CurrentWaypointIndex), _controllerToken)
                .SuppressCancellationThrow();
        }

        private bool InteractedWithPlayer() => controller.InteractedWithPlayer;

        private void LookAtPlayer()
        {
            var playerAnchor = controller.playerTransformAnchor;
            if (playerAnchor == null || !playerAnchor.isSet) return;

            var playerPosition = playerAnchor.Value.position;
            controller.transform
                .LeanLookAt(playerPosition, .7f)
                .setEaseInOutSine();
        }

        public override UniTask UpdateState(CancellationToken cancellationToken = default) => UniTask.CompletedTask;

        public override UniTask ExitState(CancellationToken cancellationToken = default)
        {
            EndState();
            _timeoutController.Dispose();
            agent.UnPause();
            return UniTask.CompletedTask;
        }

        public override UniTask CheckSwitchState(CancellationToken cancellationToken = default) => UniTask.CompletedTask;
    }
}