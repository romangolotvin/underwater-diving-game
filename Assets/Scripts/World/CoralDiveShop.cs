using UnityEngine;
using UnityEngine.UI;

namespace UnderwaterDiving
{
    /// <summary>
    /// Small buy/sell stall nestled in deep coral, staffed by a diver NPC.
    /// Independent from the main GearMarket / DecorShop platforms.
    /// </summary>
    public class CoralDiveShop : MonoBehaviour
    {
        static readonly string[] VendorNames =
        {
            "Reef Trader", "Abyss Vendor", "Coral Merchant", "Drift Seller", "Kelp Broker", "Deep Peddler"
        };

        GameObject panel;
        Text statusText;
        Text listText;
        bool open;
        bool playerNear;
        string vendorName;
        int selected;

        // Compact stock: sell catch + a few cheap useful buys.
        enum Stock
        {
            SellCatch,
            AirTopUp,
            SpearRepairKit,
            GlowFlare
        }

        static readonly Stock[] Menu =
        {
            Stock.SellCatch,
            Stock.AirTopUp,
            Stock.SpearRepairKit,
            Stock.GlowFlare
        };

        public static CoralDiveShop Create(Transform parent, Vector3 pos, int index, System.Random rng)
        {
            var root = new GameObject("CoralDiveShop_" + index);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;

            var stallMat = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                new Color(0.35f, 0.4f, 0.42f), 0.4f, 0.2f, new Vector2(2f, 2f));
            var clothMat = ProceduralTextures.MakeLit(
                ProceduralTextures.CoralAlbedo, ProceduralTextures.CoralNormal,
                new Color(0.2f, 0.55f, 0.5f), 0.3f, 0.05f, new Vector2(1f, 1f));

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "StallFloor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localScale = new Vector3(3.2f, 0.25f, 2.6f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = stallMat;

            var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Counter";
            Object.Destroy(counter.GetComponent<Collider>());
            counter.transform.SetParent(root.transform, false);
            counter.transform.localPosition = new Vector3(0f, 0.55f, 0.7f);
            counter.transform.localScale = new Vector3(2.4f, 0.7f, 0.5f);
            counter.GetComponent<MeshRenderer>().sharedMaterial = stallMat;

            var awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
            awning.name = "Awning";
            Object.Destroy(awning.GetComponent<Collider>());
            awning.transform.SetParent(root.transform, false);
            awning.transform.localPosition = new Vector3(0f, 1.5f, 0.2f);
            awning.transform.localScale = new Vector3(3.0f, 0.08f, 2.4f);
            awning.GetComponent<MeshRenderer>().sharedMaterial = clothMat;

            var lamp = new GameObject("StallLamp");
            lamp.transform.SetParent(root.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 9f;
            light.intensity = 1.6f;
            light.color = new Color(0.45f, 0.85f, 0.9f);

            CreateVendorNpc(root.transform, VendorNames[index % VendorNames.Length]);

            var shop = root.AddComponent<CoralDiveShop>();
            shop.vendorName = VendorNames[index % VendorNames.Length];
            shop.BuildUi();
            return shop;
        }

        static void CreateVendorNpc(Transform parent, string name)
        {
            var npc = new GameObject("Vendor_" + name);
            npc.transform.SetParent(parent, false);
            npc.transform.localPosition = new Vector3(0f, 1.05f, 0.15f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(npc.transform, false);
            body.transform.localScale = new Vector3(0.55f, 0.7f, 0.55f);
            body.GetComponent<MeshRenderer>().sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.SuitAlbedo, ProceduralTextures.SuitNormal,
                Color.white, 0.4f, 0.15f, new Vector2(1.5f, 1.5f));

            var tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tank.name = "Tank";
            Object.Destroy(tank.GetComponent<Collider>());
            tank.transform.SetParent(npc.transform, false);
            tank.transform.localPosition = new Vector3(0f, 0.15f, -0.22f);
            tank.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tank.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
            tank.GetComponent<MeshRenderer>().sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                new Color(0.7f, 0.75f, 0.8f), 0.55f, 0.45f, new Vector2(1f, 1f));

            var helm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            helm.name = "Helmet";
            Object.Destroy(helm.GetComponent<Collider>());
            helm.transform.SetParent(npc.transform, false);
            helm.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            helm.transform.localScale = Vector3.one * 0.42f;
            helm.GetComponent<MeshRenderer>().sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                new Color(0.55f, 0.7f, 0.85f), 0.75f, 0.35f, new Vector2(1f, 1f));

