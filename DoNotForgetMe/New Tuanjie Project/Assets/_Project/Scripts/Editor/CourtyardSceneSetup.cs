#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DoNotForgetMe.Core;
using DoNotForgetMe.Cutscene;

/// <summary>
/// 庭院子页面场景生成器。
/// 菜单：Tools/3C Setup/Create Courtyard Scene
/// 不创建 Player、不创建交互触发器。
/// 只有 Camera + 背景 + 字幕条 + CourtyardEavesdropView（全屏 UI 子页面）。
/// </summary>
public static class CourtyardSceneSetup
{
    private const string MENU_PATH = "Tools/3C Setup/Create Courtyard Scene";
    private const string SCENE_PATH = "Assets/_Project/Scenes/Courtyard.unity";

    [MenuItem(MENU_PATH)]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- 背景（电影质感排版） ---
        var bgSprite = SceneSetupBase.LoadSprite(SceneSetupBase.BG_COURTYARD_PATH);
        float bgWorldWidth = SceneSetupBase.CreateCinematicBackground(bgSprite);

        // --- 字幕条 ---
        SceneSetupBase.CreateSubtitleBar(bgWorldWidth);

        // --- 相机（固定中心，无跟随目标，无 constrainX） ---
        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0, SceneSetupBase.CAMERA_FIXED_Y, -10);
        var cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = SceneSetupBase.CAMERA_ORTHO_SIZE;
        cam.backgroundColor = Color.black;
        camObj.AddComponent<AudioListener>();

        // --- EventSystem ---
        SceneSetupBase.CreateEventSystem();

        // --- 庭院子页面控制器 ---
        var viewObj = new GameObject("CourtyardEavesdropView");
        var view = viewObj.AddComponent<DoNotForgetMe.Cutscene.CourtyardEavesdropView>();
        var viewSo = new SerializedObject(view);
        viewSo.FindProperty("backgroundSprite").objectReferenceValue = bgSprite;
        viewSo.ApplyModifiedProperties();

        // --- 保存 ---
        SceneSetupBase.SaveScene(SCENE_PATH);

        Selection.activeGameObject = viewObj;
        EditorGUIUtility.PingObject(viewObj);

        Debug.Log("[Courtyard Setup] 子页面场景已创建: " + SCENE_PATH);
        Debug.Log("[Courtyard Setup] 无 Player/交互触发器，只有进入偷听 + 退出偷听按钮");
    }
}
#endif
