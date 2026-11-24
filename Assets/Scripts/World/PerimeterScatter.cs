using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Structures, props, and sky clouds along the entire swimable soft-boundary perimeter.
    /// </summary>
    public static class PerimeterScatter
    {
        public static void Build(Transform parent)
        {
            var root = new GameObject("PerimeterScatter").transform;
            root.SetParent(parent, false);

            var rockMat = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                Color.white, 0.14f, 0.08f, new Vector2(2.2f, 2.2f));
            var coralMat = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(1f, 0.55f, 0.45f), 0.38f, 0.06f, new Vector2(1.3f, 1.3f));
            var coralMatB = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(0.35f, 0.7f, 0.85f), 0.34f, 0.05f, new Vector2(1.1f, 1.1f));
            var woodMat = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                new Color(0.55f, 0.38f, 0.22f), 0.2f, 0.12f, new Vector2(1.5f, 1.5f));
            var cloudMat = ProceduralTextures.MakeLit(
                ProceduralTextures.SandAlbedo, ProceduralTextures.SandNormal,
                new Color(1f, 1f, 1f), 0.02f, 0.85f, new Vector2(2f, 2f));

            var structures = new GameObject("Structures").transform;
            structures.SetParent(root, false);
            var props = new GameObject("Props").transform;
            props.SetParent(root, false);
            var clouds = new GameObject("SkyClouds").transform;
            clouds.SetParent(root, false);

            var rng = new System.Random(20261124);

            // Landmark structures evenly around the full swimable ring.
            const int structureCount = 36;
            for (int i = 0; i < structureCount; i++)
            {
                float a = (i + 0.5f) / structureCount;
                float frac = 0.86f + (float)(rng.NextDouble() * 0.1);
                if (!DivePerimeter.TryFloorOnRing(a, frac, out var floor, out float depth))
                    continue;

                int kind = rng.Next(0, 4);
                if (kind == 0)
                    SpawnReefTower(structures, floor, depth, rockMat, coralMat, rng);
                else if (kind == 1)
                    SpawnArch(structures, floor, depth, rockMat, rng);
                else if (kind == 2)
                    SpawnBuoyMarker(structures, floor, woodMat, coralMatB, rng);
                else
                    SpawnRockStack(structures, floor, depth, rockMat, rng);
            }

            // Dense prop scatter on the same ring (coral heads, barrels, rubble).
            const int propCount = 96;
            for (int i = 0; i < propCount; i++)
            {
                float a = (i + (float)rng.NextDouble()) / propCount;
                float frac = 0.8f + (float)(rng.NextDouble() * 0.16);
                if (!DivePerimeter.TryFloorOnRing(a, frac, out var floor, out float depth))
                    continue;
                if (depth < 1.0f)
                    continue;

                if (rng.NextDouble() < 0.55)
                    SpawnCoralProp(props, floor, depth, rng.NextDouble() < 0.5 ? coralMat : coralMatB, rng);
                else if (rng.NextDouble() < 0.5)
                    SpawnBarrel(props, floor, woodMat, rng);
                else
                    SpawnRubble(props, floor, rockMat, rng);
            }

            // Horizon cloud puffs — full 360° so no empty sky quadrant.
            const int cloudCount = 48;
            float cloudR = DiveWorldConfig.SoftBoundaryRadius * 1.05f;
            for (int i = 0; i < cloudCount; i++)
            {
                float a = (i + (float)rng.NextDouble() * 0.35f) / cloudCount;
                float ang = a * Mathf.PI * 2f;
                float rad = cloudR * Mathf.Lerp(0.92f, 1.18f, (float)rng.NextDouble());
                float x = Mathf.Cos(ang) * rad;
                float z = Mathf.Sin(ang) * rad;
                float y = Mathf.Lerp(95f, 210f, (float)rng.NextDouble());

                var cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cloud.name = "Cloud";
                Object.Destroy(cloud.GetComponent<Collider>());
                cloud.transform.SetParent(clouds, false);
                cloud.transform.position = new Vector3(x, y, z);
                float sx = Mathf.Lerp(28f, 70f, (float)rng.NextDouble());
                float sy = Mathf.Lerp(8f, 18f, (float)rng.NextDouble());
                float sz = Mathf.Lerp(18f, 48f, (float)rng.NextDouble());
                cloud.transform.localScale = new Vector3(sx, sy, sz);
                cloud.GetComponent<MeshRenderer>().sharedMaterial = cloudMat;
            }
        }

        static void SpawnReefTower(Transform parent, Vector3 floor, float depth, Material rock, Material coral, System.Random rng)
        {
            float h = Mathf.Lerp(3.5f, Mathf.Min(14f, depth * 0.65f + 4f), (float)rng.NextDouble());
            float r = Mathf.Lerp(1.2f, 3.2f, (float)rng.NextDouble());
            var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "ReefTower";
            Object.Destroy(tower.GetComponent<Collider>());
            tower.transform.SetParent(parent, false);
            tower.transform.position = floor + Vector3.up * (h * 0.45f);
            tower.transform.localScale = new Vector3(r, h * 0.5f, r);
            tower.GetComponent<MeshRenderer>().sharedMaterial = rock;

            var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "ReefCap";
            Object.Destroy(cap.GetComponent<Collider>());
            cap.transform.SetParent(parent, false);
            cap.transform.position = floor + Vector3.up * h;
            cap.transform.localScale = Vector3.one * (r * Mathf.Lerp(1.1f, 1.8f, (float)rng.NextDouble()));
            cap.GetComponent<MeshRenderer>().sharedMaterial = coral;
        }

        static void SpawnArch(Transform parent, Vector3 floor, float depth, Material rock, System.Random rng)
        {
            float span = Mathf.Lerp(4f, 9f, (float)rng.NextDouble());
            float h = Mathf.Lerp(3f, Mathf.Min(10f, depth * 0.5f + 3f), (float)rng.NextDouble());
            float yaw = (float)rng.NextDouble() * 360f;
            var rot = Quaternion.Euler(0f, yaw, 0f);

            for (int leg = -1; leg <= 1; leg += 2)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "ArchLeg";
                Object.Destroy(pillar.GetComponent<Collider>());
                pillar.transform.SetParent(parent, false);
                pillar.transform.position = floor + rot * new Vector3(leg * span * 0.5f, h * 0.45f, 0f);
                pillar.transform.rotation = rot;
                pillar.transform.localScale = new Vector3(
                    Mathf.Lerp(1.1f, 2f, (float)rng.NextDouble()),
                    h,
                    Mathf.Lerp(1.1f, 2f, (float)rng.NextDouble()));
                pillar.GetComponent<MeshRenderer>().sharedMaterial = rock;
            }

            var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintel.name = "ArchLintel";
            Object.Destroy(lintel.GetComponent<Collider>());
            lintel.transform.SetParent(parent, false);
            lintel.transform.position = floor + Vector3.up * h;
            lintel.transform.rotation = rot;
            lintel.transform.localScale = new Vector3(span + 2f, Mathf.Lerp(0.8f, 1.6f, (float)rng.NextDouble()), 2.2f);
            lintel.GetComponent<MeshRenderer>().sharedMaterial = rock;
        }

        static void SpawnBuoyMarker(Transform parent, Vector3 floor, Material wood, Material accent, System.Random rng)
        {
            float h = Mathf.Lerp(4f, 9f, (float)rng.NextDouble());
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "BuoyPole";
            Object.Destroy(pole.GetComponent<Collider>());
            pole.transform.SetParent(parent, false);
            pole.transform.position = floor + Vector3.up * (h * 0.5f);
            pole.transform.localScale = new Vector3(0.18f, h * 0.5f, 0.18f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = wood;

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "BuoyBall";
            Object.Destroy(ball.GetComponent<Collider>());
            ball.transform.SetParent(parent, false);
            ball.transform.position = floor + Vector3.up * h;
            ball.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.4f, (float)rng.NextDouble());
            ball.GetComponent<MeshRenderer>().sharedMaterial = accent;
        }

        static void SpawnRockStack(Transform parent, Vector3 floor, float depth, Material rock, System.Random rng)
        {
            int n = 2 + rng.Next(0, 3);
            float y = 0f;
            for (int i = 0; i < n; i++)
            {
                float s = Mathf.Lerp(1.2f, 3.5f, (float)rng.NextDouble()) * (1f - i * 0.15f);
                var rockGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rockGo.name = "StackRock";
                Object.Destroy(rockGo.GetComponent<Collider>());
                rockGo.transform.SetParent(parent, false);
                rockGo.transform.position = floor + Vector3.up * (y + s * 0.35f);
                rockGo.transform.localScale = new Vector3(s, s * 0.55f, s * Mathf.Lerp(0.7f, 1.2f, (float)rng.NextDouble()));
                rockGo.transform.rotation = Quaternion.Euler(
                    (float)rng.NextDouble() * 25f,
                    (float)rng.NextDouble() * 360f,
                    (float)rng.NextDouble() * 25f);
                rockGo.GetComponent<MeshRenderer>().sharedMaterial = rock;
                y += s * 0.45f;
                if (y > depth * 0.7f)
                    break;
            }
        }

        static void SpawnCoralProp(Transform parent, Vector3 floor, float depth, Material mat, System.Random rng)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            c.name = "PerimeterCoral";
            Object.Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(parent, false);
            float h = Mathf.Lerp(0.5f, Mathf.Min(2.8f, depth * 0.35f), (float)rng.NextDouble());
            float r = Mathf.Lerp(0.2f, 0.7f, (float)rng.NextDouble());
            c.transform.position = floor + Vector3.up * (h * 0.5f);
            c.transform.localScale = new Vector3(r, h * 0.5f, r);
            c.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 14f);
            c.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static void SpawnBarrel(Transform parent, Vector3 floor, Material mat, System.Random rng)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            b.name = "Barrel";
            Object.Destroy(b.GetComponent<Collider>());
            b.transform.SetParent(parent, false);
            b.transform.position = floor + Vector3.up * 0.45f;
            b.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);
            b.transform.rotation = Quaternion.Euler(
                (float)rng.NextDouble() * 20f,
                (float)rng.NextDouble() * 360f,
                85f + (float)rng.NextDouble() * 20f);
            b.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static void SpawnRubble(Transform parent, Vector3 floor, Material mat, System.Random rng)
        {
            var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.name = "Rubble";
            Object.Destroy(r.GetComponent<Collider>());
            r.transform.SetParent(parent, false);
            float s = Mathf.Lerp(0.4f, 1.6f, (float)rng.NextDouble());
            r.transform.position = floor + Vector3.up * (s * 0.25f);
            r.transform.localScale = new Vector3(s, s * 0.4f, s * 0.8f);
            r.transform.rotation = Quaternion.Euler(
                (float)rng.NextDouble() * 40f,
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 40f);
            r.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}
