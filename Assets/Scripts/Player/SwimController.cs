using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnderwaterDiving
{
    /// <summary>
    /// First-person underwater swimming with buoyancy, quadratic drag, and tuned thrust.
    /// Shore/wade WASD uses Rigidbody.MovePosition so PhysX contact cannot cancel movement.
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
        [SerializeField] float aboveWaterMaxSpeed = 6.5f;
        [SerializeField] float jumpForce = 6.5f;
        [Tooltip("Below this submersion, WASD uses MovePosition (PhysX cannot cancel). Above = swim AddForce.")]
        [SerializeField] float shoreMoveSubmersionMax = 0.55f;

        [Header("Buoyancy & Drag")]
        [SerializeField] float playerHeight = 1.8f;
        [SerializeField] float playerMass = 80f;
        [Tooltip("Upward acceleration while submerged (≈ gravity cancels free-fall).")]
        [SerializeField] float buoyancyAcceleration = 10.5f;
        [SerializeField] float neutralBuoyancyBias = -0.6f;
        [SerializeField] float linearDragUnderwater = 2.8f;
        [SerializeField] float quadraticDragUnderwater = 0.45f;
        [SerializeField] float angularDragUnderwater = 3.5f;
        [SerializeField] float linearDragAir = 0.05f;
        [SerializeField] float surfaceBreakDamping = 0.65f;

        [Header("Feel")]
        [SerializeField] float inputSmoothTime = 0.08f;
        [SerializeField] float stopAssist = 2.2f;
        [SerializeField] float nearSurfaceBobAmplitude = 0.08f;
        [SerializeField] float nearSurfaceBobFrequency = 1.1f;

        Rigidbody body;
        CapsuleCollider capsule;
        float yaw;
        float pitch;
        Vector3 smoothedMove;
        Vector3 moveVelocityRef;
        /// <summary>Move intent sampled in Update (FixedUpdate must not read Input for held keys alone).</summary>
        Vector3 cachedMoveInput;
        bool jumpQueued;
        bool cursorLocked = true;
        int lockAcquiredFrame = -1;
        DiverSuit suit;
        static readonly List<RaycastResult> UiRaycastBuffer = new List<RaycastResult>(16);

        public Transform CameraPivot => cameraPivot;
        /// <summary>Gameplay wants a locked cursor (may briefly disagree with engine until asserted).</summary>
        public bool IsCursorLocked => cursorLocked;
        /// <summary>True when locked, but not on the same frame the lock was acquired (click-to-re-lock).</summary>
        public bool CanUseCombatInput => cursorLocked && lockAcquiredFrame != Time.frameCount;

        public void SetCameraPivot(Transform pivot) => cameraPivot = pivot;

        public void AddExternalForce(Vector3 force)
        {
            if (body == null)
                return;
            EnsureDynamicBody();
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
            capsule = GetComponent<CapsuleCollider>();
            EnsureDynamicBody();
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

            // Never inherit a paused timescale from a previous play session / editor quirk.
            if (Time.timeScale <= 0.001f)
                Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            LockCursor(true);
            EnsureDynamicBody();
            ClearNearbyTrapColliders(14f);
            UnstickFromGeometry();
        }

        /// <summary>
        /// Absolute UI gate from live panel flags — closed map/shop can never leave thrust gated.
        /// </summary>
        void ReconcileUiGate()
        {
            bool map = WorldMapPanel.Instance != null && WorldMapPanel.Instance.IsOpen;
            bool gear = GearMarket.Instance != null && GearMarket.Instance.IsOpen;
            bool decor = DecorShop.Instance != null && DecorShop.Instance.IsOpen;
            bool coral = AnyCoralShopOpen();
            GameUi.ReconcileLive(map, gear, decor, coral);
        }

        static bool AnyCoralShopOpen()
        {
            var shops = Object.FindObjectsByType<CoralDiveShop>(FindObjectsSortMode.None);
            for (int i = 0; i < shops.Length; i++)
            {
                if (shops[i] != null && shops[i].IsOpen)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Lift out of sand / shop decks / reef rocks so contact constraints cannot cancel WASD.
        /// </summary>
        public void UnstickFromGeometry()
        {
            EnsureDynamicBody();
            if (body == null)
                return;

            if (capsule == null)
                capsule = GetComponent<CapsuleCollider>();
            if (capsule == null)
                return;

            float half = Mathf.Max(0.05f, capsule.height * 0.5f);
            const float skin = 0.22f;

            // Prefer raycast onto the floor mesh (works with non-convex MeshCollider).
            Vector3 origin = transform.position + Vector3.up * 12f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit ground, 40f, ~0, QueryTriggerInteraction.Ignore))
            {
                Vector3 p = transform.position;
                p.y = ground.point.y + half + skin;
                transform.position = p;
                body.position = p;
            }
            else
            {
                // Analytical fallback: beach pad is near WaterSurfaceY when ray misses.
                Vector3 p = transform.position;
                p.y = Mathf.Max(p.y, DiveWorldConfig.WaterSurfaceY + half + skin);
                transform.position = p;
                body.position = p;
            }

            // Disable overlapping primitive colliders (rocks/decks). ComputePenetration cannot
            // resolve non-convex MeshCollider embeds — those are handled by the Y snap above.
            Collider[] hits = Physics.OverlapSphere(
                transform.position, capsule.radius + 1.1f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                var other = hits[i];
                if (other == null || other == capsule || other.transform.IsChildOf(transform))
                    continue;
                if (other is MeshCollider)
                    continue; // keep OceanFloor walkable

                if (Physics.ComputePenetration(
                        capsule, transform.position, transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 dir, out float dist)
                    && dist > 0.001f)
                {
                    // Prefer disabling the trap over endless penetration fight.
                    other.enabled = false;
                }
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
        }

        /// <summary>
        /// Strip rock / stall / headland colliders around a point so the diver cannot wedge at spawn.
        /// Keeps OceanFloor MeshCollider so walking still has ground.
        /// </summary>
        public void ClearNearbyTrapColliders(float radius)
        {
            Vector3 center = transform.position;
            Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                var c = hits[i];
                if (c == null || c.transform.IsChildOf(transform))
                    continue;
                if (c is MeshCollider && c.gameObject.name.Contains("OceanFloor"))
                    continue;
                // Beach pad: kill anything that isn't the seabed mesh.
                Object.Destroy(c);
            }
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus)
                return;
            if (Time.timeScale <= 0.001f)
                Time.timeScale = 1f;
            if (!GameUi.BlocksGameplay && cursorLocked)
                LockCursor(true);
        }

        void Update()
        {
            if (Time.timeScale <= 0.001f)
                Time.timeScale = 1f;

            // Reconcile BEFORE the BlocksGameplay gate so a closed panel cannot skip a frame of move.
            ReconcileUiGate();

            // Sample move every frame — WASD held keys are reliable here; FixedUpdate alone can miss.
            cachedMoveInput = SampleMoveInput();
            if (!GameUi.BlocksGameplay && Input.GetKeyDown(KeyCode.Space) && Submersion < 0.2f && !IsUnderwater)
                jumpQueued = true;

            if (GameUi.BlocksGameplay)
            {
                // UI owns the cursor — never leave cursorLocked stuck true while free.
                if (cursorLocked || Cursor.lockState == CursorLockMode.Locked)
                    LockCursor(false);
                jumpQueued = false;
                smoothedMove = Vector3.SmoothDamp(smoothedMove, Vector3.zero, ref moveVelocityRef, inputSmoothTime);
                return;
            }

            HandleCursor();

            // Drive look from our intent flag, not raw Cursor.lockState.
            // Editor/player often drop lockState until a click; gating on engine state
            // left look dead even while WASD should work.
            if (cursorLocked)
                HandleLook();
        }

        void FixedUpdate()
        {
            EnsureDynamicBody();
            ReconcileUiGate();

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
            // Move is never gated on cursor lock — only on blocking UI (map/shops).
            if (!GameUi.BlocksGameplay)
                ApplyThrust();
            ClampMaxSpeed();
            SoftFloorCeiling(surfaceY, floorY);
        }

        void EnsureDynamicBody()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (body == null)
                return;
            if (body.isKinematic)
                body.isKinematic = false;
            if (body.constraints != RigidbodyConstraints.FreezeRotation)
                body.constraints = RigidbodyConstraints.FreezeRotation;
            if (body.IsSleeping())
                body.WakeUp();
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
            // Esc always unlocks when not consumed by map/shops first.
            if (GameUi.TryConsumeEscape())
            {
                LockCursor(false);
                return;
            }

            // Lost focus / OS released lock — keep flag, re-assert when focused.
            // Do NOT clear cursorLocked just because lockState flipped; that killed look
            // after launch when the engine refused lock until a Game View click.
            if (cursorLocked && Application.isFocused && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Click-to-re-lock: any LMB while unlocked, unless over a real Button/Selectable.
            // Ignore non-interactive HUD Images/Texts that still have raycastTarget.
            if (!cursorLocked && Input.GetMouseButtonDown(0))
            {
                if (!IsPointerOverInteractiveUi())
                    LockCursor(true);
            }
        }

        /// <summary>
        /// True only when the pointer hits a Selectable (Button, Toggle, …).
        /// Plain Images/Texts with raycastTarget must not block click-to-re-lock.
        /// </summary>
        static bool IsPointerOverInteractiveUi()
        {
            var es = EventSystem.current;
            if (es == null)
                return false;

            var ped = new PointerEventData(es) { position = Input.mousePosition };
            UiRaycastBuffer.Clear();
            es.RaycastAll(ped, UiRaycastBuffer);
            for (int i = 0; i < UiRaycastBuffer.Count; i++)
            {
                var go = UiRaycastBuffer[i].gameObject;
                if (go == null)
                    continue;
                if (go.GetComponentInParent<Selectable>() != null)
                    return true;
            }
            return false;
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
            if (cameraPivot == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null)
                    cameraPivot = cam.transform.parent != null ? cam.transform.parent : cam.transform;
            }

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
            if (Submersion > 0.4f && Submersion < 0.95f && cachedMoveInput.sqrMagnitude < 0.05f)
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
                    if (cachedMoveInput.sqrMagnitude < 0.05f)
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
            Vector3 raw = cachedMoveInput;
            smoothedMove = Vector3.SmoothDamp(smoothedMove, raw, ref moveVelocityRef, inputSmoothTime);

            bool sprint = Input.GetKey(KeyCode.LeftShift);
            float forceMul = sprint ? sprintMultiplier : 1f;
            if (suit != null)
                forceMul *= suit.SwimSpeedMultiplier;

            // Beach + wade: PhysX contact on the coarse OceanFloor MeshCollider cancels
            // AddForce / linearVelocity every step. MovePosition is the authoritative XZ write.
            // Only use swim forces once meaningfully submerged.
            if (Submersion < shoreMoveSubmersionMax)
                ApplyShoreMove(raw, forceMul);
            else
                ApplySwimMove(forceMul);
        }

        void ApplySwimMove(float forceMul)
        {
            Vector3 forward = cameraPivot != null ? cameraPivot.forward : transform.forward;
            Vector3 right = cameraPivot != null ? cameraPivot.right : transform.right;
            Vector3 wish = (forward * smoothedMove.z + right * smoothedMove.x + Vector3.up * smoothedMove.y);
            if (wish.sqrMagnitude > 1f)
                wish.Normalize();

            body.AddForce(wish * swimForce * forceMul, ForceMode.Acceleration);

            if (Mathf.Abs(smoothedMove.y) > 0.01f)
                body.AddForce(Vector3.up * smoothedMove.y * verticalForce * 0.35f * forceMul, ForceMode.Acceleration);
        }

        void ApplyShoreMove(Vector3 raw, float forceMul)
        {
            Vector3 flat = new Vector3(raw.x, 0f, raw.z);
            float inputMag = Mathf.Clamp01(flat.magnitude);
            if (flat.sqrMagnitude > 1e-6f)
                flat.Normalize();
            else
                flat = Vector3.zero;

            Vector3 world = transform.TransformDirection(flat);
            float speed = aboveWaterMaxSpeed * forceMul * inputMag;
            Vector3 wishVel = world * speed;
            float dt = Time.fixedDeltaTime;

            Vector3 p = body.position;
            p.x += wishVel.x * dt;
            p.z += wishVel.z * dt;

            if (capsule == null)
                capsule = GetComponent<CapsuleCollider>();
            float half = capsule != null ? Mathf.Max(0.05f, capsule.height * 0.5f) : 0.9f;
            const float skin = 0.12f;

            bool grounded = false;
            Vector3 rayOrigin = new Vector3(p.x, p.y + 2.5f, p.z);
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit ground, 6f, ~0, QueryTriggerInteraction.Ignore)
                && Vector3.Dot(ground.normal, Vector3.up) > 0.35f)
            {
                p.y = ground.point.y + half + skin;
                grounded = true;
            }

            body.MovePosition(p);

            float yVel = body.linearVelocity.y;
            if (jumpQueued)
            {
                jumpQueued = false;
                if (yVel < jumpForce)
                    yVel = jumpForce;
            }
            else if (grounded && yVel < 0f)
            {
                yVel = 0f;
            }

            body.linearVelocity = new Vector3(wishVel.x, yVel, wishVel.z);
            body.WakeUp();
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
                float cap = Mathf.Max(aboveWaterMaxSpeed, max * 0.65f);
                if (suit != null)
                    cap *= suit.SwimSpeedMultiplier;
                if (sprint)
                    cap *= sprintMultiplier;
                Vector3 flat = new Vector3(v.x, 0f, v.z);
                if (flat.magnitude > cap)
                {
                    flat = flat.normalized * cap;
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
                body.position = p;
                Vector3 v = body.linearVelocity;
                if (v.y < 0f) v.y = 0f;
                body.linearVelocity = v;
            }
            else if (p.y > maxY)
            {
                p.y = maxY;
                transform.position = p;
                body.position = p;
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
                    Vector3 clamped = new Vector3(flat.x, p.y, flat.z);
                    transform.position = clamped;
                    body.position = clamped;
                }
            }
        }

        /// <summary>
        /// x = strafe, y = up/down, z = forward.
        /// Keyboard GetKey is authoritative; Horizontal/Vertical axes are fallback only.
        /// Desktop keyboard only — no mobile touch joystick in this project.
        /// </summary>
        static Vector3 SampleMoveInput()
        {
            // Hard key path first — never depend solely on axis wiring / UI navigation axes.
            bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            bool back = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            bool forward = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);

            float x;
            float z;
            if (left || right)
                x = (right ? 1f : 0f) - (left ? 1f : 0f);
            else
                x = Input.GetAxisRaw("Horizontal");

            if (forward || back)
                z = (forward ? 1f : 0f) - (back ? 1f : 0f);
            else
                z = Input.GetAxisRaw("Vertical");

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
