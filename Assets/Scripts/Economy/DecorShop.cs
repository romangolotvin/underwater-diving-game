using UnityEngine;
using UnityEngine.UI;

namespace UnderwaterDiving
{
    /// <summary>
    /// Beach outfitter on the left of the shop arc.
    /// </summary>
    public class DecorShop : MonoBehaviour
    {
        public static DecorShop Instance { get; private set; }

        GameObject panel;
        Text statusText;
        Text listText;
        bool open;
        bool playerNear;
        int selected;

        public static DecorShop Create(Transform parent)
        {
            var go = GearMarket.BuildStall(parent, DiveWorldConfig.DecorShopPos, "DecorShop",
                new Color(0.55f, 0.35f, 0.45f), DiveWorldConfig.DecorShopYaw);
            var m = go.AddComponent<DecorShop>();
            m.BuildUi();
            return m;
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void BuildUi()
        {
            ShopUiUtil.EnsureEventSystem();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("DecorCanvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 121;
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                go.AddComponent<GraphicRaycaster>();
            }

            var bg = ShopUiUtil.MakePanel(canvas.transform, "DecorShopPanel", new Vector2(0.24f, 0.2f), new Vector2(0.76f, 0.8f),
                new Color(0.1f, 0.05f, 0.1f, 0.94f), raycast: true);
            panel = bg.gameObject;
            panel.SetActive(false);

            ShopUiUtil.MakeText(panel.transform, "Title", new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f), 30,
                TextAnchor.MiddleCenter, "Outfitter");
            statusText = ShopUiUtil.MakeText(panel.transform, "Status", new Vector2(0.05f, 0.8f), new Vector2(0.95f, 0.87f), 20,
                TextAnchor.MiddleCenter, "");
            listText = ShopUiUtil.MakeText(panel.transform, "List", new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.78f), 22,
                TextAnchor.UpperLeft, "");

            ShopUiUtil.MakeButton(panel.transform, "PrevBtn", new Vector2(0.1f, 0.14f), new Vector2(0.22f, 0.24f),
                "<", () => Cycle(-1));
            ShopUiUtil.MakeButton(panel.transform, "BuyBtn", new Vector2(0.24f, 0.14f), new Vector2(0.76f, 0.24f),
                "Buy / Equip", BuyOrEquipSelected);
            ShopUiUtil.MakeButton(panel.transform, "NextBtn", new Vector2(0.78f, 0.14f), new Vector2(0.9f, 0.24f),
                ">", () => Cycle(1));
            ShopUiUtil.MakeButton(panel.transform, "CloseBtn", new Vector2(0.3f, 0.04f), new Vector2(0.7f, 0.12f),
                "Close", Close);
        }

        void Update()
        {
            var swim = SwimController.Instance;
            playerNear = false;
            if (swim != null)
            {
                float d = Vector3.Distance(swim.transform.position, transform.position);
                playerNear = d <= DiveWorldConfig.ShopInteractRadius;
            }

            if (playerNear && !open && !GameUi.BlocksGameplay && ShopInteract.WantsOpen(swim))
                Open();
            if (open && GameUi.TryConsumeEscape())
                Close();
            if (open)
                Refresh();
        }

        public bool PlayerNear => playerNear;
        /// <summary>True only while the outfitter panel GameObject is active in hierarchy.</summary>
        public bool IsOpen => open && panel != null && panel.activeInHierarchy;
        /// <summary>Panel root for <see cref="GameUi.AnyModalPanelOpen"/> live checks.</summary>
        public GameObject PanelRoot => panel;

        void Open()
        {
            if (open) return;
            open = true;
            if (panel != null)
            {
                var img = panel.GetComponent<Image>();
                if (img != null) img.raycastTarget = true;
                panel.SetActive(true);
            }
            GameUi.SetBlocked(GameUi.Blocker.DecorShop, true);
            Refresh();
        }

        void Close()
        {
            if (!open) return;
            open = false;
            if (panel != null)
            {
                var img = panel.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
                panel.SetActive(false);
            }
            GameUi.SetBlocked(GameUi.Blocker.DecorShop, false);
        }

        void OnDisable()
        {
            open = false;
            GameUi.SetBlocked(GameUi.Blocker.DecorShop, false);
        }

        void LateUpdate()
        {
            // Only assert while visibly open — never SetBlocked(false) here (WASD sticky mask).
            if (IsOpen)
                GameUi.SetBlocked(GameUi.Blocker.DecorShop, true);
            else if (open)
                open = false;
        }

        void Cycle(int delta)
        {
            int n = ItemCatalog.AllCosmetics.Length;
            selected = (selected + delta + n) % n;
            Refresh();
        }

        void BuyOrEquipSelected()
        {
            var id = ItemCatalog.AllCosmetics[selected];
            var locker = CosmeticLocker.Instance;
            if (locker == null)
                return;

            if (locker.Owns(id))
            {
                locker.Equip(id);
                statusText.text = $"Equipped {ItemCatalog.ForCosmetic(id).DisplayName}";
            }
            else if (locker.TryBuy(id))
            {
                statusText.text = $"Bought {ItemCatalog.ForCosmetic(id).DisplayName}";
            }
            else
            {
                statusText.text = $"Need {ItemCatalog.ForCosmetic(id).BuyPrice} credits";
            }
            Refresh();
        }

        void Refresh()
        {
            var wallet = Wallet.Instance;
            var locker = CosmeticLocker.Instance;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Credits: {(wallet != null ? wallet.Credits : 0)}");
            sb.AppendLine();
            for (int i = 0; i < ItemCatalog.AllCosmetics.Length; i++)
            {
                var id = ItemCatalog.AllCosmetics[i];
                var def = ItemCatalog.ForCosmetic(id);
                bool own = locker != null && locker.Owns(id);
                bool eq = locker != null && locker.Equipped == id;
                string state = eq ? "EQUIPPED" : own ? "OWNED" : def.BuyPrice + " cr";
                string mark = i == selected ? "> " : "  ";
                sb.AppendLine($"{mark}{def.DisplayName}  [{state}]");
            }
            listText.text = sb.ToString();
        }
    }
}
