using System;
using UnityEngine;

namespace CC26
{
    // Robot root. Owned by RobotQueue. Once decommissioned it falls, then locks in place as a static platform.
    [RequireComponent(typeof(PlayerController))]
    public class Robot : MonoBehaviour
    {
        public event Action<Robot> Decommissioned;

        [Tooltip("Layers whose IHazard components affect this robot.")]
        [SerializeField] private LayerMask hazardLayers;

        [Header("Feedback")]
        [Tooltip("Played on manual shutdown (F).")]
        [SerializeField] private AudioCueDefinition shutdownCue;
        [Tooltip("Played on manual shutdown (F).")]
        [SerializeField] private CameraShakeDefinition shutdownShake;
        [Tooltip("Played when a hazard breaks the robot. Replaces the shutdown cue.")]
        [SerializeField] private AudioCueDefinition breakCue;
        [Tooltip("Played when a hazard breaks the robot. Replaces the shutdown shake.")]
        [SerializeField] private CameraShakeDefinition breakShake;

        public bool IsActive { get; private set; }
        public bool IsDecommissioned { get; private set; }

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
            if (IsDecommissioned && !isLocked && controller.IsGrounded) Lock();
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
