using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Shared world extents, depth bands, and tuning for the swim prototype.
    /// World is OPEN — no rock walls. SoftRadius is an invisible soft limit only.
    /// </summary>
    public static class DiveWorldConfig
    {
        public const float WaterSurfaceY = 0f;
        public const float OceanFloorY = -80f;

        /// <summary>
        /// Visual / generation extent. Terrain continues as open ocean — no boundary rocks.
        /// </summary>
        public const float OceanRadius = 280f;

        /// <summary>
        /// Invisible soft push-back only (no visible walls). Fog should hide the fade.
        /// </summary>
        public const float SoftBoundaryRadius = 240f;

        public const float MaxDepthMeters = 80f;

        public const float ShallowMaxDepth = 18f;
        public const float MidMaxDepth = 45f;

        public const float PlayerSpawnX = 0f;
        public const float PlayerSpawnY = -6f;
        public const float PlayerSpawnZ = -12f;

        public static Vector3 PlayerSpawn => new Vector3(PlayerSpawnX, PlayerSpawnY, PlayerSpawnZ);

        public static DepthBand GetBand(float depthBelowSurface)
        {
            if (depthBelowSurface < ShallowMaxDepth)
                return DepthBand.Shallow;
            if (depthBelowSurface < MidMaxDepth)
                return DepthBand.Mid;
            return DepthBand.Deep;
        }
    }

    public enum DepthBand
    {
        Shallow,
        Mid,
        Deep
    }
}
