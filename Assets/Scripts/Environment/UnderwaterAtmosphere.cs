using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Depth-based fog/light plus day/night tint. Distance fog doubles as soft world edge.
    /// Surface Maldives: bright ambient, turquoise shallow fog, long shore vista.
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
                // Bright tropical shore air.
                Color dayFog = new Color(0.62f, 0.82f, 0.95f);
                Color nightFog = new Color(0.1f, 0.14f, 0.26f);
                fog = Color.Lerp(nightFog, dayFog, day);
                density = Mathf.Lerp(0.0018f, 0.01f, edgeFog);
                ambient = Color.Lerp(new Color(0.16f, 0.2f, 0.32f), new Color(0.78f, 0.86f, 0.94f), day);
                sunMul = Mathf.Lerp(0.2f, 1.55f, day);
            }
            else
            {
                var band = DiveWorldConfig.GetBand(depth);
                switch (band)
                {
                    case DepthBand.Shallow:
                        // Turquoise Maldives shallows.
                        fog = Color.Lerp(new Color(0.18f, 0.72f, 0.78f), new Color(0.08f, 0.42f, 0.55f), depth / DiveWorldConfig.ShallowMaxDepth);
                        density = Mathf.Lerp(0.016f, 0.032f, depth / DiveWorldConfig.ShallowMaxDepth);
                        ambient = new Color(0.28f, 0.58f, 0.62f);
                        sunMul = 1.05f;
                        break;
                    case DepthBand.Mid:
                        fog = Color.Lerp(new Color(0.05f, 0.22f, 0.36f), new Color(0.03f, 0.1f, 0.2f),
                            Mathf.InverseLerp(DiveWorldConfig.ShallowMaxDepth, DiveWorldConfig.MidMaxDepth, depth));
                        density = Mathf.Lerp(0.04f, 0.055f,
                            Mathf.InverseLerp(DiveWorldConfig.ShallowMaxDepth, DiveWorldConfig.MidMaxDepth, depth));
                        ambient = new Color(0.08f, 0.16f, 0.26f);
                        sunMul = 0.42f;
                        break;
                    default:
                        fog = new Color(0.02f, 0.05f, 0.1f);
                        density = 0.065f;
                        ambient = new Color(0.03f, 0.05f, 0.09f);
                        sunMul = 0.14f;
                        break;
                }

                // Night darkens upper water more than the abyss.
                fog = Color.Lerp(fog * 0.45f, fog, Mathf.Lerp(0.55f, 1f, day));
                ambient *= Mathf.Lerp(0.45f, 1f, day);
                sunMul *= Mathf.Lerp(0.25f, 1f, day);
                density = Mathf.Lerp(density, density + 0.04f, edgeFog);
            }

            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = density;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;

            if (sun != null)
            {
                Color daySun = new Color(1f, 0.98f, 0.9f);
                Color nightSun = new Color(0.35f, 0.45f, 0.75f);
                Color baseSun = Color.Lerp(nightSun, daySun, day);
                sun.color = Color.Lerp(baseSun, new Color(0.45f, 0.7f, 0.85f), under ? 0.45f : 0f);
                sun.intensity = Mathf.Lerp(sun.intensity, sunMul, Time.deltaTime * 2.5f);
            }

            var cam = Camera.main;
            if (cam != null)
            {
                cam.backgroundColor = fog;
                // Keep long beach vista above water; tighten only when submerged / near edge.
                if (!under)
                    cam.farClipPlane = Mathf.Lerp(2800f, 900f, edgeFog);
                else
                    cam.farClipPlane = Mathf.Lerp(420f, 160f, edgeFog);
            }
        }
    }
}
