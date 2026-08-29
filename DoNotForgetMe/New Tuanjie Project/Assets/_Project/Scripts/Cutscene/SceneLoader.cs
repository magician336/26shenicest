using DoNotForgetMe.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoNotForgetMe.Cutscene
{
    /// <summary>
    /// 统一场景加载入口。封装同场景守卫和 Build Settings 存在性检查。
    /// 供 IntroCutsceneController / SceneTransitionTrigger / Coordinator 共用。
    /// </summary>
    public static class SceneLoader
    {
        /// <summary>加载目标场景。同场景或目标不存在时安全跳过。</summary>
        public static void Load(string sceneName)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
            {
                Debug.Log($"[SceneLoader] 已在场景 '{sceneName}' 中，跳过加载。");
                return;
            }

            if (!SceneNames.ExistsInBuildSettings(sceneName))
            {
                Debug.LogWarning($"[SceneLoader] 场景 '{sceneName}' 不在 Build Settings 中，跳过加载。");
                return;
            }

            Debug.Log($"[SceneLoader] 加载场景 → {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
    }
}
