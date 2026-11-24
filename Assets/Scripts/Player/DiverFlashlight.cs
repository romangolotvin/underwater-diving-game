using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Minimal head/camera-forward torch. Toggle with L (F is used by shops).
    /// Uses legacy Input; project has no Input System package (activeInputHandler=Both).
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
            if (GameUi.BlocksGameplay || spot == null)
                return;

            if (WasTogglePressed())
                SetOn(!isOn);
        }

        bool WasTogglePressed()
        {
            // Legacy Input — works with activeInputHandler Both/Input Manager.
            // No com.unity.inputsystem package in this project; Keyboard.current unavailable.
            return Input.GetKeyDown(toggleKey);
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
