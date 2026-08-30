using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;

namespace DoNotForgetMe.Cutscene
{
    /// <summary>
    /// Scene1 第一人称书桌视角控制器。
    /// 觉醒内心独白已移至 DLG_EnterMemory DialogueSequence，本组件只负责书桌视角的展示与关闭。
    ///
    /// 流程：
    ///   1. F 键交互 → 显示书桌画面
    ///   2. 按 X / Escape / 右上角按钮退出 → 恢复探索移动
    ///   3. 照片齐→相册小游戏；未齐→重看桌面
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

        [Header("桌面背景图")]
        [SerializeField] private Sprite deskBackgroundSprite;

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private GameObject _deskContent;

        private void Awake()
        {
            CreateCanvas();
            _canvas.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsActive) return;

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
            _canvas.gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _deskContent.SetActive(true);
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

            // --- 书桌内容层 ---
            CreateDeskContent(go.transform);
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

            // --- 全屏桌面背景图 ---
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
