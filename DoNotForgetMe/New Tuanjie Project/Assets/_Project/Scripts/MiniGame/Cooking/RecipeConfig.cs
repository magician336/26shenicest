using System;
using UnityEngine;

namespace DoNotForgetMe.MiniGame.Cooking
{
    /// <summary>食材/调料与 Sprite 的映射，在 Inspector 中逐条配置。</summary>
    [Serializable]
    public struct IngredientSprite
    {
        public string ingredientId;
        public Sprite sprite;
    }

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
        [SerializeField] private string[] seasoningOptions = { "sugar", "salt" };
        [SerializeField] private string recipeNote = "洪强爱吃甜的，放点糖。";
        [SerializeField] private string containerDisplayName = "锅";
        [SerializeField] private string[] hintTexts =
        {
            "找一种红色的蔬菜。",
            "再找一个能打进碗里的食材。",
            "番茄和鸡蛋轻微发光。"
        };
        [SerializeField] private string nextRecipeId;
        [Tooltip("完成后接入的对白序列 ID（照片收集后触发）")]
        [SerializeField] private string nextDialogueId;
        [SerializeField] private string[] rewardIds = { "photo_hongqiang", "tag_fifth_brother" };
        [SerializeField] private IngredientSprite[] ingredientSprites;
        [SerializeField] private Sprite rewardPhotoSprite;
        [SerializeField] private Sprite containerSprite;
        [SerializeField] private Sprite cookingBackground;
        [SerializeField] private Sprite daughterBackground;
        [SerializeField] private Sprite dishPhotoSprite;
        [SerializeField] private Sprite motherCompleteBackground;
        [SerializeField] private Sprite motherCompleteHint;

        public string RecipeId => recipeId;
        public string DisplayName => displayName;
        public string MotherTaskText => motherTaskText;
        public string DaughterTaskText => daughterTaskText;
        public string ContainerId => containerId;
        public string[] RequiredIngredients => requiredIngredients;
        public string[] DistractorIngredients => distractorIngredients;
        public string CorrectSeasoning => correctSeasoning;
        public string[] ForbiddenSeasonings => forbiddenSeasonings;
        public string[] SeasoningOptions => seasoningOptions;
        public string RecipeNote => recipeNote;
        public string ContainerDisplayName => containerDisplayName;
        public string[] HintTexts => hintTexts;
        public string[] RewardIds => rewardIds;
        public string NextRecipeId => nextRecipeId;
        public string NextDialogueId => nextDialogueId;
        public Sprite RewardPhotoSprite => rewardPhotoSprite;
        public Sprite ContainerSprite => containerSprite;
        public Sprite CookingBackground => cookingBackground;
        public Sprite DaughterBackground => daughterBackground;
        public Sprite DishPhotoSprite => dishPhotoSprite;
        public Sprite MotherCompleteBackground => motherCompleteBackground;
        public Sprite MotherCompleteHint => motherCompleteHint;

        public bool IsRequiredIngredient(string itemId)
        {
            return Array.IndexOf(requiredIngredients, itemId) >= 0;
        }

        public bool IsCorrectSeasoning(string itemId)
        {
            return string.Equals(correctSeasoning, itemId, StringComparison.Ordinal);
        }

        /// <summary>根据食材/调料 ID 查找对应 Sprite，未配置时返回 null。</summary>
        public Sprite GetIngredientSprite(string itemId)
        {
            if (ingredientSprites == null) return null;
            foreach (var entry in ingredientSprites)
            {
                if (entry.ingredientId == itemId) return entry.sprite;
            }
            return null;
        }
    }
}
