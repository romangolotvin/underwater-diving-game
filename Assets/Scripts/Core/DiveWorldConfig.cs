using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Active dive site + shared extents. Starting site is a Maldives-style
    /// curved bay: bright textured sand, clear blue walk-in, shops behind the waterline.
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

        /// <summary>World Z of the sandy waterline at bay center (x=0). Negative Z = beach hinterland.</summary>
        public const float BeachShoreZ = -900f;

        /// <summary>Half-width of the curved bay (m). Headland reefs sit near ± this.</summary>
        public const float ShoreArcHalfWidth = 92f;

        /// <summary>How far headlands push seaward relative to bay center (m).</summary>
        public const float ShoreArcBulge = 52f;

        /// <summary>Open swim channel half-width through the bay mouth (m).</summary>
        public const float ShoreChannelHalfWidth = 38f;

        /// <summary>Target shelf depth (m) at CoastalShelfDistance — soft ramp, no cliff.</summary>
        public const float CoastalShelfDepth = 20f;

        /// <summary>Offshore distance (m) where the floor reaches CoastalShelfDepth.</summary>
        public const float CoastalShelfDistance = 1500f;

        /// <summary>
        /// Gentle walk-in shallows (Turkey-like): reach ~5 m depth over this offshore distance.
        /// No abrupt drop at the waterline — depth grows gradually into turquoise.
        /// </summary>
        public const float NearshoreShallowDistance = 280f;

        // Beach waterline: standing on wet sand at bay center, facing +Z into turquoise water.
        // Y is capsule center; UnstickFromGeometry snaps feet onto the mesh after boot.
        public const float PlayerSpawnX = 0f;
        public const float PlayerSpawnY = 1.55f;
        public const float PlayerSpawnZ = BeachShoreZ - 6f;

        public static Vector3 PlayerSpawn => new Vector3(PlayerSpawnX, PlayerSpawnY, PlayerSpawnZ);

        public const int StartingCredits = 60;
        public const int MaxUpgradeLevel = 5;
        public const int UpgradePriceStep = 45;

        // Three beach shops in an arc behind spawn (dry sand).
        // Center faces the water straight on; left/right sit on the arc, angled inward.
        public static Vector3 GearMarketPos => new Vector3(0f, 1.15f, BeachShoreZ - 36f);
        public static Vector3 DecorShopPos => new Vector3(-18f, 1.15f, BeachShoreZ - 27f);
        public static Vector3 BeachSuppliesPos => new Vector3(18f, 1.15f, BeachShoreZ - 27f);

        public const float GearMarketYaw = 0f;
        public const float DecorShopYaw = 34f;
        public const float BeachSuppliesYaw = -34f;

        public const float ShopInteractRadius = 7.5f;

        public static bool IsOpenOcean => ActiveSite == DiveSite.OpenOcean;

        public static void SetActiveSite(DiveSite site) => ActiveSite = site;

        /// <summary>
        /// Curved bay waterline: center recessed inland, headlands bulge seaward (+Z).
        /// Walk +Z across this curve into the water.
        /// </summary>
        public static float ShorelineZ(float x)
        {
            float t = Mathf.Clamp(x / ShoreArcHalfWidth, -1.75f, 1.75f);
            return BeachShoreZ + ShoreArcBulge * (t * t);
        }

        /// <summary>Meters offshore of the curved waterline at this XZ (positive = toward open ocean).</summary>
        public static float OffshoreDistance(float x, float z) => z - ShorelineZ(x);

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
