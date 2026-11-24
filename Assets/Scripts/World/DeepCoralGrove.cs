using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Open Ocean only: towering coral that rises from great depth, with nestled mini-shops.
    /// Does not move player spawn or the main platform markets.
    /// </summary>
    public class DeepCoralGrove : MonoBehaviour
    {
        public static DeepCoralGrove Create(Transform parent)
        {
            if (!DiveWorldConfig.IsOpenOcean)
                return null;

            var go = new GameObject("DeepCoralGrove");
            go.transform.SetParent(parent, false);
            var grove = go.AddComponent<DeepCoralGrove>();
            grove.Build();
            return grove;
        }

        void Build()
        {
            var rng = new System.Random(9081);
            var coralMatA = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(0.75f, 0.35f, 0.4f), 0.35f, 0.05f, new Vector2(1.4f, 3f));
            var coralMatB = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(0.35f, 0.55f, 0.7f), 0.3f, 0.05f, new Vector2(1.2f, 2.5f));
            var coralMatC = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(0.85f, 0.55f, 0.25f), 0.4f, 0.08f, new Vector2(1.6f, 2.8f));

            // Far-shelf clusters (well past the beach ramp / nearshore shops).
            float far = DiveWorldConfig.BeachShoreZ + 1100f;
            Vector2[] hubs =
            {
                new Vector2(55f, far + 40f),
                new Vector2(-60f, far + 35f),
                new Vector2(45f, far - 70f),
                new Vector2(-40f, far - 55f),
                new Vector2(75f, far - 20f),
                new Vector2(-70f, far + 70f)
            };

            int shopIndex = 0;
            for (int h = 0; h < hubs.Length; h++)
            {
                var hub = hubs[h];
                int towers = 8 + rng.Next(0, 5);
                Vector3 shopAnchor = Vector3.zero;
                bool placedShopSpot = false;

                for (int t = 0; t < towers; t++)
                {
                    float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                    float rad = 4f + (float)rng.NextDouble() * 18f;
                    float x = hub.x + Mathf.Cos(ang) * rad;
                    float z = hub.y + Mathf.Sin(ang) * rad;

                    if (!Physics.Raycast(new Vector3(x, DiveWorldConfig.WaterSurfaceY - 1f, z), Vector3.down, out var hit, 130f))
                        continue;

                    // Prefer deep floor — skip if too shallow.
                    float depth = DiveWorldConfig.WaterSurfaceY - hit.point.y;
                    if (depth < DiveWorldConfig.MidMaxDepth * 0.85f)
                        continue;

                    float height = Mathf.Lerp(12f, 38f, (float)rng.NextDouble());
                    // Cap so towers reach mid-water but stay below the surface with room.
                    float topY = Mathf.Min(hit.point.y + height, DiveWorldConfig.WaterSurfaceY - 6f);
                    height = topY - hit.point.y;
                    if (height < 8f)
                        continue;

                    var mat = (t % 3) switch
                    {
                        0 => coralMatA,
                        1 => coralMatB,
                        _ => coralMatC
                    };

                    BuildTower(transform, hit.point, height, mat, rng);

                    if (!placedShopSpot && t >= towers / 2)
                    {
                        // Shelf for a mini-shop roughly mid-tower height among the grove.
                        float shelfY = hit.point.y + height * 0.45f;
                        shopAnchor = new Vector3(x, shelfY, z);
                        placedShopSpot = true;
                    }
                }

                if (placedShopSpot)
                {
                    CoralDiveShop.Create(transform, shopAnchor, shopIndex++, rng);
                }
            }
        }

        static void BuildTower(Transform parent, Vector3 basePos, float height, Material mat, System.Random rng)
        {
            var root = new GameObject("DeepCoralTower");
            root.transform.SetParent(parent, false);

            int segments = Mathf.Clamp(Mathf.RoundToInt(height / 4.5f), 3, 10);
            float segH = height / segments;
            float radius = Mathf.Lerp(1.8f, 0.55f, 0.15f);

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                radius = Mathf.Lerp(2.1f, 0.45f, t) * Mathf.Lerp(0.85f, 1.2f, (float)rng.NextDouble());
                var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                col.name = "Stem" + i;
                Object.Destroy(col.GetComponent<Collider>());
                col.transform.SetParent(root.transform, false);
                float y = basePos.y + segH * (i + 0.5f);
                float wobble = (float)(rng.NextDouble() * 0.8 - 0.4);
                col.transform.position = new Vector3(basePos.x + wobble, y, basePos.z + wobble * 0.6f);
                col.transform.localScale = new Vector3(radius, segH * 0.52f, radius);
                col.transform.rotation = Quaternion.Euler(
                    (float)rng.NextDouble() * 8f - 4f,
                    (float)rng.NextDouble() * 360f,
                    (float)rng.NextDouble() * 8f - 4f);
                col.GetComponent<MeshRenderer>().sharedMaterial = mat;

                // Branch cups
                if (i > 1 && rng.NextDouble() > 0.45)
                {
                    var branch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    branch.name = "Cup";
                    Object.Destroy(branch.GetComponent<Collider>());
                    branch.transform.SetParent(root.transform, false);
                    float bAng = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                    branch.transform.position = new Vector3(
                        col.transform.position.x + Mathf.Cos(bAng) * radius * 0.9f,
                        y + segH * 0.2f,
                        col.transform.position.z + Mathf.Sin(bAng) * radius * 0.9f);
                    float bs = Mathf.Lerp(0.6f, 1.6f, (float)rng.NextDouble());
                    branch.transform.localScale = new Vector3(bs, bs * 0.55f, bs);
                    branch.GetComponent<MeshRenderer>().sharedMaterial = mat;
                }
            }
        }
    }
}
