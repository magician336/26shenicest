using System;
using DoNotForgetMe.Network.Gameplay;

namespace DoNotForgetMe.Save
{
    /// <summary>
    /// Host 侧持久化进度快照。Client 不保存权威状态，只在加入后接收 Host 同步。
    /// </summary>
    [Serializable]
    public class GameProgressSave
    {
        public string CurrentRoomId = "Room_A";
        public GameplayPhase Phase = GameplayPhase.Exploration;
        public string ActiveMiniGameId = string.Empty;
        public string LastCompletedMiniGameId = string.Empty;
        public int CompletedMiniGameCount;
        public string[] CompletedMiniGameIds = Array.Empty<string>();
        public string UpdatedAtUtc = string.Empty;

        public GameProgressSave Clone()
        {
            return new GameProgressSave
            {
                CurrentRoomId = CurrentRoomId,
                Phase = Phase,
                ActiveMiniGameId = ActiveMiniGameId,
                LastCompletedMiniGameId = LastCompletedMiniGameId,
                CompletedMiniGameCount = CompletedMiniGameCount,
                CompletedMiniGameIds = CompletedMiniGameIds != null
                    ? (string[])CompletedMiniGameIds.Clone()
                    : Array.Empty<string>(),
                UpdatedAtUtc = UpdatedAtUtc
            };
        }
    }
}
