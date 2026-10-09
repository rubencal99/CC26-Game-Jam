using UnityEngine;

namespace CC26
{
    // Kills particles buried inside a collider, e.g. gore resting where a door snaps closed or a crate respawns on reset.
    // Without this the collision module fights to push them out and they jitter.
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleOverlapCleanup : MonoBehaviour
    {
        [Tooltip("How deep a particle must be inside a collider to be removed (units). Keeps particles resting on a surface.")]
        [SerializeField] private float minDepth = 0.05f;

        private ParticleSystem ps;
        private ParticleSystem.Particle[] particles;
        private ContactFilter2D filter;
        private readonly Collider2D[] hit = new Collider2D[1];

        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            particles = new ParticleSystem.Particle[ps.main.maxParticles];
        }

        private void LateUpdate()
        {
            if (ps.particleCount == 0) return;

            // Same layers the particles collide with, so it only removes particles the collision module would fight over.
            // Triggers skipped: gore inside a button's press zone is fine.
            filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(ps.collision.collidesWith);
            bool local = ps.main.simulationSpace == ParticleSystemSimulationSpace.Local;
            int count = ps.GetParticles(particles);
            bool changed = false;

            for (int i = 0; i < count; i++)
            {
                Vector2 p = local ? transform.TransformPoint(particles[i].position) : particles[i].position;
                if (!IsBuried(p)) continue;
                particles[i].remainingLifetime = 0f;
                changed = true;
            }

            if (changed) ps.SetParticles(particles, count);
        }

        // Center plus four inset points all inside means buried, not just touching a surface
        private bool IsBuried(Vector2 p)
        {
            return Inside(p)
                && Inside(p + Vector2.up * minDepth)
                && Inside(p + Vector2.down * minDepth)
                && Inside(p + Vector2.left * minDepth)
                && Inside(p + Vector2.right * minDepth);
        }

        private bool Inside(Vector2 p) => Physics2D.OverlapPoint(p, filter, hit) > 0;
    }
}
