using System;
using System.Collections.Generic;
using DoNotForgetMe.MiniGame.Cooking;
using DoNotForgetMe.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoNotForgetMe.Network.Gameplay
{
    /// <summary>Host 权威的游戏流程状态机；所有有效状态变化都从这里产生。</summary>
    public class SessionGameplayCoordinator : MonoBehaviour
    {
        public static SessionGameplayCoordinator Instance { get; private set; }

        [SerializeField] private RecipeConfig[] recipes;

        public CookingGameState State { get; private set; } = new();
        public event Action<CookingGameState> StateChanged;
        public event Action<string> FeedbackRequested;

        private readonly HashSet<string> _collectedRewards = new();
        private IGameplayTransport _transport;
        private CookingGameState _lastStableState = new();

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
        }

        private void Start()
        {
            var pendingSave = HostSaveContext.Consume();
            if (pendingSave != null)
            {
                RestoreHostSave(pendingSave);
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

            State = save.cookingState != null ? save.cookingState.Clone() : new CookingGameState();
            _lastStableState = State.Clone();
            _collectedRewards.Clear();
            foreach (var rewardId in save.collectedRewardIds ?? Array.Empty<string>())
            {
                _collectedRewards.Add(rewardId);
            }
            PublishState(false);
        }

        public void ApplyAuthoritativeState(CookingGameState state)
        {
            if (state == null) return;
            State = state.Clone();
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
            if (!IsHostAuthority) return;
            if (!CanRequesterPerform(intent.type, requester)) return;

            switch (intent.type)
            {
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
                case GameplayIntentType.InterruptMiniGame:
                    InterruptMiniGame();
                    break;
                case GameplayIntentType.ResumeMiniGame:
                    ResumeMiniGame();
                    break;
                case GameplayIntentType.RestartMiniGame:
                    RestartMiniGame();
                    break;
                case GameplayIntentType.FinishMiniGame:
                    FinishMiniGame();
                    break;
            }
        }

        private void StartMiniGame(string recipeId)
        {
            if (State.phase != GameplayPhase.Exploration || FindRecipe(recipeId) == null) return;

            State = new CookingGameState
            {
                phase = GameplayPhase.MiniGame,
                recipeId = recipeId,
                step = CookingStep.MotherSelectIngredients
            };
            PublishStableState();
        }

        private void SelectIngredient(string itemId)
        {
            var recipe = CurrentRecipe;
            if (recipe == null || State.phase != GameplayPhase.MiniGame || State.step != CookingStep.MotherSelectIngredients) return;

            if (!recipe.IsRequiredIngredient(itemId) || State.selectedIngredients.Contains(itemId))
            {
                FeedbackRequested?.Invoke("wrong_select");
                return;
            }

            State.selectedIngredients.Add(itemId);
            if (State.selectedIngredients.Count == recipe.RequiredIngredients.Length)
            {
                State.step = CookingStep.MotherDropIngredients;
            }
            PublishState(false);
        }

        private void DropIngredient(string itemId)
        {
            var recipe = CurrentRecipe;
            if (recipe == null || State.phase != GameplayPhase.MiniGame || State.step != CookingStep.MotherDropIngredients) return;

            if (!State.selectedIngredients.Contains(itemId) || State.droppedIngredients.Contains(itemId))
            {
                FeedbackRequested?.Invoke("wrong_drop");
                return;
            }

            State.droppedIngredients.Add(itemId);
            if (State.droppedIngredients.Count != recipe.RequiredIngredients.Length)
            {
                PublishState(false);
                return;
            }

            State.motherFoodComplete = true;
            State.daughterUnlocked = true;
            State.step = CookingStep.DaughterSeason;
            PublishStableState();
        }

        private void SelectSeasoning(string itemId)
        {
            var recipe = CurrentRecipe;
            if (recipe == null || State.phase != GameplayPhase.MiniGame || State.step != CookingStep.DaughterSeason) return;

            if (!recipe.IsCorrectSeasoning(itemId))
            {
                FeedbackRequested?.Invoke("wrong_seasoning");
                return;
            }

            State.selectedSeasoning = itemId;
            State.daughterSeasoningComplete = true;
            State.completed = true;
            State.step = CookingStep.Complete;
            foreach (var rewardId in recipe.RewardIds)
            {
                _collectedRewards.Add(rewardId);
            }
            PublishStableState();
        }

        private void RequestHint()
        {
            if (State.phase != GameplayPhase.MiniGame || State.completed) return;
            State.hintRequested = true;
            PublishState(false);
        }

        private void ShowNextHint()
        {
            var recipe = CurrentRecipe;
            if (recipe == null || !State.hintRequested || State.hintLevel >= recipe.HintTexts.Length) return;

            State.hintRequested = false;
            State.hintLevel++;
            PublishState(false);
        }

        private void InterruptMiniGame()
        {
            if (State.phase != GameplayPhase.MiniGame || State.completed) return;
            State.phase = GameplayPhase.MiniGameInterrupted;
            PublishStableState();
        }

        private void ResumeMiniGame()
        {
            if (State.phase != GameplayPhase.MiniGameInterrupted) return;
            State.phase = GameplayPhase.MiniGame;
            PublishStableState();
        }

        private void RestartMiniGame()
        {
            if (State.phase != GameplayPhase.MiniGameInterrupted) return;
            var recipeId = State.recipeId;
            State = new CookingGameState
            {
                phase = GameplayPhase.MiniGame,
                recipeId = recipeId,
                step = CookingStep.MotherSelectIngredients
            };
            PublishStableState();
        }

        private void FinishMiniGame()
        {
            if (State.phase != GameplayPhase.MiniGame || !State.completed) return;
            State = new CookingGameState { phase = GameplayPhase.Exploration };
            PublishStableState();
        }

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

        private void SaveHostProgress(CookingGameState savedState)
        {
            if (!IsHostAuthority) return;

            HostSaveService.Save(new GameProgressSave
            {
                activeSceneName = SceneManager.GetActiveScene().name,
                activeRecipeId = savedState.recipeId,
                hasInterruptedMiniGame = savedState.phase == GameplayPhase.MiniGameInterrupted,
                cookingState = savedState.Clone(),
                collectedRewardIds = new List<string>(_collectedRewards).ToArray()
            });
        }

        private RecipeConfig CurrentRecipe => FindRecipe(State.recipeId);

        private static bool CanRequesterPerform(GameplayIntentType intentType, SessionRole requester)
        {
            switch (intentType)
            {
                case GameplayIntentType.SelectIngredient:
                case GameplayIntentType.DropIngredient:
                case GameplayIntentType.RequestHint:
                    return requester == SessionRole.Client;

                case GameplayIntentType.StartMiniGame:
                case GameplayIntentType.SelectSeasoning:
                case GameplayIntentType.ShowHint:
                case GameplayIntentType.InterruptMiniGame:
                case GameplayIntentType.ResumeMiniGame:
                case GameplayIntentType.RestartMiniGame:
                case GameplayIntentType.FinishMiniGame:
                    return requester == SessionRole.Host;

                default:
                    return false;
            }
        }

        private RecipeConfig FindRecipe(string recipeId)
        {
            if (recipes == null) return null;
            foreach (var recipe in recipes)
            {
                if (recipe != null && recipe.RecipeId == recipeId) return recipe;
            }
            return null;
        }
    }
}
