namespace DoNotForgetMe.Network
{
    /// <summary>
    /// 联机会话服务的静态门面。
    /// 游戏代码统一通过 <see cref="Service"/> 访问网络能力，不关心底层实现。
    /// 默认挂载未安装桩（NotInstalledSessionService）；
    /// Fusion SDK 导入后由 FusionNetworkBootstrap 在启动时注册 FusionSessionService。
    /// </summary>
    public static class NetworkSessionManager
    {
        private static INetworkSessionService _service = new NotInstalledSessionService();

        /// <summary>当前会话服务实例（永不为 null）。</summary>
        public static INetworkSessionService Service => _service;

        /// <summary>替换底层实现。仅由网络层安装器调用（如 FusionNetworkBootstrap）。</summary>
        public static void Register(INetworkSessionService service)
        {
            if (service != null)
            {
                _service = service;
            }
        }
    }
}
