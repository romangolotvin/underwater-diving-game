using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnderwaterDiving
{
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }

        readonly Dictionary<string, int> stacks = new();

        public event Action Changed;
        public IReadOnlyDictionary<string, int> Stacks => stacks;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public int Count(string itemId) => stacks.TryGetValue(itemId, out int n) ? n : 0;

        public int TotalCatchCount()
        {
            int total = 0;
            foreach (var kv in stacks)
            {
                if (!ItemCatalog.TryGet(kv.Key, out var def))
                    continue;
                if (def.Kind == ItemKind.FishCatch || def.Kind == ItemKind.Artifact)
                    total += kv.Value;
            }
            return total;
        }

        public int MaxCatchSlots()
        {
            int bag = ShopProgress.Instance != null ? ShopProgress.Instance.LevelOf(GearUpgradeId.CatchBag) : 0;
            return 8 + bag * 4;
        }

        public bool CanAddCatch() => TotalCatchCount() < MaxCatchSlots();

        public void Add(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
                return;
            stacks.TryGetValue(itemId, out int cur);
            stacks[itemId] = cur + amount;
            Changed?.Invoke();
        }

        public int SellAllCatch()
        {
            var wallet = Wallet.Instance;
            if (wallet == null)
                return 0;

            int earned = 0;
            var remove = new List<string>();
            foreach (var kv in stacks)
            {
                if (!ItemCatalog.TryGet(kv.Key, out var def))
                    continue;
                if (def.Kind != ItemKind.FishCatch && def.Kind != ItemKind.Artifact)
                    continue;
                earned += def.SellPrice * kv.Value;
                remove.Add(kv.Key);
            }

            for (int i = 0; i < remove.Count; i++)
                stacks.Remove(remove[i]);

            if (earned > 0)
            {
                wallet.Add(earned);
                Changed?.Invoke();
            }
            return earned;
        }
    }
}
