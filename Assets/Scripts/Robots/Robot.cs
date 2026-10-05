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
            IsActive = false;
            IsDecommissioned = true;
            controller.HasControl = false;
            rb.constraints = RigidbodyConstraints2D.FreezePosition;
            Decommissioned?.Invoke(this);
        }

        // Hazard death. Kills momentum, then falls and locks like a manual shutdown.
        public void Break()
        {
            if (!IsActive) return;
            rb.linearVelocity = Vector2.zero;
            Decommission();
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
