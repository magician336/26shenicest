#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主菜单场景生成器（镜像 Scene3CSetup 的代码驱动搭建模式）。
/// 菜单：Tools/3C Setup/Create Main Menu
/// 生成内容：Canvas（背景图/房间码输入/创建会话/加入会话/开始游戏/取消连接/状态文本/房间码大字展示）
///          + EventSystem + MainMenuController，并保存到 Assets/_Project/Scenes/MainMenu.unity，
/// 且确保 MainMenu 位于 Build Settings 首位（纯联机游戏的入口场景，见 ADR 0001）。
/// </summary>
public static class MainMenuSetup
{
    private const string MENU_PATH = "Tools/3C Setup/Create Main Menu";
    private const string SCENE_PATH = "Assets/_Project/Scenes/MainMenu.unity";

    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    private const string BG_PATH = "Assets/_Project/Art/mainmenu.jpg";
    private const string BTN_CREATE_PATH = "Assets/_Project/Art/创建会话.png";
    private const string BTN_JOIN_PATH = "Assets/_Project/Art/加入会话.png";
    private const string BTN_START_PATH = "Assets/_Project/Art/开始游戏.png";

    [MenuItem(MENU_PATH)]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var canvas = CreateCanvas();
        CreateEventSystem();
        CreateCamera();

        // --- 背景 ---
        CreateBackground(canvas.transform);

        // --- Host 侧房间码大字展示（创建会话后出现） ---
        var roomCodeDisplay = CreateText(canvas.transform, "RoomCodeDisplay", new Vector2(0, 180),
            new Vector2(900, 90), 56, new Color(0.98f, 0.85f, 0.3f), TextAnchor.MiddleCenter,
            string.Empty);

        // --- 右下角竖列：输入框 + 三个按钮（从上到下） ---
        // 锚点 (1,0) = 右下角；x=-200 使元素中心距右边缘 200px
        var roomCodeInput = CreateRoomCodeInput(canvas.transform, new Vector2(-200, 500));
        roomCodeInput.gameObject.SetActive(false);

        var createButton = CreateImageButton(canvas.transform, "CreateButton",
            new Vector2(-200, 350), new Vector2(360, 130), BTN_CREATE_PATH);

        var joinButton = CreateImageButton(canvas.transform, "JoinButton",
            new Vector2(-200, 195), new Vector2(360, 130), BTN_JOIN_PATH);

        var continueButton = CreateImageButton(canvas.transform, "ContinueButton",
            new Vector2(-200, 50), new Vector2(360, 130), BTN_START_PATH);

        // --- 单人模式按钮（左下角） ---
        var singlePlayerButton = CreateButton(canvas.transform, "SinglePlayerButton",
            new Vector2(200, 100), new Vector2(360, 130), "单人模式", new Color(0.2f, 0.5f, 0.3f));
        var spRect = singlePlayerButton.GetComponent<RectTransform>();
        spRect.anchorMin = spRect.anchorMax = new Vector2(0f, 0f);

        // --- 取消连接按钮（连接中显示，置于竖列下方） ---
        var cancelButton = CreateButton(canvas.transform, "CancelButton", new Vector2(-200, -50),
            new Vector2(200, 45), "取消连接", new Color(0.55f, 0.3f, 0.3f));
        var cancelRect = cancelButton.GetComponent<RectTransform>();
        cancelRect.anchorMin = cancelRect.anchorMax = new Vector2(1f, 0f);

        // --- 状态文本 ---
        var statusText = CreateText(canvas.transform, "StatusText", new Vector2(0, -80),
            new Vector2(1200, 50), 24, new Color(0.75f, 0.75f, 0.8f), TextAnchor.MiddleCenter,
            "正在初始化…");

        // --- MainMenuController 接线 ---
        var controllerGo = new GameObject("MainMenuController");
        var controller = controllerGo.AddComponent<MainMenuController>();

        var so = new SerializedObject(controller);
        so.FindProperty("roomCodeInput").objectReferenceValue = roomCodeInput;
        so.FindProperty("createButton").objectReferenceValue = createButton;
        so.FindProperty("joinButton").objectReferenceValue = joinButton;
        so.FindProperty("cancelButton").objectReferenceValue = cancelButton;
        so.FindProperty("continueButton").objectReferenceValue = continueButton;
        so.FindProperty("singlePlayerButton").objectReferenceValue = singlePlayerButton;
        so.FindProperty("statusText").objectReferenceValue = statusText;
        so.FindProperty("roomCodeDisplay").objectReferenceValue = roomCodeDisplay;
        so.ApplyModifiedProperties();

