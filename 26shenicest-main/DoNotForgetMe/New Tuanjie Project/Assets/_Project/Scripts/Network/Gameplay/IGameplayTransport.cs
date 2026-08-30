namespace DoNotForgetMe.Network.Gameplay
{
    /// <summary>
    /// 网络适配边界。Fusion 实现负责把客户端意图送到 Host，并把 Host 快照广播回双方。
    /// 领域与 UI 层不引用 Fusion。
    /// </summary>
    public interface IGameplayTransport
    {
        void SendIntent(GameplayIntent intent);
        void BroadcastState(GameplaySnapshot snapshot);
    }
}
