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
        public SessionState State => SessionState.Connected;
        public SessionRole Role { get; private set; } = SessionRole.Host;
        public bool IsAvailable => true;

        public event Action<SessionState> StateChanged;
        public event Action<string> Error;

        public void StartHost(string sessionName)
        {
            // 调试模式：已在游戏场景中则不加载；否则加载 Intro（开场过场）
            var currentScene = SceneManager.GetActiveScene().name;
            if (!SceneNames.IsGameScene(currentScene))
            {
                if (SceneNames.ExistsInBuildSettings(SceneNames.Intro))
                {
                    // Intro 存在 → 开场过场 → 自动转 LivingRoom
                    SceneManager.LoadScene(SceneNames.Intro);
                }
                else if (SceneNames.ExistsInBuildSettings(SceneNames.Kitchen))
                {
                    // Intro 未创建，回退到 Kitchen
                    SceneManager.LoadScene(SceneNames.Kitchen);
                }
            }
        }

        public void StartClient(string sessionName)
        {
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
        }

        public void Leave()
        {
            if (SceneManager.GetActiveScene().name != SceneNames.MainMenu)
            {
                SceneManager.LoadScene(SceneNames.MainMenu);
            }
        }

        /// <summary>运行时切换角色（Tab 键调用）。</summary>
        public void SetRole(SessionRole role)
        {
            Role = role;
        }
    }
}
