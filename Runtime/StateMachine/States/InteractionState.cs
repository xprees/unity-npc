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

        /// Timeout after which the NPC will go back to idle state if no interaction is detected
        public float InteractionTimeout { get; set; } = 5;

        public InteractionState(NpcController controller) : base(controller)
        {
        }

        public override async UniTask EnterState(CancellationToken cancellationToken = default)
        {
            agent.Pause();
            LookAtPlayer();
            await WaitForInteractionOrGoBackToIdle(cancellationToken);
        }

        private async UniTask WaitForInteractionOrGoBackToIdle(CancellationToken cancellationToken = default)
        {
            try
            {
                await UniTask.WaitUntil(
                    predicate: InteractedWithPlayer,
                    cancellationToken: _timeoutController.Timeout(TimeSpan.FromSeconds(InteractionTimeout))
                ).AttachExternalCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (_timeoutController.IsTimeout() && !InteractedWithPlayer())
                {
                    await controller.SwitchState(new IdleState(controller, controller.CurrentWaypointIndex), cancellationToken);
                }
            }
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
            _timeoutController.Dispose();
            agent.UnPause();
            return UniTask.CompletedTask;
        }

        public override UniTask CheckSwitchState(CancellationToken cancellationToken = default) => UniTask.CompletedTask;
    }
}