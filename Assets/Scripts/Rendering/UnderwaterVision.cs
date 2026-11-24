using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Built-in pipeline color grade on the diver camera. No URP dependency.
    /// Starting-beach look: soft pastel sat (~1.4×), easing cooler/deeper.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UnderwaterVision : MonoBehaviour
    {
        /// <summary>Neutral sat = 1. Pastel beach boost (was 2.5 — too vivid).</summary>
        public const float BeachSaturationBoost = 1.4f;

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
            if (postMat == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            // Always grade: soft pastel beach sat, cooler/desat with depth.
            float sat = Mathf.Lerp(BeachSaturationBoost, 0.9f, depth01);
            float contrast = Mathf.Lerp(1.02f, 0.96f, depth01);

            postMat.SetFloat("_Depth", depth01);
            postMat.SetColor("_Tint", Color.Lerp(
                new Color(1f, 0.97f, 0.92f),
                new Color(0.12f, 0.28f, 0.45f),
                depth01));
            postMat.SetFloat("_Absorb", Mathf.Lerp(0.03f, 0.7f, depth01));
            postMat.SetFloat("_Vignette", Mathf.Lerp(0.08f, 0.65f, depth01));
            postMat.SetFloat("_Saturation", sat);
            postMat.SetFloat("_Contrast", contrast);
            Graphics.Blit(src, dest, postMat);
        }
    }
}
