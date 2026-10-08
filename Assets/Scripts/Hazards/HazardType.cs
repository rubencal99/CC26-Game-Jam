using System;

namespace CC26
{
    // Hazard kinds a robot can be immune to. Flags, so one robot can list several.
    [Flags]
    public enum HazardType
    {
        None = 0,
        Spikes = 1 << 0,
        Laser = 1 << 1,
    }
}
