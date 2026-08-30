namespace DoNotForgetMe.Network.Gameplay
{
    /// <summary>会话内可持久化的游戏阶段。只有 Host 可以推进阶段。</summary>
    public enum GameplayPhase
    {
        Exploration,
        Dialogue,
        MiniGame,
        /// <summary>游戏主体已结束，等待终局内容。</summary>
        GameEnded
    }
}
