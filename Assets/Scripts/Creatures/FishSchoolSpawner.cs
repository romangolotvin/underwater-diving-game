using System.Collections.Generic;
using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Open Ocean: weighted random fish picks up to CreatureCatalog.OpenOceanFishBudget (207).
    /// </summary>
    public class FishSchoolSpawner : MonoBehaviour
    {
        public static FishSchoolSpawner Create(Transform parent)
        {
            var go = new GameObject("FishSchools");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<FishSchoolSpawner>();
            s.SpawnOpenOcean();
            return s;
        }

        void SpawnOpenOcean()
        {
            // Non-deterministic each run (no fixed seed 42).
            var rng = new System.Random();
            var pool = CreatureCatalog.OpenOceanSpecies();
            int totalWeight = 0;
            for (int i = 0; i < pool.Count; i++)
                totalWeight += CreatureCatalog.Get(pool[i]).SpawnWeight;

            if (totalWeight <= 0 || pool.Count == 0)
                return;

            int budget = CreatureCatalog.OpenOceanFishBudget;
            for (int n = 0; n < budget; n++)
            {
                var species = PickWeighted(pool, totalWeight, rng);
                SpawnOne(species, rng);
            }
        }

        static FishSpecies PickWeighted(List<FishSpecies> pool, int totalWeight, System.Random rng)
        {
            int roll = rng.Next(0, totalWeight);
            int acc = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                acc += CreatureCatalog.Get(pool[i]).SpawnWeight;
                if (roll < acc)
                    return pool[i];
            }
            return pool[pool.Count - 1];
        }

        void SpawnOne(FishSpecies species, System.Random rng)
        {
            var band = FishAgent.SpeciesBand(species);
            FishAgent.BandRange(band, out float minD, out float maxD);
            var body = CreatureCatalog.Get(species).Body;
            float shoreZ = DiveWorldConfig.BeachShoreZ;

            float x;
            float z;
            // Bias shallow fauna into the nearshore reef band so the beach walk-in feels alive.
            if (band == DepthBand.Shallow && rng.NextDouble() < 0.65)
            {
                x = (float)(rng.NextDouble() * 260.0 - 130.0);
                z = shoreZ + 35f + (float)(rng.NextDouble() * 380.0);
            }
            else
            {
                float maxR = DiveWorldConfig.SoftBoundaryRadius * 0.7f;
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float rad = Mathf.Sqrt((float)rng.NextDouble()) * maxR;
                if (body == CreatureBody.Eel || body == CreatureBody.Snake)
                    rad = Mathf.Sqrt((float)rng.NextDouble()) * (maxR * 0.5f);

                x = Mathf.Cos(ang) * rad;
                z = Mathf.Sin(ang) * rad;
                // Never place fish on the dry beach pad.
                if (z < shoreZ + 20f)
                    z = shoreZ + 20f + (float)(rng.NextDouble() * 200.0);
            }

            float depth = Mathf.Lerp(minD + 1.5f, maxD - 1.5f, (float)rng.NextDouble());
            float y = DiveWorldConfig.WaterSurfaceY - depth;

            if (Physics.Raycast(new Vector3(x, DiveWorldConfig.WaterSurfaceY - 1f, z), Vector3.down, out var hit, 120f))
            {
                float floorDepth = DiveWorldConfig.WaterSurfaceY - hit.point.y;
                if (floorDepth < 0.8f)
                {
                    // Reject land hits — nudge offshore into water.
                    z = Mathf.Max(z, shoreZ + 40f);
                    if (Physics.Raycast(new Vector3(x, DiveWorldConfig.WaterSurfaceY - 1f, z), Vector3.down, out hit, 120f))
                        floorDepth = DiveWorldConfig.WaterSurfaceY - hit.point.y;
                }

                if (body == CreatureBody.Ray || body == CreatureBody.Crayfish || body == CreatureBody.Isopod)
                    y = hit.point.y + (body == CreatureBody.Ray ? 1.3f : 0.6f);
                else if (body == CreatureBody.Eel || body == CreatureBody.Snake)
                    y = hit.point.y + 1.0f;
                else
                    y = Mathf.Max(y, hit.point.y + 1.5f);
            }

            FishAgent.Spawn(transform, species, new Vector3(x, y, z));
        }
    }
}
