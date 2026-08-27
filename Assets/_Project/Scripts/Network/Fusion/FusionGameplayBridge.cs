#if FUSION_PRESENT
using System;
using DoNotForgetMe.MiniGame.Cooking;
using DoNotForgetMe.Network.Gameplay;
using global::Fusion;
using UnityEngine;

namespace DoNotForgetMe.Network.Fusion
{
    /// <summary>把玩法意图和状态同步桥接到 Photon Fusion RPC。</summary>
    [RequireComponent(typeof(NetworkObject))]
    public class FusionGameplayBridge : NetworkBehaviour, IGameplayTransport
    {
        public bool IsHostAuthority => Object != null && Object.HasStateAuthority;
        public SessionRole LocalRole => NetworkSessionManager.Service.Role;

        public event Action<GameplayIntent> IntentReceived;
        public event Action<CookingGameState> StateReceived;

        public override void Spawned()
        {
            FindObjectOfType<SessionGameplayCoordinator>()?.SetTransport(this);
        }

        public void SendIntent(GameplayIntent intent)
        {
            if (IsHostAuthority)
            {
                IntentReceived?.Invoke(intent);
                return;
            }

            RPC_SendIntent((int)intent.Role, (int)intent.Type, intent.TargetId ?? string.Empty, (int)intent.CookingStep);
        }

        public void BroadcastState(CookingGameState state)
        {
            if (!IsHostAuthority || state == null) return;

            var json = JsonUtility.ToJson(state);
            RPC_BroadcastState(json);
            StateReceived?.Invoke(state.Clone());
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SendIntent(int role, int type, string targetId, int cookingStep)
        {
            IntentReceived?.Invoke(new GameplayIntent
            {
                Role = (SessionRole)role,
                Type = (GameplayIntentType)type,
                TargetId = targetId,
                CookingStep = (CookingStep)cookingStep
            });
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_BroadcastState(string json)
        {
            var state = JsonUtility.FromJson<CookingGameState>(json);
            StateReceived?.Invoke(state);
        }
    }
}
#endif
