using System.Collections;
using TMPro;
using UnityEngine;
using DoNotForgetMe.Audio;

/// <summary>
/// 电影质感字幕控制器。底部黑色留白区域显示白色字幕文字。
/// 静态 API: CinematicSubtitle.Show(text) / Show(text, speaker, audioClip, duration) / Hide()
/// 字幕格式：【角色：台词】
///
/// 兼容两种场景结构：
/// - 新结构：SubtitleSystem(本组件) → SubtitleBar(默认隐藏) → SubtitleText
/// - 旧结构：SubtitleBar(本组件，平级) + SubtitleText(平级)
/// 音频通过 AudioManager.PlayClip 播放，不创建本地 AudioSource，避免场景中出现 🔊 图标。
/// </summary>
public class CinematicSubtitle : MonoBehaviour
{
    private static CinematicSubtitle _instance;

    private GameObject _barObj;
    private GameObject _textObj;
    private SpriteRenderer _barRenderer;
    private TextMeshPro _tmp;
    private Coroutine _currentRoutine;
    private AudioClip _pendingClip;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // 尝试新结构：SubtitleBar 是子物体
        _barObj = transform.Find("SubtitleBar")?.gameObject;

        if (_barObj != null)
        {
            // 新结构：SubtitleSystem → SubtitleBar → SubtitleText
            _barRenderer = _barObj.GetComponent<SpriteRenderer>();
            _tmp = _barObj.GetComponentInChildren<TextMeshPro>(true);
            _textObj = _tmp?.gameObject;
        }
        else
        {
            // 旧结构：当前对象就是 SubtitleBar，SubtitleText 是平级
            _barObj = gameObject;
            _barRenderer = GetComponent<SpriteRenderer>();
            _textObj = GameObject.Find("SubtitleText");
            _tmp = _textObj?.GetComponent<TextMeshPro>();
        }

        if (_tmp != null)
            _tmp.text = "";

        // 移除旧代码可能遗留的 AudioSource（旧场景中 CinematicSubtitle 曾在 SubtitleBar 上添加）
        var oldAudio = GetComponent<AudioSource>();
        if (oldAudio != null)
            Destroy(oldAudio);

        // 启动时隐藏字幕条和文字
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        // 新结构：通过 SetActive 控制 SubtitleBar
        if (transform.Find("SubtitleBar") != null)
        {
            if (_barObj != null) _barObj.SetActive(visible);
        }
        // 旧结构：通过 enabled 控制，不影响自身组件
        else
        {
            if (_barRenderer != null) _barRenderer.enabled = visible;
            if (_textObj != null) _textObj.SetActive(visible);
        }
    }

    /// <summary>显示字幕，duration 秒后自动隐藏。duration <= 0 则持续显示。</summary>
    public static void Show(string text, float duration = 0f)
    {
        Show(text, null, null, duration);
    }

    /// <summary>显示带说话者和音频的字幕。格式：【角色：台词】</summary>
    public static void Show(string text, string speaker, AudioClip audioClip, float duration = 0f)
    {
        if (_instance == null || _instance._tmp == null) return;

        _instance.SetVisible(true);

        if (_instance._currentRoutine != null)
            _instance.StopCoroutine(_instance._currentRoutine);

        // 格式化字幕
        _instance._tmp.text = string.IsNullOrEmpty(speaker)
            ? text
            : $"【{speaker}：{text}】";

        // 通过 AudioManager 播放音频（不创建本地 AudioSource）
        AudioManager.StopClip();
        if (audioClip != null)
        {
            _instance._pendingClip = audioClip;
            AudioManager.PlayClip(audioClip);
        }

        _instance.StartFadeIn();

        if (duration > 0)
            _instance._currentRoutine = _instance.StartCoroutine(_instance.HideAfter(duration));
    }

    /// <summary>隐藏字幕并停止音频。</summary>
    public static void Hide()
    {
        if (_instance == null) return;

        if (_instance._currentRoutine != null)
        {
            _instance.StopCoroutine(_instance._currentRoutine);
            _instance._currentRoutine = null;
        }
        AudioManager.StopClip();
        if (_instance._tmp != null)
            _instance._tmp.text = "";

        _instance.SetVisible(false);
    }

    private void StartFadeIn()
    {
        if (_tmp == null) return;
        var c = _tmp.color;
        c.a = 0f;
        _tmp.color = c;
        StartCoroutine(FadeTo(1f, 0.3f));
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (_tmp == null) yield break;
        var startAlpha = _tmp.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var c = _tmp.color;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            _tmp.color = c;
            yield return null;
        }
        var finalC = _tmp.color;
        finalC.a = targetAlpha;
        _tmp.color = finalC;
    }

    private IEnumerator HideAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return FadeTo(0f, 0.3f);
        if (_tmp != null)
            _tmp.text = "";
        AudioManager.StopClip();
        SetVisible(false);
    }
}
