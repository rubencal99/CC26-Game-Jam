using System;
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

        [Header("Movement")]
        [Tooltip("Run and jump tuning. Shared asset, so edits affect every robot using it.")]
        [SerializeField] private MovementSettings settings;

        [Header("Ground Check")]
        [Tooltip("Layers that count as ground.")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [Tooltip("Cast distance below the collider (units).")]
        [SerializeField] private float groundCheckDistance = 0.05f;
        [Tooltip("Min surface normal Y that counts as ground. 0.7 is about 45 degrees.")]
        [SerializeField, Range(0f, 1f)] private float minGroundNormalY = 0.7f;

        [Header("Feedback")]
        [Tooltip("Played when a jump starts.")]
        [SerializeField] private AudioCueDefinition jumpCue;
        [Tooltip("Played when a jump starts.")]
        [SerializeField] private CameraShakeDefinition jumpShake;
        [Tooltip("Played on landing while controlled, if falling at least Min Land Shake Speed.")]
        [SerializeField] private CameraShakeDefinition landShake;
        [Tooltip("Fall speed needed for the land shake (units/s). Filters out small drops.")]
        [SerializeField] private float minLandShakeSpeed = 6f;

        public event Action Jumped;

        public bool IsGrounded { get; private set; }
        public float MoveInput => moveInput;

        // Off = ignores input but keeps simulating gravity and deceleration
        public bool HasControl { get; set; } = true;

        // On = move and jump input ignored, like HasControl off, but owned by abilities (e.g. flamethrower)
        public bool IsRooted { get; set; }

        // On = no gravity or velocity changes; another component moves the body (e.g. MagneticMover). Ground check still runs.
        public bool IsSuspended { get; set; }

        private Rigidbody2D rb;
        private ContactFilter2D groundFilter;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
        private float moveInput;
        private bool jumpHeld;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private float lastVelocityY;
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
            if (!HasControl || IsRooted)
            {
                moveInput = 0f;
                jumpHeld = false;
                jumpBufferTimer = 0f;
                return;
            }

            moveInput = moveAction.action.ReadValue<Vector2>().x;
            jumpHeld = jumpAction.action.IsPressed();

            if (jumpAction.action.WasPressedThisFrame()) jumpBufferTimer = settings.jumpBufferTime;
            else jumpBufferTimer -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            dt = Time.fixedDeltaTime;
            CalculateMovement();
        }

        private void CalculateMovement()
        {
            bool wasGrounded = IsGrounded;
            CheckGround();
            if (IsSuspended) return;

            coyoteTimer = IsGrounded ? settings.coyoteTime : coyoteTimer - dt;

            // The solver has already zeroed velocity on the landing step, so use last step's
            if (IsGrounded && !wasGrounded && HasControl && -lastVelocityY >= minLandShakeSpeed)
            {
                CameraShake.Play(landShake);
            }

            Vector2 v = rb.linearVelocity;
            v.x = CalculateHorizontalVelocity(v.x);
            v.y = CalculateVerticalVelocity(v.y);
            rb.linearVelocity = v;
            lastVelocityY = v.y;
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
            float rate = Mathf.Abs(moveInput) > 0.01f ? settings.acceleration : settings.deceleration;
            if (!IsGrounded) rate *= settings.airControl;
            return Mathf.MoveTowards(currentVelocityX, moveInput * settings.maxSpeed, rate * dt);
        }

        private float CalculateVerticalVelocity(float currentVelocityY)
        {
            // h = g * t^2 / 2 and v = g * t
            float gravity = 2f * settings.maxJumpHeight / (settings.timeToApex * settings.timeToApex);

            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                currentVelocityY = gravity * settings.timeToApex;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                AudioManager.Play(jumpCue, transform.position);
                CameraShake.Play(jumpShake);
                Jumped?.Invoke();
            }

            float multiplier = 1f;
            if (currentVelocityY < 0f) multiplier = settings.fallGravityMultiplier;
            else if (currentVelocityY > 0f && !jumpHeld) multiplier = settings.jumpCutGravityMultiplier;

            return Mathf.Max(currentVelocityY - gravity * multiplier * dt, -settings.maxFallSpeed);
        }
    }
}
