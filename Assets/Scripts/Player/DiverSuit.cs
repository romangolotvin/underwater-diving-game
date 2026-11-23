using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Dive suit stats (air time, depth rating, swim boost). Expanded by the upgrade system.
    /// </summary>
    public class DiverSuit : MonoBehaviour
    {
        [SerializeField] float maxDiveSeconds = 90f;
        [SerializeField] float maxDepthMeters = 25f;
        [SerializeField] float swimSpeedMultiplier = 1f;

        public float MaxDiveSeconds => maxDiveSeconds;
        public float MaxDepthMeters => maxDepthMeters;
        public float SwimSpeedMultiplier => swimSpeedMultiplier;
        public float RemainingDiveSeconds { get; private set; }

        void Awake()
        {
            RemainingDiveSeconds = maxDiveSeconds;
        }

        public void ApplyUpgrade(float diveSeconds, float depthMeters, float speedMul)
        {
            maxDiveSeconds = diveSeconds;
            maxDepthMeters = depthMeters;
            swimSpeedMultiplier = speedMul;
            RemainingDiveSeconds = Mathf.Min(RemainingDiveSeconds, maxDiveSeconds);
        }

        public void RefillAir()
        {
            RemainingDiveSeconds = maxDiveSeconds;
        }

        void Update()
        {
            var swim = SwimController.Instance;
            if (swim != null && swim.IsUnderwater)
            {
                RemainingDiveSeconds = Mathf.Max(0f, RemainingDiveSeconds - Time.deltaTime);
            }
            else if (swim != null && !swim.IsUnderwater)
            {
                RemainingDiveSeconds = Mathf.MoveTowards(RemainingDiveSeconds, maxDiveSeconds, Time.deltaTime * 8f);
            }
        }
    }
}
