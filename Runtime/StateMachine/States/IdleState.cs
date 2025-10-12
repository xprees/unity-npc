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
        private CancellationTokenSource _cts;
        private readonly int _lastWaypointIndex;

        public bool DontGoOnRoute { get; set; }

        private bool IsAlreadyLooping => _cts is { IsCancellationRequested: false };

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

        public override async UniTask EnterState(CancellationToken cancellationToken = default) =>
            await AssignAgentRoute(cancellationToken);

        public override async UniTask UpdateState(CancellationToken cancellationToken = default)
        {
            if (!_routeChanged) return;

            _routeChanged = false;
            CancelRouting();
            await AssignAgentRoute(cancellationToken);
        }

        public override UniTask ExitState(CancellationToken cancellationToken = default)
        {
            agent.UnPause();
            CancelRouting();
            return UniTask.CompletedTask;
        }

        public override UniTask CheckSwitchState(CancellationToken cancellationToken = default) => UniTask.CompletedTask;

        private void CreateRoutingTokenSource() => _cts = new CancellationTokenSource();

        private CancellationToken GetRoutingToken() => _cts.Token;

        private void CancelRouting()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private async UniTask AssignAgentRoute(CancellationToken cancellationToken = default)
        {
            if (DontGoOnRoute || Route == null)
            {
                DontGoAnywhere();
                return;
            }

            if (IsAlreadyLooping) return;

            // Wait for a random amount of time before starting to move -> more natural not all NPCs start moving at the same time
            await UniTask.Delay(TimeSpan.FromSeconds(Random.Range(.1f, 5)), cancellationToken: cancellationToken);

            await UniTask.WaitUntil(() => agent.isOnNavMesh, cancellationToken: cancellationToken);

            if (IsAlreadyLooping) return; // Check again after delay

            CreateRoutingTokenSource();
            await LoopAgentOnRoute(GetRoutingToken());
            CancelRouting();
        }

        private void DontGoAnywhere() => agent.Pause();

        private async UniTask LoopAgentOnRoute(CancellationToken cancellationToken = default)
        {
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