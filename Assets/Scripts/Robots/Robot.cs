using System;
using System.Collections;
using UnityEngine;

namespace CC26
{
    // Robot root. Owned by RobotQueue. Shut down (F): falls (unless lockInPlace), then locks as a static platform.
    // Hazard hit: fried (locks where it is, stays as a platform) or destroyed (death animation, then gone), per hazard type.
    [RequireComponent(typeof(PlayerController))]
    public class Robot : MonoBehaviour
    {
        private static readonly int FriedTriggerParam = Animator.StringToHash("FriedTrigger");
        private static readonly int DeathTriggerParam = Animator.StringToHash("DeathTrigger");

        public event Action<Robot> Decommissioned;

        [Tooltip("Animator on the sprites child. Gets FriedTrigger and DeathTrigger. Empty = first Animator in children.")]
        [SerializeField] private Animator animator;

        [Tooltip("Layers whose IHazard components affect this robot.")]
        [SerializeField] private LayerMask hazardLayers;
        [Tooltip("Hazards that don't affect this robot.")]
        [SerializeField] private HazardType immunities;
        [Tooltip("Hazards that fry this robot: it locks where it is and stays as a platform. Any other hazard destroys it.")]
        [SerializeField] private HazardType friedBy;
        [Tooltip("Seconds the death animation plays before a destroyed robot disappears. Match the Death clip.")]
        [SerializeField] private float deathTime = 0.33f;
        [Tooltip("Lock where it is when decommissioned instead of falling first. For the Magnetic robot.")]
        [SerializeField] private bool lockInPlace;

        [Header("Feedback")]
        [Tooltip("Played on manual shutdown (F).")]
        [SerializeField] private AudioCueDefinition shutdownCue;
        [Tooltip("Played on manual shutdown (F).")]
        [SerializeField] private CameraShakeDefinition shutdownShake;
        [Tooltip("Played when a hazard fries the robot. Replaces the shutdown cue.")]
        [SerializeField] private AudioCueDefinition breakCue;
        [Tooltip("Played when a hazard fries the robot. Replaces the shutdown shake.")]
        [SerializeField] private CameraShakeDefinition breakShake;
        [Tooltip("Optional. Spawned by Explode() (hazard death, self-destruct, level reset, blasts), e.g. a gore particle prefab. Should destroy itself.")]
        [SerializeField] private GameObject explodeEffect;
        [Tooltip("Played by Explode().")]
        [SerializeField] private AudioCueDefinition explodeCue;
        [Tooltip("Played by Explode().")]
        [SerializeField] private CameraShakeDefinition explodeShake;

        public bool IsActive { get; private set; }
        public bool IsDecommissioned { get; private set; }
        public bool HasExploded { get; private set; }
        // Added to the queue's spawn delay, e.g. to hold the camera on a self-destruct
        public float ExtraSpawnDelay { get; private set; }

        private PlayerController controller;
        private Rigidbody2D rb;
        private bool isLocked;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody2D>();
            // Variants swap the sprites child, which would break an inherited reference
            if (animator == null) animator = GetComponentInChildren<Animator>();
            controller.HasControl = false;
        }

        public void Activate()
        {
            IsActive = true;
            controller.HasControl = true;
        }

        public void Decommission()
        {
            if (!IsActive) return;
            Shutdown(shutdownCue, shutdownShake);
        }

        // Ability shutdown (e.g. self-destruct). The ability plays its own feedback.
        public void Decommission(float extraSpawnDelay)
        {
            if (!IsActive) return;
            ExtraSpawnDelay = extraSpawnDelay;
            Shutdown(null, null);
        }

        // Hazard hit. Both outcomes lock on the spot: a fried body stays, a destroyed one explodes and vanishes after its death animation.
        public void Break(HazardType type)
        {
            if (!IsActive || IsImmuneTo(type)) return;
            bool fried = (friedBy & type) != 0;
            if (fried) Shutdown(breakCue, breakShake);
            else Shutdown(null, null);
            Lock();

            if (animator != null) animator.SetTrigger(fried ? FriedTriggerParam : DeathTriggerParam);
            // Death clips burst on their first frame, so the gore goes with them
            if (!fried) Explode(deathTime);
        }

        private void Shutdown(AudioCueDefinition cue, CameraShakeDefinition shake)
        {
            AudioManager.Play(cue, transform.position);
            CameraShake.Play(shake);
            IsActive = false;
            IsDecommissioned = true;
            controller.HasControl = false;
            // Freeze the robot's position when decommissioned.
            //rb.constraints = RigidbodyConstraints2D.FreezePosition;
            Decommissioned?.Invoke(this);
        }

        public bool IsImmuneTo(HazardType type) => (immunities & type) != 0;

        // Bursts into parts now, then disappears after hideDelay so an explosion clip on the sprite can finish. Safe to call more than once.
        public void Explode(float hideDelay = 0f)
        {
            if (HasExploded) return;
            HasExploded = true;
            if (explodeEffect != null) Instantiate(explodeEffect, transform.position, Quaternion.identity);
            AudioManager.Play(explodeCue, transform.position);
            CameraShake.Play(explodeShake);

            if (hideDelay > 0f) StartCoroutine(HideAfter(hideDelay));
            else Hide();
        }

        private IEnumerator HideAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            Hide();
        }

        // Hidden, not destroyed: the queue still owns it, and the camera holds on its last position
        private void Hide() => gameObject.SetActive(false);

        private void OnCollisionEnter2D(Collision2D collision) => TryHazard(collision.collider);

        private void OnTriggerEnter2D(Collider2D other) => TryHazard(other);

        private void TryHazard(Collider2D other)
        {
            if (!IsActive || (hazardLayers & (1 << other.gameObject.layer)) == 0) return;
            IHazard hazard = other.GetComponentInParent<IHazard>();
            hazard?.Apply(this);
        }

        private void FixedUpdate()
        {
            if (IsDecommissioned && !isLocked && (lockInPlace || controller.IsGrounded)) Lock();
        }

        private void Lock()
        {
            isLocked = true;
            controller.enabled = false;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }
    }
}
