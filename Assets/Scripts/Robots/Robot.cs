using System;
using UnityEngine;

namespace CC26
{
    // Robot root. Owned by RobotQueue. Once decommissioned it falls (unless lockInPlace), then locks in place as a static platform.
    [RequireComponent(typeof(PlayerController))]
    public class Robot : MonoBehaviour
    {
        public event Action<Robot> Decommissioned;

        [Tooltip("Layers whose IHazard components affect this robot.")]
        [SerializeField] private LayerMask hazardLayers;
        [Tooltip("Hazards that don't affect this robot.")]
        [SerializeField] private HazardType immunities;
        [Tooltip("Lock where it is when decommissioned instead of falling first. For the Magnetic robot.")]
        [SerializeField] private bool lockInPlace;

        [Header("Feedback")]
        [Tooltip("Played on manual shutdown (F).")]
        [SerializeField] private AudioCueDefinition shutdownCue;
        [Tooltip("Played on manual shutdown (F).")]
        [SerializeField] private CameraShakeDefinition shutdownShake;
        [Tooltip("Played when a hazard breaks the robot. Replaces the shutdown cue.")]
        [SerializeField] private AudioCueDefinition breakCue;
        [Tooltip("Played when a hazard breaks the robot. Replaces the shutdown shake.")]
        [SerializeField] private CameraShakeDefinition breakShake;
        [Tooltip("Optional. Spawned by Explode() (level reset, blasts), e.g. a gore particle prefab. Should destroy itself.")]
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

        // Hazard death. Kills momentum, then falls and locks like a manual shutdown.
        public void Break()
        {
            if (!IsActive) return;
            rb.linearVelocity = Vector2.zero;
            Shutdown(breakCue, breakShake);
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

        // Bursts into parts and disappears. Called on level reset and by blasts. Safe to call more than once.
        public void Explode()
        {
            if (HasExploded) return;
            HasExploded = true;
            if (explodeEffect != null) Instantiate(explodeEffect, transform.position, Quaternion.identity);
            AudioManager.Play(explodeCue, transform.position);
            CameraShake.Play(explodeShake);
            // Hidden, not destroyed: the queue still owns it, and the camera holds on its last position
            gameObject.SetActive(false);
        }

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
