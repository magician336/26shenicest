using System;
using System.Collections.Generic;
using DoNotForgetMe.MiniGame.Cooking;
using DoNotForgetMe.Save;
using UnityEngine;

namespace DoNotForgetMe.Network.Gameplay
{
    /// <summary>
    /// 会话内玩法协调器。Host 拥有权威状态，Client 通过 IGameplayTransport 上报意图。
    /// </summary>
    [DisallowMultipleComponent]
    public class SessionGameplayCoordinator : MonoBehaviour
    {
        public static SessionGameplayCoordinator Instance { get; private set; }

        [Header("流程")]
        [SerializeField] private string currentRoomId = "Room_A";
        [SerializeField] private RecipeConfig[] recipes;

        private readonly HashSet<string> _completedMiniGames = new HashSet<string>();
        private IGameplayTransport _transport;
        private global::MiniGameManager _miniGameManager;
        private CookingGameState _cookingState = new CookingGameState();

        public SessionRole LocalRole => _transport != null ? _transport.LocalRole : NetworkSessionManager.Service.Role;
        public bool IsHostAuthority => _transport == null
            ? NetworkSessionManager.Service.Role != SessionRole.Client
            : _transport.IsHostAuthority;

        public CookingGameState CookingState => _cookingState.Clone();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _miniGameManager = FindObjectOfType<MiniGameManager>();
            TryInstallTransport(FindTransportInScene());
        }

