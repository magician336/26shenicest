using System.Collections;
using System.Collections.Generic;
using DoNotForgetMe.Audio;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;
using DoNotForgetMe.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoNotForgetMe.MiniGame.Cooking
{
    /// <summary>
    /// 做饭小游戏控制器。使用 prefab + CookingView 进行视图绑定，
    /// Render(state) 原地更新 UI 元素，不再销毁重建。
    /// 不在此类中作出任何通关判定——所有判定走 SessionGameplayCoordinator。
    /// </summary>
    public class CookingMiniGame : MiniGameBase
    {
        private RecipeConfig _recipe;
        private SessionGameplayCoordinator _coordinator;
        private GameObject _content;
        private CookingView _view;
        private bool _completionFxPlayed;
        private string _lastRenderKey;
        private Coroutine _ambientSteam;
        private Coroutine _collectGlowRoutine;

        // 拖拽回调是否已初始化
        private bool _dragCallbacksWired;
        private bool _collectButtonWired;
        private bool _autoCollectTriggered;

        // 食材/调料 ID 与槽位索引的映射
        private readonly Dictionary<string, int> _ingredientSlotMap = new();
        private readonly Dictionary<string, int> _seasoningSlotMap = new();

        public override string GameId => _recipe != null ? _recipe.RecipeId : string.Empty;

        public void Setup(RecipeConfig recipe, SessionGameplayCoordinator coordinator)
        {
            _recipe = recipe;
            _coordinator = coordinator;
        }

        public override void StartGame()
        {
            _completionFxPlayed = false;
            _lastRenderKey = null;
            _dragCallbacksWired = false;
            _collectButtonWired = false;
            _autoCollectTriggered = false;
            _ingredientSlotMap.Clear();
            _seasoningSlotMap.Clear();

            // 加载并实例化 prefab
            var prefabName = _recipe.ViewPrefabName;
            var prefab = Resources.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                Debug.LogError($"[CookingMiniGame] 未找到 {prefabName} prefab。" +
                               "请先运行 Tools > MiniGame > Export Cooking Prefab");
                return;
            }
            _content = Object.Instantiate(prefab, Panel, false);
            _content.name = "PrivateView";
            _view = _content.GetComponent<CookingView>();

            if (_view == null)
            {
                Debug.LogError("[CookingMiniGame] Prefab 上缺少 CookingView 组件");
                return;
            }

            // 初始隐藏所有元素
            SetAllInactive();

            // 加载等待提示图
            var waitingSprite = LoadWaitingSprite();
            if (_view.MotherWaitingImage != null)
            {
                if (waitingSprite != null)
                    _view.MotherWaitingImage.sprite = waitingSprite;
                _view.MotherWaitingImage.color = Color.white;
            }
            if (_view.DaughterWaitingImage != null)
            {
                if (waitingSprite != null)
                    _view.DaughterWaitingImage.sprite = waitingSprite;
                _view.DaughterWaitingImage.color = Color.white;
            }

            // 构建食材/调料 ID → 槽位索引映射
            BuildSlotMaps();

            // 初始化拖拽回调
            WireDragCallbacks();
        }

        public void Render(CookingGameState state)
        {
            if (_view == null || _recipe == null) return;

            var role = NetworkSessionManager.Service.Role;
            var photoCollected = IsRewardPhotoCollected();
            var renderKey = $"{role}_{state.step}_{state.completed}_{state.daughterUnlocked}_{photoCollected}_{state.selectedIngredients.Count}_{state.droppedIngredients.Count}";
            if (renderKey == _lastRenderKey) return;
            _lastRenderKey = renderKey;

            // 更新背景
            UpdateBackground(role, state);

            // 按角色切换面板
            _view.MotherPanel?.SetActive(role == SessionRole.Client);
            _view.DaughterPanel?.SetActive(role == SessionRole.Host);
            _view.WaitingText?.gameObject.SetActive(role == SessionRole.None);

            if (role == SessionRole.Client)
            {
                RenderMotherView(state);
            }
            else if (role == SessionRole.Host)
            {
                RenderDaughterView(state);
            }
        }

        public override void UpdateGame()
        {
        }

        public override void EndGame()
        {
            StopAmbientSteam();
            StopCollectGlow();
            if (_content != null)
            {
                Destroy(_content);
                _content = null;
                _view = null;
            }
            _lastRenderKey = null;
            _dragCallbacksWired = false;
            _collectButtonWired = false;
        }

        // ==============================
        // 母亲端渲染
        // ==============================

        private void RenderMotherView(CookingGameState state)
        {
            var v = _view;

            // 角色文字
            if (v.MotherRoleText != null)
            {
                v.MotherRoleText.text = "母亲端 · " + _recipe.MotherTaskText;
                v.MotherRoleText.gameObject.SetActive(true);
            }

            if (state.completed)
            {
                StopAmbientSteam();
                ShowMotherElements(false, false, false);
                if (v.MotherCompleteText != null)
                {
                    v.MotherCompleteText.text = "你们一起完成了这道菜。";
                    v.MotherCompleteText.gameObject.SetActive(true);
                }
                if (v.MotherDishPhoto != null)
                {
                    v.MotherDishPhoto.sprite = _recipe.DishPhotoSprite;
                    v.MotherDishPhoto.gameObject.SetActive(true);
                }
                return;
            }

            // 完成文字和菜品照片默认隐藏
            v.MotherCompleteText?.gameObject.SetActive(false);
            v.MotherDishPhoto?.gameObject.SetActive(false);

            if (state.step == CookingStep.MotherSelectIngredients)
            {
                // 食材选择阶段
                if (v.MotherInstructionText != null)
                {
                    v.MotherInstructionText.text = "把需要的食材拖进" + _recipe.ContainerDisplayName + "里";
                    v.MotherInstructionText.gameObject.SetActive(true);
                }

                // 持续蒸汽
                StopAmbientSteam();
                _ambientSteam = StartCoroutine(AmbientSteam(new Vector2(0, 60)));

                // 锅容器
                v.MotherContainerZone?.gameObject.SetActive(true);

                // 已放入食材名
                if (v.MotherDroppedNamesText != null)
                {
                    if (state.droppedIngredients.Count > 0)
                    {
                        var names = new System.Text.StringBuilder();
                        for (var d = 0; d < state.droppedIngredients.Count; d++)
                        {
                            if (d > 0) names.Append("、");
                            names.Append(DisplayName(state.droppedIngredients[d]));
                        }
                        v.MotherDroppedNamesText.text = names.ToString();
                        v.MotherDroppedNamesText.gameObject.SetActive(true);
                    }
                    else
                    {
                        v.MotherDroppedNamesText.gameObject.SetActive(false);
                    }
                }

                // 更新食材槽位可见性
                UpdateIngredientSlots(state);

                // 隐藏等待和菜谱改痕
                v.MotherWaitingText?.gameObject.SetActive(false);
                v.MotherRecipeNoteText?.gameObject.SetActive(false);
                v.MotherHintImage?.gameObject.SetActive(false);
            }
            else
            {
                // 等待女儿端调味
                StopAmbientSteam();
                ShowMotherElements(false, true, false);

                v.MotherWaitingImage?.gameObject.SetActive(true);
                if (v.MotherRecipeNoteText != null)
                {
                    v.MotherRecipeNoteText.text = "菜谱改痕：" + _recipe.RecipeNote;
                    v.MotherRecipeNoteText.gameObject.SetActive(true);
                }
                if (v.MotherHintImage != null)
                {
                    v.MotherHintImage.sprite = _recipe.MotherCompleteHint;
                    v.MotherHintImage.gameObject.SetActive(_recipe.MotherCompleteHint != null);
                }
            }
        }

        // ==============================
        // 女儿端渲染
        // ==============================

        private void RenderDaughterView(CookingGameState state)
        {
            var v = _view;

            // 角色文字
            if (v.DaughterRoleText != null)
            {
                v.DaughterRoleText.text = "女儿端 · " + _recipe.DaughterTaskText;
                v.DaughterRoleText.gameObject.SetActive(true);
            }

            if (state.completed)
            {
                // 完成态：隐藏选择阶段元素
                v.DaughterWaitingText?.gameObject.SetActive(false);
                v.DaughterInstructionText?.gameObject.SetActive(false);
                v.DaughterDishZone?.gameObject.SetActive(false);
                v.DaughterDishPhoto?.gameObject.SetActive(false);
                HideAllSeasoningSlots();

                if (v.DaughterCompleteText != null)
                {
                    v.DaughterCompleteText.text = "你们一起完成了这道菜。";
                    v.DaughterCompleteText.gameObject.SetActive(true);
                }

                // 照片奖励
                var photoId = GetRewardPhotoId();
                bool photoCollected = photoId != null && _coordinator.State.collectedPhotoIds.Contains(photoId);

                if (photoId != null && !photoCollected)
                {
                    ShowRewardPhoto(photoId);
                    if (!_autoCollectTriggered)
                    {
                        _autoCollectTriggered = true;
                        StartCoroutine(AutoCollectPhoto(photoId));
                    }
                }
                else if (photoCollected)
                {
                    HideRewardPhoto();
                    if (v.CollectedText != null)
                    {
                        v.CollectedText.gameObject.SetActive(true);
                    }
                }

                return;
            }

            // 非完成态：隐藏完成相关元素
            v.DaughterCompleteText?.gameObject.SetActive(false);
            v.CollectedText?.gameObject.SetActive(false);
            HideRewardPhoto();

            if (!state.daughterUnlocked)
            {
                // 等待母亲
                v.DaughterWaitingText?.gameObject.SetActive(true);
                v.DaughterInstructionText?.gameObject.SetActive(false);
                v.DaughterDishZone?.gameObject.SetActive(false);
                v.DaughterDishPhoto?.gameObject.SetActive(false);
                HideAllSeasoningSlots();
            }
            else
            {
                // 调味阶段
                v.DaughterWaitingText?.gameObject.SetActive(false);
                v.DaughterInstructionText?.gameObject.SetActive(true);
                v.DaughterDishZone?.gameObject.SetActive(true);
                v.DaughterDishPhoto?.gameObject.SetActive(true);

                // 确保菜品照片使用当前菜谱的贴图
                if (v.DaughterDishPhoto != null && _recipe.DishPhotoSprite != null)
                {
                    v.DaughterDishPhoto.sprite = _recipe.DishPhotoSprite;
                    v.DaughterDishPhoto.preserveAspect = true;
                }

                // 更新调料槽位
                UpdateSeasoningSlots();
            }
        }

        // ==============================
        // 背景更新
        // ==============================

        private void UpdateBackground(SessionRole role, CookingGameState state)
        {
            if (_view.Background == null) return;

            Sprite bgSprite;
            if (role == SessionRole.Host)
            {
                bgSprite = _recipe.DaughterBackground;
            }
            else
            {
                bgSprite = state.completed && _recipe.MotherCompleteBackground != null
                    ? _recipe.MotherCompleteBackground
                    : _recipe.CookingBackground;
            }

            if (bgSprite != null)
            {
                _view.Background.sprite = bgSprite;
                _view.Background.color = Color.white;
            }
        }

        // ==============================
        // 食材/调料槽位管理
        // ==============================

        private Sprite LoadWaitingSprite()
        {
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("组 23 t:Sprite");
            if (guids.Length > 0)
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
#endif
            return null;
        }

        private void BuildSlotMaps()
        {
            var items = new List<string>();
            items.AddRange(_recipe.RequiredIngredients);
            items.AddRange(_recipe.DistractorIngredients);
            for (var i = 0; i < items.Count; i++)
            {
                if (!_ingredientSlotMap.ContainsKey(items[i]))
                    _ingredientSlotMap[items[i]] = i;
            }

            var seasonings = _recipe.SeasoningOptions;
            for (var i = 0; i < seasonings.Length; i++)
            {
                if (!_seasoningSlotMap.ContainsKey(seasonings[i]))
                    _seasoningSlotMap[seasonings[i]] = i;
            }
        }

        private void UpdateIngredientSlots(CookingGameState state)
        {
            if (_view.MotherIngredientSlots == null) return;

            foreach (var kvp in _ingredientSlotMap)
            {
                var id = kvp.Key;
                var index = kvp.Value;
                if (index >= _view.MotherIngredientSlots.Length) continue;

                var slot = _view.MotherIngredientSlots[index];
                if (slot.root == null) continue;

                bool selected = state.selectedIngredients.Contains(id) || state.droppedIngredients.Contains(id);
                slot.root.SetActive(!selected);

                if (!selected)
                {
                    // 确保位置重置
                    var rect = slot.root.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        var posX = -480 + index * 380;
                        rect.anchoredPosition = new Vector2(posX, -340);
                        rect.localScale = Vector3.one;
                        rect.localRotation = Quaternion.identity;
                    }
                    // 确保 sprite 正确
                    if (slot.image != null)
                    {
                        var sprite = _recipe.GetIngredientSprite(id);
                        if (sprite != null)
                        {
                            slot.image.sprite = sprite;
                            slot.image.color = Color.white;
                            slot.image.preserveAspect = true;
                        }
                    }
                }
            }
        }

        private void UpdateSeasoningSlots()
        {
            if (_view.DaughterSeasoningSlots == null) return;

            foreach (var kvp in _seasoningSlotMap)
            {
                var id = kvp.Key;
                var index = kvp.Value;
                if (index >= _view.DaughterSeasoningSlots.Length) continue;

                var slot = _view.DaughterSeasoningSlots[index];
                if (slot.root == null) continue;

                slot.root.SetActive(true);

                // 确保位置重置
                var rect = slot.root.GetComponent<RectTransform>();
                if (rect != null)
                {
                    var spacing = 540f / Mathf.Max(_recipe.SeasoningOptions.Length, 1);
                    var posX = -270f + index * spacing;
                    rect.anchoredPosition = new Vector2(posX, -340);
                    rect.localScale = Vector3.one;
                    rect.localRotation = Quaternion.identity;
                }
                // 确保 sprite 正确
                if (slot.image != null)
                {
                    var sprite = _recipe.GetIngredientSprite(id);
                    if (sprite != null)
                    {
                        slot.image.sprite = sprite;
                        slot.image.color = Color.white;
                        slot.image.preserveAspect = true;
                    }
                }
            }
        }

        private void HideAllSeasoningSlots()
        {
            if (_view.DaughterSeasoningSlots == null) return;
            foreach (var slot in _view.DaughterSeasoningSlots)
            {
                if (slot.root != null) slot.root.SetActive(false);
            }
        }

        // ==============================
        // 照片奖励
        // ==============================

        private void ShowRewardPhoto(string photoId)
        {
            var v = _view;

            if (v.RewardPhotoImage != null)
            {
                var rewardSprite = _recipe.RewardPhotoSprite;
                if (rewardSprite != null)
                {
                    v.RewardPhotoImage.sprite = rewardSprite;
                    v.RewardPhotoImage.color = Color.white;
                    v.RewardPhotoImage.preserveAspect = true;
                }
                v.RewardPhotoImage.gameObject.SetActive(true);
            }

            if (v.PhotoLabelText != null)
            {
                v.PhotoLabelText.gameObject.SetActive(true);
            }

            // 收集按钮
            if (v.CollectButtonRoot != null)
            {
                v.CollectButtonRoot.SetActive(true);
                var isHost = NetworkSessionManager.Service.Role == SessionRole.Host;
                if (v.CollectButton != null)
                {
                    v.CollectButton.interactable = isHost;
                    if (isHost && !_collectButtonWired)
                    {
                        _collectButtonWired = true;
                        var capturedPhotoId = photoId;
                        v.CollectButton.onClick.RemoveAllListeners();
                        v.CollectButton.onClick.AddListener(() =>
                        {
                            _coordinator.Request(new GameplayIntent(GameplayIntentType.CollectMiniGamePhoto, null, capturedPhotoId));
                        });
                    }
                }
            }

            // 呼吸光
            if (v.CollectGlowImage != null)
            {
                v.CollectGlowImage.gameObject.SetActive(true);
                StopCollectGlow();
                _collectGlowRoutine = StartCoroutine(UiFx.PulseGlow(v.CollectGlowImage));
            }

            // 首次出现：翻转 + 光芒
            if (!_completionFxPlayed)
            {
                _completionFxPlayed = true;
                AudioManager.Play(SfxId.CookComplete);
                AudioManager.Play(SfxId.CardFlip);

                if (v.RewardPhotoImage != null)
                {
                    var photoRect = v.RewardPhotoImage.rectTransform;
                    v.RewardPhotoImage.color = new Color(v.RewardPhotoImage.color.r,
                        v.RewardPhotoImage.color.g, v.RewardPhotoImage.color.b, 0f);
                    StartCoroutine(UiFx.CardFlip(photoRect, 0.5f, () =>
                    {
                        if (v.RewardPhotoImage != null)
                        {
                            v.RewardPhotoImage.color = new Color(v.RewardPhotoImage.color.r,
                                v.RewardPhotoImage.color.g, v.RewardPhotoImage.color.b, 1f);
                        }
                    }));
                }
                StartCoroutine(DelayedLightBurst(v.RewardPhotoImage != null
                    ? v.RewardPhotoImage.rectTransform.anchoredPosition
                    : Vector2.zero, 0.25f));
                StartCoroutine(DelayedTextFadeIn());
            }

            // 已收集文字隐藏
            v.CollectedText?.gameObject.SetActive(false);
        }

        /// <summary>完成菜品后延迟自动收集照片，无需手动点击。</summary>
        private IEnumerator AutoCollectPhoto(string photoId)
        {
            yield return new WaitForSecondsRealtime(2f);
            if (_coordinator != null && _coordinator.State.phase == GameplayPhase.MiniGame)
            {
                _coordinator.Request(new GameplayIntent(
                    GameplayIntentType.CollectMiniGamePhoto, null, photoId));
            }
        }

        private void HideRewardPhoto()
        {
            var v = _view;
            v.RewardPhotoImage?.gameObject.SetActive(false);
            v.PhotoLabelText?.gameObject.SetActive(false);
            v.CollectButtonRoot?.gameObject.SetActive(false);
            v.CollectGlowImage?.gameObject.SetActive(false);
            StopCollectGlow();
        }

        // ==============================
        // 拖拽回调初始化
        // ==============================

        private void WireDragCallbacks()
        {
            if (_dragCallbacksWired) return;
            _dragCallbacksWired = true;

            // 食材拖拽
            if (_view.MotherIngredientSlots != null)
            {
                foreach (var kvp in _ingredientSlotMap)
                {
                    var id = kvp.Key;
                    var index = kvp.Value;
                    if (index >= _view.MotherIngredientSlots.Length) continue;

                    var slot = _view.MotherIngredientSlots[index];
                    if (slot.draggable == null) continue;

                    var capturedId = id;
                    var zone = _view.MotherContainerZone;
                    slot.draggable.OnEndDragEvent += (eventData, _) =>
                    {
                        if (zone != null && RectTransformUtility.RectangleContainsScreenPoint(zone, eventData.position, null))
                        {
                            if (_recipe.IsCorrectIngredient(capturedId))
                            {
                                StartCoroutine(DropIngredientFx(slot.draggable, zone, () =>
                                {
                                    _coordinator.Request(new GameplayIntent(GameplayIntentType.SelectIngredient, _recipe.RecipeId, capturedId));
                                    AudioManager.Play(SfxId.CookIngredientDrop);
                                }));
                            }
                            else
                            {
                                AudioManager.Play(SfxId.UiError);
                                slot.draggable.ReturnToOrigin();
                            }
                        }
                        else
                        {
                            AudioManager.Play(SfxId.UiError);
                            slot.draggable.ReturnToOrigin();
                        }
                    };
                }
            }

            // 调料拖拽
            if (_view.DaughterSeasoningSlots != null)
            {
                foreach (var kvp in _seasoningSlotMap)
                {
                    var id = kvp.Key;
                    var index = kvp.Value;
                    if (index >= _view.DaughterSeasoningSlots.Length) continue;

                    var slot = _view.DaughterSeasoningSlots[index];
                    if (slot.draggable == null) continue;

                    var capturedId = id;
                    var zone = _view.DaughterDishZone;
                    slot.draggable.OnEndDragEvent += (eventData, _) =>
                    {
                        if (zone != null && RectTransformUtility.RectangleContainsScreenPoint(zone, eventData.position, null))
                        {
                            if (_recipe.IsCorrectSeasoning(capturedId))
                            {
                                StartCoroutine(DropIngredientFx(slot.draggable, zone, () =>
                                {
                                    _coordinator.Request(new GameplayIntent(GameplayIntentType.SelectSeasoning, _recipe.RecipeId, capturedId));
                                    AudioManager.Play(SfxId.CookIngredientDrop);
                                }));
                            }
                            else
                            {
                                AudioManager.Play(SfxId.UiError);
                                slot.draggable.ReturnToOrigin();
                            }
                        }
                        else
                        {
                            AudioManager.Play(SfxId.UiError);
                            slot.draggable.ReturnToOrigin();
                        }
                    };
                }
            }
        }

        // ==============================
        // 辅助
        // ==============================

        private void SetAllInactive()
        {
            var v = _view;
            v.MotherPanel?.SetActive(false);
            v.DaughterPanel?.SetActive(false);
            v.WaitingText?.gameObject.SetActive(false);
            v.MotherWaitingImage?.gameObject.SetActive(false);
            v.DaughterWaitingImage?.gameObject.SetActive(false);
        }

        private void ShowMotherElements(bool instruction, bool waiting, bool hint)
        {
            var v = _view;
            v.MotherInstructionText?.gameObject.SetActive(instruction);
            v.MotherContainerZone?.gameObject.SetActive(instruction);
            v.MotherDroppedNamesText?.gameObject.SetActive(false);
            HideAllIngredientSlots();
            v.MotherWaitingText?.gameObject.SetActive(waiting);
            v.MotherWaitingImage?.gameObject.SetActive(waiting);
            v.MotherRecipeNoteText?.gameObject.SetActive(waiting);
            v.MotherHintImage?.gameObject.SetActive(hint);
        }

        private void HideAllIngredientSlots()
        {
            if (_view.MotherIngredientSlots == null) return;
            foreach (var slot in _view.MotherIngredientSlots)
            {
                if (slot.root != null) slot.root.SetActive(false);
            }
        }

        private bool IsRewardPhotoCollected()
        {
            if (_recipe?.RewardIds == null || _coordinator?.State?.collectedPhotoIds == null)
                return false;
            foreach (var id in _recipe.RewardIds)
            {
                if (id != null && id.IndexOf("photo", System.StringComparison.OrdinalIgnoreCase) >= 0
                    && _coordinator.State.collectedPhotoIds.Contains(id))
                    return true;
            }
            return false;
        }

        private string GetRewardPhotoId()
        {
            if (_recipe?.RewardIds == null) return null;
            return System.Array.Find(_recipe.RewardIds,
                r => r != null && r.IndexOf("photo", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void StopAmbientSteam()
        {
            if (_ambientSteam != null)
            {
                StopCoroutine(_ambientSteam);
                _ambientSteam = null;
            }
        }

        private void StopCollectGlow()
        {
            if (_collectGlowRoutine != null)
            {
                StopCoroutine(_collectGlowRoutine);
                _collectGlowRoutine = null;
            }
        }

        // ==============================
        // 特效协程
        // ==============================

        private IEnumerator AmbientSteam(Vector2 potPosition)
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1.2f);
                StartCoroutine(UiFx.SteamBurst(Panel, potPosition, 4));
            }
        }

        private IEnumerator DropIngredientFx(DraggableItem item, RectTransform potZone,
            System.Action onComplete)
        {
            var rect = item.GetComponent<RectTransform>();
            StartCoroutine(UiFx.SteamBurst(Panel, potZone.anchoredPosition));
            yield return StartCoroutine(UiFx.ShrinkOut(rect, 0.25f));
            onComplete?.Invoke();
        }

        private IEnumerator DelayedLightBurst(Vector2 pos, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            StartCoroutine(UiFx.LightBurst(Panel, pos));
        }

        private IEnumerator DelayedTextFadeIn()
        {
            yield return new WaitForSecondsRealtime(0.3f);
            if (_view.DaughterCompleteText != null)
            {
                var rect = _view.DaughterCompleteText.rectTransform;
                rect.localScale = Vector3.zero;
                StartCoroutine(UiTween.Scale(rect, Vector3.zero, Vector3.one, 0.3f, UiTween.EaseOutBack));
            }
        }

        // ==============================
        // 显示名
        // ==============================

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
                case "vinegar": return "醋";
                case "chili": return "辣椒";
                default: return itemId;
            }
        }
    }
}
