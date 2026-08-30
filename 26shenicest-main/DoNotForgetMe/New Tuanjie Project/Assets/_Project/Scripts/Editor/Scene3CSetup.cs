#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using DoNotForgetMe.Core;
using DoNotForgetMe.MiniGame.Album;
using DoNotForgetMe.MiniGame.Cooking;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene3CSetup
{
    private const string MENU_PATH = "Tools/3C Setup/Create Basic Scene";
    private const string SCENE_PATH = "Assets/_Project/Scenes/Game.unity";
    private const string INPUT_SETTINGS_PATH = "Assets/_Project/Settings/PlayerInputSettings.asset";
    private const string PLAYER_SETTINGS_PATH = "Assets/_Project/Settings/PlayerSettings.asset";
    private const string MINIGAME_SETTINGS_PATH = "Assets/_Project/Settings/MiniGameSettings.asset";
    private const string TOMATO_EGG_RECIPE_PATH = "Assets/_Project/Settings/TomatoEggRecipe.asset";
    private const string CUCUMBER_SALAD_RECIPE_PATH = "Assets/_Project/Settings/CucumberSaladRecipe.asset";
    private const string BAGUA_CONFIG_PATH = "Assets/_Project/Settings/BaguaStoryConfig.asset";
    private const string ALBUM_CONFIG_PATH = "Assets/_Project/Settings/AlbumConfig.asset";

    private static readonly int GroundLayer = 8;
    private static readonly int PlayerLayer = 9;
    private static readonly int InteractableLayer = 10;

    [MenuItem("Tools/3C Setup/Clear Host Save")]
    public static void ClearHostSave()
    {
        DoNotForgetMe.Save.HostSaveService.Delete();
        Debug.Log("[3C Setup] Host save deleted: " + DoNotForgetMe.Save.HostSaveService.SavePath);
    }

    [MenuItem("Tools/3C Setup/Update Recipe Assets")]
    public static void UpdateRecipeAssets()
    {
        var tomatoEgg = CreateTomatoEggRecipeAsset();
        var cucumberSalad = CreateCucumberSaladRecipeAsset();
        AssetDatabase.SaveAssets();

        // 更新场景中的 Coordinator 和 MiniGameManager
        var coordinator = UnityEngine.Object.FindObjectOfType<DoNotForgetMe.Network.Gameplay.SessionGameplayCoordinator>();
        if (coordinator != null)
        {
            var so = new SerializedObject(coordinator);
            var recipesProp = so.FindProperty("recipes");
            var hasCucumber = false;
            for (var i = 0; i < recipesProp.arraySize; i++)
            {
                if (recipesProp.GetArrayElementAtIndex(i).objectReferenceValue == cucumberSalad)
                {
                    hasCucumber = true;
                    break;
                }
            }
            if (!hasCucumber)
            {
                recipesProp.arraySize = Mathf.Max(recipesProp.arraySize, 2);
                recipesProp.GetArrayElementAtIndex(1).objectReferenceValue = cucumberSalad;
                so.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(coordinator.gameObject.scene);
            }
        }

        var manager = UnityEngine.Object.FindObjectOfType<MiniGameManager>();
        if (manager != null)
        {
            var so = new SerializedObject(manager);
            var recipesProp = so.FindProperty("recipes");
            var hasCucumber = false;
            for (var i = 0; i < recipesProp.arraySize; i++)
            {
                if (recipesProp.GetArrayElementAtIndex(i).objectReferenceValue == cucumberSalad)
                {
                    hasCucumber = true;
                    break;
                }
            }
            if (!hasCucumber)
            {
                recipesProp.arraySize = Mathf.Max(recipesProp.arraySize, 2);
                recipesProp.GetArrayElementAtIndex(1).objectReferenceValue = cucumberSalad;
                so.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }
        }

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[3C Setup] Recipe assets updated: TomatoEgg.nextRecipeId=cucumber_salad, CucumberSaladRecipe.asset created, scene references wired.");
    }

    [MenuItem(MENU_PATH)]
    public static void CreateScene()
    {
        EnsureLayers();

        var inputSettings = CreateInputSettingsAsset();
        var playerSettings = CreatePlayerSettingsAsset();
        var miniGameSettings = CreateMiniGameSettingsAsset();
        var tomatoEggRecipe = CreateTomatoEggRecipeAsset();
        var cucumberSaladRecipe = CreateCucumberSaladRecipeAsset();
        var baguaConfig = CreateBaguaStoryConfigAsset();
        var albumConfig = CreateAlbumConfigAsset();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var groundLayerMask = 1 << GroundLayer;
        var interactableMask = 1 << InteractableLayer;

        // --- 地面（横跨多屏宽度，体现房间式相机效果）---
        CreatePlatform("Ground", new Vector3(0, -3, 0), new Vector3(60, 1, 1), groundLayerMask);
        CreatePlatform("Platform1", new Vector3(8, -1, 0), new Vector3(3, 0.5f, 1), groundLayerMask);
        CreatePlatform("Platform2", new Vector3(-8, 0, 0), new Vector3(3, 0.5f, 1), groundLayerMask);

        // --- 两侧边界墙壁 ---
        CreatePlatform("LeftWall", new Vector3(-31, 0, 0), new Vector3(1, 10, 1), groundLayerMask);
        CreatePlatform("RightWall", new Vector3(31, 0, 0), new Vector3(1, 10, 1), groundLayerMask);

        // --- 玩家 ---
        var player = CreatePlayer(inputSettings, playerSettings, interactableMask);

        // --- 相机（房间式）---
        var camObj = CreateCamera(player.transform);
        CreateEventSystem();

        // --- GameManager ---
        var gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();

        // --- Host 权威的会话流程 ---
        var coordinatorObj = new GameObject("SessionGameplayCoordinator");
        var coordinator = coordinatorObj.AddComponent<DoNotForgetMe.Network.Gameplay.SessionGameplayCoordinator>();
        var coordinatorSo = new SerializedObject(coordinator);
        coordinatorSo.FindProperty("recipes").arraySize = 2;
        coordinatorSo.FindProperty("recipes").GetArrayElementAtIndex(0).objectReferenceValue = tomatoEggRecipe;
        coordinatorSo.FindProperty("recipes").GetArrayElementAtIndex(1).objectReferenceValue = cucumberSaladRecipe;
        coordinatorSo.FindProperty("baguaConfigs").arraySize = 1;
        coordinatorSo.FindProperty("baguaConfigs").GetArrayElementAtIndex(0).objectReferenceValue = baguaConfig;
        coordinatorSo.FindProperty("albumConfigs").arraySize = 1;
        coordinatorSo.FindProperty("albumConfigs").GetArrayElementAtIndex(0).objectReferenceValue = albumConfig;
#if FUSION_PRESENT
        coordinatorObj.AddComponent<Fusion.NetworkObject>();
        coordinatorObj.AddComponent<DoNotForgetMe.Network.Fusion.FusionGameplayBridge>();
#else
        coordinatorSo.FindProperty("debugSingleProcess").boolValue = true;
        coordinatorObj.AddComponent<DoNotForgetMe.Network.Local.LocalGameplayBridge>();
#endif
        coordinatorSo.ApplyModifiedProperties();

        // --- MiniGameManager + 模板 ---
        CreateMiniGameManager(miniGameSettings, tomatoEggRecipe, cucumberSaladRecipe, baguaConfig, albumConfig);

        // --- MiniGameTrigger（做饭触发器，厨房区域，门右侧）---
        CreateMiniGameTrigger(miniGameSettings);

        // --- DoorListeningTrigger（八卦触发器，庭院区域，更远处）---
        CreateDoorListeningTrigger(baguaConfig);

        // --- AlbumTrigger 已被 DeskViewController 替代（DeskViewController 兼具书桌视角 + F触发相册）---

        // --- PlayerSpawn point ---
        var spawnObj = new GameObject("PlayerSpawn");
        spawnObj.transform.position = new Vector3(-3, 0, 0);

        // --- DeskViewController（第一人称书桌视角 + F键交互触发相册小游戏，客厅区域 x=0）---
        var deskObj = new GameObject("DeskViewController");
        deskObj.layer = InteractableLayer;
        deskObj.transform.position = new Vector3(2, -1.5f, 0);
        var deskSr = deskObj.AddComponent<SpriteRenderer>();
        deskSr.sprite = CreateDefaultSprite();
        deskSr.color = new Color(0.45f, 0.32f, 0.2f);
        deskSr.sortingOrder = 5;
        var deskCol = deskObj.AddComponent<BoxCollider2D>();
        deskCol.size = new Vector2(1.2f, 1f);
        deskCol.isTrigger = true;
        deskObj.AddComponent<DoNotForgetMe.Cutscene.DeskViewController>();

        // --- SceneTransitionTrigger（房门 → 厨房 Scene2，x=8）---
        var doorObj = new GameObject("DoorToKitchen");
        doorObj.layer = InteractableLayer;
        doorObj.transform.position = new Vector3(8, -1.5f, 0);
        var doorSr = doorObj.AddComponent<SpriteRenderer>();
        doorSr.sprite = CreateDefaultSprite();
        doorSr.color = new Color(0.4f, 0.5f, 0.35f);
        doorSr.sortingOrder = 5;
        var doorCol = doorObj.AddComponent<BoxCollider2D>();
        doorCol.size = new Vector2(1.2f, 2f);
        doorCol.isTrigger = true;
        var doorTrigger = doorObj.AddComponent<DoNotForgetMe.Interactables.SceneTransitionTrigger>();
        var doorSo = new SerializedObject(doorTrigger);
        doorSo.FindProperty("targetScene").stringValue = SceneNames.Kitchen;
        doorSo.ApplyModifiedProperties();

        // --- Save scene ---
        System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");
        EditorSceneManager.SaveScene(scene, SCENE_PATH);

        AddSceneToBuildSettings(SCENE_PATH);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);

        Debug.Log("[3C Setup] Scene created at " + SCENE_PATH);
        Debug.Log("[3C Setup] Controls: A/D or Left/Right = Move, F = Interact");
        Debug.Log("[3C Setup] Camera: Room-based — walks to screen edge to transition");
        Debug.Log("[3C Setup] MiniGame: Walk to the yellow box and press F to play");
    }

    private static void EnsureLayers()
    {
        var tagManager = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset");
        if (tagManager == null) return;

        var so = new SerializedObject(tagManager);
        var layersProp = so.FindProperty("layers");

        string[] layerNames = { "Ground", "Player", "Interactable" };
        for (int i = 0; i < layerNames.Length; i++)
        {
            int layerIndex = GroundLayer + i;
            if (layerIndex >= layersProp.arraySize) continue;

            var entry = layersProp.GetArrayElementAtIndex(layerIndex);
            if (string.IsNullOrEmpty(entry.stringValue))
            {
                entry.stringValue = layerNames[i];
            }
        }

        so.ApplyModifiedProperties();
    }

    private static InputSettings CreateInputSettingsAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<InputSettings>(INPUT_SETTINGS_PATH);
        if (existing != null) return existing;

        var settings = ScriptableObject.CreateInstance<InputSettings>();
        System.IO.Directory.CreateDirectory("Assets/_Project/Settings");
        AssetDatabase.CreateAsset(settings, INPUT_SETTINGS_PATH);
        AssetDatabase.SaveAssets();
        return settings;
    }

    private static PlayerSettings CreatePlayerSettingsAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<PlayerSettings>(PLAYER_SETTINGS_PATH);
        if (existing != null) return existing;

        var settings = ScriptableObject.CreateInstance<PlayerSettings>();
        System.IO.Directory.CreateDirectory("Assets/_Project/Settings");
        AssetDatabase.CreateAsset(settings, PLAYER_SETTINGS_PATH);
        AssetDatabase.SaveAssets();
        return settings;
    }

    private static MiniGameSettings CreateMiniGameSettingsAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<MiniGameSettings>(MINIGAME_SETTINGS_PATH);
        if (existing != null) return existing;

        var settings = ScriptableObject.CreateInstance<MiniGameSettings>();
        System.IO.Directory.CreateDirectory("Assets/_Project/Settings");
        AssetDatabase.CreateAsset(settings, MINIGAME_SETTINGS_PATH);
        AssetDatabase.SaveAssets();
        return settings;
    }

    private static RecipeConfig CreateTomatoEggRecipeAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<RecipeConfig>(TOMATO_EGG_RECIPE_PATH);
        if (existing != null)
        {
            var so = new SerializedObject(existing);
            if (string.IsNullOrEmpty(so.FindProperty("nextRecipeId").stringValue))
            {
                so.FindProperty("nextRecipeId").stringValue = "cucumber_salad";
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
            return existing;
        }

        var recipe = ScriptableObject.CreateInstance<RecipeConfig>();
        System.IO.Directory.CreateDirectory("Assets/_Project/Settings");
        AssetDatabase.CreateAsset(recipe, TOMATO_EGG_RECIPE_PATH);

        var so2 = new SerializedObject(recipe);
        so2.FindProperty("nextRecipeId").stringValue = "cucumber_salad";
        so2.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
        return recipe;
    }

    private static RecipeConfig CreateCucumberSaladRecipeAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<RecipeConfig>(CUCUMBER_SALAD_RECIPE_PATH);
        if (existing != null) return existing;

        var recipe = ScriptableObject.CreateInstance<RecipeConfig>();
        System.IO.Directory.CreateDirectory("Assets/_Project/Settings");
        AssetDatabase.CreateAsset(recipe, CUCUMBER_SALAD_RECIPE_PATH);

        var so = new SerializedObject(recipe);
        so.FindProperty("recipeId").stringValue = "cucumber_salad";
        so.FindProperty("displayName").stringValue = "凉拌黄瓜";
        so.FindProperty("motherTaskText").stringValue = "请做凉拌黄瓜";
        so.FindProperty("daughterTaskText").stringValue = "查看菜谱改痕，为菜调味";
        so.FindProperty("containerId").stringValue = "bowl";
        so.FindProperty("containerDisplayName").stringValue = "碗";

        var required = so.FindProperty("requiredIngredients");
        required.arraySize = 1;
        required.GetArrayElementAtIndex(0).stringValue = "cucumber";

        var distractors = so.FindProperty("distractorIngredients");
        distractors.arraySize = 3;
        distractors.GetArrayElementAtIndex(0).stringValue = "tomato";
        distractors.GetArrayElementAtIndex(1).stringValue = "egg";
        distractors.GetArrayElementAtIndex(2).stringValue = "ribs";

        so.FindProperty("correctSeasoning").stringValue = "vinegar";

        var forbidden = so.FindProperty("forbiddenSeasonings");
        forbidden.arraySize = 1;
        forbidden.GetArrayElementAtIndex(0).stringValue = "chili";

        var seasonings = so.FindProperty("seasoningOptions");
        seasonings.arraySize = 2;
        seasonings.GetArrayElementAtIndex(0).stringValue = "vinegar";
        seasonings.GetArrayElementAtIndex(1).stringValue = "chili";

        so.FindProperty("recipeNote").stringValue = "洪芳喜欢酸一点多加点醋，但是不能吃辣！";

        var hints = so.FindProperty("hintTexts");
        hints.arraySize = 3;
        hints.GetArrayElementAtIndex(0).stringValue = "找一种绿色的、长条形的蔬菜。";
        hints.GetArrayElementAtIndex(1).stringValue = "把它放进碗里。";
        hints.GetArrayElementAtIndex(2).stringValue = "黄瓜微微发光。";

        var rewards = so.FindProperty("rewardIds");
        rewards.arraySize = 2;
        rewards.GetArrayElementAtIndex(0).stringValue = "photo_hongfang";
        rewards.GetArrayElementAtIndex(1).stringValue = "tag_fourth_sister";

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        return recipe;
    }

    private static DoNotForgetMe.MiniGame.Bagua.BaguaStoryConfig CreateBaguaStoryConfigAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<DoNotForgetMe.MiniGame.Bagua.BaguaStoryConfig>(BAGUA_CONFIG_PATH);
        if (existing != null) return existing;

        var config = ScriptableObject.CreateInstance<DoNotForgetMe.MiniGame.Bagua.BaguaStoryConfig>();
        System.IO.Directory.CreateDirectory("Assets/_Project/Settings");
        AssetDatabase.CreateAsset(config, BAGUA_CONFIG_PATH);

        var so = new SerializedObject(config);

        // 人物配置（不含物品，物品移至 itemPlacements）
        var entriesProp = so.FindProperty("entries");
        entriesProp.arraySize = 3;

        var entry0 = entriesProp.GetArrayElementAtIndex(0);
        entry0.FindPropertyRelative("characterId").stringValue = "liu_hongxiu";
        entry0.FindPropertyRelative("displayName").stringValue = "刘洪秀";
        entry0.FindPropertyRelative("age").stringValue = "18岁";
        entry0.FindPropertyRelative("title").stringValue = "大姐";
        entry0.FindPropertyRelative("subtitle").stringValue = "她总是攥着那把铜钥匙，说这是家里的命根子……";

        var entry1 = entriesProp.GetArrayElementAtIndex(1);
        entry1.FindPropertyRelative("characterId").stringValue = "liu_hongju";
        entry1.FindPropertyRelative("displayName").stringValue = "刘洪菊";
        entry1.FindPropertyRelative("age").stringValue = "13岁";
        entry1.FindPropertyRelative("title").stringValue = "三妹";
        entry1.FindPropertyRelative("subtitle").stringValue = "铁皮糖盒里装的不是糖，是她攒了半年的悄悄话……";

        var entry2 = entriesProp.GetArrayElementAtIndex(2);
        entry2.FindPropertyRelative("characterId").stringValue = "liu_hongbin";
        entry2.FindPropertyRelative("displayName").stringValue = "刘洪斌";
        entry2.FindPropertyRelative("age").stringValue = "8岁";
        entry2.FindPropertyRelative("title").stringValue = "六弟";
        entry2.FindPropertyRelative("subtitle").stringValue = "小木车吱呀吱呀地响，他说那是他童年唯一的车……";

        // 桌面物件配置：3正确 + 5干扰 = 8个
        var itemsProp = so.FindProperty("itemPlacements");
        itemsProp.arraySize = 8;

        // 正确物品
        SetItemPlacement(itemsProp, 0, "key", "铜钥匙", new Vector2(-550, 120), true, "liu_hongxiu");
        SetItemPlacement(itemsProp, 1, "tin_candy_box", "铁皮糖盒", new Vector2(0, 180), true, "liu_hongju");
        SetItemPlacement(itemsProp, 2, "wooden_cart", "小木车", new Vector2(500, 100), true, "liu_hongbin");

        // 干扰物（无 displayName）
        SetItemPlacement(itemsProp, 3, "old_glasses", "", new Vector2(-350, -60), false, null);
        SetItemPlacement(itemsProp, 4, "red_comb", "", new Vector2(-150, 40), false, null);
        SetItemPlacement(itemsProp, 5, "scissors", "", new Vector2(250, -80), false, null);
        SetItemPlacement(itemsProp, 6, "paper_boat", "", new Vector2(400, 200), false, null);
        SetItemPlacement(itemsProp, 7, "abacus", "", new Vector2(-450, 220), false, null);

        // 照片投放区
        var zonesProp = so.FindProperty("photoZones");
        zonesProp.arraySize = 3;

        var zone0 = zonesProp.GetArrayElementAtIndex(0);
        zone0.FindPropertyRelative("zoneId").stringValue = "zone_left";
        zone0.FindPropertyRelative("correctCharacterId").stringValue = "liu_hongxiu";
        zone0.FindPropertyRelative("anchoredPosition").vector2Value = new Vector2(-200, 60);
        zone0.FindPropertyRelative("size").vector2Value = new Vector2(150, 200);

        var zone1 = zonesProp.GetArrayElementAtIndex(1);
        zone1.FindPropertyRelative("zoneId").stringValue = "zone_center";
        zone1.FindPropertyRelative("correctCharacterId").stringValue = "liu_hongju";
        zone1.FindPropertyRelative("anchoredPosition").vector2Value = new Vector2(0, 60);
        zone1.FindPropertyRelative("size").vector2Value = new Vector2(150, 200);

        var zone2 = zonesProp.GetArrayElementAtIndex(2);
        zone2.FindPropertyRelative("zoneId").stringValue = "zone_right";
        zone2.FindPropertyRelative("correctCharacterId").stringValue = "liu_hongbin";
        zone2.FindPropertyRelative("anchoredPosition").vector2Value = new Vector2(200, 60);
        zone2.FindPropertyRelative("size").vector2Value = new Vector2(150, 200);

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        return config;
    }

    private static void SetItemPlacement(SerializedProperty arrayProp, int index, string itemId, string displayName, Vector2 position, bool isCorrect, string characterId)
    {
        var item = arrayProp.GetArrayElementAtIndex(index);
        item.FindPropertyRelative("itemId").stringValue = itemId;
        item.FindPropertyRelative("displayName").stringValue = displayName;
        item.FindPropertyRelative("anchoredPosition").vector2Value = position;
        item.FindPropertyRelative("isCorrect").boolValue = isCorrect;
        item.FindPropertyRelative("characterId").stringValue = characterId ?? string.Empty;
    }

    private static GameObject CreatePlayer(InputSettings inputSettings, PlayerSettings playerSettings, int interactableMask)
    {
        var playerObj = new GameObject("Player");
        playerObj.layer = PlayerLayer;
        playerObj.transform.position = new Vector3(0, 1, 0);

        // SpriteRenderer
        var sr = playerObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDefaultSprite();
        sr.color = new Color(0.3f, 0.7f, 1f);
        sr.sortingOrder = 10;

        // Rigidbody2D
        var rb = playerObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
#if FUSION_PRESENT
        // 场景 NetworkObject 由 Host 模拟；NetworkTransform 将女儿的探索位置同步给母亲端。
        playerObj.AddComponent<Fusion.NetworkObject>();
        playerObj.AddComponent<Fusion.NetworkTransform>();
#endif

        // BoxCollider2D
        var col = playerObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.8f);

        // Player components
        var pc = playerObj.AddComponent<PlayerController>();
        var mc = playerObj.AddComponent<MovementController>();
        var hc = playerObj.AddComponent<HealthController>();
        var ic = playerObj.AddComponent<InteractionController>();
        var pih = playerObj.AddComponent<PlayerInputHandler>();

        // Wire up references via SerializedObject
        var pcSo = new SerializedObject(pc);
        pcSo.FindProperty("movementController").objectReferenceValue = mc;
        pcSo.FindProperty("interactionController").objectReferenceValue = ic;
        pcSo.FindProperty("healthController").objectReferenceValue = hc;
        pcSo.FindProperty("cachedInputHandler").objectReferenceValue = pih;
        pcSo.FindProperty("inputSettings").objectReferenceValue = inputSettings;
        pcSo.FindProperty("playerSettings").objectReferenceValue = playerSettings;
        pcSo.ApplyModifiedProperties();

        var icSo = new SerializedObject(ic);
        icSo.FindProperty("inputSettings").objectReferenceValue = inputSettings;
        icSo.FindProperty("playerSettings").objectReferenceValue = playerSettings;
        icSo.FindProperty("interactLayer").intValue = interactableMask;
        icSo.ApplyModifiedProperties();

        var hcSo = new SerializedObject(hc);
        hcSo.FindProperty("playerSettings").objectReferenceValue = playerSettings;
        hcSo.ApplyModifiedProperties();

        return playerObj;
    }

    private static GameObject CreateCamera(Transform followTarget)
    {
        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0, 0, -10);

        var cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5;
        cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);

        var rcc = camObj.AddComponent<RoomCameraController>();
        var rccSo = new SerializedObject(rcc);
        rccSo.FindProperty("target").objectReferenceValue = followTarget;
        rccSo.ApplyModifiedProperties();

        return camObj;
    }

    private static void CreateMiniGameManager(MiniGameSettings settings, RecipeConfig recipe1, RecipeConfig recipe2, DoNotForgetMe.MiniGame.Bagua.BaguaStoryConfig baguaConfig, AlbumConfig albumConfig)
    {
        var mgmObj = new GameObject("MiniGameManager");
        var manager = mgmObj.AddComponent<MiniGameManager>();
        var managerSo = new SerializedObject(manager);
        managerSo.FindProperty("recipes").arraySize = 2;
        managerSo.FindProperty("recipes").GetArrayElementAtIndex(0).objectReferenceValue = recipe1;
        managerSo.FindProperty("recipes").GetArrayElementAtIndex(1).objectReferenceValue = recipe2;
        managerSo.FindProperty("baguaConfigs").arraySize = 1;
        managerSo.FindProperty("baguaConfigs").GetArrayElementAtIndex(0).objectReferenceValue = baguaConfig;
        managerSo.FindProperty("albumConfigs").arraySize = 1;
        managerSo.FindProperty("albumConfigs").GetArrayElementAtIndex(0).objectReferenceValue = albumConfig;
        managerSo.ApplyModifiedProperties();
    }

    private static void CreateMiniGameTrigger(MiniGameSettings settings)
    {
        var triggerObj = new GameObject("MiniGameTrigger");
        triggerObj.layer = InteractableLayer;
        triggerObj.transform.position = new Vector3(12, -1.5f, 0);

        var sr = triggerObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDefaultSprite();
        sr.color = new Color(0.9f, 0.75f, 0.1f);
        sr.sortingOrder = 5;

        var col = triggerObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);
        col.isTrigger = true;

        var trigger = triggerObj.AddComponent<MiniGameTrigger>();
        var triggerSo = new SerializedObject(trigger);
        triggerSo.FindProperty("miniGameId").stringValue = "tomato_egg";
        triggerSo.FindProperty("settings").objectReferenceValue = settings;
        triggerSo.ApplyModifiedProperties();
    }

    private static AlbumConfig CreateAlbumConfigAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<AlbumConfig>(ALBUM_CONFIG_PATH);
        if (existing != null) return existing;

        var config = ScriptableObject.CreateInstance<AlbumConfig>();
        System.IO.Directory.CreateDirectory("Assets/_Project/Settings");
        AssetDatabase.CreateAsset(config, ALBUM_CONFIG_PATH);

        var so = new SerializedObject(config);

        // 6个人物
        var entriesProp = so.FindProperty("entries");
        entriesProp.arraySize = 6;

        // 刘洪秀
        SetAlbumEntry(entriesProp, 0, "liu_hongxiu", "刘洪秀",
            new Vector2(-400, 120), new Vector2(150, 200),
            new Vector2(-400, -20), new Vector2(200, 60),
            "大姐，总是攥着铜钥匙的人。", true);

        // 刘洪梅 / 小岩
        SetAlbumEntry(entriesProp, 1, "liu_hongmei", "刘洪梅",
            new Vector2(-160, 120), new Vector2(150, 200),
            new Vector2(-160, -20), new Vector2(200, 60),
            "她不喜欢这个名字。", false);

        // 刘洪菊
        SetAlbumEntry(entriesProp, 2, "liu_hongju", "刘洪菊",
            new Vector2(80, 120), new Vector2(150, 200),
            new Vector2(80, -20), new Vector2(200, 60),
            "三妹，铁皮糖盒里装着悄悄话。", true);

        // 刘洪芳
        SetAlbumEntry(entriesProp, 3, "liu_hongfang", "刘洪芳",
            new Vector2(320, 120), new Vector2(150, 200),
            new Vector2(320, -20), new Vector2(200, 60),
            "四妹，喜欢酸一点的味道。", true);

        // 刘洪强
        SetAlbumEntry(entriesProp, 4, "liu_hongqiang", "刘洪强",
            new Vector2(-280, -180), new Vector2(150, 200),
            new Vector2(-280, -320), new Vector2(200, 60),
            "五弟，爱吃甜的。", true);

        // 刘洪斌
        SetAlbumEntry(entriesProp, 5, "liu_hongbin", "刘洪斌",
            new Vector2(200, -180), new Vector2(150, 200),
            new Vector2(200, -320), new Vector2(200, 60),
            "六弟，小木车吱呀响。", true);

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        return config;
    }

    private static void SetAlbumEntry(SerializedProperty arrayProp, int index,
        string characterId, string displayName,
        Vector2 stickerPos, Vector2 stickerSize,
        Vector2 nameTagPos, Vector2 nameTagSize,
        string clueText, bool hasSticker)
    {
        var entry = arrayProp.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("characterId").stringValue = characterId;
        entry.FindPropertyRelative("displayName").stringValue = displayName;
        entry.FindPropertyRelative("stickerZonePosition").vector2Value = stickerPos;
        entry.FindPropertyRelative("stickerZoneSize").vector2Value = stickerSize;
        entry.FindPropertyRelative("nameTagZonePosition").vector2Value = nameTagPos;
        entry.FindPropertyRelative("nameTagZoneSize").vector2Value = nameTagSize;
        entry.FindPropertyRelative("clueText").stringValue = clueText;
        entry.FindPropertyRelative("hasSticker").boolValue = hasSticker;
    }

    private static void CreateAlbumTrigger(AlbumConfig albumConfig)
    {
        var triggerObj = new GameObject("AlbumTrigger");
        triggerObj.layer = InteractableLayer;
        triggerObj.transform.position = new Vector3(0, 0.5f, 0);

        var sr = triggerObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDefaultSprite();
        sr.color = new Color(0.55f, 0.45f, 0.3f);
        sr.sortingOrder = 5;

        var col = triggerObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.2f, 1f);
        col.isTrigger = true;

        var trigger = triggerObj.AddComponent<DoNotForgetMe.Interactables.AlbumTrigger>();
        var triggerSo = new SerializedObject(trigger);
        triggerSo.FindProperty("albumConfigId").stringValue = albumConfig.MiniGameId;
        triggerSo.ApplyModifiedProperties();
    }

    private static void CreateDoorListeningTrigger(DoNotForgetMe.MiniGame.Bagua.BaguaStoryConfig baguaConfig)
    {
        var doorObj = new GameObject("DoorListeningTrigger");
        doorObj.layer = InteractableLayer;
        doorObj.transform.position = new Vector3(20, -1.5f, 0);

        var sr = doorObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDefaultSprite();
        sr.color = new Color(0.5f, 0.3f, 0.2f);
        sr.sortingOrder = 5;

        var col = doorObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.2f, 2f);

        // 对话 AudioSource
        var audioGo = new GameObject("DialogueAudio");
        audioGo.transform.SetParent(doorObj.transform, false);
        var audioSource = audioGo.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;

        var trigger = doorObj.AddComponent<DoorListeningTrigger>();
        var triggerSo = new SerializedObject(trigger);
        triggerSo.FindProperty("baguaConfigId").stringValue = baguaConfig.MiniGameId;
        triggerSo.FindProperty("dialogueAudioSource").objectReferenceValue = audioSource;
        triggerSo.ApplyModifiedProperties();
    }

    private static void CreateEventSystem()
    {
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static void CreatePlatform(string name, Vector3 position, Vector3 scale, LayerMask groundLayer)
    {
        var obj = new GameObject(name);
        obj.layer = GroundLayer;
        obj.transform.position = position;
        obj.transform.localScale = scale;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDefaultSprite();
        sr.color = new Color(0.4f, 0.4f, 0.45f);

        var col = obj.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
    }

    private static Sprite _defaultSprite;
    private static Sprite CreateDefaultSprite()
    {
        if (_defaultSprite != null) return _defaultSprite;

        const int size = 64;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        _defaultSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _defaultSprite;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return;
        }

        var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        System.Array.Copy(scenes, newScenes, scenes.Length);
        newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;
    }
}
#endif
