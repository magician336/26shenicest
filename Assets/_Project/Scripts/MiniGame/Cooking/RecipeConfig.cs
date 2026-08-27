using System;
using UnityEngine;

namespace DoNotForgetMe.MiniGame.Cooking
{
    [CreateAssetMenu(menuName = "Data/MiniGame/Recipe Config", fileName = "RecipeConfig")]
    public class RecipeConfig : ScriptableObject
    {
        [SerializeField] private string recipeId = "tomato_egg";
        [SerializeField] private string displayName = "番茄炒蛋";
        [SerializeField] private string motherTaskText = "请做番茄炒蛋";
        [SerializeField] private string daughterTaskText = "查看菜谱改痕，为菜调味";
        [SerializeField] private string containerId = "wok";
        [SerializeField] private string[] requiredIngredients = { "tomato", "egg" };
        [SerializeField] private string[] distractorIngredients = { "cucumber", "ribs" };
        [SerializeField] private string correctSeasoning = "sugar";
        [SerializeField] private string[] forbiddenSeasonings = Array.Empty<string>();
        [SerializeField] private string[] hintTexts =
        {
            "找一种红色的蔬菜。",
            "再找一个能打进碗里的食材。",
            "番茄和鸡蛋轻微发光。"
        };
        [SerializeField] private string[] rewardIds = { "photo_hongqiang", "tag_fifth_brother" };

        public string RecipeId => recipeId;
        public string DisplayName => displayName;
        public string MotherTaskText => motherTaskText;
        public string DaughterTaskText => daughterTaskText;
        public string ContainerId => containerId;
        public string[] RequiredIngredients => requiredIngredients;
        public string[] DistractorIngredients => distractorIngredients;
        public string CorrectSeasoning => correctSeasoning;
        public string[] ForbiddenSeasonings => forbiddenSeasonings;
        public string[] HintTexts => hintTexts;
        public string[] RewardIds => rewardIds;

        public bool IsRequiredIngredient(string itemId)
        {
            return Array.IndexOf(requiredIngredients, itemId) >= 0;
        }

        public bool IsCorrectSeasoning(string itemId)
        {
            return string.Equals(correctSeasoning, itemId, StringComparison.Ordinal);
        }
    }
}
