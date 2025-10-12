using UnityEngine;
using UnityEngine.AI;

namespace Xprees.Npc.Extensions
{
    public static class NavmeshAgentExtensions
    {
        public static bool HasArrived(this NavMeshAgent agent)
        {
            if (agent == null) return false;
            if (!agent.isOnNavMesh) return false;

            var dist = agent.remainingDistance;
            return !agent.pathPending
                   && !float.IsPositiveInfinity(dist)
                   && agent.pathStatus is NavMeshPathStatus.PathComplete or NavMeshPathStatus.PathPartial
                   && dist <= Mathf.Min(agent.radius, agent.stoppingDistance);
        }

        public static bool IsMoving(this NavMeshAgent agent, float tolerance = 0.1f) =>
            agent.velocity.magnitude > tolerance;

        public static void Pause(this NavMeshAgent agent)
        {
            if (agent == null || !agent.isOnNavMesh) return;
            agent.isStopped = true;
        }

        public static void UnPause(this NavMeshAgent agent)
        {
            if (agent == null || !agent.isOnNavMesh) return;
            agent.isStopped = false;
        }

        public static bool SetDestinationWithReachabilityCheck(this NavMeshAgent agent, Vector3 destination)
        {
            var path = new NavMeshPath();
            agent.CalculatePath(destination, path);
            var reachable = path.status != NavMeshPathStatus.PathPartial;
            if (!reachable) return false;

            return agent.SetPath(path);
        }
    }
}