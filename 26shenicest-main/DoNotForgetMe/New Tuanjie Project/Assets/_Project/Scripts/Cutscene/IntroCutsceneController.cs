using System.Collections;
using DoNotForgetMe.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace DoNotForgetMe.Cutscene
{
    /// <summary>
    /// 开场过场控制器（Intro 场景）。
    /// 流程：片头视频 → 淡入黑屏 → 转场到 LivingRoom（书桌子页面）。
    /// 挂载在 Intro 场景的 GameObject 上，Awake 自动启动。
    /// </summary>
    public class IntroCutsceneController : MonoBehaviour
    {
        [Header("片头视频")]
        [Tooltip("片头视频（留空则在编辑器模式下自动加载 Assets/_Project/Video/片头.mp4）")]
        [SerializeField] private VideoClip openingVideoClip;
        [SerializeField] private float openingVideoMaxDuration = 120f;

        [Header("转场")]
        [SerializeField] private float finalFadeDuration = 1.5f;

        private Canvas _canvas;
        private Image _blackOverlay;

        private void Awake()
        {
#if UNITY_EDITOR
            if (openingVideoClip == null)
            {
                openingVideoClip = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(
                    "Assets/_Project/Video/片头.mp4");
            }
#endif
            CreateCanvas();
            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            // 片头视频
            yield return PlayOpeningVideo();

            // 视频结束 → 淡入黑屏 → 转场到 LivingRoom（书桌子页面）
            _blackOverlay.canvasRenderer.SetAlpha(0f);
            _blackOverlay.CrossFadeAlpha(1f, finalFadeDuration, false);
            yield return new WaitForSeconds(finalFadeDuration + 0.5f);

            SceneLoader.Load(SceneNames.LivingRoom);
        }

        private IEnumerator PlayOpeningVideo()
        {
            if (openingVideoClip == null)
            {
                Debug.LogWarning("[Intro] openingVideoClip 为空，跳过片头视频，直接进入 LivingRoom");
                yield break;
            }

            var rt = new RenderTexture(1920, 1080, 0);

            var videoGo = new GameObject("OpeningVideo", typeof(RawImage));
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
            player.clip = openingVideoClip;
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
                Debug.LogWarning("[Intro] 视频准备超时，跳过片头视频");
                Destroy(videoGo);
                rt.Release();
                yield break;
            }

            player.Play();

            var elapsed = 0f;
            while (elapsed < openingVideoMaxDuration)
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
