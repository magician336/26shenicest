using System.Collections;
using System.Collections.Generic;
using DoNotForgetMe.Audio;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;
using DoNotForgetMe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.MiniGame.Album
{
    /// <summary>
    /// 全家福相册小游戏控制器。使用 prefab + AlbumView 进行视图绑定，
    /// Render(state) 原地更新 UI 元素，不再销毁重建。
    /// 双端同屏，不分 Host/Client 视图。
    /// </summary>
    public class AlbumMiniGameView : MiniGameBase
    {
        private AlbumConfig _config;
        private SessionGameplayCoordinator _coordinator;
        private GameObject _content;
        private AlbumView _view;
        private bool _callbacksWired;
        private string _lastRenderKey;
        private Transform _portraitLayerRoot;

        // 角色 ID → 索引
        private readonly Dictionary<string, int> _stickerZoneMap = new();
        private readonly Dictionary<string, int> _nameTagZoneMap = new();
        private readonly Dictionary<string, int> _stickerDraggableMap = new();
        private readonly Dictionary<string, int> _nameTagDraggableMap = new();

        public override string GameId => _config != null ? _config.MiniGameId : string.Empty;

        public void Setup(AlbumConfig config, SessionGameplayCoordinator coordinator)
        {
            _config = config;
            _coordinator = coordinator;
        }

        public override void StartGame()
        {
            _callbacksWired = false;
            _lastRenderKey = null;
            _stickerZoneMap.Clear();
            _nameTagZoneMap.Clear();
            _stickerDraggableMap.Clear();
            _nameTagDraggableMap.Clear();

            var prefab = Resources.Load<GameObject>("MiniGamePrefabs/AlbumView");
            if (prefab == null)
            {
                Debug.LogError("[AlbumMiniGameView] 未找到 MiniGamePrefabs/AlbumView prefab。" +
                               "请先运行 Tools > MiniGame > Export Album Prefab");
                return;
            }
            _content = Object.Instantiate(prefab, Panel, false);
            _content.name = "AlbumView";
            _view = _content.GetComponent<AlbumView>();

            if (_view == null)
            {
                Debug.LogError("[AlbumMiniGameView] Prefab 上缺少 AlbumView 组件");
                return;
            }

            SetAllInactive();
            BuildSlotMaps();
            WireCallbacks();

            // 查找并隐藏片头全家福图层（手动添加到 prefab 中的 SpriteRenderer 对象）
            _portraitLayerRoot = _content.transform.Find("微信图片_20260830062314_1474_3230");
            if (_portraitLayerRoot != null)
            {
                _portraitLayerRoot.gameObject.SetActive(false);
            }
        }

        public void Render(AlbumGameState state)
        {
            if (_view == null || _config == null) return;

            var renderKey = $"{state.step}_{state.completed}_{state.placedStickerCharacterIds.Count}_{state.placedNameTagCharacterIds.Count}";
            if (renderKey == _lastRenderKey) return;
            _lastRenderKey = renderKey;

            if (state.completed && state.step == AlbumStep.Complete)
            {
                ShowCompleteButton(state);
                return;
            }

            RenderSharedView(state);
        }

        public override void UpdateGame()
        {
        }

        public override void EndGame()
        {
            if (_content != null)
            {
                Destroy(_content);
                _content = null;
                _view = null;
            }
            _callbacksWired = false;
            _lastRenderKey = null;
            _portraitLayerRoot = null;
        }

        // ==============================
        // 共享视图
        // ==============================

        private void RenderSharedView(AlbumGameState state)
        {
            var v = _view;

            // 标题
            if (v.TitleText != null) v.TitleText.gameObject.SetActive(true);

            // 线索按钮
            if (v.ClueButtonRoot != null) v.ClueButtonRoot.SetActive(true);

            // 相册底图
            if (v.AlbumBaseImage != null) v.AlbumBaseImage.gameObject.SetActive(true);

            // 隐藏完成动画
            v.FamilyPortraitImage?.gameObject.SetActive(false);
            v.BlackScreenImage?.gameObject.SetActive(false);
            v.CompleteButtonRoot?.SetActive(false);

            // 更新轮廓区域
            UpdateZones(state);

            // 根据阶段渲染候选区域
            if (state.step == AlbumStep.PlaceStickers)
            {
                if (v.InstructionText != null)
                {
                    v.InstructionText.text = "把人物贴纸拖到对应的轮廓里";
                    v.InstructionText.gameObject.SetActive(true);
                }
                UpdateStickerDraggables(state);
                HideAllNameTagDraggables();
            }
            else if (state.step == AlbumStep.PlaceNameTags)
            {
                if (v.InstructionText != null)
                {
                    v.InstructionText.text = "把姓名标签拖到对应的人物名牌上";
                    v.InstructionText.gameObject.SetActive(true);
                }
                HideAllStickerDraggables();
                UpdateNameTagDraggables(state);
            }
        }

        private void UpdateZones(AlbumGameState state)
        {
            var entries = _config.Entries;
            if (entries == null) return;

            // 贴纸轮廓
            if (_view.StickerZones != null)
            {
                foreach (var kvp in _stickerZoneMap)
                {
                    var characterId = kvp.Key;
                    var index = kvp.Value;
                    if (index >= _view.StickerZones.Length) continue;

                    var zone = _view.StickerZones[index];
                    if (zone.root == null) continue;

                    var entry = _config.FindEntry(characterId);
                    if (entry == null) continue;

                    var placed = state.placedStickerCharacterIds.Contains(characterId);
                    zone.root.SetActive(true);

                    if (zone.image != null)
                    {
                        zone.image.raycastTarget = false;
                        if (placed)
                        {
                            if (entry.stickerSprite != null)
                            {
                                zone.image.sprite = entry.stickerSprite;
                                zone.image.preserveAspect = true;
                                zone.image.color = Color.white;

                                // 保持贴纸原始尺寸和旋转：从对应 Draggable 同步
                                if (_stickerDraggableMap.TryGetValue(characterId, out var dragIdx)
                                    && dragIdx < _view.StickerDraggables.Length
                                    && _view.StickerDraggables[dragIdx].root != null)
                                {
                                    var dragRect = _view.StickerDraggables[dragIdx].root.GetComponent<RectTransform>();
                                    var zoneRect = zone.root.GetComponent<RectTransform>();
                                    if (dragRect != null && zoneRect != null)
                                    {
                                        zoneRect.sizeDelta = dragRect.sizeDelta;
                                        zoneRect.localRotation = dragRect.localRotation;
                                    }
                                }
                            }
                            else
                            {
                                zone.image.color = new Color(0.7f, 0.58f, 0.4f);
                            }
                        }
                        else
                        {
                            zone.image.color = entry.hasSticker
                                ? new Color(1f, 1f, 1f, 0.08f)
                                : new Color(0.3f, 0.25f, 0.2f, 0.15f);
                        }
                    }
                }
            }

            // 姓名名牌区
            if (_view.NameTagZones != null)
            {
                foreach (var kvp in _nameTagZoneMap)
                {
                    var characterId = kvp.Key;
                    var index = kvp.Value;
                    if (index >= _view.NameTagZones.Length) continue;

                    var zone = _view.NameTagZones[index];
                    if (zone.root == null) continue;

                    var entry = _config.FindEntry(characterId);
                    if (entry == null) continue;

                    var stickerPlaced = state.placedStickerCharacterIds.Contains(characterId);
                    var nameTagPlaced = state.placedNameTagCharacterIds.Contains(characterId);

                    // 贴纸放入后才显示名牌区（小岩没有贴纸也显示）
                    zone.root.SetActive(stickerPlaced || !entry.hasSticker);

                    if (zone.image != null)
                    {
                        if (nameTagPlaced)
                        {
                            // 从对应 Draggable 同步 sprite、颜色、尺寸和旋转
                            if (_nameTagDraggableMap.TryGetValue(characterId, out var dragIdx)
                                && dragIdx < _view.NameTagDraggables.Length
                                && _view.NameTagDraggables[dragIdx].root != null)
                            {
                                var dragImg = _view.NameTagDraggables[dragIdx].image;
                                var dragRect = _view.NameTagDraggables[dragIdx].root.GetComponent<RectTransform>();
                                var zoneRect = zone.root.GetComponent<RectTransform>();
                                if (dragImg != null)
                                {
                                    zone.image.sprite = dragImg.sprite;
                                    zone.image.color = dragImg.color;
                                    zone.image.preserveAspect = dragImg.preserveAspect;
                                }
                                if (dragRect != null && zoneRect != null)
                                {
                                    zoneRect.sizeDelta = dragRect.sizeDelta;
                                    zoneRect.localRotation = dragRect.localRotation;
                                }

                                // 同步姓名标签文字子组件的字体属性
                                var dragLabel = _view.NameTagDraggables[dragIdx].labelText;
                                if (dragLabel != null && zone.labelText != null)
                                {
                                    zone.labelText.font = dragLabel.font;
                                    zone.labelText.fontSize = dragLabel.fontSize;
                                    zone.labelText.fontStyle = dragLabel.fontStyle;
                                    zone.labelText.color = dragLabel.color;
                                    zone.labelText.alignment = dragLabel.alignment;
                                    zone.labelText.text = dragLabel.text;
                                }
                            }
                        }
                        else
                        {
                            zone.image.color = new Color(1f, 1f, 1f, 0.06f);
                        }
                    }

                    if (zone.labelText != null && !nameTagPlaced)
                    {
                        zone.labelText.text = "";
                    }
                }
            }
        }

        private void UpdateStickerDraggables(AlbumGameState state)
        {
            if (_view.StickerDraggables == null) return;

            foreach (var kvp in _stickerDraggableMap)
            {
                var characterId = kvp.Key;
                var index = kvp.Value;
                if (index >= _view.StickerDraggables.Length) continue;

                var slot = _view.StickerDraggables[index];
                if (slot.root == null) continue;

                var placed = state.placedStickerCharacterIds.Contains(characterId);
                slot.root.SetActive(!placed);
            }
        }

        private void UpdateNameTagDraggables(AlbumGameState state)
        {
            if (_view.NameTagDraggables == null) return;

            foreach (var kvp in _nameTagDraggableMap)
            {
                var characterId = kvp.Key;
                var index = kvp.Value;
                if (index >= _view.NameTagDraggables.Length) continue;

                var slot = _view.NameTagDraggables[index];
                if (slot.root == null) continue;

                var placed = state.placedNameTagCharacterIds.Contains(characterId);
                slot.root.SetActive(!placed);
            }
        }

        // ==============================
        // 完成按钮
        // ==============================

        private void ShowCompleteButton(AlbumGameState state)
        {
            var v = _view;

            // 隐藏候选区域
            HideAllStickerDraggables();
            HideAllNameTagDraggables();
            v.InstructionText?.gameObject.SetActive(false);

            // 更新轮廓（贴纸和名牌都已填入）
            UpdateZones(state);

            // 显示完成按钮
            if (v.CompleteButtonRoot != null)
            {
                v.CompleteButtonRoot.SetActive(true);
            }
        }

        // ==============================
        // 回调绑定
        // ==============================

        private void WireCallbacks()
        {
            if (_callbacksWired) return;
            _callbacksWired = true;

            // 贴纸拖拽
            if (_view.StickerDraggables != null)
            {
                foreach (var kvp in _stickerDraggableMap)
                {
                    var characterId = kvp.Key;
                    var index = kvp.Value;
                    if (index >= _view.StickerDraggables.Length) continue;

                    var slot = _view.StickerDraggables[index];
                    if (slot.draggable == null) continue;

                    var capturedId = characterId;
                    slot.draggable.OnEndDragEvent += (eventData, _) =>
                    {
                        var hit = IsDroppedOnOwnZone(eventData.position, capturedId,
                            _stickerZoneMap, _view.StickerZones, z => z.root);
                        if (!hit)
                        {
                            AudioManager.Play(SfxId.AlbumStickerWrong);
                            slot.draggable.ReturnToOrigin();
                            return;
                        }

                        _coordinator.Request(new GameplayIntent(GameplayIntentType.PlaceAlbumSticker,
                            _config.MiniGameId, null, capturedId, capturedId));

                        if (!_coordinator.State.album.placedStickerCharacterIds.Contains(capturedId))
                        {
                            AudioManager.Play(SfxId.AlbumStickerWrong);
                            slot.draggable.ReturnToOrigin();
                            ShakeRect(slot.root.GetComponent<RectTransform>());
                        }
                        else
                        {
                            AudioManager.Play(SfxId.AlbumStickerPlaced);
                        }
                    };
                }
            }

            // 姓名标签拖拽
            if (_view.NameTagDraggables != null)
            {
                foreach (var kvp in _nameTagDraggableMap)
                {
                    var characterId = kvp.Key;
                    var index = kvp.Value;
                    if (index >= _view.NameTagDraggables.Length) continue;

                    var slot = _view.NameTagDraggables[index];
                    if (slot.draggable == null) continue;

                    var capturedId = characterId;
                    slot.draggable.OnEndDragEvent += (eventData, _) =>
                    {
                        var hit = IsDroppedOnOwnZone(eventData.position, capturedId,
                            _nameTagZoneMap, _view.NameTagZones, z => z.root);
                        if (!hit)
                        {
                            slot.draggable.ReturnToOrigin();
                            return;
                        }

                        _coordinator.Request(new GameplayIntent(GameplayIntentType.PlaceAlbumNameTag,
                            _config.MiniGameId, null, capturedId, capturedId));

                        if (!_coordinator.State.album.placedNameTagCharacterIds.Contains(capturedId))
                        {
                            slot.draggable.ReturnToOrigin();
                            ShakeRect(slot.root.GetComponent<RectTransform>());
                        }
                    };
                }
            }

            // 线索按钮
            if (_view.ClueButton != null)
            {
                _view.ClueButton.onClick.RemoveAllListeners();
                _view.ClueButton.onClick.AddListener(ToggleCluePanel);
            }

            // 关闭线索按钮
            if (_view.CloseClueButton != null)
            {
                _view.CloseClueButton.onClick.RemoveAllListeners();
                _view.CloseClueButton.onClick.AddListener(() =>
                {
                    if (_view.CluePanelRoot != null) _view.CluePanelRoot.SetActive(false);
                });
            }

            // 完成按钮
            if (_view.CompleteButton != null)
            {
                _view.CompleteButton.onClick.RemoveAllListeners();
                _view.CompleteButton.onClick.AddListener(() => StartCoroutine(ShowFamilyPortraitAndFinish()));
            }
        }

        // ==============================
        // 线索面板
        // ==============================

        private void ToggleCluePanel()
        {
            if (_view.CluePanelRoot == null) return;
            var isActive = _view.CluePanelRoot.activeSelf;
            _view.CluePanelRoot.SetActive(!isActive);
        }

        // ==============================
        // 完成动画
        // ==============================

        private IEnumerator ShowFamilyPortraitAndFinish()
        {
            var v = _view;

            // 隐藏交互元素
            v.TitleText?.gameObject.SetActive(false);
            v.ClueButtonRoot?.SetActive(false);
            v.InstructionText?.gameObject.SetActive(false);
            v.CompleteButtonRoot?.SetActive(false);
            v.AlbumBaseImage?.gameObject.SetActive(false);

            // 隐藏所有轮廓
            if (v.StickerZones != null)
            {
                foreach (var zone in v.StickerZones)
                {
                    if (zone.root != null) zone.root.SetActive(false);
                }
            }
            if (v.NameTagZones != null)
            {
                foreach (var zone in v.NameTagZones)
                {
                    if (zone.root != null) zone.root.SetActive(false);
                }
            }

            // 弹出片头全家福图层及其子图层
            if (_portraitLayerRoot != null)
            {
                _portraitLayerRoot.gameObject.SetActive(true);

                // 记录原始 scale，从 0 弹到原始大小
                var parentScale = _portraitLayerRoot.localScale;
                _portraitLayerRoot.localScale = Vector3.zero;

                var child = _portraitLayerRoot.GetChild(0);
                var childScale = child.localScale;
                child.localScale = Vector3.zero;

                // 父图层弹出
                yield return ScaleFromZero(_portraitLayerRoot, parentScale, 0.6f);

                // 子图层延迟弹出
                yield return new WaitForSeconds(0.3f);
                yield return ScaleFromZero(child, childScale, 0.5f);
            }

            yield return new WaitForSeconds(3f);

            // 淡入黑屏
            if (v.BlackScreenImage != null)
            {
                v.BlackScreenImage.gameObject.SetActive(true);
                v.BlackScreenImage.canvasRenderer.SetAlpha(0f);
                v.BlackScreenImage.CrossFadeAlpha(1f, 2f, false);
            }

            yield return new WaitForSeconds(2.5f);

            _coordinator.Request(new GameplayIntent(GameplayIntentType.FinishMiniGame));
        }

        private static IEnumerator ScaleFromZero(Transform target, Vector3 targetScale, float duration)
        {
            if (target == null) yield break;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                // EaseOutBack
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                var eased = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                target.localScale = targetScale * eased;
                yield return null;
            }
            target.localScale = targetScale;
        }

        // ==============================
        // Slot 映射
        // ==============================

        private void BuildSlotMaps()
        {
            var entries = _config.Entries;
            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    if (entries[i] != null)
                    {
                        if (!_stickerZoneMap.ContainsKey(entries[i].characterId))
                            _stickerZoneMap[entries[i].characterId] = i;
                        if (!_nameTagZoneMap.ContainsKey(entries[i].characterId))
                            _nameTagZoneMap[entries[i].characterId] = i;
                    }
                }
            }

            var stickerEntries = _config.GetStickerEntries();
            if (stickerEntries != null)
            {
                for (var i = 0; i < stickerEntries.Length; i++)
                {
                    if (stickerEntries[i] != null)
                    {
                        if (!_stickerDraggableMap.ContainsKey(stickerEntries[i].characterId))
                            _stickerDraggableMap[stickerEntries[i].characterId] = i;
                        if (!_nameTagDraggableMap.ContainsKey(stickerEntries[i].characterId))
                            _nameTagDraggableMap[stickerEntries[i].characterId] = i;
                    }
                }
            }
        }

        // ==============================
        // 辅助
        // ==============================

        private void SetAllInactive()
        {
            var v = _view;
            v.TitleText?.gameObject.SetActive(false);
            v.InstructionText?.gameObject.SetActive(false);
            v.AlbumBaseImage?.gameObject.SetActive(false);
            v.ClueButtonRoot?.SetActive(false);
            v.CompleteButtonRoot?.SetActive(false);
            v.CluePanelRoot?.SetActive(false);
            v.FamilyPortraitImage?.gameObject.SetActive(false);
            v.BlackScreenImage?.gameObject.SetActive(false);
            HideAllStickerDraggables();
            HideAllNameTagDraggables();
        }

        private void HideAllStickerDraggables()
        {
            if (_view.StickerDraggables == null) return;
            foreach (var slot in _view.StickerDraggables)
            {
                if (slot.root != null) slot.root.SetActive(false);
            }
        }

        private void HideAllNameTagDraggables()
        {
            if (_view.NameTagDraggables == null) return;
            foreach (var slot in _view.NameTagDraggables)
            {
                if (slot.root != null) slot.root.SetActive(false);
            }
        }

        /// <summary>
        /// 检查贴纸/名牌是否被放到了它自己的轮廓区域内。
        /// 不搜索全部 zone——只看该 characterId 对应的那一个 zone，
        /// 用外接圆半径 ×2.0 作为容差，避免 zone 位置与剪影不完全重合时误判。
        /// </summary>
        private static bool IsDroppedOnOwnZone<T>(
            Vector2 screenPosition, string characterId,
            Dictionary<string, int> zoneMap, T[] zones,
            System.Func<T, GameObject> getRoot)
        {
            if (zoneMap == null || !zoneMap.TryGetValue(characterId, out var index))
                return false;
            if (index >= zones.Length) return false;

            var zone = zones[index];
            var root = getRoot(zone);
            if (root == null || !root.activeSelf) return false;
            var rect = root.GetComponent<RectTransform>();
            if (rect == null) return false;

            var canvas = root.GetComponentInParent<Canvas>();
            var cam = canvas != null ? canvas.worldCamera : null;

            // 直接命中 Rect
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, cam))
                return true;

            // 容差：外接圆半径 ×2.0
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, screenPosition, cam, out localPoint);

            var halfW = rect.rect.width * 0.5f;
            var halfH = rect.rect.height * 0.5f;
            var radius = Mathf.Sqrt(halfW * halfW + halfH * halfH);
            var dist = localPoint.magnitude;

            return dist <= radius * 2.0f;
        }

        private void ShakeRect(RectTransform rect)
        {
            if (rect != null) StartCoroutine(ShakeRoutine(rect));
        }

        private IEnumerator ShakeRoutine(RectTransform rect)
        {
            if (rect == null) yield break;
            var original = rect.anchoredPosition;
            var shakeAmount = 12f;
            for (var i = 0; i < 6; i++)
            {
                rect.anchoredPosition = original + new Vector2(
                    i % 2 == 0 ? shakeAmount : -shakeAmount, 0);
                yield return new WaitForSeconds(0.04f);
            }
            rect.anchoredPosition = original;
        }
    }
}
