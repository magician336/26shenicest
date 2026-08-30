#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using Object = UnityEngine.Object;
using DoNotForgetMe.Core;
using DoNotForgetMe.MiniGame.Album;
using DoNotForgetMe.MiniGame.Cooking;

/// <summary>
/// 场景生成共享辅助方法。供 LivingRoomSceneSetup / KitchenSceneSetup / CourtyardSceneSetup 继承使用。
/// </summary>
public static class SceneSetupBase
{
    public static readonly int GroundLayer = 8;
    public static readonly int PlayerLayer = 9;
    public static readonly int InteractableLayer = 10;

    // --- 资产路径 ---
    public const string INPUT_SETTINGS_PATH = "Assets/_Project/Settings/PlayerInputSettings.asset";
    public const string PLAYER_SETTINGS_PATH = "Assets/_Project/Settings/PlayerSettings.asset";
    public const string MINIGAME_SETTINGS_PATH = "Assets/_Project/Settings/MiniGameSettings.asset";
    public const string PLAYER_PREFAB_PATH = "Assets/_Project/Resources/ScenePrefabs/Player.prefab";
    public const string GROUND_PREFAB_PATH = "Assets/_Project/Resources/ScenePrefabs/Ground.prefab";
    public const string COURTYARD_EAVESDROP_PREFAB_PATH = "Assets/_Project/Resources/ScenePrefabs/CourtyardEavesdropView.prefab";
    public const string TOMATO_EGG_RECIPE_PATH = "Assets/_Project/Settings/TomatoEggRecipe.asset";
    public const string CUCUMBER_SALAD_RECIPE_PATH = "Assets/_Project/Settings/CucumberSaladRecipe.asset";
    public const string BAGUA_CONFIG_PATH = "Assets/_Project/Settings/BaguaStoryConfig.asset";
    public const string ALBUM_CONFIG_PATH = "Assets/_Project/Settings/AlbumConfig.asset";

    // --- 电影质感排版常量 ---
    /// <summary>所有背景图统一缩放到此高度（世界单位），上下留黑</summary>
    public const float SCENE_VIEW_HEIGHT = 7f;
    /// <summary>相机固定 Y 值：视野中心</summary>
    public const float CAMERA_FIXED_Y = -0.5f;
    /// <summary>相机正交尺寸</summary>
    public const float CAMERA_ORTHO_SIZE = 5f;
    /// <summary>玩家 Y 坐标（站在地面上）</summary>
    public const float PLAYER_Y = -2.0f;
    /// <summary>背景 Y 中心：与相机中心对齐，上下各留黑边</summary>
    public const float BG_CENTER_Y = -0.5f;
    /// <summary>字幕条 Y 位置（底部黑色留白区域中心，需低于背景底边）</summary>
    public const float SUBTITLE_BAR_Y = -5.0f;
    /// <summary>固定宽高比（16:9），Edit Mode 下 Screen.width/height 不可靠</summary>
    public const float FIXED_ASPECT = 16f / 9f;
    /// <summary>背景 sortingOrder</summary>
    public const int BG_SORTING_ORDER = -10;
    /// <summary>黑底板 sortingOrder（在背景之后）</summary>
    public const int BLACK_BG_SORTING_ORDER = -20;

    // --- 美术素材路径 ---
    public const string BG_LIVINGROOM_PATH = "Assets/_Project/Art/Backgrounds/bg_livingroom.png";
    public const string BG_KITCHEN_PATH    = "Assets/_Project/Art/Backgrounds/bg_kitchen.png";
    public const string BG_COURTYARD_PATH  = "Assets/_Project/Art/Backgrounds/bg_courtyard.png";
    public const string BG_DESK_PATH       = "Assets/_Project/Art/Backgrounds/livingroom_bgdesk.png";
    public const string BG_BAGUA_DESK_PATH = "Assets/_Project/Art/Backgrounds/bg_desk.png";

    public const string ITEM_TOMATO_PATH       = "Assets/_Project/Art/Items/item_tomato.png";
    public const string ITEM_EGG_PATH          = "Assets/_Project/Art/Items/item_egg.png";
    public const string ITEM_CUCUMBER_PATH     = "Assets/_Project/Art/Items/item_cucumber.png";
    public const string ITEM_RIBS_PATH         = "Assets/_Project/Art/Items/item_ribs.png";
    public const string ITEM_KEY_PATH          = "Assets/_Project/Art/Items/item_key.png";
    public const string ITEM_TIN_CANDY_PATH    = "Assets/_Project/Art/Items/tin_candy_box.png";
    public const string ITEM_TIN_CANDY_FILLED_PATH = "Assets/_Project/Art/勿忘我第三关/微信图片_20260830005143_1408_3230.png";
    public const string ITEM_WOODEN_CART_PATH  = "Assets/_Project/Art/Items/wooden_cart.png";
    public const string ITEM_OLD_GLASSES_PATH  = "Assets/_Project/Art/Items/item_old_glasses.png";
    public const string ITEM_RED_COMB_PATH     = "Assets/_Project/Art/Items/item_red_comb.png";
    public const string ITEM_SCISSORS_PATH     = "Assets/_Project/Art/Items/item_scissors.png";
    public const string ITEM_PAPER_BOAT_PATH   = "Assets/_Project/Art/Items/item_paper_boat.png";
    public const string ITEM_ABACUS_PATH       = "Assets/_Project/Art/Items/item_abacus.png";
    public const string ITEM_WOK_PATH          = "Assets/_Project/Art/Items/wok.jpg";

