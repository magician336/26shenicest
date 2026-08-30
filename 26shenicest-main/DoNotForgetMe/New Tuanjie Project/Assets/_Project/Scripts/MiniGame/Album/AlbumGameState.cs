using System;
using System.Collections.Generic;
using DoNotForgetMe.Network.Gameplay;

namespace DoNotForgetMe.MiniGame.Album
{
    /// <summary>
    /// 全家福相册小游戏的 Host 权威状态。
    /// </summary>
    [Serializable]
    public class AlbumGameState
    {
        public GameplayPhase phase = GameplayPhase.Exploration;
        public AlbumStep step = AlbumStep.PlaceStickers;
        public List<string> placedStickerCharacterIds = new();
        public List<string> placedNameTagCharacterIds = new();
        public bool completed;

        public AlbumGameState Clone()
        {
            return new AlbumGameState
            {
                phase = phase,
                step = step,
                placedStickerCharacterIds = placedStickerCharacterIds != null ? new List<string>(placedStickerCharacterIds) : new(),
                placedNameTagCharacterIds = placedNameTagCharacterIds != null ? new List<string>(placedNameTagCharacterIds) : new(),
                completed = completed
            };
        }

        public void Reset()
        {
            placedStickerCharacterIds.Clear();
            placedNameTagCharacterIds.Clear();
            completed = false;
            step = AlbumStep.PlaceStickers;
        }
    }
}
