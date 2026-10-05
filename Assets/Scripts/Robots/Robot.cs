using System;
using UnityEngine;

namespace CC26
{
    // Robot root. Owned by RobotQueue. Once decommissioned it falls, then locks in place as a static platform.
    [RequireComponent(typeof(PlayerController))]
    public class Robot : MonoBehaviour
    {
        public event Action<Robot> Decommissioned;

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
            Decommissioned?.Invoke(this);
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
