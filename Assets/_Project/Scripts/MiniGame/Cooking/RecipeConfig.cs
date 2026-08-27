using System;
using UnityEngine;

namespace DoNotForgetMe.MiniGame.Cooking
{
    /// <summary>做饭小游戏配方与双端提示配置。</summary>
    [CreateAssetMenu(menuName = "Do Not Forget Me/Cooking Recipe", fileName = "CookingRecipe")]
    public class RecipeConfig : ScriptableObject
    {
        public string recipeId = "tomato_egg";
        public string displayName = "番茄炒蛋";
        public CookingStep[] steps =
        {
            CookingStep.WashTomato,
            CookingStep.CutTomato,
            CookingStep.BeatEgg,
            CookingStep.HeatPan,
            CookingStep.StirFry,
            CookingStep.Plate
        };

        [TextArea] public string[] hostPrompts =
        {
            "你看到番茄和水槽。告诉对方先准备鸡蛋。",
            "把番茄切好，但不要告诉对方锅已经热了。",
            "等待对方把鸡蛋打散。",
            "开火热锅，提醒对方别急着下番茄。",
            "把食材倒进锅里翻炒。",
            "装盘完成。"
        };

        [TextArea] public string[] clientPrompts =
        {
            "你看到鸡蛋和碗。告诉 Host 鸡蛋还没处理。",
            "等待 Host 处理番茄。",
            "打散鸡蛋，然后告诉 Host 可以热锅。",
            "观察火候条，提醒 Host 什么时候下锅。",
            "根据颜色告诉 Host 是否还要继续翻炒。",
            "确认摆盘方向。"
        };

        public string GetPrompt(bool hostSide, int stepIndex)
        {
            var prompts = hostSide ? hostPrompts : clientPrompts;
            if (prompts == null || prompts.Length == 0) return string.Empty;
            var index = Mathf.Clamp(stepIndex, 0, prompts.Length - 1);
            return prompts[index];
        }

        public CookingStep GetStep(int stepIndex)
        {
            if (steps == null || steps.Length == 0) return CookingStep.Complete;
            var index = Mathf.Clamp(stepIndex, 0, steps.Length - 1);
            return steps[index];
        }

        public bool IsFinalStep(int stepIndex)
        {
            return steps == null || steps.Length == 0 || stepIndex >= steps.Length - 1;
        }
    }
}
