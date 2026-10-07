using UnityEngine;

namespace CC26
{
    // Drives the robot sprite Animator from movement and flips the visuals to face the move direction.
    [RequireComponent(typeof(PlayerController))]
    public class RobotAnimator : MonoBehaviour
    {
        private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");
        private static readonly int IsJumpingParam = Animator.StringToHash("IsJumping");
        private static readonly int YDeltaParam = Animator.StringToHash("YDelta");
        private static readonly int JumpTriggerParam = Animator.StringToHash("JumpTrigger");

        [Tooltip("Animator on the sprites child.")]
        [SerializeField] private Animator animator;
        [Tooltip("Flipped on X to face the move direction. Parent of the sprites and any aimed parts.")]
        [SerializeField] private Transform visuals;
        [Tooltip("Sprite art faces right. Off if it faces left.")]
        [SerializeField] private bool artFacesRight = true;
        [Tooltip("Horizontal speed that counts as moving (units/s).")]
        [SerializeField] private float moveThreshold = 0.1f;
        [Tooltip("Vertical speed that maps to full rise or fall in YDelta (units/s).")]
        [SerializeField] private float fullBlendSpeed = 5f;

        private PlayerController controller;
        private Rigidbody2D rb;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable() => controller.Jumped += OnJumped;

        private void OnDisable() => controller.Jumped -= OnJumped;

        private void Update()
        {
            Vector2 v = rb.linearVelocity;
            animator.SetBool(IsMovingParam, Mathf.Abs(v.x) > moveThreshold);
            animator.SetBool(IsJumpingParam, !controller.IsGrounded);
            animator.SetFloat(YDeltaParam, Mathf.Clamp(v.y / fullBlendSpeed, -1f, 1f));

            // Input, not velocity, so the robot still turns when pushing into a wall
            float input = controller.MoveInput;
            if (Mathf.Abs(input) > 0.01f)
            {
                Vector3 scale = visuals.localScale;
                float facing = Mathf.Sign(input) * (artFacesRight ? 1f : -1f);
                scale.x = Mathf.Abs(scale.x) * facing;
                visuals.localScale = scale;
            }
        }

        private void OnJumped() => animator.SetTrigger(JumpTriggerParam);
    }
}
