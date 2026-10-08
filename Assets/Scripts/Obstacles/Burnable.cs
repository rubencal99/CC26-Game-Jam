using UnityEngine;

namespace CC26
{
    // Ignited by a flame, then dissolves over burnTime and burns away. Can also be smashed instantly. Comes back on level reset.
    public class Burnable : MonoBehaviour
    {
        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

        [Tooltip("Seconds from ignition until it burns away. Keeps counting after the flame stops.")]
        [SerializeField] private float burnTime = 1f;
        [Tooltip("Child with the sprite and collider. Hidden when burned. Must not be this object, or reset can't bring it back.")]
        [SerializeField] private GameObject body;
        [Tooltip("Optional. Renderer with the dissolve material. _DissolveAmount goes 0 to 1 over Burn Time.")]
        [SerializeField] private Renderer dissolveRenderer;
        [Tooltip("Optional. Played when it burns away.")]
        [SerializeField] private AudioCueDefinition burnCue;
        [Tooltip("Optional. Played when it burns away.")]
        [SerializeField] private CameraShakeDefinition burnShake;
        [Tooltip("Optional. Spawned at the body when smashed, e.g. a splinter particle prefab. Should destroy itself.")]
        [SerializeField] private GameObject smashEffect;
        [Tooltip("Optional. Played when smashed.")]
        [SerializeField] private AudioCueDefinition smashCue;
        [Tooltip("Optional. Played when smashed.")]
        [SerializeField] private CameraShakeDefinition smashShake;

        public bool IsBurning { get; private set; }
        public bool IsBurned { get; private set; }

        private float remaining;
        // Per renderer, so crates sharing the material dissolve independently without material copies
        private MaterialPropertyBlock block;

        private void Awake() => block = new MaterialPropertyBlock();

        private void OnEnable() => RobotQueue.LevelReset += ResetBurnable;

        private void OnDisable() => RobotQueue.LevelReset -= ResetBurnable;

        public void Ignite()
        {
            if (IsBurning || IsBurned) return;
            IsBurning = true;
            remaining = burnTime;
        }

        // Instant break (Magnetic robot, blasts). Counts as burned, so reset restores it.
        public void Smash()
        {
            if (IsBurned) return;
            IsBurning = false;
            IsBurned = true;
            body.SetActive(false);
            if (smashEffect != null) Instantiate(smashEffect, body.transform.position, Quaternion.identity);
            AudioManager.Play(smashCue, transform.position);
            CameraShake.Play(smashShake);
        }

        private void Update()
        {
            if (!IsBurning) return;

            remaining -= Time.deltaTime;
            SetDissolve(1f - Mathf.Clamp01(remaining / burnTime));
            if (remaining <= 0f) BurnAway();
        }

        private void BurnAway()
        {
            IsBurning = false;
            IsBurned = true;
            body.SetActive(false);
            AudioManager.Play(burnCue, transform.position);
            CameraShake.Play(burnShake);
        }

        private void ResetBurnable()
        {
            IsBurning = false;
            IsBurned = false;
            SetDissolve(0f);
            body.SetActive(true);
        }

        private void SetDissolve(float amount)
        {
            if (dissolveRenderer == null) return;
            // Get first: SpriteRenderer keeps its sprite texture in the same block
            dissolveRenderer.GetPropertyBlock(block);
            block.SetFloat(DissolveAmountId, amount);
            dissolveRenderer.SetPropertyBlock(block);
        }
    }
}
