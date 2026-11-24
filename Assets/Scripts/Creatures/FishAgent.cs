using System.Collections.Generic;
using UnityEngine;

namespace UnderwaterDiving
{
    public class FishAgent : MonoBehaviour
    {
        public static readonly List<FishAgent> Active = new();

        public FishSpecies Species { get; private set; }
        public bool IsHostile { get; private set; }

        Vector3 velocity;
        Vector3 home;
        float speed;
        float turn;
        bool hooked;
        float attackCooldown;
        Transform bodyRoot;

        public static FishAgent Spawn(Transform parent, FishSpecies species, Vector3 pos)
        {
            var root = new GameObject("Fish_" + species);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;

            var fish = root.AddComponent<FishAgent>();
            fish.Species = species;
            fish.IsHostile = species == FishSpecies.Barracuda;
            fish.home = pos;
            fish.turn = Random.Range(0f, 360f);
            fish.speed = species switch
            {
                FishSpecies.AmberJack => 3.2f,
                FishSpecies.AbyssGrouper => 2.4f,
                FishSpecies.MorayEel => 1.1f,
                FishSpecies.SeaTurtle => 1.6f,
                FishSpecies.EagleRay => 2.5f,
                FishSpecies.Barracuda => 4.4f,
                _ => 2.8f
            };
            fish.BuildVisual();
            fish.velocity = Random.onUnitSphere;
            fish.velocity.y *= 0.3f;
            fish.velocity = fish.velocity.normalized * fish.speed * 0.5f;

            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = species switch
            {
                FishSpecies.SeaTurtle => 0.9f,
                FishSpecies.EagleRay => 0.85f,
                FishSpecies.Barracuda => 0.7f,
                FishSpecies.AbyssGrouper => 0.75f,
                FishSpecies.MorayEel => 0.55f,
                _ => 0.4f
            };

            return fish;
        }

