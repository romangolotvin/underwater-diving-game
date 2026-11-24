using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Owned decorations bought at the outfitter. Equipped looks apply to FP accents.
    /// </summary>
    public class CosmeticLocker : MonoBehaviour
    {
        public static CosmeticLocker Instance { get; private set; }

        readonly HashSet<CosmeticId> owned = new();
        CosmeticId? equipped;

        public event Action Changed;
        public CosmeticId? Equipped => equipped;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool Owns(CosmeticId id) => owned.Contains(id);

        public bool TryBuy(CosmeticId id)
        {
            if (owned.Contains(id))
                return false;
            var wallet = Wallet.Instance;
            if (wallet == null)
                return false;
            var def = ItemCatalog.ForCosmetic(id);
            if (!wallet.TrySpend(def.BuyPrice))
                return false;
            owned.Add(id);
            equipped = id;
            ApplyVisuals();
            Changed?.Invoke();
            return true;
        }

        public void Equip(CosmeticId id)
        {
            if (!owned.Contains(id))
                return;
            equipped = id;
            ApplyVisuals();
            Changed?.Invoke();
        }

        public void ApplyVisuals()
        {
            var look = DiverLook.Instance;
            if (look != null)
                look.ApplyCosmetic(equipped);
        }
    }
}
