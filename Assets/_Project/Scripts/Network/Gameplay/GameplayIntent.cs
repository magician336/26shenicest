using System;

namespace DoNotForgetMe.Network.Gameplay
{
    public enum GameplayIntentType
    {
        StartMiniGame,
        SelectIngredient,
        DropIngredient,
        SelectSeasoning,
        RequestHint,
        ShowHint,
        InterruptMiniGame,
        ResumeMiniGame,
        RestartMiniGame,
        FinishMiniGame
    }

    [Serializable]
    public struct GameplayIntent
    {
        public GameplayIntentType type;
        public string recipeId;
        public string itemId;

        public GameplayIntent(GameplayIntentType type, string recipeId = null, string itemId = null)
        {
            this.type = type;
            this.recipeId = recipeId;
            this.itemId = itemId;
        }
    }
}
