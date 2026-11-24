using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Zone flavour: dense bay seaweed plus seaweed/kelp/pillars on the full swimable perimeter.
    /// </summary>
    public static class DepthZoneVisuals
    {
        public static void Build(Transform parent)
        {
            var root = new GameObject("DepthZoneVisuals").transform;
            root.SetParent(parent, false);

            var seaweedMat = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(0.18f, 0.72f, 0.38f), 0.34f, 0.05f, new Vector2(1f, 2.5f));
            var kelpMat = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(0.12f, 0.58f, 0.32f), 0.28f, 0.05f, new Vector2(1f, 3f));
            var deepMat = ProceduralTextures.MakeLit(
                ProceduralTextures.SiltAlbedo, ProceduralTextures.SiltNormal,
                Color.white, 0.08f, 0.04f, new Vector2(2f, 2f));

            var rng = new System.Random(303);

            // Dense seaweed beds immediately past the curved waterline (short bay entrance).
            for (int i = 0; i < 95; i++)
            {
                float x = (float)(rng.NextDouble() * 170.0 - 85.0);
                float z = DiveWorldConfig.ShorelineZ(x) + 6f + (float)(rng.NextDouble() * 95.0);
                if (!Physics.Raycast(new Vector3(x, DiveWorldConfig.WaterSurfaceY - 0.5f, z), Vector3.down, out var hit, 40f))
                    continue;
                float depth = DiveWorldConfig.WaterSurfaceY - hit.point.y;
                if (depth < 0.8f || depth > DiveWorldConfig.ShallowMaxDepth * 0.85f)
                    continue;

                SpawnWeed(root, hit.point, seaweedMat, rng, 0.8f, 2.4f, 0.08f);
            }

            // Extra seaweed around the entire soft-boundary ring (not one cove).
            const int ringWeed = 120;
            for (int i = 0; i < ringWeed; i++)
            {
                float a = (i + (float)rng.NextDouble()) / ringWeed;
                float frac = 0.8f + (float)(rng.NextDouble() * 0.16);
                if (!DivePerimeter.TryFloorOnRing(a, frac, out var floor, out float depth))
                    continue;
                if (depth < 1.0f || depth > DiveWorldConfig.ShallowMaxDepth * 0.9f)
                    continue;
                SpawnWeed(root, floor, seaweedMat, rng, 0.8f, 2.4f, 0.08f);
            }

            // Mid-depth kelp on a mid/outer perimeter band (full 360°).
            const int kelpCount = 72;
            for (int i = 0; i < kelpCount; i++)
            {
                float a = (i + 0.37f) / kelpCount;
                float frac = 0.55f + (float)(rng.NextDouble() * 0.3);
                if (!DivePerimeter.TryFloorOnRing(a, frac, out var floor, out float depth))
                    continue;
                if (depth < DiveWorldConfig.ShallowMaxDepth * 0.55f || depth > DiveWorldConfig.MidMaxDepth - 1.5f)
                    continue;

                var kelp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                kelp.name = "Kelp";
                Object.Destroy(kelp.GetComponent<Collider>());
                kelp.transform.SetParent(root, false);
                float h = Mathf.Lerp(2.5f, 7f, (float)rng.NextDouble());
                kelp.transform.position = floor + Vector3.up * (h * 0.5f);
                kelp.transform.localScale = new Vector3(0.12f, h * 0.5f, 0.12f);
                kelp.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 8f);
                kelp.GetComponent<MeshRenderer>().sharedMaterial = kelpMat;
            }

            // Deep silt pillars on the outer swimable ring / basins.
            const int pillarCount = 36;
            for (int i = 0; i < pillarCount; i++)
            {
                float a = (i + 0.12f) / pillarCount;
                float frac = 0.72f + (float)(rng.NextDouble() * 0.22);
                if (!DivePerimeter.TryFloorOnRing(a, frac, out var floor, out float depth))
                    continue;
                if (depth < DiveWorldConfig.CoastalShelfDepth + 1.5f)
                    continue;

                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = "DeepPillar";
                Object.Destroy(pillar.GetComponent<Collider>());
                pillar.transform.SetParent(root, false);
                float h = Mathf.Lerp(1.5f, 5f, (float)rng.NextDouble());
                float r = Mathf.Lerp(0.5f, 1.6f, (float)rng.NextDouble());
                pillar.transform.position = floor + Vector3.up * (h * 0.45f);
                pillar.transform.localScale = new Vector3(r, h * 0.5f, r);
                pillar.GetComponent<MeshRenderer>().sharedMaterial = deepMat;
            }
        }

        static void SpawnWeed(Transform root, Vector3 floor, Material mat, System.Random rng, float hMin, float hMax, float rad)
        {
            var weed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            weed.name = "Seaweed";
            Object.Destroy(weed.GetComponent<Collider>());
            weed.transform.SetParent(root, false);
            float h = Mathf.Lerp(hMin, hMax, (float)rng.NextDouble());
            weed.transform.position = floor + Vector3.up * (h * 0.5f);
            weed.transform.localScale = new Vector3(rad, h * 0.5f, rad);
            weed.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 10f);
            weed.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}
