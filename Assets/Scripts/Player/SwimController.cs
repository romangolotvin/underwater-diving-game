using UnityEngine;
using UnityEngine.EventSystems;

namespace UnderwaterDiving
{
    /// <summary>
    /// First-person underwater swimming with buoyancy, quadratic drag, and tuned thrust.
    /// Quality-focused: smooth accel, surface transition, and responsive look.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SwimController : MonoBehaviour
    {
        public static SwimController Instance { get; private set; }

        [Header("Look")]
        [SerializeField] Transform cameraPivot;
        [SerializeField] float mouseSensitivity = 1.8f;
        [SerializeField] float pitchMin = -82f;
        [SerializeField] float pitchMax = 82f;
        [SerializeField] bool invertY;

        [Header("Thrust")]
        [SerializeField] float swimForce = 28f;
        [SerializeField] float verticalForce = 22f;
        [SerializeField] float sprintMultiplier = 1.55f;
        [SerializeField] float maxSwimSpeed = 7.5f;
        [SerializeField] float maxSprintSpeed = 11f;
        [SerializeField] float aboveWaterWalkForce = 18f;
        [SerializeField] float jumpForce = 6.5f;

        [Header("Buoyancy & Drag")]
        [SerializeField] float playerHeight = 1.8f;
        [SerializeField] float playerMass = 80f;
        [Tooltip("Upward acceleration while submerged (≈ gravity cancels free-fall).")]
        [SerializeField] float buoyancyAcceleration = 10.5f;
        [SerializeField] float neutralBuoyancyBias = -0.6f;
        [SerializeField] float linearDragUnderwater = 2.8f;
        [SerializeField] float quadraticDragUnderwater = 0.45f;
        [SerializeField] float angularDragUnderwater = 3.5f;
        [SerializeField] float linearDragAir = 0.15f;
        [SerializeField] float surfaceBreakDamping = 0.65f;

        [Header("Feel")]
        [SerializeField] float inputSmoothTime = 0.08f;
        [SerializeField] float stopAssist = 2.2f;
        [SerializeField] float nearSurfaceBobAmplitude = 0.08f;
        [SerializeField] float nearSurfaceBobFrequency = 1.1f;

        Rigidbody body;
        float yaw;
        float pitch;
        Vector3 smoothedMove;
        Vector3 moveVelocityRef;
        bool cursorLocked = true;
        int lockAcquiredFrame = -1;
        DiverSuit suit;

        public Transform CameraPivot => cameraPivot;
        public bool IsCursorLocked => cursorLocked && Cursor.lockState == CursorLockMode.Locked;
        /// <summary>True when locked, but not on the same frame the lock was acquired (click-to-re-lock).</summary>
        public bool CanUseCombatInput => IsCursorLocked && lockAcquiredFrame != Time.frameCount;

        public void SetCameraPivot(Transform pivot) => cameraPivot = pivot;

        public void AddExternalForce(Vector3 force)
        {
            if (body == null)
                return;
            body.AddForce(force, ForceMode.Acceleration);
        }

        public bool IsUnderwater { get; private set; }
        public float Submersion { get; private set; }
        public float CurrentDepth { get; private set; }
        public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;

        void Awake()
        {
            Instance = this;
            body = GetComponent<Rigidbody>();
            body.mass = playerMass;
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            suit = GetComponent<DiverSuit>();

            if (cameraPivot == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null)
                    cameraPivot = cam.transform;
            }

            Vector3 e = transform.eulerAngles;
            yaw = e.y;
            pitch = 0f;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            LockCursor(true);
        }

        void Update()
        {
            if (GameUi.BlocksGameplay)
            {
                // UI owns the cursor — never leave cursorLocked stuck true while free.
                if (cursorLocked || Cursor.lockState == CursorLockMode.Locked)
                    LockCursor(false);
                return;
            }

            HandleCursor();
            if (IsCursorLocked)
                HandleLook();
        }

