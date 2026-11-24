using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Shared UI focus flag so shops/map pause speargun / look lock.
    /// </summary>
    public static class GameUi
    {
        public static bool BlocksGameplay { get; private set; }

        static int escapeConsumedFrame = -1;

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

        public static void SetBlocking(bool blocking)
        {
            BlocksGameplay = blocking;
            var swim = SwimController.Instance;
            if (blocking)
            {
                // Keep SwimController.cursorLocked in sync — unlocking Cursor alone
                // leaves the flag true and breaks click-to-re-lock after UI closes.
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
    }
}
