using System;
using DoNotForgetMe.Network.Gameplay;

namespace DoNotForgetMe.MiniGame.Cooking
{
    /// <summary>做饭小游戏同步状态。Host 修改，Client 只渲染。</summary>
    [Serializable]
    public class CookingGameState
    {
        public string RecipeId = string.Empty;
        public GameplayPhase Phase = GameplayPhase.Exploration;
        public CookingStep CurrentStep = CookingStep.None;
        public int StepIndex;
        public bool IsComplete;
        public string LastActor = string.Empty;
        public string HostPrompt = string.Empty;
        public string ClientPrompt = string.Empty;

        public CookingGameState Clone()
        {
            return new CookingGameState
            {
                RecipeId = RecipeId,
                Phase = Phase,
                CurrentStep = CurrentStep,
                StepIndex = StepIndex,
                IsComplete = IsComplete,
                LastActor = LastActor,
                HostPrompt = HostPrompt,
                ClientPrompt = ClientPrompt
            };
        }
    }
}
