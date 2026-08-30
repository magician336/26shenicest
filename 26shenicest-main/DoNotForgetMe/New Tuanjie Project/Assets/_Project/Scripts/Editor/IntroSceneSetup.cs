#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// 开场过场场景生成器。
/// 菜单：Tools/3C Setup/Create Intro Scene
/// 生成内容：空场景 + Main Camera + IntroCutsceneController GameObject，
/// 保存到 Assets/_Project/Scenes/Intro.unity 并加入 Build Settings。
/// </summary>
public static class IntroSceneSetup
{
    private const string MENU_PATH = "Tools/3C Setup/Create Intro Scene";
    private const string SCENE_PATH = "Assets/_Project/Scenes/Intro.unity";

    [MenuItem(MENU_PATH)]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Camera (VideoPlayer.targetCamera 需要) ---
        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0, 0, -10);
        var cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5;
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;

        // --- IntroCutsceneController ---
        var controllerObj = new GameObject("IntroCutsceneController");
        var controller = controllerObj.AddComponent<DoNotForgetMe.Cutscene.IntroCutsceneController>();

        // 自动接入片头视频
        var openingClip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/_Project/Video/片头.mp4");
        if (openingClip != null)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("openingVideoClip").objectReferenceValue = openingClip;
            so.ApplyModifiedProperties();
        }

        // --- 保存 ---
        System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");
        EditorSceneManager.SaveScene(scene, SCENE_PATH);

        AddSceneToBuildSettings(SCENE_PATH);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = controllerObj;
        EditorGUIUtility.PingObject(controllerObj);

        Debug.Log("[Intro Setup] Scene created at " + SCENE_PATH);
        Debug.Log("[Intro Setup] IntroCutsceneController.Awake 自动启动过场序列");
        Debug.Log("[Intro Setup] 播完后自动加载 LivingRoom (当前=Game)");
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);

        // 已存在则跳过
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return;
        }

        // 插入到 MainMenu 之后、Game 之前（如有）
        int insertIndex = 1; // MainMenu=0 之后
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path.Contains("MainMenu"))
            {
                insertIndex = i + 1;
                break;
            }
        }

        scenes.Insert(insertIndex, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
