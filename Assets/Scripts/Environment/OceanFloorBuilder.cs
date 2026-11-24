using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Maldives-style coastal seabed: curved bay shoreline, gentle Turkey-like walk-in,
    /// organic headland reef/rock barriers on the flanks, soft shelf farther offshore.
    /// </summary>
    public static class OceanFloorBuilder
    {
        struct Landmark
        {
            public Vector2 A, B;
            public float Radius, TopY, Overhang, Waist;
            public Landmark(float ax, float az, float bx, float bz, float r, float top, float over, float waist)
            {
                A = new Vector2(ax, az);
                B = new Vector2(bx, bz);
                Radius = r;
                TopY = top;
                Overhang = over;
                Waist = waist;
            }
        }

        // Nearshore reef heads close to the bay (short entrance) + mid-shelf landmarks.
        static readonly Landmark[] Landmarks =
        {
            // Living reef just past the waterline — visible on the first swim-in.
            new Landmark(-28, DiveWorldConfig.BeachShoreZ + 28f, -18, DiveWorldConfig.BeachShoreZ + 55f, 10, -2.2f, 1.5f, 1),
            new Landmark(22, DiveWorldConfig.BeachShoreZ + 24f, 34, DiveWorldConfig.BeachShoreZ + 52f, 9, -2.4f, 1.5f, 1),
            new Landmark(-8, DiveWorldConfig.BeachShoreZ + 70f, 6, DiveWorldConfig.BeachShoreZ + 95f, 11, -3.2f, 2f, 1),
            new Landmark(-55, DiveWorldConfig.BeachShoreZ + 120f, -40, DiveWorldConfig.BeachShoreZ + 160f, 14, -4.5f, 2.5f, 1),
            new Landmark(48, DiveWorldConfig.BeachShoreZ + 110f, 68, DiveWorldConfig.BeachShoreZ + 155f, 13, -4.8f, 2.5f, 1),
            new Landmark(-120, DiveWorldConfig.BeachShoreZ + 320f, -100, DiveWorldConfig.BeachShoreZ + 390f, 18, -7f, 3f, 2),
            new Landmark(140, DiveWorldConfig.BeachShoreZ + 340f, 160, DiveWorldConfig.BeachShoreZ + 420f, 16, -8f, 3f, 2),
            // Mid-shelf rises toward the far 20 m zone.
            new Landmark(-37, -42, -33, -22, 14, -12, 3.5f, 2),
            new Landmark(30, -45, 35, -27, 14, -14, 4, 2),
            new Landmark(6, 200, 6, 260, 22, -16, 2, 1),
            new Landmark(-60, 400, -55, 480, 18, -18, 3, 2),
            new Landmark(90, 550, 110, 620, 16, -17, 3, 2),
        };

        // Soft clearings (no walls) so the bay swim-out stays readable.
        static readonly Vector4[] Clears =
        {
            new Vector4(0, DiveWorldConfig.BeachShoreZ + 45f, 28, 12),
            new Vector4(0, DiveWorldConfig.BeachShoreZ + 160f, 36, 14),
            new Vector4(0, -200, 40, 16),
            new Vector4(80, 100, 28, 12),
            new Vector4(-90, 300, 24, 12),
        };

        public static GameObject Build(Transform parent)
        {
            const int res = 288;
            float half = DiveWorldConfig.OceanRadius;
            float step = (half * 2f) / (res - 1);

            var heights = new float[res, res];
            for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
                heights[x, z] = SampleFloorY(-half + x * step, -half + z * step);

            var mesh = BuildMesh(heights, res, -half, step);
            var go = new GameObject("OceanFloor");
            go.transform.SetParent(parent, false);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            // Bright Maldives white sand.
            mr.sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.SandAlbedo,
                ProceduralTextures.SandNormal,
                new Color(1f, 0.97f, 0.9f),
                0.32f,
                0.04f,
                new Vector2(48f, 48f));
            go.AddComponent<MeshCollider>().sharedMesh = mesh;

            ScatterInteriorFeatures(go.transform, heights, res, -half, step);
            BuildBeachExtras(go.transform);
            BuildShoreBarriers(go.transform);
            return go;
        }

        /// <summary>
        /// Soft beach → gradual turquoise shelf (Turkey-like ramp, no cliff).
        /// Dry sand south of the curved waterline; ~5 m by NearshoreShallowDistance;
        /// ~20 m only after ~1.5 km.
        /// </summary>
        static float SampleFloorY(float x, float z)
        {
            float shoreZ = DiveWorldConfig.ShorelineZ(x);
            float offshore = z - shoreZ;
            float near = DiveWorldConfig.NearshoreShallowDistance;

            float shelfY;
            if (offshore < -140f)
            {
                // Dry hinterland / low dunes above the waterline.
                shelfY = 2.6f;
            }
            else if (offshore < 0f)
            {
                // Sandy beach face down to the waterline (calm walk-in along the curve).
                float t = Mathf.InverseLerp(-140f, 0f, offshore);
                shelfY = Mathf.Lerp(2.6f, 0.05f, SmoothStep(0f, 1f, t));
            }
            else if (offshore < 55f)
            {
                // First steps into water — ankle/knee depth only, no ledge.
                float t = SmoothStep(0f, 1f, offshore / 55f);
                shelfY = Mathf.Lerp(0.05f, -1.35f, t);
            }
            else if (offshore < near)
            {
                // Long gentle shallows — depth grows slowly like a Turkey beach entry.
                float t = SmoothStep(0f, 1f, (offshore - 55f) / (near - 55f));
                shelfY = Mathf.Lerp(-1.35f, -5.2f, t);
            }
            else if (offshore < DiveWorldConfig.CoastalShelfDistance)
            {
                float t = SmoothStep(0f, 1f,
                    (offshore - near) / (DiveWorldConfig.CoastalShelfDistance - near));
                shelfY = Mathf.Lerp(-5.2f, -DiveWorldConfig.CoastalShelfDepth, t);
            }
            else
            {
                shelfY = -DiveWorldConfig.CoastalShelfDepth;
            }

            // Soft noise — keep nearshore very calm so the walk-in never cliffs.
            float noiseAmp = offshore < 0f
                ? 0.18f
                : offshore < near
                    ? Mathf.Lerp(0.14f, 0.55f, Mathf.Clamp01(offshore / near))
                    : Mathf.Lerp(0.55f, 2.2f, Mathf.Clamp01((offshore - near) / 900f));
            shelfY += (Fbm(x * 0.011f, z * 0.011f, 3) - 0.5f) * noiseAmp;
            shelfY += (Fbm(x * 0.04f, z * 0.04f, 2) - 0.5f) * (noiseAmp * 0.3f);

            // Deep basins only far from shore.
            if (offshore > 1000f)
            {
                float basin = Fbm(x * 0.006f + 40f, z * 0.006f + 12f, 3);
                if (basin < 0.36f)
                    shelfY -= (0.36f - basin) * 42f;
            }

            float clear = 0f;
            for (int i = 0; i < Clears.Length; i++)
            {
                var c = Clears[i];
                float d = Vector2.Distance(new Vector2(x, z), new Vector2(c.x, c.y));
                clear = Mathf.Max(clear, 1f - SmoothStep(0f, c.z, Mathf.Max(0f, d - c.w)));
            }

            float mesaLift = 0f;
            if (offshore > 12f)
            {
                for (int i = 0; i < Landmarks.Length; i++)
                {
                    var m = Landmarks[i];
                    float d = DistToSegment(new Vector2(x, z), m.A, m.B) - m.Radius;
                    float influence = 1f - SmoothStep(0f, 10f, Mathf.Max(0f, d));
                    if (influence <= 0f)
                        continue;
                    float target = Mathf.Lerp(shelfY, m.TopY, influence);
                    float waist = 1f - Mathf.Abs(0.5f - influence) * m.Waist * 0.12f;
                    mesaLift = Mathf.Max(mesaLift, (target - shelfY) * influence * waist);
                }
            }

            float y = shelfY + mesaLift * (1f - clear * 0.8f);

            // Allow dry beach above the waterline; keep submerged floor below it.
            float maxY = offshore < 40f
                ? DiveWorldConfig.WaterSurfaceY + 4.5f
                : DiveWorldConfig.WaterSurfaceY - 0.4f;
            return Mathf.Clamp(y, DiveWorldConfig.OceanFloorY, maxY);
        }

        static void BuildBeachExtras(Transform parent)
        {
            // Low sunlit sand berm behind the waterline, bent to follow the bay arc.
            var bermRoot = new GameObject("BeachBermArc");
            bermRoot.transform.SetParent(parent, false);
            var sandMat = ProceduralTextures.MakeLit(
                ProceduralTextures.SandAlbedo,
                ProceduralTextures.SandNormal,
                new Color(1f, 0.98f, 0.92f),
                0.34f,
                0.05f,
                new Vector2(10f, 4f));

            const int segments = 14;
            float half = DiveWorldConfig.ShoreArcHalfWidth * 1.55f;
            for (int i = 0; i < segments; i++)
            {
                float t0 = (i / (float)(segments - 1)) * 2f - 1f;
                float x = t0 * half;
                float z = DiveWorldConfig.ShorelineZ(x) - 48f;
                var berm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                berm.name = "BeachBerm_" + i;
                Object.Destroy(berm.GetComponent<BoxCollider>());
                berm.transform.SetParent(bermRoot.transform, false);
                berm.transform.position = new Vector3(x, 1.35f, z);
                float yaw = Mathf.Atan2(DiveWorldConfig.ShorelineZ(x + 4f) - DiveWorldConfig.ShorelineZ(x - 4f), 8f) * Mathf.Rad2Deg;
                berm.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                berm.transform.localScale = new Vector3(half * 2f / segments * 1.35f, 1.05f, 58f);
                berm.GetComponent<MeshRenderer>().sharedMaterial = sandMat;
            }
        }

        /// <summary>
        /// Organic headland reefs/rocks on the bay flanks — boulder piles, not flat walls.
        /// Blocks swimming left/right along the shore; center channel stays open into depth.
        /// </summary>
        static void BuildShoreBarriers(Transform parent)
        {
            var root = new GameObject("ShoreReefBarriers");
            root.transform.SetParent(parent, false);

            var rockMat = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                new Color(0.78f, 0.74f, 0.68f), 0.18f, 0.08f, new Vector2(2.2f, 2.2f));
            var coralMat = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(0.95f, 0.48f, 0.38f), 0.42f, 0.06f, new Vector2(1.4f, 1.4f));
            var coralMatB = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(0.35f, 0.78f, 0.72f), 0.4f, 0.05f, new Vector2(1.2f, 1.2f));

            BuildHeadland(root.transform, -1f, rockMat, coralMat, coralMatB);
            BuildHeadland(root.transform, 1f, rockMat, coralMat, coralMatB);
        }

        static void BuildHeadland(Transform parent, float side, Material rockMat, Material coralMat, Material coralMatB)
        {
            float channel = DiveWorldConfig.ShoreChannelHalfWidth;
            float half = DiveWorldConfig.ShoreArcHalfWidth;
            var rng = new System.Random(side < 0f ? 4411 : 5522);

            // Organic boulder clusters along each headland (sphere/capsule/tilted rock).
            const int clusters = 14;
            for (int i = 0; i < clusters; i++)
            {
                float along = i / (float)(clusters - 1);
                float xInner = side * Mathf.Lerp(channel + 6f, half + 6f, along * 0.4f + 0.1f);
                float xOuter = side * Mathf.Lerp(half * 0.7f, half + 32f, along);
                float x = Mathf.Lerp(xInner, xOuter, 0.35f + (float)rng.NextDouble() * 0.45f);
                float shore = DiveWorldConfig.ShorelineZ(x);
                float z = shore + Mathf.Lerp(-6f, 95f, along) + (float)(rng.NextDouble() * 8.0 - 4.0);

                if (Mathf.Abs(x) < channel - 1.5f)
                    continue;

                int boulders = 3 + rng.Next(0, 4);
                for (int b = 0; b < boulders; b++)
                {
                    float ox = x + (float)(rng.NextDouble() * 5.5 - 2.75);
                    float oz = z + (float)(rng.NextDouble() * 5.5 - 2.75);
                    if (Mathf.Abs(ox) < channel - 1f)
                        continue;

                    float s = Mathf.Lerp(1.6f, 4.8f, (float)rng.NextDouble()) * (1f - b * 0.08f);
                    float y = DiveWorldConfig.WaterSurfaceY
                        + s * 0.18f
                        - Mathf.Lerp(0.15f, 2.4f, along)
                        - (float)rng.NextDouble() * 0.6f;

                    // Mix shapes so the flank reads as reef/rock, not a wall.
                    PrimitiveType prim = rng.NextDouble() < 0.45
                        ? PrimitiveType.Sphere
                        : rng.NextDouble() < 0.55
                            ? PrimitiveType.Capsule
                            : PrimitiveType.Cube;
                    var rock = GameObject.CreatePrimitive(prim);
                    rock.name = side < 0f ? "LeftReefRock" : "RightReefRock";
                    rock.transform.SetParent(parent, false);
                    rock.transform.position = new Vector3(ox, y, oz);
                    if (prim == PrimitiveType.Capsule)
                    {
                        rock.transform.localScale = new Vector3(
                            s * Mathf.Lerp(0.45f, 0.85f, (float)rng.NextDouble()),
                            s * Mathf.Lerp(0.35f, 0.7f, (float)rng.NextDouble()),
                            s * Mathf.Lerp(0.45f, 0.85f, (float)rng.NextDouble()));
                        rock.transform.rotation = Quaternion.Euler(
                            70f + (float)rng.NextDouble() * 40f,
                            (float)rng.NextDouble() * 360f,
                            (float)rng.NextDouble() * 30f - 15f);
                    }
                    else if (prim == PrimitiveType.Sphere)
                    {
                        float sx = s * Mathf.Lerp(0.7f, 1.25f, (float)rng.NextDouble());
                        float sy = s * Mathf.Lerp(0.45f, 0.9f, (float)rng.NextDouble());
                        float sz = s * Mathf.Lerp(0.7f, 1.25f, (float)rng.NextDouble());
                        rock.transform.localScale = new Vector3(sx, sy, sz);
                        rock.transform.rotation = Quaternion.Euler(
                            (float)rng.NextDouble() * 40f,
                            (float)rng.NextDouble() * 360f,
                            (float)rng.NextDouble() * 40f);
                    }
                    else
                    {
                        rock.transform.localScale = new Vector3(
                            s * Mathf.Lerp(0.6f, 1.2f, (float)rng.NextDouble()),
                            s * Mathf.Lerp(0.35f, 0.75f, (float)rng.NextDouble()),
                            s * Mathf.Lerp(0.6f, 1.2f, (float)rng.NextDouble()));
                        rock.transform.rotation = Quaternion.Euler(
                            (float)rng.NextDouble() * 35f,
                            (float)rng.NextDouble() * 360f,
                            (float)rng.NextDouble() * 35f);
                    }

                    rock.GetComponent<MeshRenderer>().sharedMaterial =
                        along > 0.4f && rng.NextDouble() < 0.4 ? coralMat : rockMat;
                }
            }

            // Coral crowns / fingers on the seaward tips — keep channel clear.
            for (int i = 0; i < 16; i++)
            {
                float x = side * (half + 2f + (float)rng.NextDouble() * 22f);
                float z = DiveWorldConfig.ShorelineZ(x) + 35f + (float)rng.NextDouble() * 55f;
                if (Mathf.Abs(x) < channel)
                    continue;

                bool sphere = rng.NextDouble() < 0.4;
                var crown = GameObject.CreatePrimitive(sphere ? PrimitiveType.Sphere : PrimitiveType.Cylinder);
                crown.name = "HeadlandCoral";
                Object.Destroy(crown.GetComponent<Collider>());
                crown.transform.SetParent(parent, false);
                float hgt = Mathf.Lerp(0.7f, 2.6f, (float)rng.NextDouble());
                float rad = Mathf.Lerp(0.3f, 1.1f, (float)rng.NextDouble());
                crown.transform.position = new Vector3(x, -1.1f + hgt * 0.3f, z);
                crown.transform.localScale = sphere
                    ? new Vector3(rad * 1.4f, hgt * 0.55f, rad * 1.4f)
                    : new Vector3(rad, hgt * 0.5f, rad);
                crown.transform.rotation = Quaternion.Euler(
                    (float)rng.NextDouble() * 18f,
                    (float)rng.NextDouble() * 360f,
                    (float)rng.NextDouble() * 18f);
                crown.GetComponent<MeshRenderer>().sharedMaterial =
                    rng.NextDouble() < 0.5 ? coralMat : coralMatB;
            }
        }

        static Mesh BuildMesh(float[,] h, int res, float origin, float step)
        {
            int vertCount = res * res;
            var verts = new Vector3[vertCount];
            var norms = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var tris = new int[(res - 1) * (res - 1) * 6];

            for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                int i = x + z * res;
                float wx = origin + x * step;
                float wz = origin + z * step;
                verts[i] = new Vector3(wx, h[x, z], wz);
                uvs[i] = new Vector2(wx * 0.05f, wz * 0.05f);
            }

            int t = 0;
            for (int z = 0; z < res - 1; z++)
            for (int x = 0; x < res - 1; x++)
            {
                int i = x + z * res;
                tris[t++] = i;
                tris[t++] = i + res;
                tris[t++] = i + 1;
                tris[t++] = i + 1;
                tris[t++] = i + res;
                tris[t++] = i + res + 1;
            }

            for (int i = 0; i < vertCount; i++)
                norms[i] = Vector3.zero;
            for (int i = 0; i < tris.Length; i += 3)
            {
                var n = Vector3.Cross(verts[tris[i + 1]] - verts[tris[i]], verts[tris[i + 2]] - verts[tris[i]]).normalized;
                norms[tris[i]] += n;
                norms[tris[i + 1]] += n;
                norms[tris[i + 2]] += n;
            }
            for (int i = 0; i < vertCount; i++)
                norms[i].Normalize();

            var mesh = new Mesh
            {
                name = "OceanFloorMesh",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Nearshore coral + rocks packed close to the bay. No perimeter ring walls.
        /// </summary>
        static void ScatterInteriorFeatures(Transform parent, float[,] h, int res, float origin, float step)
        {
            var rocks = new GameObject("Rocks");
            rocks.transform.SetParent(parent, false);
            var coral = new GameObject("CoralShallow");
            coral.transform.SetParent(parent, false);

            var rockMat = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                Color.white, 0.12f, 0.08f, new Vector2(2f, 2f));
            var deepRockMat = ProceduralTextures.MakeLit(
                ProceduralTextures.SiltAlbedo, ProceduralTextures.SiltNormal,
                Color.white, 0.08f, 0.05f, new Vector2(1.5f, 1.5f));
            var coralMat = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(1f, 0.55f, 0.4f), 0.4f, 0.05f, new Vector2(1.2f, 1.2f));

            var rng = new System.Random(20261110);
            int rockCount = 160;
            float maxScatterR = DiveWorldConfig.SoftBoundaryRadius * 0.92f;

            for (int n = 0; n < rockCount; n++)
            {
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float rad = Mathf.Sqrt((float)rng.NextDouble()) * maxScatterR;
                float wx = Mathf.Cos(ang) * rad;
                float wz = Mathf.Sin(ang) * rad;
                float offshore = DiveWorldConfig.OffshoreDistance(wx, wz);
                // Keep decorative rocks out of the dry beach pad and shop arc.
                if (offshore < 12f)
                    continue;

                int ix = Mathf.Clamp(Mathf.RoundToInt((wx - origin) / step), 1, res - 2);
                int iz = Mathf.Clamp(Mathf.RoundToInt((wz - origin) / step), 1, res - 2);
                float wy = h[ix, iz];
                float depth = DiveWorldConfig.WaterSurfaceY - wy;
                if (depth < 0.5f)
                    continue;

                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "Rock";
                Object.Destroy(rock.GetComponent<BoxCollider>());
                rock.transform.SetParent(rocks.transform, false);
                float s = depth > DiveWorldConfig.MidMaxDepth
                    ? Mathf.Lerp(2f, 5.5f, (float)rng.NextDouble())
                    : Mathf.Lerp(0.7f, 2.4f, (float)rng.NextDouble());
                rock.transform.position = new Vector3(wx, wy + s * 0.2f, wz);
                rock.transform.localScale = new Vector3(
                    s,
                    s * Mathf.Lerp(0.35f, 0.85f, (float)rng.NextDouble()),
                    s * Mathf.Lerp(0.65f, 1.25f, (float)rng.NextDouble()));
                rock.transform.rotation = Quaternion.Euler(
                    (float)rng.NextDouble() * 28f,
                    (float)rng.NextDouble() * 360f,
                    (float)rng.NextDouble() * 28f);
                rock.GetComponent<MeshRenderer>().sharedMaterial =
                    depth > DiveWorldConfig.MidMaxDepth ? deepRockMat : rockMat;
            }

            // Dense reef scatter through the gentle nearshore shallows (fish companions).
            int coralCount = 180;
            for (int n = 0; n < coralCount; n++)
            {
                float wx = (float)(rng.NextDouble() * 180.0 - 90.0);
                // Keep the open channel mostly clear; bias life toward flanks + just past mouth.
                if (Mathf.Abs(wx) < DiveWorldConfig.ShoreChannelHalfWidth * 0.55f && rng.NextDouble() < 0.55)
                    wx = (wx < 0f ? -1f : 1f) * (DiveWorldConfig.ShoreChannelHalfWidth * 0.6f
                        + (float)rng.NextDouble() * 40f);
                float wz = DiveWorldConfig.ShorelineZ(wx) + 12f
                    + (float)(rng.NextDouble() * DiveWorldConfig.NearshoreShallowDistance * 0.85);
                int ix = Mathf.Clamp(Mathf.RoundToInt((wx - origin) / step), 1, res - 2);
                int iz = Mathf.Clamp(Mathf.RoundToInt((wz - origin) / step), 1, res - 2);
                float wy = h[ix, iz];
                float depth = DiveWorldConfig.WaterSurfaceY - wy;
                if (depth < 0.9f || depth > DiveWorldConfig.ShallowMaxDepth)
                    continue;

                var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                c.name = "Coral";
                Object.Destroy(c.GetComponent<CapsuleCollider>());
                c.transform.SetParent(coral.transform, false);
                float hgt = Mathf.Lerp(0.4f, 1.8f, (float)rng.NextDouble());
                float radScale = Mathf.Lerp(0.15f, 0.45f, (float)rng.NextDouble());
                c.transform.position = new Vector3(wx, wy + hgt * 0.5f, wz);
                c.transform.localScale = new Vector3(radScale, hgt * 0.5f, radScale);
                c.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 12f);
                c.GetComponent<MeshRenderer>().sharedMaterial = coralMat;
            }
        }

        static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(Vector2.Dot(ba, ba), 1e-4f));
            return (pa - ba * h).magnitude;
        }

        static float SmoothStep(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / Mathf.Max(1e-4f, b - a));
            return t * t * (3f - 2f * t);
        }

        static float Hash(float x, float z)
        {
            float n = Mathf.Sin(x * 127.1f + z * 311.7f) * 43758.5453f;
            return n - Mathf.Floor(n);
        }

        static float Noise(float x, float z)
        {
            float ix = Mathf.Floor(x), iz = Mathf.Floor(z);
            float fx = x - ix, fz = z - iz;
            fx = fx * fx * (3f - 2f * fx);
            fz = fz * fz * (3f - 2f * fz);
            return Mathf.Lerp(
                Mathf.Lerp(Hash(ix, iz), Hash(ix + 1, iz), fx),
                Mathf.Lerp(Hash(ix, iz + 1), Hash(ix + 1, iz + 1), fx), fz);
        }

        static float Fbm(float x, float z, int oct)
        {
            float a = 0.5f, s = 0f, sum = 0f;
            for (int i = 0; i < oct; i++)
            {
                s += a * Noise(x, z);
                sum += a;
                x = x * 2.03f + 17.1f;
                z = z * 2.03f + 31.7f;
                a *= 0.5f;
            }
            return s / sum;
        }
    }
}
