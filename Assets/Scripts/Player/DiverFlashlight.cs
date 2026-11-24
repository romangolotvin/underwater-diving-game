using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Minimal head/camera-forward torch. Toggle with L (F is used by shops).
    /// Uses legacy Input; project has no Input System package (activeInputHandler=Input Manager).
    /// </summary>
    public class DiverFlashlight : MonoBehaviour
    {
        public static DiverFlashlight Instance { get; private set; }

        [SerializeField] KeyCode toggleKey = KeyCode.L;
        [SerializeField] float intensity = 4.2f;
        [SerializeField] float range = 28f;
        [SerializeField] float spotAngle = 55f;

        Light spot;
        bool isOn;

        public bool IsOn => isOn;

        public void Bind(Transform cameraPivot)
        {
            if (cameraPivot == null)
                return;

            var go = new GameObject("Flashlight");
            go.transform.SetParent(cameraPivot, false);
            // Slightly in front of the eyes so the cone doesn't clip the near plane.
            go.transform.localPosition = new Vector3(0.08f, -0.05f, 0.12f);
            go.transform.localRotation = Quaternion.identity;

            spot = go.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.96f, 0.88f);
            spot.intensity = intensity;
            spot.range = range;
            spot.spotAngle = spotAngle;
            spot.innerSpotAngle = spotAngle * 0.55f;
            spot.shadows = LightShadows.None;
            spot.enabled = false;
            isOn = false;
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            // L must not depend on GameUi.BlocksGameplay: WASD agents left that mask
            // sticky across Update-order races, which silently killed the torch while
            // shops/map were already closed. Light toggle is harmless over UI.
            EnsureBound();
            if (spot == null)
                return;

            if (WasTogglePressed())
                SetOn(!isOn);
        }

        void EnsureBound()
        {
            if (spot != null)
                return;
            var swim = SwimController.Instance;
            if (swim != null && swim.CameraPivot != null)
                Bind(swim.CameraPivot);
        }

        bool WasTogglePressed()
        {
            // Legacy Input Manager only (no com.unity.inputsystem in this project).
            // Always honor L even if a serialized toggleKey was cleared in the inspector.
            return Input.GetKeyDown(toggleKey) || Input.GetKeyDown(KeyCode.L);
        }

        public void SetOn(bool on)
        {
            isOn = on;
            if (spot != null)
                spot.enabled = isOn;
        }

        public void Toggle() => SetOn(!isOn);
    }
}
