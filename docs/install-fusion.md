# Photon Fusion 2 安装指南（Tuanjie 1.6.12 / Unity 2022.3.61 基础）

> 适用版本：**Fusion SDK 2.1.2 Stable Build 2279**（2026-08-13 发布，当前最新稳定版）
> 官方要求 Unity 2021.3.45 / 2022.3.45 / 6.0.x / 6.3.x —— 本项目 Tuanjie 基于 2022.3.61，版本号满足。
>
> **重要勘误**：Fusion 2 的安装**不是** scoped registry + npm 令牌（那是 PUN2 时代的做法）。
> 官方方式是登录 Photon 账号下载 `.unitypackage` 后导入。**只需要 AppId，不需要 Dashboard 令牌。**

## 已具备的前置条件

| 项 | 状态 |
|---|---|
| Fusion AppId | 需在本机 Fusion Hub / PhotonAppSettings 中填写；该资产已被 Git 忽略，不能提交 |
| Asset Serialization = Force Text | ✅ 已满足（`EditorSettings.asset` 中 `m_SerializationMode: 2`） |
| 主菜单与 Fusion 接入代码 | ✅ 已就位；直接依赖 Fusion 的脚本由 `FUSION_PRESENT` 条件编译保护 |

## 安装步骤

### 第 1 步：下载 SDK（需要浏览器登录 Photon 账号）

下载服务器要求登录态，命令行直链会返回 403，所以这一步要手动做：

1. 浏览器打开：<https://downloads.photonengine.com/download/latest/photon-fusion-sdk-2-1>
2. 登录 Photon 账号（就是创建 AppId 用的那个）
3. 下载得到的 `.unitypackage` 保存到：`D:\code_warehouse\Projects\hackathon\DoNotForgetMe\FusionSDK\`

> 直链（登录后浏览器可用）：
> `https://downloads.photonengine.com/download/fusion/photon-fusion-2.1.2-stable-2279.unitypackage`
> 不要把 `.unitypackage` 放进项目的 `Assets/` 目录。

### 第 2 步：调整项目设置

在 Tuanjie 编辑器中：

1. `Edit > Project Settings > Player > Other Settings > Api Compatibility Level`
   改为 **.NET Standard 2.1**（Fusion 2 的异步网络库依赖此等级，不切换可能产生难排查的编译错误）。
2. `Edit > Project Settings > Player > Other Settings > Scripting Define Symbols`
   增加 `FUSION_PRESENT`。

### 第 3 步：导入 SDK

1. `Assets > Import Package > Custom Package…`
2. 选择第 1 步下载的 `photon-fusion-2.1.2-stable-2279.unitypackage`
3. 导入窗口中点 **Import**（全部默认勾选）
4. 导入后若弹出 **Fusion Hub** 欢迎窗口：填写你自己的 Fusion AppId。
   `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` 是本机配置，已被 `.gitignore` 忽略，不要提交。
5. 等待编译完成。若报 `Mono.Cecil` 缺失（Tuanjie 注册表缺少该包时）：
   `Window > Package Manager > + > Add package from git URL`，输入
   `com.unity.nuget.mono-cecil@1.10.2`

### 第 4 步：确认 Fusion 实现已启用

以下文件已在运行时代码目录中，但只有定义 `FUSION_PRESENT` 后才会参与编译：

- `Assets/_Project/Scripts/Network/Fusion/FusionSessionService.cs` —— `INetworkSessionService` 的 Fusion Host 模式实现；
- `Assets/_Project/Scripts/Network/Fusion/FusionNetworkBootstrap.cs` —— 启动时注册会话服务；
- `Assets/_Project/Scripts/Network/Fusion/FusionGameplayBridge.cs` —— Host 权威玩法意图与状态同步桥。

### 第 5 步：生成场景

1. 菜单 `Tools > 3C Setup/Create Main Menu` —— 生成主菜单场景（入口，自动排到 Build Settings 第 1 位）
2. 若 `Assets/_Project/Scenes/Game.unity` 尚不存在：菜单 `Tools/3C Setup/Create Basic Scene`
3. 确认 `File > Build Settings` 场景列表顺序：**MainMenu 在前，Game 在后**

### 第 6 步：验证清单

- [ ] 编译无错误（Console 干净）
- [ ] 打开 MainMenu 场景，Play：能看到标题、"输入房间码"输入框、创建/加入按钮
- [ ] 点「创建房间」：出现 5 位房间码大字 + 状态变为"等待对方加入"
- [ ] Console 出现 `[Net] FusionSessionService registered`
- [ ] （联机冒烟）本机双开：一个实例创建房间，另一个输入房间码加入，双端进入 Game 场景；
      任一端停止 Play，另一端回到主菜单并提示"对方已断开"

## 疑难排查

| 症状 | 处理 |
|---|---|
| 连接慢或超时 | PhotonAppSettings 的 `Fixed Region` 设为 `asia`（新加坡，国内延迟通常最优）；大陆直连不稳时走代理 |
| `.unitypackage` 导入报版本不符 | 确认 Tuanjie 版本 ≥ 2022.3.45 基线；忽略纯版本号警告，仅处理编译错误 |
| Tuanjie 专属 API 冲突 | 见 ADR 0001 回退方案（Mirror，LAN 直连无需云） |
| hackathon 现场断网 | Photon Cloud 不可用即无法联机（ADR 0001 已记录该风险）；提前双开验证 |

## 下一步（安装完成后）

主菜单骨架只是入口。后续改造顺序（按 grill 敲定的架构）：

1. `GameManager` 重构为网络生成（Host 生成角色A，Client 侧观战态）
2. 探索阶段输入门控（Client 输入无效，仅观战视角）
3. `MiniGameManager` 改网络权威 + 去失败态/去倒计时（ADR 0002）
4. 第一个非对称小游戏 demo
