using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Built-in pipeline color grade on the diver camera. No URP dependency.
    /// Starting-beach look: scene-wide saturation ~+150% (2.5×), easing deeper.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UnderwaterVision : MonoBehaviour
    {
        /// <summary>Neutral sat = 1. Beach target ≈ +150% → 2.5.</summary>
        public const float BeachSaturationBoost = 2.5f;

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

            // Always grade: vivid beach/surface sat, cooler/desat with depth.
            float sat = Mathf.Lerp(BeachSaturationBoost, 0.85f, depth01);
            float contrast = Mathf.Lerp(1.08f, 0.95f, depth01);

            postMat.SetFloat("_Depth", depth01);
            postMat.SetColor("_Tint", Color.Lerp(
                new Color(1f, 0.92f, 0.78f),
                new Color(0.08f, 0.22f, 0.4f),
                depth01));
            postMat.SetFloat("_Absorb", Mathf.Lerp(0.05f, 0.75f, depth01));
            postMat.SetFloat("_Vignette", Mathf.Lerp(0.12f, 0.7f, depth01));
            postMat.SetFloat("_Saturation", sat);
            postMat.SetFloat("_Contrast", contrast);
            Graphics.Blit(src, dest, postMat);
        }
    }
}
