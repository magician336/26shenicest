// ============================================================================
// FusionNetworkBootstrap.cs
// 依赖 Photon Fusion 2 —— 必须在 Fusion SDK 导入项目之后，
// 才能移入 Assets/_Project/Scripts/Network/Fusion/ 目录（否则编译失败）。
//
// 职责：
// 1. 把 FusionSessionService 注册为全局会话服务（NetworkSessionManager），
//    替换掉“未安装”桩实现——主菜单由此获得真实联机能力。
// ============================================================================

using DoNotForgetMe.Network;
using UnityEngine;

public static class FusionNetworkBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        // 在 Fusion Hub 本地配置 AppId，不将任何 AppId 提交到仓库。
        // 注册会话服务（幂等：Fast Enter Play Mode 下避免重复挂载）。
        if (NetworkSessionManager.Service is NotInstalledSessionService)
        {
            var go = new GameObject("FusionSessionService");
            Object.DontDestroyOnLoad(go);
            NetworkSessionManager.Register(go.AddComponent<FusionSessionService>());
            Debug.Log("[Net] FusionSessionService registered");
        }
    }
}
