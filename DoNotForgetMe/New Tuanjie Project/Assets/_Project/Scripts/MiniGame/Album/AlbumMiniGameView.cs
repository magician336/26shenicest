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

            // 中断按钮
            if (v.InterruptButtonRoot != null) v.InterruptButtonRoot.SetActive(true);

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
                        if (placed)
                        {
                            if (entry.stickerSprite != null)
                            {
                                zone.image.sprite = entry.stickerSprite;
                                zone.image.preserveAspect = true;
                                zone.image.color = Color.white;
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
                            zone.image.color = new Color(0.4f, 0.6f, 0.35f, 0.9f);
                        }
                        else
                        {
                            zone.image.color = new Color(1f, 1f, 1f, 0.06f);
                        }
                    }

                    if (zone.labelText != null)
                    {
                        zone.labelText.text = nameTagPlaced ? entry.displayName : "";
                    }
                }
            }
        }

        private void UpdateStickerDraggables(AlbumGameState state)
        {
            if (_view.StickerDraggables == null) return;
            var stickerEntries = _config.GetStickerEntries();
            if (stickerEntries == null) return;

            var spacing = 220f;
            var startX = -(stickerEntries.Length - 1) * spacing / 2f;

            foreach (var kvp in _stickerDraggableMap)
            {
                var characterId = kvp.Key;
                var index = kvp.Value;
                if (index >= _view.StickerDraggables.Length) continue;

                var slot = _view.StickerDraggables[index];
                if (slot.root == null) continue;

                var placed = state.placedStickerCharacterIds.Contains(characterId);
                slot.root.SetActive(!placed);

                if (!placed)
                {
                    var rect = slot.root.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchoredPosition = new Vector2(startX + index * spacing, -320);
                        rect.localScale = Vector3.one;
                        rect.localRotation = Quaternion.identity;
                    }
                }
            }
        }

        private void UpdateNameTagDraggables(AlbumGameState state)
        {
            if (_view.NameTagDraggables == null) return;
            var stickerEntries = _config.GetStickerEntries();
            if (stickerEntries == null) return;

            var spacing = 220f;
            var startX = -(stickerEntries.Length - 1) * spacing / 2f;

            foreach (var kvp in _nameTagDraggableMap)
            {
                var characterId = kvp.Key;
                var index = kvp.Value;
                if (index >= _view.NameTagDraggables.Length) continue;

                var slot = _view.NameTagDraggables[index];
                if (slot.root == null) continue;

                var placed = state.placedNameTagCharacterIds.Contains(characterId);
                slot.root.SetActive(!placed);

                if (!placed)
                {
                    var rect = slot.root.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchoredPosition = new Vector2(startX + index * spacing, -320);
                        rect.localScale = Vector3.one;
                        rect.localRotation = Quaternion.identity;
                    }
                }
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
            v.InterruptButtonRoot?.SetActive(false);

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
                        var targetZone = FindZoneAt(eventData.position, _stickerZoneMap, _view.StickerZones);
                        if (targetZone == null)
                        {
                            AudioManager.Play(SfxId.AlbumStickerWrong);
                            slot.draggable.ReturnToOrigin();
                            return;
                        }

                        _coordinator.Request(new GameplayIntent(GameplayIntentType.PlaceAlbumSticker,
                            _config.MiniGameId, null, capturedId, targetZone));

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
                        var targetZone = FindZoneAt(eventData.position, _nameTagZoneMap, _view.NameTagZones);
                        if (targetZone == null)
                        {
                            slot.draggable.ReturnToOrigin();
                            return;
                        }

                        _coordinator.Request(new GameplayIntent(GameplayIntentType.PlaceAlbumNameTag,
                            _config.MiniGameId, null, capturedId, targetZone));

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

            // 中断按钮
            if (_view.InterruptButton != null)
            {
                _view.InterruptButton.onClick.RemoveAllListeners();
                _view.InterruptButton.onClick.AddListener(() =>
                    _coordinator.Request(new GameplayIntent(GameplayIntentType.InterruptMiniGame, _config.MiniGameId)));
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
            v.InterruptButtonRoot?.SetActive(false);
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

            // 全屏写实风全家福
            if (v.FamilyPortraitImage != null)
            {
                v.FamilyPortraitImage.gameObject.SetActive(true);
                v.FamilyPortraitImage.canvasRenderer.SetAlpha(0f);
                v.FamilyPortraitImage.CrossFadeAlpha(1f, 1.5f, false);
            }

            yield return new WaitForSeconds(4f);

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
            v.InterruptButtonRoot?.SetActive(false);
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

        private static string FindZoneAt(Vector2 screenPosition,
            Dictionary<string, int> zoneMap, StickerZoneView[] zones)
        {
            foreach (var kvp in zoneMap)
            {
                if (kvp.Value >= zones.Length) continue;
                var zone = zones[kvp.Value];
                if (zone.root == null || !zone.root.activeSelf) continue;
                var rect = zone.root.GetComponent<RectTransform>();
                if (rect == null) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, null))
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        private static string FindZoneAt(Vector2 screenPosition,
            Dictionary<string, int> zoneMap, NameTagZoneView[] zones)
        {
            foreach (var kvp in zoneMap)
            {
                if (kvp.Value >= zones.Length) continue;
                var zone = zones[kvp.Value];
                if (zone.root == null || !zone.root.activeSelf) continue;
                var rect = zone.root.GetComponent<RectTransform>();
                if (rect == null) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, null))
                {
                    return kvp.Key;
                }
            }
            return null;
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
