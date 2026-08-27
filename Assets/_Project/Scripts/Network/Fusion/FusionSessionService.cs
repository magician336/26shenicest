#if FUSION_PRESENT
using System;
using System.Threading.Tasks;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoNotForgetMe.Network.Fusion
{
    /// <summary>Photon Fusion Host 模式的会话生命周期实现。</summary>
    public class FusionSessionService : NetworkRunnerBehaviour, INetworkSessionService
    {
        public const string GameSceneName = "Game";
        public const string MainMenuSceneName = "MainMenu";

        private NetworkRunner _runner;

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

            var gameSceneIndex = FindBuildSceneIndex(GameSceneName);
            if (gameSceneIndex < 0)
            {
                Error?.Invoke("未在 Build Settings 中找到 Game 场景。");
                return;
            }

            SetState(SessionState.Connecting);
            Role = mode == GameMode.Host ? SessionRole.Host : SessionRole.Client;
            var runnerGo = new GameObject("NetworkRunner");
            DontDestroyOnLoad(runnerGo);
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
                return;
            }

            Error?.Invoke("会话启动失败：" + result.ShutdownReason);
            CleanupRunner();
            SetState(SessionState.Disconnected);
        }

        public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (Role == SessionRole.Host && State == SessionState.Connected)
            {
                Leave();
            }
        }

        public override void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            CleanupRunner();
            SetState(SessionState.Disconnected);
            if (SceneManager.GetActiveScene().name != MainMenuSceneName)
            {
                SceneManager.LoadScene(MainMenuSceneName);
            }
        }

        public override void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Error?.Invoke("连接 Photon Cloud 失败：" + reason);
            CleanupRunner();
            SetState(SessionState.Disconnected);
        }

        private void CleanupRunner()
        {
            if (_runner != null && _runner.gameObject != null) Destroy(_runner.gameObject);
            _runner = null;
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
