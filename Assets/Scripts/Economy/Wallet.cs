using System;
using UnityEngine;

namespace UnderwaterDiving
{
    public class Wallet : MonoBehaviour
    {
        public static Wallet Instance { get; private set; }

        int credits;

        public int Credits => credits;
        public event Action<int> CreditsChanged;

        void Awake()
        {
            Instance = this;
            credits = DiveWorldConfig.StartingCredits;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool CanAfford(int amount) => credits >= amount;

        public bool TrySpend(int amount)
        {
            if (amount < 0 || credits < amount)
                return false;
            credits -= amount;
            CreditsChanged?.Invoke(credits);
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;
            credits += amount;
            CreditsChanged?.Invoke(credits);
        }
    }
}
