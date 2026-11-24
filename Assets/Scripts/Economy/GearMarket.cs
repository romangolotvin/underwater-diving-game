using UnityEngine;
using UnityEngine.UI;

namespace UnderwaterDiving
{
    /// <summary>
    /// Surface gear market: sell catch, buy suit / speargun upgrades.
    /// </summary>
    public class GearMarket : MonoBehaviour
    {
        public static GearMarket Instance { get; private set; }

        GameObject panel;
        Text statusText;
        Text listText;
        bool open;
        bool playerNear;
        int selected;

        public static GearMarket Create(Transform parent)
        {
            var go = BuildStall(parent, DiveWorldConfig.GearMarketPos, "GearMarket", new Color(0.2f, 0.45f, 0.55f));
            var m = go.AddComponent<GearMarket>();
            m.BuildUi();
            return m;
        }

        public static GameObject BuildStall(Transform parent, Vector3 pos, string name, Color tint)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;

            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deck";
            deck.transform.SetParent(root.transform, false);
            deck.transform.localScale = new Vector3(5.5f, 0.35f, 4f);
            deck.GetComponent<MeshRenderer>().sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                tint, 0.35f, 0.15f, new Vector2(2f, 2f));

            var mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Mast";
            Object.Destroy(mast.GetComponent<Collider>());
            mast.transform.SetParent(root.transform, false);
            mast.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            mast.transform.localScale = new Vector3(0.15f, 1.5f, 0.15f);
            mast.GetComponent<MeshRenderer>().sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.SuitAlbedo, ProceduralTextures.SuitNormal,
                Color.Lerp(tint, Color.white, 0.2f), 0.4f, 0.1f, new Vector2(1f, 1f));

            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            Object.Destroy(flag.GetComponent<Collider>());
            flag.transform.SetParent(root.transform, false);
            flag.transform.localPosition = new Vector3(0.55f, 2.7f, 0f);
            flag.transform.localScale = new Vector3(1.1f, 0.55f, 0.05f);
            flag.GetComponent<MeshRenderer>().sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                tint, 0.3f, 0.05f, new Vector2(1f, 1f));

            var buoy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            buoy.name = "Buoy";
            Object.Destroy(buoy.GetComponent<Collider>());
            buoy.transform.SetParent(root.transform, false);
            buoy.transform.localPosition = new Vector3(2.2f, 0.5f, 1.4f);
            buoy.transform.localScale = Vector3.one * 0.7f;
            buoy.GetComponent<MeshRenderer>().sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(0.85f, 0.35f, 0.2f), 0.4f, 0.1f, new Vector2(1f, 1f));

            return root;
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
            Transform root = canvas != null ? canvas.transform : CreateFallbackCanvas().transform;

            var bg = ShopUiUtil.MakePanel(root, "GearMarketPanel", new Vector2(0.2f, 0.14f), new Vector2(0.8f, 0.86f),
                new Color(0.04f, 0.08f, 0.12f, 0.94f));
            panel = bg.gameObject;
            panel.SetActive(false);

            ShopUiUtil.MakeText(panel.transform, "Title", new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.98f), 32,
                TextAnchor.MiddleCenter, "Harbor Market");
            statusText = ShopUiUtil.MakeText(panel.transform, "Status", new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.9f), 20,
                TextAnchor.MiddleCenter, "");
            listText = ShopUiUtil.MakeText(panel.transform, "List", new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.83f), 20,
                TextAnchor.UpperLeft, "");

            ShopUiUtil.MakeButton(panel.transform, "SellBtn", new Vector2(0.06f, 0.16f), new Vector2(0.36f, 0.25f),
                "Sell Catch", SellCatch);
            ShopUiUtil.MakeButton(panel.transform, "PrevBtn", new Vector2(0.38f, 0.16f), new Vector2(0.48f, 0.25f),
                "<", () => Cycle(-1));
            ShopUiUtil.MakeButton(panel.transform, "BuyBtn", new Vector2(0.5f, 0.16f), new Vector2(0.74f, 0.25f),
                "Buy Upgrade", BuySelected);
            ShopUiUtil.MakeButton(panel.transform, "NextBtn", new Vector2(0.76f, 0.16f), new Vector2(0.86f, 0.25f),
                ">", () => Cycle(1));
            ShopUiUtil.MakeButton(panel.transform, "CloseBtn", new Vector2(0.3f, 0.04f), new Vector2(0.7f, 0.12f),
                "Close", Close);
        }

        static Canvas CreateFallbackCanvas()
        {
            var go = new GameObject("ShopCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        void Update()
        {
            var swim = SwimController.Instance;
            playerNear = false;
            if (swim != null)
            {
                float d = Vector3.Distance(swim.transform.position, transform.position);
                playerNear = d <= DiveWorldConfig.ShopInteractRadius && swim.transform.position.y > -3.5f;
            }

            if (playerNear && !open && !GameUi.BlocksGameplay && Input.GetKeyDown(KeyCode.F))
                Open();
            if (open && Input.GetKeyDown(KeyCode.Escape))
                Close();
            if (open)
                Refresh();
        }

        public bool PlayerNear => playerNear;

        void Open()
        {
            open = true;
            panel.SetActive(true);
            GameUi.SetBlocking(true);
            Refresh();
        }

        void Close()
        {
            open = false;
            if (panel != null)
                panel.SetActive(false);
            GameUi.SetBlocking(false);
        }

        void Cycle(int delta)
        {
            int n = ItemCatalog.AllGear.Length;
            selected = (selected + delta + n) % n;
            Refresh();
        }

        void SellCatch()
        {
            int earned = Inventory.Instance != null ? Inventory.Instance.SellAllCatch() : 0;
            statusText.text = earned > 0 ? $"+{earned} credits" : "Nothing to sell";
            Refresh();
        }

        void BuySelected()
        {
            var id = ItemCatalog.AllGear[selected];
            var shop = ShopProgress.Instance;
            if (shop == null)
                return;
            if (shop.LevelOf(id) >= DiveWorldConfig.MaxUpgradeLevel)
            {
                statusText.text = "Maxed";
                return;
            }
            int price = shop.NextPrice(id);
            if (shop.TryBuy(id))
                statusText.text = $"Bought {ItemCatalog.ForGear(id).DisplayName}";
            else
                statusText.text = $"Need {price} credits";
            Refresh();
        }

        void Refresh()
        {
            var wallet = Wallet.Instance;
            var inv = Inventory.Instance;
            var shop = ShopProgress.Instance;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Credits: {(wallet != null ? wallet.Credits : 0)}   Catch: {(inv != null ? inv.TotalCatchCount() : 0)}/{(inv != null ? inv.MaxCatchSlots() : 0)}");
            sb.AppendLine();
            if (shop != null)
            {
                for (int i = 0; i < ItemCatalog.AllGear.Length; i++)
                {
                    var id = ItemCatalog.AllGear[i];
                    int lv = shop.LevelOf(id);
                    string price = lv >= DiveWorldConfig.MaxUpgradeLevel ? "MAX" : shop.NextPrice(id) + " cr";
                    string mark = i == selected ? "> " : "  ";
                    sb.AppendLine($"{mark}{ItemCatalog.ForGear(id).DisplayName}  Lv {lv}/{DiveWorldConfig.MaxUpgradeLevel}  [{price}]");
                }
            }
            sb.AppendLine();
            sb.AppendLine("Line Length / Harpoon Edge / Harpoon Mass upgrade the speargun.");
            listText.text = sb.ToString();
        }
    }
}