        void FixedUpdate()
        {
            float surfaceY = DiveWorldConfig.WaterSurfaceY;
            float floorY = DiveWorldConfig.OceanFloorY;
            var water = WaterVolume.Instance;
            if (water != null)
            {
                surfaceY = water.SurfaceY;
                floorY = water.FloorY;
                Submersion = water.GetSubmersion(transform.position, playerHeight);
                CurrentDepth = water.DepthBelowSurface(transform.position);
            }
            else
            {
                // Fallback when WaterVolume is missing — still resist free-fall underwater.
                Submersion = GetSubmersionAt(transform.position, playerHeight, surfaceY);
                CurrentDepth = Mathf.Max(0f, surfaceY - transform.position.y);
            }

            IsUnderwater = Submersion > 0.15f;

            ApplyBuoyancy();
            ApplyDrag();
            if (!GameUi.BlocksGameplay)
                ApplyThrust();
            ClampMaxSpeed();
            SoftFloorCeiling(surfaceY, floorY);
        }

        static float GetSubmersionAt(Vector3 center, float height, float surfaceY)
        {
            float half = height * 0.5f;
            float top = center.y + half;
            float bottom = center.y - half;
            if (bottom >= surfaceY)
                return 0f;
            if (top <= surfaceY)
                return 1f;
            return Mathf.Clamp01((surfaceY - bottom) / Mathf.Max(0.01f, height));
        }

        void HandleCursor()
        {
            // Engine / focus can unlock without our flag (Esc on some players).
            if (cursorLocked && Cursor.lockState != CursorLockMode.Locked)
                cursorLocked = false;

            // Click-to-re-lock: use real lock state so a desynced flag can't block recovery.
            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                if (!IsPointerOverUi())
                    LockCursor(true);
                return;
            }

