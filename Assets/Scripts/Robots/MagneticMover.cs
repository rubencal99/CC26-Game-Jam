using UnityEngine;
using UnityEngine.InputSystem;

namespace CC26
{
    // Magnetic robot movement. A direction press slides it straight along one axis, ignoring gravity, until it hits something solid.
    // Stays put until the next press. Smashes Burnables in its path and keeps going.
    [RequireComponent(typeof(Robot))]
    public class MagneticMover : MonoBehaviour
    {
        // Gap that counts as touching (units). Above the 2D contact offset.
        private const float ContactDistance = 0.05f;

        [Tooltip("Vector2 move action (Player/Move). Needs up and down bindings.")]
        [SerializeField] private InputActionReference moveAction;
        [Tooltip("Slide speed (units/s).")]
        [SerializeField] private float speed = 20f;
        [Tooltip("Slide down on spawn, so it doesn't hover at the spawn point.")]
        [SerializeField] private bool dropOnSpawn = true;

        [Header("Feedback")]
        [Tooltip("Played when a slide starts.")]
        [SerializeField] private AudioCueDefinition launchCue;
        [Tooltip("Played when a slide ends against something.")]
        [SerializeField] private AudioCueDefinition impactCue;
        [Tooltip("Played when a slide ends against something.")]
        [SerializeField] private CameraShakeDefinition impactShake;

        public Vector2 Direction => direction;
        public bool IsSliding => direction != Vector2.zero;

        private Robot robot;
        private Rigidbody2D rb;
        private ContactFilter2D filter;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        private Vector2 direction;
        private Vector2 queued;
        private Vector2 lastInput;

        private void Awake()
        {
            robot = GetComponent<Robot>();
            rb = GetComponent<Rigidbody2D>();
            GetComponent<PlayerController>().IsSuspended = true;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;

            // Same layers the robot physically collides with, so the probe agrees with the solver
            filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        }

        private void OnEnable() => moveAction.action.Enable();

        private void Start()
        {
            if (dropOnSpawn) queued = Vector2.down;
        }

        // Presses during a slide are ignored, not buffered
        private void Update()
        {
            if (!robot.IsActive || Time.timeScale == 0f) return;

            Vector2 input = Snap(moveAction.action.ReadValue<Vector2>());
            if (!IsSliding)
            {
                // Edge per axis, so pressing up while still holding right turns
                if (input.x != 0f && input.x != lastInput.x) queued = new Vector2(input.x, 0f);
                else if (input.y != 0f && input.y != lastInput.y) queued = new Vector2(0f, input.y);
            }
            lastInput = input;
        }

        private void FixedUpdate()
        {
            if (!robot.IsActive) return;

            if (!IsSliding)
            {
                Vector2 next = queued;
                queued = Vector2.zero;
                if (next == Vector2.zero || Probe(next, ContactDistance)) return;
                Launch(next);
            }

            // A full step ahead, so crates break before the solver stops the robot on them
            if (Probe(direction, speed * Time.fixedDeltaTime + ContactDistance)) Stop();
            else rb.linearVelocity = direction * speed;
        }

        // Smashes Burnables within distance along dir. True if anything else is touching on that side.
        private bool Probe(Vector2 dir, float distance)
        {
            bool blocked = false;
            int count = rb.Cast(dir, filter, hits, distance);
            for (int i = 0; i < count; i++)
            {
                // Surfaces it slides along don't count
                if (Vector2.Dot(hits[i].normal, dir) > -0.5f) continue;

                Burnable crate = hits[i].collider.GetComponentInParent<Burnable>();
                if (crate != null) crate.Smash();
                else if (hits[i].distance <= ContactDistance) blocked = true;
            }
            return blocked;
        }

        private void Launch(Vector2 dir)
        {
            direction = dir;
            // Freeze the other axis so the slide stays straight
            rb.constraints = RigidbodyConstraints2D.FreezeRotation |
                (dir.x != 0f ? RigidbodyConstraints2D.FreezePositionY : RigidbodyConstraints2D.FreezePositionX);
            AudioManager.Play(launchCue, transform.position);
        }

        private void Stop()
        {
            direction = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            // Frozen so other robots can stand on it without pushing it
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            AudioManager.Play(impactCue, transform.position);
            CameraShake.Play(impactShake);
        }

        private static Vector2 Snap(Vector2 v)
        {
            return new Vector2(Mathf.Abs(v.x) > 0.5f ? Mathf.Sign(v.x) : 0f, Mathf.Abs(v.y) > 0.5f ? Mathf.Sign(v.y) : 0f);
        }
    }
}
