using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Xprees.Npc.Extensions;
using Xprees.Npc.Navigation;
using Random = UnityEngine.Random;

namespace Xprees.Npc.StateMachine.States
{
    public class IdleState : NpcStateBase
    {
        private CancellationTokenSource _routingCts;
        private CancellationToken _stateToken;
        private readonly int _lastWaypointIndex;

        public bool DontGoOnRoute { get; set; }

        private bool IsAlreadyLooping => _routingCts is { IsCancellationRequested: false };

        private bool _routeChanged;
        private Route _route;

        public Route Route
        {
            get => _route;
            set
            {
                _route = value;
                _routeChanged = true;
            }
        }

        public IdleState(NpcController controller, int lastWaypointIndex = 0) : base(controller)
        {
            _lastWaypointIndex = lastWaypointIndex;
        }

        public override async UniTask EnterState(CancellationToken cancellationToken = default)
        {
            _stateToken = BeginState(cancellationToken);
            _routeChanged = false; // The route set while constructing is the one we are about to assign
            await AssignAgentRoute(_stateToken);
        }

        public override async UniTask UpdateState(CancellationToken cancellationToken = default)
        {
            if (!_routeChanged) return;

            _routeChanged = false;
            CancelRouting();
            await AssignAgentRoute(_stateToken);
        }

        public override UniTask ExitState(CancellationToken cancellationToken = default)
        {
            agent.UnPause();
            CancelRouting();
            EndState();
            return UniTask.CompletedTask;
        }

        public override UniTask CheckSwitchState(CancellationToken cancellationToken = default) => UniTask.CompletedTask;

        /// Cancels the running route chain (including its start delay). The chain disposes its own source.
        private void CancelRouting()
        {
            var routing = _routingCts;
            _routingCts = null;
            routing?.Cancel();
        }

        private async UniTask AssignAgentRoute(CancellationToken cancellationToken = default)
        {
            if (DontGoOnRoute || Route == null)
            {
                DontGoAnywhere();
                return;
            }

            if (IsAlreadyLooping) return;

            var routing = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _routingCts = routing;
            try
            {
                // Wait for a random amount of time before starting to move -> more natural not all NPCs start moving at the same time
                await UniTask.Delay(TimeSpan.FromSeconds(Random.Range(.1f, 5)), cancellationToken: routing.Token);
                await UniTask.WaitUntil(() => agent.isOnNavMesh, cancellationToken: routing.Token);
                await LoopAgentOnRoute(routing.Token);
            }
            finally
            {
                if (_routingCts == routing) _routingCts = null;
                routing.Dispose();
            }
        }

        private void DontGoAnywhere() => agent.Pause();

        private async UniTask LoopAgentOnRoute(CancellationToken cancellationToken = default)
        {
            if (Route.waypoints.Count == 0) return;

            var waypointIndex = _lastWaypointIndex; // Recalling last waypoint
            while (true)
            {
                for (; waypointIndex < Route.waypoints.Count; waypointIndex++)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    if (_routeChanged) return; // Stop current if route changed - UpdateState will start new one

                    var waypoint = Route.waypoints[waypointIndex];
                    controller.CurrentWaypointIndex = waypointIndex;

                    var destination = waypoint.Position;
                    if (destination == Vector3.positiveInfinity) continue;

                    var success = agent.SetDestination(destination);
                    if (!success) continue;

                    agent.UnPause();

                    await UniTask.WaitUntil(agent.HasArrived, cancellationToken: cancellationToken);

                    agent.Pause();
                    await waypoint.OnArrivedAtWaypoint(cancellationToken);
                }

                waypointIndex = 0; // Reset waypoint after first loop
            }
        }
    }
}