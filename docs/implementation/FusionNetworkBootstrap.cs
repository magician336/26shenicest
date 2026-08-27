// ============================================================================
// FusionNetworkBootstrap.cs
// 依赖 Photon Fusion 2 —— 必须在 Fusion SDK 导入项目之后，
// 才能移入 Assets/_Project/Scripts/Network/Fusion/ 目录（否则编译失败）。
//
// 职责：
// 1. 启动时兜底注入 Fusion AppId（不覆盖 Fusion Hub 中已填的值）；
// 2. 把 FusionSessionService 注册为全局会话服务（NetworkSessionManager），
//    替换掉“未安装”桩实现——主菜单由此获得真实联机能力。
// ============================================================================

using DoNotForgetMe.Network;
using Fusion;
using UnityEngine;

public static class FusionNetworkBootstrap
{
    // hackathon demo 用的 Fusion AppId（Photon Dashboard 创建）。
    // 注意：此 AppId 绑定 Photon Cloud 免费额度；正式项目建议
    // 改为在 Fusion Hub / PhotonAppSettings 资产中配置并移除本硬编码。
    private const string FusionAppId = "ecb11ee8-d468-495d-bb7f-6c6efd8a1242";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        // 1) 兜底注入 AppId（用户若在 Fusion Hub 里填过则不覆盖）。
        var appSettings = PhotonAppSettings.Global.AppSettings;
        if (string.IsNullOrEmpty(appSettings.AppIdFusion))
        {
            appSettings.AppIdFusion = FusionAppId;
            Debug.Log("[Net] Fusion AppId injected by FusionNetworkBootstrap");
        }

        // 2) 注册会话服务（幂等：Fast Enter Play Mode 下避免重复挂载）。
        if (NetworkSessionManager.Service is NotInstalledSessionService)
        {
            var go = new GameObject("FusionSessionService");
            Object.DontDestroyOnLoad(go);
            NetworkSessionManager.Register(go.AddComponent<FusionSessionService>());
            Debug.Log("[Net] FusionSessionService registered");
        }
    }
}
