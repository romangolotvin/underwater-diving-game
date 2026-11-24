using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Soft caustic shimmer via sun/ambient modulation. Built-in RP friendly.
    /// </summary>
    public class CausticShimmer : MonoBehaviour
    {
        Light sun;
        float baseIntensity = 1f;

        public static CausticShimmer Create(Transform parent, Light sunLight)
        {
            var go = new GameObject("CausticShimmer");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<CausticShimmer>();
            c.sun = sunLight;
            if (sunLight != null)
                c.baseIntensity = sunLight.intensity;
            return c;
        }

        void LateUpdate()
        {
            var swim = SwimController.Instance;
            bool under = swim != null && swim.IsUnderwater;
            if (sun == null)
                return;

            if (!under)
                return;

            float depth = swim.CurrentDepth;
            float shallowFactor = 1f - Mathf.Clamp01(depth / DiveWorldConfig.MidMaxDepth);
            Vector3 p = swim.transform.position;
            float wave =
                Mathf.Sin(Time.time * 2.1f + p.x * 0.15f) * 0.5f +
                Mathf.Sin(Time.time * 3.4f + p.z * 0.21f) * 0.35f +
                Mathf.Sin(Time.time * 5.2f + (p.x + p.z) * 0.08f) * 0.15f;
            float shimmer = 1f + wave * 0.12f * shallowFactor;
            sun.intensity *= shimmer;

            // Cooler caustic tint near surface.
            Color amb = RenderSettings.ambientLight;
            amb *= 1f + wave * 0.06f * shallowFactor;
            RenderSettings.ambientLight = amb;
        }
    }
}
