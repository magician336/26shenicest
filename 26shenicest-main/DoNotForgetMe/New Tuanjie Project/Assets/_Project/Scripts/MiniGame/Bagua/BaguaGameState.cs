using System;
using System.Collections.Generic;
using DoNotForgetMe.Network.Gameplay;

namespace DoNotForgetMe.MiniGame.Bagua
{
    /// <summary>
    /// 八卦小游戏的 Host 权威状态；仅保存需要验证、同步与恢复的事实。
    /// 音频播放进度、按钮缩放和抖动等纯表现不进入网络状态。
    /// </summary>
    [Serializable]
    public class BaguaGameState
    {
        public GameplayPhase phase = GameplayPhase.Exploration;
        public BaguaStep step = BaguaStep.ClientMatchItems;
        public List<string> heardStoryIds = new();
        public List<string> matchedCharacterIds = new();
        public List<string> assignedPhotoZoneIds = new();
        public bool clientComplete;
        public bool hostComplete;
        public bool completed;

        public BaguaGameState Clone()
        {
            return new BaguaGameState
            {
                phase = phase,
                step = step,
                heardStoryIds = heardStoryIds != null ? new List<string>(heardStoryIds) : new(),
                matchedCharacterIds = matchedCharacterIds != null ? new List<string>(matchedCharacterIds) : new(),
                assignedPhotoZoneIds = assignedPhotoZoneIds != null ? new List<string>(assignedPhotoZoneIds) : new(),
                clientComplete = clientComplete,
                hostComplete = hostComplete,
                completed = completed
            };
        }

        /// <summary>重置为初始状态（保留 phase 和 step 由调用方设置）。</summary>
        public void Reset()
        {
            heardStoryIds.Clear();
            matchedCharacterIds.Clear();
            assignedPhotoZoneIds.Clear();
            clientComplete = false;
            hostComplete = false;
            completed = false;
            step = BaguaStep.ClientMatchItems;
        }
    }
}
