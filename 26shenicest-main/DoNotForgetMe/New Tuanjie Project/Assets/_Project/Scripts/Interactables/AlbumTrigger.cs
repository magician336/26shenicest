using System.Collections;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.Interactables
{
    /// <summary>
    /// 全家福相册触发器：放置在游戏开局位置（客厅书桌）。
    /// 检查已收集照片是否齐全，未集齐时显示提示文字，集齐后启动相册小游戏。
    /// </summary>
    public class AlbumTrigger : MonoBehaviour, IInteractable
    {
        [Header("相册配置")]
        [SerializeField] private string albumConfigId = "album_family_portrait";
        [SerializeField] private string[] requiredPhotoIds = { "photo_hongqiang", "photo_hongfang", "bagua_old_family_photo" };

        [Header("提示")]
        [SerializeField] private string lockedMessage = "照片还不齐，等找齐了再拼吧。";
        [SerializeField] private float messageDuration = 3f;

        private SpriteRenderer _spriteRenderer;
        private GameObject _messageOverlay;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void TriggerInteract()
        {
            if (NetworkSessionManager.Service.Role != SessionRole.Host) return;

            var coordinator = SessionGameplayCoordinator.Instance;
            if (coordinator == null) return;

            if (coordinator.State.phase == GameplayPhase.GameEnded) return;
            if (coordinator.State.phase == GameplayPhase.MiniGame) return;

            if (!ArePhotosCollected(coordinator))
            {
                ShowMessage(lockedMessage);
                return;
            }

            coordinator.Request(new GameplayIntent(GameplayIntentType.StartAlbumMiniGame, albumConfigId));
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

        private void ShowMessage(string message)
        {
            if (_messageOverlay != null) Destroy(_messageOverlay);

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("MessageCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 200;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            _messageOverlay = new GameObject("AlbumLockedMessage", typeof(Image));
            _messageOverlay.transform.SetParent(canvas.transform, false);
            var rect = _messageOverlay.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800, 120);
            _messageOverlay.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.06f, 0.95f);

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(_messageOverlay.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(760, 100);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 28;
            text.color = new Color(0.9f, 0.85f, 0.7f);
            text.alignment = TextAnchor.MiddleCenter;
            text.text = message;

            StartCoroutine(HideMessageAfterDelay());
        }

        private IEnumerator HideMessageAfterDelay()
        {
            yield return new WaitForSeconds(messageDuration);
            if (_messageOverlay != null)
            {
                Destroy(_messageOverlay);
                _messageOverlay = null;
            }
        }
    }
}
