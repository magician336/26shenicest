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
/// 生成内容：Canvas（标题/房间码输入/创建房间/加入房间/状态文本/房间码大字展示）
///          + EventSystem + MainMenuController，并保存到 Assets/_Project/Scenes/MainMenu.unity，
/// 且确保 MainMenu 位于 Build Settings 首位（纯联机游戏的入口场景，见 ADR 0001）。
/// </summary>
public static class MainMenuSetup
{
    private const string MENU_PATH = "Tools/3C Setup/Create Main Menu";
    private const string SCENE_PATH = "Assets/_Project/Scenes/MainMenu.unity";

    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    [MenuItem(MENU_PATH)]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var canvas = CreateCanvas();
        CreateEventSystem();

        // --- 背景 ---
        CreateBackground(canvas.transform);

        // --- 标题 ---
        CreateText(canvas.transform, "Title", new Vector2(0, 320),
            new Vector2(900, 100), 64, new Color(0.95f, 0.95f, 0.98f), TextAnchor.MiddleCenter,
            "DO NOT FORGET ME");

        // --- Host 侧房间码大字展示（创建房间后出现） ---
        var roomCodeDisplay = CreateText(canvas.transform, "RoomCodeDisplay", new Vector2(0, 190),
            new Vector2(900, 90), 56, new Color(0.98f, 0.85f, 0.3f), TextAnchor.MiddleCenter,
            string.Empty);

        // --- 房间码输入框 ---
        var roomCodeInput = CreateRoomCodeInput(canvas.transform);

        // --- 按钮 ---
        var createButton = CreateButton(canvas.transform, "CreateButton", new Vector2(-190, -120),
            new Vector2(320, 70), "创建房间", new Color(0.22f, 0.55f, 0.9f));

        var joinButton = CreateButton(canvas.transform, "JoinButton", new Vector2(190, -120),
            new Vector2(320, 70), "加入房间", new Color(0.2f, 0.65f, 0.45f));

        // --- 状态文本 ---
        var statusText = CreateText(canvas.transform, "StatusText", new Vector2(0, -260),
            new Vector2(1200, 80), 26, new Color(0.75f, 0.75f, 0.8f), TextAnchor.MiddleCenter,
            "正在初始化…");

        // --- MainMenuController 接线 ---
        var controllerGo = new GameObject("MainMenuController");
        var controller = controllerGo.AddComponent<MainMenuController>();

        var so = new SerializedObject(controller);
        so.FindProperty("roomCodeInput").objectReferenceValue = roomCodeInput;
        so.FindProperty("createButton").objectReferenceValue = createButton;
        so.FindProperty("joinButton").objectReferenceValue = joinButton;
        so.FindProperty("statusText").objectReferenceValue = statusText;
        so.FindProperty("roomCodeDisplay").objectReferenceValue = roomCodeDisplay;
        so.ApplyModifiedProperties();

        // --- 保存与注册 ---
        System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        EnsureSceneFirstInBuildSettings(SCENE_PATH);

        Debug.Log("[MainMenu Setup] Scene created at " + SCENE_PATH);
        Debug.Log("[MainMenu Setup] Buttons: Create Room (Host) / Join Room (Client). Room code: 4-6 chars.");
        Debug.Log("[MainMenu Setup] Before Fusion SDK import, buttons show a friendly error only.");
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
        return canvas;
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

        go.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.17f);
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

    private static InputField CreateRoomCodeInput(Transform parent)
    {
        var go = new GameObject("RoomCodeInput", typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -20);
        rect.sizeDelta = new Vector2(440, 70);

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
        labelText.fontSize = 32;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.text = label;

        return button;
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
