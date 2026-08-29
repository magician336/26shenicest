using System.Collections;
using DoNotForgetMe.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace DoNotForgetMe.Cutscene
{
    /// <summary>
    /// 开场过场控制器（Scene0）。
    /// 流程：黑屏 → 逐句内心独白文字 → AIGC 视频 → 时光回溯动画 → 加载客厅场景。
    /// 挂载在 Intro 场景的 GameObject 上，Awake 自动启动。
    /// </summary>
    public class IntroCutsceneController : MonoBehaviour
    {
        [Header("文字节奏")]
        [SerializeField] private float textFadeInDuration = 1.5f;
        [SerializeField] private float textHoldDuration = 3f;
        [SerializeField] private float textFadeOutDuration = 1f;
        [SerializeField] private float gapBetweenLines = 0.5f;

        [Header("视频")]
        [Tooltip("AIGC 视频（留空则跳过）")]
        [SerializeField] private VideoClip aigcVideoClip;
        [SerializeField] private float videoMaxDuration = 15f;

        [Header("时光回溯")]
        [SerializeField] private float reversalDuration = 2f;
        [SerializeField] private Color reversalFlashColor = new Color(0.9f, 0.85f, 0.7f, 0f);

        [Header("转场")]
        [SerializeField] private float finalFadeDuration = 1.5f;

        [Header("内心独白文本")]
        [TextArea(2, 4)]
        [SerializeField] private string[] monologueLines = new string[]
        {
            "丧事刚过。屋里人来人往，电话不停。",
            "有人喊二姐，有人喊梅姨，有人喊嫂子，有人喊刘师傅。",
            "母亲有时应，有时没反应。",
            "她攥着一张残缺、模糊的旧全家福。",
            "「小岩……」",
            "「小岩去哪儿了……」",
            "没关系。我们一起找。"
        };

        private Canvas _canvas;
        private Image _blackOverlay;
        private Text _monologueText;

        private void Awake()
        {
            CreateCanvas();
            StartCoroutine(PlaySequence());
        }

        private void CreateCanvas()
        {
            var go = new GameObject("IntroCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 300;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();

            // 黑屏背景
            _blackOverlay = CreateFullImage("BlackOverlay", Color.black);
            _blackOverlay.rectTransform.SetParent(go.transform, false);

            // 文字
            var textGo = new GameObject("MonologueText");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(1400, 300);

            _monologueText = textGo.AddComponent<Text>();
            _monologueText.font = GetDefaultFont();
            _monologueText.fontSize = 42;
            _monologueText.color = new Color(0.9f, 0.85f, 0.7f, 1f);
            _monologueText.alignment = TextAnchor.MiddleCenter;
            _monologueText.supportRichText = true;
            _monologueText.text = "";
        }

        private IEnumerator PlaySequence()
        {
            // --- 逐句内心独白 ---
            foreach (var line in monologueLines)
            {
                _monologueText.text = line;

                // 渐入
                yield return FadeText(0f, 1f, textFadeInDuration);

                // 停留
                yield return new WaitForSeconds(textHoldDuration);

                // 渐出
                yield return FadeText(1f, 0f, textFadeOutDuration);

                yield return new WaitForSeconds(gapBetweenLines);
            }

            _monologueText.text = "";

            // --- AIGC 视频 ---
            if (aigcVideoClip != null)
            {
                yield return PlayVideo();
            }

            // --- 时光回溯闪白 ---
            yield return PlayReversalFlash();

            // --- 淡入黑屏并转场 ---
            _blackOverlay.color = new Color(0, 0, 0, 0);
            _blackOverlay.canvasRenderer.SetAlpha(0f);
            _blackOverlay.CrossFadeAlpha(1f, finalFadeDuration, false);
            yield return new WaitForSeconds(finalFadeDuration + 0.5f);

            // 加载客厅场景
            SceneLoader.Load(SceneNames.LivingRoom);
        }

        private IEnumerator FadeText(float from, float to, float duration)
        {
            _monologueText.canvasRenderer.SetAlpha(from);
            _monologueText.CrossFadeAlpha(to, duration, false);
            yield return new WaitForSeconds(duration);
        }

        private IEnumerator PlayVideo()
        {
            var videoGo = new GameObject("AIGCVideoPlayer");
            videoGo.transform.SetParent(_canvas.transform, false);

            var player = videoGo.AddComponent<VideoPlayer>();
            player.clip = aigcVideoClip;
            player.renderMode = VideoRenderMode.CameraNearPlane;
            player.targetCamera = Camera.main;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            player.Play();

            var elapsed = 0f;
            while (player.isPlaying && elapsed < videoMaxDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            player.Stop();
            Destroy(videoGo);
        }

        private IEnumerator PlayReversalFlash()
        {
            var flashGo = new GameObject("ReversalFlash", typeof(Image));
            flashGo.transform.SetParent(_canvas.transform, false);
            var rect = flashGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var flash = flashGo.GetComponent<Image>();
            flash.color = reversalFlashColor;
            flash.canvasRenderer.SetAlpha(0f);
            flash.CrossFadeAlpha(1f, reversalDuration * 0.4f, false);

            yield return new WaitForSeconds(reversalDuration * 0.4f);

            flash.CrossFadeAlpha(0f, reversalDuration * 0.6f, false);
            yield return new WaitForSeconds(reversalDuration * 0.6f);

            Destroy(flashGo);
        }

        private Image CreateFullImage(string name, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Font GetDefaultFont()
        {
            try
            {
                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                return Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }
    }
}
