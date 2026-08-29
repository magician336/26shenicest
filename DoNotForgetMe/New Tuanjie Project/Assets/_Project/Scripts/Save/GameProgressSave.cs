using System;
using DoNotForgetMe.MiniGame.Album;
using DoNotForgetMe.MiniGame.Bagua;
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
        public BaguaGameState baguaState;
        public string baguaMiniGameId;
        public AlbumGameState albumState;
        public string albumMiniGameId;
        public string[] collectedRewardIds = Array.Empty<string>();
        public string pendingPhotoId;
        public string previewPhotoId;
    }
}
