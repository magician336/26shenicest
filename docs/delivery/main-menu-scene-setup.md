# 主菜单场景搭建交付文档

## 目标与范围

`MainMenu` 是本项目纯联机模式的入口场景。玩家可创建或加入一个**会话**；创建方为 Host，加入方为 Client。用于配对的短代码称为**房间码**。这些术语遵循 [CONTEXT.md](../../CONTEXT.md)。

本交付包含：

- 一键生成可预览的主菜单场景；
- 创建会话、加入会话与取消连接的菜单交互；
- 缺少 Fusion 或 `Game` 场景时的可诊断提示；
- 生成后的场景与 Build Settings 检查流程。

它不导入 Photon Fusion、不创建 `Game` 场景，也不替代双端联机冒烟测试。

## 前置条件

- 使用 Tuanjie Engine 1.6.12（Unity 2022.3.61 基础）打开 `DoNotForgetMe/New Tuanjie Project/`。
- 若要进行真实联机测试，先完成 [install-fusion.md](../install-fusion.md) 的 SDK 导入和 Fusion 实现代码移入步骤。
- `Game` 场景必须已创建并在 Build Settings 中启用。若尚未创建，先运行 `Tools > 3C Setup > Create Basic Scene`。

## 正式搭建流程

1. 在编辑器主菜单选择 `Tools > 3C Setup > Create Main Menu`。
2. 工具创建并保存 `Assets/_Project/Scenes/MainMenu.unity`。
3. 打开 `File > Build Settings`，确认场景顺序为：

   1. `MainMenu`（已启用）
   2. `Game`（已启用）

   一键工具只负责将 `MainMenu` 置于首位；它不会隐式创建 `Game`。
4. 打开 `MainMenu` 场景并点击 Play，完成下方验收。

重复执行该菜单命令会重建 `MainMenu` 场景。需要保留的手工美术或布局调整，应在执行前提交或备份。

## 生成结果

根对象包含：

- `Canvas`：背景、标题、房间码输入、房间码展示、状态文本与三枚按钮；
- `EventSystem`：使用 `StandaloneInputModule`；
- `MainMenuController`：已自动关联所有 UI 引用。

按钮默认显示“创建会话”和“加入会话”。仅在连接过程中显示“取消连接”。生成器使用 `CanvasScaler` 的 `1920 x 1080` 参考分辨率。

## 行为验收

### UI 骨架预览（未导入 Fusion 时可执行）

- [ ] 场景能无编译错误打开并运行。
- [ ] 能看到标题、房间码输入框、“创建会话”和“加入会话”。
- [ ] 输入少于 4 位、超过 6 位，或包含 `0`、`O`、`1`、`I`、`L` 的房间码时，显示校验说明且不发起连接。
- [ ] 点击创建或加入时，显示网络层未安装提示；菜单恢复可再次操作状态。

此阶段仅证明 UI 和本地校验可用，不表示联机可用。

### 联机前置条件

- [ ] Fusion 已按 [install-fusion.md](../install-fusion.md) 导入，且 `FusionSessionService` 和 `FusionNetworkBootstrap` 已放入运行时代码目录。
- [ ] Build Settings 中存在且启用了 `MainMenu` 与 `Game`，并且 `MainMenu` 在前。
- [ ] Console 没有编译错误。

若缺少 `Game`，创建或加入会话应给出指向 `Tools > 3C Setup > Create Basic Scene` 的错误，而不是尝试进入一个不存在的场景。

### 双端联机冒烟

- [ ] 实例 A 点击“创建会话”，显示 5 位房间码，并显示“取消连接”。
- [ ] 实例 B 输入该房间码并点击“加入会话”。
- [ ] 连接成功后，双方进入 `Game` 场景。
- [ ] 在连接中点击“取消连接”，留在主菜单，隐藏取消按钮，清空 Host 房间码，并可重新创建或加入会话。
- [ ] 任意一端在游戏中断开，另一端返回主菜单并收到会话结束提示。
- [ ] 连接失败后，Host 展示的房间码被清空；加入方输入框保留原输入，便于更正或重试。

## 实现边界

`MainMenuController` 只依赖 `INetworkSessionService`，不直接引用 Fusion，因此未导入 SDK 时仍可编译和预览。Fusion 导入前默认使用 `NotInstalledSessionService`；导入后由启动器注册真实实现。

房间码是会话名称，不是游戏场景中的“房间”。因此界面与文档使用“会话”描述联机配对，保留“房间码”描述短代码。
