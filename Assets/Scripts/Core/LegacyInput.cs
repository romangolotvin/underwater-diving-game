using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Hardened legacy Input reads. Input System-only (activeInputHandler=1) makes
    /// <see cref="Input.GetAxisRaw"/> throw — that exception aborted SwimController.Update
    /// before HandleLook / before cached move was refreshed, killing WASD + mouse together.
    /// </summary>
    public static class LegacyInput
    {
        static Vector3 lastMousePosition;
        static int lastMouseFrame = -1;
        static bool hasLastMouse;

        public static float AxisRaw(string name)
        {
            try
            {
                return Input.GetAxisRaw(name);
            }
            catch (System.Exception)
            {
                return 0f;
            }
        }

        public static float Axis(string name)
        {
            try
            {
                return Input.GetAxis(name);
            }
            catch (System.Exception)
            {
                return 0f;
            }
        }

        public static bool Key(KeyCode key)
        {
            try
            {
                return Input.GetKey(key);
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public static bool KeyDown(KeyCode key)
        {
            try
            {
                return Input.GetKeyDown(key);
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public static bool MouseButtonDown(int button)
        {
            try
            {
                return Input.GetMouseButtonDown(button);
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Mouse look delta in "axis units". Prefers Mouse X/Y; if those are zero while the
        /// cursor is unlocked (Editor often refuses lock until Game View click), falls back to
        /// mouse-position delta so look still works over the Game View.
        /// </summary>
        public static void ReadMouseLook(out float mouseX, out float mouseY)
        {
            mouseX = AxisRaw("Mouse X");
            mouseY = AxisRaw("Mouse Y");

            if (Mathf.Abs(mouseX) > 0.0001f || Mathf.Abs(mouseY) > 0.0001f)
            {
                // Axes are live — keep absolute mouse tracker in sync and bail.
                RememberMouse();
                return;
            }

            // Unlocked / Editor: axes often sit at 0 until Cursor.lockState sticks.
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                RememberMouse();
                return;
            }

            Vector3 pos;
            try
            {
                pos = Input.mousePosition;
            }
            catch (System.Exception)
            {
                mouseX = 0f;
                mouseY = 0f;
                return;
            }

            if (!hasLastMouse || lastMouseFrame != Time.frameCount - 1 && lastMouseFrame != Time.frameCount)
            {
                lastMousePosition = pos;
                hasLastMouse = true;
                lastMouseFrame = Time.frameCount;
                mouseX = 0f;
                mouseY = 0f;
                return;
            }

            Vector3 delta = pos - lastMousePosition;
            lastMousePosition = pos;
            lastMouseFrame = Time.frameCount;
            // Scale roughly toward GetAxisRaw magnitudes at default sensitivity.
            mouseX = delta.x * 0.12f;
            mouseY = delta.y * 0.12f;
        }

        static void RememberMouse()
        {
            try
            {
                lastMousePosition = Input.mousePosition;
                hasLastMouse = true;
                lastMouseFrame = Time.frameCount;
            }
            catch (System.Exception)
            {
                hasLastMouse = false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            hasLastMouse = false;
            lastMouseFrame = -1;
            lastMousePosition = Vector3.zero;
        }
    }
}
