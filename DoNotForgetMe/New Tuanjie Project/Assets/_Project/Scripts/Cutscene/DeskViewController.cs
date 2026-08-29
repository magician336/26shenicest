using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;

namespace DoNotForgetMe.Cutscene
{
    /// <summary>
    /// Scene1 第一人称书桌视角控制器。
    /// 时光回溯结束后，主角在客厅书桌前以第一人称醒来。
    ///
    /// 流程：
    ///   1. 黑屏 → 2句觉醒字幕（渐入渐出）
    ///   2. 淡入书桌画面（暖色老屋氛围）
    ///   3. 按 X / Escape / 右上角按钮退出 → 恢复探索移动
    ///   4. 退出后 F 键再次交互：照片齐→相册小游戏；未齐→重看桌面
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DeskViewController : MonoBehaviour, IInteractable
    {
        public static bool IsActive { get; private set; }

        [Header("退出键")]
        [SerializeField] private KeyCode exitKey = KeyCode.X;

        [Header("相册配置")]
        [SerializeField] private string albumConfigId = "album_family_portrait";
        [SerializeField] private string[] requiredPhotoIds = { "photo_hongqiang", "photo_hongfang", "bagua_old_family_photo" };

        [Header("觉醒字幕")]
        [SerializeField] private float subtitleFadeIn = 1.5f;
        [SerializeField] private float subtitleHold = 2.5f;
        [SerializeField] private float subtitleFadeOut = 1f;
        [SerializeField] private float subtitleGap = 0.5f;
        [SerializeField] private float deskFadeInDuration = 2f;

        [Header("桌面背景图")]
        [SerializeField] private Sprite deskBackgroundSprite;

        private static readonly string[] AwakeningLines =
        {
            "这是哪里……这双手，不是我的。",
            "他们叫我洪梅……难道，这是妈妈小时候住过的地方？"
        };

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private GameObject _deskContent;
        private GameObject _subtitleOverlay;
        private Text _subtitleText;
        private bool _hasShownOnce;
        private bool _isPlayingAwakening;
        private const string AwakeningShownKey = "DeskView_AwakeningShown";

        private void Awake()
        {
            if (PlayerPrefs.GetInt(AwakeningShownKey, 0) == 1)
            {
                // 已播过觉醒序列，不自动弹出书桌，等玩家按 F
                _hasShownOnce = true;
                CreateCanvas();
                _canvas.gameObject.SetActive(false);
            }
            else
            {
                // 首次进入，播放觉醒序列
                ShowDeskView();
                PlayerPrefs.SetInt(AwakeningShownKey, 1);
            }
        }

        private void Update()
        {
            if (!IsActive || _isPlayingAwakening) return;

            if (Input.GetKeyDown(exitKey) || Input.GetKeyDown(KeyCode.Escape))
            {
                CloseDeskView();
            }
        }

        // ==============================
        // IInteractable
        // ==============================

        public void TriggerInteract()
        {
            Debug.Log($"[DeskView] TriggerInteract called. IsActive={IsActive}, Role={NetworkSessionManager.Service.Role}");

            if (NetworkSessionManager.Service.Role != SessionRole.Host)
            {
                Debug.Log("[DeskView] TriggerInteract: not Host, skipping");
                return;
            }
            if (IsActive)
            {
                Debug.Log("[DeskView] TriggerInteract: already active, skipping");
                return;
            }

            var coordinator = SessionGameplayCoordinator.Instance;
            if (coordinator == null)
            {
                Debug.Log("[DeskView] TriggerInteract: coordinator is null");
                return;
            }

            Debug.Log($"[DeskView] phase={coordinator.State.phase}, collectedPhotos={string.Join(",", coordinator.State.collectedPhotoIds)}");

            if (coordinator.State.phase == GameplayPhase.MiniGame ||
                coordinator.State.phase == GameplayPhase.GameEnded) return;

            if (ArePhotosCollected(coordinator))
            {
                Debug.Log("[DeskView] Photos collected → starting album mini-game");
                coordinator.Request(new GameplayIntent(GameplayIntentType.StartAlbumMiniGame, albumConfigId));
            }
            else
            {
                Debug.Log("[DeskView] Photos not collected → ShowDeskView");
                ShowDeskView();
            }
        }

        private bool ArePhotosCollected(SessionGameplayCoordinator coordinator)
        {
            if (requiredPhotoIds == null || requiredPhotoIds.Length == 0) return true;
            foreach (var photoId in requiredPhotoIds)
            {
                if (!coordinator.State.collectedPhotoIds.Contains(photoId)) return false;
            }
            return true;
        }

        // ==============================
        // 展示 / 关闭
        // ==============================

        private void ShowDeskView()
        {
            IsActive = true;
            _hasShownOnce = true;

            if (_canvas == null)
            {
                CreateCanvas();
                StartCoroutine(PlayAwakeningSequence());
            }
            else
            {
                _canvas.gameObject.SetActive(true);
                _canvasGroup.alpha = 1f;
                _deskContent.SetActive(true);
                _subtitleOverlay.SetActive(false);
            }
        }

        private void CloseDeskView()
        {
            Debug.Log("[DeskView] CloseDeskView called");
            IsActive = false;

            if (_canvasGroup != null)
            {
                StartCoroutine(FadeOutAndHide());
            }
        }

        // ==============================
        // 觉醒序列
        // ==============================

        private IEnumerator PlayAwakeningSequence()
        {
            _isPlayingAwakening = true;

            // 阶段1：黑屏 + 觉醒字幕
            _deskContent.SetActive(false);
            _subtitleOverlay.SetActive(true);
            // subtitleOverlay 是独立的 Image+Text，不归 canvasGroup 管
            // canvasGroup 控制 deskContent 的淡入

            foreach (var line in AwakeningLines)
            {
                _subtitleText.text = line;
                _subtitleText.canvasRenderer.SetAlpha(0f);
                _subtitleText.CrossFadeAlpha(1f, subtitleFadeIn, false);
                yield return new WaitForSeconds(subtitleFadeIn);
                yield return new WaitForSeconds(subtitleHold);
                _subtitleText.CrossFadeAlpha(0f, subtitleFadeOut, false);
                yield return new WaitForSeconds(subtitleFadeOut);
                yield return new WaitForSeconds(subtitleGap);
            }

            // 阶段2：淡出黑屏，淡入书桌画面
            var blackBg = _subtitleOverlay.GetComponent<Image>();
            blackBg.canvasRenderer.SetAlpha(1f);
            blackBg.CrossFadeAlpha(0f, deskFadeInDuration, false);

            _deskContent.SetActive(true);
            _canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < deskFadeInDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(elapsed / deskFadeInDuration);
                yield return null;
            }
            _canvasGroup.alpha = 1f;

            // 隐藏字幕层
            _subtitleOverlay.SetActive(false);
            _isPlayingAwakening = false;
        }

