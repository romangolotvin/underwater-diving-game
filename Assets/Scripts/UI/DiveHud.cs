using UnityEngine;
using UnityEngine.UI;

namespace UnderwaterDiving
{
    /// <summary>
    /// Minimal dive readouts only — no prompt/meta text.
    /// </summary>
    public class DiveHud : MonoBehaviour
    {
        Text depthText;
        Text airText;
        Image airFill;

        public static DiveHud Create()
        {
            var canvasGo = new GameObject("DiveHUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var hud = canvasGo.AddComponent<DiveHud>();
            hud.Build(canvasGo.transform);
            return hud;
        }

        void Build(Transform root)
        {
            depthText = MakeLabel(root, "DepthReadout", new Vector2(0.02f, 0.92f), new Vector2(0.28f, 0.98f), TextAnchor.UpperLeft);
            airText = MakeLabel(root, "AirReadout", new Vector2(0.72f, 0.92f), new Vector2(0.98f, 0.98f), TextAnchor.UpperRight);

            var barGo = new GameObject("AirBar", typeof(RectTransform));
            barGo.transform.SetParent(root, false);
            var rt = barGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.72f, 0.88f);
            rt.anchorMax = new Vector2(0.98f, 0.91f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bg = barGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(barGo.transform, false);
            var frt = fillGo.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(2, 2);
            frt.offsetMax = new Vector2(-2, -2);
            airFill = fillGo.AddComponent<Image>();
            airFill.color = new Color(0.25f, 0.75f, 0.85f, 0.9f);
            airFill.type = Image.Type.Filled;
            airFill.fillMethod = Image.FillMethod.Horizontal;
            airFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        static Text MakeLabel(Transform parent, string name, Vector2 aMin, Vector2 aMax, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 28;
            text.color = new Color(0.9f, 0.95f, 1f, 0.92f);
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = "";
            return text;
        }

        void LateUpdate()
        {
            var swim = SwimController.Instance;
            var suit = swim != null ? swim.GetComponent<DiverSuit>() : null;
            if (swim == null)
                return;

            float depth = swim.CurrentDepth;
            depthText.text = $"{depth:0.0} m";

            if (suit != null)
            {
                float pct = suit.MaxDiveSeconds > 0.01f ? suit.RemainingDiveSeconds / suit.MaxDiveSeconds : 0f;
                airFill.fillAmount = Mathf.Clamp01(pct);
                airText.text = $"{suit.RemainingDiveSeconds:0}s";
                airFill.color = Color.Lerp(new Color(0.85f, 0.25f, 0.2f), new Color(0.25f, 0.75f, 0.85f), pct);
            }
        }
    }
}