            // Map/shops consume Escape first via GameUi.TryConsumeEscape.
            if (GameUi.TryConsumeEscape())
                LockCursor(false);
        }

        static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        public void LockCursor(bool locked)
        {
            if (locked && !cursorLocked)
                lockAcquiredFrame = Time.frameCount;
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void HandleLook()
        {
            float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * (invertY ? 1f : -1f);
            yaw += mx;
            pitch = Mathf.Clamp(pitch + my, pitchMin, pitchMax);

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void ApplyBuoyancy()
        {
            body.useGravity = true;
            if (Submersion <= 0.001f)
                return;

            // Acceleration-mode buoyancy ≈ cancels gravity when fully submerged (9–12 range).
            float vest = suit != null ? suit.BuoyancyAssist : 0f;
            float accel = (buoyancyAcceleration + neutralBuoyancyBias + vest) * Submersion;
            body.AddForce(Vector3.up * accel, ForceMode.Acceleration);

            // Soft surface slap: bleed upward velocity when crossing the surface.
            if (Submersion < 0.45f && body.linearVelocity.y > 0f)
            {
                Vector3 v = body.linearVelocity;
                v.y *= Mathf.Lerp(1f, surfaceBreakDamping, 1f - Submersion);
                body.linearVelocity = v;
            }

            // Subtle idle bob when nearly neutrally floating near surface.
            if (Submersion > 0.4f && Submersion < 0.95f && GetMoveInput().sqrMagnitude < 0.05f)
            {
                float bob = Mathf.Sin(Time.time * nearSurfaceBobFrequency * Mathf.PI * 2f) * nearSurfaceBobAmplitude;
                body.AddForce(Vector3.up * bob, ForceMode.Acceleration);
            }
        }

        void ApplyDrag()
        {
            if (IsUnderwater)
            {
                // Built-in linear damping plus a light quadratic term so falls slow quickly.
                body.linearDamping = linearDragUnderwater;
                body.angularDamping = angularDragUnderwater;

                Vector3 v = body.linearVelocity;
                float speed = v.magnitude;
                if (speed > 0.001f)
                {
                    Vector3 drag = -v.normalized * (quadraticDragUnderwater * speed * speed);
                    if (GetMoveInput().sqrMagnitude < 0.05f)
                        drag *= stopAssist;
                    body.AddForce(drag, ForceMode.Acceleration);
                }
            }
            else
            {
                body.linearDamping = linearDragAir;
                body.angularDamping = 0.5f;
            }
        }

        void ApplyThrust()
        {
            Vector3 raw = GetMoveInput();
            smoothedMove = Vector3.SmoothDamp(smoothedMove, raw, ref moveVelocityRef, inputSmoothTime);

            bool sprint = Input.GetKey(KeyCode.LeftShift);
            float forceMul = sprint ? sprintMultiplier : 1f;
            if (suit != null)
                forceMul *= suit.SwimSpeedMultiplier;

            if (IsUnderwater)
            {
                Vector3 forward = cameraPivot != null ? cameraPivot.forward : transform.forward;
                Vector3 right = cameraPivot != null ? cameraPivot.right : transform.right;
                Vector3 wish = (forward * smoothedMove.z + right * smoothedMove.x + Vector3.up * smoothedMove.y);
                if (wish.sqrMagnitude > 1f)
                    wish.Normalize();

                body.AddForce(wish * swimForce * forceMul, ForceMode.Acceleration);

                // Extra dedicated vertical authority (Space / Ctrl already in GetMoveInput.y).
                if (Mathf.Abs(smoothedMove.y) > 0.01f)
                    body.AddForce(Vector3.up * smoothedMove.y * verticalForce * 0.35f * forceMul, ForceMode.Acceleration);
            }
            else
            {
                Vector3 flat = new Vector3(smoothedMove.x, 0f, smoothedMove.z);
                if (flat.sqrMagnitude > 1f)
                    flat.Normalize();
                Vector3 world = transform.TransformDirection(flat);
                body.AddForce(world * aboveWaterWalkForce, ForceMode.Acceleration);

                if (Input.GetKeyDown(KeyCode.Space) && Submersion < 0.2f)
                    body.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            }
        }

        void ClampMaxSpeed()
        {
            bool sprint = Input.GetKey(KeyCode.LeftShift);
            float max = sprint ? maxSprintSpeed : maxSwimSpeed;
            if (suit != null)
                max *= suit.SwimSpeedMultiplier;

            Vector3 v = body.linearVelocity;
            if (IsUnderwater)
            {
                if (v.magnitude > max)
                    body.linearVelocity = v.normalized * max;
            }
            else
            {
                Vector3 flat = new Vector3(v.x, 0f, v.z);
                if (flat.magnitude > max * 0.65f)
                {
                    flat = flat.normalized * (max * 0.65f);
                    body.linearVelocity = new Vector3(flat.x, v.y, flat.z);
                }
            }
        }

        void SoftFloorCeiling(float surfaceY, float floorY)
        {
            Vector3 p = transform.position;
            float minY = floorY + 1.1f;
            float maxY = surfaceY + 8f;
            if (p.y < minY)
            {
                p.y = minY;
                transform.position = p;
                Vector3 v = body.linearVelocity;
                if (v.y < 0f) v.y = 0f;
                body.linearVelocity = v;
            }
            else if (p.y > maxY)
            {
                p.y = maxY;
                transform.position = p;
            }

            // Invisible soft boundary only — no rock walls. Fog hides the limit.
            Vector3 flat = new Vector3(p.x, 0f, p.z);
            float soft = DiveWorldConfig.SoftBoundaryRadius;
            float mag = flat.magnitude;
            if (mag > soft * 0.85f)
            {
                Vector3 outward = flat.normalized;
                float t = Mathf.InverseLerp(soft * 0.85f, soft, mag);
                Vector3 v = body.linearVelocity;
                float outwardSpeed = Vector3.Dot(v, outward);
                if (outwardSpeed > 0f)
                    body.linearVelocity = v - outward * (outwardSpeed * Mathf.Lerp(0.35f, 1f, t));

                if (mag > soft)
                {
                    flat = outward * soft;
                    transform.position = new Vector3(flat.x, p.y, flat.z);
                }
            }
        }

        /// <summary>
        /// x = strafe, y = up/down, z = forward.
        /// </summary>
        Vector3 GetMoveInput()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            float y = 0f;
            if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E))
                y += 1f;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.C))
                y -= 1f;

            // Mouse wheel for fine vertical trim while diving.
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
                y += Mathf.Clamp(scroll * 8f, -1f, 1f);

            return new Vector3(x, y, z);
        }
    }
}
