using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Builds the playable ocean dive prototype at runtime.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        static bool booted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (booted || FindFirstObjectByType<GameBootstrap>() != null || FindFirstObjectByType<SwimController>() != null)
                return;
            var go = new GameObject("GameBootstrap");
            go.AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            if (booted)
            {
                Destroy(gameObject);
                return;
            }
            booted = true;
            DontDestroyOnLoad(gameObject);
            GameUi.ClearBlocking();
            if (Time.timeScale <= 0.001f)
                Time.timeScale = 1f;
            BuildWorld();
        }

        void BuildWorld()
        {
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (cam.CompareTag("MainCamera") || cam.gameObject.name.Contains("Main Camera"))
                    Destroy(cam.gameObject);
            }

            var root = new GameObject("OceanWorld").transform;
            root.SetParent(transform, false);

            var water = new GameObject("WaterVolume");
            water.transform.SetParent(root, false);
            water.AddComponent<WaterVolume>();

            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.antiAliasing = 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(root, false);
            // Bright Maldives noon — white sand / turquoise water read.
            sunGo.transform.rotation = Quaternion.Euler(58f, -28f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            sun.intensity = 1.55f;
            sun.color = new Color(1f, 0.98f, 0.9f);

            RenderSettings.skybox = null;
            RenderSettings.subtractiveShadowColor = new Color(0.35f, 0.55f, 0.7f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.8f, 0.88f);

            // Starting location: sunny shore walk-in (OpenOcean site with beach geometry).
            DiveWorldConfig.SetActiveSite(DiveWorldConfig.DiveSite.OpenOcean);

            OceanFloorBuilder.Build(root);
            WaveOceanSurface.Create(root);
            DayNightCycle.Create(root, sun);
            UnderwaterAtmosphere.Create(root, sun);
            CausticShimmer.Create(root, sun);
            OceanAmbience.Create(root);

            // Economy services on bootstrap object.
            gameObject.AddComponent<Wallet>();
            gameObject.AddComponent<Inventory>();
            gameObject.AddComponent<ShopProgress>();
            gameObject.AddComponent<CosmeticLocker>();

            CreatePlayer(root);

            // Zone flavour + life + loot after floor collider exists.
            DepthZoneVisuals.Build(root);
            PerimeterScatter.Build(root);
            DeepCoralGrove.Create(root);
            FishSchoolSpawner.Create(root);
            ArtifactSpawner.Create(root);
            // Three beach shops in an arc behind the waterline spawn.
            GearMarket.Create(root);
            DecorShop.Create(root);
            CoralDiveShop.Create(root, DiveWorldConfig.BeachSuppliesPos, 0, new System.Random(77),
                DiveWorldConfig.BeachSuppliesYaw, beachStall: true);

            DiveHud.Create();
            CreateHorizonCard(root);

            if (ShopProgress.Instance != null)
                ShopProgress.Instance.ApplyToPlayer();
            if (CosmeticLocker.Instance != null)
                CosmeticLocker.Instance.ApplyVisuals();
        }

        void CreatePlayer(Transform root)
        {
            var player = new GameObject("Diver");
            player.tag = "Player";
            player.transform.SetParent(root, false);
            // Beach waterline — feet on wet sand, facing +Z into calm open water.
            player.transform.position = DiveWorldConfig.PlayerSpawn;
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            var rb = player.AddComponent<Rigidbody>();
            rb.mass = 80f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            var col = player.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.35f;
            col.center = new Vector3(0f, 0f, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "SuitBody";
            Destroy(body.GetComponent<CapsuleCollider>());
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            body.GetComponent<MeshRenderer>().sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.SuitAlbedo,
                ProceduralTextures.SuitNormal,
                Color.white,
                0.35f,
                0.15f,
                new Vector2(1.5f, 1.5f));

            var tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tank.name = "Tank";
            Destroy(tank.GetComponent<CapsuleCollider>());
            tank.transform.SetParent(player.transform, false);
            tank.transform.localPosition = new Vector3(0f, 0.15f, -0.28f);
            tank.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tank.transform.localScale = new Vector3(0.22f, 0.28f, 0.22f);
            tank.GetComponent<MeshRenderer>().sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.RockAlbedo,
                ProceduralTextures.RockNormal,
                new Color(0.75f, 0.78f, 0.8f),
                0.6f,
                0.55f,
                new Vector2(1f, 1f));

            var camPivot = new GameObject("CameraPivot").transform;
            camPivot.SetParent(player.transform, false);
            camPivot.localPosition = new Vector3(0f, 0.55f, 0f);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(camPivot, false);
            camGo.transform.localPosition = Vector3.zero;
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            // Long vista for the 1.5 km coastal shelf from the beach.
            cam.farClipPlane = 2800f;
            cam.fieldOfView = 72f;
            cam.allowMSAA = true;
            cam.allowHDR = true;
            camGo.AddComponent<AudioListener>();
            UnderwaterVision.Attach(cam);

            body.GetComponent<MeshRenderer>().enabled = false;
            tank.GetComponent<MeshRenderer>().enabled = false;

            player.AddComponent<DiverSuit>();
            var look = player.AddComponent<DiverLook>();
            look.Bind(camPivot);
            var swim = player.AddComponent<SwimController>();
            swim.enabled = true;
            swim.SetCameraPivot(camPivot);

            var gun = player.AddComponent<Speargun>();
            gun.Bind(camPivot);

            var torch = player.AddComponent<DiverFlashlight>();
            torch.Bind(camPivot);

            BubbleParticles.CreateAroundPlayer(player.transform);
        }

        void CreateHorizonCard(Transform root)
        {
            var skirt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            skirt.name = "HorizonSeabed";
            Destroy(skirt.GetComponent<Collider>());
            skirt.transform.SetParent(root, false);
            skirt.transform.position = new Vector3(0f, DiveWorldConfig.OceanFloorY + 6f, 0f);
            float d = DiveWorldConfig.OceanRadius * 2.2f;
            skirt.transform.localScale = new Vector3(d, 0.2f, d);
            skirt.GetComponent<MeshRenderer>().sharedMaterial = ProceduralTextures.MakeLit(
                ProceduralTextures.SiltAlbedo,
                ProceduralTextures.SiltNormal,
                Color.white,
                0.1f,
                0.04f,
                new Vector2(80f, 80f));
        }
    }
}
