using System.Collections;
using DoNotForgetMe.Audio;
using DoNotForgetMe.Cutscene;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.Interactables
{
    /// <summary>
    /// 场景切换触发器：玩家按 F 交互后加载目标场景。
    /// 用于门、灶台等场景间过渡点。
    /// 可选前置条件：检查 Coordinator 状态（如做饭必须完成才能出门）。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SceneTransitionTrigger : MonoBehaviour, IInteractable
    {
        [Header("目标场景")]
        [Tooltip("按 F 后加载的场景名（使用 SceneNames 常量）")]
        [SerializeField] private string targetScene = "Game";

        [Header("前置条件")]
        [Tooltip("需要收集的照片 ID 列表，全部收集后才允许触发。留空表示无前置。")]
        [SerializeField] private string[] requiredPhotoIds;

        [Tooltip("前置条件不满足时的提示文字")]
        [SerializeField] private string lockedMessage = "还不能去那边。";

        [Header("过渡")]
        [SerializeField] private float fadeDuration = 0.8f;
        [SerializeField] private float holdBlackDuration = 0.3f;

        [Header("角色限制")]
        [Tooltip("只有 Host 端可触发场景切换")]
        [SerializeField] private bool hostOnly = true;

        private bool _isTransitioning;

        public void TriggerInteract()
        {
            if (_isTransitioning) return;

            if (hostOnly && NetworkSessionManager.Service.Role != SessionRole.Host) return;

            var coordinator = SessionGameplayCoordinator.Instance;
            if (coordinator != null)
            {
                if (coordinator.State.phase == GameplayPhase.MiniGame ||
                    coordinator.State.phase == GameplayPhase.MiniGameInterrupted) return;

                if (!ArePrerequisitesMet(coordinator))
                {
                    ShowMessage(lockedMessage);
                    return;
                }
            }

            AudioManager.Play(SfxId.SceneTransition);
            StartCoroutine(TransitionRoutine());
        }

        private bool ArePrerequisitesMet(SessionGameplayCoordinator coordinator)
        {
            if (requiredPhotoIds == null || requiredPhotoIds.Length == 0) return true;
            foreach (var photoId in requiredPhotoIds)
            {
                if (!coordinator.State.collectedPhotoIds.Contains(photoId)) return false;
            }
            return true;
        }

        private IEnumerator TransitionRoutine()
        {
            _isTransitioning = true;

            // 停止玩家移动
            var player = GameManager.Instance?.Player;
            if (player != null)
            {
                var mc = player.GetComponent<MovementController>();
                if (mc != null) mc.Stop();
            }

            // 淡入黑屏
            var overlay = CreateFadeOverlay();
            var blackImage = overlay.GetComponentInChildren<Image>();
            blackImage.canvasRenderer.SetAlpha(0f);
            blackImage.CrossFadeAlpha(1f, fadeDuration, false);

            yield return new WaitForSeconds(fadeDuration + holdBlackDuration);

            // 加载目标场景
            var previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            SceneLoader.Load(targetScene);

            // 同场景守卫跳过了加载 → 淡出黑屏恢复
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == previousScene)
            {
                blackImage.CrossFadeAlpha(0f, fadeDuration, false);
                yield return new WaitForSeconds(fadeDuration);
                Destroy(overlay);
            }

            // 加载后由 GameManager.HandleSceneLoaded 处理 Player spawn
            _isTransitioning = false;
        }

        private GameObject CreateFadeOverlay()
        {
            var canvasGo = new GameObject("SceneTransitionCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("BlackPanel", typeof(Image));
            panel.transform.SetParent(canvasGo.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = Color.black;

            return canvasGo;
        }

        private void ShowMessage(string message)
        {
            var canvasGo = new GameObject("TransitionMessage", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var panel = new GameObject("Panel", typeof(Image));
            panel.transform.SetParent(canvasGo.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800, 120);
            panel.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.06f, 0.95f);

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(panel.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(760, 100);
            var text = textGo.GetComponent<Text>();
            try { text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { text.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            text.fontSize = 28;
            text.color = new Color(0.9f, 0.85f, 0.7f);
            text.alignment = TextAnchor.MiddleCenter;
            text.text = message;

            Destroy(canvasGo, 3f);
        }
    }
}
