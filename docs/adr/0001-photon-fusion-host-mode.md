# 0001: 联机网络栈采用 Photon Fusion 2（Host 模式）

## Status

accepted

## 决策

联机模式使用 **Photon Fusion 2**（官方 `.unitypackage` 导入安装，免费 AppId，免费档 20 CCU 足够 2 人），拓扑采用 **Host 模式**：Host 玩家的机器运行模拟并拥有权威，Client 仅上报输入。全游戏纯联机、无单机入口。

## 背景

项目（Tuanjie 1.6.12，Unity 2022.3.61 基础）原本是纯单机：`GameManager` 单例管理唯一玩家、`MiniGameManager` 冻结本地玩家后弹出全屏 UI 小游戏。需求要求两名玩家以不同角色联网共玩（探索 + 小游戏全联机），且经确认采用"主机权威"模型。`manifest.json` 中无任何网络包，需从零选型。

## Considered Options

- **Mirror**：Git URL 直接安装、自带 Kcp 传输、API 简单。落选原因：用户明确选择 Fusion；且 Fusion 的 [Networked] 状态同步 + 内置预测对 2D 平台移动手感更省心。
- **Unity Netcode (NGO + Transport)**：官方正统，但 Tuanjie 包注册表可用性不确定、样板代码多。
- **自研轻量网络层**：零依赖但工作量大，hackathon 时间窗风险高。

## Consequences

- **必须联网才能玩**：Fusion 所有流量（包括同局域网两台机器）都经 Photon Cloud，运行时需要互联网可达 photonengine.com；hackathon 现场断网则联机不可用。缓解：demo 前预先用本机双开验证，并备好 AppId。
- **AppId 是硬前置**：需要注册 Photon 账号、创建 Fusion 类型 App 获取 AppId（免费）。已获得 AppId 并由 `FusionNetworkBootstrap` 运行时兜底注入。
- **权威归属**：小游戏判定、胜负、重生规则全部在 Host 侧执行；Client 只做输入上报与状态展示。现有 `GameManager`/`MiniGameManager` 的本地权威逻辑需重构为"仅 Host 判定"。
- **单机调试路径消失**：日常调试需双开（Host + Client）或临时加单人测试入口，需在开发流程中安排。
- **Tuanjie 兼容性**：Fusion 面向 Vanilla Unity，与团结引擎存在轻微兼容风险；若 `.unitypackage` 导入或运行时报错，回退方案为 Mirror（LAN 可行，无需云）。

## 修正记录

- **2026-08-27**：安装方式勘误。原文写"UPM scoped registry 安装（需 npm 令牌）"——该说法已过时（那是 PUN2 时代做法）。Fusion 2 官方安装方式为：登录 Photon 账号从官方下载页获取 `.unitypackage`，经 `Assets > Import Package > Custom Package` 导入。**无需 scoped registry、无需 Dashboard 令牌**，仅需 AppId。详细步骤见 `docs/install-fusion.md`。
