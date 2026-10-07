using UnityEngine;

namespace CC26
{
    // Burns away after enough time in a flame. Comes back on level reset.
    public class Burnable : MonoBehaviour
    {
        [Tooltip("Seconds in the flame before it burns away. Not lost when the flame stops.")]
        [SerializeField] private float burnTime = 1f;
        [Tooltip("Child with the sprite and collider. Hidden when burned. Must not be this object, or reset can't bring it back.")]
        [SerializeField] private GameObject body;
        [Tooltip("Optional. Played when it burns away.")]
        [SerializeField] private AudioCueDefinition burnCue;
        [Tooltip("Optional. Played when it burns away.")]
        [SerializeField] private CameraShakeDefinition burnShake;

        public bool IsBurned { get; private set; }

        private float heat;

        private void OnEnable() => RobotQueue.LevelReset += ResetBurnable;

        private void OnDisable() => RobotQueue.LevelReset -= ResetBurnable;

        public void Burn(float seconds)
        {
            if (IsBurned) return;
            heat += seconds;
            if (heat < burnTime) return;

            IsBurned = true;
            body.SetActive(false);
            AudioManager.Play(burnCue, transform.position);
            CameraShake.Play(burnShake);
        }

        private void ResetBurnable()
        {
            IsBurned = false;
            heat = 0f;
            body.SetActive(true);
        }
    }
}
