using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Built-in pipeline depth color grade on the diver camera. No URP dependency.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UnderwaterVision : MonoBehaviour
    {
        Material postMat;
        float depth01;

        public static UnderwaterVision Attach(Camera cam)
        {
            if (cam == null)
                return null;
            var fx = cam.GetComponent<UnderwaterVision>();
            if (fx == null)
                fx = cam.gameObject.AddComponent<UnderwaterVision>();
            return fx;
        }

        void Awake()
        {
            var shader = Shader.Find("Underwater/DepthGrade");
            if (shader != null)
                postMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        void OnDestroy()
        {
            if (postMat != null)
                Destroy(postMat);
        }

        void LateUpdate()
        {
            var swim = SwimController.Instance;
            if (swim == null || !swim.IsUnderwater)
            {
                depth01 = Mathf.MoveTowards(depth01, 0f, Time.deltaTime * 1.5f);
                return;
            }

            float target = Mathf.Clamp01(swim.CurrentDepth / DiveWorldConfig.MaxDepthMeters);
            depth01 = Mathf.Lerp(depth01, target, Time.deltaTime * 2.5f);
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (postMat == null || depth01 < 0.001f)
            {
                Graphics.Blit(src, dest);
                return;
            }

            postMat.SetFloat("_Depth", depth01);
            postMat.SetColor("_Tint", Color.Lerp(
                new Color(0.35f, 0.7f, 0.75f),
                new Color(0.08f, 0.22f, 0.4f),
                depth01));
            postMat.SetFloat("_Absorb", Mathf.Lerp(0.2f, 0.75f, depth01));
            postMat.SetFloat("_Vignette", Mathf.Lerp(0.2f, 0.7f, depth01));
            postMat.SetFloat("_Saturation", Mathf.Lerp(1.05f, 0.7f, depth01));
            postMat.SetFloat("_Contrast", Mathf.Lerp(1.05f, 0.95f, depth01));
            Graphics.Blit(src, dest, postMat);
        }
    }
}
