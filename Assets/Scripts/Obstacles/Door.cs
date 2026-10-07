using UnityEngine;

namespace CC26
{
    // Toggles between closed and open each time it is activated. Starts closed and closes again on level reset.
    [RequireComponent(typeof(Rigidbody2D))]
    public class Door : MonoBehaviour, IObstacle
    {
        public enum ShakeMode { OnActivate, WhileMoving, WhileOpen }

        [Tooltip("Local position when closed.")]
        [SerializeField] private Vector3 closedPosition;
        [Tooltip("Local position when open.")]
        [SerializeField] private Vector3 openPosition;
        [Tooltip("Closed (0) to open (1) over normalized time. Played in reverse when closing.")]
        [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Seconds for a full open or close.")]
        [SerializeField] private float duration = 0.5f;

        [Header("Camera Shake")]
        [Tooltip("Optional. Shake played by this door.")]
        [SerializeField] private CameraShakeDefinition shake;
        [Tooltip("OnActivate: once per open or close. WhileMoving / WhileOpen: held at the shake's starting strength, then fades out normally.")]
        [SerializeField] private ShakeMode shakeMode = ShakeMode.OnActivate;

        public bool IsOpen { get; private set; }
        public bool IsMoving => !Mathf.Approximately(progress, IsOpen ? 1f : 0f);

        private Rigidbody2D rb;
        // 0 = closed, 1 = open. Reversing mid-move continues from here, so there is no snap.
        private float progress;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            // Dynamic or position-frozen bodies ignore MovePosition
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.None;
            ResetDoor();
        }

        private void OnEnable() => RobotQueue.LevelReset += ResetDoor;

        private void OnDisable() => RobotQueue.LevelReset -= ResetDoor;

        public void Activate()
        {
            IsOpen = !IsOpen;
            Debug.Log("Door activated. IsOpen: " + IsOpen);
            if (shakeMode == ShakeMode.OnActivate) CameraShake.Play(shake);
        }

        // Replaying restarts the shake at time 0 each frame, which holds it there until we stop
        private void Update()
        {
            bool hold = shakeMode == ShakeMode.WhileMoving ? IsMoving : shakeMode == ShakeMode.WhileOpen && IsOpen;
            if (hold) CameraShake.Play(shake);
        }

        // Kinematic MovePosition so robots get pushed or carried properly
        private void FixedUpdate()
        {
            if (!IsMoving) return;

            progress = Mathf.MoveTowards(progress, IsOpen ? 1f : 0f, Time.fixedDeltaTime / Mathf.Max(duration, 0.0001f));
            rb.MovePosition(ToWorld(Vector3.LerpUnclamped(closedPosition, openPosition, curve.Evaluate(progress))));
        }

        private void ResetDoor()
        {
            IsOpen = false;
            progress = 0f;
            transform.localPosition = closedPosition;
            rb.position = transform.position;
        }

        private Vector3 ToWorld(Vector3 local)
        {
            return transform.parent != null ? transform.parent.TransformPoint(local) : local;
        }
    }
}
