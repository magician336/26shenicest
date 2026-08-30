# 本地调试网络层（无 Fusion 单进程模式）

> 适用场景：hackathon 开发期间，尚未导入 Photon Fusion SDK，需要在单个 Unity 编辑器实例内跑通
> Game 场景的完整小游戏流程（探索 → 触发小游戏 → 母亲选食材 → 女儿调味 → 完成）。

## 原理

项目联机架构通过 `INetworkSessionService` 接口隔离 Fusion 依赖。当 Fusion 未导入时，
`LocalNetworkBootstrap`（`#if !FUSION_PRESENT`）自动将桩服务 `NotInstalledSessionService`
替换为 `LocalDebugService`——一个无网络通信的单进程调试服务。

`SessionGameplayCoordinator` 新增 `debugSingleProcess` 标志（由 `Scene3CSetup` 在生成场景时
自动设为 `true`）。该标志开启后，`HandleHostIntent` 绕过 Host 权限检查和角色权限校验，
允许单进程内切换角色、本地环回意图。

```
┌─ Game 场景 ───────────────────────────────────────────┐
│                                                         │
│  LocalDebugService (Role=Host/Client, 可切换)           │
│       ↑                                                 │
│       │ Role 查询                                       │
│  SessionGameplayCoordinator                             │
│    ├─ Request(intent)                                   │
│    │   ├─ Role=Host  → HandleHostIntent(Host) 直通     │
│    │   └─ Role=Client → transport.SendIntent → 环回     │
│    │                          ↓                         │
│    │              LocalGameplayBridge                   │
│    │              └─ HandleHostIntent(Client)           │
│    │                                                    │
│    └─ PublishState → StateChanged → MiniGameManager    │
│                                         ↓               │
│                                  CookingMiniGame.Render │
│                                  (按 Role 显示私有视图)  │
│                                                         │
│  Tab 键 → LocalDebugService.SetRole() 切换角色          │
│         → ApplyAuthoritativeState() 触发重渲染           │
└─────────────────────────────────────────────────────────┘
```

## 新增文件

| 文件 | 说明 |
|---|---|
| `Assets/_Project/Scripts/Network/Local/LocalDebugService.cs` | 单进程调试会话服务，Role 可运行时切换 |
| `Assets/_Project/Scripts/Network/Local/LocalGameplayBridge.cs` | IGameplayTransport 实现，Tab 切换角色，意图本地环回 |
| `Assets/_Project/Scripts/Network/Local/LocalNetworkBootstrap.cs` | `#if !FUSION_PRESENT` 运行时注册 LocalDebugService |

## 修改文件

| 文件 | 改动 |
|---|---|
| `SessionGameplayCoordinator.cs` | 新增 `debugSingleProcess` 字段；`HandleHostIntent` 在调试模式下绕过权限检查 |
| `Editor/Scene3CSetup.cs` | `#else` 分支：设 `debugSingleProcess=true` + 挂载 `LocalGameplayBridge` |

## 使用步骤

### 1. 生成场景

在 Tuanjie 编辑器中：

```
Tools > 3C Setup > Create Main Menu   （生成主菜单场景，可选）
Tools > 3C Setup > Create Basic Scene  （生成 Game 场景，必需）
```

### 2. 直接调试 Game 场景

1. 打开 `Assets/_Project/Scenes/Game.unity`
2. 按 **Play**

### 3. 操作流程

| 阶段 | 角色 | 操作 |
|---|---|---|
| 探索 | Host（女儿） | A/D 或 ←/→ 移动；走到黄色方块按 **F** 触发小游戏 |
| 小游戏-选食材 | **按 Tab** → Client（母亲） | 点击「番茄」「鸡蛋」按钮选择食材 |
| 小游戏-入锅 | Client（母亲） | 将食材拖入「锅」区域 |
| 小游戏-调味 | **按 Tab** → Host（女儿） | 将「糖」拖入「番茄炒蛋」区域 |
| 完成 | 任意 | 显示"你们一起完成了这道菜" |
| 回到探索 | 自动切回 Host | 可再次触发小游戏 |

### 控制键

| 按键 | 功能 |
|---|---|
| A/D 或 ←/→ | 左右移动 |
| F | 交互（触发小游戏） |
| **Tab** | 切换 Host（女儿）/ Client（母亲）角色 |

### 4. Console 日志

切换角色时 Console 会输出：
```
[Debug] 角色切换 → Host（女儿端）
[Debug] 角色切换 → Client（母亲端）
[Debug] 回到探索阶段，自动切回 Host（女儿）。
```

## 限制

- **无网络通信**：单进程内模拟，两个角色在同一实例内切换，不是真正的双进程联机
- **无玩家位置同步**：Client 角色下玩家不会移动（探索阶段切换到 Client 仅用于小游戏阶段）
- **无 Photon Cloud**：不连接任何外部服务器
- **仅调试用**：`debugSingleProcess` 标志在 `Scene3CSetup` 中自动设置，正式构建不应启用

## 切回真实 Photon Fusion

当需要真正的联机测试时：

1. 按 [install-fusion.md](install-fusion.md) 导入 Fusion SDK
2. 在 Player Settings 添加 `FUSION_PRESENT` 宏
3. 重新执行 `Tools > 3C Setup > Create Basic Scene`
   - `Scene3CSetup` 的 `#if FUSION_PRESENT` 分支生效，挂载 `FusionNetworkObject` + `FusionGameplayBridge`
   - `#else` 分支（`LocalGameplayBridge`）不编译
   - `debugSingleProcess` 不会被设为 `true`
4. `LocalNetworkBootstrap` 不编译（`#if !FUSION_PRESENT`），`FusionNetworkBootstrap` 接管
5. 主菜单创建/加入会话走真实 Fusion 流程

无需删除本地调试文件——条件编译自动隔离。
