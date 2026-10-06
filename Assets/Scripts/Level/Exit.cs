using UnityEngine;

namespace CC26
{
    // Level goal. The active robot touching it completes the level. Dead bodies don't count.
    public class Exit : MonoBehaviour
    {
        [SerializeField] private GameSceneManager gameSceneManager;

        private void OnCollisionEnter2D(Collision2D collision) => TryComplete(collision.collider);

        private void OnTriggerEnter2D(Collider2D other) => TryComplete(other);

        private void TryComplete(Collider2D other)
        {
            if (other.attachedRigidbody == null) return;
            if (!other.attachedRigidbody.TryGetComponent(out Robot robot) || !robot.IsActive) return;

            gameSceneManager.CompleteLevel();
        }
    }
}
