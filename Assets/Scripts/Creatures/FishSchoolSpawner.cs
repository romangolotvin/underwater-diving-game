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
            float maxR = DiveWorldConfig.SoftBoundaryRadius * 0.7f;
            var body = CreatureCatalog.Get(species).Body;

            float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float rad = Mathf.Sqrt((float)rng.NextDouble()) * maxR;
            if (body == CreatureBody.Eel || body == CreatureBody.Snake)
                rad = Mathf.Sqrt((float)rng.NextDouble()) * (maxR * 0.5f);

            float x = Mathf.Cos(ang) * rad;
            float z = Mathf.Sin(ang) * rad;
            float depth = Mathf.Lerp(minD + 1.5f, maxD - 1.5f, (float)rng.NextDouble());
            float y = DiveWorldConfig.WaterSurfaceY - depth;

            if (Physics.Raycast(new Vector3(x, DiveWorldConfig.WaterSurfaceY - 1f, z), Vector3.down, out var hit, 120f))
            {
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