    // --- 调料素材路径 ---
    public const string SEASONING_SUGAR_PATH   = "Assets/_Project/Art/Seasonings/suger.png";
    public const string SEASONING_SALT_PATH    = "Assets/_Project/Art/Seasonings/salt.png";
    public const string SEASONING_VINEGAR_PATH = "Assets/_Project/Art/Seasonings/vinegar.png";
    public const string SEASONING_CHILI_PATH   = "Assets/_Project/Art/Seasonings/chili.png";

    // --- 角色立绘素材路径 ---
    public const string PORTRAIT_HONGXIU_PATH  = "Assets/_Project/Art/Characters/portrait_hongxiu.png";
    public const string PORTRAIT_HONGJU_PATH   = "Assets/_Project/Art/Characters/portrait_hongju.png";
    public const string PORTRAIT_HONGBIN_PATH  = "Assets/_Project/Art/Characters/portrait_hongbin.png";
    public const string STICKER_HONGQIANG_PATH = "Assets/_Project/Art/Characters/sticker_hongqiang.png";
    public const string NAMETAG_HONGXIU_PATH   = "Assets/_Project/Art/Characters/name_hongxiu.png";
    public const string NAMETAG_HONGJU_PATH     = "Assets/_Project/Art/Characters/name_hongju.png";
    public const string NAMETAG_HONGBIN_PATH    = "Assets/_Project/Art/Characters/name_hongbin.png";

    // --- 照片素材路径 ---
    public const string PHOTO_HONGFANG_PATH      = "Assets/_Project/Art/Photos/photo_hongfang.png";
    public const string REWARD_TOMATO_EGG_PATH    = "Assets/_Project/Art/Photos/reward_tomato_egg.png";
    public const string REWARD_CUCUMBER_SALAD_PATH = "Assets/_Project/Art/Photos/reward_cucumber_salad.png";
    public const string BAGUA_OLD_FAMILY_PHOTO_PATH = "Assets/_Project/Art/Photos/bagua_old_family_photo.png";
    public const string LISTEN_BUTTON_PATH = "Assets/_Project/Art/Seasonings/listen.png";

    // --- 小游戏背景路径 ---
    public const string BG_COOKING_PATH = "Assets/_Project/Art/Backgrounds/bg_cooking.jpg";

    // --- 菜品照片路径 ---
    public const string DISH_TOMATO_EGG_PATH = "Assets/_Project/Art/Photos/dish_tomato_egg.png";
    public const string DISH_CUCUMBER_SALAD_PATH = "Assets/_Project/Art/Photos/dish_cucumber_salad.png";
    public const string DAUGHTER_BG_PATH = "Assets/_Project/Art/Backgrounds/daughter_bg.jpg";
    public const string MOM_BG_FINAL_PATH = "Assets/_Project/Art/Backgrounds/mom_bg_final.png";
    public const string DAUGHTER_GAME2_BG_PATH = "Assets/_Project/Art/Backgrounds/daughter_game2_bg.png";
    public const string PHOTO_ZONE_SPRITE_PATH = "Assets/_Project/Art/Items/match_person.png";

    public static InputSettings LoadInputSettings()
    {
        return AssetDatabase.LoadAssetAtPath<InputSettings>(INPUT_SETTINGS_PATH);
    }

    public static PlayerSettings LoadPlayerSettings()
    {
        return AssetDatabase.LoadAssetAtPath<PlayerSettings>(PLAYER_SETTINGS_PATH);
    }

    public static MiniGameSettings LoadMiniGameSettings()
    {
        return AssetDatabase.LoadAssetAtPath<MiniGameSettings>(MINIGAME_SETTINGS_PATH);
    }

    public static RecipeConfig LoadTomatoEggRecipe()
    {
        return AssetDatabase.LoadAssetAtPath<RecipeConfig>(TOMATO_EGG_RECIPE_PATH);
    }

    public static RecipeConfig LoadCucumberSaladRecipe()
    {
        return AssetDatabase.LoadAssetAtPath<RecipeConfig>(CUCUMBER_SALAD_RECIPE_PATH);
    }

    public static DoNotForgetMe.MiniGame.Bagua.BaguaStoryConfig LoadBaguaConfig()
    {
        return AssetDatabase.LoadAssetAtPath<DoNotForgetMe.MiniGame.Bagua.BaguaStoryConfig>(BAGUA_CONFIG_PATH);
    }

    public static AlbumConfig LoadAlbumConfig()
    {
        return AssetDatabase.LoadAssetAtPath<AlbumConfig>(ALBUM_CONFIG_PATH);
    }

    public static Sprite LoadSprite(string assetPath)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    // ==============================
    // 场景对象创建
    // ==============================

