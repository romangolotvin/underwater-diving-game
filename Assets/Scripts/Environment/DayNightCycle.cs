using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Continuous day/night lighting. No countdown / clock UI — atmosphere only.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Instance { get; private set; }

        Light sun;
        /// <summary>0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset.</summary>
        public float DayPhase { get; private set; } = 0.42f;

        /// <summary>1 at full day, 0 at deep night.</summary>
        public float DayAmount { get; private set; } = 1f;

        // Slow natural drift — not exposed as a timer readout.
        const float PhaseSeconds = 480f;

        public static DayNightCycle Create(Transform parent, Light sunLight)
        {
            var go = new GameObject("DayNightCycle");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<DayNightCycle>();
            c.sun = sunLight;
            return c;
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            DayPhase = Mathf.Repeat(DayPhase + Time.deltaTime / PhaseSeconds, 1f);
            // Smooth day curve: bright around noon, dark around midnight.
            float angle = DayPhase * Mathf.PI * 2f;
            DayAmount = Mathf.Clamp01(Mathf.Sin(angle - Mathf.PI * 0.5f) * 0.5f + 0.5f);
            // Soften nights so shore + shallows stay readable (Maldives bright bias).
            DayAmount = Mathf.Lerp(0.28f, 1f, DayAmount);

            if (sun != null)
            {
                float elev = Mathf.Lerp(-8f, 68f, DayAmount);
                float azim = Mathf.Lerp(200f, -20f, DayPhase);
                sun.transform.rotation = Quaternion.Euler(elev, azim, 0f);
                // Warm daylight sun against a blue sky (not turquoise fill light).
                sun.color = Color.Lerp(new Color(0.42f, 0.52f, 0.85f), new Color(1f, 0.98f, 0.92f), DayAmount);
            }

            var sky = RenderSettings.skybox;
            if (sky != null)
            {
                if (sky.HasProperty("_Exposure"))
                    sky.SetFloat("_Exposure", Mathf.Lerp(0.3f, 1.35f, DayAmount));
                if (sky.HasProperty("_SkyTint"))
                {
                    // Keep sky blue day and night; only dim via exposure.
                    Color daySky = new Color(0.28f, 0.48f, 0.95f);
                    Color nightSky = new Color(0.12f, 0.18f, 0.45f);
                    sky.SetColor("_SkyTint", Color.Lerp(nightSky, daySky, DayAmount));
                }
            }
        }
    }
}
