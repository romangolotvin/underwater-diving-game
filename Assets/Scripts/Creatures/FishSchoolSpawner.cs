using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Spawns Open Ocean fauna by depth band from CreatureCatalog.
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
            var rng = new System.Random(42);

            // Shallow / upper column
            Spawn(FishSpecies.ReefChromis, 18, rng);
            Spawn(FishSpecies.Clownfish, 10, rng);
            Spawn(FishSpecies.Parrotfish, 8, rng);
            Spawn(FishSpecies.Angelfish, 8, rng);
            Spawn(FishSpecies.Butterflyfish, 8, rng);
            Spawn(FishSpecies.Needlefish, 6, rng);
            Spawn(FishSpecies.Seahorse, 5, rng);
            Spawn(FishSpecies.MorayEel, 7, rng);
            Spawn(FishSpecies.SeaTurtle, 5, rng);
            Spawn(FishSpecies.Lionfish, 4, rng);
            Spawn(FishSpecies.Pufferfish, 5, rng);

            // Mid
            Spawn(FishSpecies.AmberJack, 10, rng);
            Spawn(FishSpecies.Mackerel, 12, rng);
            Spawn(FishSpecies.Tuna, 6, rng);
            Spawn(FishSpecies.Barracuda, 6, rng);
            Spawn(FishSpecies.EagleRay, 7, rng);
            Spawn(FishSpecies.MantaRay, 3, rng);
            Spawn(FishSpecies.Squid, 6, rng);
            Spawn(FishSpecies.Cuttlefish, 4, rng);
            Spawn(FishSpecies.Jellyfish, 8, rng);
            Spawn(FishSpecies.Dolphin, 4, rng);
            Spawn(FishSpecies.Octopus, 4, rng);
            Spawn(FishSpecies.Marlin, 3, rng);

            // Sharks
            Spawn(FishSpecies.ReefShark, 4, rng);
            Spawn(FishSpecies.Hammerhead, 3, rng);
            Spawn(FishSpecies.TigerShark, 2, rng);
            Spawn(FishSpecies.ThresherShark, 2, rng);
            Spawn(FishSpecies.WhaleShark, 1, rng);
            Spawn(FishSpecies.GreatWhite, 1, rng);

            // Deep
            Spawn(FishSpecies.AbyssGrouper, 7, rng);
            Spawn(FishSpecies.Lanternfish, 10, rng);
            Spawn(FishSpecies.GulperEel, 4, rng);
            Spawn(FishSpecies.GiantIsopod, 3, rng);
            Spawn(FishSpecies.VampireSquid, 3, rng);
            Spawn(FishSpecies.Anglerfish, 3, rng);
            Spawn(FishSpecies.SeaSnake, 4, rng);
            Spawn(FishSpecies.WaterSnake, 3, rng);
        }

        void Spawn(FishSpecies species, int count, System.Random rng)
        {
            var band = FishAgent.SpeciesBand(species);
            FishAgent.BandRange(band, out float minD, out float maxD);
            float maxR = DiveWorldConfig.SoftBoundaryRadius * 0.7f;
            var body = CreatureCatalog.Get(species).Body;

            for (int i = 0; i < count; i++)
            {
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
}
