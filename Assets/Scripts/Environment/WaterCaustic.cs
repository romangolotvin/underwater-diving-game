using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Additive caustic light plane for shallow-water shimmer.
    /// </summary>
    public class WaterCaustic : MonoBehaviour
    {
        Material mat;
        Vector2 offset;

        public static WaterCaustic Create(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "WaterCaustic";
            Object.Destroy(go.GetComponent<MeshCollider>());
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            float size = DiveWorldConfig.OceanRadius * 2.2f;
            go.transform.localScale = new Vector3(size, size, 1f);
            go.transform.position = new Vector3(0f, DiveWorldConfig.WaterSurfaceY - 1f, 0f);

            var mat = ProceduralTextures.MakeAdditive(ProceduralTextures.CausticAlbedo, new Color(0.65f, 0.9f, 1f, 0.45f), 0.5f);
            mat.mainTextureScale = new Vector2(18f, 18f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var comp = go.AddComponent<WaterCaustic>();
            comp.mat = mat;
            return comp;
        }

        void Update()
        {
            offset.x += 0.04f * Time.deltaTime;
            offset.y += 0.025f * Time.deltaTime;
            if (mat != null)
                mat.mainTextureOffset = offset;

            var swim = SwimController.Instance;
            if (swim == null || !swim.IsUnderwater)
                return;

            float depth = swim.CurrentDepth;
            float shallow = 1f - Mathf.Clamp01(depth / DiveWorldConfig.MidMaxDepth);
            var c = mat.color;
            c.a = 0.45f * shallow;
            mat.color = c;
        }
    }
}
