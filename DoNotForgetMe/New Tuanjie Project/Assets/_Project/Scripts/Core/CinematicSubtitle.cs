using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 电影质感字幕控制器。底部黑色留白区域显示白色字幕文字。
/// 静态 API: CinematicSubtitle.Show(text) / Show(text, speaker, audioClip, duration) / Hide()
/// 字幕格式：【角色：台词】
/// </summary>
public class CinematicSubtitle : MonoBehaviour
{
    private static CinematicSubtitle _instance;

    private TextMeshPro _tmp;
    private AudioSource _audioSource;
    private Coroutine _currentRoutine;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;

        var textObj = GameObject.Find("SubtitleText");
        if (textObj != null)
            _tmp = textObj.GetComponent<TextMeshPro>();
        if (_tmp == null)
            _tmp = GetComponentInChildren<TextMeshPro>();
        if (_tmp != null)
            _tmp.text = "";
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

        if (_instance._currentRoutine != null)
            _instance.StopCoroutine(_instance._currentRoutine);

        // 格式化字幕
        _instance._tmp.text = string.IsNullOrEmpty(speaker)
            ? text
            : $"【{speaker}：{text}】";

        // 播放音频
        if (_instance._audioSource != null)
        {
            _instance._audioSource.Stop();
            if (audioClip != null)
            {
                _instance._audioSource.clip = audioClip;
                _instance._audioSource.Play();
            }
        }

        _instance.StartFadeIn();

        if (duration > 0)
            _instance._currentRoutine = _instance.StartCoroutine(_instance.HideAfter(duration));
    }

    /// <summary>隐藏字幕并停止音频。</summary>
    public static void Hide()
    {
        if (_instance == null || _instance._tmp == null) return;

        if (_instance._currentRoutine != null)
        {
            _instance.StopCoroutine(_instance._currentRoutine);
            _instance._currentRoutine = null;
        }
        if (_instance._audioSource != null && _instance._audioSource.isPlaying)
            _instance._audioSource.Stop();
        _instance._tmp.text = "";
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
        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.Stop();
    }
}
