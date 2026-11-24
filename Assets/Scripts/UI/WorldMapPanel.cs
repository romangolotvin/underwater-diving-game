using UnityEngine;
using UnityEngine.UI;

namespace UnderwaterDiving
{
    /// <summary>
    /// Simple Canvas world map: toggle panel, clickable region pins, teleport to spawn.
    /// Locations come from DiveWorldConfig (Open Ocean) + DiveRegionCatalog country sites.
    /// </summary>
    public class WorldMapPanel : MonoBehaviour
    {
        struct MapSite
        {
            public string Label;
            public string Subtitle;
            public Vector3 WorldPos;
            public Vector2 Anchor;
        }

        GameObject panelRoot;
        Button toggleButton;
        Text toggleLabel;
        bool open;

        public static WorldMapPanel Attach(Transform hudRoot)
        {
            ShopUiUtil.EnsureEventSystem();
            var panel = hudRoot.gameObject.AddComponent<WorldMapPanel>();
            panel.Build(hudRoot);
            return panel;
        }

        void Build(Transform hudRoot)
        {
            toggleButton = ShopUiUtil.MakeButton(
                hudRoot,
                "MapToggle",
                new Vector2(0.42f, 0.02f),
                new Vector2(0.58f, 0.07f),
                "Карта",
                Toggle);

            toggleLabel = toggleButton.GetComponentInChildren<Text>();

            var backdrop = ShopUiUtil.MakePanel(
                hudRoot,
                "WorldMapPanel",
                new Vector2(0.12f, 0.12f),
                new Vector2(0.88f, 0.88f),
                new Color(0.02f, 0.08f, 0.14f, 0.94f));
            panelRoot = backdrop.gameObject;

            ShopUiUtil.MakeText(
                panelRoot.transform,
                "Title",
                new Vector2(0.05f, 0.9f),
                new Vector2(0.75f, 0.98f),
                30,
                TextAnchor.MiddleLeft,
                "World Map / Карта мира");

            ShopUiUtil.MakeButton(
                panelRoot.transform,
                "CloseMap",
                new Vector2(0.82f, 0.9f),
                new Vector2(0.97f, 0.97f),
                "✕",
                Close);

            var mapArea = ShopUiUtil.MakePanel(
                panelRoot.transform,
                "MapArea",
                new Vector2(0.04f, 0.08f),
                new Vector2(0.96f, 0.88f),
                new Color(0.04f, 0.22f, 0.32f, 0.95f));

            // Soft grid lines for a readable generated map.
            for (int i = 1; i <= 3; i++)
            {
                float t = i / 4f;
                ShopUiUtil.MakePanel(mapArea.transform, $"HGrid{i}",
                    new Vector2(0.05f, t - 0.002f), new Vector2(0.95f, t + 0.002f),
                    new Color(0.3f, 0.55f, 0.65f, 0.25f));
                ShopUiUtil.MakePanel(mapArea.transform, $"VGrid{i}",
                    new Vector2(t - 0.002f, 0.05f), new Vector2(t + 0.002f, 0.95f),
                    new Color(0.3f, 0.55f, 0.65f, 0.25f));
            }

            ShopUiUtil.MakeText(
                mapArea.transform,
                "Hint",
                new Vector2(0.05f, 0.02f),
                new Vector2(0.95f, 0.1f),
                18,
                TextAnchor.MiddleCenter,
                "Click a site to travel · Клик по точке — телепорт");

            foreach (var site in BuildSites())
                CreatePin(mapArea.transform, site);

            panelRoot.SetActive(false);
        }

        static MapSite[] BuildSites()
        {
            var regions = DiveRegionCatalog.All;
            var sites = new MapSite[1 + regions.Length];
            sites[0] = new MapSite
            {
                Label = "Open Ocean",
                Subtitle = "Default dive",
                WorldPos = DiveWorldConfig.PlayerSpawn,
                Anchor = new Vector2(0.48f, 0.42f)
            };
            for (int i = 0; i < regions.Length; i++)
            {
                var r = regions[i];
                sites[i + 1] = new MapSite
                {
                    Label = r.CountryName,
                    Subtitle = r.WaterName,
                    WorldPos = r.Spawn,
                    Anchor = r.MapAnchor
                };
            }
            return sites;
        }

        void CreatePin(Transform mapArea, MapSite site)
        {
            const float half = 0.045f;
            var aMin = new Vector2(site.Anchor.x - half, site.Anchor.y - half);
            var aMax = new Vector2(site.Anchor.x + half, site.Anchor.y + half);

            var pin = ShopUiUtil.MakePanel(
                mapArea,
                $"Pin_{site.Label}",
                aMin,
                aMax,
                new Color(0.85f, 0.55f, 0.2f, 0.95f));

            var btn = pin.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 0.75f, 0.35f, 1f);
            colors.pressedColor = new Color(0.65f, 0.35f, 0.1f, 1f);
            btn.colors = colors;

            ShopUiUtil.MakeText(
                pin.transform,
                "Dot",
                Vector2.zero,
                Vector2.one,
                16,
                TextAnchor.MiddleCenter,
                "●");

            var caption = ShopUiUtil.MakeText(
                mapArea,
                $"Caption_{site.Label}",
                new Vector2(site.Anchor.x - 0.08f, site.Anchor.y - 0.11f),
                new Vector2(site.Anchor.x + 0.08f, site.Anchor.y - 0.05f),
                14,
                TextAnchor.UpperCenter,
                $"{site.Label}\n{site.Subtitle}");
            caption.color = new Color(0.85f, 0.95f, 1f, 0.9f);

            Vector3 dest = site.WorldPos;
            string name = site.Label;
            btn.onClick.AddListener(() => TeleportTo(dest, name));
        }

        void Toggle()
        {
            if (open) Close();
            else Open();
        }

        void Open()
        {
            if (open) return;
            open = true;
            panelRoot.SetActive(true);
            if (toggleLabel != null) toggleLabel.text = "Закрыть";
            GameUi.SetBlocking(true);
        }

        void Close()
        {
            if (!open) return;
            open = false;
            panelRoot.SetActive(false);
            if (toggleLabel != null) toggleLabel.text = "Карта";
            GameUi.SetBlocking(false);
        }

        void TeleportTo(Vector3 worldPos, string siteName)
        {
            var swim = SwimController.Instance;
            if (swim == null)
            {
                Debug.LogWarning($"WorldMap: no player to teleport to {siteName}");
                return;
            }

            var rb = swim.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = worldPos;
            }
            swim.transform.position = worldPos;
            Close();
        }

        void Update()
        {
            // Consume Escape before SwimController unlocks cursor on the same key.
            if (open && GameUi.TryConsumeEscape())
                Close();
        }
    }
}
