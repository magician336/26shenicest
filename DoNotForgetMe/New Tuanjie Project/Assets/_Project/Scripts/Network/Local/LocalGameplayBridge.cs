using DoNotForgetMe.Network.Gameplay;
using UnityEngine;

namespace DoNotForgetMe.Network.Local
{
    /// <summary>
    /// 单进程调试用的网络桥接——替代 FusionGameplayBridge。
    /// 将 SessionGameplayCoordinator 与 LocalDebugService 对接，
    /// 并提供 Tab 键切换 Host/Client 角色。
    /// </summary>
    [RequireComponent(typeof(SessionGameplayCoordinator))]
    public class LocalGameplayBridge : MonoBehaviour, IGameplayTransport
    {
        private SessionGameplayCoordinator _coordinator;
        private LocalDebugService _service;

        private void Start()
        {
            _coordinator = GetComponent<SessionGameplayCoordinator>();
            _service = NetworkSessionManager.Service as LocalDebugService;

            if (_service == null)
            {
                Debug.LogError("[LocalGameplayBridge] NetworkSessionManager.Service 不是 LocalDebugService。" +
                              "请确认 LocalNetworkBootstrap 已运行且未定义 FUSION_PRESENT。");
                return;
            }

            if (_coordinator == null)
            {
                Debug.LogError("[LocalGameplayBridge] 缺少 SessionGameplayCoordinator");
                return;
            }

            _coordinator.StateChanged += OnStateChanged;
            _coordinator.RegisterTransport(this);

            Debug.Log("[LocalGameplayBridge] 调试模式就绪。Tab 切换 Host/Client 角色。");
        }

        private void OnDestroy()
        {
            if (_coordinator != null)
            {
                _coordinator.StateChanged -= OnStateChanged;
            }
        }

        private void Update()
        {
            if (_service == null || _coordinator == null) return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                SwitchRole();
            }
        }

        // --- IGameplayTransport ---

        /// <summary>
        /// Client 角色时由 coordinator.Request() 调用。
        /// 单进程环回：直接以 Client 身份交给 Host 权威处理。
        /// </summary>
        public void SendIntent(GameplayIntent intent)
        {
            _coordinator.HandleHostIntent(intent, SessionRole.Client);
        }

        /// <summary>
        /// Host 广播状态——单进程下 state 已在 coordinator 内，
        /// StateChanged 事件已触发 UI 更新，无需额外操作。
        /// </summary>
        public void BroadcastState(GameplaySnapshot snapshot)
        {
        }

        // --- 内部逻辑 ---

        private void SwitchRole()
        {
            // 探索阶段或终局阶段不允许切角色
            if (_coordinator.State.phase == GameplayPhase.Exploration ||
                _coordinator.State.phase == GameplayPhase.GameEnded)
            {
                Debug.Log("[Debug] 当前阶段不支持切换角色。");
                return;
            }

            var newRole = _service.Role == SessionRole.Host
                ? SessionRole.Client
                : SessionRole.Host;

            _service.SetRole(newRole);

            // 触发重渲染，使当前小游戏视图显示新角色的私有视图
            _coordinator.ApplyAuthoritativeState(_coordinator.State);

            Debug.Log($"[Debug] 角色切换 → {newRole}" +
                      $"（{ (newRole == SessionRole.Host ? "女儿端" : "母亲端")}）");
        }

        private void OnStateChanged(GameplaySnapshot snapshot)
        {
            // 小游戏结束回到探索阶段时，自动切回 Host 确保能再次触发小游戏
            if (snapshot.phase == GameplayPhase.Exploration &&
                _service.Role != SessionRole.Host)
            {
                _service.SetRole(SessionRole.Host);
                // 重新渲染以 Host 角色显示 UI（照片拾取等需要 Host 权限）
                _coordinator.ApplyAuthoritativeState(_coordinator.State);
                Debug.Log("[Debug] 回到探索阶段，自动切回 Host（女儿）。");
            }
        }
    }
}
