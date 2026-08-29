# 对白序列状态纳入 GameplaySnapshot

对白序列的进度（当前序列 ID、当前条目索引）通过 GameplaySnapshot 同步，而非走独立的 RPC 通道。新增 `GameplayPhase.Dialogue` 阶段，Host 通过 `AdvanceDialogue` / `FinishDialogue` Intent 推进，两端经 `StateChanged` 事件渲染字幕。

## Considered Options

- **RPC / 网络事件**：对白进度不进 Snapshot，走轻量同步通道。每条台词只传一个 int（entryIndex），开销更小。
- **各自本地播放**：不做进度同步。被否决，因为 Host 独占推进，Client 必须收到推进信号。

## Consequences

- 一段 15 句的过场会产生 15 次 Intent → Snapshot 序列化 → 广播 → StateChanged，在 Fusion 下是 15 次网络状态同步。性能开销大于 RPC，但 hackathon 阶段对白序列长度有限（最长约 15 句），可接受。
- 对白进度成为权威状态的一部分，断线重连时可从 Snapshot 恢复到当前条目，这是 RPC 方案无法做到的。
- 与现有 `cooking` / `bagua` 子状态在 Snapshot 中并列，Coordinator 的路由模式统一：按 `miniGameId` 路由小游戏、按 `phase == Dialogue` 路由对白。
