using System;
using DoNotForgetMe.Network.Gameplay;

namespace DoNotForgetMe.MiniGame.Bagua
{
    /// <summary>
    /// 八卦小游戏的 Host 权威逻辑。由 SessionGameplayCoordinator 委托调用，
    /// 不直接发布状态——通过返回值告知 coordinator 是否需要广播与存档。
    /// </summary>
    public class BaguaSessionLogic
    {
        public enum PublishKind { None, NonStable, Stable }

        private readonly BaguaStoryConfig _config;

        public BaguaSessionLogic(BaguaStoryConfig config)
        {
            _config = config;
        }

        /// <summary>开始八卦小游戏：初始化 bagua 状态，进入 ClientMatchItems 步骤。</summary>
        public PublishKind StartMiniGame(BaguaGameState state)
        {
            if (state.phase != GameplayPhase.Exploration) return PublishKind.None;

            state.phase = GameplayPhase.MiniGame;
            state.step = BaguaStep.ClientMatchItems;
            state.Reset();
            state.phase = GameplayPhase.MiniGame;
            state.step = BaguaStep.ClientMatchItems;
            return PublishKind.Stable;
        }

        /// <summary>Client 标记已听完一段故事。</summary>
        public PublishKind MarkStoryHeard(BaguaGameState state, string characterId)
        {
            if (state.phase != GameplayPhase.MiniGame || state.step != BaguaStep.ClientMatchItems) return PublishKind.None;
            if (string.IsNullOrEmpty(characterId)) return PublishKind.None;
            if (state.heardStoryIds.Contains(characterId)) return PublishKind.None;

            state.heardStoryIds.Add(characterId);
            return PublishKind.NonStable;
        }

        /// <summary>Client 提交人物—物品配对。正确时锁定，错误时仅反馈。</summary>
        public PublishKind MatchItem(BaguaGameState state, string characterId, string itemId, out string feedback)
        {
            feedback = null;
            if (state.phase != GameplayPhase.MiniGame || state.step != BaguaStep.ClientMatchItems) return PublishKind.None;
            if (state.matchedCharacterIds.Contains(characterId)) return PublishKind.None;

            if (!_config.IsCorrectMatch(characterId, itemId))
            {
                feedback = "wrong_match";
                return PublishKind.None;
            }

            state.matchedCharacterIds.Add(characterId);

            // 三组全部配对完成 → 推进到 HostIdentifyPeople
            if (state.matchedCharacterIds.Count >= _config.Entries.Length)
            {
                state.clientComplete = true;
                state.step = BaguaStep.HostIdentifyPeople;
                return PublishKind.Stable;
            }

            return PublishKind.Stable;
        }

        /// <summary>Host 在照片区域投放姓名。正确时固定，错误时仅反馈。</summary>
        public PublishKind AssignPhotoName(BaguaGameState state, string zoneId, string characterId, out string feedback)
        {
            feedback = null;
            if (state.phase != GameplayPhase.MiniGame || state.step != BaguaStep.HostIdentifyPeople) return PublishKind.None;
            if (state.assignedPhotoZoneIds.Contains(zoneId)) return PublishKind.None;

            if (!_config.IsCorrectPhotoAssignment(zoneId, characterId))
            {
                feedback = "wrong_assign";
                return PublishKind.None;
            }

            state.assignedPhotoZoneIds.Add(zoneId);

            // 所有区域投放完成 → 小游戏完成
            if (state.assignedPhotoZoneIds.Count >= _config.PhotoZones.Length)
            {
                state.hostComplete = true;
                state.completed = true;
                state.step = BaguaStep.Complete;
                return PublishKind.Stable;
            }

            return PublishKind.Stable;
        }

        /// <summary>完成小游戏，回到探索阶段。</summary>
        public PublishKind FinishMiniGame(BaguaGameState state)
        {
            if (state.phase != GameplayPhase.MiniGame || !state.completed) return PublishKind.None;

            state.phase = GameplayPhase.Exploration;
            state.step = BaguaStep.ClientMatchItems;
            state.heardStoryIds.Clear();
            state.matchedCharacterIds.Clear();
            state.assignedPhotoZoneIds.Clear();
            state.clientComplete = false;
            state.hostComplete = false;
            state.completed = false;
            return PublishKind.Stable;
        }

        /// <summary>中断小游戏。</summary>
        public PublishKind Interrupt(BaguaGameState state)
        {
            if (state.phase != GameplayPhase.MiniGame || state.completed) return PublishKind.None;
            state.phase = GameplayPhase.MiniGameInterrupted;
            return PublishKind.Stable;
        }

        /// <summary>恢复中断的小游戏。</summary>
        public PublishKind Resume(BaguaGameState state)
        {
            if (state.phase != GameplayPhase.MiniGameInterrupted) return PublishKind.None;
            state.phase = GameplayPhase.MiniGame;
            return PublishKind.Stable;
        }

        /// <summary>重新开始八卦小游戏（保留 miniGameId，清空进度）。</summary>
        public PublishKind Restart(BaguaGameState state)
        {
            if (state.phase != GameplayPhase.MiniGameInterrupted) return PublishKind.None;
            state.Reset();
            state.phase = GameplayPhase.MiniGame;
            state.step = BaguaStep.ClientMatchItems;
            return PublishKind.Stable;
        }
    }
}
