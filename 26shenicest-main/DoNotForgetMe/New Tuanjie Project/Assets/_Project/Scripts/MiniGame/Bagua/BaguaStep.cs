namespace DoNotForgetMe.MiniGame.Bagua
{
    /// <summary>八卦小游戏的流程步骤，由 Host 权威推进。</summary>
    public enum BaguaStep
    {
        /// <summary>Client 听故事并完成人物—物品配对。</summary>
        ClientMatchItems,
        /// <summary>Host 在老照片上完成姓名投放。</summary>
        HostIdentifyPeople,
        /// <summary>小游戏已完成。</summary>
        Complete
    }
}
