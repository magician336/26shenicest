namespace DoNotForgetMe.Save
{
    /// <summary>主菜单“继续游戏”向已加载 Game 场景传递的 Host 存档。</summary>
    public static class HostSaveContext
    {
        public static GameProgressSave PendingSave { get; private set; }

        public static void SetPending(GameProgressSave save)
        {
            PendingSave = save;
        }

        public static GameProgressSave Consume()
        {
            var save = PendingSave;
            PendingSave = null;
            return save;
        }

        public static void Clear()
        {
            PendingSave = null;
        }
    }
}
