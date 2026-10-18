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

        private CancellationToken _controllerToken;

        public override async UniTask EnterState(CancellationToken cancellationToken = default)
        {
            _controllerToken = cancellationToken;
            var token = BeginState(cancellationToken);
            if (!CanBeInterrupted) controller.IsDisabledInteraction = true;
            try
            {
                agent.Pause();
                var success = agent.SetDestination(_destination);

                if (!success)
                {
                    ReleaseInterruption(); // Otherwise the NPC would stay locked in this state forever
                    return;
                }

                await UniTask.WaitUntilValueChanged(agent, a => a.hasPath, cancellationToken: token);

                agent.UnPause();
                await UniTask.WaitUntil(agent.HasArrived, cancellationToken: token);
                AfterAgentArrived();

                if (WaitTimeAfterArrive <= 0) return; // Stay in this state until new state is set

                await UniTask.Delay(TimeSpan.FromSeconds(WaitTimeAfterArrive), cancellationToken: token);
                if (!IsCurrentState) return; // Something else (e.g. a dialog) took over the NPC meanwhile
                await SwitchBackToIdleState();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void AfterAgentArrived()
        {
            agent.Pause();
            ReleaseInterruption();
            _onDestinationReached?.Invoke();
        }

        private void ReleaseInterruption()
        {
            CanBeInterrupted = true; // Allow other states to interrupt this state
            controller.IsDisabledInteraction = false;
        }

        private async UniTask SwitchBackToIdleState() =>
            await controller.SwitchState(new IdleState(controller, controller.CurrentWaypointIndex), _controllerToken);

        public override UniTask UpdateState(CancellationToken cancellationToken = default) => UniTask.CompletedTask;

        public override UniTask ExitState(CancellationToken cancellationToken = default)
        {
            agent.UnPause();
            EndState();
            return UniTask.CompletedTask;
        }

        public override UniTask CheckSwitchState(CancellationToken cancellationToken = default) => UniTask.CompletedTask;
    }
}