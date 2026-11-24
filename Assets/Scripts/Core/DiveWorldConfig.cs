using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Active dive site + shared extents. Starting site is a sunny beach walk-in
    /// with a soft shelf ramp; deep water sits ~1–1.5 km offshore.
    /// </summary>
    public static class DiveWorldConfig
    {
        public enum DiveSite
        {
            /// <summary>Starting coastal shelf — beach waterline spawn, calm walk-in, open water beyond.</summary>
            OpenOcean
            // Country sites are separate travel destinations (added elsewhere).
        }

        /// <summary>Current playable site. Default: coastal Open Ocean (beach start).</summary>
        public static DiveSite ActiveSite { get; private set; } = DiveSite.OpenOcean;

        public const float WaterSurfaceY = 0f;
        public const float OceanFloorY = -80f;

        // Large enough that ~1.5 km of calm shelf fits between shore and far water.
        public const float OceanRadius = 1600f;
        public const float SoftBoundaryRadius = 1480f;

        public const float MaxDepthMeters = 80f;
        public const float ShallowMaxDepth = 18f;
        public const float MidMaxDepth = 45f;

        /// <summary>World Z of the sandy waterline (negative Z = beach hinterland).</summary>
        public const float BeachShoreZ = -900f;

        /// <summary>Target shelf depth (m) at ~1.5 km offshore — soft ramp, no cliff.</summary>
        public const float CoastalShelfDepth = 20f;

        /// <summary>Offshore distance (m) where the floor reaches CoastalShelfDepth.</summary>
        public const float CoastalShelfDistance = 1500f;

        // Beach waterline: standing on wet sand, facing +Z into calm open water.
        public const float PlayerSpawnX = 0f;
        public const float PlayerSpawnY = 1.25f;
        public const float PlayerSpawnZ = BeachShoreZ - 8f;

        public static Vector3 PlayerSpawn => new Vector3(PlayerSpawnX, PlayerSpawnY, PlayerSpawnZ);

        public const int StartingCredits = 60;
        public const int MaxUpgradeLevel = 5;
        public const int UpgradePriceStep = 45;

        // Nearshore underwater platforms (reachable after a short walk-in swim).
        public static Vector3 GearMarketPos => new Vector3(42f, -5.5f, BeachShoreZ + 220f);
        public static Vector3 DecorShopPos => new Vector3(-38f, -5.5f, BeachShoreZ + 195f);

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
            _ => "Sunny Shore"
        };
    }

    public enum DepthBand
    {
        Shallow,
        Mid,
        Deep
    }
}
