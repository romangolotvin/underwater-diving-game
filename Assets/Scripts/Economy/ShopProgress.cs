using System;
using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Gear upgrade levels bought at the market. Applied to suit and speargun.
    /// </summary>
    public class ShopProgress : MonoBehaviour
    {
        public static ShopProgress Instance { get; private set; }

        readonly int[] levels = new int[System.Enum.GetValues(typeof(GearUpgradeId)).Length];

        public event Action Changed;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public int LevelOf(GearUpgradeId id) => levels[(int)id];

        public int NextPrice(GearUpgradeId id)
        {
            var def = ItemCatalog.ForGear(id);
            return def.BuyPrice + LevelOf(id) * DiveWorldConfig.UpgradePriceStep;
        }

        public bool TryBuy(GearUpgradeId id)
        {
            var wallet = Wallet.Instance;
            if (wallet == null)
                return false;

            int idx = (int)id;
            if (levels[idx] >= DiveWorldConfig.MaxUpgradeLevel)
                return false;

            int price = NextPrice(id);
            if (!wallet.TrySpend(price))
                return false;

            levels[idx]++;
            ApplyToPlayer();
            Changed?.Invoke();
            return true;
        }

        public void ApplyToPlayer()
        {
            var swim = SwimController.Instance;
            var suit = swim != null ? swim.GetComponent<DiverSuit>() : null;
            if (suit != null)
            {
                float air = 90f + LevelOf(GearUpgradeId.AirTank) * 35f;
                float depth = 28f + LevelOf(GearUpgradeId.DepthSuit) * 12f;
                float speed = 1f + LevelOf(GearUpgradeId.FinBoost) * 0.12f;
                float buoyancy = LevelOf(GearUpgradeId.BuoyancyVest) * 0.18f;
                float lamp = LevelOf(GearUpgradeId.LampBeam) * 0.35f;
                suit.ApplyUpgrade(air, depth, speed, buoyancy, lamp);
            }

            if (Speargun.Instance != null)
            {
                Speargun.Instance.ApplyUpgrades(
                    LevelOf(GearUpgradeId.RopeLength),
                    LevelOf(GearUpgradeId.SpearSharpness),
                    LevelOf(GearUpgradeId.SpearWeight));
            }
        }
    }
}
