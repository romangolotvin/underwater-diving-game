using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Underwater speargun: fired spear on a line. Upgrades: line length, edge, mass (pull).
    /// </summary>
    public class Speargun : MonoBehaviour
    {
        public static Speargun Instance { get; private set; }

        Transform camPivot;
        Transform muzzle;
        LineRenderer line;
        GameObject gunBody;
        GameObject spearVisual;
        MeshRenderer gunRenderer;
        MeshRenderer spearRenderer;
        Material gunMat;
        Material spearMat;

        float ropeLevel;
        float sharpLevel;
        float weightLevel;

        bool spearOut;
        Vector3 spearPos;
        Vector3 spearVel;
        FishAgent hooked;
        float reloadTimer;
        float lineSlack;

        public float MaxRope => 14f + ropeLevel * 6f;
        public float SpearSpeed => 22f + sharpLevel * 3.5f;
        public float CatchRadius => 0.55f + sharpLevel * 0.18f;
        public float MassPull => 2.5f + weightLevel * 3.2f;
        public bool IsReady => !spearOut && reloadTimer <= 0f;

        public void Bind(Transform cameraPivot)
        {
            camPivot = cameraPivot;
            BuildGun();
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void BuildGun()
        {
            if (camPivot == null)
                return;

            gunBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gunBody.name = "SpeargunBody";
            Object.Destroy(gunBody.GetComponent<Collider>());
            gunBody.transform.SetParent(camPivot, false);
            gunBody.transform.localPosition = new Vector3(0.22f, -0.18f, 0.42f);
            gunBody.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            gunBody.transform.localScale = new Vector3(0.06f, 0.08f, 0.38f);
            gunMat = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                new Color(0.35f, 0.4f, 0.42f), 0.55f, 0.45f, new Vector2(2f, 2f));
            gunRenderer = gunBody.GetComponent<MeshRenderer>();
            gunRenderer.sharedMaterial = gunMat;

            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            Object.Destroy(barrel.GetComponent<Collider>());
            barrel.transform.SetParent(gunBody.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            barrel.GetComponent<MeshRenderer>().sharedMaterial = gunMat;

            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(gunBody.transform, false);
            muzzle.localPosition = new Vector3(0f, 0f, 0.95f);

            spearVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spearVisual.name = "Spear";
            Object.Destroy(spearVisual.GetComponent<Collider>());
            spearVisual.transform.SetParent(null, true);
            spearVisual.transform.localScale = new Vector3(0.03f, 0.35f, 0.03f);
            spearMat = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo, ProceduralTextures.RockNormal,
                new Color(0.7f, 0.72f, 0.75f), 0.7f, 0.65f, new Vector2(1f, 1f));
            spearRenderer = spearVisual.GetComponent<MeshRenderer>();
            spearRenderer.sharedMaterial = spearMat;
            spearVisual.SetActive(false);

            var lineGo = new GameObject("SpearLine");
            lineGo.transform.SetParent(transform, false);
            line = lineGo.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = 0.012f;
            line.endWidth = 0.008f;
            var lineMat = new Material(Shader.Find("Sprites/Default"));
            lineMat.mainTexture = ProceduralTextures.RopeAlbedo;
            line.material = lineMat;
            line.textureMode = LineTextureMode.Tile;
            line.startColor = new Color(1f, 1f, 1f, 0.9f);
            line.endColor = new Color(1f, 1f, 1f, 0.55f);
            line.enabled = false;
        }

        public void ApplyUpgrades(int rope, int sharp, int weight)
        {
            ropeLevel = rope;
            sharpLevel = sharp;
            weightLevel = weight;
        }

        public void ApplyCosmeticTint(Color accent, CosmeticId? id)
        {
            if (gunMat == null)
                return;
            if (id == CosmeticId.AbyssPaint)
                gunMat.color = Color.Lerp(new Color(0.2f, 0.15f, 0.3f), accent, 0.6f);
            else if (id == CosmeticId.ReefStripe)
                gunMat.color = Color.Lerp(new Color(0.35f, 0.4f, 0.42f), accent, 0.45f);
            else if (id == CosmeticId.GoldWrist)
                gunMat.color = Color.Lerp(new Color(0.4f, 0.38f, 0.3f), accent, 0.35f);
            else
                gunMat.color = new Color(0.35f, 0.4f, 0.42f);
        }

        void Update()
        {
            if (GameUi.BlocksGameplay)
                return;

            if (reloadTimer > 0f)
                reloadTimer -= Time.deltaTime;

            var swim = SwimController.Instance;
            if (swim == null || !swim.IsUnderwater)
            {
                if (spearOut)
                    Retract(false);
                return;
            }

            // Skip the click that re-locks the cursor (same frame as LockCursor).
            if (Input.GetMouseButtonDown(0) && IsReady && swim.CanUseCombatInput)
                Fire();

            if (Input.GetMouseButtonDown(1) && spearOut)
                Retract(false);

            if (spearOut)
                TickSpear();
        }

        void Fire()
        {
            if (muzzle == null || camPivot == null)
                return;

            spearOut = true;
            spearPos = muzzle.position;
            spearVel = camPivot.forward * SpearSpeed;
            hooked = null;
            lineSlack = 0f;
            spearVisual.SetActive(true);
            line.enabled = true;
            reloadTimer = Mathf.Max(0.35f, 0.9f - sharpLevel * 0.08f);
        }

        void TickSpear()
        {
            float dt = Time.deltaTime;
            Vector3 origin = muzzle != null ? muzzle.position : transform.position;

            if (hooked == null)
            {
                // Mass pulls spear slightly downward while flying.
                spearVel += Vector3.down * (MassPull * 0.35f * dt);
                spearVel *= 1f - 0.4f * dt;
                spearPos += spearVel * dt;

                float dist = Vector3.Distance(origin, spearPos);
                if (dist > MaxRope)
                {
                    Retract(false);
                    return;
                }

                // Water drag slows spear; sharpness keeps penetration window longer via catch radius.
                TryHookFish();
            }
            else
            {
                if (hooked == null || !hooked.isActiveAndEnabled)
                {
                    Retract(false);
                    return;
                }

                spearPos = hooked.transform.position;
                float dist = Vector3.Distance(origin, spearPos);
                if (dist > MaxRope * 1.05f)
                {
                    FinishCatch(hooked);
                    return;
                }

                // Heavy harpoon reels the diver toward the catch / downward.
                var rb = SwimController.Instance != null
                    ? SwimController.Instance.GetComponent<Rigidbody>()
                    : null;
                if (rb != null)
                {
                    Vector3 toSpear = (spearPos - rb.position);
                    Vector3 pull = toSpear.normalized * MassPull;
                    pull += Vector3.down * (MassPull * 0.25f);
                    rb.AddForce(pull, ForceMode.Acceleration);

                    // Auto-reel when close enough.
                    if (dist < 2.2f + sharpLevel * 0.15f)
                    {
                        FinishCatch(hooked);
                        return;
                    }
                }

                lineSlack += dt;
                if (lineSlack > 8f)
                    FinishCatch(hooked);
            }

            UpdateVisuals(origin);
        }

        void TryHookFish()
        {
            var fishes = FishAgent.Active;
            float best = CatchRadius;
            FishAgent hit = null;
            for (int i = 0; i < fishes.Count; i++)
            {
                var f = fishes[i];
                if (f == null || !f.isActiveAndEnabled)
                    continue;
                float d = Vector3.Distance(spearPos, f.transform.position);
                if (d < best)
                {
                    best = d;
                    hit = f;
                }
            }

            if (hit != null)
            {
                // Sharpness improves chance to stick on a glancing pass.
                float chance = Mathf.Clamp01(0.55f + sharpLevel * 0.1f);
                if (best < CatchRadius * 0.55f || Random.value < chance)
                {
                    hooked = hit;
                    hit.OnHooked();
                    spearVel = Vector3.zero;
                }
            }
        }

        void FinishCatch(FishAgent fish)
        {
            var inv = Inventory.Instance;
            if (fish != null && inv != null && inv.CanAddCatch())
            {
                var def = ItemCatalog.ForFish(fish.Species);
                inv.Add(def.Id, 1);
                fish.DespawnCaught();
            }
            else if (fish != null)
            {
                fish.ReleaseHook();
            }

            Retract(true);
        }

        void Retract(bool afterCatch)
        {
            spearOut = false;
            hooked = null;
            if (spearVisual != null)
                spearVisual.SetActive(false);
            if (line != null)
                line.enabled = false;
            reloadTimer = afterCatch ? 0.45f : 0.25f;
        }

        void UpdateVisuals(Vector3 origin)
        {
            if (spearVisual != null)
            {
                spearVisual.transform.position = spearPos;
                if (spearVel.sqrMagnitude > 0.05f)
                    spearVisual.transform.rotation = Quaternion.LookRotation(spearVel.normalized) * Quaternion.Euler(90f, 0f, 0f);
                else if (hooked != null)
                    spearVisual.transform.rotation = Quaternion.LookRotation((spearPos - origin).normalized) * Quaternion.Euler(90f, 0f, 0f);
            }

            if (line != null)
            {
                line.SetPosition(0, origin);
                line.SetPosition(1, spearPos);
            }
        }
    }
}