        private void Start()
        {
            if (IsHostAuthority)
            {
                var pendingSave = HostSaveContext.Consume();
                if (pendingSave != null)
                {
                    ApplySave(pendingSave);
                    Debug.Log("[Gameplay] Host save restored");
                }
                else
                {
                    SaveLastStableState();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            UninstallTransport();
        }

        public void SetTransport(IGameplayTransport transport)
        {
            TryInstallTransport(transport);
        }

        public void RequestMiniGame(string gameId)
        {
            if (IsHostAuthority)
            {
                StartMiniGameAsHost(gameId);
                return;
            }

            Debug.Log("[Gameplay] Client 无小游戏触发权，等待 Host 操作。");
        }

        public void SubmitCookingStep(string gameId, CookingStep step)
        {
            if (IsHostAuthority)
            {
                ApplyCookingStep(LocalRole, gameId, step);
                return;
            }

            _transport?.SendIntent(GameplayIntent.CompleteCookingStep(LocalRole, gameId, step));
        }

        public void ApplyRemoteState(CookingGameState state)
        {
            if (state == null) return;

            _cookingState = state.Clone();
            if (_cookingState.Phase == GameplayPhase.MiniGame && _miniGameManager != null && !_miniGameManager.IsActive)
            {
                var recipe = FindRecipe(_cookingState.RecipeId);
                if (recipe != null)
                {
                    _miniGameManager.StartCookingMiniGame(recipe, _cookingState, this, LocalRole);
                    return;
                }
            }

            _miniGameManager?.ApplyCookingState(_cookingState);
        }

        public void SaveLastStableState()
        {
            if (!IsHostAuthority) return;

            HostSaveService.Save(new GameProgressSave
            {
                CurrentRoomId = currentRoomId,
                Phase = _cookingState.Phase,
                ActiveMiniGameId = _cookingState.Phase == GameplayPhase.MiniGame ? _cookingState.RecipeId : string.Empty,
                LastCompletedMiniGameId = _cookingState.IsComplete ? _cookingState.RecipeId : string.Empty,
                CompletedMiniGameCount = _completedMiniGames.Count,
                CompletedMiniGameIds = new List<string>(_completedMiniGames).ToArray()
            });
        }

        private void TryInstallTransport(IGameplayTransport transport)
        {
            if (transport == null || ReferenceEquals(_transport, transport)) return;

            UninstallTransport();
            _transport = transport;
            _transport.IntentReceived += HandleIntent;
            _transport.StateReceived += ApplyRemoteState;
        }

        private void UninstallTransport()
        {
            if (_transport == null) return;

            _transport.IntentReceived -= HandleIntent;
            _transport.StateReceived -= ApplyRemoteState;
            _transport = null;
        }

        private void HandleIntent(GameplayIntent intent)
        {
            if (!IsHostAuthority) return;

            switch (intent.Type)
            {
                case GameplayIntentType.StartMiniGame:
                    StartMiniGameAsHost(intent.TargetId);
                    break;

                case GameplayIntentType.CompleteCookingStep:
                    ApplyCookingStep(intent.Role, intent.TargetId, intent.CookingStep);
                    break;
            }
        }

        private void StartMiniGameAsHost(string gameId)
        {
            var recipe = FindRecipe(gameId);
            if (recipe == null)
            {
                Debug.LogWarning("[Gameplay] 未找到配方：" + gameId);
                return;
            }

            _cookingState = CreateStateForRecipe(recipe, 0);
            _miniGameManager?.StartCookingMiniGame(recipe, _cookingState, this, LocalRole);
            PublishState();
            SaveLastStableState();
        }

        private void ApplyCookingStep(SessionRole role, string gameId, CookingStep step)
        {
            if (_cookingState == null || _cookingState.Phase != GameplayPhase.MiniGame) return;
            if (_cookingState.RecipeId != gameId) return;
            if (_cookingState.CurrentStep != step) return;

            _cookingState.LastActor = role.ToString();

            var recipe = FindRecipe(gameId);
            if (recipe == null) return;

            if (recipe.IsFinalStep(_cookingState.StepIndex))
            {
                _cookingState.CurrentStep = CookingStep.Complete;
                _cookingState.Phase = GameplayPhase.Completed;
                _cookingState.IsComplete = true;
                _cookingState.HostPrompt = "料理完成。";
                _cookingState.ClientPrompt = "料理完成。";
                _completedMiniGames.Add(gameId);
            }
            else
            {
                _cookingState = CreateStateForRecipe(recipe, _cookingState.StepIndex + 1);
                _cookingState.LastActor = role.ToString();
            }

            _miniGameManager?.ApplyCookingState(_cookingState);
            PublishState();
            SaveLastStableState();
        }

        private CookingGameState CreateStateForRecipe(RecipeConfig recipe, int stepIndex)
        {
            return new CookingGameState
            {
                RecipeId = recipe.recipeId,
                Phase = GameplayPhase.MiniGame,
                CurrentStep = recipe.GetStep(stepIndex),
                StepIndex = stepIndex,
                IsComplete = false,
                HostPrompt = recipe.GetPrompt(true, stepIndex),
                ClientPrompt = recipe.GetPrompt(false, stepIndex)
            };
        }

        private void PublishState()
        {
            _transport?.BroadcastState(_cookingState);
        }

        private void ApplySave(GameProgressSave save)
        {
            currentRoomId = string.IsNullOrEmpty(save.CurrentRoomId) ? currentRoomId : save.CurrentRoomId;
            _completedMiniGames.Clear();
            if (save.CompletedMiniGameIds != null)
            {
                foreach (var id in save.CompletedMiniGameIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        _completedMiniGames.Add(id);
                    }
                }
            }

            if (!string.IsNullOrEmpty(save.ActiveMiniGameId))
            {
                StartMiniGameAsHost(save.ActiveMiniGameId);
            }
        }

        private RecipeConfig FindRecipe(string gameId)
        {
            if (recipes == null) return null;

            foreach (var recipe in recipes)
            {
                if (recipe != null && recipe.recipeId == gameId)
                {
                    return recipe;
                }
            }
            return null;
        }

        private static IGameplayTransport FindTransportInScene()
        {
            foreach (var behaviour in FindObjectsOfType<MonoBehaviour>())
            {
                if (behaviour is IGameplayTransport transport)
                {
                    return transport;
                }
            }
            return null;
        }
    }
}
