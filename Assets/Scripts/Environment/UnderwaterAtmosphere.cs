using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Depth-based fog/light plus day/night tint. Distance fog doubles as soft world edge.
    /// Surface Maldives: blue sky ambient, clear shallow water, long shore vista.
    /// </summary>
    public class UnderwaterAtmosphere : MonoBehaviour
    {
        [SerializeField] Light sun;

        public static UnderwaterAtmosphere Create(Transform parent, Light sun)
        {
            var go = new GameObject("UnderwaterAtmosphere");
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<UnderwaterAtmosphere>();
            a.sun = sun;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            return a;
        }

        void LateUpdate()
        {
            var swim = SwimController.Instance;
            float depth = swim != null ? swim.CurrentDepth : 0f;
            bool under = swim != null && swim.IsUnderwater;
            float day = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayAmount : 1f;

            float edgeFog = 0f;
            if (swim != null)
            {
                float r = new Vector2(swim.transform.position.x, swim.transform.position.z).magnitude;
                edgeFog = Mathf.InverseLerp(DiveWorldConfig.SoftBoundaryRadius * 0.7f, DiveWorldConfig.SoftBoundaryRadius, r);
            }

            Color fog;
            float density;
            Color ambient;
            float sunMul;

            if (!under)
            {
                // Blue sky haze (not turquoise) so the beach reads under a clear noon sky.
                Color dayFog = new Color(0.52f, 0.70f, 0.92f);
                Color nightFog = new Color(0.08f, 0.12f, 0.24f);
                fog = Color.Lerp(nightFog, dayFog, day);
                density = Mathf.Lerp(0.0012f, 0.009f, edgeFog);
                ambient = Color.Lerp(new Color(0.14f, 0.18f, 0.32f), new Color(0.62f, 0.74f, 0.92f), day);
                sunMul = Mathf.Lerp(0.2f, 1.45f, day);
            }
            else
            {
                // 20 m cliff shelf stays bright/clear — no dark mid/abyss fog pockets on the drop.
                float t = Mathf.Clamp01(depth / Mathf.Max(1f, DiveWorldConfig.CoastalShelfDepth));
                fog = Color.Lerp(new Color(0.48f, 0.78f, 0.88f), new Color(0.32f, 0.62f, 0.78f), t);
                density = Mathf.Lerp(0.006f, 0.014f, t);
                ambient = Color.Lerp(new Color(0.48f, 0.68f, 0.78f), new Color(0.34f, 0.55f, 0.68f), t);
                sunMul = Mathf.Lerp(1.2f, 0.85f, t);

                // Night darkens water gently without crushing the cliff into a black zone.
                fog = Color.Lerp(fog * 0.55f, fog, Mathf.Lerp(0.6f, 1f, day));
                ambient *= Mathf.Lerp(0.5f, 1f, day);
                sunMul *= Mathf.Lerp(0.3f, 1f, day);
                density = Mathf.Lerp(density, density + 0.02f, edgeFog);
            }

            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = density;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;

            if (sun != null)
            {
                Color daySun = new Color(1f, 0.98f, 0.92f);
                Color nightSun = new Color(0.4f, 0.5f, 0.82f);
                Color baseSun = Color.Lerp(nightSun, daySun, day);
                // Mild cool shift underwater — keep it blue, not turquoise green.
                sun.color = Color.Lerp(baseSun, new Color(0.55f, 0.72f, 0.92f), under ? 0.35f : 0f);
                sun.intensity = Mathf.Lerp(sun.intensity, sunMul, Time.deltaTime * 2.5f);
            }

            var sky = RenderSettings.skybox;
            if (sky != null && sky.HasProperty("_Exposure"))
                sky.SetFloat("_Exposure", Mathf.Lerp(0.35f, 1.3f, day));

            var cam = Camera.main;
            if (cam != null)
            {
                // Above water: procedural blue sky + sun. Underwater: fog clear color.
                if (!under && RenderSettings.skybox != null)
                {
                    cam.clearFlags = CameraClearFlags.Skybox;
                    cam.backgroundColor = fog;
                }
                else
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = fog;
                }
                // Keep long beach vista above water; tighten only when submerged / near edge.
                if (!under)
                    cam.farClipPlane = Mathf.Lerp(2800f, 900f, edgeFog);
                else
                    cam.farClipPlane = Mathf.Lerp(420f, 160f, edgeFog);
            }
        }
    }
}
