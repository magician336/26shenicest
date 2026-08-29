using UnityEngine;
using UnityEngine.UI;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;
using DoNotForgetMe.Audio;
using DoNotForgetMe.Core;
using DoNotForgetMe.UI;

namespace DoNotForgetMe.Cutscene
{
    /// <summary>
    /// Courtyard 子页面控制器。
    /// 不显示角色、不显示物品栏，只有全屏背景 + "进入偷听"按钮。
    /// 八卦完成后 → 照片收集 → 对白自动播放 → 自动转场回 LivingRoom，无需手动退出。
    /// </summary>
    public class CourtyardEavesdropView : MonoBehaviour
    {
        [SerializeField] private Sprite backgroundSprite;

        private GameObject _canvas;
        private GameObject _enterBtn;

        private void Awake()
        {
            CreateUI();
        }

        private void Start()
        {
            var coord = SessionGameplayCoordinator.Instance;
            if (coord == null)
            {
                coord = FindObjectOfType<SessionGameplayCoordinator>();
            }
            if (coord != null)
            {
                coord.StateChanged += OnStateChanged;
                OnStateChanged(coord.State);
            }
            else
            {
                Debug.LogWarning("[CourtyardEavesdrop] SessionGameplayCoordinator not found — " +
                    "open LivingRoom scene first or enter via Kitchen door transition.");
                if (_enterBtn != null) _enterBtn.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            var coord = SessionGameplayCoordinator.Instance;
            if (coord != null) coord.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameplaySnapshot snapshot)
        {
            if (snapshot == null) return;

            // 小游戏进行中或对白播放中 → 隐藏按钮
            if (snapshot.phase == GameplayPhase.MiniGame ||
                snapshot.phase == GameplayPhase.Dialogue)
            {
                if (_enterBtn != null) _enterBtn.SetActive(false);
            }
            else if (snapshot.phase == GameplayPhase.Exploration)
            {
                if (_enterBtn != null) _enterBtn.SetActive(true);
            }
        }

        private void CreateUI()
        {
            // Canvas
            _canvas = new GameObject("CourtyardEavesdropCanvas", typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = _canvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = _canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // 不创建 UI 背景 — 背景由场景的世界空间 SpriteRenderer (Background) 提供
            // 只创建按钮层

            // "进入偷听"按钮
            _enterBtn = CreateButton("EnterEavesdropBtn", "进入偷听", new Vector2(0, -400),
                new Color(0.5f, 0.35f, 0.15f, 0.95f));
            _enterBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                var coord = SessionGameplayCoordinator.Instance;
                if (coord == null) return;
                if (coord.State.phase == GameplayPhase.MiniGame) return;
                AudioManager.Play(SfxId.UiButtonClick);
                coord.Request(new GameplayIntent(GameplayIntentType.StartBaguaMiniGame, "bagua_old_photo"));
            });
        }

        private GameObject CreateButton(string name, string label, Vector2 position, Color color)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(_canvas.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(360, 100);
            go.GetComponent<Image>().color = color;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();

            var labelGo = new GameObject("Label", typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var text = labelGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 38;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
            text.raycastTarget = false;

            go.AddComponent<ButtonHoverEffect>();

            return go;
        }
    }
}
