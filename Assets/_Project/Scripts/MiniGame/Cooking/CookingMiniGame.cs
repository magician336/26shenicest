using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.MiniGame.Cooking
{
    /// <summary>
    /// 第一个非对称合作小游戏：双方看到不同提示，通过沟通推进同一份 Host 权威状态。
    /// </summary>
    public class CookingMiniGame : global::MiniGameBase
    {
        public const string DefaultId = "tomato_egg";

        public override string GameId => _recipe != null ? _recipe.recipeId : DefaultId;

        private RecipeConfig _recipe;
        private SessionGameplayCoordinator _coordinator;
        private SessionRole _localRole = SessionRole.Host;
        private CookingGameState _state = new CookingGameState();
        private Text _title;
        private Text _prompt;
        private Text _step;
        private Text _role;
        private Button _actionButton;

        public void Configure(RecipeConfig recipe, SessionGameplayCoordinator coordinator, SessionRole role)
        {
            _recipe = recipe;
            _coordinator = coordinator;
            _localRole = role == SessionRole.None ? SessionRole.Host : role;
        }

        public void ApplyState(CookingGameState state)
        {
            _state = state != null ? state.Clone() : new CookingGameState();
            RenderState();
        }

        public override void StartGame()
        {
            ClearPanel();

            var hostSide = _localRole == SessionRole.Host;
            _title = CreateText("Title", _recipe != null ? _recipe.displayName : "做饭", 52,
                new Color(1f, 0.94f, 0.78f), new Vector2(0, 330), new Vector2(960, 90));
            _role = CreateText("Role", hostSide ? "Host 视角" : "Client 视角", 30,
                hostSide ? new Color(0.45f, 0.75f, 1f) : new Color(1f, 0.58f, 0.72f),
                new Vector2(0, 260), new Vector2(520, 60));
            _step = CreateText("Step", string.Empty, 34, Color.white,
                new Vector2(0, 130), new Vector2(960, 70));
            _prompt = CreateText("Prompt", string.Empty, 30, new Color(0.9f, 0.9f, 0.94f),
                new Vector2(0, 20), new Vector2(1100, 130));

            _actionButton = CreateButton("ActionButton", new Vector2(0, -175),
                new Vector2(360, 70), hostSide ? "完成当前步骤" : "告诉 Host 已完成", new Color(0.25f, 0.55f, 0.85f));
            _actionButton.onClick.AddListener(SubmitCurrentStep);

            RenderState();
        }

        public override void UpdateGame()
        {
        }

        public override void EndGame()
        {
            if (_actionButton != null)
            {
                _actionButton.onClick.RemoveListener(SubmitCurrentStep);
            }
        }

        private void SubmitCurrentStep()
        {
            if (_state == null || _state.IsComplete)
            {
                IsComplete = true;
                IsSuccess = true;
                return;
            }

            if (_coordinator != null)
            {
                _coordinator.SubmitCookingStep(GameId, _state.CurrentStep);
            }
            else
            {
                IsComplete = true;
                IsSuccess = true;
            }
        }

        private void RenderState()
        {
            if (_title == null) return;

            var state = _state ?? new CookingGameState();
            var prompt = _localRole == SessionRole.Host ? state.HostPrompt : state.ClientPrompt;

            if (_step != null)
            {
                _step.text = state.IsComplete
                    ? "完成"
                    : $"步骤 {state.StepIndex + 1}: {state.CurrentStep}";
            }

            if (_prompt != null)
            {
                _prompt.text = string.IsNullOrEmpty(prompt) ? "等待对方沟通信息。" : prompt;
            }

            if (_actionButton != null)
            {
                _actionButton.interactable = !state.IsComplete;
            }

            if (state.IsComplete)
            {
                IsComplete = true;
                IsSuccess = true;
            }
        }

        private void ClearPanel()
        {
            if (Panel == null) return;

            for (int i = Panel.childCount - 1; i >= 0; i--)
            {
                Destroy(Panel.GetChild(i).gameObject);
            }
        }

        private Text CreateText(string name, string content, int fontSize, Color color, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(Panel, false);

            var text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = GetDefaultFont();
            text.raycastTarget = false;

            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            return text;
        }

        private Button CreateButton(string name, Vector2 position, Vector2 size, string label, Color color)
        {
            var obj = new GameObject(name, typeof(Image), typeof(Button));
            obj.transform.SetParent(Panel, false);

            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var image = obj.GetComponent<Image>();
            image.color = color;

            var button = obj.GetComponent<Button>();
            button.targetGraphic = image;

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(obj.transform, false);
            var labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var text = labelObj.AddComponent<Text>();
            text.text = label;
            text.font = GetDefaultFont();
            text.fontSize = 30;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            return button;
        }
    }
}
