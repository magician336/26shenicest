using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace DoNotForgetMe.Cutscene
{
    /// <summary>
    /// 结尾序列控制器。GAME_3 完成后由 MiniGameManager 在 GameEnded 阶段创建。
    /// 流程：黑屏字幕 → 片尾视频 → 结束
    /// </summary>
    public class EndingController : MonoBehaviour
    {
        [Header("黑屏字幕")]
        [SerializeField] private string[] epilogueLines =
        {
            "全球有数千万人正在与阿尔茨海默病共同生活。",
            "阿尔茨海默病并不意味着一个人正在消失。",
            "当世界开始改变时，我们不需要把她恢复原样。",
        };

        [Header("节奏")]
        [SerializeField] private float epilogueFadeIn = 2f;
        [SerializeField] private float epilogueHold = 4f;
        [SerializeField] private float epilogueFadeOut = 1.5f;

        [Header("片尾视频")]
        [Tooltip("片尾视频（留空则跳过）")]
        [SerializeField] private VideoClip outroVideoClip;
        [SerializeField] private float outroVideoMaxDuration = 120f;

        public void SetOutroVideo(VideoClip clip) => outroVideoClip = clip;

        private Canvas _canvas;
        private Image _blackOverlay;
        private Text _actText;

        private void Awake()
        {
#if UNITY_EDITOR
            if (outroVideoClip == null)
            {
                outroVideoClip = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(
                    "Assets/_Project/Video/片尾.mp4");
            }
#endif
            CreateCanvas();
        }

        private void Start()
        {
            StartCoroutine(PlayEndingSequence());
        }

        private IEnumerator PlayEndingSequence()
        {
            yield return null;

            _blackOverlay.canvasRenderer.SetAlpha(1f);
            yield return new WaitForSeconds(1f);

            // 黑屏字幕
            foreach (var line in epilogueLines)
            {
                _actText.text = line;
                _actText.color = new Color(0.85f, 0.85f, 0.9f, 0f);

                yield return FadeText(0f, 1f, epilogueFadeIn);
                yield return new WaitForSeconds(epilogueHold);
                yield return FadeText(1f, 0f, epilogueFadeOut);

                yield return new WaitForSeconds(0.5f);
            }

            _actText.text = "";

            // 片尾视频
            yield return PlayOutroVideo();

            Debug.Log("[Ending] 结尾序列播放完毕");
        }

        private IEnumerator PlayOutroVideo()
        {
            if (outroVideoClip == null) yield break;

            // 隐藏文字，确保视频画面干净
            _actText.color = new Color(1, 1, 1, 0);

            var rt = new RenderTexture(1920, 1080, 0);

            var videoGo = new GameObject("OutroVideo", typeof(RawImage));
            videoGo.transform.SetParent(_canvas.transform, false);
            var videoRect = videoGo.GetComponent<RectTransform>();
            videoRect.anchorMin = Vector2.zero;
            videoRect.anchorMax = Vector2.one;
            videoRect.offsetMin = videoRect.offsetMax = Vector2.zero;
            videoRect.SetAsLastSibling();
            var rawImage = videoGo.GetComponent<RawImage>();
            rawImage.texture = rt;
            rawImage.raycastTarget = false;

            var player = videoGo.AddComponent<VideoPlayer>();
            player.clip = outroVideoClip;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = rt;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            player.Prepare();

            // 等待视频准备完成
            var prepTimeout = 0f;
            while (!player.isPrepared && prepTimeout < 10f)
            {
                prepTimeout += Time.deltaTime;
                yield return null;
            }

            if (!player.isPrepared)
            {
                Debug.LogWarning("[Ending] 片尾视频准备超时，跳过");
                Destroy(videoGo);
                rt.Release();
                yield break;
            }

            player.Play();

            var elapsed = 0f;
            while (elapsed < outroVideoMaxDuration)
            {
                elapsed += Time.deltaTime;
                if (!player.isPlaying) break;
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                {
                    break;
                }
                yield return null;
            }

            player.Stop();
            Destroy(videoGo);
            rt.Release();
        }

        private IEnumerator FadeText(float from, float to, float duration)
        {
            _actText.canvasRenderer.SetAlpha(from);
            _actText.CrossFadeAlpha(to, duration, false);
            yield return new WaitForSeconds(duration);
        }

        private void CreateCanvas()
        {
            var go = new GameObject("EndingCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 300;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();

            // 纯黑底
            var blackGo = new GameObject("BlackOverlay", typeof(Image));
            blackGo.transform.SetParent(go.transform, false);
            var blackRect = blackGo.GetComponent<RectTransform>();
            blackRect.anchorMin = Vector2.zero;
            blackRect.anchorMax = Vector2.one;
            blackRect.offsetMin = blackRect.offsetMax = Vector2.zero;
            _blackOverlay = blackGo.GetComponent<Image>();
            _blackOverlay.color = Color.black;
            _blackOverlay.raycastTarget = true;

            // 正文
            var textGo = new GameObject("ActText", typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(1400, 200);
            _actText = textGo.GetComponent<Text>();
            _actText.font = GetDefaultFont();
            _actText.fontSize = 36;
            _actText.alignment = TextAnchor.MiddleCenter;
            _actText.color = new Color(1f, 1f, 1f, 0f);
            _actText.raycastTarget = false;
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
