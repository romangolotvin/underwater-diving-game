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
            float maxR = DiveWorldConfig.SoftBoundaryRadius * 0.65f;

            for (int i = 0; i < count; i++)
            {
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float rad = Mathf.Sqrt((float)rng.NextDouble()) * maxR;
                float x = Mathf.Cos(ang) * rad;
                float z = Mathf.Sin(ang) * rad;
                if (z < DiveWorldConfig.BeachShoreZ + 30f)
                    z = DiveWorldConfig.BeachShoreZ + 30f + (float)(rng.NextDouble() * 250.0);

                float y = DiveWorldConfig.WaterSurfaceY - Mathf.Lerp(minD + 2f, maxD - 1f, (float)rng.NextDouble());
                if (Physics.Raycast(new Vector3(x, DiveWorldConfig.WaterSurfaceY - 0.5f, z), Vector3.down, out var hit, 120f))
                    y = hit.point.y + 0.6f;

                ArtifactPickup.Spawn(transform, id, new Vector3(x, y, z));
            }
        }
    }
}
