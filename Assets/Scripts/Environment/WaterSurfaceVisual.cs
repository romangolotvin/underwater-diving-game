using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Textured water surface with scroll and underside plane.
    /// </summary>
    public class WaterSurfaceVisual : MonoBehaviour
    {
        [SerializeField] float scrollSpeed = 0.03f;
        Material mat;
        Material underMat;
        Vector2 offset;

        public static WaterSurfaceVisual Create(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "WaterSurface";
            Object.Destroy(go.GetComponent<MeshCollider>());
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, DiveWorldConfig.WaterSurfaceY, 0f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            float size = DiveWorldConfig.OceanRadius * 2.6f;
            go.transform.localScale = new Vector3(size, size, 1f);

            var mat = ProceduralTextures.MakeTransparentLit(
                ProceduralTextures.WaterAlbedo,
                new Color(1f, 1f, 1f, 0.55f),
                0.9f);
            mat.mainTextureScale = new Vector2(32f, 32f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var under = GameObject.CreatePrimitive(PrimitiveType.Quad);
            under.name = "WaterUnderside";
            Object.Destroy(under.GetComponent<MeshCollider>());
            under.transform.SetParent(go.transform, false);
            under.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            under.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            under.transform.localScale = Vector3.one;
            var um = ProceduralTextures.MakeTransparentLit(
                ProceduralTextures.WaterAlbedo,
                new Color(0.7f, 0.9f, 1f, 0.4f),
                0.75f);
            um.mainTextureScale = new Vector2(28f, 28f);
            under.GetComponent<MeshRenderer>().sharedMaterial = um;
            under.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var comp = go.AddComponent<WaterSurfaceVisual>();
            comp.mat = mat;
            comp.underMat = um;
            return comp;
        }

        void Update()
        {
            offset.x += scrollSpeed * Time.deltaTime;
            offset.y += scrollSpeed * 0.65f * Time.deltaTime;
            if (mat != null)
                mat.mainTextureOffset = offset;
            if (underMat != null)
                underMat.mainTextureOffset = -offset * 0.7f;

            var p = transform.position;
            p.y = DiveWorldConfig.WaterSurfaceY + Mathf.Sin(Time.time * 0.35f) * 0.05f;
            transform.position = p;
        }
    }
}
