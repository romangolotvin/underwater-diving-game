using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnderwaterDiving
{
    /// <summary>
    /// Simple Canvas world map: toggle panel (M or UI button), clickable region pins, teleport to spawn.
    /// Locations come from DiveWorldConfig (Open Ocean) + DiveRegionCatalog country sites.
    /// </summary>
    [DefaultExecutionOrder(40)] // After SwimController (-100); M must not wait on BlocksGameplay
    public class WorldMapPanel : MonoBehaviour
    {
        const KeyCode MapToggleKey = KeyCode.M;

        struct MapSite
        {
            public string Label;
            public string Subtitle;
            public Vector3 WorldPos;
            public Vector2 Anchor;
        }

        public static WorldMapPanel Instance { get; private set; }

        GameObject panelRoot;
        Button toggleButton;
        Text toggleLabel;
        bool open;

        /// <summary>True only while the map panel is actually visible in the hierarchy.</summary>
        public bool IsOpen => open && panelRoot != null && panelRoot.activeInHierarchy;

        public static WorldMapPanel Attach(Transform hudRoot)
        {
            ShopUiUtil.EnsureEventSystem();
            var panel = hudRoot.gameObject.AddComponent<WorldMapPanel>();
            Instance = panel;
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
            DisableNavigation(toggleButton);

            toggleLabel = toggleButton.GetComponentInChildren<Text>();

            // Decorative backdrop — no raycast needed; buttons/pins handle clicks.
            var backdrop = ShopUiUtil.MakePanel(
                hudRoot,
                "WorldMapPanel",
                new Vector2(0.12f, 0.12f),
                new Vector2(0.88f, 0.88f),
                new Color(0.02f, 0.08f, 0.14f, 0.94f),
                raycast: false);
            panelRoot = backdrop.gameObject;

            ShopUiUtil.MakeText(
                panelRoot.transform,
                "Title",
                new Vector2(0.05f, 0.9f),
                new Vector2(0.75f, 0.98f),
                30,
                TextAnchor.MiddleLeft,
                "World Map / Карта мира");

            var closeBtn = ShopUiUtil.MakeButton(
                panelRoot.transform,
                "CloseMap",
                new Vector2(0.82f, 0.9f),
                new Vector2(0.97f, 0.97f),
                "✕",
                Close);
            DisableNavigation(closeBtn);

            var mapArea = ShopUiUtil.MakePanel(
                panelRoot.transform,
                "MapArea",
                new Vector2(0.04f, 0.08f),
                new Vector2(0.96f, 0.88f),
                new Color(0.04f, 0.22f, 0.32f, 0.95f),
                raycast: false);

            // Soft grid lines for a readable generated map.
            for (int i = 1; i <= 3; i++)
            {
                float t = i / 4f;
                ShopUiUtil.MakePanel(mapArea.transform, $"HGrid{i}",
                    new Vector2(0.05f, t - 0.002f), new Vector2(0.95f, t + 0.002f),
                    new Color(0.3f, 0.55f, 0.65f, 0.25f), raycast: false);
                ShopUiUtil.MakePanel(mapArea.transform, $"VGrid{i}",
                    new Vector2(t - 0.002f, 0.05f), new Vector2(t + 0.002f, 0.95f),
                    new Color(0.3f, 0.55f, 0.65f, 0.25f), raycast: false);
            }

            ShopUiUtil.MakeText(
                mapArea.transform,
                "Hint",
                new Vector2(0.05f, 0.02f),
                new Vector2(0.95f, 0.1f),
                18,
                TextAnchor.MiddleCenter,
                "Click a site to travel · Клик по точке — телепорт · M — map");

            foreach (var site in BuildSites())
                CreatePin(mapArea.transform, site);

            panelRoot.SetActive(false);
            open = false;
            GameUi.SetBlocked(GameUi.Blocker.Map, false);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        static void DisableNavigation(Selectable selectable)
        {
            if (selectable == null) return;
            var nav = selectable.navigation;
            nav.mode = Navigation.Mode.None;
            selectable.navigation = nav;
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

            // Pins need raycast — they are the Selectable hit target.
            var pin = ShopUiUtil.MakePanel(
                mapArea,
                $"Pin_{site.Label}",
                aMin,
                aMax,
                new Color(0.85f, 0.55f, 0.2f, 0.95f),
                raycast: true);

            var btn = pin.gameObject.AddComponent<Button>();
            DisableNavigation(btn);
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
            if (panelRoot != null)
                panelRoot.SetActive(true);
            if (toggleLabel != null) toggleLabel.text = "Закрыть";
            GameUi.SetBlocked(GameUi.Blocker.Map, true);
        }

        void Close()
        {
            if (!open) return;
            open = false;
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (toggleLabel != null) toggleLabel.text = "Карта";
            GameUi.SetBlocked(GameUi.Blocker.Map, false);
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
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = worldPos;
                rb.WakeUp();
            }
            swim.transform.position = worldPos;
            swim.ClearNearbyTrapColliders(12f);
            swim.UnstickFromGeometry();
            Close();
            GameUi.ClearBlocking();
        }

        void OnDisable()
        {
            // Never leave Map bit stuck if the HUD is torn down while open.
            open = false;
            GameUi.SetBlocked(GameUi.Blocker.Map, false);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            open = false;
            GameUi.SetBlocked(GameUi.Blocker.Map, false);
        }

        void LateUpdate()
        {
            // Only assert while open — clearing here raced ReconcileLive / WASD.
            if (IsOpen)
                GameUi.SetBlocked(GameUi.Blocker.Map, true);
            else if (open)
                open = false;
        }

        void Update()
        {
            // Consume Escape before SwimController unlocks cursor on the same key.
            if (open && GameUi.TryConsumeEscape())
            {
                Close();
                return;
            }

            if (!Input.GetKeyDown(MapToggleKey))
                return;

            // M must toggle from live open state — never from GameUi.BlocksGameplay.
            // A stale mask (shop LateUpdate / Update-order vs ReconcileLive) used to
            // swallow Open while every panel was already closed.
            if (open)
                Close();
            else
                Open();
        }
    }
}
