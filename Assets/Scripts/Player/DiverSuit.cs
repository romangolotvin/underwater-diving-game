using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Dive suit stats: air, depth rating, swim boost, buoyancy assist, lamp.
    /// </summary>
    public class DiverSuit : MonoBehaviour
    {
        [SerializeField] float maxDiveSeconds = 90f;
        [SerializeField] float maxDepthMeters = 28f;
        [SerializeField] float swimSpeedMultiplier = 1f;
        [SerializeField] float buoyancyAssist;
        [SerializeField] float lampStrength;

        Light diveLamp;

        public float MaxDiveSeconds => maxDiveSeconds;
        public float MaxDepthMeters => maxDepthMeters;
        public float SwimSpeedMultiplier => swimSpeedMultiplier;
        public float BuoyancyAssist => buoyancyAssist;
        public float LampStrength => lampStrength;
        public float RemainingDiveSeconds { get; private set; }

        void Awake()
        {
            RemainingDiveSeconds = maxDiveSeconds;
        }

        public void ApplyUpgrade(float diveSeconds, float depthMeters, float speedMul, float buoyancy, float lamp)
        {
            maxDiveSeconds = diveSeconds;
            maxDepthMeters = depthMeters;
            swimSpeedMultiplier = speedMul;
            buoyancyAssist = buoyancy;
            lampStrength = lamp;
            RemainingDiveSeconds = Mathf.Min(RemainingDiveSeconds, maxDiveSeconds);
            EnsureLamp();
        }

        public void RefillAir() => RemainingDiveSeconds = maxDiveSeconds;

        public void DrainAir(float seconds)
        {
            RemainingDiveSeconds = Mathf.Max(0f, RemainingDiveSeconds - Mathf.Max(0f, seconds));
        }

        void EnsureLamp()
        {
            if (diveLamp == null)
            {
                var swim = GetComponent<SwimController>();
                Transform pivot = swim != null ? swim.CameraPivot : transform;
                var go = new GameObject("DiveLamp");
                go.transform.SetParent(pivot, false);
                go.transform.localPosition = new Vector3(0.12f, -0.05f, 0.2f);
                diveLamp = go.AddComponent<Light>();
                diveLamp.type = LightType.Spot;
                diveLamp.spotAngle = 55f;
                diveLamp.range = 28f;
                diveLamp.color = new Color(0.75f, 0.9f, 1f);
            }

            diveLamp.intensity = lampStrength > 0.01f ? 0.4f + lampStrength * 2.2f : 0f;
            diveLamp.enabled = lampStrength > 0.01f;
            diveLamp.range = 22f + lampStrength * 18f;
        }

        void Update()
        {
            var swim = SwimController.Instance;
            if (swim != null && swim.IsUnderwater)
            {
                float drain = 1f;
                if (swim.CurrentDepth > maxDepthMeters)
                    drain += 0.75f;
                // Deeper water drains air a bit faster without depth suit.
                if (swim.CurrentDepth > DiveWorldConfig.MidMaxDepth)
                    drain += 0.25f;
                RemainingDiveSeconds = Mathf.Max(0f, RemainingDiveSeconds - Time.deltaTime * drain);
            }
            else if (swim != null && !swim.IsUnderwater)
            {
                RemainingDiveSeconds = Mathf.MoveTowards(RemainingDiveSeconds, maxDiveSeconds, Time.deltaTime * 8f);
            }
        }
    }
}
