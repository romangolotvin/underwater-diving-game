using UnityEngine;

namespace UnderwaterDiving
{
    public class ArtifactSpawner : MonoBehaviour
    {
        public static ArtifactSpawner Create(Transform parent)
        {
            var go = new GameObject("Artifacts");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<ArtifactSpawner>();
            s.SpawnAll();
            return s;
        }

        void SpawnAll()
        {
            var rng = new System.Random(77);
            SpawnMany(ArtifactId.CoralCharm, 8, DepthBand.Shallow, rng);
            SpawnMany(ArtifactId.BronzeCompass, 6, DepthBand.Mid, rng);
            SpawnMany(ArtifactId.AbyssIdol, 4, DepthBand.Deep, rng);
        }

        void SpawnMany(ArtifactId id, int count, DepthBand band, System.Random rng)
        {
            FishAgent.BandRange(band, out float minD, out float maxD);

            for (int i = 0; i < count; i++)
            {
                // Spread props evenly around the swimable perimeter ring.
                float a = (i + 0.5f + (float)rng.NextDouble() * 0.2f) / count;
                // Offset each band so shallow/mid/deep don't stack on the same bearings.
                a = Mathf.Repeat(a + (band == DepthBand.Mid ? 0.33f : band == DepthBand.Deep ? 0.66f : 0f), 1f);
                float frac = band switch
                {
                    DepthBand.Shallow => Mathf.Lerp(0.84f, 0.96f, (float)rng.NextDouble()),
                    DepthBand.Mid => Mathf.Lerp(0.72f, 0.9f, (float)rng.NextDouble()),
                    _ => Mathf.Lerp(0.78f, 0.97f, (float)rng.NextDouble())
                };

                float x;
                float z;
                float y;
                if (DivePerimeter.TryFloorOnRing(a, frac, out var floor, out _))
                {
                    x = floor.x;
                    z = floor.z;
                    y = floor.y + 0.6f;
                }
                else
                {
                    var xz = DivePerimeter.RingXZ(a, frac);
                    x = xz.x;
                    z = Mathf.Max(xz.y, DiveWorldConfig.ShorelineZ(x) + 30f);
                    y = DiveWorldConfig.WaterSurfaceY - Mathf.Lerp(minD + 2f, maxD - 1f, (float)rng.NextDouble());
                    if (Physics.Raycast(new Vector3(x, DiveWorldConfig.WaterSurfaceY - 0.5f, z), Vector3.down, out var hit, 120f))
                        y = hit.point.y + 0.6f;
                }

                ArtifactPickup.Spawn(transform, id, new Vector3(x, y, z));
            }
        }
    }
}
