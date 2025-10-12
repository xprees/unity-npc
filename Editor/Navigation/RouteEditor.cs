using System.Linq;
using UnityEditor;
using UnityEngine;
using Xprees.Npc.Navigation;

namespace Xprees.Npc.Editor.Editor.Navigation
{
    [CustomEditor(typeof(Route))]
    public class RouteEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawAssignRouteFromChildrenButton();
            DrawDefaultInspector();
        }

        private void DrawAssignRouteFromChildrenButton()
        {
            var route = (Route) target;
            if (GUILayout.Button("Add children as Waypoints"))
            {
                AddWaypointsFromChildrenToRoute(route);
            }
        }

        private void AddWaypointsFromChildrenToRoute(Route route)
        {
            var children = route.GetComponentsInChildren<Transform>()
                .Where(t => t != route.transform)
                .Select(t => t.gameObject);

            foreach (var child in children)
            {
                var childWaypoint = child.GetComponent<Waypoint>();
                if (childWaypoint == null)
                {
                    childWaypoint = child.AddComponent<Waypoint>();
                }

                if (route.waypoints.Contains(childWaypoint)) continue;
                route.waypoints.Add(childWaypoint);
            }
        }

    }
}