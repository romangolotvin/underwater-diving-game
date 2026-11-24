using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Extra zone flavour: mid kelp, deep silt pillars. No coral in deep. No perimeter walls.
    /// </summary>
    public static class DepthZoneVisuals
    {
        public static void Build(Transform parent)
        {
            var root = new GameObject("DepthZoneVisuals").transform;
            root.SetParent(parent, false);

            var kelpMat = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(0.15f, 0.45f, 0.28f), 0.25f, 0.05f, new Vector2(1f, 3f));
            var deepMat = ProceduralTextures.MakeLit(
                ProceduralTextures.SiltAlbedo, ProceduralTextures.SiltNormal,
                Color.white, 0.08f, 0.04f, new Vector2(2f, 2f));

            var rng = new System.Random(303);
            float maxR = DiveWorldConfig.SoftBoundaryRadius * 0.7f;

            // Mid-depth kelp strands in open water (no beach coral). Fewer in pure Open Ocean.
            int kelpCount = DiveWorldConfig.IsOpenOcean ? 28 : 55;
            for (int i = 0; i < kelpCount; i++)
            {
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float rad = Mathf.Sqrt((float)rng.NextDouble()) * maxR;
                float x = Mathf.Cos(ang) * rad;
                float z = Mathf.Sin(ang) * rad;
                if (!Physics.Raycast(new Vector3(x, -1f, z), Vector3.down, out var hit, 120f))
                    continue;
                float depth = DiveWorldConfig.WaterSurfaceY - hit.point.y;
                if (depth < DiveWorldConfig.ShallowMaxDepth + 2f || depth > DiveWorldConfig.MidMaxDepth - 2f)
                    continue;

                var kelp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                kelp.name = "Kelp";
                Object.Destroy(kelp.GetComponent<Collider>());
                kelp.transform.SetParent(root, false);
                float h = Mathf.Lerp(2.5f, 7f, (float)rng.NextDouble());
                kelp.transform.position = hit.point + Vector3.up * (h * 0.5f);
                kelp.transform.localScale = new Vector3(0.12f, h * 0.5f, 0.12f);
                kelp.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 8f);
                kelp.GetComponent<MeshRenderer>().sharedMaterial = kelpMat;
            }

            // Deep silt pillars / vents — larger, darker, no coral.
            for (int i = 0; i < 24; i++)
            {
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float rad = Mathf.Sqrt((float)rng.NextDouble()) * maxR;
                float x = Mathf.Cos(ang) * rad;
                float z = Mathf.Sin(ang) * rad;
                if (!Physics.Raycast(new Vector3(x, -1f, z), Vector3.down, out var hit, 120f))
                    continue;
                float depth = DiveWorldConfig.WaterSurfaceY - hit.point.y;
                if (depth < DiveWorldConfig.MidMaxDepth + 1f)
                    continue;

                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = "DeepPillar";
                Object.Destroy(pillar.GetComponent<Collider>());
                pillar.transform.SetParent(root, false);
                float h = Mathf.Lerp(1.5f, 5f, (float)rng.NextDouble());
                float r = Mathf.Lerp(0.5f, 1.6f, (float)rng.NextDouble());
                pillar.transform.position = hit.point + Vector3.up * (h * 0.45f);
                pillar.transform.localScale = new Vector3(r, h * 0.5f, r);
                pillar.GetComponent<MeshRenderer>().sharedMaterial = deepMat;
            }
        }
    }
}
