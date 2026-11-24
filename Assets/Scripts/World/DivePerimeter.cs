using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Samples the wet swimable perimeter (soft-boundary ring), not a single nearshore pocket.
    /// Respects Maldives curved shoreline via DiveWorldConfig.ShorelineZ / OffshoreDistance.
    /// </summary>
    public static class DivePerimeter
    {
        /// <summary>Even angle around the soft boundary, 0..1 → radians via 2π.</summary>
        public static Vector2 RingXZ(float angle01, float radiusFrac = 0.92f)
        {
            float ang = angle01 * Mathf.PI * 2f;
            float rad = DiveWorldConfig.SoftBoundaryRadius * Mathf.Clamp(radiusFrac, 0.05f, 1.05f);
            return new Vector2(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad);
        }

        /// <summary>
        /// Floor hit on the soft-boundary ring. Skips dry beach hinterland by nudging into water.
        /// </summary>
        public static bool TryFloorOnRing(float angle01, float radiusFrac, out Vector3 floorPoint, out float depth)
        {
            var xz = RingXZ(angle01, radiusFrac);
            return TryFloorAt(xz.x, xz.y, out floorPoint, out depth);
        }

        public static bool TryFloorAt(float x, float z, out Vector3 floorPoint, out float depth)
        {
            floorPoint = default;
            depth = 0f;

            // Dry pad / hinterland relative to the curved bay waterline.
            float offshore = DiveWorldConfig.OffshoreDistance(x, z);
            if (offshore < 12f)
                z = DiveWorldConfig.ShorelineZ(x) + 18f + Mathf.Max(0f, -offshore) * 0.05f;

            for (int attempt = 0; attempt < 6; attempt++)
            {
                if (!Physics.Raycast(
                        new Vector3(x, DiveWorldConfig.WaterSurfaceY - 0.4f, z),
                        Vector3.down, out var hit, 140f))
                {
                    z += 35f;
                    continue;
                }

                depth = DiveWorldConfig.WaterSurfaceY - hit.point.y;
                if (depth >= 0.75f)
                {
                    floorPoint = hit.point;
                    return true;
                }

                // Still on wet sand / berm — push further seaward (+Z) past the curve.
                z = DiveWorldConfig.ShorelineZ(x) + 28f + attempt * 22f;
            }

            return false;
        }

        /// <summary>Random wet point on a perimeter band (full 360°, not one cove).</summary>
        public static bool TrySampleRing(System.Random rng, float minFrac, float maxFrac, out Vector3 floorPoint, out float depth)
        {
            float a = (float)rng.NextDouble();
            float frac = Mathf.Lerp(minFrac, maxFrac, (float)rng.NextDouble());
            return TryFloorOnRing(a, frac, out floorPoint, out depth);
        }

        /// <summary>True when XZ is clearly seaward of the curved waterline.</summary>
        public static bool IsWetEnough(float x, float z, float minOffshore = 16f)
        {
            return DiveWorldConfig.OffshoreDistance(x, z) >= minOffshore;
        }
    }
}
