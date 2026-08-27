using System;
using DoNotForgetMe.MiniGame.Cooking;

namespace DoNotForgetMe.Network.Gameplay
{
    /// <summary>Client 上报给 Host 的玩法意图。Host 仍负责最终判定。</summary>
    [Serializable]
    public struct GameplayIntent
    {
        public SessionRole Role;
        public GameplayIntentType Type;
        public string TargetId;
        public CookingStep CookingStep;

        public static GameplayIntent StartMiniGame(SessionRole role, string gameId)
        {
            return new GameplayIntent
            {
                Role = role,
                Type = GameplayIntentType.StartMiniGame,
                TargetId = gameId,
                CookingStep = CookingStep.None
            };
        }

        public static GameplayIntent CompleteCookingStep(SessionRole role, string gameId, CookingStep step)
        {
            return new GameplayIntent
            {
                Role = role,
                Type = GameplayIntentType.CompleteCookingStep,
                TargetId = gameId,
                CookingStep = step
            };
        }
    }

    public enum GameplayIntentType
    {
        StartMiniGame,
        CompleteCookingStep
    }
}
