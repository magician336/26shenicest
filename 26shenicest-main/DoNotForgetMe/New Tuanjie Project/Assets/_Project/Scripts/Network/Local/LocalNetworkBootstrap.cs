#if !FUSION_PRESENT
using UnityEngine;

namespace DoNotForgetMe.Network.Local
{
    /// <summary>
    /// 当 Fusion SDK 未导入时，在运行时将桩服务替换为 <see cref="LocalDebugService"/>。
    /// Fusion 导入后（定义 FUSION_PRESENT 宏），本文件不编译，FusionNetworkBootstrap 接管。
    /// </summary>
    public static class LocalNetworkBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (!(NetworkSessionManager.Service is NotInstalledSessionService)) return;

            NetworkSessionManager.Register(new LocalDebugService());
            Debug.Log("[LocalNetworkBootstrap] 已注册 LocalDebugService（单进程调试模式）。");
        }
    }
}
#endif
