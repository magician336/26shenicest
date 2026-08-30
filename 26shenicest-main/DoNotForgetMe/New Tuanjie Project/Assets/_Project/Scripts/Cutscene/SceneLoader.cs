using DoNotForgetMe.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoNotForgetMe.Cutscene
{
    /// <summary>
    /// 统一场景加载入口。封装同场景守卫和 Build Settings 存在性检查。
    /// 供 IntroCutsceneController / SceneTransitionTrigger / Coordinator 共用。
    /// 联机模式下通过 Fusion NetworkRunner.LoadScene 同步给所有客户端。
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

#if FUSION_PRESENT
            // 联机模式：通过 Fusion NetworkRunner 加载场景，自动同步给 Client
            var runner = Object.FindObjectOfType<Fusion.NetworkRunner>();
            if (runner != null && runner.IsRunning)
            {
                // Client 不主动加载场景——Fusion 会自动同步到 Host 加载的场景
                if (!runner.IsSceneAuthority)
                {
                    Debug.Log($"[SceneLoader] Client 端跳过场景加载 '{sceneName}'，等待 Host 同步。");
                    return;
                }

                var sceneIndex = SceneIndexForName(sceneName);
                if (sceneIndex >= 0)
                {
                    Debug.Log($"[SceneLoader] Fusion 加载场景 → {sceneName} (index={sceneIndex})");
                    runner.LoadScene(Fusion.SceneRef.FromIndex(sceneIndex));
                    return;
                }
            }
#endif

            Debug.Log($"[SceneLoader] 加载场景 → {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        private static int SceneIndexForName(string sceneName)
        {
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                    return i;
            }
            return -1;
        }
    }
}
