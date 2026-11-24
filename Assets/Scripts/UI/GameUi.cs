using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Shared UI focus flag so shops/map pause speargun / look lock / thrust.
    /// <see cref="BlocksGameplay"/> is LIVE panel state only (map/shop GameObject
    /// activeInHierarchy) — never a sticky bitmask. Mask bookkeeping remains for
    /// cursor edge helpers; it cannot gate WASD/look by itself.
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
        static bool lastLiveBlocked;

        /// <summary>
        /// True only while a map/shop panel is actually open and active in hierarchy.
        /// Sticky mask bits are ignored — that was the chronic Editor WASD/look killer.
        /// </summary>
        public static bool BlocksGameplay => AnyModalPanelOpen();

        /// <summary>Live open check used by movement/look and by <see cref="BlocksGameplay"/>.</summary>
        public static bool AnyModalPanelOpen()
        {
            if (WorldMapPanel.Instance != null && WorldMapPanel.Instance.IsOpen)
                return true;
            if (GearMarket.Instance != null && GearMarket.Instance.IsOpen)
                return true;
            if (DecorShop.Instance != null && DecorShop.Instance.IsOpen)
                return true;
            if (CoralDiveShop.AnyOpen())
                return true;
            return false;
        }

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
        /// Set or clear one UI owner's contribution to the bookkeeping mask.
        /// Cursor lock edges follow live modal state, not the mask alone.
        /// </summary>
        public static void SetBlocked(Blocker blocker, bool blocked)
        {
            int bit = (int)blocker;
            if (blocked)
                mask |= bit;
            else
                mask &= ~bit;

            SyncCursorToLive();
        }

        /// <summary>
        /// Absolute sync from live panel open flags (keeps mask aligned; cursor follows live).
        /// </summary>
        public static void ReconcileLive(bool mapOpen, bool gearOpen, bool decorOpen, bool coralOpen)
        {
            int desired = 0;
            if (mapOpen) desired |= (int)Blocker.Map;
            if (gearOpen) desired |= (int)Blocker.GearMarket;
            if (decorOpen) desired |= (int)Blocker.DecorShop;
            if (coralOpen) desired |= (int)Blocker.CoralShop;
            mask = desired;
            SyncCursorToLive();
        }

        /// <summary>
        /// Call once per frame from SwimController so cursor tracks live panels even if
        /// no SetBlocked edge fired.
        /// </summary>
        public static void SyncCursorToLive()
        {
            bool now = AnyModalPanelOpen();
            if (now == lastLiveBlocked)
                return;

            bool was = lastLiveBlocked;
            lastLiveBlocked = now;
            ApplyCursorEdge(was, now);
        }

        static void ApplyCursorEdge(bool wasBlocked, bool nowBlocked)
        {
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

        /// <summary>Force-clear every blocker (boot / recovery) and re-lock cursor if no live modal.</summary>
        public static void ClearBlocking()
        {
            mask = 0;
            // Heal open flags that cannot be true without a panel (boot path).
            SyncCursorToLive();
            if (!AnyModalPanelOpen())
            {
                lastLiveBlocked = false;
                var swim = SwimController.Instance;
                if (swim != null)
                    swim.LockCursor(true);
            }
        }
    }
}
