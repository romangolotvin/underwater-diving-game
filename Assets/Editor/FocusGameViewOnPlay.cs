using UnityEditor;
using UnityEngine;

namespace UnderwaterDiving.Editor
{
    /// <summary>
    /// Editor Play Mode often leaves keyboard/mouse on the Scene view until the Game View is
    /// clicked. That click can also hit HUD Selectables (map button) and open a modal that
    /// then blocks WASD/look. Force-focus Game View when entering Play.
    /// </summary>
    [InitializeOnLoad]
    static class FocusGameViewOnPlay
    {
        static FocusGameViewOnPlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
                return;
            EditorApplication.delayCall += FocusGameView;
        }

        static void FocusGameView()
        {
            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null)
                return;
            var window = EditorWindow.GetWindow(gameViewType);
            if (window != null)
                window.Focus();
        }
    }
}
