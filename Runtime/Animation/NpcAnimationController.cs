using UnityEngine;

namespace Xprees.Npc.Animation
{
    [RequireComponent(typeof(NpcController))]
    public class NpcAnimationController : MonoBehaviour
    {
        private readonly static int walkSpeedMultiplier = Animator.StringToHash("WalkSpeedMultiplier");
        private readonly static int isMoving = Animator.StringToHash("IsMoving");
        private readonly static int isInteracting = Animator.StringToHash("IsInteracting");
        private readonly static int isTalking = Animator.StringToHash("IsTalking");

        [Header("Animation")]
        public Animator animator;

        private INpcAnimationControllerSource _npcController;

        private void Awake() => _npcController = GetComponent<INpcAnimationControllerSource>();

        private void Update() => UpdateAnimator();

        private void UpdateAnimator()
        {
            var walkSpeedMultiplierValue = Mathf.Clamp(_npcController.Velocity.magnitude, 0, _npcController.Speed);
            animator.SetFloat(walkSpeedMultiplier, walkSpeedMultiplierValue, .25f, Time.fixedDeltaTime);
            animator.SetBool(isMoving, _npcController.IsMoving);
            animator.SetBool(isInteracting, _npcController.IsInteracting);
            animator.SetBool(isTalking, _npcController.IsTalking);
        }
    }
}