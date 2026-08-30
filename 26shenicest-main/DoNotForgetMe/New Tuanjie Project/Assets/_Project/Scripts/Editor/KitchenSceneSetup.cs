#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DoNotForgetMe.Core;

/// <summary>
/// 厨房场景生成器。
/// 菜单：Tools/3C Setup/Create Kitchen Scene
/// 包含：Ground + 空气墙 + Player + Camera(constrainX) + MiniGameTrigger（做饭） + DoorToLivingRoom
/// 不放管理器（靠 DontDestroyOnLoad 从客厅带过来）。
/// </summary>
public static class KitchenSceneSetup
{
    private const string MENU_PATH = "Tools/3C Setup/Create Kitchen Scene";
    private const string SCENE_PATH = "Assets/_Project/Scenes/Kitchen.unity";

    [MenuItem(MENU_PATH)]
    public static void CreateScene()
    {
        var miniGameSettings = SceneSetupBase.LoadMiniGameSettings();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- 背景（电影质感排版） ---
        var bgSprite = SceneSetupBase.LoadSprite(SceneSetupBase.BG_KITCHEN_PATH);
        float bgWorldWidth = SceneSetupBase.CreateCinematicBackground(bgSprite);

        // --- 字幕条 ---
        SceneSetupBase.CreateSubtitleBar(bgWorldWidth);

        // --- 玩家 ---
        var player = SceneSetupBase.CreatePlayer();

        // --- 相机（水平边界按背景宽度计算） ---
        float aspect = SceneSetupBase.FIXED_ASPECT;
        float halfView = SceneSetupBase.CAMERA_ORTHO_SIZE * aspect;
        float camMinX = -bgWorldWidth * 0.5f + halfView;
        float camMaxX = bgWorldWidth * 0.5f - halfView;
        if (camMinX > camMaxX) { camMinX = 0; camMaxX = 0; }
        SceneSetupBase.CreateCamera(player.transform, camMinX, camMaxX);

        // --- EventSystem ---
        SceneSetupBase.CreateEventSystem();

        // --- 做饭触发器（灶台） ---
        SceneSetupBase.CreateMiniGameTrigger(miniGameSettings, new Vector3(2, SceneSetupBase.PLAYER_Y, 0));

        // --- 门（左→LivingRoom） ---
        SceneSetupBase.CreateSceneTransitionDoor("DoorToLivingRoom", new Vector3(-6, SceneSetupBase.PLAYER_Y, 0), SceneNames.LivingRoom);

        // --- 门（右→Courtyard，需做饭完成） ---
        SceneSetupBase.CreateSceneTransitionDoor("DoorToCourtyard", new Vector3(6, SceneSetupBase.PLAYER_Y, 0), SceneNames.Courtyard,
            new string[] { "photo_hongqiang", "photo_hongfang" });

        // --- 出生点 ---
        SceneSetupBase.CreatePlayerSpawn(-3f);

        // --- 保存 ---
        SceneSetupBase.SaveScene(SCENE_PATH);

        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);

        Debug.Log("[Kitchen Setup] 场景已创建: " + SCENE_PATH);
        Debug.Log("[Kitchen Setup] 管理器靠 DontDestroyOnLoad 从客厅带过来");
    }
}
#endif
