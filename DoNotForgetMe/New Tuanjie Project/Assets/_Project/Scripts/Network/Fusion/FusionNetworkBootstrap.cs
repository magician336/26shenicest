#if FUSION_PRESENT
using DoNotForgetMe.Network;
using UnityEngine;

namespace DoNotForgetMe.Network.Fusion
{
    /// <summary>Fusion 已导入时注册真实会话服务。AppId 必须在 Fusion Hub 本地配置。</summary>
    public static class FusionNetworkBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (!(NetworkSessionManager.Service is NotInstalledSessionService)) return;
            var go = new GameObject("FusionSessionService");
            Object.DontDestroyOnLoad(go);
            NetworkSessionManager.Register(go.AddComponent<FusionSessionService>());
        }
    }
}
#endif
