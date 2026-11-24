using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Open seabed — no enclosing rock walls. Landmarks sit inside an endless-feeling floor;
    /// play limits are soft/invisible only.
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

        static readonly Landmark[] Landmarks =
        {
            new Landmark(-37, -42, -33, -22, 14, -8, 3.5f, 2),
            new Landmark(-29, -15, -33, 5, 16, -16, 3, 2),
            new Landmark(30, -45, 35, -27, 14, -10, 4, 2),
            new Landmark(37, -20, 31, -3, 15, -18, 3, 2),
            new Landmark(6, -88, 6, -88, 18, -34, 0, 0),
            new Landmark(0, -50, 0, -50, 10, -5, 2, 1),
            new Landmark(-60, 20, -55, 40, 14, -12, 4, 2),
            new Landmark(22, 67, 24, 80, 11, -14, 5, 2),
            new Landmark(44, 6, 47, -8, 12, -20, 3, 1),
            new Landmark(-20, 30, -10, 45, 13, -22, 3, 2),
        };

        // Soft clearings (no walls) so swim lanes stay readable.
        static readonly Vector4[] Clears =
        {
            new Vector4(0, -20, 28, 12),
            new Vector4(0, -86, 36, 14),
            new Vector4(40, 20, 20, 10),
            new Vector4(-40, 40, 18, 10),
        };

        public static GameObject Build(Transform parent)
        {
            const int res = 224;
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
            mr.sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.SandAlbedo,
                ProceduralTextures.SandNormal,
                Color.white,
                0.22f,
                0.05f,
                new Vector2(48f, 48f));
            // Blend toward silt in shader via vertex colors isn't in Standard easily;
            // use sand base + second material regions via rock scatter instead.
            go.AddComponent<MeshCollider>().sharedMesh = mesh;

            ScatterInteriorFeatures(go.transform, heights, res, -half, step);
            return go;
        }

        static float SampleFloorY(float x, float z)
        {
            // Continuous open seafloor — NO radial cliff / ring wall at the edge.
            float baseY = DiveWorldConfig.OceanFloorY + 14f;
            baseY += (Fbm(x * 0.011f, z * 0.011f, 4) - 0.5f) * 16f;
            baseY += (Fbm(x * 0.035f, z * 0.035f, 3) - 0.5f) * 7f;
            baseY += (Fbm(x * 0.09f, z * 0.09f, 2) - 0.5f) * 2.5f;

            // Gentle basins (deeper pockets), not perimeter walls.
            float basin = Fbm(x * 0.008f + 40f, z * 0.008f + 12f, 3);
            if (basin < 0.38f)
                baseY -= (0.38f - basin) * 28f;

            float clear = 0f;
            for (int i = 0; i < Clears.Length; i++)
            {
                var c = Clears[i];
                float d = Vector2.Distance(new Vector2(x, z), new Vector2(c.x, c.y));
                clear = Mathf.Max(clear, 1f - SmoothStep(0f, c.z, Mathf.Max(0f, d - c.w)));
            }

            float mesaLift = 0f;
            for (int i = 0; i < Landmarks.Length; i++)
            {
                var m = Landmarks[i];
                float d = DistToSegment(new Vector2(x, z), m.A, m.B) - m.Radius;
                float influence = 1f - SmoothStep(0f, 10f, Mathf.Max(0f, d));
                if (influence <= 0f)
                    continue;
                float target = Mathf.Lerp(baseY, m.TopY, influence);
                float waist = 1f - Mathf.Abs(0.5f - influence) * m.Waist * 0.12f;
                mesaLift = Mathf.Max(mesaLift, (target - baseY) * influence * waist);
            }

            float y = baseY + mesaLift * (1f - clear * 0.8f);
            // Keep surface open; never raise floor into a rim at map edge.
            return Mathf.Clamp(y, DiveWorldConfig.OceanFloorY, DiveWorldConfig.WaterSurfaceY - 3f);
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
        /// Rocks and shallow coral only in the interior. Never ring the perimeter.
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
            int rockCount = 120;
            float maxScatterR = DiveWorldConfig.SoftBoundaryRadius * 0.92f;

            for (int n = 0; n < rockCount; n++)
            {
                // Disk sample — avoid concentrating on the outer rim.
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float rad = Mathf.Sqrt((float)rng.NextDouble()) * maxScatterR;
                float wx = Mathf.Cos(ang) * rad;
                float wz = Mathf.Sin(ang) * rad;

                int ix = Mathf.Clamp(Mathf.RoundToInt((wx - origin) / step), 1, res - 2);
                int iz = Mathf.Clamp(Mathf.RoundToInt((wz - origin) / step), 1, res - 2);
                float wy = h[ix, iz];
                float depth = DiveWorldConfig.WaterSurfaceY - wy;

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

            // Coral / reef detail only in shallow band — none in deep (per design notes).
            int coralCount = 70;
            for (int n = 0; n < coralCount; n++)
            {
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float rad = Mathf.Sqrt((float)rng.NextDouble()) * (maxScatterR * 0.55f);
                float wx = Mathf.Cos(ang) * rad + (float)(rng.NextDouble() * 20.0 - 10.0);
                float wz = Mathf.Sin(ang) * rad - 20f + (float)(rng.NextDouble() * 30.0);
                int ix = Mathf.Clamp(Mathf.RoundToInt((wx - origin) / step), 1, res - 2);
                int iz = Mathf.Clamp(Mathf.RoundToInt((wz - origin) / step), 1, res - 2);
                float wy = h[ix, iz];
                float depth = DiveWorldConfig.WaterSurfaceY - wy;
                if (depth > DiveWorldConfig.ShallowMaxDepth)
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
