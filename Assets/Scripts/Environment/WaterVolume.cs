using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Marks the water body. Provides submersion queries for buoyancy and visuals.
    /// </summary>
    public class WaterVolume : MonoBehaviour
    {
        public static WaterVolume Instance { get; private set; }

        [SerializeField] float surfaceY = DiveWorldConfig.WaterSurfaceY;
        [SerializeField] float floorY = DiveWorldConfig.OceanFloorY;
        [SerializeField] float density = 1.02f;

        public float SurfaceY => surfaceY;
        public float FloorY => floorY;
        public float Density => density;

        void Awake()
        {
            Instance = this;
            surfaceY = DiveWorldConfig.WaterSurfaceY;
            floorY = DiveWorldConfig.OceanFloorY;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool IsPointUnderwater(Vector3 worldPoint) => worldPoint.y < surfaceY;

        /// <summary>
        /// Returns 0 (fully above) to 1 (fully submerged) for a vertical capsule.
        /// </summary>
        public float GetSubmersion(Vector3 center, float height)
        {
            float half = height * 0.5f;
            float top = center.y + half;
            float bottom = center.y - half;

            if (bottom >= surfaceY)
                return 0f;
            if (top <= surfaceY)
                return 1f;

            return Mathf.Clamp01((surfaceY - bottom) / Mathf.Max(0.01f, height));
        }

        public float DepthBelowSurface(Vector3 worldPoint) => Mathf.Max(0f, surfaceY - worldPoint.y);
    }
}
