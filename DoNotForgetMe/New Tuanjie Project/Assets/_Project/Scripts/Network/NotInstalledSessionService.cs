using System;

namespace DoNotForgetMe.Network
{
    /// <summary>
    /// Fusion SDK 未导入时的占位实现。
    /// 所有操作只报错提示，保证项目在无网络库状态下可编译、主菜单可预览。
    /// </summary>
    public class NotInstalledSessionService : INetworkSessionService
    {
        private const string Message =
            "网络层未安装：请先导入 Photon Fusion SDK（见 docs/install-fusion.md）";

        public SessionState State => SessionState.Disconnected;
        public SessionRole Role => SessionRole.None;
        public bool IsAvailable => false;

        public event Action<SessionState> StateChanged;
        public event Action<string> Error;

        public void StartHost(string sessionName) => Error?.Invoke(Message);
        public void StartClient(string sessionName) => Error?.Invoke(Message);

        public void Leave()
        {
            // 无会话可离开，静默即可。
        }
    }
}