        // --- 保存与注册 ---
        System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        EnsureSceneFirstInBuildSettings(SCENE_PATH);

        Debug.Log("[MainMenu Setup] Scene created at " + SCENE_PATH);
    }

    // ---------------------------------------------------------------
    // UI 构建辅助
    // ---------------------------------------------------------------

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        // Canvas 的 RectTransform localScale 默认可能为 (0,0,0)，导致整个 UI 不可见
        go.GetComponent<RectTransform>().localScale = Vector3.one;

        return canvas;
    }

    private static void CreateCamera()
    {
        var go = new GameObject("MainCamera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0, 0, -10);

        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.12f, 0.17f);
        cam.orthographic = false;
        cam.fieldOfView = 60;

        go.AddComponent<AudioListener>();
    }

    private static void CreateEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private static void CreateBackground(Transform parent)
    {
        var go = new GameObject("Background", typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        var image = go.GetComponent<Image>();
        var bgSprite = LoadSprite(BG_PATH);
        if (bgSprite != null)
        {
            image.sprite = bgSprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
        }
        else
        {
            image.color = new Color(0.12f, 0.12f, 0.17f);
        }
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchoredPos,
        Vector2 size, int fontSize, Color color, TextAnchor alignment, string content)
    {
        var go = new GameObject(name, typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var text = go.GetComponent<Text>();
        text.font = GetBuiltinFont();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.text = content;

        return text;
    }

    private static InputField CreateRoomCodeInput(Transform parent, Vector2 anchoredPos)
    {
        var go = new GameObject("RoomCodeInput", typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(360, 70);

        go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.24f);

        // 输入本体 Text
        var textGo = new GameObject("Text", typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(16, 4);
        textRect.offsetMax = new Vector2(-16, -4);
        var textComp = textGo.GetComponent<Text>();
        textComp.font = GetBuiltinFont();
        textComp.fontSize = 34;
        textComp.color = Color.white;
        textComp.alignment = TextAnchor.MiddleCenter;

        // 占位 Text
        var phGo = new GameObject("Placeholder", typeof(Text));
        phGo.transform.SetParent(go.transform, false);
        var phRect = phGo.GetComponent<RectTransform>();
        phRect.anchorMin = new Vector2(0, 0);
        phRect.anchorMax = new Vector2(1, 1);
        phRect.offsetMin = new Vector2(16, 4);
        phRect.offsetMax = new Vector2(-16, -4);
        var phComp = phGo.GetComponent<Text>();
        phComp.font = GetBuiltinFont();
        phComp.fontSize = 30;
        phComp.fontStyle = FontStyle.Italic;
        phComp.color = new Color(0.5f, 0.5f, 0.55f);
        phComp.alignment = TextAnchor.MiddleCenter;
        phComp.text = "输入房间码";

        var input = go.AddComponent<InputField>();
        input.textComponent = textComp;
        input.placeholder = phComp;
        input.contentType = InputField.ContentType.Alphanumeric;
        input.characterLimit = 6;
        input.caretColor = Color.white;

        return input;
    }

    /// <summary>创建图片按钮（无文字子节点，图片自带文字）。</summary>
    private static Button CreateImageButton(Transform parent, string name, Vector2 anchoredPos,
        Vector2 size, string spritePath)
    {
        var go = new GameObject(name, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var image = go.GetComponent<Image>();
        var sprite = LoadSprite(spritePath);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }
        else
        {
            image.color = new Color(0.22f, 0.55f, 0.9f);
        }

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        return button;
    }

    private static Button CreateButton(Transform parent, string name, Vector2 anchoredPos,
        Vector2 size, string label, Color color)
    {
        var go = new GameObject(name, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var image = go.GetComponent<Image>();
        image.color = color;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        var labelGo = new GameObject("Label", typeof(Text));
        labelGo.transform.SetParent(go.transform, false);
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var labelText = labelGo.GetComponent<Text>();
        labelText.font = GetBuiltinFont();
        labelText.fontSize = 28;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.text = label;

        return button;
    }

    /// <summary>加载图片为 Sprite，自动修正导入设置。</summary>
    private static Sprite LoadSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Font _builtinFont;

    private static Font GetBuiltinFont()
    {
        if (_builtinFont != null) return _builtinFont;

        try
        {
            _builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch (Exception)
        {
            _builtinFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return _builtinFont;
    }

    // ---------------------------------------------------------------
    // Build Settings
    // ---------------------------------------------------------------

    /// <summary>确保场景位于 Build Settings 首位（入口场景）。</summary>
    private static void EnsureSceneFirstInBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        // 移除已有同名条目（可能 enabled 状态不同）
        scenes.RemoveAll(s => s.path == scenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
