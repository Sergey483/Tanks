using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Збирає проєкт «ТАНКИ» (уроки 71–77): URP, арену, танки, снаряди, ефекти, звуки, NavMesh, світло, постобробку та UI.
public static class TanksSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string OopScenePath = "Assets/OOP_Homework/OOP_Homework.unity";
    private const string SettingsFolder = "Assets/Settings";
    private const string TmpEssentials = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

    private static readonly Vector3 PlayerStart = new Vector3(0f, 0.05f, -20f);

    [MenuItem("Tanks/1. Import TMP Essentials")]
    public static void ImportTmpEssentials()
    {
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
            return;

        AssetDatabase.ImportPackage(TmpEssentials, false);
        AssetDatabase.Refresh();
    }

    [MenuItem("Tanks/2. Setup URP")]
    public static void SetupUrp()
    {
        EnsureFolder(SettingsFolder);

        string rendererPath = SettingsFolder + "/Tanks_Renderer.asset";
        string pipelinePath = SettingsFolder + "/Tanks_URP.asset";

        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if (rendererData == null)
        {
            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, rendererPath);
        }

        if (rendererData.postProcessData == null)
        {
            rendererData.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
        }

        // SSAO (додаткове завдання) та Decal для слідів бою.
        AddRendererFeature(rendererData, "UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion", "SSAO");
        AddRendererFeature(rendererData, typeof(DecalRendererFeature).FullName, "Decal");
        EditorUtility.SetDirty(rendererData);

        UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if (pipeline == null)
        {
            pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipeline, pipelinePath);
        }

        SetSerialized(pipeline, "m_ShadowDistance", 60f);
        SetSerialized(pipeline, "m_ShadowCascadeCount", 2);
        SetSerialized(pipeline, "m_MainLightShadowsSupported", true);
        SetSerialized(pipeline, "m_MainLightShadowmapResolution", 2048);
        SetSerialized(pipeline, "m_SoftShadowsSupported", true);
        SetSerialized(pipeline, "m_UseSRPBatcher", true);
        SetSerialized(pipeline, "m_SupportsHDR", true);
        SetSerialized(pipeline, "m_RequireDepthTexture", true);
        EditorUtility.SetDirty(pipeline);

        GraphicsSettings.defaultRenderPipeline = pipeline;
        int currentLevel = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipeline;
        }
        QualitySettings.SetQualityLevel(currentLevel, false);

        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.companyName = "Sergey483";
        PlayerSettings.productName = "Tanks";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;

        AssetDatabase.SaveAssets();
        Debug.Log("[TanksSceneBuilder] URP configured: " + pipelinePath);
    }

    [MenuItem("Tanks/3. Rebuild Scene")]
    public static void Build()
    {
        int groundLayer = EnsureLayer("Ground");
        int tankLayer = EnsureLayer("Tank");
        ConfigureNavMeshAgent(1.3f, 1.6f);

        foreach (string folder in new[]
        {
            "Assets/Art", "Assets/Art/Materials", "Assets/Art/Models", "Assets/Art/Textures",
            "Assets/Audio", "Assets/Prefabs", "Assets/Prefabs/Projectiles", "Assets/Prefabs/VFX",
            "Assets/Prefabs/Tanks", "Assets/Prefabs/Pickups", "Assets/Prefabs/Environment", "Assets/Scenes",
            "Assets/ScriptableObjects", "Assets/ScriptableObjects/Tanks", "Assets/VFX", SettingsFolder
        })
        {
            EnsureFolder(folder);
        }

        // Звуки: прості синтезовані заглушки, щоб усе працювало без сторонніх файлів.
        AudioClip shootClip = MakeClip("Shoot", 0.35f, ShootSample);
        AudioClip impactClip = MakeClip("Impact", 0.4f, ImpactSample);
        AudioClip explosionClip = MakeClip("Explosion", 1.6f, ExplosionSample);
        AudioClip engineClip = MakeClip("EngineLoop", 1f, EngineSample);

        // Матеріали URP/Lit: оточення менш контрастне, танки — помітні.
        Texture2D groundTexture = MakeGroundTexture();
        Material groundMat = MakeMaterial("Ground", new Color(0.52f, 0.5f, 0.42f), 0.15f, 0f, groundTexture, new Vector2(12f, 12f));
        Material wallMat = MakeMaterial("Wall", new Color(0.58f, 0.56f, 0.52f), 0.2f);
        Material concreteMat = MakeMaterial("Concrete", new Color(0.66f, 0.65f, 0.62f), 0.25f);
        Material roofMat = MakeMaterial("Roof", new Color(0.36f, 0.38f, 0.4f), 0.4f, 0.3f);
        Material containerBlueMat = MakeMaterial("ContainerBlue", new Color(0.2f, 0.33f, 0.48f), 0.35f, 0.4f);
        Material containerRustMat = MakeMaterial("ContainerRust", new Color(0.5f, 0.27f, 0.17f), 0.3f, 0.4f);
        Material sandbagMat = MakeMaterial("Sandbag", new Color(0.62f, 0.55f, 0.4f), 0.05f);
        Material barrelMat = MakeMaterial("Barrel", new Color(0.65f, 0.2f, 0.12f), 0.45f, 0.5f);
        Material metalMat = MakeMaterial("Metal", new Color(0.3f, 0.31f, 0.32f), 0.55f, 0.8f);
        Material beaconMat = MakeMaterial("Beacon", new Color(1f, 0.55f, 0.15f), 0.6f);
        beaconMat.EnableKeyword("_EMISSION");
        beaconMat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f) * 4f);
        beaconMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        Material playerMat = MakeMaterial("PlayerBody", new Color(0.36f, 0.5f, 0.2f), 0.35f, 0.3f);
        Material playerTurretMat = MakeMaterial("PlayerTurret", new Color(0.27f, 0.39f, 0.15f), 0.35f, 0.3f);
        Material enemyMat = MakeMaterial("EnemyBody", new Color(0.62f, 0.16f, 0.12f), 0.35f, 0.3f);
        Material enemyTurretMat = MakeMaterial("EnemyTurret", new Color(0.45f, 0.1f, 0.08f), 0.35f, 0.3f);
        Material projectileMat = MakeMaterial("Projectile", new Color(1f, 0.85f, 0.2f), 0.5f);
        projectileMat.EnableKeyword("_EMISSION");
        projectileMat.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.1f) * 3f);
        Material pickupMat = MakeMaterial("Repair", new Color(0.2f, 0.8f, 0.9f), 0.6f);
        pickupMat.EnableKeyword("_EMISSION");
        pickupMat.SetColor("_EmissionColor", new Color(0.1f, 0.6f, 0.8f) * 1.5f);

        Material particleMat = MakeParticleMaterial("ParticleAdditive");
        Material trailMat = MakeParticleMaterial("ProjectileTrail");
        Material scorchMat = MakeDecalMaterial();

        // VFX префаби.
        ImpactEffect impactPrefab = MakeEffectPrefab(
            "ImpactEffect", particleMat, impactClip, 2f,
            new Color(1f, 0.75f, 0.3f), 12, 25, 3f, 6f, 0.15f, 0.4f, 0.3f);

        ImpactEffect explosionPrefab = MakeEffectPrefab(
            "TankExplosion", particleMat, explosionClip, 3f,
            new Color(1f, 0.45f, 0.1f), 40, 60, 4f, 10f, 0.6f, 1.6f, 0.8f);

        Projectile projectilePrefab = MakeProjectilePrefab(projectileMat, trailMat, impactPrefab);

        TankConfig playerConfig = MakeConfig("PlayerTankConfig", 25, 18f, 0.7f, 5f, projectilePrefab);
        MakeConfig("RapidTankConfig", 10, 24f, 0.25f, 4f, projectilePrefab);
        TankConfig enemyConfig = MakeConfig("EnemyTankConfig", 10, 16f, 1.2f, 5f, projectilePrefab);

        GameObject playerPrefab = MakeTankPrefab(
            "PlayerTank", false, playerMat, playerTurretMat, particleMat, shootClip, engineClip,
            playerConfig, explosionPrefab.gameObject, tankLayer);

        GameObject enemyPrefab = MakeTankPrefab(
            "EnemyTank", true, enemyMat, enemyTurretMat, particleMat, shootClip, null,
            enemyConfig, explosionPrefab.gameObject, tankLayer);

        GameObject pickupPrefab = MakeRepairPickupPrefab(pickupMat);

        // Префаби оточення (заміна Greybox-кубів).
        GameObject containerBlue = MakeContainerPrefab("Container_Blue", containerBlueMat, metalMat);
        GameObject containerRust = MakeContainerPrefab("Container_Rust", containerRustMat, metalMat);
        GameObject sandbags = MakeSandbagPrefab(sandbagMat);
        GameObject barrels = MakeBarrelsPrefab(barrelMat, metalMat);
        GameObject concreteBlock = MakeConcreteBlockPrefab(concreteMat);
        GameObject cornerCover = MakeCornerCoverPrefab(concreteMat);

        // ---------- Сцена ----------
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Transform level = new GameObject("Level").transform;
        Transform groundGroup = Group(level, "Ground");
        Transform blockout = Group(level, "Blockout");
        Transform environment = Group(level, "Environment");
        Transform navigationGroup = Group(level, "Navigation");
        Transform patrolGroup = Group(level, "PatrolRoutes");
        Transform spawnGroup = Group(level, "SpawnPoints");
        Transform lightingGroup = Group(level, "Lighting");

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(groundGroup, false);
        ground.transform.localScale = new Vector3(5f, 1f, 5f);
        ground.layer = groundLayer;
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;
        MakeStatic(ground);

        // Межі арени 50 × 50.
        MakeBlock(blockout, "Border_North", new Vector3(0f, 2f, 25.5f), new Vector3(52f, 4f, 1f), wallMat);
        MakeBlock(blockout, "Border_South", new Vector3(0f, 2f, -25.5f), new Vector3(52f, 4f, 1f), wallMat);
        MakeBlock(blockout, "Border_East", new Vector3(25.5f, 2f, 0f), new Vector3(1f, 4f, 52f), wallMat);
        MakeBlock(blockout, "Border_West", new Vector3(-25.5f, 2f, 0f), new Vector3(1f, 4f, 52f), wallMat);

        // Повні укриття: стіни між центром і бічними маршрутами, з проходом посередині.
        MakeBlock(blockout, "Divider_West_South", new Vector3(-8f, 1.5f, -9f), new Vector3(1.2f, 3f, 10f), concreteMat);
        MakeBlock(blockout, "Divider_West_North", new Vector3(-8f, 1.5f, 9f), new Vector3(1.2f, 3f, 10f), concreteMat);
        MakeBlock(blockout, "Divider_East_South", new Vector3(8f, 1.5f, -9f), new Vector3(1.2f, 3f, 10f), concreteMat);
        MakeBlock(blockout, "Divider_East_North", new Vector3(8f, 1.5f, 9f), new Vector3(1.2f, 3f, 10f), concreteMat);

        // Ангари у кутах півночі.
        MakeHangar(environment, "Hangar_West", new Vector3(-17f, 0f, 19f), concreteMat, roofMat);
        MakeHangar(environment, "Hangar_East", new Vector3(17f, 0f, 19f), concreteMat, roofMat);

        // Головний орієнтир — радіовежа в центрі з теплим маячком.
        MakeLandmark(environment, new Vector3(0f, 0f, 2f), concreteMat, metalMat, beaconMat);

        // Контейнери в бічних маршрутах (повні укриття).
        PlaceProp(containerBlue, environment, new Vector3(-17f, 0f, -12f), 0f);
        PlaceProp(containerRust, environment, new Vector3(-14f, 0f, 2f), 90f);
        PlaceProp(containerRust, environment, new Vector3(17f, 0f, -6f), 0f);
        PlaceProp(containerBlue, environment, new Vector3(14f, 0f, 7f), 90f);

        // Кутові укриття.
        PlaceProp(cornerCover, environment, new Vector3(-20f, 0f, -20f), 0f);
        PlaceProp(cornerCover, environment, new Vector3(20f, 0f, -20f), 90f);
        PlaceProp(cornerCover, environment, new Vector3(4f, 0f, 14f), 180f);

        // Часткові укриття: мішки з піском.
        PlaceProp(sandbags, environment, new Vector3(-4f, 0f, -14f), 0f);
        PlaceProp(sandbags, environment, new Vector3(4f, 0f, -14f), 0f);
        PlaceProp(sandbags, environment, new Vector3(-3.5f, 0f, 9f), 0f);
        PlaceProp(sandbags, environment, new Vector3(-18f, 0f, 10f), 90f);
        PlaceProp(sandbags, environment, new Vector3(19f, 0f, 1f), 90f);

        // Бетонні блоки біля центру.
        PlaceProp(concreteBlock, environment, new Vector3(-4.5f, 0f, -5f), 15f);
        PlaceProp(concreteBlock, environment, new Vector3(4.5f, 0f, -4f), -10f);

        // М'які перешкоди: бочки (змінюють маршрут, але снаряди пролітають над ними).
        PlaceProp(barrels, environment, new Vector3(-12f, 0f, -18f), 0f);
        PlaceProp(barrels, environment, new Vector3(21f, 0f, 12f), 40f);
        PlaceProp(barrels, environment, new Vector3(-6f, 0f, 19f), 0f);

        // NavMesh після фінальної геометрії (танки виключені маскою шарів).
        NavMeshSurface surface = navigationGroup.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~(1 << tankLayer);
        surface.BuildNavMesh();
        string navMeshPath = "Assets/Scenes/Game_NavMesh.asset";
        AssetDatabase.DeleteAsset(navMeshPath);
        AssetDatabase.CreateAsset(surface.navMeshData, navMeshPath);

        // Точки появи та маршрути патрулів.
        Transform playerSpawn = MakeMarker(spawnGroup, "PlayerSpawn", PlayerStart);
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.SetPositionAndRotation(playerSpawn.position, Quaternion.identity);

        Transform routeWest = MakePatrolRoute(patrolGroup, "PatrolRoute_West", new[]
        {
            new Vector3(-12f, 0f, 14f), new Vector3(-20f, 0f, 7f), new Vector3(-20f, 0f, -8f), new Vector3(-12f, 0f, -6f)
        });
        Transform routeEast = MakePatrolRoute(patrolGroup, "PatrolRoute_East", new[]
        {
            new Vector3(12f, 0f, 14f), new Vector3(21f, 0f, 4f), new Vector3(21f, 0f, -12f), new Vector3(12f, 0f, -2f)
        });
        Transform routeCenter = MakePatrolRoute(patrolGroup, "PatrolRoute_Center", new[]
        {
            new Vector3(-4f, 0f, 20f), new Vector3(4f, 0f, 20f), new Vector3(4f, 0f, 9f), new Vector3(-1f, 0f, 9f)
        });

        MakeEnemy(enemyPrefab, "EnemyTank_West", MakeMarker(spawnGroup, "EnemySpawn_West", new Vector3(-12f, 0f, 14f)).position, player.transform, routeWest);
        MakeEnemy(enemyPrefab, "EnemyTank_East", MakeMarker(spawnGroup, "EnemySpawn_East", new Vector3(12f, 0f, 14f)).position, player.transform, routeEast);
        MakeEnemy(enemyPrefab, "EnemyTank_Center", MakeMarker(spawnGroup, "EnemySpawn_Center", new Vector3(0f, 0f, 20f)).position, player.transform, routeCenter);

        Transform pickups = Group(level, "Pickups");
        foreach (Vector3 position in new[] { new Vector3(-21f, 0.8f, -17f), new Vector3(21f, 0.8f, 18f), new Vector3(0f, 0.8f, -6f) })
        {
            GameObject pickup = (GameObject)PrefabUtility.InstantiatePrefab(pickupPrefab, pickups);
            pickup.transform.position = position;
        }

        // Декалі — сліди бою.
        Transform decals = Group(environment, "BattleDecals");
        MakeScorch(decals, scorchMat, new Vector3(-2f, 0f, -9f), 3.5f, 20f);
        MakeScorch(decals, scorchMat, new Vector3(6f, 0f, 3f), 2.5f, 110f);
        MakeScorch(decals, scorchMat, new Vector3(-15f, 0f, -5f), 3f, 200f);
        MakeScorch(decals, scorchMat, new Vector3(15f, 0f, 12f), 4f, 300f);
        MakeScorch(decals, scorchMat, new Vector3(1f, 0f, 16f), 2.8f, 45f);

        // ---------- Світло та атмосфера ----------
        Light sun = UnityEngine.Object.FindFirstObjectByType<Light>();
        sun.name = "Sun";
        sun.transform.SetParent(lightingGroup, true);
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.8f;
        sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        sun.color = new Color(1f, 0.96f, 0.88f);
        sun.intensity = 1.3f;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.68f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.68f, 0.74f, 0.8f);
        RenderSettings.fogDensity = 0.006f;

        VolumeProfile dayProfile = MakeVolumeProfile("DayVolumeProfile", false);
        VolumeProfile sunsetProfile = MakeVolumeProfile("SunsetVolumeProfile", true);

        GameObject volumeObject = new GameObject("Global Volume");
        volumeObject.transform.SetParent(lightingGroup, false);
        Volume globalVolume = volumeObject.AddComponent<Volume>();
        globalVolume.isGlobal = true;
        globalVolume.priority = 0f;
        globalVolume.sharedProfile = dayProfile;

        AtmosphereSwitcher switcher = lightingGroup.gameObject.AddComponent<AtmosphereSwitcher>();
        SetField(switcher, "sun", sun);
        SetField(switcher, "globalVolume", globalVolume);
        SerializedObject switcherObject = new SerializedObject(switcher);
        SerializedProperty atmospheres = switcherObject.FindProperty("atmospheres");
        atmospheres.arraySize = 2;
        FillAtmosphere(atmospheres.GetArrayElementAtIndex(0), "Day", new Vector3(50f, -35f, 0f), new Color(1f, 0.96f, 0.88f), 1.3f,
            new Color(0.55f, 0.6f, 0.68f), new Color(0.68f, 0.74f, 0.8f), 0.006f, dayProfile);
        FillAtmosphere(atmospheres.GetArrayElementAtIndex(1), "Sunset", new Vector3(16f, -65f, 0f), new Color(1f, 0.74f, 0.52f), 1.15f,
            new Color(0.42f, 0.44f, 0.55f), new Color(0.72f, 0.58f, 0.5f), 0.007f, sunsetProfile);
        switcherObject.ApplyModifiedPropertiesWithoutUndo();

        Camera mainCamera = Camera.main;
        mainCamera.farClipPlane = 200f;
        UniversalAdditionalCameraData cameraData = mainCamera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        CameraFollow follow = mainCamera.gameObject.AddComponent<CameraFollow>();
        SetField(follow, "target", player.transform);
        mainCamera.transform.position = player.transform.position + new Vector3(0f, 12f, -8f);
        mainCamera.transform.LookAt(player.transform);

        MakeHud(player.GetComponent<TankHealth>());

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();

        CaptureScreenshots(mainCamera, globalVolume, cameraData, switcher);

        Debug.Log("[TanksSceneBuilder] Scene built: " + ScenePath);
    }

    [MenuItem("Tanks/4. Build OOP Homework Scene")]
    public static void BuildOopScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Material groundMat = MakeMaterial("OOP_Ground", new Color(0.45f, 0.55f, 0.45f), 0.1f);
        Material playerMat = MakeMaterial("OOP_Player", new Color(0.2f, 0.45f, 0.85f), 0.4f);
        Material orcMat = MakeMaterial("OOP_Orc", new Color(0.25f, 0.6f, 0.2f), 0.3f);
        Material trollMat = MakeMaterial("OOP_Troll", new Color(0.5f, 0.5f, 0.55f), 0.3f);
        Material warriorMat = MakeMaterial("OOP_Warrior", new Color(0.85f, 0.7f, 0.2f), 0.5f);
        Material goldMat = MakeMaterial("OOP_Gold", new Color(1f, 0.8f, 0.1f), 0.8f, 0.9f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1f, -6f);
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        player.AddComponent<OopPlayerMover>();

        // Рівень 1: Батько і син.
        GameObject warrior = GameObject.CreatePrimitive(PrimitiveType.Cube);
        warrior.name = "Warrior";
        warrior.transform.position = new Vector3(-6f, 0.75f, -6f);
        warrior.transform.localScale = new Vector3(1f, 1.5f, 1f);
        warrior.GetComponent<Renderer>().sharedMaterial = warriorMat;
        Warrior warriorScript = warrior.AddComponent<Warrior>();
        warriorScript.characterName = "Лицар Артур";

        // Рівень 2: BankBox.
        GameObject bank = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bank.name = "BankBox";
        bank.transform.position = new Vector3(6f, 0.5f, -6f);
        bank.GetComponent<Renderer>().sharedMaterial = goldMat;
        BankBox bankBox = bank.AddComponent<BankBox>();
        BankBoxTest bankTest = bank.AddComponent<BankBoxTest>();
        SetField(bankTest, "bankBox", bankBox);

        // Рівень 3: Enemy → Orc і Troll.
        GameObject orc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        orc.name = "Orc";
        orc.transform.position = new Vector3(-8f, 1f, 10f);
        orc.GetComponent<Renderer>().sharedMaterial = orcMat;
        Orc orcScript = orc.AddComponent<Orc>();

        GameObject troll = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        troll.name = "Troll";
        troll.transform.position = new Vector3(8f, 1.5f, 12f);
        troll.transform.localScale = new Vector3(1.6f, 1.5f, 1.6f);
        troll.GetComponent<Renderer>().sharedMaterial = trollMat;
        Troll trollScript = troll.AddComponent<Troll>();
        SetField(trollScript, "moveSpeed", 1.2f);
        SetField(trollScript, "stopDistance", 2.5f);

        GameObject tester = new GameObject("EnemyTest");
        EnemyTest enemyTest = tester.AddComponent<EnemyTest>();
        SetObjectArray(enemyTest, "enemies", new UnityEngine.Object[] { orcScript, trollScript });

        OopDebugOverlay overlay = tester.AddComponent<OopDebugOverlay>();
        SetObjectArray(overlay, "enemies", new UnityEngine.Object[] { orcScript, trollScript });
        SetField(overlay, "bankBox", bankBox);

        Camera camera = Camera.main;
        camera.transform.position = new Vector3(0f, 16f, -18f);
        camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

        EditorSceneManager.SaveScene(scene, OopScenePath);
        Debug.Log("[TanksSceneBuilder] OOP scene built: " + OopScenePath);
    }

    [MenuItem("Tanks/5. Build Windows Player")]
    public static void BuildWindows()
    {
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Windows/Tanks.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("[TanksSceneBuilder] Build result: " + report.summary.result + ", size " + report.summary.totalSize);

        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
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
            agent.radius = 1.3f;
            agent.height = 1.6f;

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
        // Один фізичний Collider на корені об'єкта.
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

        SetObjectArray(ai, "patrolPoints", points);
    }

    private static Transform MakePatrolRoute(Transform parent, string name, Vector3[] points)
    {
        Transform route = Group(parent, name);
        for (int i = 0; i < points.Length; i++)
            MakeMarker(route, "Point_" + (i + 1).ToString("00"), points[i]);
        return route;
    }

    private static Transform MakeMarker(Transform parent, string name, Vector3 position)
    {
        Transform marker = new GameObject(name).transform;
        marker.SetParent(parent, false);
        marker.position = position;
        return marker;
    }

    // ---------- Level ----------

    private static Transform Group(Transform parent, string name)
    {
        Transform group = new GameObject(name).transform;
        group.SetParent(parent, false);
        return group;
    }

    private static void MakeBlock(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.position = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = material;
        MakeStatic(block);
    }

    private static GameObject PlaceProp(GameObject prefab, Transform parent, Vector3 position, float yaw)
    {
        GameObject prop = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        prop.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        return prop;
    }

    private static GameObject PropRoot(string name, Vector3 colliderCenter, Vector3 colliderSize)
    {
        GameObject root = new GameObject(name);
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = colliderCenter;
        collider.size = colliderSize;
        return root;
    }

    private static GameObject MakeContainerPrefab(string name, Material bodyMat, Material frameMat)
    {
        GameObject root = PropRoot(name, new Vector3(0f, 1.3f, 0f), new Vector3(2.5f, 2.6f, 6f));
        MakePart(PrimitiveType.Cube, "Body", root.transform, new Vector3(0f, 1.3f, 0f), new Vector3(2.4f, 2.5f, 5.9f), bodyMat);
        for (int i = 0; i < 7; i++)
        {
            float z = -2.7f + i * 0.9f;
            MakePart(PrimitiveType.Cube, "Rib_L_" + i, root.transform, new Vector3(-1.23f, 1.3f, z), new Vector3(0.08f, 2.4f, 0.18f), bodyMat);
            MakePart(PrimitiveType.Cube, "Rib_R_" + i, root.transform, new Vector3(1.23f, 1.3f, z), new Vector3(0.08f, 2.4f, 0.18f), bodyMat);
        }
        MakePart(PrimitiveType.Cube, "Frame_Top", root.transform, new Vector3(0f, 2.58f, 0f), new Vector3(2.5f, 0.1f, 6f), frameMat);
        MakePart(PrimitiveType.Cube, "Frame_Bottom", root.transform, new Vector3(0f, 0.05f, 0f), new Vector3(2.5f, 0.1f, 6f), frameMat);
        MakeStatic(root);
        return SavePrefab(root, "Assets/Prefabs/Environment/" + name + ".prefab");
    }

    private static GameObject MakeSandbagPrefab(Material material)
    {
        GameObject root = PropRoot("Sandbags", new Vector3(0f, 0.65f, 0f), new Vector3(4f, 1.3f, 1.1f));
        for (int row = 0; row < 3; row++)
        {
            int count = 5 - row;
            for (int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) * 0.5f) * 0.8f;
                GameObject bag = MakePart(PrimitiveType.Capsule, $"Bag_{row}_{i}", root.transform,
                    new Vector3(x, 0.22f + row * 0.4f, 0f), new Vector3(0.45f, 0.42f, 0.9f), material);
                bag.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }
        MakeStatic(root);
        return SavePrefab(root, "Assets/Prefabs/Environment/Sandbags.prefab");
    }

    private static GameObject MakeBarrelsPrefab(Material barrelMat, Material rimMat)
    {
        GameObject root = PropRoot("Barrels", new Vector3(0f, 0.45f, 0f), new Vector3(2.2f, 0.9f, 2.2f));
        Vector3[] spots = { new Vector3(-0.5f, 0f, -0.4f), new Vector3(0.55f, 0f, -0.3f), new Vector3(0f, 0f, 0.55f) };
        for (int i = 0; i < spots.Length; i++)
        {
            GameObject barrel = MakePart(PrimitiveType.Cylinder, "Barrel_" + i, root.transform, spots[i] + Vector3.up * 0.45f, new Vector3(0.75f, 0.45f, 0.75f), barrelMat);
            barrel.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            MakePart(PrimitiveType.Cylinder, "Rim_" + i, root.transform, spots[i] + Vector3.up * 0.88f, new Vector3(0.78f, 0.02f, 0.78f), rimMat)
                .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
        MakeStatic(root);
        return SavePrefab(root, "Assets/Prefabs/Environment/Barrels.prefab");
    }

    private static GameObject MakeConcreteBlockPrefab(Material material)
    {
        GameObject root = PropRoot("ConcreteBlock", new Vector3(0f, 1f, 0f), new Vector3(3f, 2f, 1.5f));
        MakePart(PrimitiveType.Cube, "Block", root.transform, new Vector3(0f, 0.9f, 0f), new Vector3(3f, 1.8f, 1.5f), material);
        MakePart(PrimitiveType.Cube, "Cap", root.transform, new Vector3(0f, 1.9f, 0f), new Vector3(2.6f, 0.2f, 1.2f), material);
        MakeStatic(root);
        return SavePrefab(root, "Assets/Prefabs/Environment/ConcreteBlock.prefab");
    }

    private static GameObject MakeCornerCoverPrefab(Material material)
    {
        GameObject root = new GameObject("CornerCover");
        AddBoxPart(root.transform, "Wall_A", new Vector3(0f, 1.25f, 1.75f), new Vector3(1f, 2.5f, 4.5f), material);
        AddBoxPart(root.transform, "Wall_B", new Vector3(1.75f, 1.25f, 0f), new Vector3(4.5f, 2.5f, 1f), material);
        MakeStatic(root);
        return SavePrefab(root, "Assets/Prefabs/Environment/CornerCover.prefab");
    }

    private static void AddBoxPart(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void MakeHangar(Transform parent, string name, Vector3 position, Material wallMat, Material roofMat)
    {
        GameObject hangar = new GameObject(name);
        hangar.transform.SetParent(parent, false);
        hangar.transform.position = position;
        AddBoxPart(hangar.transform, "Walls", new Vector3(0f, 2.5f, 0f), new Vector3(10f, 5f, 8f), wallMat);
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Roof";
        roof.transform.SetParent(hangar.transform, false);
        roof.transform.localPosition = new Vector3(0f, 5.2f, 0f);
        roof.transform.localScale = new Vector3(10.6f, 0.4f, 8.6f);
        roof.GetComponent<Renderer>().sharedMaterial = roofMat;
        UnityEngine.Object.DestroyImmediate(roof.GetComponent<Collider>());
        MakeStatic(hangar);
    }

    private static void MakeLandmark(Transform parent, Vector3 position, Material baseMat, Material metalMat, Material beaconMat)
    {
        GameObject tower = new GameObject("Landmark_RadioTower");
        tower.transform.SetParent(parent, false);
        tower.transform.position = position;

        BoxCollider collider = tower.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1f, 0f);
        collider.size = new Vector3(3.2f, 2f, 3.2f);

        MakePart(PrimitiveType.Cube, "Base", tower.transform, new Vector3(0f, 1f, 0f), new Vector3(3.2f, 2f, 3.2f), baseMat);
        MakePart(PrimitiveType.Cylinder, "Mast", tower.transform, new Vector3(0f, 6f, 0f), new Vector3(0.5f, 4f, 0.5f), metalMat);
        MakePart(PrimitiveType.Cube, "Platform", tower.transform, new Vector3(0f, 8f, 0f), new Vector3(1.8f, 0.15f, 1.8f), metalMat);
        MakePart(PrimitiveType.Sphere, "Beacon", tower.transform, new Vector3(0f, 10.3f, 0f), Vector3.one * 0.8f, beaconMat)
            .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        GameObject lightObject = new GameObject("BeaconLight");
        lightObject.transform.SetParent(tower.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 9.5f, 0f);
        Light beaconLight = lightObject.AddComponent<Light>();
        beaconLight.type = LightType.Point;
        beaconLight.color = new Color(1f, 0.55f, 0.2f);
        beaconLight.range = 14f;
        beaconLight.intensity = 3f;
        beaconLight.shadows = LightShadows.None;

        MakeStatic(tower);
    }

    private static void MakeScorch(Transform parent, Material material, Vector3 position, float size, float yaw)
    {
        GameObject decal = new GameObject("Scorch");
        decal.transform.SetParent(parent, false);
        decal.transform.position = position + Vector3.up * 0.5f;
        decal.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
        DecalProjector projector = decal.AddComponent<DecalProjector>();
        projector.material = material;
        projector.size = new Vector3(size, size, 1.5f);
        projector.fadeFactor = 0.85f;
    }

    private static void MakeStatic(GameObject target)
    {
        StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI
            | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.ReflectionProbeStatic;

        foreach (Transform child in target.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(child.gameObject, flags);
    }

    private static void ConfigureNavMeshAgent(float radius, float height)
    {
        SerializedObject navMeshSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset")[0]);
        SerializedProperty settings = navMeshSettings.FindProperty("m_Settings");
        if (settings == null || settings.arraySize == 0)
            return;

        SerializedProperty agent = settings.GetArrayElementAtIndex(0);
        agent.FindPropertyRelative("agentRadius").floatValue = radius;
        agent.FindPropertyRelative("agentHeight").floatValue = height;
        agent.FindPropertyRelative("agentClimb").floatValue = 0.3f;
        navMeshSettings.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Post-processing ----------

    private static VolumeProfile MakeVolumeProfile(string name, bool isSunset)
    {
        string path = SettingsFolder + "/" + name + ".asset";
        AssetDatabase.DeleteAsset(path);

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        Tonemapping tonemapping = AddOverride<Tonemapping>(profile);
        tonemapping.mode.Override(TonemappingMode.ACES);

        ColorAdjustments color = AddOverride<ColorAdjustments>(profile);
        color.postExposure.Override(isSunset ? 0.1f : 0f);
        color.contrast.Override(isSunset ? 12f : 10f);
        color.saturation.Override(isSunset ? 0f : 0f);
        color.colorFilter.Override(isSunset ? new Color(1f, 0.97f, 0.93f) : new Color(1f, 0.99f, 0.96f));

        Bloom bloom = AddOverride<Bloom>(profile);
        bloom.intensity.Override(isSunset ? 0.4f : 0.3f);
        bloom.threshold.Override(1.1f);
        bloom.scatter.Override(0.6f);

        Vignette vignette = AddOverride<Vignette>(profile);
        vignette.intensity.Override(isSunset ? 0.2f : 0.15f);
        vignette.smoothness.Override(0.4f);

        WhiteBalance whiteBalance = AddOverride<WhiteBalance>(profile);
        whiteBalance.temperature.Override(isSunset ? 10f : 3f);
        whiteBalance.tint.Override(0f);

        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T component = profile.Add<T>(true);
        component.name = typeof(T).Name;
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    private static void FillAtmosphere(SerializedProperty atmosphere, string name, Vector3 rotation, Color sunColor, float intensity,
        Color ambient, Color fog, float fogDensity, VolumeProfile profile)
    {
        atmosphere.FindPropertyRelative("name").stringValue = name;
        atmosphere.FindPropertyRelative("sunRotation").vector3Value = rotation;
        atmosphere.FindPropertyRelative("sunColor").colorValue = sunColor;
        atmosphere.FindPropertyRelative("sunIntensity").floatValue = intensity;
        atmosphere.FindPropertyRelative("ambientColor").colorValue = ambient;
        atmosphere.FindPropertyRelative("fogColor").colorValue = fog;
        atmosphere.FindPropertyRelative("fogDensity").floatValue = fogDensity;
        atmosphere.FindPropertyRelative("volumeProfile").objectReferenceValue = profile;
    }

    private static void AddRendererFeature(ScriptableRendererData rendererData, string typeName, string featureName)
    {
        foreach (ScriptableRendererFeature existing in rendererData.rendererFeatures)
        {
            if (existing != null && existing.GetType().FullName == typeName)
                return;
        }

        Type type = typeof(UniversalRendererData).Assembly.GetType(typeName);
        if (type == null)
        {
            Debug.LogWarning("[TanksSceneBuilder] Renderer feature not found: " + typeName);
            return;
        }

        ScriptableRendererFeature feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(type);
        feature.name = featureName;
        AssetDatabase.AddObjectToAsset(feature, rendererData);
        rendererData.rendererFeatures.Add(feature);

        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
        SerializedObject serialized = new SerializedObject(rendererData);
        SerializedProperty map = serialized.FindProperty("m_RendererFeatureMap");
        if (map != null)
        {
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CaptureScreenshots(Camera camera, Volume volume, UniversalAdditionalCameraData cameraData, AtmosphereSwitcher switcher)
    {
        try
        {
            Directory.CreateDirectory("Docs/Screenshots");

            Vector3 originalPosition = camera.transform.position;
            Quaternion originalRotation = camera.transform.rotation;
            camera.transform.position = new Vector3(0f, 26f, -36f);
            camera.transform.LookAt(new Vector3(0f, 0f, 2f));

            volume.enabled = false;
            cameraData.renderPostProcessing = false;
            // Перший кадр у batchmode рендериться без даних матеріалів, тому його пропускаємо.
            RenderToFile(camera, "Docs/Screenshots/1_before_postprocessing.png");
            RenderToFile(camera, "Docs/Screenshots/1_before_postprocessing.png");

            volume.enabled = true;
            cameraData.renderPostProcessing = true;
            RenderToFile(camera, "Docs/Screenshots/2_after_postprocessing_day.png");

            switcher.Apply(1);
            RenderToFile(camera, "Docs/Screenshots/3_after_postprocessing_sunset.png");
            switcher.Apply(0);

            camera.transform.SetPositionAndRotation(originalPosition, originalRotation);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[TanksSceneBuilder] Screenshots skipped: " + exception.Message);
        }
    }

    private static void RenderToFile(Camera camera, string path)
    {
        RenderTexture target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
        RenderTexture previous = camera.targetTexture;
        camera.targetTexture = target;
        camera.Render();
        camera.targetTexture = previous;

        RenderTexture.active = target;
        Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        image.Apply();
        RenderTexture.active = null;

        File.WriteAllBytes(path, image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
    }

    // ---------- Projectile & VFX ----------

    private static Projectile MakeProjectilePrefab(Material material, Material trailMat, ImpactEffect impactPrefab)
    {
        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = "Projectile";
        shell.transform.localScale = Vector3.one * 0.3f;
        shell.GetComponent<Renderer>().sharedMaterial = material;
        shell.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

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
        trail.shadowCastingMode = ShadowCastingMode.Off;
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

        ApplyFade(particles);
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
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)8, (short)15) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.05f;

        ApplyFade(particles);
        flash.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMat;
        return particles;
    }

    private static void ApplyFade(ParticleSystem particles)
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

    private static void MakeHud(TankHealth playerHealth)
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // HP гравця.
        GameObject panel = MakePanel(canvasObject.transform, "PlayerHealthPanel", new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(420f, 90f), new Color(0f, 0f, 0f, 0.5f));

        Slider slider = MakeSlider(panel.transform, "HealthSlider", out Image fill);
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.offsetMin = new Vector2(15f, 15f);
        sliderRect.offsetMax = new Vector2(-15f, 45f);

        TextMeshProUGUI healthText = MakeText(panel.transform, "HealthText", "100 / 100", 28f, TextAlignmentOptions.Left);
        RectTransform textRect = healthText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.offsetMin = new Vector2(15f, -45f);
        textRect.offsetMax = new Vector2(-15f, -5f);

        TankHealthBar bar = panel.AddComponent<TankHealthBar>();
        SetField(bar, "health", playerHealth);
        SetField(bar, "slider", slider);
        SetField(bar, "healthText", healthText);
        SetField(bar, "fillImage", fill);

        // Лічильник знищених ворогів.
        GameObject killPanel = MakePanel(canvasObject.transform, "KillPanel", new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(320f, 60f), new Color(0f, 0f, 0f, 0.5f));
        TextMeshProUGUI killText = MakeText(killPanel.transform, "KillText", "Знищено: 0 / 3", 30f, TextAlignmentOptions.Center);
        Stretch(killText.rectTransform);

        // Підказка з керуванням.
        TextMeshProUGUI hint = MakeText(canvasObject.transform, "ControlsHint",
            "Стрілки — рух   Z/X або миша — башта   ЛКМ — постріл   T — день/захід", 22f, TextAlignmentOptions.Right);
        RectTransform hintRect = hint.rectTransform;
        hintRect.anchorMin = hintRect.anchorMax = hintRect.pivot = new Vector2(1f, 0f);
        hintRect.anchoredPosition = new Vector2(-30f, 30f);
        hintRect.sizeDelta = new Vector2(1000f, 40f);
        hint.color = new Color(1f, 1f, 1f, 0.85f);

        // Екрани завершення гри.
        GameObject gameOver = MakeEndPanel(canvasObject.transform, "GameOverPanel", "ГРУ ЗАКІНЧЕНО", new Color(0.85f, 0.2f, 0.15f), out Button gameOverButton);
        GameObject victory = MakeEndPanel(canvasObject.transform, "VictoryPanel", "ПЕРЕМОГА!", new Color(0.3f, 0.85f, 0.35f), out Button victoryButton);

        GameStateController controller = canvasObject.AddComponent<GameStateController>();
        SetField(controller, "playerHealth", playerHealth);
        SetField(controller, "killText", killText);
        SetField(controller, "gameOverPanel", gameOver);
        SetField(controller, "victoryPanel", victory);
        SetObjectArray(controller, "restartButtons", new UnityEngine.Object[] { gameOverButton, victoryButton });

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject MakePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = panel.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return panel;
    }

    private static GameObject MakeEndPanel(Transform parent, string name, string title, Color titleColor, out Button restartButton)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        TextMeshProUGUI titleText = MakeText(panel.transform, "Title", title, 96f, TextAlignmentOptions.Center);
        titleText.color = titleColor;
        titleText.fontStyle = FontStyles.Bold;
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = titleRect.anchorMax = titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 90f);
        titleRect.sizeDelta = new Vector2(1200f, 140f);

        TextMeshProUGUI hint = MakeText(panel.transform, "Hint", "Натисни R або кнопку, щоб зіграти ще раз", 30f, TextAlignmentOptions.Center);
        RectTransform hintRect = hint.rectTransform;
        hintRect.anchorMin = hintRect.anchorMax = hintRect.pivot = new Vector2(0.5f, 0.5f);
        hintRect.anchoredPosition = new Vector2(0f, 0f);
        hintRect.sizeDelta = new Vector2(1200f, 50f);

        GameObject buttonObject = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panel.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -90f);
        buttonRect.sizeDelta = new Vector2(360f, 80f);
        buttonObject.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
        restartButton = buttonObject.GetComponent<Button>();

        TextMeshProUGUI label = MakeText(buttonObject.transform, "Label", "Грати знову", 34f, TextAlignmentOptions.Center);
        label.color = new Color(0.12f, 0.12f, 0.12f);
        Stretch(label.rectTransform);

        panel.SetActive(false);
        return panel;
    }

    private static TextMeshProUGUI MakeText(Transform parent, string name, string value, float size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
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
        Stretch(slider.GetComponent<RectTransform>());

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
        background.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
        background.raycastTarget = false;
        Stretch(background.rectTransform);

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

    private static Material MakeMaterial(string name, Color color, float smoothness, float metallic = 0f, Texture2D baseMap = null, Vector2? tiling = null)
    {
        string path = "Assets/Art/Materials/" + name + ".mat";
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(lit);
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != lit)
        {
            material.shader = lit;
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        material.SetTexture("_BaseMap", baseMap);
        material.SetTextureScale("_BaseMap", tiling ?? Vector2.one);
        material.DisableKeyword("_EMISSION");
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material MakeParticleMaterial(string name)
    {
        string path = "Assets/Art/Materials/" + name + ".mat";
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetTexture("_BaseMap", AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd"));
        material.SetColor("_BaseColor", Color.white);

        // Прозорий адитивний режим (як вибухи та спалахи).
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 2f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material MakeDecalMaterial()
    {
        string texturePath = "Assets/Art/Textures/Scorch.png";
        if (!File.Exists(texturePath))
        {
            Texture2D scorch = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            System.Random random = new System.Random(7);
            for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    float dx = (x - 128f) / 128f;
                    float dy = (y - 128f) / 128f;
                    float angle = Mathf.Atan2(dy, dx);
                    float radius = 0.75f + 0.12f * Mathf.Sin(angle * 5f) + 0.06f * Mathf.Sin(angle * 13f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    float alpha = Mathf.Clamp01(1f - distance) * (0.75f + 0.25f * (float)random.NextDouble());
                    float shade = 0.05f + 0.1f * (float)random.NextDouble();
                    scorch.SetPixel(x, y, new Color(shade, shade * 0.9f, shade * 0.8f, Mathf.SmoothStep(0f, 1f, alpha * 1.4f)));
                }
            }
            File.WriteAllBytes(texturePath, scorch.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(scorch);
            AssetDatabase.ImportAsset(texturePath);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        string path = "Assets/Art/Materials/Scorch.mat";
        Shader shader = Shader.Find("Shader Graphs/Decal");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetTexture("Base_Map", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D MakeGroundTexture()
    {
        string path = "Assets/Art/Textures/GroundTile.png";
        if (!File.Exists(path))
        {
            Texture2D tile = new Texture2D(256, 256, TextureFormat.RGB24, true);
            System.Random random = new System.Random(3);
            for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    float noise = 0.9f + 0.1f * (float)random.NextDouble();
                    bool seam = x < 2 || y < 2;
                    float value = seam ? 0.82f : noise;
                    tile.SetPixel(x, y, new Color(value, value, value));
                }
            }
            File.WriteAllBytes(path, tile.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tile);
            AssetDatabase.ImportAsset(path);
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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

    private static void SetObjectArray(UnityEngine.Object target, string field, IList<UnityEngine.Object> values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty array = serialized.FindProperty(field);
        array.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSerialized(UnityEngine.Object target, string field, object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null)
        {
            Debug.LogWarning("[TanksSceneBuilder] " + target.GetType().Name + " has no field " + field);
            return;
        }

        switch (value)
        {
            case bool flag: property.boolValue = flag; break;
            case int number: property.intValue = number; break;
            case float number: property.floatValue = number; break;
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
        if (File.Exists(path))
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);

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
