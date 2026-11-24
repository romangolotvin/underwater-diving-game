using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Active dive site + shared extents. Default site is Open Ocean (no shore).
    /// </summary>
    public static class DiveWorldConfig
    {
        public enum DiveSite
        {
            /// <summary>Standalone open ocean — spawn underwater, shops on submerged platforms.</summary>
            OpenOcean
            // Country sites are separate travel destinations (added elsewhere).
        }

        /// <summary>Current playable site. Default: open ocean (not a shore country).</summary>
        public static DiveSite ActiveSite { get; private set; } = DiveSite.OpenOcean;

        public const float WaterSurfaceY = 0f;
        public const float OceanFloorY = -80f;

        public const float OceanRadius = 280f;
        public const float SoftBoundaryRadius = 240f;

        public const float MaxDepthMeters = 80f;
        public const float ShallowMaxDepth = 18f;
        public const float MidMaxDepth = 45f;

        // Open Ocean: spawn ~9 m below the surface, mid-column near platforms.
        public const float PlayerSpawnX = 0f;
        public const float PlayerSpawnY = -9f;
        public const float PlayerSpawnZ = -14f;

        public static Vector3 PlayerSpawn => new Vector3(PlayerSpawnX, PlayerSpawnY, PlayerSpawnZ);

        public const int StartingCredits = 60;
        public const int MaxUpgradeLevel = 5;
        public const int UpgradePriceStep = 45;

        // Underwater shop platforms (deck tops ~ same depth as spawn).
        public static Vector3 GearMarketPos => new Vector3(12f, -8.5f, -4f);
        public static Vector3 DecorShopPos => new Vector3(-11f, -8.5f, -2f);

        public const float ShopInteractRadius = 7.5f;

        public static bool IsOpenOcean => ActiveSite == DiveSite.OpenOcean;

        public static void SetActiveSite(DiveSite site) => ActiveSite = site;

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

        public static string SiteLabel => ActiveSite switch
        {
            _ => "Open Ocean"
        };
    }

    public enum DepthBand
    {
        Shallow,
        Mid,
        Deep
    }
}
