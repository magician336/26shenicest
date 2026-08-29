using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DoNotForgetMe.Network;

/// <summary>
/// 房门偷听触发器：Host 靠近时距离渐变音频（音量 + 低通滤波），
/// 按 F 后播放清晰对话与过渡文字，随后发起八卦小游戏。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DoorListeningTrigger : MonoBehaviour, IInteractable
{
    [Header("八卦小游戏配置")]
    [SerializeField] private string baguaConfigId = "bagua_old_photo";

    [Header("音频配置")]
    [SerializeField] private AudioSource dialogueAudioSource;
    [SerializeField] private float maxHearingDistance = 8f;
    [SerializeField] private float minCutoffFrequency = 200f;
    [SerializeField] private float maxCutoffFrequency = 22000f;

    [Header("过渡")]
    [SerializeField] private float transitionDelay = 3f;
    [SerializeField] private string transitionText = "你听见了他们的故事……";

    private AudioLowPassFilter _lowPassFilter;
    private bool _isTransitioning;

    private void Awake()
    {
        if (dialogueAudioSource == null)
            dialogueAudioSource = GetComponentInChildren<AudioSource>();
        if (dialogueAudioSource != null)
        {
            _lowPassFilter = dialogueAudioSource.GetComponent<AudioLowPassFilter>();
            if (_lowPassFilter == null)
                _lowPassFilter = dialogueAudioSource.gameObject.AddComponent<AudioLowPassFilter>();
        }
    }

    private void Update()
    {
        if (_isTransitioning) return;

        var player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (player == null) return;

        var distance = Vector2.Distance(transform.position, player.transform.position);
        var normalized = Mathf.Clamp01(1f - distance / maxHearingDistance);

        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.volume = normalized * 0.6f;
            if (!dialogueAudioSource.isPlaying && normalized > 0.05f)
                dialogueAudioSource.Play();
        }
        if (_lowPassFilter != null)
        {
            _lowPassFilter.cutoffFrequency = Mathf.Lerp(minCutoffFrequency, maxCutoffFrequency, normalized);
        }
    }

    public void TriggerInteract()
    {
        if (NetworkSessionManager.Service.Role != SessionRole.Host) return;
        if (_isTransitioning) return;
        if (MiniGameManager.Instance == null)
        {
            Debug.LogWarning("[DoorListeningTrigger] 场景中未找到 MiniGameManager");
            return;
        }

        StartCoroutine(TransitionSequence());
    }

    private IEnumerator TransitionSequence()
    {
        _isTransitioning = true;

        // 停止玩家移动
        var player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (player != null)
        {
            var mc = player.GetComponent<MovementController>();
            if (mc != null) mc.Stop();
        }

        // 播放清晰对话
        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.volume = 1f;
            if (_lowPassFilter != null)
                _lowPassFilter.cutoffFrequency = maxCutoffFrequency;
            if (!dialogueAudioSource.isPlaying)
                dialogueAudioSource.Play();
        }

        // 显示过渡文字
        var overlay = CreateTransitionOverlay();

        // 等待过渡
        yield return new WaitForSeconds(transitionDelay);

        // 销毁过渡文字
        if (overlay != null) Destroy(overlay);

        // 发起八卦小游戏
        MiniGameManager.Instance.StartBaguaMiniGame(baguaConfigId);

        _isTransitioning = false;
    }

    private GameObject CreateTransitionOverlay()
    {
        var canvasObj = new GameObject("DoorTransitionCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.04f, 0.03f, 0.95f);
        var bgRect = panel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(panel.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 42;
        text.color = new Color(0.9f, 0.85f, 0.7f);
        text.alignment = TextAnchor.MiddleCenter;
        text.text = transitionText;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(1400, 200);

        return canvasObj;
    }
}
