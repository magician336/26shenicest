using System;
using System.Collections.Generic;
using DoNotForgetMe.Network.Gameplay;

namespace DoNotForgetMe.MiniGame.Cooking
{
    /// <summary>Host 权威的可序列化做饭状态；客户端只把它用于渲染私有视图。</summary>
    [Serializable]
    public class CookingGameState
    {
        public GameplayPhase phase = GameplayPhase.Exploration;
        public string recipeId;
        public CookingStep step = CookingStep.MotherSelectIngredients;
        public List<string> selectedIngredients = new();
        public List<string> droppedIngredients = new();
        public bool motherFoodComplete;
        public bool daughterUnlocked;
        public bool daughterSeasoningComplete;
        public string selectedSeasoning;
        public int hintLevel;
        public bool hintRequested;
        public bool completed;

        public CookingGameState Clone()
        {
            return new CookingGameState
            {
                phase = phase,
                recipeId = recipeId,
                step = step,
                selectedIngredients = selectedIngredients != null ? new List<string>(selectedIngredients) : new List<string>(),
                droppedIngredients = droppedIngredients != null ? new List<string>(droppedIngredients) : new List<string>(),
                motherFoodComplete = motherFoodComplete,
                daughterUnlocked = daughterUnlocked,
                daughterSeasoningComplete = daughterSeasoningComplete,
                selectedSeasoning = selectedSeasoning,
                hintLevel = hintLevel,
                hintRequested = hintRequested,
                completed = completed
            };
        }
    }
}
