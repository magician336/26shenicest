using System;
using DoNotForgetMe.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoNotForgetMe.Network.Local
{
    /// <summary>
    /// 单进程调试用会话服务——不依赖 Photon Fusion，无网络通信。
    /// 默认 Role = Host，可通过 <see cref="SetRole"/> 在运行时切换。
    /// StartHost/StartClient 不强制加载场景：调试时直接打开目标场景即可。
    /// Leave 返回 MainMenu。
    /// </summary>
    public class LocalDebugService : INetworkSessionService
    {
        private SessionState _state = SessionState.Disconnected;
        public SessionState State => _state;
        public SessionRole Role { get; private set; } = SessionRole.Host;
        public bool IsAvailable => true;

        public event Action<SessionState> StateChanged;
        public event Action<string> Error;

        public void StartHost(string sessionName)
        {
            if (_state == SessionState.Connected) return;
            SetState(SessionState.Connecting);

            // 单人模式：加载 Intro（开场过场）
            var currentScene = SceneManager.GetActiveScene().name;
            if (!SceneNames.IsGameScene(currentScene))
            {
                if (SceneNames.ExistsInBuildSettings(SceneNames.Intro))
                {
                    SceneManager.LoadScene(SceneNames.Intro);
                }
                else if (SceneNames.ExistsInBuildSettings(SceneNames.Kitchen))
                {
                    SceneManager.LoadScene(SceneNames.Kitchen);
                }
            }

            SetState(SessionState.Connected);
        }

        public void StartClient(string sessionName)
        {
            if (_state == SessionState.Connected) return;
            SetState(SessionState.Connecting);
            SetState(SessionState.Connected);
        }

        public void Leave()
        {
            SetState(SessionState.Disconnected);

            if (SceneManager.GetActiveScene().name != SceneNames.MainMenu)
            {
                SceneManager.LoadScene(SceneNames.MainMenu);
            }
        }

        private void SetState(SessionState newState)
        {
            if (_state == newState) return;
            _state = newState;
            StateChanged?.Invoke(_state);
        }

        /// <summary>运行时切换角色（Tab 键调用）。</summary>
        public void SetRole(SessionRole role)
        {
            Role = role;
        }
    }
}
