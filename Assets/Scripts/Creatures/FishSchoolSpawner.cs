using UnityEngine;

namespace UnderwaterDiving
{
    public class FishSchoolSpawner : MonoBehaviour
    {
        public static FishSchoolSpawner Create(Transform parent)
        {
            var go = new GameObject("FishSchools");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<FishSchoolSpawner>();
            s.SpawnAll();
            return s;
        }

        void SpawnAll()
        {
            var rng = new System.Random(42);
            SpawnBand(FishSpecies.ReefChromis, 26, rng);
            SpawnBand(FishSpecies.AmberJack, 14, rng);
            SpawnBand(FishSpecies.AbyssGrouper, 9, rng);
            SpawnBand(FishSpecies.MorayEel, 10, rng);
            SpawnBand(FishSpecies.SeaTurtle, 6, rng);
            SpawnBand(FishSpecies.EagleRay, 8, rng);
            SpawnBand(FishSpecies.Barracuda, 7, rng);
        }

        void SpawnBand(FishSpecies species, int count, System.Random rng)
        {
            var band = FishAgent.SpeciesBand(species);
            FishAgent.BandRange(band, out float minD, out float maxD);
            float maxR = DiveWorldConfig.SoftBoundaryRadius * 0.7f;

            for (int i = 0; i < count; i++)
            {
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float rad = Mathf.Sqrt((float)rng.NextDouble()) * maxR;
                // Morays prefer nearer reef / spawn shallows.
                if (species == FishSpecies.MorayEel)
                    rad = Mathf.Sqrt((float)rng.NextDouble()) * (maxR * 0.45f);

                float x = Mathf.Cos(ang) * rad;
                float z = Mathf.Sin(ang) * rad;
                float depth = Mathf.Lerp(minD + 1.5f, maxD - 1.5f, (float)rng.NextDouble());
                float y = DiveWorldConfig.WaterSurfaceY - depth;

                if (Physics.Raycast(new Vector3(x, DiveWorldConfig.WaterSurfaceY - 1f, z), Vector3.down, out var hit, 120f))
                {
                    if (species == FishSpecies.MorayEel || species == FishSpecies.EagleRay)
                        y = hit.point.y + (species == FishSpecies.MorayEel ? 0.9f : 1.3f);
                    else
                        y = Mathf.Max(y, hit.point.y + 1.4f);
                }

                FishAgent.Spawn(transform, species, new Vector3(x, y, z));
            }
        }
    }
}
