namespace DoNotForgetMe.MiniGame.Album
{
    /// <summary>全家福相册小游戏的流程步骤，由 Host 权威推进。</summary>
    public enum AlbumStep
    {
        /// <summary>贴纸阶段：将人物贴纸拖入轮廓框。</summary>
        PlaceStickers,
        /// <summary>姓名tag阶段：将姓名标签拖入名牌区域。</summary>
        PlaceNameTags,
        /// <summary>全部完成，等待玩家点击完成按钮。</summary>
        Complete
    }
}
