using System;

namespace DoNotForgetMe.Network
{
    /// <summary>会话角色。Host = 探索阶段的操作者（玩家1·角色A），Client = 观战者（玩家2·角色B）。</summary>
    public enum SessionRole
    {
        None,
        Host,
        Client
    }

    /// <summary>会话状态机的当前状态。</summary>
    public enum SessionState
    {
        /// <summary>未连接（主菜单初始态，或会话已结束）。</summary>
        Disconnected,
        /// <summary>正在建立会话（创建或加入中）。</summary>
        Connecting,
        /// <summary>会话已建立，双端在线。</summary>
        Connected
    }

    /// <summary>
    /// 联机会话服务的抽象接口。
    /// 主菜单与游戏逻辑只依赖本接口，不直接依赖具体网络库（Photon Fusion），
    /// 以便在 Fusion SDK 尚未导入项目时整套代码仍可编译、单端可调试 UI。
    /// 具体实现见 FusionSessionService（Fusion SDK 导入后放入
    /// Assets/_Project/Scripts/Network/Fusion/ 并由 FusionNetworkBootstrap 注册）。
    /// </summary>
    public interface INetworkSessionService
    {
        /// <summary>当前会话状态。</summary>
        SessionState State { get; }

        /// <summary>本端角色。会话建立前为 None。</summary>
        SessionRole Role { get; }

        /// <summary>网络层是否可用（Fusion 未导入时为 false，主菜单据此提示）。</summary>
        bool IsAvailable { get; }

        /// <summary>创建房间（本端成为 Host）。</summary>
        /// <param name="sessionName">房间码，作为 Fusion 会话名。</param>
        void StartHost(string sessionName);

        /// <summary>加入房间（本端成为 Client）。</summary>
        /// <param name="sessionName">房间码。</param>
        void StartClient(string sessionName);

        /// <summary>主动离开会话（触发与断线相同的收尾路径）。</summary>
        void Leave();

        /// <summary>状态迁移时触发（UI 据此更新文案）。</summary>
        event Action<SessionState> StateChanged;

        /// <summary>会话失败或中途中断时触发，参数为面向玩家的错误描述。</summary>
        event Action<string> Error;
    }
}