        void BuildVisual()
        {
            bodyRoot = new GameObject("Body").transform;
            bodyRoot.SetParent(transform, false);

            Color tint = ItemCatalog.FishTint(Species);
            Texture2D albedo = Species switch
            {
                FishSpecies.MorayEel => ProceduralTextures.FishScaleAlbedo,
                FishSpecies.SeaTurtle => ProceduralTextures.ShellAlbedo,
                FishSpecies.EagleRay => ProceduralTextures.RayAlbedo,
                FishSpecies.Barracuda => ProceduralTextures.FishScaleAlbedo,
                _ => ProceduralTextures.FishScaleAlbedo
            };
            Texture2D normal = Species switch
            {
                FishSpecies.SeaTurtle => ProceduralTextures.ShellNormal,
                FishSpecies.EagleRay => ProceduralTextures.RayNormal,
                _ => ProceduralTextures.FishScaleNormal
            };

            var mat = ProceduralTextures.MakeLit(albedo, normal, tint, 0.42f, 0.08f, new Vector2(2.5f, 2.5f));

            switch (Species)
            {
                case FishSpecies.MorayEel:
                    AddPrim(PrimitiveType.Capsule, new Vector3(0f, 0f, 0f), new Vector3(0.25f, 0.25f, 1.4f), mat);
                    AddPrim(PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0.65f), Vector3.one * 0.28f, mat);
                    break;
                case FishSpecies.SeaTurtle:
                    AddPrim(PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), new Vector3(0.9f, 0.45f, 1.1f), mat);
                    AddPrim(PrimitiveType.Cube, new Vector3(0.55f, 0f, 0.15f), new Vector3(0.35f, 0.08f, 0.5f), mat);
                    AddPrim(PrimitiveType.Cube, new Vector3(-0.55f, 0f, 0.15f), new Vector3(0.35f, 0.08f, 0.5f), mat);
                    AddPrim(PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0.65f), Vector3.one * 0.28f, mat);
                    break;
                case FishSpecies.EagleRay:
                    AddPrim(PrimitiveType.Sphere, Vector3.zero, new Vector3(1.6f, 0.18f, 0.9f), mat);
                    AddPrim(PrimitiveType.Capsule, new Vector3(0f, 0f, -0.7f), new Vector3(0.12f, 0.12f, 0.9f), mat);
                    break;
                case FishSpecies.Barracuda:
                    AddPrim(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.28f, 0.22f, 1.35f), mat);
                    var finMat = ProceduralTextures.MakeLit(albedo, normal, Color.Lerp(tint, Color.black, 0.25f), 0.35f, 0.1f, new Vector2(2f, 2f));
                    AddPrim(PrimitiveType.Cube, new Vector3(0f, 0.18f, -0.2f), new Vector3(0.05f, 0.25f, 0.35f), finMat);
                    break;
                case FishSpecies.AbyssGrouper:
                    AddPrim(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.55f, 0.45f, 1.1f), mat);
                    break;
                case FishSpecies.AmberJack:
                    AddPrim(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.35f, 0.28f, 0.95f), mat);
                    break;
                default:
                    AddPrim(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.22f, 0.18f, 0.55f), mat);
                    break;
            }
        }

        void AddPrim(PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(bodyRoot, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (type == PrimitiveType.Capsule || type == PrimitiveType.Cylinder)
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        public void OnHooked()
        {
            hooked = true;
            velocity *= 0.15f;
        }

        public void ReleaseHook() => hooked = false;

        public void DespawnCaught() => Destroy(gameObject);

        void Update()
        {
            if (hooked)
            {
                transform.position += Random.insideUnitSphere * (0.35f * Time.deltaTime);
                return;
            }

            float dt = Time.deltaTime;
            turn += dt * (IsHostile ? 1.2f : 0.7f);
            Vector3 wander = new Vector3(Mathf.Sin(turn), Mathf.Sin(turn * 0.37f) * 0.2f, Mathf.Cos(turn * 0.9f));
            Vector3 toHome = home - transform.position;
            Vector3 desired;

            var swim = SwimController.Instance;
            bool chase = false;
            if (IsHostile && swim != null && swim.IsUnderwater && !GameUi.BlocksGameplay)
            {
                float dist = Vector3.Distance(transform.position, swim.transform.position);
                if (dist < 18f)
                {
                    chase = true;
                    desired = (swim.transform.position - transform.position).normalized * (speed * 1.35f);
                    if (dist < 1.6f && attackCooldown <= 0f)
                    {
                        AttackPlayer(swim);
                        attackCooldown = 1.4f;
                    }
                }
                else
                {
                    desired = (wander + toHome * 0.05f).normalized * speed;
                }
            }
            else if (Species == FishSpecies.MorayEel)
            {
                // Stay near dens; occasional short lunges.
                float lunge = Mathf.Sin(turn * 2.2f);
                desired = toHome * 0.35f + wander * 0.25f;
                if (lunge > 0.85f)
                    desired += transform.forward * 1.5f;
                desired = desired.normalized * speed;
                // Keep close to home den.
                if (toHome.magnitude > 3.5f)
                    desired = toHome.normalized * speed;
            }
            else if (Species == FishSpecies.EagleRay)
            {
                desired = (wander + Vector3.right * Mathf.Sin(turn * 0.4f)).normalized * speed;
                desired.y *= 0.25f;
                // Prefer near seabed.
                if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out var hit, 40f))
                {
                    float targetY = hit.point.y + 1.2f;
                    desired.y += (targetY - transform.position.y) * 0.5f;
                }
            }
            else if (Species == FishSpecies.SeaTurtle)
            {
                desired = (wander * 0.6f + toHome * 0.04f).normalized * speed;
                desired.y *= 0.5f;
            }
            else
            {
                desired = (wander + toHome * 0.08f).normalized * speed;
            }

            float depth = DiveWorldConfig.WaterSurfaceY - transform.position.y;
            BandRange(SpeciesBand(Species), out float minD, out float maxD);
            if (!chase)
            {
                if (depth < minD + 1f) desired.y -= 1.5f;
                if (depth > maxD - 1f) desired.y += 1.5f;
            }

            Vector3 flat = new Vector3(transform.position.x, 0f, transform.position.z);
            if (flat.magnitude > DiveWorldConfig.SoftBoundaryRadius * 0.75f)
                desired += -flat.normalized * 2f;

            velocity = Vector3.Lerp(velocity, desired, dt * (chase ? 2.4f : 1.5f));
            transform.position += velocity * dt;
            if (velocity.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(velocity.normalized), dt * 3.5f);

            if (attackCooldown > 0f)
                attackCooldown -= dt;

            // Subtle wing/body motion for rays/turtles.
            if (bodyRoot != null && (Species == FishSpecies.EagleRay || Species == FishSpecies.SeaTurtle))
            {
                float flap = Mathf.Sin(Time.time * (Species == FishSpecies.EagleRay ? 3.5f : 2f)) * 8f;
                bodyRoot.localRotation = Quaternion.Euler(flap, 0f, 0f);
            }
        }

        void AttackPlayer(SwimController swim)
        {
            var suit = swim.GetComponent<DiverSuit>();
            if (suit != null)
                suit.DrainAir(8f);

            var rb = swim.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 push = (swim.transform.position - transform.position).normalized;
                push.y = 0.15f;
                rb.AddForce(push * 6.5f, ForceMode.VelocityChange);
            }
        }

        public static DepthBand SpeciesBand(FishSpecies s) => s switch
        {
            FishSpecies.AmberJack => DepthBand.Mid,
            FishSpecies.AbyssGrouper => DepthBand.Deep,
            FishSpecies.MorayEel => DepthBand.Shallow,
            FishSpecies.SeaTurtle => DepthBand.Shallow,
            FishSpecies.EagleRay => DepthBand.Mid,
            FishSpecies.Barracuda => DepthBand.Mid,
            _ => DepthBand.Shallow
        };

        public static void BandRange(DepthBand band, out float minD, out float maxD)
        {
            switch (band)
            {
                case DepthBand.Shallow:
                    minD = 2f; maxD = DiveWorldConfig.ShallowMaxDepth; break;
                case DepthBand.Mid:
                    minD = DiveWorldConfig.ShallowMaxDepth; maxD = DiveWorldConfig.MidMaxDepth; break;
                default:
                    minD = DiveWorldConfig.MidMaxDepth; maxD = DiveWorldConfig.MaxDepthMeters - 2f; break;
            }
        }
    }
}
