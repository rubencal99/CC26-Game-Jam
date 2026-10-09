using System.Collections.Generic;
using UnityEngine;

namespace CC26
{
    // Beam along the muzzle's right (red) axis. Stops at the first solid hit, robot bodies included. Breaks the active robot unless it is immune.
    public class Laser : MonoBehaviour, IHazard
    {
        [Tooltip("Draws the beam. Positions are set in code, world space.")]
        [SerializeField] private LineRenderer beam;
        [Tooltip("Optional. Beam start; fires along its red axis. Defaults to this object.")]
        [SerializeField] private Transform muzzle;
        [Tooltip("Max beam length (units).")]
        [SerializeField] private float maxDistance = 30f;
        [Tooltip("Layers that stop the beam. Must include the Robot layer.")]
        [SerializeField] private LayerMask blockingLayers = ~0;
        [Tooltip("Optional. Moved to the beam end and shown only when the beam hits something, e.g. sparks.")]
        [SerializeField] private Transform impact;

        [Header("Audio")]
        [Tooltip("Optional looping AudioSource on the emitter, e.g. a hum. Plays while the laser is enabled.")]
        [SerializeField] private AudioSource ambientSound;
        [Tooltip("Optional looping spatial AudioSource, e.g. sparks. Moved to the beam end; plays only while the beam hits something.")]
        [SerializeField] private AudioSource impactSound;

        private ContactFilter2D filter;
        private readonly List<RaycastHit2D> hits = new();
        private float length;
        private bool hasHit;

        private void Awake()
        {
            if (muzzle == null) muzzle = transform;
            filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(blockingLayers);
            beam.useWorldSpace = true;
            beam.positionCount = 2;
        }

        private void OnEnable()
        {
            if (ambientSound != null) ambientSound.Play();
        }

        private void OnDisable()
        {
            if (ambientSound != null) ambientSound.Stop();
            if (impactSound != null) impactSound.Stop();
        }

        public void Apply(Robot robot)
        {
            robot.Break(HazardType.Laser);
        }

        private void FixedUpdate()
        {
            int count = Physics2D.Raycast(muzzle.position, muzzle.right, filter, hits, maxDistance);
            length = maxDistance;
            Collider2D nearest = null;
            for (int i = 0; i < count; i++)
            {
                // The muzzle can sit inside the emitter's own collider
                if (hits[i].collider.transform.IsChildOf(transform)) continue;
                if (hits[i].distance >= length) continue;
                length = hits[i].distance;
                nearest = hits[i].collider;
            }
            hasHit = nearest != null;

            Rigidbody2D body = hasHit ? nearest.attachedRigidbody : null;
            if (body != null && body.TryGetComponent(out Robot robot) && robot.IsActive) Apply(robot);
        }

        // Drawn every frame from the last physics result, so it still shows while paused
        private void LateUpdate()
        {
            Vector3 start = muzzle.position;
            Vector3 end = start + muzzle.right * length;
            beam.SetPosition(0, start);
            beam.SetPosition(1, end);

            if (impact != null)
            {
                impact.position = end;
                if (impact.gameObject.activeSelf != hasHit) impact.gameObject.SetActive(hasHit);
            }

            if (impactSound != null)
            {
                impactSound.transform.position = end;
                if (hasHit && !impactSound.isPlaying) impactSound.Play();
                else if (!hasHit && impactSound.isPlaying) impactSound.Stop();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform from = muzzle != null ? muzzle : transform;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(from.position, from.position + from.right * maxDistance);
        }
    }
}
