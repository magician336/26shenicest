#if FUSION_PRESENT
using DoNotForgetMe.Network.Gameplay;
using Fusion;
using UnityEngine;

namespace DoNotForgetMe.Network.Fusion
{
    /// <summary>
    /// Fusion 与纯 C# 流程层的唯一连接点。将 Client 意图上送 Host，并广播 Host 快照。
    /// 需要与 NetworkObject 挂在同一个场景对象上。
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class FusionGameplayBridge : NetworkBehaviour, IGameplayTransport
    {
        private SessionGameplayCoordinator _coordinator;

        public override void Spawned()
        {
            TryRegister();
        }

        private void Start()
        {
            TryRegister();
        }

        private void TryRegister()
        {
            if (_coordinator != null) return;
            _coordinator = SessionGameplayCoordinator.Instance;
            if (_coordinator == null)
            {
                return;
            }
            _coordinator.RegisterTransport(this);
        }

        public void SendIntent(GameplayIntent intent)
        {
            RpcSendIntent(JsonUtility.ToJson(intent));
        }

        public void BroadcastState(GameplaySnapshot snapshot)
        {
            if (!Object.HasStateAuthority) return;
            RpcApplyState(JsonUtility.ToJson(snapshot));
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RpcSendIntent(string payload)
        {
            var intent = JsonUtility.FromJson<GameplayIntent>(payload);
            // Host 的本地意图直接进入协调器；此 RPC 路径只接收 Client 上送的意图。
            _coordinator?.HandleHostIntent(intent, SessionRole.Client);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcApplyState(string payload)
        {
            var state = JsonUtility.FromJson<GameplaySnapshot>(payload);
            _coordinator?.ApplyAuthoritativeState(state);
        }
    }
}
#endif
