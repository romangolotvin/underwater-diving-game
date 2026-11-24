using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Shared UI focus flag so shops/map pause speargun / look lock / thrust.
    /// Uses a blocker mask so one panel's Close cannot clear another panel's block,
    /// and a closed map cannot leave BlocksGameplay stuck true.
    /// </summary>
    public static class GameUi
    {
        [System.Flags]
        public enum Blocker
        {
            None = 0,
            Map = 1 << 0,
            GearMarket = 1 << 1,
            DecorShop = 1 << 2,
            CoralShop = 1 << 3,
        }

        static int mask;
        static int escapeConsumedFrame = -1;

        public static bool BlocksGameplay => mask != 0;

        /// <summary>
        /// First caller this frame wins Escape (map/shops before swim unlock).
        /// </summary>
        public static bool TryConsumeEscape()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return false;
            if (escapeConsumedFrame == Time.frameCount)
                return false;
            escapeConsumedFrame = Time.frameCount;
            return true;
        }

        /// <summary>
        /// Set or clear one UI owner's contribution to the gameplay block.
        /// Cursor lock only updates when the overall blocked state edges.
        /// </summary>
        public static void SetBlocked(Blocker blocker, bool blocked)
        {
            int before = mask;
            int bit = (int)blocker;
            if (blocked)
                mask |= bit;
            else
                mask &= ~bit;

            if (before == mask)
                return;

            bool nowBlocked = mask != 0;
            bool wasBlocked = before != 0;
            if (nowBlocked == wasBlocked)
                return;

            var swim = SwimController.Instance;
            if (nowBlocked)
            {
                if (swim != null)
                    swim.LockCursor(false);
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            else if (swim != null)
            {
                swim.LockCursor(true);
            }
        }

        /// <summary>
        /// Legacy absolute API — prefer <see cref="SetBlocked"/>. Still used by older call sites.
        /// True sets Map bit; false clears the entire mask (boot / recovery style).
        /// </summary>
        public static void SetBlocking(bool blocking)
        {
            if (blocking)
                SetBlocked(Blocker.Map, true);
            else
                ClearBlocking();
        }

        /// <summary>Force-clear every blocker (boot / recovery).</summary>
        public static void ClearBlocking()
        {
            int before = mask;
            mask = 0;
            if (before == 0)
                return;

            var swim = SwimController.Instance;
            if (swim != null)
                swim.LockCursor(true);
        }
    }
}
