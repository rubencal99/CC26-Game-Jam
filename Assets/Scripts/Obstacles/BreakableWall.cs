using UnityEngine;

namespace CC26
{
    // Explodes when caught in a self-destruct blast. Comes back on level reset.
    public class BreakableWall : MonoBehaviour
    {
        [Tooltip("Child with the sprite and collider. Hidden when exploded. Must not be this object, or reset can't bring it back.")]
        [SerializeField] private GameObject body;
        [Tooltip("Optional. Spawned at the body on explode, e.g. a debris particle prefab. Should destroy itself.")]
        [SerializeField] private GameObject explodeEffect;
        [Tooltip("Optional. Played on explode.")]
        [SerializeField] private AudioCueDefinition explodeCue;
        [Tooltip("Optional. Played on explode.")]
        [SerializeField] private CameraShakeDefinition explodeShake;

        public bool IsExploded { get; private set; }

        private void OnEnable() => RobotQueue.LevelReset += ResetWall;

        private void OnDisable() => RobotQueue.LevelReset -= ResetWall;

        public void Explode()
        {
            if (IsExploded) return;
            IsExploded = true;
            body.SetActive(false);
            Vector3 position = body.transform.position;
            if (explodeEffect != null) Instantiate(explodeEffect, position, Quaternion.identity);
            AudioManager.Play(explodeCue, position);
            CameraShake.Play(explodeShake);
        }

        private void ResetWall()
        {
            IsExploded = false;
            body.SetActive(true);
        }
    }
}
