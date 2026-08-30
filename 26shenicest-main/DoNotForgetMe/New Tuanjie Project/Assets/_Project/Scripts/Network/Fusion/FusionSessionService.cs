#if FUSION_PRESENT
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoNotForgetMe.Core;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoNotForgetMe.Network.Fusion
{
    /// <summary>Photon Fusion Host 模式的会话生命周期实现。
    /// 连接后加载 Intro 场景（开场过场），非旧的 Game 场景。</summary>
    public class FusionSessionService : SimulationBehaviour, INetworkRunnerCallbacks, INetworkSessionService
    {
        private NetworkRunner _runner;
        private int _pendingSceneIndex = -1;
        private bool _bridgeSpawned;

        public SessionState State { get; private set; } = SessionState.Disconnected;
        public SessionRole Role { get; private set; } = SessionRole.None;
        public bool IsAvailable => true;
        public event Action<SessionState> StateChanged;
        public event Action<string> Error;

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
            SessionGameplayCoordinator.Instance?.SaveLastStableState();
            if (_runner != null) _runner.Shutdown();
            else SetState(SessionState.Disconnected);
        }

        private async Task StartGame(GameMode mode, string sessionName)
        {
            if (_runner != null)
            {
                Error?.Invoke("已在会话中，请先退出当前会话");
                return;
            }

            // 联机路径：连接后加载 Intro 场景（开场过场）
            var initialSceneIndex = FindBuildSceneIndex(SceneNames.Intro);
            if (initialSceneIndex < 0)
            {
                // Intro 场景尚未创建，回退到 Kitchen 场景
                initialSceneIndex = FindBuildSceneIndex(SceneNames.Kitchen);
            }
            if (initialSceneIndex < 0)
            {
                Error?.Invoke("未在 Build Settings 中找到游戏场景。");
                return;
            }

            SetState(SessionState.Connecting);
            Role = mode == GameMode.Host ? SessionRole.Host : SessionRole.Client;
            var runnerGo = new GameObject("NetworkRunner");
            DontDestroyOnLoad(runnerGo);
            _runner = runnerGo.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);

            // Host 不在 StartGame 时加载场景——留在 MainMenu 展示房间码，
            // 等 Client 加入后（OnPlayerJoined）再加载游戏场景。
            // Client 也不指定场景——NetworkSceneManager 会自动同步到 Host 的场景。
            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SessionName = sessionName,
                PlayerCount = 2,
                SceneManager = runnerGo.AddComponent<NetworkSceneManagerDefault>()
            });

            if (result.Ok)
            {
                _pendingSceneIndex = initialSceneIndex;
                SessionGameplayCoordinator.OnTransportNeeded += OnTransportNeeded;
                SetState(SessionState.Connected);
                return;
            }

            Error?.Invoke("会话启动失败：" + result.ShutdownReason);
            CleanupRunner();
            SetState(SessionState.Disconnected);
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            // Host 侧：等 Client（第二个玩家）加入后再加载游戏场景。
            // 不能用 player != runner.LocalPlayer 判断——Host 启动时 LocalPlayer 可能尚未赋值。
            // 用 SessionInfo.PlayerCount >= 2 确保至少有两个玩家才加载场景。
            if (Role == SessionRole.Host && _pendingSceneIndex >= 0 && runner.SessionInfo.PlayerCount >= 2)
            {
                runner.LoadScene(SceneRef.FromIndex(_pendingSceneIndex));
                _pendingSceneIndex = -1;
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (Role == SessionRole.Host && State == SessionState.Connected)
            {
                Leave();
            }
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            CleanupRunner();
            SetState(SessionState.Disconnected);
            if (SceneManager.GetActiveScene().name != SceneNames.MainMenu)
            {
                SceneManager.LoadScene(SceneNames.MainMenu);
            }
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Error?.Invoke("连接 Photon Cloud 失败：" + reason);
            CleanupRunner();
            SetState(SessionState.Disconnected);
        }

        private void OnTransportNeeded(SessionGameplayCoordinator coordinator)
        {
            if (_bridgeSpawned || _runner == null || !_runner.IsRunning) return;

            var prefab = Resources.Load<GameObject>("NetworkPrefabs/FusionGameplayBridge");
            if (prefab == null)
            {
                Debug.LogError("[FusionSessionService] FusionGameplayBridge prefab 未找到于 Resources/NetworkPrefabs/");
                return;
            }

            _runner.Spawn(prefab, Vector3.zero, Quaternion.identity);
            _bridgeSpawned = true;
            Debug.Log("[FusionSessionService] FusionGameplayBridge 已 spawn");
        }

        // --- INetworkRunnerCallbacks 空实现 ---
        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }
        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
        {
            // 销毁场景中残留的 LocalGameplayBridge，避免 Error log 和 debugSingleProcess 干扰
            var localBridge = FindAnyObjectByType<DoNotForgetMe.Network.Local.LocalGameplayBridge>();
            if (localBridge != null)
            {
                Destroy(localBridge);
                Debug.Log("[FusionSessionService] 已销毁残留的 LocalGameplayBridge");
            }
        }

        private void CleanupRunner()
        {
            SessionGameplayCoordinator.OnTransportNeeded -= OnTransportNeeded;
            if (_runner != null && _runner.gameObject != null) Destroy(_runner.gameObject);
            _runner = null;
            _pendingSceneIndex = -1;
            _bridgeSpawned = false;
            Role = SessionRole.None;
        }

        private void SetState(SessionState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(state);
        }

        private static int FindBuildSceneIndex(string sceneName)
        {
            for (var index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(index);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name == sceneName) return index;
            }
            return -1;
        }
    }
}
#endif
