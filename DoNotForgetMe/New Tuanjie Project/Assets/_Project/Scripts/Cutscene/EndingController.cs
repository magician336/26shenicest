using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DoNotForgetMe.Audio;

namespace DoNotForgetMe.Cutscene
{
    /// <summary>
    /// 结尾序列控制器。GAME_3 完成后由 MiniGameManager 在 GameEnded 阶段创建。
    /// 流程：黑屏 → 4幕文本渐入渐出 → 3句黑屏公益字幕 → 停留
    /// </summary>
    public class EndingController : MonoBehaviour
    {
        private Canvas _canvas;
        private Image _blackOverlay;
        private Text _actText;
        private Text _speakerText;

        private static readonly (string speaker, string text)[] Acts =
        {
            // 第三幕：照片→记忆
            ("", "那小岩是谁呢？"),
            // 第四幕：童年捡石
            ("姥爷", "这丫头不像花，风一吹就倒。像块小岩头，有自己的地方。小岩！"),
            ("姥姥", "小岩，回来吃饭啦！"),
            // 第五幕：成长→称呼
            ("旁白", "她的名洪梅。洪，是娘家那一辈的字辈。"),
            ("旁白", "洪梅、嫂子、梅姨、老妈……梅兰菊芳，少一个也有人补上。"),
            // 第六幕：母女牵手
            ("旁白", "再也没有人叫她小岩。"),
            ("旁白", "其实她喜欢小岩这个名字。"),
            ("知夏", "小岩！"),
        };

        private static readonly string[] EpilogueLines =
        {
            "全球有数千万人正在与阿尔茨海默病共同生活。",
            "阿尔茨海默病并不意味着一个人正在消失。",
            "当世界开始改变时，我们不需要把她恢复原样。",
        };

        private void Awake()
        {
            CreateCanvas();
        }

        private void Start()
        {
            StartCoroutine(PlayEndingSequence());
        }

        private IEnumerator PlayEndingSequence()
        {
            // 等待一帧确保 canvas 就绪
            yield return null;

            // 黑屏已由 AlbumMiniGameView 的 BlackScreen 渐入，这里确认黑屏
            _blackOverlay.canvasRenderer.SetAlpha(1f);
            yield return new WaitForSeconds(1f);

            // 4幕内容
            foreach (var act in Acts)
            {
                // 设置说话者
                _speakerText.text = string.IsNullOrEmpty(act.speaker) ? "" : act.speaker;
                _speakerText.color = new Color(0.7f, 0.7f, 0.7f, 0f);

                // 设置正文
                _actText.text = act.text;
                _actText.color = new Color(1f, 1f, 1f, 0f);

                // 淡入
                float fadeInDuration = 1.5f;
                float elapsed = 0f;
                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.deltaTime;
                    float a = elapsed / fadeInDuration;
                    _actText.color = new Color(1f, 1f, 1f, a);
                    if (!string.IsNullOrEmpty(act.speaker))
                        _speakerText.color = new Color(0.7f, 0.7f, 0.7f, a * 0.8f);
                    yield return null;
                }

                // 停留
                yield return new WaitForSeconds(3.5f);

                // 淡出
                float fadeOutDuration = 1f;
                elapsed = 0f;
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    float a = 1f - elapsed / fadeOutDuration;
                    _actText.color = new Color(1f, 1f, 1f, a);
                    _speakerText.color = new Color(0.7f, 0.7f, 0.7f, a * 0.8f);
                    yield return null;
                }

                yield return new WaitForSeconds(0.5f);
            }

            // 黑屏公益字幕
            foreach (var line in EpilogueLines)
            {
                _speakerText.text = "";
                _actText.text = line;
                _actText.color = new Color(0.85f, 0.85f, 0.9f, 0f);

                float fadeInDuration = 2f;
                float elapsed = 0f;
                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.deltaTime;
                    _actText.color = new Color(0.85f, 0.85f, 0.9f, elapsed / fadeInDuration);
                    yield return null;
                }

                yield return new WaitForSeconds(4f);

                float fadeOutDuration = 1.5f;
                elapsed = 0f;
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    _actText.color = new Color(0.85f, 0.85f, 0.9f, 1f - elapsed / fadeOutDuration);
                    yield return null;
                }

                yield return new WaitForSeconds(0.5f);
            }

            // 最终黑屏停留
            _actText.text = "";
            yield return new WaitForSeconds(2f);

            Debug.Log("[Ending] 结尾序列播放完毕");
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

            // 说话者文字
            var speakerGo = new GameObject("Speaker", typeof(Text));
            speakerGo.transform.SetParent(go.transform, false);
            var speakerRect = speakerGo.GetComponent<RectTransform>();
            speakerRect.anchorMin = speakerRect.anchorMax = new Vector2(0.5f, 0.5f);
            speakerRect.pivot = new Vector2(0.5f, 0);
            speakerRect.anchoredPosition = new Vector2(0, 40);
            speakerRect.sizeDelta = new Vector2(800, 40);
            _speakerText = speakerGo.GetComponent<Text>();
            _speakerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _speakerText.fontSize = 28;
            _speakerText.alignment = TextAnchor.MiddleCenter;
            _speakerText.color = new Color(0.7f, 0.7f, 0.7f, 0f);
            _speakerText.raycastTarget = false;

            // 正文
            var textGo = new GameObject("ActText", typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(1400, 200);
            _actText = textGo.GetComponent<Text>();
            _actText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _actText.fontSize = 36;
            _actText.alignment = TextAnchor.MiddleCenter;
            _actText.color = new Color(1f, 1f, 1f, 0f);
            _actText.raycastTarget = false;
        }
    }
}
