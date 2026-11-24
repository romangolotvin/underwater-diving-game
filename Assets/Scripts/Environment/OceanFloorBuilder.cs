using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Coastal seabed: sandy beach on the south, soft walk-in ramp, shelf ~20 m
    /// about 1.5 km offshore. No enclosing rock walls; soft limits only.
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

        // Nearshore reef heads + mid-shelf landmarks (absolute XZ).
        static readonly Landmark[] Landmarks =
        {
            // Shallow reef patches just off the beach (tops stay near surface).
            new Landmark(-55, -780, -40, -720, 18, -3.5f, 2f, 1),
            new Landmark(70, -760, 95, -700, 16, -4f, 2f, 1),
            new Landmark(10, -650, 18, -600, 14, -5f, 2.5f, 1),
            new Landmark(-120, -500, -100, -430, 20, -7f, 3f, 2),
            new Landmark(140, -480, 160, -400, 18, -8f, 3f, 2),
            // Mid-shelf rises toward the far 20 m zone.
            new Landmark(-37, -42, -33, -22, 14, -12, 3.5f, 2),
            new Landmark(30, -45, 35, -27, 14, -14, 4, 2),
            new Landmark(6, 200, 6, 260, 22, -16, 2, 1),
            new Landmark(-60, 400, -55, 480, 18, -18, 3, 2),
            new Landmark(90, 550, 110, 620, 16, -17, 3, 2),
        };

        // Soft clearings (no walls) so swim lanes stay readable.
        static readonly Vector4[] Clears =
        {
            new Vector4(0, DiveWorldConfig.BeachShoreZ + 80f, 36, 14),
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
            // Coastal start: sand floor (beach + soft shelf).
            mr.sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.SandAlbedo,
                ProceduralTextures.SandNormal,
                Color.white,
                0.22f,
                0.05f,
                new Vector2(48f, 48f));
            go.AddComponent<MeshCollider>().sharedMesh = mesh;

            ScatterInteriorFeatures(go.transform, heights, res, -half, step);
            BuildBeachExtras(go.transform);
            return go;
        }

        /// <summary>
        /// Soft beach → shelf profile. Dry sand south of the waterline; ~20 m depth
        /// only after ~1.5 km offshore. No radial cliff at the map edge.
        /// </summary>
        static float SampleFloorY(float x, float z)
        {
            float shoreZ = DiveWorldConfig.BeachShoreZ;
            float offshore = z - shoreZ;

            float shelfY;
            if (offshore < -140f)
            {
                // Dry hinterland / low dunes above the waterline.
                shelfY = 2.6f;
            }
            else if (offshore < 0f)
            {
                // Sandy beach face down to the waterline (calm walk-in).
                float t = Mathf.InverseLerp(-140f, 0f, offshore);
                shelfY = Mathf.Lerp(2.6f, 0.06f, SmoothStep(0f, 1f, t));
            }
            else if (offshore < 320f)
            {
                // Gentle shallows — no drop-off cliff.
                float t = SmoothStep(0f, 1f, offshore / 320f);
                shelfY = Mathf.Lerp(0.06f, -5.5f, t);
            }
            else if (offshore < DiveWorldConfig.CoastalShelfDistance)
            {
                // Soft ramp to ~20 m at ~1.5 km.
                float t = SmoothStep(0f, 1f,
                    (offshore - 320f) / (DiveWorldConfig.CoastalShelfDistance - 320f));
                shelfY = Mathf.Lerp(-5.5f, -DiveWorldConfig.CoastalShelfDepth, t);
            }
            else
            {
                shelfY = -DiveWorldConfig.CoastalShelfDepth;
            }

            // Gentle noise — stronger only well offshore (still no cliffs).
            float noiseAmp = offshore < 0f
                ? 0.28f
                : Mathf.Lerp(0.35f, 2.2f, Mathf.Clamp01(offshore / 900f));
            shelfY += (Fbm(x * 0.011f, z * 0.011f, 3) - 0.5f) * noiseAmp;
            shelfY += (Fbm(x * 0.04f, z * 0.04f, 2) - 0.5f) * (noiseAmp * 0.35f);

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
            if (offshore > 25f)
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
            // Low sunlit sand berm behind the waterline for a clear shore read.
            var berm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            berm.name = "BeachBerm";
            Object.Destroy(berm.GetComponent<BoxCollider>());
            berm.transform.SetParent(parent, false);
            berm.transform.position = new Vector3(0f, 1.4f, DiveWorldConfig.BeachShoreZ - 55f);
            berm.transform.localScale = new Vector3(420f, 1.1f, 70f);
            berm.GetComponent<MeshRenderer>().sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.SandAlbedo,
                ProceduralTextures.SandNormal,
                new Color(1f, 0.96f, 0.88f),
                0.28f,
                0.06f,
                new Vector2(18f, 6f));
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
        /// Nearshore coral + rocks. No perimeter ring walls.
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
                Color.white, 0.35f, 0.05f, new Vector2(1.2f, 1.2f));

            var rng = new System.Random(20261110);
            int rockCount = 160;
            float maxScatterR = DiveWorldConfig.SoftBoundaryRadius * 0.92f;
            float shoreZ = DiveWorldConfig.BeachShoreZ;

            for (int n = 0; n < rockCount; n++)
            {
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float rad = Mathf.Sqrt((float)rng.NextDouble()) * maxScatterR;
                float wx = Mathf.Cos(ang) * rad;
                float wz = Mathf.Sin(ang) * rad;
                // Keep decorative rocks out of the dry beach pad.
                if (wz < shoreZ + 20f)
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

            // Reef scatter in the nearshore shallows (fish + seaweed companions).
            int coralCount = 110;
            for (int n = 0; n < coralCount; n++)
            {
                float wx = (float)(rng.NextDouble() * 280.0 - 140.0);
                float wz = shoreZ + 40f + (float)(rng.NextDouble() * 420.0);
                int ix = Mathf.Clamp(Mathf.RoundToInt((wx - origin) / step), 1, res - 2);
                int iz = Mathf.Clamp(Mathf.RoundToInt((wz - origin) / step), 1, res - 2);
                float wy = h[ix, iz];
                float depth = DiveWorldConfig.WaterSurfaceY - wy;
                if (depth < 1.2f || depth > DiveWorldConfig.ShallowMaxDepth)
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
