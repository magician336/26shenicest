#if FUSION_PRESENT
using UnityEngine;

namespace DoNotForgetMe.Network.Fusion
{
    /// <summary>
    /// Fusion SDK 启用后注册真实网络会话服务。AppId 由本机 PhotonAppSettings 配置，不写入代码仓库。
    /// </summary>
    public static class FusionNetworkBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (NetworkSessionManager.Service is NotInstalledSessionService)
            {
                var go = new GameObject("FusionSessionService");
                Object.DontDestroyOnLoad(go);
                NetworkSessionManager.Register(go.AddComponent<FusionSessionService>());
                Debug.Log("[Net] FusionSessionService registered");
            }
        }
    }
}
#endif