        private IEnumerator FadeOutAndHide()
        {
            float elapsed = 0f;
            float duration = 0.6f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = 1f - (elapsed / duration);
                yield return null;
            }
            _canvas.gameObject.SetActive(false);
            _canvasGroup.alpha = 1f;
            Debug.Log("[DeskView] FadeOutAndHide complete, canvas hidden");
        }

        // ==============================
        // UI 构建
        // ==============================

        private void CreateCanvas()
        {
            var go = new GameObject("DeskViewCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 150;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            _canvasGroup = go.AddComponent<CanvasGroup>();

            // --- 字幕层（觉醒时全屏黑底 + 居中文字）---
            CreateSubtitleOverlay(go.transform);

            // --- 书桌内容层 ---
            CreateDeskContent(go.transform);
        }

        // ==============================
        // 字幕层
        // ==============================

        private void CreateSubtitleOverlay(Transform parent)
        {
            _subtitleOverlay = new GameObject("SubtitleOverlay", typeof(Image));
            _subtitleOverlay.transform.SetParent(parent, false);
            var rect = _subtitleOverlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _subtitleOverlay.GetComponent<Image>().color = Color.black;

            var textGo = new GameObject("SubtitleText", typeof(Text));
            textGo.transform.SetParent(_subtitleOverlay.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(1400, 200);

            _subtitleText = textGo.GetComponent<Text>();
            _subtitleText.font = GetDefaultFont();
            _subtitleText.fontSize = 40;
            _subtitleText.color = new Color(0.9f, 0.85f, 0.7f, 1f);
            _subtitleText.alignment = TextAnchor.MiddleCenter;
            _subtitleText.supportRichText = true;
            _subtitleText.text = "";
            _subtitleText.raycastTarget = false;

            _subtitleOverlay.SetActive(false);
        }

        // ==============================
        // 书桌内容层
        // ==============================

        private void CreateDeskContent(Transform parent)
        {
            // _deskContent 自身是 RectTransform 铺满全屏
            _deskContent = new GameObject("DeskContent", typeof(RectTransform));
            _deskContent.transform.SetParent(parent, false);
            var dcRect = _deskContent.GetComponent<RectTransform>();
            dcRect.anchorMin = Vector2.zero;
            dcRect.anchorMax = Vector2.one;
            dcRect.offsetMin = dcRect.offsetMax = Vector2.zero;

            // --- 全屏桌面背景图（替代之前的彩色方块模拟） ---
            var deskBg = new GameObject("DeskSurface", typeof(Image));
            deskBg.transform.SetParent(_deskContent.transform, false);
            var bgRect = deskBg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            var bgImage = deskBg.GetComponent<Image>();
            if (deskBackgroundSprite != null)
            {
                bgImage.sprite = deskBackgroundSprite;
                bgImage.color = Color.white;
                bgImage.preserveAspect = true;
            }
            else
            {
                bgImage.color = new Color(0.30f, 0.21f, 0.13f, 1f);
            }
            bgImage.raycastTarget = true; // 拦截点击

            // --- 顶部提示 ---
            CreateTextLabel(_deskContent.transform, "Hint", "按 X 离开书桌", new Vector2(0, 460),
                new Vector2(600, 50), 26, new Color(0.85f, 0.8f, 0.65f, 0.6f));

            // --- 右上角关闭按钮 ---
            CreateCloseButton(_deskContent.transform);

            _deskContent.SetActive(false);
        }

        private void CreateWoodGrain(Transform parent)
        {
            for (var i = 0; i < 7; i++)
            {
                var line = new GameObject($"Grain{i}", typeof(Image));
                line.transform.SetParent(parent, false);
                var rect = line.GetComponent<RectTransform>();
                var y = 0.15f + i * 0.1f;
                rect.anchorMin = new Vector2(0, y);
                rect.anchorMax = new Vector2(1, y);
                rect.offsetMin = new Vector2(0, -1);
                rect.offsetMax = new Vector2(0, 1);
                var alpha = 0.15f + (i % 3) * 0.05f;
                line.GetComponent<Image>().color = new Color(0.22f, 0.15f, 0.08f, alpha);
                line.GetComponent<Image>().raycastTarget = false;
            }
        }

        private void CreateHandsArea(Transform parent)
        {
            // 左手
            var leftHand = new GameObject("LeftHand", typeof(Image));
            leftHand.transform.SetParent(parent, false);
            var lhRect = leftHand.GetComponent<RectTransform>();
            lhRect.anchorMin = new Vector2(0, 0);
            lhRect.anchorMax = new Vector2(0.4f, 0.18f);
            lhRect.offsetMin = Vector2.zero;
            lhRect.offsetMax = Vector2.zero;
            leftHand.GetComponent<Image>().color = new Color(0.52f, 0.4f, 0.32f, 0.5f);
            leftHand.GetComponent<Image>().raycastTarget = false;

            // 右手
            var rightHand = new GameObject("RightHand", typeof(Image));
            rightHand.transform.SetParent(parent, false);
            var rhRect = rightHand.GetComponent<RectTransform>();
            rhRect.anchorMin = new Vector2(0.6f, 0);
            rhRect.anchorMax = new Vector2(1f, 0.18f);
            rhRect.offsetMin = Vector2.zero;
            rhRect.offsetMax = Vector2.zero;
            rightHand.GetComponent<Image>().color = new Color(0.52f, 0.4f, 0.32f, 0.5f);
            rightHand.GetComponent<Image>().raycastTarget = false;
        }

        private void CreateDeskItem(string name, string label, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(_deskContent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            // 物品标签
            var labelGo = new GameObject(name + "_Label", typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0);
            labelRect.pivot = new Vector2(0.5f, 1);
            labelRect.anchoredPosition = new Vector2(0, -6);
            labelRect.sizeDelta = new Vector2(size.x + 40, 28);
            var text = labelGo.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = 18;
            text.color = new Color(0.85f, 0.8f, 0.65f, 0.7f);
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
            text.raycastTarget = false;
        }

        private void CreateTextLabel(Transform parent, string name, string content,
            Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = content;
            text.raycastTarget = false;
        }

        private void CreateCloseButton(Transform parent)
        {
            var go = new GameObject("CloseButton", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-20, -20);
            rect.sizeDelta = new Vector2(56, 56);
            go.GetComponent<Image>().color = new Color(0.5f, 0.25f, 0.25f, 0.85f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick.AddListener(CloseDeskView);

            var labelGo = new GameObject("X", typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var text = labelGo.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = 32;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = "X";
            text.raycastTarget = false;
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
