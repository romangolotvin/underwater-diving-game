using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Zone flavour: nearshore seaweed, mid kelp, deep silt pillars. No perimeter walls.
    /// </summary>
    public static class DepthZoneVisuals
    {
        public static void Build(Transform parent)
        {
            var root = new GameObject("DepthZoneVisuals").transform;
            root.SetParent(parent, false);

            var seaweedMat = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(0.2f, 0.55f, 0.32f), 0.3f, 0.05f, new Vector2(1f, 2.5f));
            var kelpMat = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(0.15f, 0.45f, 0.28f), 0.25f, 0.05f, new Vector2(1f, 3f));
            var deepMat = ProceduralTextures.MakeLit(
                ProceduralTextures.SiltAlbedo, ProceduralTextures.SiltNormal,
                Color.white, 0.08f, 0.04f, new Vector2(2f, 2f));

            var rng = new System.Random(303);
            float shoreZ = DiveWorldConfig.BeachShoreZ;

            // Shallow seaweed beds just off the beach (visible on the first swim-in).
            for (int i = 0; i < 70; i++)
            {
                float x = (float)(rng.NextDouble() * 220.0 - 110.0);
                float z = shoreZ + 25f + (float)(rng.NextDouble() * 280.0);
                if (!Physics.Raycast(new Vector3(x, DiveWorldConfig.WaterSurfaceY - 0.5f, z), Vector3.down, out var hit, 40f))
                    continue;
                float depth = DiveWorldConfig.WaterSurfaceY - hit.point.y;
                if (depth < 1.0f || depth > DiveWorldConfig.ShallowMaxDepth * 0.85f)
                    continue;

                var weed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                weed.name = "Seaweed";
                Object.Destroy(weed.GetComponent<Collider>());
                weed.transform.SetParent(root, false);
                float h = Mathf.Lerp(0.8f, 2.4f, (float)rng.NextDouble());
                weed.transform.position = hit.point + Vector3.up * (h * 0.5f);
                weed.transform.localScale = new Vector3(0.08f, h * 0.5f, 0.08f);
                weed.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 10f);
                weed.GetComponent<MeshRenderer>().sharedMaterial = seaweedMat;
            }

            // Mid-depth kelp farther offshore on the soft shelf.
            int kelpCount = 48;
            for (int i = 0; i < kelpCount; i++)
            {
                float x = (float)(rng.NextDouble() * 400.0 - 200.0);
                float z = shoreZ + 350f + (float)(rng.NextDouble() * 900.0);
                if (!Physics.Raycast(new Vector3(x, DiveWorldConfig.WaterSurfaceY - 1f, z), Vector3.down, out var hit, 80f))
                    continue;
                float depth = DiveWorldConfig.WaterSurfaceY - hit.point.y;
                if (depth < DiveWorldConfig.ShallowMaxDepth * 0.6f || depth > DiveWorldConfig.MidMaxDepth - 2f)
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

            // Deep silt pillars / vents — far shelf / basins only.
            for (int i = 0; i < 24; i++)
            {
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float rad = 400f + Mathf.Sqrt((float)rng.NextDouble()) * (DiveWorldConfig.SoftBoundaryRadius * 0.55f);
                float x = Mathf.Cos(ang) * rad;
                float z = shoreZ + 1100f + Mathf.Sin(ang) * rad * 0.35f;
                if (!Physics.Raycast(new Vector3(x, DiveWorldConfig.WaterSurfaceY - 1f, z), Vector3.down, out var hit, 120f))
                    continue;
                float depth = DiveWorldConfig.WaterSurfaceY - hit.point.y;
                if (depth < DiveWorldConfig.CoastalShelfDepth + 2f)
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
