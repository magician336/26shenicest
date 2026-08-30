using System;

namespace DoNotForgetMe.Dialogue
{
    /// <summary>
    /// 对白序列的运行时状态；纳入 GameplaySnapshot，
    /// 由 Coordinator 通过 Intent→Snapshot→StateChanged 链路同步。
    /// </summary>
    [Serializable]
    public class DialogueState
    {
        public string sequenceId;
        public int currentEntryIndex;

        public bool IsActive => !string.IsNullOrEmpty(sequenceId);

        public DialogueState Clone()
        {
            return new DialogueState
            {
                sequenceId = sequenceId,
                currentEntryIndex = currentEntryIndex
            };
        }

        public void Reset()
        {
            sequenceId = null;
            currentEntryIndex = 0;
        }
    }
}
