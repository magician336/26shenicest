#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DoNotForgetMe.Core;

/// <summary>
/// 客厅场景生成器。
/// 菜单：Tools/3C Setup/Create Living Room Scene
/// 包含：Ground + 空气墙 + Player + Camera(constrainX) + GameManager + Coordinator + MiniGameManager
///       + DeskViewController（书桌，F交互） + DoorToKitchen（门，F转场）
/// 这是管理器首次创建的场景，后续场景靠 DontDestroyOnLoad 带过去。
/// </summary>
public static class LivingRoomSceneSetup
{
    private const string MENU_PATH = "Tools/3C Setup/Create Living Room Scene";
    private const string SCENE_PATH = "Assets/_Project/Scenes/LivingRoom.unity";

    [MenuItem(MENU_PATH)]
    public static void CreateScene()
    {
        var inputSettings = SceneSetupBase.LoadInputSettings();
        var playerSettings = SceneSetupBase.LoadPlayerSettings();
        var miniGameSettings = SceneSetupBase.LoadMiniGameSettings();
        var tomatoEgg = SceneSetupBase.LoadTomatoEggRecipe();
        var cucumberSalad = SceneSetupBase.LoadCucumberSaladRecipe();
        var baguaConfig = SceneSetupBase.LoadBaguaConfig();
        var albumConfig = SceneSetupBase.LoadAlbumConfig();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 重置觉醒标记，确保首次进入播放过场字幕
        PlayerPrefs.DeleteKey("DeskView_AwakeningShown");

        var interactableMask = 1 << SceneSetupBase.InteractableLayer;

        // --- 背景（电影质感排版：黑底板 + 居中背景图 + 上下留黑） ---
        var bgSprite = SceneSetupBase.LoadSprite(SceneSetupBase.BG_LIVINGROOM_PATH);
        float bgWorldWidth = SceneSetupBase.CreateCinematicBackground(bgSprite);

        // --- 字幕条 ---
        SceneSetupBase.CreateSubtitleBar(bgWorldWidth);

        // --- 玩家 ---
        var player = SceneSetupBase.CreatePlayer(inputSettings, playerSettings, interactableMask);

        // --- 相机（水平边界按背景宽度计算） ---
        float aspect = SceneSetupBase.FIXED_ASPECT;
        float halfView = SceneSetupBase.CAMERA_ORTHO_SIZE * aspect;
        float camMinX = -bgWorldWidth * 0.5f + halfView;
        float camMaxX = bgWorldWidth * 0.5f - halfView;
        if (camMinX > camMaxX) { camMinX = 0; camMaxX = 0; }
        SceneSetupBase.CreateCamera(player.transform, camMinX, camMaxX);

        // --- EventSystem ---
        SceneSetupBase.CreateEventSystem();

        // --- 管理器（首次创建，DontDestroyOnLoad） ---
        SceneSetupBase.CreateGameManager();
        SceneSetupBase.CreateCoordinator(tomatoEgg, cucumberSalad, baguaConfig, albumConfig);
        SceneSetupBase.CreateMiniGameManager(tomatoEgg, cucumberSalad, baguaConfig, albumConfig);

        // --- 书桌（F交互：照片齐→相册；未齐→书桌视角） ---
        SceneSetupBase.CreateDeskViewController(new Vector3(2, SceneSetupBase.PLAYER_Y, 0));

        // --- 门（F→Kitchen） ---
        SceneSetupBase.CreateSceneTransitionDoor("DoorToKitchen", new Vector3(6, SceneSetupBase.PLAYER_Y, 0), SceneNames.Kitchen);

        // --- 出生点 ---
        SceneSetupBase.CreatePlayerSpawn(-3f);

        // --- 保存 ---
        SceneSetupBase.SaveScene(SCENE_PATH);

        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);

        Debug.Log("[LivingRoom Setup] 场景已创建: " + SCENE_PATH);
        Debug.Log("[LivingRoom Setup] 管理器首次创建，DontDestroyOnLoad 带到后续场景");
    }
}
#endif
