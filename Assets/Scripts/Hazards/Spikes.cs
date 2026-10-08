using UnityEngine;

namespace CC26
{
    // Breaks any robot that touches it, unless immune.
    public class Spikes : MonoBehaviour, IHazard
    {
        public void Apply(Robot robot)
        {
            if (!robot.IsImmuneTo(HazardType.Spikes)) robot.Break();
        }
    }
}
