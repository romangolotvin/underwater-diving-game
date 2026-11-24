using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Runtime albedo + normal textures so nothing ships as flat untextured color.
    /// Generated once and cached; not a substitute for authored art, but meets the textured bar.
    /// </summary>
    public static class ProceduralTextures
    {
        static Texture2D sandAlbedo, sandNormal;
        static Texture2D rockAlbedo, rockNormal;
        static Texture2D siltAlbedo, siltNormal;
        static Texture2D coralAlbedo, coralNormal;
        static Texture2D suitAlbedo, suitNormal;
        static Texture2D waterAlbedo;
        static Texture2D fishScaleAlbedo, fishScaleNormal;
        static Texture2D shellAlbedo, shellNormal;
        static Texture2D rayAlbedo, rayNormal;
        static Texture2D ropeAlbedo;

        public static Texture2D SandAlbedo => sandAlbedo ??= MakeAlbedo(512, 0.42f, 0.38f, 0.28f, 0.55f, 0.48f, 0.34f, 4.2f, 0.35f);
        public static Texture2D SandNormal => sandNormal ??= MakeNormal(512, 9f, 0.55f);
        public static Texture2D RockAlbedo => rockAlbedo ??= MakeAlbedo(512, 0.22f, 0.24f, 0.26f, 0.35f, 0.36f, 0.34f, 2.4f, 0.55f);
        public static Texture2D RockNormal => rockNormal ??= MakeNormal(512, 5.5f, 1.1f);
        public static Texture2D SiltAlbedo => siltAlbedo ??= MakeAlbedo(512, 0.12f, 0.14f, 0.18f, 0.18f, 0.2f, 0.26f, 1.8f, 0.4f);
        public static Texture2D SiltNormal => siltNormal ??= MakeNormal(512, 3.5f, 0.4f);
        public static Texture2D CoralAlbedo => coralAlbedo ??= MakeAlbedo(512, 0.55f, 0.28f, 0.32f, 0.75f, 0.45f, 0.35f, 6.5f, 0.5f);
        public static Texture2D CoralNormal => coralNormal ??= MakeNormal(512, 7f, 0.85f);
        public static Texture2D SuitAlbedo => suitAlbedo ??= MakeSuitAlbedo(512);
        public static Texture2D SuitNormal => suitNormal ??= MakeNormal(512, 4f, 0.35f);
        public static Texture2D WaterAlbedo => waterAlbedo ??= MakeWater(512);
        public static Texture2D FishScaleAlbedo => fishScaleAlbedo ??= MakeFishScale(512);
        public static Texture2D FishScaleNormal => fishScaleNormal ??= MakeNormal(512, 11f, 0.9f);
        public static Texture2D ShellAlbedo => shellAlbedo ??= MakeShell(512);
        public static Texture2D ShellNormal => shellNormal ??= MakeNormal(512, 6f, 1.2f);
        public static Texture2D RayAlbedo => rayAlbedo ??= MakeRaySkin(512);
        public static Texture2D RayNormal => rayNormal ??= MakeNormal(512, 4.5f, 0.55f);
        public static Texture2D RopeAlbedo => ropeAlbedo ??= MakeRope(256);

        public static Material MakeLit(Texture2D albedo, Texture2D normal, Color tint, float gloss, float metal, Vector2 tiling)
        {
            var shader = Shader.Find("Standard");
            var mat = new Material(shader) { name = albedo != null ? albedo.name + "_Lit" : "Lit" };
            mat.color = tint;
            if (albedo != null)
            {
                mat.mainTexture = albedo;
                mat.mainTextureScale = tiling;
            }
            if (normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetTextureScale("_BumpMap", tiling);
                mat.EnableKeyword("_NORMALMAP");
                mat.SetFloat("_BumpScale", 1f);
            }
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", gloss);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metal);
            mat.enableInstancing = true;
            return mat;
        }

        public static Material MakeTransparentLit(Texture2D albedo, Color tint, float gloss)
        {
            var mat = MakeLit(albedo, null, tint, gloss, 0.05f, new Vector2(8f, 8f));
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            var c = tint;
            c.a = tint.a;
            mat.color = c;
            return mat;
        }

        static Texture2D MakeAlbedo(int size, float r0, float g0, float b0, float r1, float g1, float b1, float freq, float contrast)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
            {
                name = $"ProcAlbedo_{freq:0.0}",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8
            };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float n = Fbm(u * freq, v * freq, 5);
                    float m = Fbm(u * freq * 2.7f + 19f, v * freq * 2.7f + 7f, 3);
                    float t = Mathf.Clamp01((n - 0.5f) * contrast * 2f + 0.5f);
                    float speck = Mathf.Pow(m, 3f) * 0.25f;
                    Color c = Color.Lerp(new Color(r0, g0, b0), new Color(r1, g1, b1), t);
                    c += new Color(speck, speck * 0.9f, speck * 0.7f);
                    px[x + y * size] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }

        static Texture2D MakeNormal(int size, float freq, float strength)
        {
            var height = new float[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                height[x + y * size] = Fbm(x / (float)size * freq, y / (float)size * freq, 4);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, true)
            {
                name = $"ProcNormal_{freq:0.0}",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8
            };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int xl = (x + size - 1) % size, xr = (x + 1) % size;
                    int yd = (y + size - 1) % size, yu = (y + 1) % size;
                    float dx = (height[xr + y * size] - height[xl + y * size]) * strength;
                    float dy = (height[x + yu * size] - height[x + yd * size]) * strength;
                    Vector3 n = new Vector3(-dx, -dy, 1f).normalized;
                    px[x + y * size] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }

        static Texture2D MakeSuitAlbedo(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
            {
                name = "SuitAlbedo",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8
            };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float weave = Mathf.Abs(Mathf.Sin(u * 80f) * Mathf.Sin(v * 80f));
                float wear = Fbm(u * 5f, v * 5f, 4);
                Color baseC = Color.Lerp(new Color(0.05f, 0.12f, 0.18f), new Color(0.1f, 0.22f, 0.28f), wear);
                baseC = Color.Lerp(baseC, new Color(0.15f, 0.15f, 0.12f), weave * 0.15f);
                // Stripe accent
                if (Mathf.Abs((v * 8f) % 1f - 0.5f) < 0.06f)
                    baseC = Color.Lerp(baseC, new Color(0.7f, 0.55f, 0.15f), 0.65f);
                px[x + y * size] = baseC;
            }
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }

        static Texture2D MakeFishScale(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
            {
                name = "FishScaleAlbedo",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8
            };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float row = Mathf.Floor(v * 28f);
                float ox = (row % 2f) * 0.5f;
                float sx = Mathf.Abs(((u * 28f + ox) % 1f) - 0.5f);
                float sy = Mathf.Abs(((v * 28f) % 1f) - 0.5f);
                float scale = Mathf.Clamp01(1f - (sx * sx + sy * sy * 1.4f) * 4f);
                float n = Fbm(u * 5f, v * 5f, 4);
                Color c = Color.Lerp(new Color(0.18f, 0.28f, 0.32f), new Color(0.55f, 0.7f, 0.65f), scale);
                c = Color.Lerp(c, c * (0.7f + n * 0.5f), 0.45f);
                px[x + y * size] = c;
            }
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }

        static Texture2D MakeShell(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
            {
                name = "ShellAlbedo",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8
            };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float plate = Mathf.Abs(Mathf.Sin(u * 9f) * Mathf.Cos(v * 7f));
                float n = Fbm(u * 4f, v * 4f, 4);
                Color c = Color.Lerp(new Color(0.25f, 0.38f, 0.22f), new Color(0.55f, 0.62f, 0.35f), plate);
                c = Color.Lerp(c, new Color(0.4f, 0.32f, 0.18f), n * 0.35f);
                px[x + y * size] = c;
            }
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }

        static Texture2D MakeRaySkin(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
            {
                name = "RayAlbedo",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8
            };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float spots = Mathf.Pow(Fbm(u * 10f, v * 10f, 3), 2.2f);
                float n = Fbm(u * 3f, v * 3f, 4);
                Color c = Color.Lerp(new Color(0.28f, 0.26f, 0.32f), new Color(0.55f, 0.5f, 0.48f), n);
                c = Color.Lerp(c, new Color(0.75f, 0.72f, 0.68f), spots * 0.65f);
                px[x + y * size] = c;
            }
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }

        static Texture2D MakeRope(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
            {
                name = "RopeAlbedo",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float twist = Mathf.Abs(Mathf.Sin((u + v) * 40f));
                float n = Fbm(u * 8f, v * 8f, 3);
                Color c = Color.Lerp(new Color(0.45f, 0.38f, 0.22f), new Color(0.7f, 0.62f, 0.4f), twist);
                c *= 0.85f + n * 0.25f;
                px[x + y * size] = c;
            }
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }

        static Texture2D MakeWater(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
            {
                name = "WaterAlbedo",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float w = Fbm(u * 6f, v * 6f, 4);
                float r = Fbm(u * 14f + 3f, v * 14f, 3);
                Color c = Color.Lerp(new Color(0.05f, 0.25f, 0.35f, 0.55f), new Color(0.2f, 0.55f, 0.65f, 0.4f), w);
                c.a = Mathf.Lerp(0.35f, 0.65f, r);
                px[x + y * size] = c;
            }
            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        static float Hash(float x, float y)
        {
            float n = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
            return n - Mathf.Floor(n);
        }

        static float Noise(float x, float y)
        {
            float ix = Mathf.Floor(x), iy = Mathf.Floor(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(
                Mathf.Lerp(Hash(ix, iy), Hash(ix + 1, iy), fx),
                Mathf.Lerp(Hash(ix, iy + 1), Hash(ix + 1, iy + 1), fx), fy);
        }

        static float Fbm(float x, float y, int oct)
        {
            float a = 0.5f, s = 0f, sum = 0f;
            for (int i = 0; i < oct; i++)
            {
                s += a * Noise(x, y);
                sum += a;
                x = x * 2.03f + 17.1f;
                y = y * 2.03f + 31.7f;
                a *= 0.5f;
            }
            return s / sum;
        }
    }
}
