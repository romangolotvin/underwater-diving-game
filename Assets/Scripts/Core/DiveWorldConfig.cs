using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Shared world extents and tuning used across systems.
    /// </summary>
    public static class DiveWorldConfig
    {
        public const float WaterSurfaceY = 0f;
        public const float OceanFloorY = -80f;
        public const float OceanRadius = 220f;

        public const float MaxDepthMeters = 80f;
    }
}
