using UnityEngine;
using UnityEngine.UI;

namespace UnderwaterDiving
{
    public static class ShopUiUtil
    {
        public static Font Font =>
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        public static Text MakeText(Transform parent, string name, Vector2 aMin, Vector2 aMax, int size, TextAnchor anchor, string content = "")
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = go.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.color = new Color(0.9f, 0.95f, 1f, 0.95f);
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;
            return text;
        }

        public static Image MakePanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Button MakeButton(Transform parent, string name, Vector2 aMin, Vector2 aMax, string label, UnityEngine.Events.UnityAction onClick)
        {
            var img = MakePanel(parent, name, aMin, aMax, new Color(0.12f, 0.28f, 0.36f, 0.92f));
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.18f, 0.42f, 0.5f, 1f);
            colors.pressedColor = new Color(0.08f, 0.2f, 0.26f, 1f);
            btn.colors = colors;
            MakeText(img.transform, "Label", Vector2.zero, Vector2.one, 20, TextAnchor.MiddleCenter, label);
            btn.onClick.AddListener(onClick);
            return btn;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
                return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}
