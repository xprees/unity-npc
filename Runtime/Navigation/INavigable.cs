using UnityEngine;

namespace Xprees.Npc.Navigation
{
    public interface INavigable
    {
        void SetDestination(Vector3 destination);
        void SetDestination(Transform destination);
    }
}