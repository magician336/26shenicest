# 0003: 进度保存由 Host 本机持有

## Status

accepted

## 决策

联机会话中的可恢复进度只由 **Host** 本机保存。Client 不保存权威进度，也不参与合并存档。继续游戏时，Host 从本地 Host 存档恢复玩法状态并创建新的会话；Client 使用新的房间码重新加入。

## 背景

当前联机架构采用 Photon Fusion Host 模式，Host 负责权威游戏决策。若双方都保存进度，就需要处理冲突、旧状态覆盖、Client 本地状态伪造等问题，和本项目 hackathon 阶段目标不匹配。

## Consequences

- 断线、应用暂停或退出时，Host 保存最后一个稳定玩法状态。
- Client 断线后只能通过 Host 新开的会话继续，不能独立恢复。
- `PhotonAppSettings.asset` 仍是本机密钥配置，不进入存档、不进入版本控制。
- 后续若要做云存档或跨 Host 迁移，需要新的 ADR。
