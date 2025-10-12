using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Xprees.Npc.Extensions;

namespace Xprees.Npc.StateMachine.States
{
    public class MoveToState : NpcStateBase
    {
        private readonly Vector3 _destination;
        private readonly Action _onDestinationReached;

        /// Seconds to wait after arriving at destination
        public float WaitTimeAfterArrive { get; set; } = 15;

        public MoveToState(NpcController controller, Vector3 destination, Action onDestinationReached = null) : base(controller)
        {
            _destination = destination;
            _onDestinationReached = onDestinationReached;
        }

        public override async UniTask EnterState(CancellationToken cancellationToken = default)
        {
            if (!CanBeInterrupted) controller.IsDisabledInteraction = true;
            try
            {
                agent.Pause();
                var success = agent.SetDestination(_destination);

                if (!success) return;

                await UniTask.WaitUntilValueChanged(agent, a => a.hasPath, cancellationToken: cancellationToken);

                agent.UnPause();
                await UniTask.WaitUntil(agent.HasArrived, cancellationToken: cancellationToken);
                AfterAgentArrived();

                if (WaitTimeAfterArrive <= 0) return; // Stay in this state until new state is set

                await UniTask.Delay(TimeSpan.FromSeconds(WaitTimeAfterArrive), cancellationToken: cancellationToken);
                await SwitchBackToIdleState();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void AfterAgentArrived()
        {
            agent.Pause();
            CanBeInterrupted = true; // Allow other states to interrupt this state
            controller.IsDisabledInteraction = false;
            _onDestinationReached?.Invoke();
        }

        private async UniTask SwitchBackToIdleState() =>
            await controller.SwitchState(new IdleState(controller, controller.CurrentWaypointIndex));

        public override UniTask UpdateState(CancellationToken cancellationToken = default) => UniTask.CompletedTask;

        public override UniTask ExitState(CancellationToken cancellationToken = default)
        {
            agent.UnPause();
            return UniTask.CompletedTask;
        }

        public override UniTask CheckSwitchState(CancellationToken cancellationToken = default) => UniTask.CompletedTask;
    }
}