    /// <summary>创建地面，宽度应匹配背景图横向长度。从 Ground Prefab 实例化并设置缩放。</summary>
    public static void CreateGround(float width = 20f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GROUND_PREFAB_PATH);
        if (prefab == null)
        {
            Debug.LogError("[SceneSetup] Ground Prefab 不存在，请先运行 Tools > Scene > Export Player & Ground Prefabs");
            return;
        }
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        obj.transform.localScale = new Vector3(width, 1, 1);
    }

    /// <summary>
    /// 创建电影质感排版的全套场景元素：黑底板 + 背景图 + 地面 + 空气墙。
    /// 返回背景图的世界宽度，供相机边界和走区域参考。
    /// </summary>
    public static float CreateCinematicBackground(Sprite bgSprite)
    {
        if (bgSprite == null)
        {
            Debug.LogWarning("[SceneSetup] 背景精灵为空，跳过背景创建。");
            return 20f;
        }

        // 计算背景图缩放到统一高度后的世界宽度
        float spriteAspect = bgSprite.rect.width / bgSprite.rect.height;
        float bgWorldHeight = SCENE_VIEW_HEIGHT;
        float bgWorldWidth = bgWorldHeight * spriteAspect;

        // --- 黑色底板（覆盖整个相机视野） ---
        CreateBlackUnderlay(bgWorldWidth);

        // --- 背景图 ---
        var bgObj = new GameObject("Background");
        bgObj.transform.position = new Vector3(0, BG_CENTER_Y, 5);

        var sr = bgObj.AddComponent<SpriteRenderer>();
        sr.sprite = bgSprite;
        sr.sortingOrder = BG_SORTING_ORDER;

        float spriteW = bgSprite.rect.width / bgSprite.pixelsPerUnit;
        float spriteH = bgSprite.rect.height / bgSprite.pixelsPerUnit;
        float scale = bgWorldHeight / spriteH;
        bgObj.transform.localScale = new Vector3(scale, scale, 1);

        // --- 地面（宽度匹配背景图横向长度） ---
        CreateGround(bgWorldWidth);

        // --- 空气墙（在背景图两端） ---
        float wallX = bgWorldWidth * 0.5f;
        CreateAirWall("LeftWall", -wallX);
        CreateAirWall("RightWall", wallX);

        Debug.Log($"[Cinematic] bgWorldWidth={bgWorldWidth:F2}, bgWorldHeight={bgWorldHeight}, aspect={spriteAspect:F2}");

        return bgWorldWidth;
    }

    public static void CreateAirWall(string name, float xPos)
    {
        var obj = new GameObject(name);
        obj.layer = GroundLayer;
        obj.transform.position = new Vector3(xPos, 0, 0);
        obj.transform.localScale = new Vector3(1, 10, 1);

        // 无 SpriteRenderer（隐形空气墙）
        var col = obj.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
    }

    /// <summary>创建纯黑底板，覆盖整个相机视野及背景图横向范围。</summary>
    private static void CreateBlackUnderlay(float bgWorldWidth)
    {
        // 底板需要覆盖整个相机视野宽度（含 aspect）和背景宽度中较大者
        float aspect = FIXED_ASPECT;
        if (aspect <= 0) aspect = 16f / 9f;
        float viewWidth = CAMERA_ORTHO_SIZE * 2f * aspect;
        float underlayWidth = Mathf.Max(bgWorldWidth, viewWidth) + 4f;
        float underlayHeight = CAMERA_ORTHO_SIZE * 2f + 2f;

        var obj = new GameObject("BlackUnderlay");
        obj.transform.position = new Vector3(0, CAMERA_FIXED_Y, 10);

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDefaultSprite();
        sr.color = Color.black;
        sr.sortingOrder = BLACK_BG_SORTING_ORDER;

        obj.transform.localScale = new Vector3(underlayWidth, underlayHeight, 1);
    }

    /// <summary>创建电影质感相机：正交尺寸5，固定Y，黑色背景，水平跟随有边界。</summary>
    public static GameObject CreateCamera(Transform followTarget, float minX, float maxX)
    {
        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0, CAMERA_FIXED_Y, -10);

        var cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = CAMERA_ORTHO_SIZE;
        cam.backgroundColor = Color.black;

        // AudioListener 必须存在于场景中，否则所有音频播放无效
        camObj.AddComponent<AudioListener>();

        var rcc = camObj.AddComponent<RoomCameraController>();
        var rccSo = new SerializedObject(rcc);
        rccSo.FindProperty("target").objectReferenceValue = followTarget;
        rccSo.FindProperty("constrainX").boolValue = true;
        rccSo.FindProperty("minX").floatValue = minX;
        rccSo.FindProperty("maxX").floatValue = maxX;
        rccSo.FindProperty("constrainY").boolValue = true;
        rccSo.FindProperty("fixedY").floatValue = CAMERA_FIXED_Y;
        rccSo.ApplyModifiedProperties();

        return camObj;
    }

    /// <summary>从 Player Prefab 实例化玩家。所有组件和引用已在 Prefab 中配置好。</summary>
    public static GameObject CreatePlayer()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
        if (prefab == null)
        {
            Debug.LogError("[SceneSetup] Player Prefab 不存在，请先运行 Tools > Scene > Export Player & Ground Prefabs");
            return null;
        }
        return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    }

    public static void CreateEventSystem()
    {
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    public static void CreatePlayerSpawn(float x, float y = -2.0f)
    {
        var spawnObj = new GameObject("PlayerSpawn");
        spawnObj.transform.position = new Vector3(x, y, 0);
    }

    public static GameObject CreateGameManager()
    {
        var gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();
        return gmObj;
    }

    public static GameObject CreateCoordinator(RecipeConfig tomatoEgg, RecipeConfig cucumberSalad,
        DoNotForgetMe.MiniGame.Bagua.BaguaStoryConfig baguaConfig, AlbumConfig albumConfig)
    {
        var coordinatorObj = new GameObject("SessionGameplayCoordinator");
        var coordinator = coordinatorObj.AddComponent<DoNotForgetMe.Network.Gameplay.SessionGameplayCoordinator>();
        var so = new SerializedObject(coordinator);
        so.FindProperty("recipes").arraySize = 2;
        so.FindProperty("recipes").GetArrayElementAtIndex(0).objectReferenceValue = tomatoEgg;
        so.FindProperty("recipes").GetArrayElementAtIndex(1).objectReferenceValue = cucumberSalad;
        so.FindProperty("baguaConfigs").arraySize = 1;
        so.FindProperty("baguaConfigs").GetArrayElementAtIndex(0).objectReferenceValue = baguaConfig;
        so.FindProperty("albumConfigs").arraySize = 1;
        so.FindProperty("albumConfigs").GetArrayElementAtIndex(0).objectReferenceValue = albumConfig;

        // 对白配置
        var dlg1 = AssetDatabase.LoadAssetAtPath<DoNotForgetMe.Dialogue.DialogueSequence>("Assets/_Project/Audio/Dialogue/DLG_EnterMemory.asset");
        var dlg2 = AssetDatabase.LoadAssetAtPath<DoNotForgetMe.Dialogue.DialogueSequence>("Assets/_Project/Audio/Dialogue/DLG_Game1ToGame2.asset");
        var dlg3 = AssetDatabase.LoadAssetAtPath<DoNotForgetMe.Dialogue.DialogueSequence>("Assets/_Project/Audio/Dialogue/DLG_Game2ToGame3.asset");
        var dlg4 = AssetDatabase.LoadAssetAtPath<DoNotForgetMe.Dialogue.DialogueSequence>("Assets/_Project/Audio/Dialogue/DLG_Ending.asset");
        var dlg5 = AssetDatabase.LoadAssetAtPath<DoNotForgetMe.Dialogue.DialogueSequence>("Assets/_Project/Audio/Dialogue/DLG_AlbumPrompt.asset");
        var dialogues = new[] { dlg1, dlg2, dlg3, dlg4, dlg5 };
        so.FindProperty("dialogueConfigs").arraySize = dialogues.Length;
        for (int i = 0; i < dialogues.Length; i++)
            so.FindProperty("dialogueConfigs").GetArrayElementAtIndex(i).objectReferenceValue = dialogues[i];
        so.FindProperty("openingDialogueId").stringValue = "DLG_EnterMemory";

#if !FUSION_PRESENT
        so.FindProperty("debugSingleProcess").boolValue = true;
        coordinatorObj.AddComponent<DoNotForgetMe.Network.Local.LocalGameplayBridge>();
#else
        coordinatorObj.AddComponent<Fusion.NetworkObject>();
        coordinatorObj.AddComponent<DoNotForgetMe.Network.Fusion.FusionGameplayBridge>();
#endif
        so.ApplyModifiedProperties();

        // AudioManager (不在 Edit Mode 调 DontDestroyOnLoad)
        var audioMgrObj = new GameObject("AudioManager");
        var audioMgr = audioMgrObj.AddComponent<DoNotForgetMe.Audio.AudioManager>();
        var audioLib = AssetDatabase.LoadAssetAtPath<DoNotForgetMe.Audio.AudioLibrary>("Assets/_Project/Settings/AudioLibrary.asset");
        var amSo = new SerializedObject(audioMgr);
        amSo.FindProperty("library").objectReferenceValue = audioLib;
        amSo.ApplyModifiedProperties();

        return coordinatorObj;
    }

    public static void CreateMiniGameManager(RecipeConfig recipe1, RecipeConfig recipe2,
        DoNotForgetMe.MiniGame.Bagua.BaguaStoryConfig baguaConfig, AlbumConfig albumConfig)
    {
        var mgmObj = new GameObject("MiniGameManager");
        var manager = mgmObj.AddComponent<MiniGameManager>();
        var so = new SerializedObject(manager);
        so.FindProperty("recipes").arraySize = 2;
        so.FindProperty("recipes").GetArrayElementAtIndex(0).objectReferenceValue = recipe1;
        so.FindProperty("recipes").GetArrayElementAtIndex(1).objectReferenceValue = recipe2;
        so.FindProperty("baguaConfigs").arraySize = 1;
        so.FindProperty("baguaConfigs").GetArrayElementAtIndex(0).objectReferenceValue = baguaConfig;
        so.FindProperty("albumConfigs").arraySize = 1;
        so.FindProperty("albumConfigs").GetArrayElementAtIndex(0).objectReferenceValue = albumConfig;
        so.ApplyModifiedProperties();
    }

    public static GameObject CreateSceneTransitionDoor(string name, Vector3 position, string targetScene,
        string[] requiredPhotoIds = null)
    {
        var doorObj = new GameObject(name);
        doorObj.layer = InteractableLayer;
        doorObj.transform.position = position;

        var sr = doorObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        sr.sprite = CreateDefaultSprite();
        sr.color = new Color(1f, 0.8f, 0.2f, 0.3f);

        var col = doorObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.2f, 2f);
        col.isTrigger = true;

        var trigger = doorObj.AddComponent<DoNotForgetMe.Interactables.SceneTransitionTrigger>();
        var so = new SerializedObject(trigger);
        so.FindProperty("targetScene").stringValue = targetScene;
        if (requiredPhotoIds != null && requiredPhotoIds.Length > 0)
        {
            var prop = so.FindProperty("requiredPhotoIds");
            prop.arraySize = requiredPhotoIds.Length;
            for (int i = 0; i < requiredPhotoIds.Length; i++)
                prop.GetArrayElementAtIndex(i).stringValue = requiredPhotoIds[i];
        }
        so.ApplyModifiedProperties();
        return doorObj;
    }

    public static void CreateMiniGameTrigger(MiniGameSettings settings, Vector3 position, string miniGameId = "tomato_egg")
    {
        var triggerObj = new GameObject("MiniGameTrigger");
        triggerObj.layer = InteractableLayer;
        triggerObj.transform.position = position;

        var sr = triggerObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        sr.sprite = CreateDefaultSprite();
        sr.color = new Color(0.3f, 1f, 0.4f, 0.3f);

        var col = triggerObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);
        col.isTrigger = true;

        var trigger = triggerObj.AddComponent<MiniGameTrigger>();
        var so = new SerializedObject(trigger);
        so.FindProperty("miniGameId").stringValue = miniGameId;
        so.FindProperty("settings").objectReferenceValue = settings;
        so.ApplyModifiedProperties();
    }

    public static void CreateDoorListeningTrigger(DoNotForgetMe.MiniGame.Bagua.BaguaStoryConfig baguaConfig, Vector3 position)
    {
        var doorObj = new GameObject("DoorListeningTrigger");
        doorObj.layer = InteractableLayer;
        doorObj.transform.position = position;

        var sr = doorObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        sr.sprite = CreateDefaultSprite();
        sr.color = new Color(1f, 0.4f, 0.4f, 0.3f);

        var col = doorObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.2f, 2f);

        var audioGo = new GameObject("DialogueAudio");
        audioGo.transform.SetParent(doorObj.transform, false);
        var audioSource = audioGo.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;

        var trigger = doorObj.AddComponent<DoorListeningTrigger>();
        var so = new SerializedObject(trigger);
        so.FindProperty("baguaConfigId").stringValue = baguaConfig.MiniGameId;
        so.FindProperty("dialogueAudioSource").objectReferenceValue = audioSource;
        so.ApplyModifiedProperties();
    }

    public static void CreateDeskViewController(Vector3 position)
    {
        var deskObj = new GameObject("DeskViewController");
        deskObj.layer = InteractableLayer;
        deskObj.transform.position = position;

        var sr = deskObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        sr.sprite = CreateDefaultSprite();
        sr.color = new Color(0.4f, 0.6f, 1f, 0.3f);

        var col = deskObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.2f, 1f);
        col.isTrigger = true;

        var dvc = deskObj.AddComponent<DoNotForgetMe.Cutscene.DeskViewController>();
        var so = new SerializedObject(dvc);
        so.FindProperty("deskBackgroundSprite").objectReferenceValue = LoadSprite(BG_DESK_PATH);
        so.ApplyModifiedProperties();
    }

    private static void CreatePlatform(string name, Vector3 position, Vector3 scale)
    {
        var obj = new GameObject(name);
        obj.layer = GroundLayer;
        obj.transform.position = position;
        obj.transform.localScale = scale;

        var col = obj.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
    }

    /// <summary>创建电影质感字幕条（底部黑色留白区域 + TMP 文字），宽度匹配背景图。
    /// 结构：SubtitleSystem(始终激活,AudioSource) → SubtitleBar(默认隐藏) → SubtitleText(默认隐藏)</summary>
    public static void CreateSubtitleBar(float bgWorldWidth = 0f)
    {
        var aspect = (float)Screen.width / Screen.height;
        if (aspect <= 0) aspect = 16f / 9f;
        float viewWidth = CAMERA_ORTHO_SIZE * 2f * aspect;
        float barWidth = Mathf.Max(bgWorldWidth, viewWidth) + 4f;

        // SubtitleSystem：始终激活，承载 CinematicSubtitle + AudioSource（不在 SubtitleBar 上显示喇叭图标）
        var systemObj = new GameObject("SubtitleSystem");

        // SubtitleBar：底部黑色留白区域，默认隐藏
        var barObj = new GameObject("SubtitleBar");
        barObj.transform.SetParent(systemObj.transform, false);
        barObj.transform.position = new Vector3(0, SUBTITLE_BAR_Y, 0);

        var barSr = barObj.AddComponent<SpriteRenderer>();
        barSr.sprite = CreateDefaultSprite();
        barSr.color = Color.black;
        barSr.sortingOrder = 5;
        barSr.enabled = false;
        barObj.transform.localScale = new Vector3(barWidth, 1.6f, 1);

        // SubtitleText：TMP 文字，默认隐藏
        var textObj = new GameObject("SubtitleText");
        textObj.transform.SetParent(barObj.transform, false);
        textObj.transform.localPosition = new Vector3(0, 0, -1);
        var tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = "";
        tmp.fontSize = 3.0f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(barWidth, 1.2f);

        var mr = textObj.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 10;

        var fontAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_Project/Fonts/ZhongHuaXinHuoTi SDF.asset");
        if (fontAsset != null)
            tmp.font = fontAsset;

        textObj.SetActive(false);
        barObj.SetActive(false);

        // CinematicSubtitle 挂在 SubtitleSystem 上（始终激活），AudioSource 也在 SubtitleSystem 上
        systemObj.AddComponent<CinematicSubtitle>();
    }

    private static Sprite _defaultSprite;
    public static Sprite CreateDefaultSprite()
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

    public static void SaveScene(string scenePath)
    {
        System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
        AddSceneToBuildSettings(scenePath);
    }

    public static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return;
        }
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    /// <summary>将已导入的美术素材接线到现有 ScriptableObject 配置资产。</summary>
    [MenuItem("Tools/3C Setup/Wire Art Assets")]
    public static void WireArtAssets()
    {
        // 1. 确保所有纹理以 Sprite 模式导入
        var texPaths = new[] {
            BG_LIVINGROOM_PATH, BG_KITCHEN_PATH, BG_COURTYARD_PATH, BG_DESK_PATH, BG_BAGUA_DESK_PATH, BG_COOKING_PATH,
            ITEM_TOMATO_PATH, ITEM_EGG_PATH, ITEM_CUCUMBER_PATH, ITEM_RIBS_PATH,
            ITEM_KEY_PATH, ITEM_TIN_CANDY_PATH, ITEM_WOODEN_CART_PATH,
            ITEM_OLD_GLASSES_PATH, ITEM_RED_COMB_PATH, ITEM_SCISSORS_PATH, ITEM_PAPER_BOAT_PATH, ITEM_ABACUS_PATH,
            ITEM_WOK_PATH,
            SEASONING_SUGAR_PATH, SEASONING_SALT_PATH, SEASONING_VINEGAR_PATH, SEASONING_CHILI_PATH,
            PORTRAIT_HONGXIU_PATH, PORTRAIT_HONGJU_PATH, PORTRAIT_HONGBIN_PATH,
            STICKER_HONGQIANG_PATH,
            PHOTO_HONGFANG_PATH, REWARD_TOMATO_EGG_PATH, REWARD_CUCUMBER_SALAD_PATH,
            BAGUA_OLD_FAMILY_PHOTO_PATH, LISTEN_BUTTON_PATH,
            DISH_TOMATO_EGG_PATH, DISH_CUCUMBER_SALAD_PATH,
            DAUGHTER_BG_PATH, MOM_BG_FINAL_PATH, DAUGHTER_GAME2_BG_PATH
        };
        foreach (var tp in texPaths)
        {
            var importer = AssetImporter.GetAtPath(tp) as TextureImporter;
            if (importer == null) continue;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }
        AssetDatabase.Refresh();

        int wired = 0;

        // --- RecipeConfig: 食材+调料 Sprite (8个) + 奖励照片 ---
        var recipeSprites = new (string id, string path)[] {
            ("tomato",   ITEM_TOMATO_PATH),
            ("egg",      ITEM_EGG_PATH),
            ("cucumber", ITEM_CUCUMBER_PATH),
            ("ribs",     ITEM_RIBS_PATH),
            ("sugar",    SEASONING_SUGAR_PATH),
            ("salt",     SEASONING_SALT_PATH),
            ("vinegar",  SEASONING_VINEGAR_PATH),
            ("chili",    SEASONING_CHILI_PATH),
        };

        var tomatoEgg = AssetDatabase.LoadAssetAtPath<RecipeConfig>(TOMATO_EGG_RECIPE_PATH);
        if (tomatoEgg != null)
        {
            var so = new SerializedObject(tomatoEgg);
            var arr = so.FindProperty("ingredientSprites");
            arr.arraySize = recipeSprites.Length;
            for (int i = 0; i < recipeSprites.Length; i++)
                SetIngredientSprite(arr, i, recipeSprites[i].id, LoadSprite(recipeSprites[i].path));
            so.FindProperty("rewardPhotoSprite").objectReferenceValue = LoadSprite(REWARD_TOMATO_EGG_PATH);
            so.FindProperty("containerSprite").objectReferenceValue = LoadSprite(ITEM_WOK_PATH);
            so.FindProperty("cookingBackground").objectReferenceValue = LoadSprite(BG_COOKING_PATH);
            so.FindProperty("dishPhotoSprite").objectReferenceValue = LoadSprite(DISH_TOMATO_EGG_PATH);
            so.FindProperty("daughterBackground").objectReferenceValue = LoadSprite(DAUGHTER_BG_PATH);
            so.FindProperty("motherCompleteBackground").objectReferenceValue = LoadSprite(MOM_BG_FINAL_PATH);
            so.FindProperty("motherCompleteHint").objectReferenceValue = LoadSprite("Assets/_Project/Art/Resources/prompt_sugar.png");
            so.ApplyModifiedProperties();
            wired++;
            Debug.Log("[WireArt] TomatoEgg 食材/调料+奖励照片+容器+背景+菜图+女儿背景+完成背景+提示图 已接线");
        }

        var cucumberSalad = AssetDatabase.LoadAssetAtPath<RecipeConfig>(CUCUMBER_SALAD_RECIPE_PATH);
        if (cucumberSalad != null)
        {
            var so = new SerializedObject(cucumberSalad);
            var arr = so.FindProperty("ingredientSprites");
            arr.arraySize = recipeSprites.Length;
            for (int i = 0; i < recipeSprites.Length; i++)
                SetIngredientSprite(arr, i, recipeSprites[i].id, LoadSprite(recipeSprites[i].path));
            so.FindProperty("rewardPhotoSprite").objectReferenceValue = LoadSprite(REWARD_CUCUMBER_SALAD_PATH);
            so.FindProperty("containerSprite").objectReferenceValue = LoadSprite(ITEM_WOK_PATH);
            so.FindProperty("cookingBackground").objectReferenceValue = LoadSprite(BG_COOKING_PATH);
            so.FindProperty("dishPhotoSprite").objectReferenceValue = LoadSprite(DISH_CUCUMBER_SALAD_PATH);
            so.FindProperty("daughterBackground").objectReferenceValue = LoadSprite(DAUGHTER_BG_PATH);
            so.FindProperty("motherCompleteBackground").objectReferenceValue = LoadSprite(MOM_BG_FINAL_PATH);
            so.ApplyModifiedProperties();
            wired++;
            Debug.Log("[WireArt] CucumberSalad 食材/调料+奖励照片+容器+背景+菜图+女儿背景+完成背景 已接线");
        }

        // --- BaguaStoryConfig: 物件 Sprite + 人物立绘 + 桌面背景 ---
        var bagua = AssetDatabase.LoadAssetAtPath<DoNotForgetMe.MiniGame.Bagua.BaguaStoryConfig>(BAGUA_CONFIG_PATH);
        if (bagua != null)
        {
            var so = new SerializedObject(bagua);

            // 物件 Sprite
            var itemSprites = new (string itemId, string path)[] {
                ("key",           ITEM_KEY_PATH),
                ("tin_candy_box", ITEM_TIN_CANDY_FILLED_PATH),
                ("wooden_cart",   ITEM_WOODEN_CART_PATH),
                ("old_glasses",   ITEM_OLD_GLASSES_PATH),
                ("red_comb",      ITEM_RED_COMB_PATH),
                ("scissors",      ITEM_SCISSORS_PATH),
                ("paper_boat",    ITEM_PAPER_BOAT_PATH),
                ("abacus",        ITEM_ABACUS_PATH),
            };
            var items = so.FindProperty("itemPlacements");
            foreach (var (itemId, path) in itemSprites)
            {
                var sprite = LoadSprite(path);
                if (sprite == null) continue;
                for (int i = 0; i < items.arraySize; i++)
                {
                    var item = items.GetArrayElementAtIndex(i);
                    if (item.FindPropertyRelative("itemId").stringValue == itemId)
                    {
                        item.FindPropertyRelative("sprite").objectReferenceValue = sprite;
                        Debug.Log($"[WireArt] BaguaStoryConfig item '{itemId}' Sprite 已接线");
                        break;
                    }
                }
            }

            // 人物立绘
            var portraitSprites = new (string characterId, string path)[] {
                ("liu_hongxiu", PORTRAIT_HONGXIU_PATH),
                ("liu_hongju",  PORTRAIT_HONGJU_PATH),
                ("liu_hongbin", PORTRAIT_HONGBIN_PATH),
            };
            var entries = so.FindProperty("entries");
            foreach (var (characterId, path) in portraitSprites)
            {
                var sprite = LoadSprite(path);
                if (sprite == null) continue;
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("characterId").stringValue == characterId)
                    {
                        entry.FindPropertyRelative("portrait").objectReferenceValue = sprite;
                        Debug.Log($"[WireArt] BaguaStoryConfig portrait '{characterId}' 已接线");
                        break;
                    }
                }
            }

            // 桌面背景（八卦小游戏专用 bg_desk.png，与书桌视角 livingroom_bgdesk.png 区分）
            var deskBg = LoadSprite(BG_BAGUA_DESK_PATH);
            if (deskBg != null)
            {
                so.FindProperty("deskBackground").objectReferenceValue = deskBg;
                Debug.Log("[WireArt] BaguaStoryConfig deskBackground (bg_desk) 已接线");
            }

            // 老家庭照片
            var oldPhoto = LoadSprite(BAGUA_OLD_FAMILY_PHOTO_PATH);
            if (oldPhoto != null)
            {
                so.FindProperty("oldFamilyPhoto").objectReferenceValue = oldPhoto;
                Debug.Log("[WireArt] BaguaStoryConfig oldFamilyPhoto 已接线");
            }

            // 听按钮图标
            var listenSprite = LoadSprite(LISTEN_BUTTON_PATH);
            if (listenSprite != null)
            {
                so.FindProperty("listenButtonSprite").objectReferenceValue = listenSprite;
                Debug.Log("[WireArt] BaguaStoryConfig listenButtonSprite 已接线");
            }

            // 女儿端照片认人背景
            var daughterGame2Bg = LoadSprite(DAUGHTER_GAME2_BG_PATH);
            if (daughterGame2Bg != null)
            {
                so.FindProperty("daughterPhotoBackground").objectReferenceValue = daughterGame2Bg;
                Debug.Log("[WireArt] BaguaStoryConfig daughterPhotoBackground 已接线");
            }

            // 照片投放区图标
            var photoZoneSprite = LoadSprite(PHOTO_ZONE_SPRITE_PATH);
            if (photoZoneSprite != null)
            {
                so.FindProperty("photoZoneSprite").objectReferenceValue = photoZoneSprite;
                Debug.Log("[WireArt] BaguaStoryConfig photoZoneSprite 已接线");
            }

            // 姓名标签图标
            var nameTagSprites = new (string characterId, string path)[] {
                ("liu_hongxiu", NAMETAG_HONGXIU_PATH),
                ("liu_hongju",  NAMETAG_HONGJU_PATH),
                ("liu_hongbin", NAMETAG_HONGBIN_PATH),
            };
            var baguaEntries = so.FindProperty("entries");
            if (baguaEntries != null)
            {
                foreach (var (characterId, path) in nameTagSprites)
                {
                    var sprite = LoadSprite(path);
                    if (sprite == null) continue;
                    for (int i = 0; i < baguaEntries.arraySize; i++)
                    {
                        var entry = baguaEntries.GetArrayElementAtIndex(i);
                        if (entry.FindPropertyRelative("characterId").stringValue == characterId)
                        {
                            entry.FindPropertyRelative("nameTagSprite").objectReferenceValue = sprite;
                            Debug.Log($"[WireArt] BaguaStoryConfig nameTagSprite '{characterId}' 已接线");
                            break;
                        }
                    }
                }
            }

            so.ApplyModifiedProperties();
            wired++;
        }

        // --- AlbumConfig: 贴纸 + 照片 ---
        var album = AssetDatabase.LoadAssetAtPath<AlbumConfig>(ALBUM_CONFIG_PATH);
        if (album != null)
        {
            var so = new SerializedObject(album);
            var entries = so.FindProperty("entries");

            // 刘洪强贴纸
            var stickerSprite = LoadSprite(STICKER_HONGQIANG_PATH);
            if (stickerSprite != null)
            {
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("characterId").stringValue == "liu_hongqiang")
                    {
                        entry.FindPropertyRelative("stickerSprite").objectReferenceValue = stickerSprite;
                        Debug.Log("[WireArt] AlbumConfig sticker 'liu_hongqiang' 已接线");
                        break;
                    }
                }
            }

            // 刘洪芳线索照片
            var photoSprite = LoadSprite(PHOTO_HONGFANG_PATH);
            if (photoSprite != null)
            {
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("characterId").stringValue == "liu_hongfang")
                    {
                        entry.FindPropertyRelative("photoSprite").objectReferenceValue = photoSprite;
                        Debug.Log("[WireArt] AlbumConfig photo 'liu_hongfang' 已接线");
                        break;
                    }
                }
            }

            so.ApplyModifiedProperties();
            wired++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[WireArt] 完成！{wired} 个配置资产已更新。");
    }

    private static void SetIngredientSprite(SerializedProperty arrayProp, int index, string ingredientId, Sprite sprite)
    {
        var entry = arrayProp.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("ingredientId").stringValue = ingredientId;
        entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
    }

    /// <summary>仅更新八卦小游戏的背景图和刘洪菊物品图。</summary>
    [MenuItem("Tools/MiniGame/Update Bagua Assets")]
    public static void UpdateBaguaAssets()
    {
        var guids = AssetDatabase.FindAssets("t:BaguaStoryConfig");
        if (guids.Length == 0)
        {
            Debug.LogWarning("[UpdateBagua] 未找到 BaguaStoryConfig.asset");
            return;
        }
        var config = AssetDatabase.LoadAssetAtPath<DoNotForgetMe.MiniGame.Bagua.BaguaStoryConfig>(
            AssetDatabase.GUIDToAssetPath(guids[0]));
        if (config == null) return;

        var so = new SerializedObject(config);

        // 1. 背景图 → bg_desk.png
        var deskBg = LoadSprite(BG_BAGUA_DESK_PATH);
        if (deskBg != null)
        {
            so.FindProperty("deskBackground").objectReferenceValue = deskBg;
            Debug.Log("[UpdateBagua] deskBackground → bg_desk.png");
        }

        // 2. tin_candy_box 物品图 → 微信图片_20260830005143_1408_3230.png
        var tinFilled = LoadSprite(ITEM_TIN_CANDY_FILLED_PATH);
        if (tinFilled != null)
        {
            var items = so.FindProperty("itemPlacements");
            for (int i = 0; i < items.arraySize; i++)
            {
                var item = items.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("itemId").stringValue == "tin_candy_box")
                {
                    item.FindPropertyRelative("sprite").objectReferenceValue = tinFilled;
                    Debug.Log("[UpdateBagua] tin_candy_box sprite → 微信图片_20260830005143_1408_3230.png");
                    break;
                }
            }
        }

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(config);
        Debug.Log("[UpdateBagua] 完成！请在 Inspector 中确认 BaguaStoryConfig 已更新。");
    }
}
#endif
