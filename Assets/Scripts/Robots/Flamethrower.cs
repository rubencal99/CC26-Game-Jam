using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CC26
{
    // Fire robot ability. Hold fire to root in place and ignite any Burnable inside the flame box.
    [RequireComponent(typeof(Robot))]
    public class Flamethrower : MonoBehaviour
    {
        private static readonly int IsFlameThrowingParam = Animator.StringToHash("IsFlameThrowing");

        [Tooltip("Fire button action (Player/Attack).")]
        [SerializeField] private InputActionReference fireAction;
        [SerializeField] private Animator animator;

        [Header("Flame")]
        [Tooltip("Center of the flame box. Child of the flipped visuals so it follows facing.")]
        [SerializeField] private Transform flameOrigin;
        [Tooltip("Flame box width and height (units).")]
        [SerializeField] private Vector2 flameSize = new(2f, 0.6f);
        [Tooltip("Layers with Burnable objects.")]
        [SerializeField] private LayerMask burnableLayers;

        [Header("Feedback")]
        [Tooltip("Optional looping AudioSource, played while firing.")]
        [SerializeField] private AudioSource flameSound;
        [Tooltip("Optional. Held while firing.")]
        [SerializeField] private CameraShakeDefinition flameShake;

        public bool IsFiring { get; private set; }

        private Robot robot;
        private PlayerController controller;
        private ContactFilter2D filter;
        private readonly List<Collider2D> hits = new();

        private void Awake()
        {
            robot = GetComponent<Robot>();
            controller = GetComponent<PlayerController>();
            filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(burnableLayers);
        }

        private void OnEnable() => fireAction.action.Enable();

        private void OnDisable() => SetFiring(false);

        private void Update()
        {
            SetFiring(robot.IsActive && Time.timeScale > 0f && fireAction.action.IsPressed());
            // Replaying holds the shake at its start strength
            if (IsFiring) CameraShake.Play(flameShake);
        }

        private void FixedUpdate()
        {
            if (!IsFiring) return;

            int count = Physics2D.OverlapBox(flameOrigin.position, flameSize, 0f, filter, hits);
            for (int i = 0; i < count; i++)
            {
                Burnable burnable = hits[i].GetComponentInParent<Burnable>();
                if (burnable != null) burnable.Ignite();
            }
        }

        private void SetFiring(bool firing)
        {
            if (firing == IsFiring) return;
            IsFiring = firing;
            controller.IsRooted = firing;
            animator.SetBool(IsFlameThrowingParam, firing);

            if (flameSound == null) return;
            if (firing) flameSound.Play();
            else flameSound.Stop();
        }

        private void OnDrawGizmosSelected()
        {
            if (flameOrigin == null) return;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireCube(flameOrigin.position, flameSize);
        }
    }
}
