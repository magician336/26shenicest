using System;
using System.Collections.Generic;
using DoNotForgetMe.Dialogue;
using DoNotForgetMe.MiniGame.Album;
using DoNotForgetMe.MiniGame.Bagua;
using DoNotForgetMe.MiniGame.Cooking;

namespace DoNotForgetMe.Network.Gameplay
{
    /// <summary>
    /// 统一游戏快照：同时容纳做饭、八卦小游戏与对白序列状态。
    /// FusionGameplayBridge 只序列化与广播此快照；
    /// SessionGameplayCoordinator 根据 miniGameId 将状态交给对应逻辑。
    /// </summary>
    [Serializable]
    public class GameplaySnapshot
    {
        public GameplayPhase phase = GameplayPhase.Exploration;
        public string miniGameId;
        public CookingGameState cooking;
        public BaguaGameState bagua;
        public AlbumGameState album;
        public DialogueState dialogue;
        public List<string> collectedPhotoIds = new();
        public string pendingPhotoId;
        public string previewPhotoId;

        public GameplaySnapshot()
        {
            phase = GameplayPhase.Exploration;
            cooking = new CookingGameState();
            bagua = new BaguaGameState();
            album = new AlbumGameState();
            dialogue = new DialogueState();
        }

        public GameplaySnapshot Clone()
        {
            return new GameplaySnapshot
            {
                phase = phase,
                miniGameId = miniGameId,
                cooking = cooking != null ? cooking.Clone() : new CookingGameState(),
                bagua = bagua != null ? bagua.Clone() : new BaguaGameState(),
                album = album != null ? album.Clone() : new AlbumGameState(),
                dialogue = dialogue != null ? dialogue.Clone() : new DialogueState(),
                collectedPhotoIds = collectedPhotoIds != null ? new List<string>(collectedPhotoIds) : new List<string>(),
                pendingPhotoId = pendingPhotoId,
                previewPhotoId = previewPhotoId
            };
        }
    }
}
