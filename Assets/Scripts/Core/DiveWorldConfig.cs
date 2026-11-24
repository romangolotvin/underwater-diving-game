using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Shared world extents, depth bands, economy, and tuning.
    /// World is OPEN — no rock walls. SoftRadius is an invisible soft limit only.
    /// </summary>
    public static class DiveWorldConfig
    {
        public const float WaterSurfaceY = 0f;
        public const float OceanFloorY = -80f;

        public const float OceanRadius = 280f;
        public const float SoftBoundaryRadius = 240f;

        public const float MaxDepthMeters = 80f;
        public const float ShallowMaxDepth = 18f;
        public const float MidMaxDepth = 45f;

        public const float PlayerSpawnX = 0f;
        public const float PlayerSpawnY = -6f;
        public const float PlayerSpawnZ = -12f;

        public static Vector3 PlayerSpawn => new Vector3(PlayerSpawnX, PlayerSpawnY, PlayerSpawnZ);

        public const int StartingCredits = 60;
        public const int MaxUpgradeLevel = 5;
        public const int UpgradePriceStep = 45;

        public static Vector3 GearMarketPos => new Vector3(10f, 0.35f, -6f);
        public static Vector3 DecorShopPos => new Vector3(-9f, 0.35f, -4f);

        public const float ShopInteractRadius = 6.5f;

        public static DepthBand GetBand(float depthBelowSurface)
        {
            if (depthBelowSurface < ShallowMaxDepth)
                return DepthBand.Shallow;
            if (depthBelowSurface < MidMaxDepth)
                return DepthBand.Mid;
            return DepthBand.Deep;
        }

        public static string BandLabel(DepthBand band) => band switch
        {
            DepthBand.Shallow => "Shallow",
            DepthBand.Mid => "Twilight",
            _ => "Abyss"
        };
    }

    public enum DepthBand
    {
        Shallow,
        Mid,
        Deep
    }
}
