using System.Collections;
using System.Collections.Generic;
using DoNotForgetMe.Audio;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;
using DoNotForgetMe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.MiniGame.Bagua
{
    /// <summary>
    /// 八卦小游戏控制器。使用 prefab + BaguaView 进行视图绑定，
    /// Render(state) 原地更新 UI 元素，不再销毁重建。
    /// 不在此类中作出任何通关判定——所有判定走 SessionGameplayCoordinator。
    /// </summary>
    public class BaguaMiniGameView : MiniGameBase
    {
        private BaguaStoryConfig _config;
        private SessionGameplayCoordinator _coordinator;
        private GameObject _content;
        private BaguaView _view;
        private AudioSource _audioSource;

        private string _playingCharacterId;
        private Coroutine _subtitleRoutine;
        private Coroutine _collectGlowRoutine;
        private bool _callbacksWired;
        private bool _collectButtonWired;
        private bool _completionFxPlayed;
        private string _lastRenderKey;
        private bool _autoCollectTriggered;
        private Button _nextLevelButton;

        // 物品 ID → 槽位索引
        private readonly Dictionary<string, int> _itemSlotMap = new();
        // 角色 ID → 卡片索引
        private readonly Dictionary<string, int> _cardIndexMap = new();
        // 角色 ID → 照片区索引
        private readonly Dictionary<string, int> _photoZoneIndexMap = new();
        // 角色 ID → 姓名标签索引
        private readonly Dictionary<string, int> _nameTagIndexMap = new();

        public override string GameId => _config != null ? _config.MiniGameId : string.Empty;

        public void Setup(BaguaStoryConfig config, SessionGameplayCoordinator coordinator)
        {
            _config = config;
            _coordinator = coordinator;
        }

        public override void StartGame()
        {
            _callbacksWired = false;
            _collectButtonWired = false;
            _completionFxPlayed = false;
            _lastRenderKey = null;
            _autoCollectTriggered = false;
            _nextLevelButton = null;
            _itemSlotMap.Clear();
            _cardIndexMap.Clear();
            _photoZoneIndexMap.Clear();
            _nameTagIndexMap.Clear();

            var prefab = Resources.Load<GameObject>("MiniGamePrefabs/BaguaView");
            if (prefab == null)
            {
                Debug.LogError("[BaguaMiniGameView] 未找到 MiniGamePrefabs/BaguaView prefab。" +
                               "请先运行 Tools > MiniGame > Export Bagua Prefab");
                return;
            }
            _content = Object.Instantiate(prefab, Panel, false);
            _content.name = "BaguaView";
            _view = _content.GetComponent<BaguaView>();
            _audioSource = _content.AddComponent<AudioSource>();

            if (_view == null)
            {
                Debug.LogError("[BaguaMiniGameView] Prefab 上缺少 BaguaView 组件");
                return;
            }

            SetAllInactive();
            BuildSlotMaps();
            WireCallbacks();
        }

        public void Render(BaguaGameState state)
        {
            if (_view == null || _config == null) return;

            var role = NetworkSessionManager.Service.Role;
            var photoId = GetRewardPhotoId();
            var photoCollected = photoId != null && _coordinator.State.collectedPhotoIds.Contains(photoId);
            var renderKey = $"{role}_{state.step}_{state.completed}_{state.clientComplete}_{state.matchedCharacterIds.Count}_{state.assignedPhotoZoneIds.Count}_{state.heardStoryIds.Count}_{photoCollected}";
            if (renderKey == _lastRenderKey) return;
            _lastRenderKey = renderKey;

            UpdateBackground();

            if (state.completed)
            {
                RenderCompleteView();
                return;
            }

            if (role == SessionRole.Client)
            {
                RenderClientView(state);
            }
            else if (role == SessionRole.Host)
            {
                RenderHostView(state);
            }
            else
            {
                _view.ClientPanel?.SetActive(false);
                _view.HostPanel?.SetActive(false);
                _view.WaitingText?.gameObject.SetActive(true);
            }
        }

        public override void UpdateGame()
        {
        }

        public override void EndGame()
        {
            if (_audioSource != null) _audioSource.Stop();
            if (_subtitleRoutine != null)
            {
                StopCoroutine(_subtitleRoutine);
                _subtitleRoutine = null;
            }
            StopCollectGlow();
            _playingCharacterId = null;
            if (_content != null)
            {
                Destroy(_content);
                _content = null;
                _view = null;
            }
            _callbacksWired = false;
            _collectButtonWired = false;
            _lastRenderKey = null;
            _autoCollectTriggered = false;
            _nextLevelButton = null;
        }

        // ==============================
        // 背景更新
        // ==============================

        private void UpdateBackground()
        {
            if (_view.Background == null) return;

            var bg = _config.DeskBackground;
#if UNITY_EDITOR
            if (bg == null)
            {
                bg = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Project/Art/Backgrounds/bg_desk.png");
            }
#endif
            if (bg != null)
            {
                _view.Background.sprite = bg;
                _view.Background.color = Color.white;

                // 确保背景铺满屏幕，不留透明边距
                _view.Background.preserveAspect = false;
                var arf = _view.Background.GetComponent<AspectRatioFitter>();
                if (arf != null) arf.enabled = false;

                _view.Background.gameObject.SetActive(true);
            }
        }

        // ==============================
        // 母亲端渲染
        // ==============================

        private void RenderClientView(BaguaGameState state)
        {
            var v = _view;
            v.HostPanel?.SetActive(false);
            v.ClientPanel?.SetActive(true);
            v.WaitingText?.gameObject.SetActive(false);

            // 任务横幅
            if (v.TaskBannerText != null)
            {
                v.TaskBannerText.gameObject.SetActive(true);
            }

            if (state.step == BaguaStep.HostIdentifyPeople || state.clientComplete)
            {
                // 等待女儿认人：先更新卡片显示配对结果，再隐藏交互
                UpdateCharacterCards(state);
                HideAllDesktopItems();
                HideAllCharacterCardInteractions();
                if (v.ClientWaitingText != null)
                {
                    v.ClientWaitingText.gameObject.SetActive(true);
                }
                return;
            }

            v.ClientWaitingText?.gameObject.SetActive(false);

            // 更新桌面物件
            UpdateDesktopItems(state);

            // 更新人物卡
            UpdateCharacterCards(state);
        }

        private void UpdateDesktopItems(BaguaGameState state)
        {
            if (_view.DesktopItemSlots == null) return;
            var placements = _config.ItemPlacements;
            if (placements == null) return;

            foreach (var kvp in _itemSlotMap)
            {
                var itemId = kvp.Key;
                var index = kvp.Value;
                if (index >= _view.DesktopItemSlots.Length) continue;

                var slot = _view.DesktopItemSlots[index];
                if (slot.root == null) continue;

                // 找到该物品的 placement
                var placement = FindPlacement(itemId);
                if (placement.Equals(default(ItemPlacement))) continue;

                // 已配对的正确物品从桌面消失
                bool matched = placement.isCorrect && state.matchedCharacterIds.Contains(placement.characterId);
                slot.root.SetActive(!matched);

                if (!matched)
                {
                    var rect = slot.root.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchoredPosition = placement.anchoredPosition + new Vector2(0, 60);
                        rect.localScale = Vector3.one;
                        rect.localRotation = Quaternion.identity;
                    }
                }
            }
        }

        private void UpdateCharacterCards(BaguaGameState state)
        {
            if (_view.CharacterCards == null || _config.Entries == null) return;

            foreach (var kvp in _cardIndexMap)
            {
                var characterId = kvp.Key;
                var index = kvp.Value;
                if (index >= _view.CharacterCards.Length) continue;

                var card = _view.CharacterCards[index];
                if (card.root == null) continue;

                var entry = _config.FindEntry(characterId);
                if (entry == null) continue;

                var matched = state.matchedCharacterIds.Contains(characterId);
                var heard = state.heardStoryIds.Contains(characterId);

                // 卡片背景色
                if (card.cardImage != null)
                {
                    card.cardImage.color = matched
                        ? new Color(0.3f, 0.45f, 0.3f, 0.95f)
                        : new Color(0.38f, 0.31f, 0.23f, 0.95f);
                }

                // 声音按钮
                if (card.audioButtonRoot != null)
                {
                    card.audioButtonRoot.SetActive(!matched);
                }

                // 虚线槽颜色
                if (card.dropSlotRect != null)
                {
                    var slotImg = card.dropSlotRect.GetComponent<Image>();
                    if (slotImg != null)
                    {
                        slotImg.color = heard
                            ? new Color(1f, 0.9f, 0.5f, 0.25f)
                            : new Color(1f, 1f, 1f, 0.06f);
                    }
                    card.dropSlotRect.gameObject.SetActive(!matched);
                }

                // 已填入物品
                if (card.filledItemRoot != null)
                {
                    card.filledItemRoot.SetActive(matched);
                    if (matched)
                    {
                        var matchedItem = FindMatchedItem(characterId);
                        if (card.filledItemImage != null)
                        {
                            var filledSprite = matchedItem.filledSprite;
#if UNITY_EDITOR
                            if (filledSprite == null && !string.IsNullOrEmpty(matchedItem.characterId))
                            {
                                filledSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                                    "Assets/_Project/Art/勿忘我图片1/72d26e157911b066ff42692fdf2d6761.png");
                            }
#endif
                            if (filledSprite != null)
                            {
                                card.filledItemImage.sprite = filledSprite;
                                card.filledItemImage.color = Color.white;
                                card.filledItemImage.preserveAspect = true;
                            }
                            else if (matchedItem.sprite != null)
                            {
                                card.filledItemImage.sprite = matchedItem.sprite;
                                card.filledItemImage.color = Color.white;
                                card.filledItemImage.preserveAspect = true;
                            }
                            else
                            {
                                card.filledItemImage.color = new Color(0.7f, 0.58f, 0.4f);
                            }
                        }
                        if (card.filledItemNameText != null && !string.IsNullOrEmpty(matchedItem.displayName))
                        {
                            card.filledItemNameText.text = matchedItem.displayName;
                        }
                    }
                }
            }
        }

        // ==============================
        // 女儿端渲染
        // ==============================

        private void RenderHostView(BaguaGameState state)
        {
            var v = _view;
            v.ClientPanel?.SetActive(false);
            v.HostPanel?.SetActive(true);
            v.WaitingText?.gameObject.SetActive(false);

            if (v.HostRoleText != null)
            {
                v.HostRoleText.gameObject.SetActive(true);
            }

            if (state.step == BaguaStep.ClientMatchItems)
            {
                // 等待母亲
                if (v.HostWaitingText != null)
                {
                    v.HostWaitingText.gameObject.SetActive(true);
                }
                HidePhotoView();
            }
            else if (state.step == BaguaStep.HostIdentifyPeople)
            {
                v.HostWaitingText?.gameObject.SetActive(false);
                RenderPhotoView(state);
            }
        }

        private void RenderPhotoView(BaguaGameState state)
        {
            var v = _view;

            // 照片背景
            if (v.PhotoBackgroundImage != null)
            {
                v.PhotoBackgroundImage.sprite = _config.DaughterPhotoBackground;
                v.PhotoBackgroundImage.color = Color.white;
                v.PhotoBackgroundImage.gameObject.SetActive(true);
            }

            // 指令
            if (v.PhotoInstructionText != null)
            {
                v.PhotoInstructionText.gameObject.SetActive(true);
            }

            // 照片投放区
            if (v.PhotoZones != null)
            {
                var zones = _config.PhotoZones;
                if (zones != null)
                {
                    foreach (var kvp in _photoZoneIndexMap)
                    {
                        var characterId = kvp.Key;
                        var index = kvp.Value;
                        if (index >= v.PhotoZones.Length) continue;

                        var zone = v.PhotoZones[index];
                        if (zone.root == null) continue;

                        zone.root.SetActive(true);

                        // 找到该角色对应的 zoneId
                        var zoneConfig = FindZoneForCharacter(characterId);
                        if (zoneConfig == null) continue;

                        var assigned = state.assignedPhotoZoneIds.Contains(zoneConfig.Value.zoneId);
                        if (zone.image != null)
                        {
                            zone.image.color = assigned
                                ? new Color(0.3f, 0.6f, 0.3f, 0.8f)
                                : (_config.PhotoZoneSprite != null ? new Color(1f, 1f, 1f, 0.5f) : new Color(1f, 1f, 1f, 0.08f));
                        }
                    }
                }
            }

            // 姓名标签
            if (v.NameTagSlots != null)
            {
                var entries = _config.Entries;
                if (entries != null)
                {
                    foreach (var kvp in _nameTagIndexMap)
                    {
                        var characterId = kvp.Key;
                        var index = kvp.Value;
                        if (index >= v.NameTagSlots.Length) continue;

                        var slot = v.NameTagSlots[index];
                        if (slot.root == null) continue;

                        var entry = _config.FindEntry(characterId);
                        if (entry == null) continue;

                        var zoneConfig = FindZoneForCharacter(characterId);
                        var assigned = zoneConfig != null && state.assignedPhotoZoneIds.Contains(zoneConfig.Value.zoneId);

                        if (assigned)
                        {
                            // 已投放：移动到投放区位置并隐藏拖拽
                            var fixedPos = zoneConfig.Value.anchoredPosition + new Vector2(0, 60);
                            var rect = slot.root.GetComponent<RectTransform>();
                            if (rect != null) rect.anchoredPosition = fixedPos;
                            slot.root.SetActive(true);
                            if (slot.image != null)
                            {
                                if (entry.nameTagSprite != null)
                                {
                                    slot.image.sprite = entry.nameTagSprite;
                                    slot.image.color = Color.white;
                                }
                                else
                                {
                                    slot.image.color = new Color(0.4f, 0.6f, 0.35f);
                                }
                            }
                            if (slot.draggable != null) slot.draggable.enabled = false;
                        }
                        else
                        {
                            // 未投放：回到原位
                            var tagPos = new Vector2(-350 + index * 350, -380);
                            var rect = slot.root.GetComponent<RectTransform>();
                            if (rect != null)
                            {
                                rect.anchoredPosition = tagPos;
                                rect.localScale = Vector3.one;
                                rect.localRotation = Quaternion.identity;
                            }
                            slot.root.SetActive(true);
                            if (slot.draggable != null) slot.draggable.enabled = true;
                        }
                    }
                }
            }
        }

        private void HidePhotoView()
        {
            var v = _view;
            v.PhotoBackgroundImage?.gameObject.SetActive(false);
            v.PhotoInstructionText?.gameObject.SetActive(false);
            if (v.PhotoZones != null)
            {
                foreach (var zone in v.PhotoZones)
                {
                    if (zone.root != null) zone.root.SetActive(false);
                }
            }
            if (v.NameTagSlots != null)
            {
                foreach (var slot in v.NameTagSlots)
                {
                    if (slot.root != null) slot.root.SetActive(false);
                }
            }
        }

        // ==============================
        // 完成视图
        // ==============================

        private void RenderCompleteView()
        {
            var v = _view;
            v.ClientPanel?.SetActive(false);
            v.HostPanel?.SetActive(false);
            v.WaitingText?.gameObject.SetActive(false);

            if (v.CompleteText != null)
            {
                v.CompleteText.gameObject.SetActive(true);
            }

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
                v.CollectedText?.gameObject.SetActive(true);
                ShowNextLevelButton();
            }
            else
            {
                ShowNextLevelButton();
            }
        }

        private void ShowRewardPhoto(string photoId)
        {
            var v = _view;
            v.RewardPhotoImage?.gameObject.SetActive(true);
            v.PhotoLabelText?.gameObject.SetActive(true);

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

            v.CollectedText?.gameObject.SetActive(false);
        }

        private void HideRewardPhoto()
        {
            var v = _view;
            v.RewardPhotoImage?.gameObject.SetActive(false);
            v.PhotoLabelText?.gameObject.SetActive(false);
            v.CollectButtonRoot?.gameObject.SetActive(false);
            v.CollectedText?.gameObject.SetActive(false);
            StopCollectGlow();
        }

        /// <summary>完成照片收集后延迟自动收集，无需手动点击。</summary>
        private IEnumerator AutoCollectPhoto(string photoId)
        {
            yield return new WaitForSecondsRealtime(2f);
            if (_coordinator != null && _coordinator.State.phase == GameplayPhase.MiniGame)
            {
                _coordinator.Request(new GameplayIntent(
                    GameplayIntentType.CollectMiniGamePhoto, null, photoId));
            }
        }

        /// <summary>显示进入下一关的按钮。</summary>
        private void ShowNextLevelButton()
        {
            if (_nextLevelButton != null) return;

            var sprite = LoadNextLevelSprite();
            var btnGo = new GameObject("NextLevelButton", typeof(Image), typeof(Button));
            btnGo.transform.SetParent(_content.transform, false);
            var rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, -300);
            rect.sizeDelta = new Vector2(400, 120);
            rect.localScale = Vector3.one;

            var img = btnGo.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
                img.preserveAspect = true;
            }
            else
            {
                img.color = new Color(0.5f, 0.35f, 0.15f, 0.95f);
            }

            _nextLevelButton = btnGo.GetComponent<Button>();
            _nextLevelButton.onClick.AddListener(() =>
            {
                _coordinator.Request(new GameplayIntent(GameplayIntentType.FinishMiniGame));
            });
        }

        private Sprite LoadNextLevelSprite()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Art/勿忘我图片1/组 15.png");
