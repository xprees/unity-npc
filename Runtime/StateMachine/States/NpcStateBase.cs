using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AI;

namespace Xprees.Npc.StateMachine.States
{
    public abstract class NpcStateBase
    {
        protected readonly NavMeshAgent agent;
        protected readonly NpcController controller;

        public bool CanBeInterrupted { get; set; } = true;
        public bool WaitUntilCanBeInterrupted { get; set; } = true;

        protected NpcStateBase(NpcController controller)
        {
            this.controller = controller;
            agent = controller.agent;
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

            await ExitState(cancellationToken);
            controller.CurrentState = newState;
            await newState.EnterState(cancellationToken);
        }

        public void ForceExitState() => CanBeInterrupted = true;
    }
}