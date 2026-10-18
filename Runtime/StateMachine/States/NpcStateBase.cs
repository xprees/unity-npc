using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AI;

namespace Xprees.Npc.StateMachine.States
{
    public abstract class NpcStateBase
    {
        protected readonly NavMeshAgent agent;
        protected readonly NpcController controller;

        private CancellationTokenSource _stateCts;

        public bool CanBeInterrupted { get; set; } = true;
        public bool WaitUntilCanBeInterrupted { get; set; } = true;

        protected NpcStateBase(NpcController controller)
        {
            this.controller = controller;
            agent = controller.agent;
        }

        /// <summary>True while this state is the one the controller runs. Delayed work must check it before touching the agent or switching state.</summary>
        protected bool IsCurrentState => controller.CurrentState == this;

        /// <summary>
        /// Creates the token every wait/delay of this state must use. It is cancelled by <see cref="EndState"/>,
        /// so nothing started by this state can outlive it (the controller token lives as long as the NPC).
        /// </summary>
        protected CancellationToken BeginState(CancellationToken controllerToken)
        {
            _stateCts?.Dispose();
            _stateCts = CancellationTokenSource.CreateLinkedTokenSource(controllerToken);
            return _stateCts.Token;
        }

        /// Call from <see cref="ExitState"/>.
        protected void EndState()
        {
            if (_stateCts == null) return;
            var cts = _stateCts;
            _stateCts = null;
            cts.Cancel();
            cts.Dispose();
        }

        public abstract UniTask EnterState(CancellationToken cancellationToken = default);

        public abstract UniTask UpdateState(CancellationToken cancellationToken = default);

        public abstract UniTask ExitState(CancellationToken cancellationToken = default);

        public abstract UniTask CheckSwitchState(CancellationToken cancellationToken = default);

        ///<summary>
        /// Instead of calling this method it's to preferred to call <see cref="INpcStateMachine.SwitchState"> controller.SwitchState()</see> method.
        /// Controller does some additional checks and calls this method and properties setup.
        /// </summary>
        public async UniTask SwitchState(NpcStateBase newState, CancellationToken cancellationToken = default)
        {
            if (!CanBeInterrupted)
            {
                if (!WaitUntilCanBeInterrupted) return; // ignore switch request
                await UniTask.WaitUntil(() => CanBeInterrupted, cancellationToken: cancellationToken);
            }

            // Another switch won while this one was waiting; this state is already gone.
            if (!IsCurrentState) return;

            await ExitState(cancellationToken);
            controller.CurrentState = newState;
            await newState.EnterState(cancellationToken);
        }

        public void ForceExitState() => CanBeInterrupted = true;
    }
}