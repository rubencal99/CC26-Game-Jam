using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace CC26
{
    // Down while any robot (active or dead) is in the press zone. Activates its targets on press and on release.
    public class FloorButton : MonoBehaviour
    {
        [Tooltip("Objects with IObstacle components to activate.")]
        [SerializeField] private GameObject[] targets;
        [Tooltip("Trigger collider just above the button top. Any robot inside holds it down.")]
        [SerializeField] private Collider2D pressZone;
        [SerializeField] private Animator animator;
        [Tooltip("Animator bool, true while held down.")]
        [FormerlySerializedAs("pressTrigger")]
        [SerializeField] private string pressedParam = "Pressed";
        [Tooltip("Optional. Played on press.")]
        [SerializeField] private AudioSource clickSound;

        public bool IsPressed { get; private set; }

        private readonly List<IObstacle> obstacles = new();
        private readonly List<Collider2D> overlaps = new();
        private ContactFilter2D filter;

        private void Awake()
        {
            foreach (GameObject target in targets)
            {
                obstacles.AddRange(target.GetComponents<IObstacle>());
            }
            filter = ContactFilter2D.noFilter;
        }

        private void OnEnable() => RobotQueue.LevelReset += ResetButton;

        private void OnDisable() => RobotQueue.LevelReset -= ResetButton;

        // Polled, not event driven: a robot locking on the button turns Static and fires a false exit event
        private void FixedUpdate()
        {
            bool pressed = HasRobot();
            if (pressed == IsPressed) return;

            IsPressed = pressed;
            animator.SetBool(pressedParam, pressed);
            if (pressed && clickSound != null) clickSound.Play();
            foreach (IObstacle obstacle in obstacles) obstacle.Activate();
        }

        private bool HasRobot()
        {
            int count = pressZone.Overlap(filter, overlaps);
            for (int i = 0; i < count; i++)
            {
                Rigidbody2D body = overlaps[i].attachedRigidbody;
                if (body != null && body.TryGetComponent(out Robot _)) return true;
            }
            return false;
        }

        // Robots are destroyed on reset, so release silently. Doors reset themselves.
        private void ResetButton()
        {
            IsPressed = false;
            animator.Rebind();
            animator.Update(0f);
        }
    }
}
