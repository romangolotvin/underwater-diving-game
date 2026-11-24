using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Open-ocean water surface with real mesh deformation (sum of sines / light Gerstner).
    /// Logical water level stays at DiveWorldConfig.WaterSurfaceY via WaterVolume — no solid blocker.
    /// </summary>
    public class WaveOceanSurface : MonoBehaviour
    {
        [SerializeField] int resolution = 64;
        [SerializeField] float amplitude1 = 0.45f;
        [SerializeField] float amplitude2 = 0.28f;
        [SerializeField] float amplitude3 = 0.16f;
        [SerializeField] float wavelength1 = 28f;
        [SerializeField] float wavelength2 = 17f;
        [SerializeField] float wavelength3 = 9f;
        [SerializeField] float speed1 = 0.55f;
        [SerializeField] float speed2 = 0.8f;
        [SerializeField] float speed3 = 1.15f;
        [SerializeField] Vector2 dir1 = new Vector2(1f, 0.35f);
        [SerializeField] Vector2 dir2 = new Vector2(-0.6f, 1f);
        [SerializeField] Vector2 dir3 = new Vector2(0.4f, -0.85f);

        Mesh mesh;
        Vector3[] baseVerts;
        Vector3[] verts;
        Vector3[] normals;
        Material surfaceMat;
        Material underMat;

        public static WaveOceanSurface Create(Transform parent)
        {
            var go = new GameObject("WaveOceanSurface");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, DiveWorldConfig.WaterSurfaceY, 0f);
            var w = go.AddComponent<WaveOceanSurface>();
            w.Build();
            return w;
        }

        void Build()
        {
            float half = DiveWorldConfig.OceanRadius * 1.3f;
            mesh = new Mesh { name = "WaveOceanMesh", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            int res = resolution;
            int vertCount = res * res;
            baseVerts = new Vector3[vertCount];
            verts = new Vector3[vertCount];
            normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var tris = new int[(res - 1) * (res - 1) * 6];

            float step = (half * 2f) / (res - 1);
            for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                int i = x + z * res;
                float wx = -half + x * step;
                float wz = -half + z * step;
                baseVerts[i] = new Vector3(wx, 0f, wz);
                verts[i] = baseVerts[i];
                uvs[i] = new Vector2(x / (float)(res - 1) * 24f, z / (float)(res - 1) * 24f);
                normals[i] = Vector3.up;
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

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            var mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // Clear blue-green sheet — transparent enough to read the sand below.
            surfaceMat = MakeWaterMaterial(new Color(0.22f, 0.58f, 0.72f, 0.26f));
            mr.sharedMaterial = surfaceMat;

            // Underside facing down for when looking up from below.
            var under = new GameObject("WaveUnderside");
            under.transform.SetParent(transform, false);
            under.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            var umf = under.AddComponent<MeshFilter>();
            umf.sharedMesh = mesh;
            var umr = under.AddComponent<MeshRenderer>();
            umr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            underMat = MakeWaterMaterial(new Color(0.16f, 0.45f, 0.62f, 0.2f));
            umr.sharedMaterial = underMat;

            // No MeshCollider — WaterVolume owns the logical surface height so the diver can cross it.
        }

        static Material MakeWaterMaterial(Color tint)
        {
            var shader = Shader.Find("Underwater/WaterSurface");
            if (shader != null)
            {
                var mat = new Material(shader) { name = "WaveWaterMat" };
                mat.SetColor("_Color", tint);
                mat.SetTexture("_MainTex", ProceduralTextures.WaterAlbedo);
                mat.SetTexture("_BumpMap", ProceduralTextures.FishScaleNormal);
                mat.SetFloat("_BumpScale", 1.3f);
                mat.mainTextureScale = new Vector2(18f, 18f);
                return mat;
            }
            return ProceduralTextures.MakeTransparentLit(ProceduralTextures.WaterAlbedo, tint, 0.9f);
        }

        void LateUpdate()
        {
            if (mesh == null || baseVerts == null)
                return;

            float time = Time.time;
            Vector2 d1 = dir1.normalized;
            Vector2 d2 = dir2.normalized;
            Vector2 d3 = dir3.normalized;
            float k1 = 2f * Mathf.PI / Mathf.Max(0.01f, wavelength1);
            float k2 = 2f * Mathf.PI / Mathf.Max(0.01f, wavelength2);
            float k3 = 2f * Mathf.PI / Mathf.Max(0.01f, wavelength3);

            int res = resolution;
            for (int i = 0; i < baseVerts.Length; i++)
            {
                Vector3 b = baseVerts[i];
                float x = b.x;
                float z = b.z;

                // Gerstner-ish: vertical sine + small horizontal drift.
                float s1 = Mathf.Sin(k1 * (x * d1.x + z * d1.y) + time * speed1);
                float s2 = Mathf.Sin(k2 * (x * d2.x + z * d2.y) + time * speed2);
                float s3 = Mathf.Sin(k3 * (x * d3.x + z * d3.y) + time * speed3);
                float c1 = Mathf.Cos(k1 * (x * d1.x + z * d1.y) + time * speed1);
                float c2 = Mathf.Cos(k2 * (x * d2.x + z * d2.y) + time * speed2);

                float y = amplitude1 * s1 + amplitude2 * s2 + amplitude3 * s3;
                float dx = -d1.x * amplitude1 * c1 * 0.35f - d2.x * amplitude2 * c2 * 0.25f;
                float dz = -d1.y * amplitude1 * c1 * 0.35f - d2.y * amplitude2 * c2 * 0.25f;
                verts[i] = new Vector3(x + dx, y, z + dz);
            }

            // Approximate normals from finite differences on the grid.
            for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                int i = x + z * res;
                int xl = Mathf.Max(0, x - 1) + z * res;
                int xr = Mathf.Min(res - 1, x + 1) + z * res;
                int zd = x + Mathf.Max(0, z - 1) * res;
                int zu = x + Mathf.Min(res - 1, z + 1) * res;
                Vector3 tx = verts[xr] - verts[xl];
                Vector3 tz = verts[zu] - verts[zd];
                normals[i] = Vector3.Cross(tz, tx).normalized;
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.RecalculateBounds();
        }
    }
}
