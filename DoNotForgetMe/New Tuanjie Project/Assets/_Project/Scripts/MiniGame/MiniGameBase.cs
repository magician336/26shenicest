using UnityEngine;
using UnityEngine.UI;

public abstract class MiniGameBase : MonoBehaviour
{
    public abstract string GameId { get; }

    protected MiniGameSettings Settings { get; private set; }
    protected RectTransform Panel { get; private set; }

    public bool IsComplete { get; protected set; }
    public bool IsSuccess { get; protected set; }

    public void Initialize(MiniGameSettings settings, RectTransform panel)
    {
        Settings = settings;
        Panel = panel;
    }

    public abstract void StartGame();
    public abstract void UpdateGame();
    public abstract void EndGame();

    // --- UI 工具方法 ---

    protected Text CreateText(string name, string content, int fontSize, Color color,
        TextAnchor anchor = TextAnchor.MiddleCenter, Vector2 position = default)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(Panel, false);

        var text = obj.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = anchor;
        text.font = GetDefaultFont();
        text.raycastTarget = false;

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(800, 100);

        return text;
    }

    protected Image CreateImage(string name, Color color, Vector2 size, Vector2 position = default)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(Panel, false);

        var img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        return img;
    }

    /// <summary>
    /// 创建可点击的 UI 元素（带 Image + ClickableItem）
    /// </summary>
    protected ClickableItem CreateClickable(string name, Color color, Vector2 size, Vector2 position,
        System.Action onClick = null)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(Panel, false);

        var img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        var clickable = obj.AddComponent<ClickableItem>();
        if (onClick != null)
        {
            clickable.OnClickEvent += _ => onClick();
        }

        return clickable;
    }

    /// <summary>
    /// 创建可拖拽的 UI 元素（带 Image + DraggableItem）
    /// </summary>
    protected DraggableItem CreateDraggable(string name, Color color, Vector2 size, Vector2 position)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(Panel, false);

        var img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        return obj.AddComponent<DraggableItem>();
    }

    /// <summary>
    /// 创建目标区域（仅用于碰撞检测的透明 Image）
    /// </summary>
    protected RectTransform CreateDropZone(string name, Vector2 size, Vector2 position)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(Panel, false);

        var img = obj.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.08f);
        img.raycastTarget = false;

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        return rt;
    }

    /// <summary>
    /// 检测拖拽元素是否在目标区域内
    /// </summary>
    protected bool IsInside(RectTransform dragged, RectTransform zone)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            zone, dragged.position, null);
    }

    private static Font _defaultFont;
    protected static Font GetDefaultFont()
    {
        if (_defaultFont == null)
        {
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_defaultFont == null)
                _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return _defaultFont;
    }
}
