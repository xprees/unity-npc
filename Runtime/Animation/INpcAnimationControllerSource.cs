using UnityEngine;

namespace Xprees.Npc.Animation
{
    public interface INpcAnimationControllerSource
    {
        bool IsMoving { get; }
        bool IsInteracting { get; }
        bool IsTalking { get; }
        Vector3 Velocity { get; }
        float Speed { get; }
    }
}