using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CC26
{
    // Fire robot ability. Arming shuts the robot down and plays its countdown, then the blast breaks walls, crates, and dead robots in range.
    [RequireComponent(typeof(Robot))]
    public class SelfDestruct : MonoBehaviour
    {
        private static readonly int SelfDestructParam = Animator.StringToHash("SelfDestruct");

        [Tooltip("Arms the self-destruct (Player/Interact). The action's Hold interaction, if any, applies.")]
        [SerializeField] private InputActionReference selfDestructAction;
        [SerializeField] private Animator animator;

        [Header("Blast")]
        [Tooltip("Seconds from arming to the blast. Match the countdown animation.")]
        [SerializeField] private float fuseTime = 1.5f;
        [Tooltip("Seconds the camera holds on the blast before the queue's spawn delay starts.")]
        [SerializeField] private float lingerTime = 1.5f;
        [Tooltip("Blast radius (units). Breakable walls, crates, and dead robots touching it explode.")]
        [SerializeField] private float blastRadius = 3f;

        [Header("Feedback")]
        [Tooltip("Optional. Played when armed, e.g. a countdown beep.")]
        [SerializeField] private AudioCueDefinition armCue;
        [Tooltip("Optional. Spawned at the robot on blast, e.g. a fireball particle prefab. Should destroy itself.")]
        [SerializeField] private GameObject blastEffect;
        [Tooltip("Optional. Played on blast, on top of the robot's explode feedback.")]
        [SerializeField] private AudioCueDefinition blastCue;
        [Tooltip("Optional. Played on blast, on top of the robot's explode feedback.")]
        [SerializeField] private CameraShakeDefinition blastShake;

        public bool IsArmed { get; private set; }

        private Robot robot;
        private readonly List<Collider2D> hits = new();

        private void Awake() => robot = GetComponent<Robot>();

        private void OnEnable()
        {
            selfDestructAction.action.performed += OnSelfDestruct;
            selfDestructAction.action.Enable();
        }

        private void OnDisable() => selfDestructAction.action.performed -= OnSelfDestruct;

        // Input fires while paused, so ignore it then. Every fire robot hears the action, so only the active one arms.
        private void OnSelfDestruct(InputAction.CallbackContext _)
        {
            if (IsArmed || !robot.IsActive || Time.timeScale == 0f) return;

            IsArmed = true;
            // The queue waits out the fuse and linger, so the camera stays on this robot
            robot.Decommission(fuseTime + lingerTime);
            animator.SetTrigger(SelfDestructParam);
            AudioManager.Play(armCue, transform.position);
            StartCoroutine(Fuse());
        }

        private IEnumerator Fuse()
        {
            yield return new WaitForSeconds(fuseTime);
            Blast();
        }

        private void Blast()
        {
            Vector2 center = transform.position;
            if (blastEffect != null) Instantiate(blastEffect, center, Quaternion.identity);
            AudioManager.Play(blastCue, center);
            CameraShake.Play(blastShake);

            int count = Physics2D.OverlapCircle(center, blastRadius, ContactFilter2D.noFilter, hits);
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = hits[i];

                BreakableWall wall = hit.GetComponentInParent<BreakableWall>();
                if (wall != null) wall.Explode();

                Burnable crate = hit.GetComponentInParent<Burnable>();
                if (crate != null) crate.Smash();

                // Dead bodies only: hiding the active robot would stall the queue
                Rigidbody2D body = hit.attachedRigidbody;
                if (body != null && body.TryGetComponent(out Robot other) && other != robot && other.IsDecommissioned) other.Explode();
            }

            // Last, since it hides this object
            robot.Explode();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, blastRadius);
        }
    }
}
