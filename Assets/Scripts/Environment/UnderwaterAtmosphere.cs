using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Depth-based fog/light. Distance fog doubles as the soft world edge (no rock walls).
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

            // Soft edge: thicken fog when near invisible boundary so the open ocean fades out.
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
                fog = new Color(0.55f, 0.72f, 0.88f);
                density = Mathf.Lerp(0.0035f, 0.012f, edgeFog);
                ambient = new Color(0.58f, 0.65f, 0.72f);
                sunMul = 1.2f;
            }
            else
            {
                var band = DiveWorldConfig.GetBand(depth);
                switch (band)
                {
                    case DepthBand.Shallow:
                        fog = Color.Lerp(new Color(0.12f, 0.48f, 0.58f), new Color(0.06f, 0.28f, 0.4f), depth / DiveWorldConfig.ShallowMaxDepth);
                        density = Mathf.Lerp(0.022f, 0.038f, depth / DiveWorldConfig.ShallowMaxDepth);
                        ambient = new Color(0.18f, 0.42f, 0.48f);
                        sunMul = 0.85f;
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
                density = Mathf.Lerp(density, density + 0.04f, edgeFog);
            }

            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = density;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;

            if (sun != null)
            {
                sun.color = Color.Lerp(new Color(1f, 0.96f, 0.88f), new Color(0.45f, 0.6f, 0.85f), under ? 0.85f : 0f);
                sun.intensity = Mathf.Lerp(sun.intensity, sunMul, Time.deltaTime * 2.5f);
            }

            var cam = Camera.main;
            if (cam != null)
            {
                cam.backgroundColor = fog;
                cam.farClipPlane = Mathf.Lerp(220f, 140f, edgeFog);
            }
        }
    }
}
