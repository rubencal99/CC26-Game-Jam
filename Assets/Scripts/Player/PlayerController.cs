using UnityEngine;
using UnityEngine.InputSystem;

namespace CC26
{
    // 2D side-scroller movement. Gravity is applied manually so jump height and timing are exact.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Vector2 move action. Only X is used.")]
        [SerializeField] private InputActionReference moveAction;
        [Tooltip("Jump button action.")]
        [SerializeField] private InputActionReference jumpAction;

        [Header("Run")]
        [Tooltip("Top horizontal speed (units/s).")]
        [SerializeField] private float maxSpeed = 8f;
        [Tooltip("Speed gained per second while holding a direction.")]
        [SerializeField] private float acceleration = 60f;
        [Tooltip("Speed lost per second with no input.")]
        [SerializeField] private float deceleration = 70f;
        [Tooltip("Acceleration and deceleration multiplier while airborne.")]
        [SerializeField, Range(0f, 1f)] private float airControl = 0.6f;

        [Header("Jump")]
        [Tooltip("Peak height with jump held (units).")]
        [SerializeField] private float maxJumpHeight = 3f;
        [Tooltip("Seconds to reach peak height. Sets gravity.")]
        [SerializeField] private float timeToApex = 0.4f;
        [Tooltip("Gravity multiplier while falling.")]
        [SerializeField] private float fallGravityMultiplier = 1.8f;
        [Tooltip("Gravity multiplier while rising with jump released. Higher means shorter taps.")]
        [SerializeField] private float jumpCutGravityMultiplier = 3f;
        [Tooltip("Max fall speed (units/s).")]
        [SerializeField] private float maxFallSpeed = 20f;
        [Tooltip("Seconds after leaving ground that a jump is still allowed.")]
        [SerializeField] private float coyoteTime = 0.1f;
        [Tooltip("Seconds a jump press is remembered before landing.")]
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Ground Check")]
        [Tooltip("Layers that count as ground.")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [Tooltip("Cast distance below the collider (units).")]
        [SerializeField] private float groundCheckDistance = 0.05f;
        [Tooltip("Min surface normal Y that counts as ground. 0.7 is about 45 degrees.")]
        [SerializeField, Range(0f, 1f)] private float minGroundNormalY = 0.7f;

        public bool IsGrounded { get; private set; }

        // Off = ignores input but keeps simulating gravity and deceleration
        public bool HasControl { get; set; } = true;

        private Rigidbody2D rb;
        private ContactFilter2D groundFilter;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
        private float moveInput;
        private bool jumpHeld;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private float dt = 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }

        private void OnEnable()
        {
            moveAction.action.Enable();
            jumpAction.action.Enable();
        }

        private void Update()
        {
            if (!HasControl)
            {
                moveInput = 0f;
                jumpHeld = false;
                jumpBufferTimer = 0f;
                return;
            }

            moveInput = moveAction.action.ReadValue<Vector2>().x;
            jumpHeld = jumpAction.action.IsPressed();

            if (jumpAction.action.WasPressedThisFrame()) jumpBufferTimer = jumpBufferTime;
            else jumpBufferTimer -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            dt = Time.fixedDeltaTime;
            CalculateMovement();
        }

        private void CalculateMovement()
        {
            CheckGround();
            coyoteTimer = IsGrounded ? coyoteTime : coyoteTimer - dt;

            Vector2 v = rb.linearVelocity;
            v.x = CalculateHorizontalVelocity(v.x); 
            v.y = CalculateVerticalVelocity(v.y); 
            rb.linearVelocity = v;
        }

        private void CheckGround()
        {
            IsGrounded = false;

            // Still rising from a jump
            if (rb.linearVelocity.y > 0.01f) return;

            groundFilter.SetLayerMask(groundLayers);
            int count = rb.Cast(Vector2.down, groundFilter, groundHits, groundCheckDistance);
            for (int i = 0; i < count; i++)
            {
                if (groundHits[i].normal.y >= minGroundNormalY)
                {
                    IsGrounded = true;
                    return;
                }
            }
        }
        
        private float CalculateHorizontalVelocity(float currentVelocityX)
        {
            // if we're providing input, use acceleration; otherwise, use deceleration
            float rate = Mathf.Abs(moveInput) > 0.01f ? acceleration : deceleration;
            if (!IsGrounded) rate *= airControl;
            return Mathf.MoveTowards(currentVelocityX, moveInput * maxSpeed, rate * dt);
        }

        private float CalculateVerticalVelocity(float currentVelocityY)
        {
            // h = g * t^2 / 2 and v = g * t
            float gravity = 2f * maxJumpHeight / (timeToApex * timeToApex);

            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                currentVelocityY = gravity * timeToApex;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }

            float multiplier = 1f;
            if (currentVelocityY < 0f) multiplier = fallGravityMultiplier;
            else if (currentVelocityY > 0f && !jumpHeld) multiplier = jumpCutGravityMultiplier;

            return Mathf.Max(currentVelocityY - gravity * multiplier * dt, -maxFallSpeed);
        }
    }
}
