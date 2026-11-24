using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Shared UI focus flag so shops pause speargun / look lock.
    /// </summary>
    public static class GameUi
    {
        public static bool BlocksGameplay { get; private set; }

        public static void SetBlocking(bool blocking)
        {
            BlocksGameplay = blocking;
            if (blocking)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (SwimController.Instance != null)
            {
                SwimController.Instance.LockCursor(true);
            }
        }
    }
}