#else
            return null;
#endif
        }

        // ==============================
        // 声音按钮交互
        // ==============================

        private void OnAudioButtonClicked(string characterId)
        {
            AudioManager.Play(SfxId.BaguaAudioButton);
            var entry = _config.FindEntry(characterId);
            if (entry == null) return;

            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
            if (_subtitleRoutine != null)
            {
                StopCoroutine(_subtitleRoutine);
                _subtitleRoutine = null;
                _playingCharacterId = null;
            }

            _playingCharacterId = characterId;

            if (entry.storyAudio != null)
            {
                _audioSource.clip = entry.storyAudio;
                _audioSource.volume = entry.audioVolume;
                _audioSource.Play();
                StartCoroutine(OnAudioCompleteRoutine(characterId));
            }
            else
            {
                var subtitle = entry.subtitle ?? "";
                var duration = subtitle.Length / 5f + 1f;
                _subtitleRoutine = StartCoroutine(OnSubtitleCompleteRoutine(characterId, subtitle, duration));
            }
        }

        private IEnumerator OnAudioCompleteRoutine(string characterId)
        {
            while (_audioSource != null && _audioSource.isPlaying)
            {
                yield return null;
            }
            if (_playingCharacterId == characterId)
            {
                _playingCharacterId = null;
                _coordinator.Request(new GameplayIntent(GameplayIntentType.MarkBaguaStoryHeard,
                    _config.MiniGameId, null, characterId, null));
            }
        }

        private IEnumerator OnSubtitleCompleteRoutine(string characterId, string subtitle, float duration)
        {
            // 显示字幕条
            if (_view.SubtitleBarRoot != null && _view.SubtitleText != null)
            {
                _view.SubtitleText.text = subtitle;
                _view.SubtitleBarRoot.SetActive(true);
            }

            yield return new WaitForSeconds(duration);

            // 隐藏字幕条
            if (_view.SubtitleBarRoot != null)
            {
                _view.SubtitleBarRoot.SetActive(false);
            }

            if (_playingCharacterId == characterId)
            {
                _playingCharacterId = null;
                _coordinator.Request(new GameplayIntent(GameplayIntentType.MarkBaguaStoryHeard,
                    _config.MiniGameId, null, characterId, null));
            }
            _subtitleRoutine = null;
        }

        // ==============================
        // 回调绑定
        // ==============================

        private void WireCallbacks()
        {
            if (_callbacksWired) return;
            _callbacksWired = true;

            // 桌面物品拖拽
            if (_view.DesktopItemSlots != null)
            {
                foreach (var kvp in _itemSlotMap)
                {
                    var itemId = kvp.Key;
                    var index = kvp.Value;
                    if (index >= _view.DesktopItemSlots.Length) continue;

                    var slot = _view.DesktopItemSlots[index];
                    if (slot.draggable == null) continue;

                    var placement = FindPlacement(itemId);
                    if (placement.Equals(default(ItemPlacement))) continue;

                    var capturedItemId = itemId;
                    var capturedCharacterId = placement.characterId;
                    slot.draggable.OnEndDragEvent += (eventData, _) =>
                    {
                        // 查找投放到哪个人物卡
                        var targetCharacterId = FindCardDropZoneAt(eventData.position);
                        if (targetCharacterId == null)
                        {
                            AudioManager.Play(SfxId.BaguaWrongMatch);
                            slot.draggable.ReturnToOrigin();
                            return;
                        }

                        _coordinator.Request(new GameplayIntent(GameplayIntentType.MatchBaguaItem,
                            _config.MiniGameId, capturedItemId, targetCharacterId, null));

                        if (!_coordinator.State.bagua.matchedCharacterIds.Contains(targetCharacterId))
                        {
                            AudioManager.Play(SfxId.BaguaWrongMatch);
                            slot.draggable.ReturnToOrigin();
                            ShakeRect(slot.root.GetComponent<RectTransform>());
                        }
                    };
                }
            }

            // 声音按钮
            if (_view.CharacterCards != null)
            {
                foreach (var kvp in _cardIndexMap)
                {
                    var characterId = kvp.Key;
                    var index = kvp.Value;
                    if (index >= _view.CharacterCards.Length) continue;

                    var card = _view.CharacterCards[index];
                    if (card.audioButton == null) continue;

                    var capturedId = characterId;
                    card.audioButton.onClick.RemoveAllListeners();
                    card.audioButton.onClick.AddListener(() => OnAudioButtonClicked(capturedId));
                }
            }

            // 姓名标签拖拽
            if (_view.NameTagSlots != null)
            {
                foreach (var kvp in _nameTagIndexMap)
                {
                    var characterId = kvp.Key;
                    var index = kvp.Value;
                    if (index >= _view.NameTagSlots.Length) continue;

                    var slot = _view.NameTagSlots[index];
                    if (slot.draggable == null) continue;

                    var capturedId = characterId;
                    slot.draggable.OnEndDragEvent += (eventData, _) =>
                    {
                        var droppedZoneId = FindPhotoZoneAt(eventData.position);
                        if (droppedZoneId != null)
                        {
                            _coordinator.Request(new GameplayIntent(GameplayIntentType.AssignBaguaPhotoName,
                                _config.MiniGameId, null, capturedId, droppedZoneId));

                            if (!_coordinator.State.bagua.assignedPhotoZoneIds.Contains(droppedZoneId))
                            {
                                slot.draggable.ReturnToOrigin();
                            }
                        }
                        else
                        {
                            slot.draggable.ReturnToOrigin();
                        }
                    };
                }
            }
        }

        // ==============================
        // 拖拽校验
        // ==============================

        private string FindCardDropZoneAt(Vector2 screenPosition)
        {
            if (_view.CharacterCards == null) return null;
            foreach (var kvp in _cardIndexMap)
            {
                var index = kvp.Value;
                if (index >= _view.CharacterCards.Length) continue;
                var card = _view.CharacterCards[index];
                if (card.dropSlotRect == null || !card.dropSlotRect.gameObject.activeSelf) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(card.dropSlotRect, screenPosition, null))
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        private string FindPhotoZoneAt(Vector2 screenPosition)
        {
            if (_view.PhotoZones == null) return null;
            foreach (var kvp in _photoZoneIndexMap)
            {
                var index = kvp.Value;
                if (index >= _view.PhotoZones.Length) continue;
                var zone = _view.PhotoZones[index];
                if (zone.dropZone == null || !zone.root.activeSelf) continue;
                if (zone.dropZone.Contains(screenPosition))
                {
                    // 返回 zoneId
                    var zoneConfig = FindZoneForCharacter(kvp.Key);
                    return zoneConfig?.zoneId;
                }
            }
            return null;
        }

        // ==============================
        // Slot 映射
        // ==============================

        private void BuildSlotMaps()
        {
            var placements = _config.ItemPlacements;
            if (placements != null)
            {
                for (var i = 0; i < placements.Length; i++)
                {
                    if (!_itemSlotMap.ContainsKey(placements[i].itemId))
                        _itemSlotMap[placements[i].itemId] = i;
                }
            }

            var entries = _config.Entries;
            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    if (entries[i] != null && !_cardIndexMap.ContainsKey(entries[i].characterId))
                        _cardIndexMap[entries[i].characterId] = i;
                }
            }

            var zones = _config.PhotoZones;
            if (zones != null)
            {
                for (var i = 0; i < zones.Length; i++)
                {
                    if (!_photoZoneIndexMap.ContainsKey(zones[i].correctCharacterId))
                        _photoZoneIndexMap[zones[i].correctCharacterId] = i;
                }
            }

            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    if (entries[i] != null && !_nameTagIndexMap.ContainsKey(entries[i].characterId))
                        _nameTagIndexMap[entries[i].characterId] = i;
                }
            }
        }

        // ==============================
        // 辅助
        // ==============================

        private void SetAllInactive()
        {
            var v = _view;
            v.Background?.gameObject.SetActive(false);
            v.ClientPanel?.SetActive(false);
            v.HostPanel?.SetActive(false);
            v.WaitingText?.gameObject.SetActive(false);
            v.CompleteText?.gameObject.SetActive(false);
            v.RewardPhotoImage?.gameObject.SetActive(false);
            v.PhotoLabelText?.gameObject.SetActive(false);
            v.CollectButtonRoot?.gameObject.SetActive(false);
            v.CollectedText?.gameObject.SetActive(false);
            v.SubtitleBarRoot?.SetActive(false);
        }

        private void HideAllDesktopItems()
        {
            if (_view.DesktopItemSlots == null) return;
            foreach (var slot in _view.DesktopItemSlots)
            {
                if (slot.root != null) slot.root.SetActive(false);
            }
        }

        private void HideAllCharacterCardInteractions()
        {
            if (_view.CharacterCards == null) return;
            foreach (var card in _view.CharacterCards)
            {
                if (card.audioButtonRoot != null) card.audioButtonRoot.SetActive(false);
            }
        }

        private ItemPlacement FindPlacement(string itemId)
        {
            if (_config.ItemPlacements == null) return default;
            foreach (var item in _config.ItemPlacements)
            {
                if (item.itemId == itemId) return item;
            }
            return default;
        }

        private ItemPlacement FindMatchedItem(string characterId)
        {
            if (_config.ItemPlacements == null) return default;
            foreach (var item in _config.ItemPlacements)
            {
                if (item.isCorrect && item.characterId == characterId) return item;
            }
            return default;
        }

        private BaguaStoryConfig.PhotoZoneConfig? FindZoneForCharacter(string characterId)
        {
            if (_config.PhotoZones == null) return null;
            foreach (var zone in _config.PhotoZones)
            {
                if (zone.correctCharacterId == characterId) return zone;
            }
            return null;
        }

        private string GetRewardPhotoId()
        {
            if (_config?.RewardIds == null) return null;
            return System.Array.Find(_config.RewardIds,
                r => r != null && r.IndexOf("photo", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void StopCollectGlow()
        {
            if (_collectGlowRoutine != null)
            {
                StopCoroutine(_collectGlowRoutine);
                _collectGlowRoutine = null;
            }
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
