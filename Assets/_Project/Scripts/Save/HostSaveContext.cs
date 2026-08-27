namespace DoNotForgetMe.Save
{
    /// <summary>
    /// 主菜单加载存档后，在 Game 场景初始化前临时传递给 Host 权威流程。
    /// </summary>
    public static class HostSaveContext
    {
        private static GameProgressSave _pendingSave;

        public static bool HasPendingSave => _pendingSave != null;

        public static void SetPending(GameProgressSave save)
        {
            _pendingSave = save != null ? save.Clone() : null;
        }

        public static GameProgressSave Consume()
        {
            var save = _pendingSave;
            _pendingSave = null;
            return save != null ? save.Clone() : null;
        }

        public static void Clear()
        {
            _pendingSave = null;
        }
    }
}
