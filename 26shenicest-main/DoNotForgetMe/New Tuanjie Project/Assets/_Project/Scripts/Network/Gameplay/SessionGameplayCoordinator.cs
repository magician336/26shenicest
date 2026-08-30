using System;
using System.Collections;
using System.Collections.Generic;
using DoNotForgetMe.Core;
using DoNotForgetMe.Dialogue;
using DoNotForgetMe.MiniGame.Album;
using DoNotForgetMe.MiniGame.Bagua;
using DoNotForgetMe.MiniGame.Cooking;
using DoNotForgetMe.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoNotForgetMe.Network.Gameplay
{
    /// <summary>Host 权威的游戏流程状态机；所有有效状态变化都从这里产生。
    /// 跨场景持久化：通过 DontDestroyOnLoad 在 CookingScene → BaguaScene 切换时保持状态。</summary>
    public class SessionGameplayCoordinator : MonoBehaviour
    {
        public static SessionGameplayCoordinator Instance { get; private set; }

        [SerializeField] private RecipeConfig[] recipes;
        [SerializeField] private BaguaStoryConfig[] baguaConfigs;
        [SerializeField] private AlbumConfig[] albumConfigs;
        [SerializeField] private DialogueSequence[] dialogueConfigs;

        [Tooltip("单进程调试模式：绕过 Host 权限检查，允许本地切换角色测试双端流程")]
        [SerializeField] private bool debugSingleProcess = false;

        [Tooltip("场景加载后自动播放的开场对白序列 ID（留空则不自动播放）")]
        [SerializeField] private string openingDialogueId;

        public GameplaySnapshot State { get; private set; } = new();
        public event Action<GameplaySnapshot> StateChanged;
        public event Action<string> FeedbackRequested;

        private readonly HashSet<string> _collectedRewards = new();
        private IGameplayTransport _transport;
        private GameplaySnapshot _lastStableState = new();
        private BaguaSessionLogic _baguaLogic;
        private string _nextRecipeAfterPhoto;
        private string _nextDialogueAfterPhoto;
        private bool _cookingChainCompleted;
        private bool _baguaChainCompleted;

        /// <summary>当 Coordinator 需要注册 transport 但尚未注册时触发（Fusion 侧订阅后 spawn bridge）。</summary>
        public static event Action<SessionGameplayCoordinator> OnTransportNeeded;

        public bool IsHostAuthority => NetworkSessionManager.Service.Role == SessionRole.Host;
        public bool IsMother => NetworkSessionManager.Service.Role == SessionRole.Client;
        public bool IsDaughter => NetworkSessionManager.Service.Role == SessionRole.Host;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            EnsureDependencies();
            InitializeSessionState();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneNames.MainMenu) return;
            EnsureDependencies();
            InitializeSessionState();
        }

        /// <summary>确保 MemoryAlbumController 和 DialogueController 存在（场景切换后可能被销毁）。</summary>
        private void EnsureDependencies()
        {
            if (FindObjectOfType<DoNotForgetMe.UI.MemoryAlbumController>() == null)
            {
                var prefab = Resources.Load<GameObject>("UIPrefabs/MemoryAlbumView");
                GameObject go;
                if (prefab != null)
                {
                    go = Instantiate(prefab);
                }
                else
                {
                    go = new GameObject("MemoryAlbumController");
                }
                go.name = "MemoryAlbumController";
                if (go.GetComponent<DoNotForgetMe.UI.MemoryAlbumController>() == null)
                    go.AddComponent<DoNotForgetMe.UI.MemoryAlbumController>();
                DontDestroyOnLoad(go);
            }

            if (FindObjectOfType<DialogueController>() == null)
            {
                var go = new GameObject("DialogueController");
                go.AddComponent<DialogueController>();
                DontDestroyOnLoad(go);
            }

            // 通知 Fusion 侧 spawn bridge（如果尚未注册 transport 且网络层可用）
            if (_transport == null && NetworkSessionManager.Service.IsAvailable)
            {
                OnTransportNeeded?.Invoke(this);
            }
        }

        /// <summary>初始化 bagua 逻辑、存档恢复、开场对白。每次场景加载后调用。</summary>
        private void InitializeSessionState()
        {
            if (baguaConfigs != null && baguaConfigs.Length > 0 && baguaConfigs[0] != null)
            {
                _baguaLogic = new BaguaSessionLogic(baguaConfigs[0]);
            }

            var pendingSave = HostSaveContext.Consume();
            if (pendingSave != null)
            {
                RestoreHostSave(pendingSave);
            }

            if (IsHostAuthority && !string.IsNullOrEmpty(openingDialogueId)
                && State.phase == GameplayPhase.Exploration
                && State.collectedPhotoIds.Count == 0
                && !State.dialogue.IsActive)
            {
                StartDialogue(openingDialogueId);
            }
            else if (IsHostAuthority
                && State.phase == GameplayPhase.Exploration
                && State.collectedPhotoIds.Count >= 3
                && !State.dialogue.IsActive)
            {
                // 八卦完成后回到 LivingRoom：播放相册前系统提示对白
                StartDialogue("DLG_AlbumPrompt");
            }
        }

        public void RegisterTransport(IGameplayTransport transport)
        {
            _transport = transport;
            if (IsHostAuthority && _transport != null)
            {
                _transport.BroadcastState(State.Clone());
            }
        }

        public void RestoreHostSave(GameProgressSave save)
        {
            if (!IsHostAuthority || save == null) return;

            var snapshot = new GameplaySnapshot();

            if (save.cookingState != null)
            {
                snapshot.cooking = save.cookingState.Clone();
                snapshot.phase = save.cookingState.phase;
                snapshot.miniGameId = save.cookingState.recipeId;
            }

            if (save.baguaState != null)
            {
                snapshot.bagua = save.baguaState.Clone();
                if (save.baguaState.phase != GameplayPhase.Exploration)
                {
                    snapshot.phase = save.baguaState.phase;
                    snapshot.miniGameId = save.baguaMiniGameId;
                }
            }

            if (save.albumState != null)
            {
                snapshot.album = save.albumState.Clone();
                if (save.albumState.phase == GameplayPhase.MiniGame)
                {
                    snapshot.phase = save.albumState.phase;
                    snapshot.miniGameId = save.albumMiniGameId;
                }
                else if (save.albumState.phase == GameplayPhase.GameEnded)
                {
                    snapshot.phase = GameplayPhase.GameEnded;
                }
            }

            _collectedRewards.Clear();
            foreach (var rewardId in save.collectedRewardIds ?? Array.Empty<string>())
            {
                if (IsMemoryPhoto(rewardId)) snapshot.collectedPhotoIds.Add(rewardId);
                else _collectedRewards.Add(rewardId);
            }
            snapshot.pendingPhotoId = save.pendingPhotoId;
            snapshot.previewPhotoId = save.previewPhotoId;
            State = snapshot;
            _lastStableState = State.Clone();
            PublishState(false);
        }

        public void ApplyAuthoritativeState(GameplaySnapshot snapshot)
        {
            if (snapshot == null) return;
            State = snapshot.Clone();
            StateChanged?.Invoke(State.Clone());
        }

        public void Request(GameplayIntent intent)
        {
            if (IsHostAuthority)
            {
                HandleHostIntent(intent, SessionRole.Host);
                return;
            }

            _transport?.SendIntent(intent);
        }

        /// <summary>只能由 Fusion Host RPC 回调或本地 Host 调用。</summary>
        public void HandleHostIntent(GameplayIntent intent, SessionRole requester)
        {
            if (!IsHostAuthority && !debugSingleProcess) return;
            if (!debugSingleProcess && !CanRequesterPerform(intent.type, requester)) return;

            switch (intent.type)
            {
                // --- 做饭小游戏 ---
                case GameplayIntentType.StartMiniGame:
                    StartMiniGame(intent.recipeId);
                    break;
                case GameplayIntentType.SelectIngredient:
                    SelectIngredient(intent.itemId);
                    break;
                case GameplayIntentType.DropIngredient:
                    DropIngredient(intent.itemId);
                    break;
                case GameplayIntentType.SelectSeasoning:
                    SelectSeasoning(intent.itemId);
                    break;
                case GameplayIntentType.RequestHint:
                    RequestHint();
                    break;
                case GameplayIntentType.ShowHint:
                    ShowNextHint();
                    break;
                case GameplayIntentType.FinishMiniGame:
                    FinishMiniGame();
                    break;

                // --- 八卦小游戏 ---
                case GameplayIntentType.StartBaguaMiniGame:
                    StartBaguaMiniGame(intent.recipeId);
                    break;
                case GameplayIntentType.MarkBaguaStoryHeard:
                    MarkBaguaStoryHeard(intent.characterId);
                    break;
                case GameplayIntentType.MatchBaguaItem:
                    MatchBaguaItem(intent.characterId, intent.itemId);
                    break;
                case GameplayIntentType.AssignBaguaPhotoName:
                    AssignBaguaPhotoName(intent.zoneId, intent.characterId);
                    break;

                case GameplayIntentType.CollectMemoryPhoto:
                    CollectMemoryPhoto(intent.itemId);
                    break;
                case GameplayIntentType.CollectMiniGamePhoto:
                    CollectMiniGamePhoto(intent.itemId);
                    break;
                case GameplayIntentType.CloseMemoryPhotoPreview:
                    CloseMemoryPhotoPreview();
                    break;

                // --- 全家福相册小游戏 ---
                case GameplayIntentType.StartAlbumMiniGame:
                    StartAlbumMiniGame(intent.recipeId);
                    break;
                case GameplayIntentType.PlaceAlbumSticker:
                    PlaceAlbumSticker(intent.characterId, intent.zoneId);
                    break;
                case GameplayIntentType.PlaceAlbumNameTag:
                    PlaceAlbumNameTag(intent.characterId, intent.zoneId);
                    break;

                // --- 对白序列 ---
                case GameplayIntentType.StartDialogue:
                    StartDialogue(intent.dialogueSequenceId);
                    break;
                case GameplayIntentType.AdvanceDialogue:
                    AdvanceDialogue();
                    break;
                case GameplayIntentType.FinishDialogue:
                    FinishDialogue();
                    break;
            }
        }

        // ==============================
        // 做饭小游戏逻辑
        // ==============================

        private void StartMiniGame(string recipeId)
        {
            if (State.phase != GameplayPhase.Exploration || FindRecipe(recipeId) == null)
            {
                Debug.Log($"[Coordinator] StartMiniGame blocked: phase={State.phase} recipeFound={FindRecipe(recipeId) != null}");
                return;
            }

            State.phase = GameplayPhase.MiniGame;
            State.miniGameId = recipeId;
            State.cooking = new CookingGameState
            {
                phase = GameplayPhase.MiniGame,
                recipeId = recipeId,
                step = CookingStep.MotherSelectIngredients
            };
            Debug.Log($"[Coordinator] StartMiniGame: recipeId={recipeId} collectedPhotoIds=[{string.Join(", ", State.collectedPhotoIds)}]");
            PublishStableState();
        }

        private void SelectIngredient(string itemId)
        {
            var recipe = CurrentRecipe;
            var cooking = State.cooking;
            if (recipe == null || State.phase != GameplayPhase.MiniGame || cooking.step != CookingStep.MotherSelectIngredients) return;

            if (!recipe.IsRequiredIngredient(itemId) || cooking.selectedIngredients.Contains(itemId))
            {
                FeedbackRequested?.Invoke("wrong_select");
                return;
            }

            cooking.selectedIngredients.Add(itemId);
            cooking.droppedIngredients.Add(itemId);

            if (cooking.selectedIngredients.Count == recipe.RequiredIngredients.Length)
            {
                cooking.motherFoodComplete = true;
                cooking.daughterUnlocked = true;
                cooking.step = CookingStep.DaughterSeason;
                PublishStableState();
                return;
            }
            PublishState(false);
        }

        private void DropIngredient(string itemId)
        {
            var recipe = CurrentRecipe;
            var cooking = State.cooking;
            if (recipe == null || State.phase != GameplayPhase.MiniGame || cooking.step != CookingStep.MotherDropIngredients) return;

            if (!cooking.selectedIngredients.Contains(itemId) || cooking.droppedIngredients.Contains(itemId))
            {
                FeedbackRequested?.Invoke("wrong_drop");
                return;
            }

            cooking.droppedIngredients.Add(itemId);
            if (cooking.droppedIngredients.Count != recipe.RequiredIngredients.Length)
            {
                PublishState(false);
                return;
            }

            cooking.motherFoodComplete = true;
            cooking.daughterUnlocked = true;
            cooking.step = CookingStep.DaughterSeason;
            PublishStableState();
        }

        private void SelectSeasoning(string itemId)
        {
            var recipe = CurrentRecipe;
            var cooking = State.cooking;
            if (recipe == null || State.phase != GameplayPhase.MiniGame || cooking.step != CookingStep.DaughterSeason) return;

            if (!recipe.IsCorrectSeasoning(itemId))
            {
                FeedbackRequested?.Invoke("wrong_seasoning");
                return;
            }

            cooking.selectedSeasoning = itemId;
            cooking.daughterSeasoningComplete = true;
            cooking.completed = true;
            cooking.step = CookingStep.Complete;
            // 延迟到 FinishMiniGame 时再发奖励，避免照片收集按钮在小游戏期间出现在探索界面
            PublishStableState();
        }

        private void RequestHint()
        {
            var cooking = State.cooking;
            if (State.phase != GameplayPhase.MiniGame || cooking.completed) return;
            cooking.hintRequested = true;
            PublishState(false);
        }

        private void ShowNextHint()
        {
            var recipe = CurrentRecipe;
            var cooking = State.cooking;
            if (recipe == null || !cooking.hintRequested || cooking.hintLevel >= recipe.HintTexts.Length) return;

            cooking.hintRequested = false;
            cooking.hintLevel++;
            PublishState(false);
        }

        // ==============================
        // 八卦小游戏逻辑
        // ==============================

        private void StartBaguaMiniGame(string configId)
        {
            var config = FindBaguaConfig(configId);
            if (config == null || State.phase != GameplayPhase.Exploration) return;

            _baguaLogic = new BaguaSessionLogic(config);
            State.phase = GameplayPhase.MiniGame;
            State.miniGameId = configId;
            State.bagua = new BaguaGameState
            {
                phase = GameplayPhase.MiniGame,
                step = BaguaStep.ClientMatchItems
            };
            PublishStableState();
        }

        private void MarkBaguaStoryHeard(string characterId)
        {
            if (_baguaLogic == null) return;
            var result = _baguaLogic.MarkStoryHeard(State.bagua, characterId);
            if (result == BaguaSessionLogic.PublishKind.NonStable) PublishState(false);
        }

        private void MatchBaguaItem(string characterId, string itemId)
        {
            if (_baguaLogic == null) return;
            var result = _baguaLogic.MatchItem(State.bagua, characterId, itemId, out var feedback);
            if (feedback != null) FeedbackRequested?.Invoke(feedback);
            if (result == BaguaSessionLogic.PublishKind.Stable)
            {
                State.bagua.phase = State.phase;
                PublishStableState();
            }
        }

        private void AssignBaguaPhotoName(string zoneId, string characterId)
        {
            if (_baguaLogic == null) return;
            var result = _baguaLogic.AssignPhotoName(State.bagua, zoneId, characterId, out var feedback);
            if (feedback != null) FeedbackRequested?.Invoke(feedback);
            if (result == BaguaSessionLogic.PublishKind.Stable)
            {
                State.bagua.phase = State.phase;
                // 延迟到 FinishMiniGame 时再发奖励
                PublishStableState();
            }
        }

        // ==============================
        // 全家福相册小游戏逻辑
        // ==============================

        private void StartAlbumMiniGame(string configId)
        {
            var config = FindAlbumConfig(configId);
            if (config == null || State.phase != GameplayPhase.Exploration) return;

            // 前置照片检查
            if (!AreAlbumPhotosCollected(config)) return;

            State.phase = GameplayPhase.MiniGame;
            State.miniGameId = configId;
            State.album = new AlbumGameState
            {
                phase = GameplayPhase.MiniGame,
                step = AlbumStep.PlaceStickers
            };
            PublishStableState();
        }

        private void PlaceAlbumSticker(string characterId, string zoneCharacterId)
        {
            if (State.phase != GameplayPhase.MiniGame || !IsAlbumActive) return;
            if (State.album.step != AlbumStep.PlaceStickers) return;

            // 逻辑匹配：贴纸的 characterId 必须等于轮廓的 characterId
            if (characterId != zoneCharacterId)
            {
                FeedbackRequested?.Invoke("wrong_sticker");
                return;
            }

            var entry = FindAlbumConfig(State.miniGameId)?.FindEntry(characterId);
            if (entry == null || !entry.hasSticker) return;
            if (State.album.placedStickerCharacterIds.Contains(characterId)) return;

            State.album.placedStickerCharacterIds.Add(characterId);

            // 检查是否所有贴纸都已放入
            var stickerEntries = FindAlbumConfig(State.miniGameId).GetStickerEntries();
            if (State.album.placedStickerCharacterIds.Count >= stickerEntries.Length)
            {
                State.album.step = AlbumStep.PlaceNameTags;
            }

            PublishStableState();
        }

        private void PlaceAlbumNameTag(string characterId, string zoneCharacterId)
        {
            if (State.phase != GameplayPhase.MiniGame || !IsAlbumActive) return;
            if (State.album.step != AlbumStep.PlaceNameTags) return;

            if (characterId != zoneCharacterId)
            {
                FeedbackRequested?.Invoke("wrong_nametag");
                return;
            }

            var entry = FindAlbumConfig(State.miniGameId)?.FindEntry(characterId);
            if (entry == null || !entry.hasSticker) return;
            if (State.album.placedNameTagCharacterIds.Contains(characterId)) return;

            State.album.placedNameTagCharacterIds.Add(characterId);

            var stickerEntries = FindAlbumConfig(State.miniGameId).GetStickerEntries();
            if (State.album.placedNameTagCharacterIds.Count >= stickerEntries.Length)
            {
                State.album.step = AlbumStep.Complete;
                State.album.completed = true;
            }

            PublishStableState();
        }

        // ==============================
        // 对白序列逻辑
        // ==============================

        private void StartDialogue(string sequenceId)
        {
            var config = FindDialogueConfig(sequenceId);
            if (config == null || State.phase != GameplayPhase.Exploration)
            {
                Debug.Log($"[Coordinator] StartDialogue blocked: configFound={config != null} phase={State.phase}");
                return;
            }

            State.phase = GameplayPhase.Dialogue;
            State.dialogue.sequenceId = sequenceId;
            State.dialogue.currentEntryIndex = 0;
            PublishStableState();
        }

        private void AdvanceDialogue()
        {
            if (State.phase != GameplayPhase.Dialogue) return;
            var config = FindDialogueConfig(State.dialogue.sequenceId);
            if (config == null) return;

            if (State.dialogue.currentEntryIndex < config.EntryCount - 1)
            {
                State.dialogue.currentEntryIndex++;
                PublishState(false);
            }
            else
            {
                FinishDialogue();
            }
        }

        private void FinishDialogue()
        {
            if (State.phase != GameplayPhase.Dialogue) return;

            var config = FindDialogueConfig(State.dialogue.sequenceId);
            State.dialogue.Reset();
            State.phase = GameplayPhase.Exploration;
            PublishStableState();

            if (config != null && !string.IsNullOrEmpty(config.NextMiniGameId))
            {
                // 做饭小游戏 vs 相册小游戏
                if (FindRecipe(config.NextMiniGameId) != null)
                    StartMiniGame(config.NextMiniGameId);
                else if (FindAlbumConfig(config.NextMiniGameId) != null)
                    StartAlbumMiniGame(config.NextMiniGameId);
                return;
            }
            if (config != null && !string.IsNullOrEmpty(config.NextDialogueId))
            {
                StartDialogue(config.NextDialogueId);
                return;
            }
            if (config != null && !string.IsNullOrEmpty(config.NextSceneName))
            {
                TransitionToScene(config.NextSceneName);
                return;
            }
            if (config != null && config.TriggerGameEnded)
            {
                State.phase = GameplayPhase.GameEnded;
                PublishStableState();
                return;
            }
        }

        // ==============================
        // 共享逻辑（做饭 + 八卦）
        // ==============================

        private void FinishMiniGame()
        {
            if (State.phase != GameplayPhase.MiniGame) return;

            if (IsAlbumActive)
            {
                if (!State.album.completed) return;
                State.phase = GameplayPhase.Exploration;
                State.miniGameId = null;
                State.album.phase = GameplayPhase.Exploration;
                PublishStableState();
                StartDialogue("DLG_Ending");
                return;
            }

            if (IsBaguaActive)
            {
                if (!State.bagua.completed) return;
                _baguaLogic.FinishMiniGame(State.bagua);
                State.phase = GameplayPhase.Exploration;

                var baguaConfig = FindBaguaConfig(State.miniGameId);
                State.miniGameId = null;
                State.bagua.phase = GameplayPhase.Exploration;

                _nextDialogueAfterPhoto = baguaConfig != null ? baguaConfig.NextDialogueId : null;
                _baguaChainCompleted = string.IsNullOrEmpty(_nextDialogueAfterPhoto);

                PublishStableState();

                // 有后续对白 → 播放
                if (!string.IsNullOrEmpty(_nextDialogueAfterPhoto))
                {
                    var nextDialogue = _nextDialogueAfterPhoto;
                    _nextDialogueAfterPhoto = null;
                    StartDialogue(nextDialogue);
                    return;
                }

                // 八卦链完成 → 自动转场到 LivingRoom（相册小游戏）
                if (_baguaChainCompleted)
                {
                    _baguaChainCompleted = false;
                    TransitionToScene(SceneNames.LivingRoom);
                    return;
                }

                return;
            }

            if (!State.cooking.completed) return;

            var recipe = CurrentRecipe;
            _nextRecipeAfterPhoto = recipe != null ? recipe.NextRecipeId : null;
            _nextDialogueAfterPhoto = recipe != null ? recipe.NextDialogueId : null;
            _cookingChainCompleted = string.IsNullOrEmpty(_nextRecipeAfterPhoto)
                && string.IsNullOrEmpty(_nextDialogueAfterPhoto);

            State.phase = GameplayPhase.Exploration;
            State.miniGameId = null;
            State.cooking.phase = GameplayPhase.Exploration;
            PublishStableState();

            // 延迟一帧再启动链式流程，确保 MiniGameManager 先关闭旧 view
            if (!string.IsNullOrEmpty(_nextRecipeAfterPhoto))
            {
                var nextId = _nextRecipeAfterPhoto;
                _nextRecipeAfterPhoto = null;
                StartCoroutine(DelayedAction(() => StartMiniGame(nextId)));
                return;
            }

            // 有后续对白 → 播放
            if (!string.IsNullOrEmpty(_nextDialogueAfterPhoto))
            {
                var nextDialogue = _nextDialogueAfterPhoto;
                _nextDialogueAfterPhoto = null;
                StartCoroutine(DelayedAction(() => StartDialogue(nextDialogue)));
                return;
            }

            // 做饭链完成 → 不自动转场，玩家自己走到门按 F 进院子
        }

        private void CollectMemoryPhoto(string photoId)
        {
            if (State.phase != GameplayPhase.Exploration ||
                string.IsNullOrEmpty(photoId) || State.pendingPhotoId != photoId) return;

            State.pendingPhotoId = null;
            if (!State.collectedPhotoIds.Contains(photoId)) State.collectedPhotoIds.Add(photoId);
            State.previewPhotoId = photoId;
            PublishStableState();
        }

        /// <summary>小游戏内收集照片：加入 collectedPhotoIds 并发布状态，不设 previewPhotoId，不触发 FinishMiniGame。</summary>
        private void CollectMiniGamePhoto(string photoId)
        {
            if (State.phase != GameplayPhase.MiniGame || string.IsNullOrEmpty(photoId)) return;
            if (!State.collectedPhotoIds.Contains(photoId)) State.collectedPhotoIds.Add(photoId);
            PublishStableState();
        }

        private void CloseMemoryPhotoPreview()
        {
            if (string.IsNullOrEmpty(State.previewPhotoId)) return;
            State.previewPhotoId = null;

            if (!string.IsNullOrEmpty(_nextRecipeAfterPhoto))
            {
                var nextId = _nextRecipeAfterPhoto;
                _nextRecipeAfterPhoto = null;
                StartCoroutine(DelayedAction(() => StartMiniGame(nextId)));
                return;
            }

            if (!string.IsNullOrEmpty(_nextDialogueAfterPhoto))
            {
                var nextDialogue = _nextDialogueAfterPhoto;
                _nextDialogueAfterPhoto = null;
                StartCoroutine(DelayedAction(() => StartDialogue(nextDialogue)));
                return;
            }

            if (_cookingChainCompleted)
            {
                _cookingChainCompleted = false;
                PublishStableState();
                TransitionToScene(SceneNames.Courtyard);
                return;
            }

            if (_baguaChainCompleted)
            {
                _baguaChainCompleted = false;
                // 不自动转场 — 由 CourtyardEavesdropView 的"退出偷听"按钮手动回到 Kitchen
                PublishStableState();
                return;
            }

            PublishStableState();
        }

        /// <summary>跨场景转场：委托给 SceneLoader 统一处理守卫逻辑。</summary>
        private static void TransitionToScene(string sceneName)
        {
            DoNotForgetMe.Cutscene.SceneLoader.Load(sceneName);
        }

        /// <summary>延迟一帧执行，确保前一次 PublishStableState 的 subscriber 回调全部执行完。</summary>
        private IEnumerator DelayedAction(System.Action action)
        {
            yield return null;
            action?.Invoke();
        }

        // ==============================
        // 发布与存档
        // ==============================

        private void PublishStableState()
        {
            _lastStableState = State.Clone();
            SaveLastStableState();
            PublishState(true);
        }

        public void SaveLastStableState()
        {
            if (!IsHostAuthority) return;
            SaveHostProgress(_lastStableState);
        }

        private void PublishState(bool stable)
        {
            var snapshot = State.Clone();
            _transport?.BroadcastState(snapshot);
            StateChanged?.Invoke(snapshot);
        }

        private void SaveHostProgress(GameplaySnapshot savedState)
        {
            if (!IsHostAuthority) return;

            HostSaveService.Save(new GameProgressSave
            {
                activeSceneName = SceneManager.GetActiveScene().name,
                activeRecipeId = savedState.cooking?.recipeId,
                cookingState = savedState.cooking?.Clone(),
                baguaState = savedState.bagua?.Clone(),
                baguaMiniGameId = IsBaguaActive ? savedState.miniGameId : null,
                albumState = savedState.album?.Clone(),
                albumMiniGameId = IsAlbumActive ? savedState.miniGameId : null,
                collectedRewardIds = BuildCollectedRewardIds(savedState),
                pendingPhotoId = savedState.pendingPhotoId,
                previewPhotoId = savedState.previewPhotoId
            });
        }

        // ==============================
        // 辅助
        // ==============================

        private bool IsBaguaActive =>
            !string.IsNullOrEmpty(State.miniGameId) && FindBaguaConfig(State.miniGameId) != null;

        private bool IsAlbumActive =>
            !string.IsNullOrEmpty(State.miniGameId) && FindAlbumConfig(State.miniGameId) != null;

        private RecipeConfig CurrentRecipe => FindRecipe(State.cooking.recipeId);

        private static bool CanRequesterPerform(GameplayIntentType intentType, SessionRole requester)
        {
            switch (intentType)
            {
                // Client 可执行
                case GameplayIntentType.SelectIngredient:
                case GameplayIntentType.DropIngredient:
                case GameplayIntentType.RequestHint:
                case GameplayIntentType.MarkBaguaStoryHeard:
                case GameplayIntentType.MatchBaguaItem:
                    return requester == SessionRole.Client;

                // 双端均可执行（同屏共玩）
                case GameplayIntentType.PlaceAlbumSticker:
                case GameplayIntentType.PlaceAlbumNameTag:
                    return true;

                // Host 可执行
                case GameplayIntentType.StartMiniGame:
                case GameplayIntentType.SelectSeasoning:
                case GameplayIntentType.ShowHint:
                case GameplayIntentType.FinishMiniGame:
                case GameplayIntentType.StartBaguaMiniGame:
                case GameplayIntentType.AssignBaguaPhotoName:
                case GameplayIntentType.StartAlbumMiniGame:
                case GameplayIntentType.CollectMemoryPhoto:
                case GameplayIntentType.CollectMiniGamePhoto:
                case GameplayIntentType.CloseMemoryPhotoPreview:
                case GameplayIntentType.StartDialogue:
                case GameplayIntentType.AdvanceDialogue:
                case GameplayIntentType.FinishDialogue:
                    return requester == SessionRole.Host;

                default:
                    return false;
            }
        }

        private RecipeConfig FindRecipe(string recipeId)
        {
            if (recipes == null || string.IsNullOrEmpty(recipeId)) return null;
            foreach (var recipe in recipes)
            {
                if (recipe != null && recipe.RecipeId == recipeId) return recipe;
            }
            return null;
        }

        private BaguaStoryConfig FindBaguaConfig(string configId)
        {
            if (baguaConfigs == null || string.IsNullOrEmpty(configId)) return null;
            foreach (var config in baguaConfigs)
            {
                if (config != null && config.MiniGameId == configId) return config;
            }
            return null;
        }

        private AlbumConfig FindAlbumConfig(string configId)
        {
            if (albumConfigs == null || string.IsNullOrEmpty(configId)) return null;
            foreach (var config in albumConfigs)
            {
                if (config != null && config.MiniGameId == configId) return config;
            }
            return null;
        }

        private static bool AreAlbumPhotosCollected(AlbumConfig config)
        {
            if (config == null || config.RequiredPhotoIds == null) return true;
            foreach (var photoId in config.RequiredPhotoIds)
            {
                if (!Instance.State.collectedPhotoIds.Contains(photoId)) return false;
            }
            return true;
        }

        private DialogueSequence FindDialogueConfig(string sequenceId)
        {
            if (dialogueConfigs == null || string.IsNullOrEmpty(sequenceId)) return null;
            foreach (var config in dialogueConfigs)
            {
                if (config != null && config.SequenceId == sequenceId) return config;
            }
            return null;
        }

        /// <summary>公开查找方法，供 DialogueController 等外部组件查询当前对白配置。</summary>
        public DialogueSequence GetDialogueConfig(string sequenceId) => FindDialogueConfig(sequenceId);

        private void QueueRewards(string[] rewardIds)
        {
            if (rewardIds == null) return;
            foreach (var rewardId in rewardIds)
            {
                if (string.IsNullOrEmpty(rewardId)) continue;
                if (!IsMemoryPhoto(rewardId))
                {
                    _collectedRewards.Add(rewardId);
                    continue;
                }

                if (State.collectedPhotoIds.Contains(rewardId) || State.pendingPhotoId == rewardId) continue;
                State.pendingPhotoId = rewardId;
            }
        }

        private static bool IsMemoryPhoto(string rewardId)
        {
            return !string.IsNullOrEmpty(rewardId) &&
                   rewardId.IndexOf("photo", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string[] BuildCollectedRewardIds(GameplaySnapshot savedState)
        {
            var rewards = new List<string>(_collectedRewards);
            foreach (var photoId in savedState.collectedPhotoIds ?? new List<string>())
            {
                if (!rewards.Contains(photoId)) rewards.Add(photoId);
            }
            return rewards.ToArray();
        }
    }
}
