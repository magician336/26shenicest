#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene3CSetup
{
    private const string MENU_PATH = "Tools/3C Setup/Create Basic Scene";
    private const string SCENE_PATH = "Assets/_Project/Scenes/Game.unity";
    private const string INPUT_SETTINGS_PATH = "Assets/_Project/Settings/PlayerInputSettings.asset";
    private const string PLAYER_SETTINGS_PATH = "Assets/_Project/Settings/PlayerSettings.asset";
    private const string MINIGAME_SETTINGS_PATH = "Assets/_Project/Settings/MiniGameSettings.asset";

    private static readonly int GroundLayer = 8;
    private static readonly int PlayerLayer = 9;
    private static readonly int InteractableLayer = 10;

    [MenuItem(MENU_PATH)]
    public static void CreateScene()
    {
        EnsureLayers();

        var inputSettings = CreateInputSettingsAsset();
        var playerSettings = CreatePlayerSettingsAsset();
        var miniGameSettings = CreateMiniGameSettingsAsset();

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

        // --- GameManager ---
        var gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();

        // --- MiniGameManager + 模板 ---
        CreateMiniGameManager(miniGameSettings);

        // --- MiniGameTrigger（交互物体）---
        CreateMiniGameTrigger(miniGameSettings);

        // --- PlayerSpawn point ---
        var spawnObj = new GameObject("PlayerSpawn");
        spawnObj.transform.position = new Vector3(0, 0, 0);

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

    private static void CreateMiniGameManager(MiniGameSettings settings)
    {
        var mgmObj = new GameObject("MiniGameManager");
        mgmObj.AddComponent<MiniGameManager>();

        // SampleMiniGame 模板（不激活，MiniGameManager 自动注册）
        var templateObj = new GameObject("SampleMiniGame");
        templateObj.transform.SetParent(mgmObj.transform, false);
        templateObj.SetActive(false);
        templateObj.AddComponent<SampleMiniGame>();
    }

    private static void CreateMiniGameTrigger(MiniGameSettings settings)
    {
        var triggerObj = new GameObject("MiniGameTrigger");
        triggerObj.layer = InteractableLayer;
        triggerObj.transform.position = new Vector3(3, -1.5f, 0);

        var sr = triggerObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDefaultSprite();
        sr.color = new Color(0.9f, 0.75f, 0.1f);
        sr.sortingOrder = 5;

        var col = triggerObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        var trigger = triggerObj.AddComponent<MiniGameTrigger>();
        var triggerSo = new SerializedObject(trigger);
        triggerSo.FindProperty("miniGameId").stringValue = SampleMiniGame.Id;
        triggerSo.FindProperty("settings").objectReferenceValue = settings;
        triggerSo.ApplyModifiedProperties();
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

        var tex = new Texture2D(4, 4);
        var pixels = new Color[4 * 4];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        _defaultSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
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
