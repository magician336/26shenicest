// ============================================================================
// FusionSessionService.cs
// 依赖 Photon Fusion 2 —— 必须在 Fusion SDK 导入项目之后，
// 才能移入 Assets/_Project/Scripts/Network/Fusion/ 目录（否则编译失败）。
//
// 职责（对应 ADR 0001 / CONTEXT.md 中敲定的决策）：
// - Host 模式会话：创建房间者 = Host = 探索阶段操作者（玩家1·角色A）
// - 房间码即 Fusion SessionName（4~6 位，RoomCodeGenerator 生成）
// - 会话建立即加载 Game 场景（双端同步）
// - 断线策略：任一方断线/离开 → 会话结束 → 双端回主菜单
// ============================================================================

using System;
using System.Threading.Tasks;
using DoNotForgetMe.Network;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// INetworkSessionService 的 Photon Fusion 2 实现（Host 模式）。
/// 挂载在常驻 GameObject 上（由 FusionNetworkBootstrap 创建），
/// 同时继承 NetworkRunnerBehaviour 以获得 Runner 回调。
/// </summary>
public class FusionSessionService : NetworkRunnerBehaviour
{
    public const string GameSceneName = "Game";
    public const string MainMenuSceneName = "MainMenu";

    private NetworkRunner _runner;

    public SessionState State { get; private set; } = SessionState.Disconnected;
    public SessionRole Role { get; private set; } = SessionRole.None;
    public bool IsAvailable => true;

    public event Action<SessionState> StateChanged;
    public event Action<string> Error;

    // ------------------------------------------------------------------
    // INetworkSessionService
    // ------------------------------------------------------------------

    public async void StartHost(string sessionName)
    {
        await StartGame(GameMode.Host, sessionName);
    }

    public async void StartClient(string sessionName)
    {
        await StartGame(GameMode.Client, sessionName);
    }

    public void Leave()
    {
        if (_runner != null)
        {
            // Shutdown 完成后经 OnShutdown 收尾（清 runner、回主菜单）。
            _runner.Shutdown();
        }
        else
        {
            SetState(SessionState.Disconnected);
        }
    }

    // ------------------------------------------------------------------
    // 会话建立
    // ------------------------------------------------------------------

    private async Task StartGame(GameMode mode, string sessionName)
    {
        if (_runner != null)
        {
            Error?.Invoke("已在会话中，请先退出当前会话");
            return;
        }

        var gameSceneIndex = FindBuildSceneIndex(GameSceneName);
        if (gameSceneIndex < 0)
        {
            Error?.Invoke("未在 Build Settings 中找到 Game 场景：请先运行 Tools/3C Setup/Create Basic Scene");
            return;
        }

        SetState(SessionState.Connecting);
        Role = mode == GameMode.Host ? SessionRole.Host : SessionRole.Client;

        var runnerGo = new GameObject("NetworkRunner");
        DontDestroyOnLoad(runnerGo);

        _runner = runnerGo.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true; // 输入采集统一开启；探索阶段的观战门控由游戏逻辑负责
        _runner.AddCallbacks(this);

        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            PlayerCount = 2,          // 双人固定会话
            Scene = SceneRef.FromIndex(gameSceneIndex), // 会话建立即进入 Game 场景（双端同步）
            SceneManager = runnerGo.AddComponent<NetworkSceneManagerDefault>(),
        });

        if (result.Ok)
        {
            SetState(SessionState.Connected);
        }
        else
        {
            var reason = result.ShutdownReason != ShutdownReason.None
                ? result.ShutdownReason.ToString()
                : "未知原因";
            Error?.Invoke("会话启动失败：" + reason);
            CleanupRunner();
            SetState(SessionState.Disconnected);
        }
    }

    // ------------------------------------------------------------------
    // Runner 回调：断线即结束会话（ADR 0001 决策）
    // ------------------------------------------------------------------

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Host 侧：Client 断线/退出 → 结束会话（不做等待重连）。
        if (Role == SessionRole.Host && State == SessionState.Connected)
        {
            Debug.Log("[Net] 对方已离开，结束会话");
            Leave();
        }
    }

    public override void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        // Client 侧：与 Host 失去连接。runner 会随后 shutdown，收尾在 OnShutdown。
        Debug.Log("[Net] 与主机断开：" + reason);
    }

    public override void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (shutdownReason != ShutdownReason.Ok)
        {
            Debug.Log("[Net] 会话关闭：" + shutdownReason);
        }

        CleanupRunner();
        SetState(SessionState.Disconnected);
        ReturnToMainMenu();
    }

    public override void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Error?.Invoke("连接失败：无法连接到 Photon Cloud（请检查网络）。原因：" + reason);
        CleanupRunner();
        SetState(SessionState.Disconnected);
    }

    // ------------------------------------------------------------------
    // 内部
    // ------------------------------------------------------------------

    private void CleanupRunner()
    {
        if (_runner != null && _runner.gameObject != null)
        {
            Destroy(_runner.gameObject);
        }
        _runner = null;
        Role = SessionRole.None;
    }

    private void ReturnToMainMenu()
    {
        // 只有已在游戏场景中才需要返回；主菜单中的失败/取消不切场景。
        if (SceneManager.GetActiveScene().name != MainMenuSceneName)
        {
            SceneManager.LoadScene(MainMenuSceneName);
        }
    }

    private void SetState(SessionState state)
    {
        if (State == state) return;
        State = state;
        StateChanged?.Invoke(state);
    }

    private static int FindBuildSceneIndex(string sceneName)
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path)) continue;
            var name = path.Substring(path.LastIndexOf('/') + 1);
            if (name.EndsWith(".unity")) name = name.Substring(0, name.Length - ".unity".Length);
            if (name == sceneName) return i;
        }
        return -1;
    }
}
