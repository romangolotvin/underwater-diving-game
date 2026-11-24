using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Open Ocean: dense weighted fauna (fish, turtles, rays) up to OpenOceanFishBudget.
    /// Critical nearshore schools spawn immediately; the rest fills across frames for faster load.
    /// </summary>
    public class FishSchoolSpawner : MonoBehaviour
    {
        const int ImmediateBudget = 80;
        const int PerFrame = 36;
        const int GuaranteedTurtles = 28;
        const int GuaranteedEagleRays = 22;
        const int GuaranteedMantas = 14;

        public static FishSchoolSpawner Create(Transform parent)
        {
            var go = new GameObject("FishSchools");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<FishSchoolSpawner>();
            s.BeginSpawn();
            return s;
        }

        void BeginSpawn()
        {
            StartCoroutine(SpawnOpenOceanRoutine());
        }

        IEnumerator SpawnOpenOceanRoutine()
        {
            var rng = new System.Random();
            var pool = CreatureCatalog.OpenOceanSpecies();
            int totalWeight = 0;
            for (int i = 0; i < pool.Count; i++)
                totalWeight += CreatureCatalog.Get(pool[i]).SpawnWeight;

            if (totalWeight <= 0 || pool.Count == 0)
                yield break;

            // Guaranteed turtles + rays visible near the cliff / bay at once.
            for (int i = 0; i < GuaranteedTurtles; i++)
                SpawnOne(FishSpecies.SeaTurtle, rng, i / (float)GuaranteedTurtles, preferNearshore: true);
            for (int i = 0; i < GuaranteedEagleRays; i++)
                SpawnOne(FishSpecies.EagleRay, rng, i / (float)GuaranteedEagleRays, preferNearshore: true);
            for (int i = 0; i < GuaranteedMantas; i++)
                SpawnOne(FishSpecies.MantaRay, rng, i / (float)GuaranteedMantas, preferNearshore: false);

            int guaranteed = GuaranteedTurtles + GuaranteedEagleRays + GuaranteedMantas;
            int budget = Mathf.Max(0, CreatureCatalog.OpenOceanFishBudget - guaranteed);

            int spawned = 0;
            while (spawned < budget)
            {
                int batch = spawned < ImmediateBudget
                    ? Mathf.Min(ImmediateBudget - spawned, PerFrame)
                    : PerFrame;
                for (int n = 0; n < batch && spawned < budget; n++, spawned++)
                {
                    var species = PickWeighted(pool, totalWeight, rng);
                    float angle01 = (spawned + (float)rng.NextDouble()) / Mathf.Max(1, budget);
                    SpawnOne(species, rng, angle01, preferNearshore: spawned < ImmediateBudget && rng.NextDouble() < 0.45);
                }

                if (spawned < ImmediateBudget)
                    continue; // finish critical nearshore before yielding
                yield return null;
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

        void SpawnOne(FishSpecies species, System.Random rng, float angle01, bool preferNearshore = false)
        {
            var band = FishAgent.SpeciesBand(species);
            FishAgent.BandRange(band, out float minD, out float maxD);
            var body = CreatureCatalog.Get(species).Body;

            float x;
            float z;
            double roll = rng.NextDouble();

            bool nearshore = preferNearshore || (band == DepthBand.Shallow && roll < 0.4);
            if (nearshore)
            {
                float along = Mathf.Lerp(-DiveWorldConfig.ShoreArcHalfWidth * 1.15f,
                    DiveWorldConfig.ShoreArcHalfWidth * 1.15f, (float)rng.NextDouble());
                x = along + (float)(rng.NextDouble() * 20.0 - 10.0);
                // Along the 20 m cliff face and just past the lip.
                z = DiveWorldConfig.ShorelineZ(x) + 2f
                    + (float)(rng.NextDouble() * (DiveWorldConfig.CliffDropDistance + 18f));
            }
            else if (roll < 0.82)
            {
                float minFrac = body == CreatureBody.Eel || body == CreatureBody.Snake ? 0.7f : 0.82f;
                float maxFrac = 0.97f;
                float frac = Mathf.Lerp(minFrac, maxFrac, (float)rng.NextDouble());
                float a = Mathf.Repeat(angle01 + (float)(rng.NextDouble() * 0.02 - 0.01), 1f);
                var xz = DivePerimeter.RingXZ(a, frac);
                x = xz.x;
                z = xz.y;
                if (!DivePerimeter.IsWetEnough(x, z, 8f))
                    z = DiveWorldConfig.ShorelineZ(x) + 6f + (float)(rng.NextDouble() * 40.0);
            }
            else
            {
                float maxR = DiveWorldConfig.SoftBoundaryRadius * 0.72f;
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float rad = Mathf.Sqrt((float)rng.NextDouble()) * maxR;
                if (body == CreatureBody.Eel || body == CreatureBody.Snake)
                    rad = Mathf.Sqrt((float)rng.NextDouble()) * (maxR * 0.5f);
                x = Mathf.Cos(ang) * rad;
                z = Mathf.Sin(ang) * rad;
                if (!DivePerimeter.IsWetEnough(x, z, 8f))
                    z = DiveWorldConfig.ShorelineZ(x) + 8f + (float)(rng.NextDouble() * 60.0);
            }

            float depth = Mathf.Lerp(minD, maxD, (float)rng.NextDouble());
            float y = DiveWorldConfig.WaterSurfaceY - depth;

            if (Physics.Raycast(new Vector3(x, DiveWorldConfig.WaterSurfaceY - 0.5f, z), Vector3.down, out var hit, 40f))
            {
                float floorDepth = DiveWorldConfig.WaterSurfaceY - hit.point.y;
                if (floorDepth < 0.6f)
                {
                    z = DiveWorldConfig.ShorelineZ(x) + DiveWorldConfig.NearshoreShallowDistance + 2f;
                    if (Physics.Raycast(new Vector3(x, DiveWorldConfig.WaterSurfaceY - 0.5f, z), Vector3.down, out hit, 40f))
                        floorDepth = DiveWorldConfig.WaterSurfaceY - hit.point.y;
                }

                if (body == CreatureBody.Ray || body == CreatureBody.Crayfish || body == CreatureBody.Isopod)
                    y = hit.point.y + (body == CreatureBody.Ray ? 1.3f : 0.6f);
                else if (body == CreatureBody.Turtle)
                    y = Mathf.Lerp(hit.point.y + 2f, DiveWorldConfig.WaterSurfaceY - 1.5f, (float)rng.NextDouble());
                else if (body == CreatureBody.Eel || body == CreatureBody.Snake)
                    y = hit.point.y + 1.0f;
                else
                    y = Mathf.Clamp(y, hit.point.y + 1.2f, DiveWorldConfig.WaterSurfaceY - 0.8f);
            }

            FishAgent.Spawn(transform, species, new Vector3(x, y, z));
        }
    }
}
