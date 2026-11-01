using System;
using System.IO;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Збирає сцену проєкту «ТАНКИ» (уроки 71–75): арену, танки, снаряди, ефекти, звуки, NavMesh та UI.
public static class TanksSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string TmpEssentials = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

    [MenuItem("Tanks/1. Import TMP Essentials")]
    public static void ImportTmpEssentials()
    {
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
            return;

        AssetDatabase.ImportPackage(TmpEssentials, false);
        AssetDatabase.Refresh();
    }

    [MenuItem("Tanks/2. Rebuild Scene")]
    public static void Build()
    {
        int groundLayer = EnsureLayer("Ground");
        int tankLayer = EnsureLayer("Tank");

        foreach (string folder in new[]
        {
            "Assets/Art", "Assets/Art/Materials", "Assets/Art/Models", "Assets/Art/Textures",
            "Assets/Audio", "Assets/Prefabs", "Assets/Prefabs/Projectiles", "Assets/Prefabs/VFX",
            "Assets/Prefabs/Tanks", "Assets/Prefabs/Pickups", "Assets/Scenes",
            "Assets/ScriptableObjects", "Assets/ScriptableObjects/Tanks", "Assets/VFX"
        })
        {
            EnsureFolder(folder);
        }

        // Звуки: прості синтезовані заглушки, щоб усе працювало без сторонніх файлів.
        AudioClip shootClip = MakeClip("Shoot", 0.35f, ShootSample);
        AudioClip impactClip = MakeClip("Impact", 0.4f, ImpactSample);
        AudioClip explosionClip = MakeClip("Explosion", 1.6f, ExplosionSample);
        AudioClip engineClip = MakeClip("EngineLoop", 1f, EngineSample);

        // Матеріали.
        Material groundMat = MakeMaterial("Ground", new Color(0.36f, 0.45f, 0.3f));
        Material wallMat = MakeMaterial("Wall", new Color(0.55f, 0.53f, 0.5f));
        Material playerMat = MakeMaterial("PlayerBody", new Color(0.33f, 0.45f, 0.2f));
        Material playerTurretMat = MakeMaterial("PlayerTurret", new Color(0.25f, 0.35f, 0.15f));
        Material enemyMat = MakeMaterial("EnemyBody", new Color(0.55f, 0.18f, 0.15f));
        Material enemyTurretMat = MakeMaterial("EnemyTurret", new Color(0.4f, 0.12f, 0.1f));
        Material projectileMat = MakeMaterial("Projectile", new Color(1f, 0.85f, 0.2f));
        projectileMat.EnableKeyword("_EMISSION");
        projectileMat.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.1f) * 2f);
        Material pickupMat = MakeMaterial("Repair", new Color(0.2f, 0.8f, 0.9f));
        Material particleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
        Material trailMat = MakeTrailMaterial();

        // VFX префаби.
        ImpactEffect impactPrefab = MakeEffectPrefab(
            "ImpactEffect", particleMat, impactClip, 2f,
            new Color(1f, 0.75f, 0.3f), 12, 25, 3f, 6f, 0.15f, 0.4f, 0.3f);

        ImpactEffect explosionPrefab = MakeEffectPrefab(
            "TankExplosion", particleMat, explosionClip, 3f,
            new Color(1f, 0.45f, 0.1f), 40, 60, 4f, 10f, 0.6f, 1.6f, 0.8f);

        // Снаряд.
        Projectile projectilePrefab = MakeProjectilePrefab(projectileMat, trailMat, impactPrefab);

        // Конфіги.
        TankConfig playerConfig = MakeConfig("PlayerTankConfig", 25, 18f, 0.7f, 5f, projectilePrefab);
        MakeConfig("RapidTankConfig", 10, 24f, 0.25f, 4f, projectilePrefab);
        TankConfig enemyConfig = MakeConfig("EnemyTankConfig", 10, 16f, 1.2f, 5f, projectilePrefab);

        // Танки.
        GameObject playerPrefab = MakeTankPrefab(
            "PlayerTank", false, playerMat, playerTurretMat, particleMat, shootClip, engineClip,
            playerConfig, explosionPrefab.gameObject, tankLayer);

        GameObject enemyPrefab = MakeTankPrefab(
            "EnemyTank", true, enemyMat, enemyTurretMat, particleMat, shootClip, null,
            enemyConfig, explosionPrefab.gameObject, tankLayer);

        GameObject pickupPrefab = MakeRepairPickupPrefab(pickupMat);

        // Сцена.
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(5f, 1f, 5f);
        ground.layer = groundLayer;
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;
        ground.isStatic = true;

        GameObject arena = new GameObject("Arena");
        MakeWall(arena.transform, "Wall_North", new Vector3(0f, 1.5f, 25f), new Vector3(50f, 3f, 1f), wallMat);
        MakeWall(arena.transform, "Wall_South", new Vector3(0f, 1.5f, -25f), new Vector3(50f, 3f, 1f), wallMat);
        MakeWall(arena.transform, "Wall_East", new Vector3(25f, 1.5f, 0f), new Vector3(1f, 3f, 50f), wallMat);
        MakeWall(arena.transform, "Wall_West", new Vector3(-25f, 1.5f, 0f), new Vector3(1f, 3f, 50f), wallMat);
        MakeWall(arena.transform, "Block_01", new Vector3(-8f, 1.5f, 4f), new Vector3(4f, 3f, 4f), wallMat);
        MakeWall(arena.transform, "Block_02", new Vector3(9f, 1.5f, -3f), new Vector3(3f, 3f, 7f), wallMat);
        MakeWall(arena.transform, "Block_03", new Vector3(2f, 1.5f, 12f), new Vector3(8f, 3f, 2f), wallMat);
        MakeWall(arena.transform, "Block_04", new Vector3(-14f, 1.5f, -12f), new Vector3(5f, 3f, 3f), wallMat);
        MakeWall(arena.transform, "Block_05", new Vector3(15f, 1.5f, 15f), new Vector3(3f, 3f, 3f), wallMat);

        // NavMesh: запікаємо все, крім танків.
        GameObject navigation = new GameObject("Navigation");
        NavMeshSurface surface = navigation.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~(1 << tankLayer);
        surface.BuildNavMesh();
        string navMeshPath = "Assets/Scenes/Game_NavMesh.asset";
        AssetDatabase.DeleteAsset(navMeshPath);
        AssetDatabase.CreateAsset(surface.navMeshData, navMeshPath);

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -18f), Quaternion.identity);

        Transform route1 = MakePatrolRoute("PatrolRoute_01", new[]
        {
            new Vector3(-16f, 0f, 18f), new Vector3(-4f, 0f, 18f), new Vector3(-4f, 0f, 6f), new Vector3(-17f, 0f, 6f)
        });
        Transform route2 = MakePatrolRoute("PatrolRoute_02", new[]
        {
            new Vector3(18f, 0f, 5f), new Vector3(18f, 0f, -18f), new Vector3(4f, 0f, -10f)
        });

        MakeEnemy(enemyPrefab, "EnemyTank_01", new Vector3(-10f, 0f, 18f), player.transform, route1);
        MakeEnemy(enemyPrefab, "EnemyTank_02", new Vector3(18f, 0f, 0f), player.transform, route2);

        GameObject pickups = new GameObject("Pickups");
        foreach (Vector3 position in new[] { new Vector3(-18f, 0.8f, -18f), new Vector3(18f, 0.8f, 20f) })
        {
            GameObject pickup = (GameObject)PrefabUtility.InstantiatePrefab(pickupPrefab, pickups.transform);
            pickup.transform.position = position;
        }

        Camera mainCamera = Camera.main;
        CameraFollow follow = mainCamera.gameObject.AddComponent<CameraFollow>();
        SetField(follow, "target", player.transform);
        mainCamera.transform.position = player.transform.position + new Vector3(0f, 12f, -8f);
        mainCamera.transform.LookAt(player.transform);

        MakePlayerHud(player.GetComponent<TankHealth>());

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();

        Debug.Log("[TanksSceneBuilder] Scene built: " + ScenePath);
    }

    // ---------- Tanks ----------

    private static GameObject MakeTankPrefab(
        string name, bool isEnemy, Material bodyMat, Material turretMat, Material particleMat,
        AudioClip shootClip, AudioClip engineClip, TankConfig config, GameObject explosionPrefab, int tankLayer)
    {
        GameObject root = new GameObject(name);
        root.layer = tankLayer;
        if (!isEnemy)
            root.tag = "Player";

        BoxCollider rootCollider = root.AddComponent<BoxCollider>();
        rootCollider.center = new Vector3(0f, 0.75f, 0f);
        rootCollider.size = new Vector3(2f, 1.5f, 3f);

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.mass = 5f;
        body.linearDamping = 1f;
        body.angularDamping = 5f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        GameObject hull = MakePart(PrimitiveType.Cube, "Body", root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(2f, 0.8f, 3f), bodyMat);
        hull.layer = tankLayer;

        GameObject turretPivot = new GameObject("TurretPivot");
        turretPivot.transform.SetParent(root.transform, false);
        turretPivot.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        turretPivot.layer = tankLayer;

        MakePart(PrimitiveType.Cube, "Turret", turretPivot.transform, new Vector3(0f, 0.25f, 0f), new Vector3(1.2f, 0.5f, 1.4f), turretMat).layer = tankLayer;
        GameObject barrel = MakePart(PrimitiveType.Cylinder, "Barrel", turretPivot.transform, new Vector3(0f, 0.25f, 1.4f), new Vector3(0.25f, 0.7f, 0.25f), turretMat);
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        barrel.layer = tankLayer;

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(turretPivot.transform, false);
        firePoint.transform.localPosition = new Vector3(0f, 0.25f, 2.2f);

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(root.transform, false);

        ParticleSystem muzzleFlash = MakeMuzzleFlash(firePoint.transform, particleMat);

        AudioSource shotSource = root.AddComponent<AudioSource>();
        shotSource.playOnAwake = false;
        shotSource.spatialBlend = 1f;
        shotSource.minDistance = 4f;
        shotSource.maxDistance = 40f;
        shotSource.volume = 0.7f;

        TankShooter shooter = root.AddComponent<TankShooter>();
        SetField(shooter, "config", config);
        SetField(shooter, "firePoint", firePoint.transform);
        SetField(shooter, "ownerCollider", rootCollider);
        SetField(shooter, "usePlayerInput", !isEnemy);
        SetField(shooter, "muzzleFlash", muzzleFlash);
        SetField(shooter, "audioSource", shotSource);
        SetField(shooter, "shootClip", shootClip);

        TankHealth health = root.AddComponent<TankHealth>();
        SetField(health, "maxHealth", 100);
        SetField(health, "currentHealth", 100);

        TankDeath death = root.AddComponent<TankDeath>();
        SetField(death, "explosionPrefab", explosionPrefab);

        root.AddComponent<DamageFlash>();

        if (isEnemy)
        {
            // Ворогом керує NavMesh Agent, тому Rigidbody лише кінематичний.
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.speed = 3.5f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0f;
            agent.autoBraking = true;
            agent.radius = 1.5f;
            agent.height = 1.5f;

            EnemyTankAI ai = root.AddComponent<EnemyTankAI>();
            SetField(ai, "turretPivot", turretPivot.transform);
            SetField(ai, "shooter", shooter);

            MakeEnemyHealthBar(root.transform, health);
        }
        else
        {
            TankController controller = root.AddComponent<TankController>();
            SetField(controller, "turretPivot", turretPivot.transform);

            GameObject engineObject = new GameObject("EngineAudio");
            engineObject.transform.SetParent(root.transform, false);
            AudioSource engineSource = engineObject.AddComponent<AudioSource>();
            engineSource.clip = engineClip;
            engineSource.loop = true;
            engineSource.playOnAwake = true;
            engineSource.spatialBlend = 0.8f;
            engineSource.volume = 0f;

            TankEngineAudio engineAudio = root.AddComponent<TankEngineAudio>();
            SetField(engineAudio, "engineSource", engineSource);
        }

        return SavePrefab(root, "Assets/Prefabs/Tanks/" + name + ".prefab");
    }

    private static GameObject MakePart(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        // Один фізичний Collider на корені танка.
        UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        return part;
    }

    private static void MakeEnemy(GameObject prefab, string name, Vector3 position, Transform player, Transform route)
    {
        GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        enemy.name = name;
        enemy.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 180f, 0f));

        EnemyTankAI ai = enemy.GetComponent<EnemyTankAI>();
        SetField(ai, "player", player);

        Transform[] points = new Transform[route.childCount];
        for (int i = 0; i < route.childCount; i++)
            points[i] = route.GetChild(i);

        SerializedObject serialized = new SerializedObject(ai);
        SerializedProperty array = serialized.FindProperty("patrolPoints");
        array.arraySize = points.Length;
        for (int i = 0; i < points.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform MakePatrolRoute(string name, Vector3[] points)
    {
        Transform route = new GameObject(name).transform;
        for (int i = 0; i < points.Length; i++)
        {
            Transform point = new GameObject("Point_" + (i + 1).ToString("00")).transform;
            point.SetParent(route, false);
            point.position = points[i];
        }
        return route;
    }

    // ---------- Projectile & VFX ----------

    private static Projectile MakeProjectilePrefab(Material material, Material trailMat, ImpactEffect impactPrefab)
    {
        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = "Projectile";
        shell.transform.localScale = Vector3.one * 0.3f;
        shell.GetComponent<Renderer>().sharedMaterial = material;
        shell.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Rigidbody body = shell.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        TrailRenderer trail = shell.AddComponent<TrailRenderer>();
        trail.time = 0.18f;
        trail.minVertexDistance = 0.05f;
        trail.widthCurve = AnimationCurve.Linear(0f, 0.12f, 1f, 0f);
        trail.widthMultiplier = 1f;
        trail.alignment = LineAlignment.View;
        trail.textureMode = LineTextureMode.Stretch;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.sharedMaterial = trailMat;
        Gradient trailGradient = new Gradient();
        trailGradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.95f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.1f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = trailGradient;

        Projectile projectile = shell.AddComponent<Projectile>();
        SetField(projectile, "impactEffectPrefab", impactPrefab);

        return SavePrefab(shell, "Assets/Prefabs/Projectiles/Projectile.prefab").GetComponent<Projectile>();
    }

    private static ImpactEffect MakeEffectPrefab(
        string name, Material particleMat, AudioClip clip, float lifetime, Color color,
        short minCount, short maxCount, float minSpeed, float maxSpeed, float minSize, float maxSize, float particleLifetime)
    {
        GameObject root = new GameObject(name);

        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime * 0.6f, particleLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = color;
        main.gravityModifier = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, minCount, maxCount) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.2f;

        ApplyFade(particles, color);
        root.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMat;

        AudioSource source = root.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.volume = 0.7f;
        source.minDistance = 2f;
        source.maxDistance = 25f;
        source.rolloffMode = AudioRolloffMode.Linear;

        ImpactEffect effect = root.AddComponent<ImpactEffect>();
        SetField(effect, "particles", particles);
        SetField(effect, "audioSource", source);
        SetField(effect, "impactClip", clip);
        SetField(effect, "lifetime", lifetime);

        return SavePrefab(root, "Assets/Prefabs/VFX/" + name + ".prefab").GetComponent<ImpactEffect>();
    }

    private static ParticleSystem MakeMuzzleFlash(Transform firePoint, Material particleMat)
    {
        GameObject flash = new GameObject("MuzzleFlash");
        flash.transform.SetParent(firePoint, false);

        ParticleSystem particles = flash.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.15f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startColor = new Color(1f, 0.8f, 0.3f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 15) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.05f;

        ApplyFade(particles, new Color(1f, 0.8f, 0.3f));
        flash.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMat;
        return particles;
    }

    private static void ApplyFade(ParticleSystem particles, Color color)
    {
        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;
    }

    private static GameObject MakeRepairPickupPrefab(Material material)
    {
        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pickup.name = "RepairPickup";
        pickup.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        pickup.transform.rotation = Quaternion.Euler(45f, 0f, 45f);
        pickup.GetComponent<Renderer>().sharedMaterial = material;
        pickup.GetComponent<BoxCollider>().isTrigger = true;
        pickup.AddComponent<RepairPickup>();
        return SavePrefab(pickup, "Assets/Prefabs/Pickups/RepairPickup.prefab");
    }

    // ---------- UI ----------

    private static void MakePlayerHud(TankHealth playerHealth)
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panel = new GameObject("PlayerHealthPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(30f, 30f);
        panelRect.sizeDelta = new Vector2(420f, 90f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.45f);
        panelImage.raycastTarget = false;

        Slider slider = MakeSlider(panel.transform, "HealthSlider", out Image fill);
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.offsetMin = new Vector2(15f, 15f);
        sliderRect.offsetMax = new Vector2(-15f, 45f);

        GameObject textObject = new GameObject("HealthText", typeof(RectTransform));
        textObject.transform.SetParent(panel.transform, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = "100 / 100";
        text.fontSize = 28f;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.offsetMin = new Vector2(15f, -45f);
        textRect.offsetMax = new Vector2(-15f, -5f);

        TankHealthBar bar = panel.AddComponent<TankHealthBar>();
        SetField(bar, "health", playerHealth);
        SetField(bar, "slider", slider);
        SetField(bar, "healthText", text);
        SetField(bar, "fillImage", fill);

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void MakeEnemyHealthBar(Transform enemy, TankHealth health)
    {
        GameObject canvasObject = new GameObject("EnemyHealthCanvas", typeof(Canvas));
        canvasObject.transform.SetParent(enemy, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200f, 26f);
        canvasRect.localPosition = new Vector3(0f, 2.4f, 0f);
        canvasRect.localScale = Vector3.one * 0.01f;

        Slider slider = MakeSlider(canvasObject.transform, "HealthSlider", out Image fill);
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = sliderRect.offsetMax = Vector2.zero;

        canvasObject.AddComponent<Billboard>();

        TankHealthBar bar = canvasObject.AddComponent<TankHealthBar>();
        SetField(bar, "health", health);
        SetField(bar, "slider", slider);
        SetField(bar, "fillImage", fill);
        SetField(bar, "hideWhenFull", true);
        SetField(bar, "barRoot", slider.gameObject);
    }

    private static Slider MakeSlider(Transform parent, string name, out Image fill)
    {
        GameObject sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderObject.name = name;
        sliderObject.transform.SetParent(parent, false);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        Transform handleArea = sliderObject.transform.Find("Handle Slide Area");
        if (handleArea != null)
            UnityEngine.Object.DestroyImmediate(handleArea.gameObject);
        slider.handleRect = null;

        Image background = sliderObject.transform.Find("Background").GetComponent<Image>();
        background.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        background.raycastTarget = false;
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = backgroundRect.offsetMax = Vector2.zero;

        RectTransform fillArea = (RectTransform)sliderObject.transform.Find("Fill Area");
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(3f, 3f);
        fillArea.offsetMax = new Vector2(-3f, -3f);

        fill = fillArea.Find("Fill").GetComponent<Image>();
        fill.color = new Color(0.25f, 0.85f, 0.3f);
        fill.raycastTarget = false;
        fill.rectTransform.sizeDelta = Vector2.zero;

        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;
        return slider;
    }

    // ---------- Assets ----------

    private static void MakeWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = material;
        wall.isStatic = true;
    }

    private static TankConfig MakeConfig(string name, int damage, float speed, float cooldown, float lifetime, Projectile projectile)
    {
        string path = "Assets/ScriptableObjects/Tanks/" + name + ".asset";
        TankConfig config = AssetDatabase.LoadAssetAtPath<TankConfig>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<TankConfig>();
            AssetDatabase.CreateAsset(config, path);
        }

        config.damage = damage;
        config.projectileSpeed = speed;
        config.fireCooldown = cooldown;
        config.projectileLifetime = lifetime;
        config.projectilePrefab = projectile;
        EditorUtility.SetDirty(config);
        return config;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = "Assets/Art/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        material.SetFloat("_Glossiness", 0.25f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material MakeTrailMaterial()
    {
        string path = "Assets/Art/Materials/ProjectileTrail.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static void SetField(UnityEngine.Object target, string field, object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null)
            throw new ArgumentException(target.GetType().Name + " has no field " + field);

        switch (value)
        {
            case UnityEngine.Object reference: property.objectReferenceValue = reference; break;
            case bool flag: property.boolValue = flag; break;
            case int number: property.intValue = number; break;
            case float number: property.floatValue = number; break;
            default: throw new ArgumentException("Unsupported value for " + field);
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0)
            return existing;

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 6; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }

        throw new InvalidOperationException("No free layer for " + layerName);
    }

    // ---------- Synthesized audio ----------

    private const int SampleRate = 44100;

    private static AudioClip MakeClip(string name, float seconds, Func<float, System.Random, float> sample)
    {
        string path = "Assets/Audio/" + name + ".wav";
        int count = Mathf.RoundToInt(seconds * SampleRate);
        System.Random random = new System.Random(name.GetHashCode());

        using (FileStream stream = new FileStream(path, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + count * 2);
            writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(count * 2);

            for (int i = 0; i < count; i++)
            {
                float value = Mathf.Clamp(sample((float)i / SampleRate, random), -1f, 1f);
                writer.Write((short)(value * short.MaxValue));
            }
        }

        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static float noiseState;

    private static float LowNoise(System.Random random, float smoothing)
    {
        float white = (float)(random.NextDouble() * 2.0 - 1.0);
        noiseState += smoothing * (white - noiseState);
        return noiseState;
    }

    private static float ShootSample(float t, System.Random random)
    {
        float boom = Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 14f);
        float crack = LowNoise(random, 0.5f) * Mathf.Exp(-t * 30f);
        return (boom * 0.7f + crack * 1.6f) * 0.8f;
    }

    private static float ImpactSample(float t, System.Random random)
    {
        float thud = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 12f);
        float debris = LowNoise(random, 0.25f) * Mathf.Exp(-t * 16f);
        return (thud * 0.6f + debris * 2.2f) * 0.8f;
    }

    private static float ExplosionSample(float t, System.Random random)
    {
        float rumble = LowNoise(random, 0.06f) * 5f * Mathf.Exp(-t * 2.5f);
        float boom = Mathf.Sin(2f * Mathf.PI * 40f * t) * Mathf.Exp(-t * 5f);
        return (rumble + boom * 0.6f) * 0.8f;
    }

    private static float EngineSample(float t, System.Random random)
    {
        // Цілі частоти за 1 секунду дають безшовний loop.
        float hum = Mathf.Sin(2f * Mathf.PI * 45f * t)
                    + 0.5f * Mathf.Sin(2f * Mathf.PI * 90f * t)
                    + 0.25f * Mathf.Sin(2f * Mathf.PI * 135f * t)
                    + 0.15f * Mathf.Sin(2f * Mathf.PI * 270f * t);
        float pulse = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 12f * t);
        return hum * pulse * 0.35f;
    }
}
