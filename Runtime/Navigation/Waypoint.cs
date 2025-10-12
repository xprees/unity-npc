using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Xprees.Npc.Navigation
{
    public class Waypoint : MonoBehaviour
    {
        [Header("Waypoint Settings")]
        [Tooltip("The time the NPC will wait at this waypoint before moving to the next one.")]
        public float onArriveWaitTime = 2f;

        public Vector3 Position => transform.position;

        public virtual UniTask OnArrivedAtWaypoint(CancellationToken cancellationToken) =>
            UniTask.Delay(TimeSpan.FromSeconds(onArriveWaitTime), cancellationToken: cancellationToken);

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var tint = Color.red;
            if (WaypointIsPartOfRoute(out var route))
            {
                tint = Color.green;
                if (route != null)
                {
                    if (route.ShouldNotShowRouteAndWaypoints()) return;

                    tint = route.gizmosColor;
                }
            }

            Gizmos.DrawIcon(transform.position, "white_pin.png", true, tint);
        }

        private bool WaypointIsPartOfRoute(out Route route)
        {
            route = GetComponentInParent<Route>();
            if (route == null) return false;
            return route.waypoints?.Contains(this) ?? false;
        }
#endif
    }
}