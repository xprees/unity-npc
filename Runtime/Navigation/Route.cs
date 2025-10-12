using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xprees.Npc.Navigation
{
    public class Route : MonoBehaviour
    {
        public List<Waypoint> waypoints;

#if UNITY_EDITOR
        [Header("Editor")]
        public bool showRouteOnlyWhenSelected = true;

        public Color gizmosColor = new(0, 1, 0, 1);

        private void OnValidate()
        {
            // Check that the GameObject is present in the scene and not a prefab
            if (gameObject.scene.rootCount == 0) return;
            waypoints = waypoints?.Where(t => t != null).ToList(); // remove left null refs
        }

        private void OnDrawGizmos()
        {
            if (ShouldNotShowRouteAndWaypoints()) return;
            if (waypoints == null || waypoints.Count == 0) return;

            Gizmos.color = gizmosColor;

            for (var i = 0; i < waypoints.Count; i++)
            {
                var currentWaypoint = waypoints[i];
                if (currentWaypoint == null) break;

                if (i <= 0) continue;
                var previousWaypoint = waypoints[i - 1];
                if (previousWaypoint == null) break; // DrawGizmos will be called again on next frame -> causes null ref when removing waypoints

                Gizmos.DrawLine(previousWaypoint.transform.position, currentWaypoint.transform.position);
            }

            if (waypoints.Count > 2)
            {
                if (waypoints[0] == null || waypoints[^1] == null) return;

                Gizmos.DrawLine(waypoints[0].transform.position, waypoints[^1].transform.position);
            }
        }

        internal bool ShouldNotShowRouteAndWaypoints() => showRouteOnlyWhenSelected && IsNotSelectedRouteOrWaypoint();

        private bool IsNotSelectedRouteOrWaypoint()
        {
            var routePartsGameObjects = waypoints?
                .Select(t => t.gameObject)
                .Append(gameObject);
            return !routePartsGameObjects?.Contains(Selection.activeGameObject) ?? true;
        }
#endif
    }
}