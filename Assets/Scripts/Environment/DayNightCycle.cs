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
        public float DayPhase { get; private set; } = 0.35f;

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
            // Soften nights so underwater stays readable.
            DayAmount = Mathf.Lerp(0.18f, 1f, DayAmount);

            if (sun != null)
            {
                float elev = Mathf.Lerp(-12f, 62f, DayAmount);
                float azim = Mathf.Lerp(200f, -20f, DayPhase);
                sun.transform.rotation = Quaternion.Euler(elev, azim, 0f);
                sun.color = Color.Lerp(new Color(0.35f, 0.45f, 0.75f), new Color(1f, 0.96f, 0.88f), DayAmount);
            }
        }
    }
}
