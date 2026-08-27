using System;
using DoNotForgetMe.MiniGame.Cooking;

namespace DoNotForgetMe.Save
{
    [Serializable]
    public class GameProgressSave
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string activeSceneName;
        public string activeRecipeId;
        public bool hasInterruptedMiniGame;
        public CookingGameState cookingState;
        public string[] collectedRewardIds = Array.Empty<string>();
    }
}
