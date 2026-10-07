using UnityEngine;

namespace CC26
{
    // Run and jump tuning for PlayerController. Read every physics step, so edits apply live in Play Mode and persist.
    [CreateAssetMenu(fileName = "Movement_", menuName = "CC26/Movement Settings")]
    public class MovementSettings : ScriptableObject
    {
        [Header("Run")]
        [Tooltip("Top horizontal speed (units/s).")]
        public float maxSpeed = 20f;
        [Tooltip("Speed gained per second while holding a direction.")]
        public float acceleration = 60f;
        [Tooltip("Speed lost per second with no input.")]
        public float deceleration = 70f;
        [Tooltip("Acceleration and deceleration multiplier while airborne.")]
        [Range(0f, 1f)] public float airControl = 0.6f;

        [Header("Jump")]
        [Tooltip("Peak height with jump held (units).")]
        public float maxJumpHeight = 5f;
        [Tooltip("Seconds to reach peak height. Sets gravity.")]
        public float timeToApex = 0.4f;
        [Tooltip("Gravity multiplier while falling.")]
        public float fallGravityMultiplier = 1.8f;
        [Tooltip("Gravity multiplier while rising with jump released. Higher means shorter taps.")]
        public float jumpCutGravityMultiplier = 3f;
        [Tooltip("Max fall speed (units/s).")]
        public float maxFallSpeed = 20f;
        [Tooltip("Seconds after leaving ground that a jump is still allowed.")]
        public float coyoteTime = 0.1f;
        [Tooltip("Seconds a jump press is remembered before landing.")]
        public float jumpBufferTime = 0.1f;
    }
}
