using UnityEngine;
using UnityEngine.UI;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;

namespace DoNotForgetMe.Dialogue
{
    /// <summary>
    /// 对白序列控制器：通过 CinematicSubtitle 显示字幕 + 同步播放音频。
    /// 订阅 Coordinator 的 StateChanged，在 Dialogue 阶段渲染字幕。
    /// Host 点击全屏透明 Button 推进台词。
    /// </summary>
    public class DialogueController : MonoBehaviour
    {
        private Button _clickOverlay;
        private Canvas _canvas;
        private bool _wasActive;

        private void Awake()
        {
            CreateClickOverlay();
        }

        private void Start()
        {
            var coordinator = SessionGameplayCoordinator.Instance;
            if (coordinator == null)
            {
                Debug.LogWarning("[DialogueController] 缺少 SessionGameplayCoordinator");
                return;
            }
            coordinator.StateChanged += OnStateChanged;
            OnStateChanged(coordinator.State);
        }

        private void OnDestroy()
        {
            var coordinator = SessionGameplayCoordinator.Instance;
            if (coordinator != null) coordinator.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameplaySnapshot snapshot)
        {
            if (snapshot == null) return;

            var isActive = snapshot.phase == GameplayPhase.Dialogue && snapshot.dialogue.IsActive;

            if (isActive)
            {
                var config = FindConfig(snapshot.dialogue.sequenceId);
                if (config == null) return;

                if (!_wasActive)
                    ShowClickOverlay();

                RenderEntry(config, snapshot.dialogue.currentEntryIndex);
                _wasActive = true;
            }
            else if (_wasActive)
            {
                HideAll();
                _wasActive = false;
            }
        }

        private void RenderEntry(DialogueSequence config, int index)
        {
            var entry = config.GetEntry(index);
            if (entry == null) return;

            // 通过 CinematicSubtitle 显示字幕 + 音频
            CinematicSubtitle.Show(entry.text, entry.speaker, entry.audioClip);
        }

        private void ShowClickOverlay()
        {
            var isHost = NetworkSessionManager.Service != null &&
                         NetworkSessionManager.Service.Role == SessionRole.Host;
            _clickOverlay.interactable = isHost;
            _clickOverlay.gameObject.SetActive(true);
        }

        private void HideAll()
        {
            if (_clickOverlay != null) _clickOverlay.gameObject.SetActive(false);
            CinematicSubtitle.Hide();
        }

        private void OnAdvanceClicked()
        {
            SessionGameplayCoordinator.Instance?.Request(
                new GameplayIntent(GameplayIntentType.AdvanceDialogue));
        }

        private DialogueSequence FindConfig(string sequenceId)
        {
            var coordinator = SessionGameplayCoordinator.Instance;
            if (coordinator == null || string.IsNullOrEmpty(sequenceId)) return null;
            return coordinator.GetDialogueConfig(sequenceId);
        }

        private void CreateClickOverlay()
        {
            var go = new GameObject("DialogueClickOverlay");
            DontDestroyOnLoad(go);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();

            var overlayGo = new GameObject("ClickOverlay", typeof(Image), typeof(Button));
            overlayGo.transform.SetParent(go.transform, false);
            var rect = overlayGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = overlayGo.GetComponent<Image>();
            image.color = new Color(0, 0, 0, 0);
            _clickOverlay = overlayGo.GetComponent<Button>();
            _clickOverlay.targetGraphic = image;
            _clickOverlay.onClick.AddListener(OnAdvanceClicked);
            _clickOverlay.gameObject.SetActive(false);
        }
    }
}