            npc.AddComponent<VendorBob>();
        }

        void BuildUi()
        {
            ShopUiUtil.EnsureEventSystem();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("CoralShopCanvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 125;
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                go.AddComponent<GraphicRaycaster>();
            }

            var bg = ShopUiUtil.MakePanel(canvas.transform, "CoralShopPanel_" + vendorName,
                new Vector2(0.28f, 0.22f), new Vector2(0.72f, 0.78f),
                new Color(0.03f, 0.1f, 0.12f, 0.94f), raycast: true);
            panel = bg.gameObject;
            panel.SetActive(false);

            ShopUiUtil.MakeText(panel.transform, "Title", new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.96f), 26,
                TextAnchor.MiddleCenter, vendorName);
            statusText = ShopUiUtil.MakeText(panel.transform, "Status", new Vector2(0.06f, 0.76f), new Vector2(0.94f, 0.85f), 18,
                TextAnchor.MiddleCenter, "");
            listText = ShopUiUtil.MakeText(panel.transform, "List", new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.74f), 20,
                TextAnchor.UpperLeft, "");

            ShopUiUtil.MakeButton(panel.transform, "Prev", new Vector2(0.1f, 0.14f), new Vector2(0.22f, 0.24f),
                "<", () => Cycle(-1));
            ShopUiUtil.MakeButton(panel.transform, "Act", new Vector2(0.24f, 0.14f), new Vector2(0.76f, 0.24f),
                "Confirm", Confirm);
            ShopUiUtil.MakeButton(panel.transform, "Next", new Vector2(0.78f, 0.14f), new Vector2(0.9f, 0.24f),
                ">", () => Cycle(1));
            ShopUiUtil.MakeButton(panel.transform, "Close", new Vector2(0.3f, 0.04f), new Vector2(0.7f, 0.12f),
                "Close", Close);
        }

        void Update()
        {
            var swim = SwimController.Instance;
            playerNear = false;
            if (swim != null)
            {
                float d = Vector3.Distance(swim.transform.position, transform.position);
                playerNear = d <= 5.5f;
            }

            if (playerNear && !open && !GameUi.BlocksGameplay && Input.GetKeyDown(KeyCode.F))
                Open();
            if (open && GameUi.TryConsumeEscape())
                Close();
            if (open)
                Refresh();

            // Soft hint via DiveHud uses main markets; local pulse light already marks stalls.
        }

        public bool PlayerNear => playerNear;

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
            GameUi.SetBlocking(true);
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
            GameUi.SetBlocking(false);
        }

        void Cycle(int delta)
        {
            selected = (selected + delta + Menu.Length) % Menu.Length;
            Refresh();
        }

        void Confirm()
        {
            var stock = Menu[selected];
            switch (stock)
            {
                case Stock.SellCatch:
                {
                    int earned = Inventory.Instance != null ? Inventory.Instance.SellAllCatch() : 0;
                    statusText.text = earned > 0 ? $"+{earned} credits" : "Nothing to sell";
                    break;
                }
                case Stock.AirTopUp:
                {
                    if (Wallet.Instance != null && Wallet.Instance.TrySpend(25))
                    {
                        var suit = SwimController.Instance != null
                            ? SwimController.Instance.GetComponent<DiverSuit>()
                            : null;
                        if (suit != null)
                            suit.RefillAir();
                        statusText.text = "Air topped up";
                    }
                    else statusText.text = "Need 25 credits";
                    break;
                }
                case Stock.SpearRepairKit:
                {
                    if (Wallet.Instance != null && Wallet.Instance.TrySpend(40))
                    {
                        if (Speargun.Instance != null)
                            Speargun.Instance.ApplyUpgrades(
                                Mathf.Max(ShopProgress.Instance != null ? ShopProgress.Instance.LevelOf(GearUpgradeId.RopeLength) : 0, 0),
                                Mathf.Max(ShopProgress.Instance != null ? ShopProgress.Instance.LevelOf(GearUpgradeId.SpearSharpness) : 0, 0) + 0,
                                Mathf.Max(ShopProgress.Instance != null ? ShopProgress.Instance.LevelOf(GearUpgradeId.SpearWeight) : 0, 0));
                        statusText.text = "Spear kit applied";
                    }
                    else statusText.text = "Need 40 credits";
                    break;
                }
                case Stock.GlowFlare:
                {
                    if (Wallet.Instance != null && Wallet.Instance.TrySpend(30))
                    {
                        var suit = SwimController.Instance != null
                            ? SwimController.Instance.GetComponent<DiverSuit>()
                            : null;
                        if (suit != null)
                        {
                            // Temporary lamp bump via ApplyUpgrade preserving other stats.
                            suit.ApplyUpgrade(
                                suit.MaxDiveSeconds,
                                suit.MaxDepthMeters,
                                suit.SwimSpeedMultiplier,
                                suit.BuoyancyAssist,
                                Mathf.Max(suit.LampStrength, 0.5f) + 0.35f);
                        }
                        statusText.text = "Glow flare lit";
                    }
                    else statusText.text = "Need 30 credits";
                    break;
                }
            }
            Refresh();
        }

        void Refresh()
        {
            var wallet = Wallet.Instance;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Credits: {(wallet != null ? wallet.Credits : 0)}");
            sb.AppendLine();
            for (int i = 0; i < Menu.Length; i++)
            {
                string mark = i == selected ? "> " : "  ";
                sb.AppendLine(mark + Label(Menu[i]));
            }
            listText.text = sb.ToString();
        }

        static string Label(Stock s) => s switch
        {
            Stock.SellCatch => "Sell Catch",
            Stock.AirTopUp => "Air Top-Up  [25 cr]",
            Stock.SpearRepairKit => "Spear Kit  [40 cr]",
            Stock.GlowFlare => "Glow Flare  [30 cr]",
            _ => s.ToString()
        };
    }

    public class VendorBob : MonoBehaviour
    {
        Vector3 origin;
        void Start() => origin = transform.localPosition;
        void Update()
        {
            float bob = Mathf.Sin(Time.time * 1.4f + origin.x) * 0.04f;
            transform.localPosition = origin + Vector3.up * bob;
            transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.6f) * 12f, 0f);
        }
    }
}
