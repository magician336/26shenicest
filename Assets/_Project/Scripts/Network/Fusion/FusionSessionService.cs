#if FUSION_PRESENT
using System;
using System.Threading.Tasks;
using DoNotForgetMe.Network.Gameplay;
using global::Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoNotForgetMe.Network.Fusion
{
    /// <summary>
    /// INetworkSessionService 的 Photon Fusion 2 Host 模式实现。
    /// </summary>
    public class FusionSessionService : NetworkRunnerBehaviour, INetworkSessionService
    {
        public const string GameSceneName = "Game";
        public const string MainMenuSceneName = "MainMenu";

        private NetworkRunner _runner;
        private bool _isLeaving;

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
            _isLeaving = true;
            if (_runner != null)
            {
                _runner.Shutdown();
            }
            else
            {
                SetState(SessionState.Disconnected);
            }
        }

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
            _isLeaving = false;

            var runnerGo = new GameObject("NetworkRunner");
            UnityEngine.Object.DontDestroyOnLoad(runnerGo);

            _runner = runnerGo.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);

            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SessionName = sessionName,
                PlayerCount = 2,
                Scene = SceneRef.FromIndex(gameSceneIndex),
                SceneManager = runnerGo.AddComponent<NetworkSceneManagerDefault>()
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

        public override void OnSceneLoadDone(NetworkRunner runner)
        {
            if (Role == SessionRole.Host)
            {
                EnsureGameplayBridge();
            }
        }

        public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (Role == SessionRole.Host && State == SessionState.Connected)
            {
                Debug.Log("[Net] 对方已离开，结束会话");
                Leave();
            }
        }

        public override void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.Log("[Net] 与 Host 断开：" + reason);
        }

        public override void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (shutdownReason != ShutdownReason.Ok)
            {
                Debug.Log("[Net] 会话关闭：" + shutdownReason);
            }

            var wasConnected = State == SessionState.Connected;
            CleanupRunner();
            SetState(SessionState.Disconnected);

            if (wasConnected || !_isLeaving)
            {
                ReturnToMainMenu();
            }
            _isLeaving = false;
        }

        public override void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Error?.Invoke("连接失败：无法连接到 Photon Cloud（请检查网络）。原因：" + reason);
            CleanupRunner();
            SetState(SessionState.Disconnected);
        }

        private void EnsureGameplayBridge()
        {
            if (_runner == null) return;
            if (FindObjectOfType<FusionGameplayBridge>() != null) return;

            var bridge = new GameObject("FusionGameplayBridge");
            var networkObject = bridge.AddComponent<NetworkObject>();
            bridge.AddComponent<FusionGameplayBridge>();
            _runner.Spawn(networkObject);
        }

        private void CleanupRunner()
        {
            if (_runner != null && _runner.gameObject != null)
            {
                UnityEngine.Object.Destroy(_runner.gameObject);
            }
            _runner = null;
            Role = SessionRole.None;
        }

        private void ReturnToMainMenu()
        {
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
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
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
}
#endif
