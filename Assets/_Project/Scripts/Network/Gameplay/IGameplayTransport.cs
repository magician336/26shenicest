using System;
using DoNotForgetMe.MiniGame.Cooking;

namespace DoNotForgetMe.Network.Gameplay
{
    /// <summary>
    /// Host 权威玩法层与具体网络库之间的桥。Fusion 实现见 Network/Fusion。
    /// </summary>
    public interface IGameplayTransport
    {
        bool IsHostAuthority { get; }
        SessionRole LocalRole { get; }

        event Action<GameplayIntent> IntentReceived;
        event Action<CookingGameState> StateReceived;

        void SendIntent(GameplayIntent intent);
        void BroadcastState(CookingGameState state);
    }
}
