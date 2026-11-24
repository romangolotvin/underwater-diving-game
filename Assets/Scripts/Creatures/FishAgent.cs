using System.Collections.Generic;
using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Runtime fish: textured primitive assemblies + swim motion. Stats from CreatureCatalog.
    /// </summary>
    public class FishAgent : MonoBehaviour
    {
        public static readonly List<FishAgent> Active = new();

        public FishSpecies Species { get; private set; }
        public bool IsHostile { get; private set; }

        CreatureStats stats;
        Vector3 velocity;
        Vector3 home;
        float speed;
        float turn;
        bool hooked;
        float attackCooldown;
        Transform bodyRoot;
        float animPhase;
        float flapAmp = 10f;
        float flapRate = 3f;

        public static FishAgent Spawn(Transform parent, FishSpecies species, Vector3 pos)
        {
            var stats = CreatureCatalog.Get(species);
            var root = new GameObject("Fish_" + species);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.localScale = Vector3.one * Mathf.Max(0.35f, stats.Scale);

            var fish = root.AddComponent<FishAgent>();
            fish.Species = species;
            fish.stats = stats;
            fish.IsHostile = stats.Hostile;
            fish.home = pos;
            fish.speed = stats.Speed;
            fish.turn = Random.Range(0f, 360f);
            fish.animPhase = Random.Range(0f, Mathf.PI * 2f);
            fish.ConfigureAnim(stats.Body);
            fish.BuildVisual(stats);
            fish.velocity = Random.onUnitSphere;
            fish.velocity.y *= 0.3f;
            fish.velocity = fish.velocity.normalized * fish.speed * 0.45f;

            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = stats.ColliderRadius;
            return fish;
        }

        void ConfigureAnim(CreatureBody body)
        {
            switch (body)
            {
                case CreatureBody.Ray:
                    flapAmp = 14f; flapRate = 3.6f; break;
                case CreatureBody.Turtle:
                    flapAmp = 8f; flapRate = 2.1f; break;
                case CreatureBody.Shark:
                case CreatureBody.Dolphin:
                    flapAmp = 7f; flapRate = 2.8f; break;
                case CreatureBody.Eel:
                case CreatureBody.Snake:
                    flapAmp = 18f; flapRate = 4.2f; break;
                case CreatureBody.Jelly:
                    flapAmp = 12f; flapRate = 1.6f; break;
                case CreatureBody.Seahorse:
                    flapAmp = 6f; flapRate = 5f; break;
                default:
                    flapAmp = 9f; flapRate = 3.2f; break;
            }
        }

        void BuildVisual(CreatureStats s)
        {
            bodyRoot = new GameObject("Body").transform;
            bodyRoot.SetParent(transform, false);

            Texture2D albedo = s.Body switch
            {
                CreatureBody.Turtle => ProceduralTextures.ShellAlbedo,
                CreatureBody.Ray => ProceduralTextures.RayAlbedo,
                CreatureBody.Shark => ProceduralTextures.FishScaleAlbedo,
                _ => ProceduralTextures.FishScaleAlbedo
            };
            Texture2D normal = s.Body switch
            {
                CreatureBody.Turtle => ProceduralTextures.ShellNormal,
                CreatureBody.Ray => ProceduralTextures.RayNormal,
                _ => ProceduralTextures.FishScaleNormal
            };

            Material mat = MakeFishMat(albedo, normal, s.Tint, 0.55f);
            Material dark = MakeFishMat(albedo, normal, Color.Lerp(s.Tint, Color.black, 0.28f), 0.4f);

            switch (s.Body)
            {
                case CreatureBody.Eel:
                case CreatureBody.Snake:
                    AddPrim(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.22f, 0.22f, 1.5f), mat, true);
                    AddPrim(PrimitiveType.Sphere, new Vector3(0f, 0.04f, 0.7f), Vector3.one * 0.28f, mat, false);
                    break;
                case CreatureBody.Turtle:
                    AddPrim(PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), new Vector3(0.95f, 0.42f, 1.15f), mat, false);
                    AddPrim(PrimitiveType.Cube, new Vector3(0.55f, 0f, 0.1f), new Vector3(0.4f, 0.08f, 0.55f), dark, false);
                    AddPrim(PrimitiveType.Cube, new Vector3(-0.55f, 0f, 0.1f), new Vector3(0.4f, 0.08f, 0.55f), dark, false);
                    AddPrim(PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0.7f), Vector3.one * 0.3f, mat, false);
                    break;
                case CreatureBody.Ray:
                    AddPrim(PrimitiveType.Sphere, Vector3.zero, new Vector3(1.7f, 0.16f, 0.95f), mat, false);
                    AddPrim(PrimitiveType.Capsule, new Vector3(0f, 0f, -0.85f), new Vector3(0.1f, 0.1f, 1f), dark, true);
                    break;
                case CreatureBody.Shark:
                    AddPrim(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.45f, 0.35f, 1.6f), mat, true);
                    AddPrim(PrimitiveType.Cube, new Vector3(0f, 0.32f, -0.1f), new Vector3(0.06f, 0.4f, 0.45f), dark, false);
                    AddPrim(PrimitiveType.Cube, new Vector3(0f, 0.05f, -0.85f), new Vector3(0.55f, 0.08f, 0.25f), dark, false);
                    if (Species == FishSpecies.Hammerhead)
                        AddPrim(PrimitiveType.Cube, new Vector3(0f, 0.05f, 0.75f), new Vector3(0.95f, 0.12f, 0.22f), mat, false);
                    if (Species == FishSpecies.ThresherShark)
                        AddPrim(PrimitiveType.Capsule, new Vector3(0f, 0.35f, -1.1f), new Vector3(0.08f, 0.08f, 1.1f), dark, true);
                    break;
                case CreatureBody.Dolphin:
                    AddPrim(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.4f, 0.32f, 1.4f), mat, true);
                    AddPrim(PrimitiveType.Cube, new Vector3(0f, 0.28f, -0.15f), new Vector3(0.05f, 0.28f, 0.35f), dark, false);
                    AddPrim(PrimitiveType.Sphere, new Vector3(0f, 0f, 0.65f), new Vector3(0.28f, 0.25f, 0.35f), mat, false);
                    break;
                case CreatureBody.Jelly:
                    AddPrim(PrimitiveType.Sphere, new Vector3(0f, 0.15f, 0f), new Vector3(0.9f, 0.55f, 0.9f), mat, false);
                    AddPrim(PrimitiveType.Cylinder, new Vector3(0f, -0.35f, 0f), new Vector3(0.15f, 0.45f, 0.15f), dark, false);
                    AddPrim(PrimitiveType.Cylinder, new Vector3(0.2f, -0.4f, 0.1f), new Vector3(0.08f, 0.5f, 0.08f), dark, false);
                    AddPrim(PrimitiveType.Cylinder, new Vector3(-0.2f, -0.4f, -0.1f), new Vector3(0.08f, 0.5f, 0.08f), dark, false);
                    break;
                case CreatureBody.Squid:
                    AddPrim(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.35f, 0.35f, 0.9f), mat, true);
                    AddPrim(PrimitiveType.Cylinder, new Vector3(0.15f, 0f, -0.7f), new Vector3(0.06f, 0.45f, 0.06f), dark, true);
                    AddPrim(PrimitiveType.Cylinder, new Vector3(-0.15f, 0f, -0.7f), new Vector3(0.06f, 0.45f, 0.06f), dark, true);
                    AddPrim(PrimitiveType.Cylinder, new Vector3(0f, 0.12f, -0.75f), new Vector3(0.06f, 0.5f, 0.06f), dark, true);
                    break;
                case CreatureBody.Seahorse:
                    AddPrim(PrimitiveType.Capsule, new Vector3(0f, 0.1f, 0f), new Vector3(0.2f, 0.35f, 0.2f), mat, false);
                    AddPrim(PrimitiveType.Sphere, new Vector3(0f, 0.4f, 0.12f), Vector3.one * 0.22f, mat, false);
                    AddPrim(PrimitiveType.Cube, new Vector3(0f, -0.25f, 0f), new Vector3(0.12f, 0.35f, 0.08f), dark, false);
                    break;
                case CreatureBody.Isopod:
                    AddPrim(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.55f, 0.28f, 1.0f), mat, true);
                    AddPrim(PrimitiveType.Cube, new Vector3(0.35f, -0.05f, 0.2f), new Vector3(0.2f, 0.06f, 0.08f), dark, false);
                    AddPrim(PrimitiveType.Cube, new Vector3(-0.35f, -0.05f, 0.2f), new Vector3(0.2f, 0.06f, 0.08f), dark, false);
                    break;
                case CreatureBody.Crayfish:
                    AddPrim(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.3f, 0.2f, 0.7f), mat, true);
                    AddPrim(PrimitiveType.Cube, new Vector3(0.35f, 0f, 0.25f), new Vector3(0.35f, 0.08f, 0.12f), dark, false);
                    AddPrim(PrimitiveType.Cube, new Vector3(-0.35f, 0f, 0.25f), new Vector3(0.35f, 0.08f, 0.12f), dark, false);
                    break;
                case CreatureBody.BulkyFish:
                    AddPrim(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.5f, 0.42f, 1.15f), mat, true);
                    AddPrim(PrimitiveType.Cube, new Vector3(0f, 0.28f, -0.15f), new Vector3(0.05f, 0.3f, 0.35f), dark, false);
                    break;
                case CreatureBody.LongFish:
                    AddPrim(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.32f, 0.26f, 1.35f), mat, true);
                    AddPrim(PrimitiveType.Cube, new Vector3(0f, 0.2f, -0.2f), new Vector3(0.04f, 0.28f, 0.4f), dark, false);
                    AddPrim(PrimitiveType.Cube, new Vector3(0f, 0.02f, -0.75f), new Vector3(0.35f, 0.06f, 0.2f), dark, false);
                    break;
                default: // SmallFish
                    AddPrim(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.24f, 0.2f, 0.65f), mat, true);
                    AddPrim(PrimitiveType.Cube, new Vector3(0f, 0.12f, -0.15f), new Vector3(0.03f, 0.16f, 0.22f), dark, false);
                    break;
            }
        }

        static Material MakeFishMat(Texture2D albedo, Texture2D normal, Color tint, float gloss)
        {
            var shader = Shader.Find("Underwater/FishScales");
            if (shader != null)
            {
                var mat = new Material(shader) { name = "FishMat" };
                mat.SetColor("_Color", tint);
                mat.SetTexture("_MainTex", albedo);
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_Glossiness", gloss);
                mat.SetFloat("_SpecIntensity", 1.5f);
                mat.SetFloat("_SSSStrength", 0.4f);
                mat.mainTextureScale = new Vector2(2.5f, 2.5f);
                return mat;
            }
            return ProceduralTextures.MakeLit(albedo, normal, tint, gloss, 0.08f, new Vector2(2.5f, 2.5f));
        }

        void AddPrim(PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat, bool layFlat)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(bodyRoot, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (layFlat && (type == PrimitiveType.Capsule || type == PrimitiveType.Cylinder))
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
            animPhase += Time.deltaTime * flapRate * (1f + velocity.magnitude * 0.15f);

            if (hooked)
            {
                transform.position += Random.insideUnitSphere * (0.35f * Time.deltaTime);
                AnimateBody(1.6f);
                return;
            }

            float dt = Time.deltaTime;
            turn += dt * (IsHostile ? 1.25f : 0.7f);
            Vector3 wander = new Vector3(Mathf.Sin(turn), Mathf.Sin(turn * 0.37f) * 0.22f, Mathf.Cos(turn * 0.9f));
            Vector3 toHome = home - transform.position;
            Vector3 desired;
            bool chase = false;

            var swim = SwimController.Instance;
            if (IsHostile && swim != null && swim.IsUnderwater && !GameUi.BlocksGameplay)
            {
                float dist = Vector3.Distance(transform.position, swim.transform.position);
                float aggro = stats.Body == CreatureBody.Shark ? 22f : 16f;
                if (dist < aggro)
                {
                    chase = true;
                    desired = (swim.transform.position - transform.position).normalized * (speed * 1.35f);
                    if (dist < 1.7f && attackCooldown <= 0f)
                    {
                        AttackPlayer(swim);
                        attackCooldown = 1.35f;
                    }
                }
                else desired = (wander + toHome * 0.05f).normalized * speed;
            }
            else
            {
                desired = DesiredByBody(wander, toHome);
            }

            float depth = DiveWorldConfig.WaterSurfaceY - transform.position.y;
            BandRange(stats.PreferredBand, out float minD, out float maxD);
            if (!chase)
            {
                if (depth < minD + 1f) desired.y -= 1.5f;
                if (depth > maxD - 1f) desired.y += 1.5f;
            }

            Vector3 flat = new Vector3(transform.position.x, 0f, transform.position.z);
            if (flat.magnitude > DiveWorldConfig.SoftBoundaryRadius * 0.75f)
                desired += -flat.normalized * 2f;

            velocity = Vector3.Lerp(velocity, desired, dt * (chase ? 2.5f : 1.5f));
            transform.position += velocity * dt;
            if (velocity.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(velocity.normalized), dt * 3.5f);

            if (attackCooldown > 0f)
                attackCooldown -= dt;

            AnimateBody(chase ? 1.4f : 1f);
        }

        Vector3 DesiredByBody(Vector3 wander, Vector3 toHome)
        {
            switch (stats.Body)
            {
                case CreatureBody.Eel:
                case CreatureBody.Snake:
                {
                    float lunge = Mathf.Sin(turn * 2.2f);
                    Vector3 d = toHome * 0.35f + wander * 0.25f;
                    if (lunge > 0.85f) d += transform.forward * 1.5f;
                    if (toHome.magnitude > 3.5f) d = toHome.normalized * speed;
                    return d.normalized * speed;
                }
                case CreatureBody.Ray:
                {
                    Vector3 d = (wander + Vector3.right * Mathf.Sin(turn * 0.4f)).normalized * speed;
                    d.y *= 0.25f;
                    if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out var hit, 40f))
                        d.y += (hit.point.y + 1.2f - transform.position.y) * 0.5f;
                    return d;
                }
                case CreatureBody.Turtle:
                    return (wander * 0.55f + toHome * 0.04f).normalized * speed;
                case CreatureBody.Jelly:
                    return (Vector3.up * Mathf.Sin(turn * 1.5f) * 0.6f + wander * 0.4f).normalized * speed;
                case CreatureBody.Crayfish:
                case CreatureBody.Isopod:
                {
                    Vector3 d = wander.normalized * (speed * 0.7f);
                    if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out var hit, 30f))
                        d.y += (hit.point.y + 0.5f - transform.position.y) * 0.8f;
                    return d;
                }
                default:
                    return (wander + toHome * 0.08f).normalized * speed;
            }
        }

        void AnimateBody(float mul)
        {
            if (bodyRoot == null)
                return;
            float wave = Mathf.Sin(animPhase) * flapAmp * mul;
            switch (stats.Body)
            {
                case CreatureBody.Ray:
                case CreatureBody.Turtle:
                    bodyRoot.localRotation = Quaternion.Euler(wave, 0f, 0f);
                    break;
                case CreatureBody.Eel:
                case CreatureBody.Snake:
                    bodyRoot.localRotation = Quaternion.Euler(0f, wave, 0f);
                    break;
                case CreatureBody.Jelly:
                    float squash = 1f + Mathf.Sin(animPhase) * 0.08f * mul;
                    bodyRoot.localScale = new Vector3(squash, 1f / squash, squash);
                    break;
                default:
                    bodyRoot.localRotation = Quaternion.Euler(0f, 0f, wave * 0.45f);
                    break;
            }
        }

        void AttackPlayer(SwimController swim)
        {
            var suit = swim.GetComponent<DiverSuit>();
            if (suit != null)
                suit.DrainAir(Mathf.Max(6f, stats.AttackAirDrain));

            var rb = swim.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 push = (swim.transform.position - transform.position).normalized;
                push.y = 0.15f;
                rb.AddForce(push * (stats.Body == CreatureBody.Shark ? 9f : 6.5f), ForceMode.VelocityChange);
            }
        }

        public static DepthBand SpeciesBand(FishSpecies s) => CreatureCatalog.Get(s).PreferredBand;

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
