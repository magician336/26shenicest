using System;

namespace DoNotForgetMe.Network.Gameplay
{
    public enum GameplayIntentType
    {
        // --- 做饭小游戏 ---
        StartMiniGame,
        SelectIngredient,
        DropIngredient,
        SelectSeasoning,
        RequestHint,
        ShowHint,
        InterruptMiniGame,
        ResumeMiniGame,
        RestartMiniGame,
        FinishMiniGame,

        // --- 八卦小游戏 ---
        StartBaguaMiniGame,
        MarkBaguaStoryHeard,
        MatchBaguaItem,
        AssignBaguaPhotoName,

        // --- 家庭记忆相册 ---
        CollectMemoryPhoto,
        CloseMemoryPhotoPreview,

        // --- 全家福相册小游戏 ---
        StartAlbumMiniGame,
        PlaceAlbumSticker,
        PlaceAlbumNameTag,

        // --- 对白序列 ---
        StartDialogue,
        AdvanceDialogue,
        FinishDialogue
    }

    [Serializable]
    public struct GameplayIntent
    {
        public GameplayIntentType type;
        public string recipeId;
        public string itemId;
        public string characterId;
        public string zoneId;
        public string dialogueSequenceId;

        public GameplayIntent(GameplayIntentType type, string recipeId = null, string itemId = null)
        {
            this.type = type;
            this.recipeId = recipeId;
            this.itemId = itemId;
            this.characterId = null;
            this.zoneId = null;
            this.dialogueSequenceId = null;
        }

        public GameplayIntent(GameplayIntentType type, string recipeId, string itemId, string characterId, string zoneId)
        {
            this.type = type;
            this.recipeId = recipeId;
            this.itemId = itemId;
            this.characterId = characterId;
            this.zoneId = zoneId;
            this.dialogueSequenceId = null;
        }
    }
}
