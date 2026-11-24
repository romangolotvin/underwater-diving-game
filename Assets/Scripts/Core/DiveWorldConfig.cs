using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Active dive site + shared extents. Starting site is a Maldives-style
    /// curved bay: golden sand, clear walk-in, then a smooth 1:1 cliff to 20 m.
    /// </summary>
    public static class DiveWorldConfig
    {
        public enum DiveSite
        {
            /// <summary>Starting coastal shelf — beach waterline spawn, 20 m cliff, open water beyond.</summary>
            OpenOcean
            // Country sites are separate travel destinations (added elsewhere).
        }

        /// <summary>Current playable site. Default: coastal Open Ocean (beach start).</summary>
        public static DiveSite ActiveSite { get; private set; } = DiveSite.OpenOcean;

        public const float WaterSurfaceY = 0f;
        /// <summary>Absolute floor clamp — shelf rests at −CoastalShelfDepth, not a 70–80 m abyss.</summary>
        public const float OceanFloorY = -22f;

        // Large enough that the bay + soft boundary feel open from the beach.
        public const float OceanRadius = 1600f;
        public const float SoftBoundaryRadius = 1480f;

        public const float MaxDepthMeters = 22f;
        /// <summary>Entire 20 m shelf stays in the bright shallow fog band (no dark cliff pocket).</summary>
        public const float ShallowMaxDepth = 22f;
        public const float MidMaxDepth = 22f;

        /// <summary>World Z of the sandy waterline at bay center (x=0). Negative Z = beach hinterland.</summary>
        public const float BeachShoreZ = -900f;

        /// <summary>Half-width of the curved bay (m). Headland reefs sit near ± this.</summary>
        public const float ShoreArcHalfWidth = 92f;

        /// <summary>How far headlands push seaward relative to bay center (m).</summary>
        public const float ShoreArcBulge = 52f;

        /// <summary>Open swim channel half-width through the bay mouth (m).</summary>
        public const float ShoreChannelHalfWidth = 38f;

        /// <summary>Target shelf depth (m) after the cliff run — was a 70 m drop; now 20 m.</summary>
        public const float CoastalShelfDepth = 20f;

        /// <summary>
        /// Horizontal distance (m) of the cliff face. Matched to CoastalShelfDepth for a smooth 1:1
        /// decline (1 m down per 1 m forward) — not the old stepped terrace then sudden 70 m pit.
        /// </summary>
        public const float CoastalShelfDistance = 20f;

        /// <summary>Alias: cliff run length equals shelf depth for 1:1 slope.</summary>
        public const float CliffDropDistance = CoastalShelfDepth;

        /// <summary>
        /// Short ankle-depth lip before the 1:1 cliff (meters offshore). Keeps the waterline walkable.
        /// </summary>
        public const float NearshoreShallowDistance = 4f;

        // Beach waterline: standing on wet sand at bay center, facing +Z into turquoise water.
        // Y is capsule center (height 1.8 → feet at Y-0.9). Kept well above dry-sand mesh (~0–0.3)
        // so boot never embeds; UnstickFromGeometry / ClearNearbyTrapColliders finish the job.
        public const float PlayerSpawnX = 0f;
        public const float PlayerSpawnY = 2.6f;
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
