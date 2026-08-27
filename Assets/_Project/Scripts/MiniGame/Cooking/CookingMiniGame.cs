using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoNotForgetMe.MiniGame.Cooking
{
    /// <summary>做饭小游戏的本地私有视图；不在此类中作出任何通关判定。</summary>
    public class CookingMiniGame : MiniGameBase
    {
        private RecipeConfig _recipe;
        private SessionGameplayCoordinator _coordinator;
        private GameObject _content;

        public override string GameId => _recipe != null ? _recipe.RecipeId : string.Empty;

        public void Setup(RecipeConfig recipe, SessionGameplayCoordinator coordinator)
        {
            _recipe = recipe;
            _coordinator = coordinator;
        }

        public override void StartGame()
        {
            _content = new GameObject("PrivateView");
            _content.transform.SetParent(Panel, false);
        }

        public void Render(CookingGameState state)
        {
            if (_content == null || _recipe == null) return;
            ClearContent();

            var role = NetworkSessionManager.Service.Role;
            if (role == SessionRole.Client)
            {
                RenderMotherView(state);
            }
            else if (role == SessionRole.Host)
            {
                RenderDaughterView(state);
            }
            else
            {
                CreateText("Waiting", "等待联机角色…", 40, Color.white, Vector2.zero);
            }
        }

        public override void UpdateGame()
        {
        }

        public override void EndGame()
        {
        }

        private void RenderMotherView(CookingGameState state)
        {
            CreateText("Role", "母亲端 · " + _recipe.MotherTaskText, 42, Color.white, new Vector2(0, 410));
            if (state.completed)
            {
                CreateText("Complete", "你们一起完成了这道菜。", 48, new Color(0.9f, 0.7f, 0.4f), Vector2.zero);
                return;
            }

            if (state.step == CookingStep.MotherSelectIngredients)
            {
                CreateText("Instruction", "选出需要的食材", 30, new Color(0.85f, 0.8f, 0.7f), new Vector2(0, 300));
                var items = new[] { "tomato", "egg", "cucumber", "ribs" };
                for (var i = 0; i < items.Length; i++)
                {
                    var item = items[i];
                    var chosen = state.selectedIngredients.Contains(item);
                    CreateButton(item, DisplayName(item), new Vector2(-480 + i * 320, -120), chosen,
                        () => _coordinator.Request(new GameplayIntent(GameplayIntentType.SelectIngredient, _recipe.RecipeId, item)));
                }
            }
            else if (state.step == CookingStep.MotherDropIngredients)
            {
                CreateText("Instruction", "把选好的食材拖进锅里", 30, new Color(0.85f, 0.8f, 0.7f), new Vector2(0, 300));
                var zone = CreateDropZone("Wok", "锅", new Vector2(0, -40));
                for (var i = 0; i < state.selectedIngredients.Count; i++)
                {
                    var item = state.selectedIngredients[i];
                    if (state.droppedIngredients.Contains(item)) continue;
                    CreateDraggable(item, DisplayName(item), new Vector2(-400 + i * 300, -360), zone,
                        () => _coordinator.Request(new GameplayIntent(GameplayIntentType.DropIngredient, _recipe.RecipeId, item)));
                }
            }
            else
            {
                CreateText("Waiting", "菜已经做好了。请等待女儿端调味。", 34, Color.white, Vector2.zero);
            }

            CreateButton("Help", "帮我看看", new Vector2(730, -400), false,
                () => _coordinator.Request(new GameplayIntent(GameplayIntentType.RequestHint, _recipe.RecipeId)));
            RenderCurrentHint(state);
        }

        private void RenderDaughterView(CookingGameState state)
        {
            CreateText("Role", "女儿端 · " + _recipe.DaughterTaskText, 42, Color.white, new Vector2(0, 410));
            if (state.completed)
            {
                CreateText("Complete", "你们一起完成了这道菜。", 48, new Color(0.9f, 0.7f, 0.4f), Vector2.zero);
                return;
            }

            if (!state.daughterUnlocked)
            {
                CreateText("Waiting", "等待母亲把食材放入锅中…", 34, Color.white, Vector2.zero);
            }
            else
            {
                CreateText("RecipeNote", "菜谱改痕：洪强爱吃甜的，放点糖。", 30,
                    new Color(0.9f, 0.82f, 0.65f), new Vector2(0, 260));
                CreateText("Instruction", "拖入正确的调料", 30, new Color(0.85f, 0.8f, 0.7f), new Vector2(0, 190));
                var zone = CreateDropZone("Dish", "番茄炒蛋", new Vector2(0, -40));
                CreateDraggable("sugar", "糖", new Vector2(-220, -360), zone,
                    () => _coordinator.Request(new GameplayIntent(GameplayIntentType.SelectSeasoning, _recipe.RecipeId, "sugar")));
                CreateDraggable("salt", "盐", new Vector2(220, -360), zone,
                    () => _coordinator.Request(new GameplayIntent(GameplayIntentType.SelectSeasoning, _recipe.RecipeId, "salt")));
            }

            if (state.hintRequested)
            {
                CreateButton("ShowHint", "发送下一层提示", new Vector2(0, -460), false,
                    () => _coordinator.Request(new GameplayIntent(GameplayIntentType.ShowHint, _recipe.RecipeId)));
            }
            else
            {
                CreateButton("Interrupt", "暂时离开", new Vector2(730, -400), false,
                    () => _coordinator.Request(new GameplayIntent(GameplayIntentType.InterruptMiniGame, _recipe.RecipeId)));
            }
            RenderCurrentHint(state);
        }

        private void RenderCurrentHint(CookingGameState state)
        {
            if (state.hintLevel <= 0 || state.hintLevel > _recipe.HintTexts.Length) return;
            CreateText("Hint", _recipe.HintTexts[state.hintLevel - 1], 28, new Color(0.8f, 0.6f, 0.3f), new Vector2(0, 70));
        }

        private void ClearContent()
        {
            for (var i = _content.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_content.transform.GetChild(i).gameObject);
            }
        }

        private Text CreateText(string name, string text, int fontSize, Color color, Vector2 position)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(_content.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(1500, 100);
            var label = go.GetComponent<Text>();
            label.font = GetDefaultFont();
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = text;
            return label;
        }

        private void CreateButton(string name, string label, Vector2 position, bool selected, System.Action onClick)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(_content.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(250, 130);
            var image = go.GetComponent<Image>();
            image.color = selected ? new Color(0.85f, 0.72f, 0.45f) : new Color(0.38f, 0.31f, 0.23f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());
            CreateChildLabel(go.transform, label);
        }

        private RectTransform CreateDropZone(string name, string label, Vector2 position)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(_content.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(420, 280);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.35f, 0.25f, 0.16f, 0.8f);
            image.raycastTarget = false;
            CreateChildLabel(go.transform, label);
            return rect;
        }

        private void CreateDraggable(string id, string label, Vector2 position, RectTransform zone, System.Action onCorrectDrop)
        {
            var go = new GameObject(id, typeof(Image), typeof(DraggableItem));
            go.transform.SetParent(_content.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(220, 120);
            go.GetComponent<Image>().color = new Color(0.7f, 0.58f, 0.4f);
            CreateChildLabel(go.transform, label);
            var draggable = go.GetComponent<DraggableItem>();
            draggable.OnEndDragEvent += (_, __) =>
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(zone, rect.position, null))
                {
                    onCorrectDrop?.Invoke();
                }
                else
                {
                    draggable.ReturnToOrigin();
                }
            };
        }

        private void CreateChildLabel(Transform parent, string label)
        {
            var textGo = new GameObject("Label", typeof(Text));
            textGo.transform.SetParent(parent, false);
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = label;
        }

        private static string DisplayName(string itemId)
        {
            switch (itemId)
            {
                case "tomato": return "番茄";
                case "egg": return "鸡蛋";
                case "cucumber": return "黄瓜";
                case "ribs": return "排骨";
                case "sugar": return "糖";
                case "salt": return "盐";
                default: return itemId;
            }
        }
    }
}
