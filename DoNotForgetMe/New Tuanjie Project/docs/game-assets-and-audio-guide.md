# 游戏全流程资产清单 · 交互逻辑 · 音效动效指南

> 生成时间：2026-08-28 · 基于 `5d306e6` (origin/main) 全量代码通读

---

## 目录

1. [整体流程](#1-整体流程)
2. [MainMenu 主菜单](#2-mainmenu-主菜单)
3. [Intro 开场过场](#3-intro-开场过场)
4. [LivingRoom 客厅（首次进入）](#4-livingroom-客厅首次进入)
5. [Kitchen 厨房·做饭小游戏](#5-kitchen-厨房做饭小游戏)
6. [Courtyard 庭院·八卦小游戏](#6-courtyard-庭院八卦小游戏)
7. [LivingRoom 客厅·重访·相册小游戏](#7-livingroom-客厅重访相册小游戏)
8. [中断覆盖层（通用）](#8-中断覆盖层通用)
9. [探索阶段常驻 UI](#9-探索阶段常驻-ui)
10. [对白系统](#10-对白系统)
11. [ScriptableObject 配置资产清单](#11-scriptableobject-配置资产清单)
12. [现有美术资产状况](#12-现有美术资产状况)
13. [音效接口设计](#13-音效接口设计)
14. [音效需求清单](#14-音效需求清单)
15. [动效需求清单](#15-动效需求清单)

---

## 1. 整体流程

```
MainMenu → Intro → LivingRoom → Kitchen → Courtyard → LivingRoom(相册) → GameEnded
```

| 阶段 | 场景 | 场景文件 | 角色操作 |
|---|---|---|---|
| 标题/联机 | MainMenu | `Assets/_Project/Scenes/MainMenu.unity` | Host 创建房间码 / Client 输入房间码 |
| 开场过场 | Intro | `Assets/_Project/Scenes/Intro.unity` | 自动播放，无操作 |
| 探索① | LivingRoom | `Assets/_Project/Scenes/LivingRoom.unity` | Host 控制角色，首次自动播放书桌觉醒序列 |
| 小游戏① | Kitchen | `Assets/_Project/Scenes/Kitchen.unity` | 做饭小游戏（番茄炒蛋→黄瓜凉拌） |
| 小游戏② | Courtyard | `Assets/_Project/Scenes/Courtyard.unity` | 八卦小游戏（听故事→配对→认人） |
| 小游戏③ | LivingRoom 重访 | LivingRoom（同场景文件） | 相册小游戏（贴纸→姓名tag→终局） |
| 终局 | — | — | 黑屏 GameEnded |

**核心类：** `SessionGameplayCoordinator`（Host 权威状态机）、`MiniGameManager`（本地 UI 渲染）、`SceneLoader`（统一场景加载）

---

## 2. MainMenu 主菜单

### 场景生成

- 菜单：`Tools > 3C Setup > Create Main Menu`
- 脚本：`Assets/_Project/Scripts/Editor/MainMenuSetup.cs`
- 输出：`Assets/_Project/Scenes/MainMenu.unity`（Build Settings 首位）

### UI 元素清单

| UI元素 | 代码位置 | 规格 | 颜色 | 功能/意图 |
|---|---|---|---|---|
| Background (Image) | `MainMenuSetup.CreateBackground` | 全屏铺满 | `(0.12, 0.12, 0.17)` | 暗色背景 |
| Title (Text) | `MainMenuSetup.CreateText` | 900×100, fontSize 64 | `(0.95, 0.95, 0.98)` | 标题 "DO NOT FORGET ME" |
| RoomCodeDisplay (Text) | `MainMenuSetup.CreateText` | 900×90, fontSize 56 | `(0.98, 0.85, 0.3)` 金色 | Host 创建后大字展示房间码 |
| RoomCodeInput (InputField) | `MainMenuSetup.CreateRoomCodeInput` | 440×70 | `(0.18, 0.18, 0.24)` | Client 输入房间码, Alphanumeric, 6字符上限 |
| CreateButton | `MainMenuSetup.CreateButton` | 320×70 | `(0.22, 0.55, 0.9)` 蓝 | "创建会话" → Host 模式 |
| JoinButton | 同上 | 320×70 | `(0.2, 0.65, 0.45)` 绿 | "加入会话" → Client 模式 |
| CancelButton | 同上 | 240×56 | `(0.55, 0.3, 0.3)` 红 | "取消连接" → 仅连接中显示 |
| ContinueButton | 同上 | 320×70 | `(0.55, 0.42, 0.25)` 棕 | "继续游戏" → 仅存档存在时显示 |
| StatusText | `MainMenuSetup.CreateText` | 1200×80, fontSize 26 | `(0.75, 0.75, 0.8)` | 状态提示文字 |

### 交互逻辑

- Host 点"创建会话"→ 生成5位房间码 → 大字展示 → 等待 Client 加入
- 若已有存档，再次点击"创建会话"需二次确认覆盖
- Client 在输入框填房间码 → 点"加入会话" → 校验4~6位（排除0/O/1/I/L）
- 双端连接成功 → 自动进入 Intro 场景
- 断线即回主菜单

### 字体

全部 UI 使用 `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`

---

## 3. Intro 开场过场

### 场景生成

- 菜单：`Tools > 3C Setup > Create Intro Scene`
- 脚本：`Assets/_Project/Scripts/Cutscene/IntroCutsceneController.cs`
- 输出：`Assets/_Project/Scenes/Intro.unity`

### UI 元素清单

| 元素 | 代码位置 | 规格 | 颜色 | 功能 |
|---|---|---|---|---|
| BlackOverlay (Image) | `IntroCutsceneController.CreateCanvas` | 全屏 | 纯黑 | 背景黑屏 |
| MonologueText (Text) | 同上 | 1400×300, fontSize 42 | `(0.9, 0.85, 0.7)` 暖色 | 逐句内心独白渐入渐出 |
| AIGCVideoPlayer (VideoPlayer) | `IntroCutsceneController.PlayVideo` | 全屏 CameraNearPlane | — | AIGC视频(Inspector 挂接 `aigcVideoClip`) |
| ReversalFlash (Image) | `IntroCutsceneController.PlayReversalFlash` | 全屏 | `(0.9, 0.85, 0.7)` 闪白 | 时光回溯闪白效果 |

### 流程

1. 黑屏逐句渐入渐出 7 句独白（每句: fadeIn 1.5s → hold 3s → fadeOut 1s → gap 0.5s）
2. AIGC 视频播放（最多15s，Inspector 挂接 `aigcVideoClip`）
3. 闪白时光回溯（0.4×2s + 0.6×2s，共2s）
4. 淡入黑屏 → 加载 LivingRoom

### 独白文本（硬编码于 `IntroCutsceneController`）

```
丧事刚过。屋里人来人往，电话不停。
有人喊二姐，有人喊梅姨，有人喊嫂子，有人喊刘师傅。
母亲有时应，有时没反应。
她攥着一张残缺、模糊的旧全家福。
「小岩……」
「小岩去哪儿了……」
没关系。我们一起找。
```

### Inspector 挂接点

| 字段 | 类型 | 说明 | 是否已接入 |
|---|---|---|---|
| `aigcVideoClip` | VideoClip | AIGC 开场视频，留空则跳过 | ❌ 待视频文件 |

---

## 4. LivingRoom 客厅（首次进入）

### 场景生成

- 菜单：`Tools > 3C Setup > Create Living Room Scene`
- 脚本：`Assets/_Project/Scripts/Editor/LivingRoomSceneSetup.cs`
- 输出：`Assets/_Project/Scenes/LivingRoom.unity`

### 场景对象

| 对象 | 创建方式 | 位置 | 规格 | 颜色 | 功能 |
|---|---|---|---|---|---|
| Ground | `SceneSetupBase.CreateGround` | (0, -3) | 20×1 | `(0.4, 0.4, 0.45)` 灰 | 地面平台 |
| LeftWall / RightWall | `SceneSetupBase.CreateAirWall` | x=±11 | 隐形 BoxCollider2D | — | 空气墙边界 |
| Player | `SceneSetupBase.CreatePlayer` | (0, -1.5) | 0.8×0.8 collider | `(0.3, 0.7, 1)` 蓝 | 玩家角色 |
| Main Camera | `SceneSetupBase.CreateCamera` | (0, 0, -10) | 正交 size=5, constrainX[-5,5] | `(0.15, 0.15, 0.2)` | 房间式相机 |
| GameManager | `SceneSetupBase.CreateGameManager` | — | DontDestroyOnLoad | — | 游戏管理器（首次创建） |
| SessionGameplayCoordinator | `SceneSetupBase.CreateCoordinator` | — | DontDestroyOnLoad | — | 流程状态机（首次创建） |
| MiniGameManager | `SceneSetupBase.CreateMiniGameManager` | — | DontDestroyOnLoad | — | 小游戏UI管理器（首次创建） |
| DeskViewController | `SceneSetupBase.CreateDeskViewController` | (2, -1.5) | 1.2×1 collider | `(0.45, 0.32, 0.2)` 棕 | 书桌 F交互 |
| DoorToKitchen | `SceneSetupBase.CreateSceneTransitionDoor` | (8, -1.5) | 1.2×2 collider | `(0.4, 0.5, 0.35)` 绿 | F→Kitchen |
| PlayerSpawn | `SceneSetupBase.CreatePlayerSpawn` | (-3, -1.5) | — | — | 出生点 |

### 书桌觉醒序列（DeskViewController）

**首次进入自动触发**：黑屏 → 2句觉醒字幕渐入渐出 → 淡入书桌画面 → 按 X 退出

觉醒文本（硬编码）：
```
这是哪里……这双手，不是我的。
他们叫我洪梅……难道，这是妈妈小时候住过的地方？
```

**后续 F 交互**：检查3张照片是否集齐 → 集齐则启动相册小游戏 → 未集齐则再次展示书桌

### 书桌画面 UI 元素（代码生成布局，已接入书桌背景图）

| 元素 | 位置 | 规格 | 颜色 | 意图 |
|---|---|---|---|---|
| DeskSurface (全屏背景) | 铺满 | 全屏 | `(0.30, 0.21, 0.13)` 不透明暖棕 | 桌面 |
| WoodGrain ×7 | 横线条 | 铺宽×2px | `(0.22, 0.15, 0.08)` alpha递增 | 木纹 |
| WindowLight | 右上区域 | 30%~100% | `(0.95, 0.8, 0.45)` alpha 0.08 | 窗光投射 |
| Photo (模糊全家福) | (-550, 60) | 220×160 | `(0.7, 0.68, 0.6)` alpha 0.7 | 桌上模糊照片 |
| ExerciseBook (作业本) | (-180, 100) | 300×220 | `(0.78, 0.74, 0.65)` | 翻开的作业本 |
| SongBook (歌本) | (180, 80) | 200×150 | `(0.55, 0.45, 0.35)` | 歌本 |
| HairClip (黄发卡) | (180, 200) | 70×25 | `(0.92, 0.78, 0.12)` | 小物件·母亲身份线索 |
| PencilCase (铁皮文具盒) | (450, 40) | 180×55 | `(0.48, 0.42, 0.38)` | 桌面物件 |
| Scrapbook (合上的剪贴簿) | (-450, -180) | 180×120 | `(0.35, 0.26, 0.18)` | 暗色不显眼 |
| LeftHand / RightHand | 底部左右各半 | 各40%×18% | `(0.52, 0.4, 0.32)` alpha 0.5 | 第一人称双手 |
| Hint (Text) | (0, 460) | 600×50, fontSize 26 | alpha 0.6 暖色 | "按 X 离开书桌" |
| CloseButton | 右上角 | 56×56 | `(0.5, 0.25, 0.25)` 红 | X按钮关闭 |

### Inspector 挂接点（DeskViewController）

| 字段 | 类型 | 说明 | 是否已接入 |
|---|---|---|---|
| `albumConfigId` | string | 相册小游戏配置 ID（默认 "album_family_portrait"） | ✅ 硬编码默认值 |
| `requiredPhotoIds` | string[] | 启动相册小游戏所需的照片 ID 列表 | ✅ 硬编码默认值 |
| `deskBackgroundSprite` | Sprite | 书桌背景图 | ✅ livingroom_bgdesk.png |

---

## 5. Kitchen 厨房·做饭小游戏

### 场景生成

- 菜单：`Tools > 3C Setup > Create Kitchen Scene`
- 脚本：`Assets/_Project/Scripts/Editor/KitchenSceneSetup.cs`
- 输出：`Assets/_Project/Scenes/Kitchen.unity`

### 场景对象

| 对象 | 位置 | 规格 | 颜色 | 功能 |
|---|---|---|---|---|
| Ground / AirWalls | 同客厅 | 同客厅 | 同客厅 | 同客厅 |
| Player | (0, -1.5) | 同客厅 | 同客厅 | 同客厅 |
| Camera | constrainX[-5,5] | 同客厅 | 同客厅 | 同客厅 |
| MiniGameTrigger | (3, -1.5) | 1×1 collider, 黄色 `(0.9, 0.75, 0.1)` | miniGameId="tomato_egg" |
| DoorToLivingRoom | (-8, -1.5) | 1.2×2 collider, 绿色 | F→LivingRoom |
| DoorToCourtyard | (8, -1.5) | 1.2×2 collider, 绿色 | F→Courtyard |
| PlayerSpawn | (-5, -1.5) | — | — | 出生点 |

### 做饭小游戏 UI（CookingMiniGame，代码生成布局，已接入食材/调料/背景 Sprite）

**Canvas 配置：** sortingOrder 100, 1920×1080 参考分辨率, 底色 `(0.12, 0.1, 0.08)` alpha 0.98

#### 母亲端（Client）视图

| 元素 | 位置 | 规格 | 颜色 | 功能 |
|---|---|---|---|---|
| Role (Text) | (0, 410) | 1500×100, fontSize 42 | 白 | "母亲端 · {MotherTaskText}" |
| Instruction (Text) | (0, 300) | 1500×100, fontSize 30 | `(0.85, 0.8, 0.7)` 暖灰 | "把需要的食材拖进{锅}里" |
| Container/DropZone (锅) | (0, -40) | 420×280 | 深棕 `(0.35,0.25,0.16)` | 拖拽目标区域 |
| Draggable 食材 ×N | (-400+slot×300, -360) | 220×120 | 棕色 `(0.7,0.58,0.4)` | 可拖拽：番茄/鸡蛋(正确)+黄瓜/排骨(干扰) |
| Help Button | (730, -400) | 250×130 | `(0.38,0.31,0.23)` 棕 | "帮我看看" → RequestHint |
| Hint (Text) | (0, 70) | 1500×100, fontSize 28 | `(0.8,0.6,0.3)` 暖色 | 分层提示文本 |

#### 女儿端（Host）视图

| 元素 | 位置 | 规格 | 颜色 | 功能 |
|---|---|---|---|---|
| Role (Text) | (0, 410) | 同上 | 白 | "女儿端 · {DaughterTaskText}" |
| Instruction (Text) | (0, 260) | 同上 | 暖灰 | "拖入正确的调料" |
| Dish/DropZone | (0, -40) | 420×280 | 深棕 | 调料拖入目标 |
| Draggable 调料 ×N | (-220+i×spacing, -360) | 220×120 | 棕色 | 糖/盐(可选) |
| Interrupt Button | (730, -400) | 250×130 | 棕色 | "暂时离开" → InterruptMiniGame |
| ShowHint Button | (0, -460) | 250×130 | 棕色 | "发送下一层提示" → ShowHint（仅 hintRequested 时显示） |
| Hint (Text) | (0, 70) | 同上 | 暖色 | 提示文本 |
| RewardPhoto (Image) | (0, 40) | 300×220 | `(0.85,0.82,0.7)` 米色 | 完成后照片奖励卡片 |
| PhotoLabel (Text) | (0, -8) | 340×40, fontSize 24 | `(0.9,0.85,0.65)` | "获得照片" |
| CollectPhotoBtn (Button) | (0, -200) | 280×80 | `(0.85,0.65,0.2)` 金色 | "收集照片" → FinishMiniGame |

### 交互流程

1. Host（女儿）走到 MiniGameTrigger 按 F → 启动小游戏
2. Client（母亲）拖拽正确食材到锅 → 全部放入后切换到女儿端调味
3. 女儿拖入正确调料 → `completed=true`
4. 女儿端显示照片奖励卡片 → 点"收集照片" → `FinishMiniGame`
5. 若有 `nextRecipeId` → 自动启动下一道菜（黄瓜凉拌）
6. 做饭链完成 → 玩家走到门按 F → 场景转场到 Courtyard

### RecipeConfig 字段说明

| 字段 | 类型 | 说明 | 示例值（番茄炒蛋） |
|---|---|---|---|
| `recipeId` | string | 菜谱唯一ID | "tomato_egg" |
| `displayName` | string | 显示名称 | "番茄炒蛋" |
| `motherTaskText` | string | 母亲端任务提示 | "请做番茄炒蛋" |
| `daughterTaskText` | string | 女儿端任务提示 | "查看菜谱改痕，为菜调味" |
| `containerDisplayName` | string | 容器显示名 | "锅" |
| `requiredIngredients` | string[] | 正确食材ID | ["tomato", "egg"] |
| `distractorIngredients` | string[] | 干扰食材ID | ["cucumber", "ribs"] |
| `seasoningOptions` | string[] | 可选调料ID | ["sugar", "salt"] |
| `correctSeasoning` | string | 正确调料ID | "sugar" |
| `recipeNote` | string | 菜谱改痕（母亲端完成后显示） | "洪强爱吃甜的，放点糖。" |
| `hintTexts` | string[] | 分层提示文本 | 3条递进提示 |
| `rewardIds` | string[] | 奖励ID（含 "photo" 前缀的为照片） | ["photo_hongqiang"] |
| `nextRecipeId` | string | 完成后自动启动的下一道菜ID | "cucumber_salad" |
| `nextDialogueId` | string | 完成后自动播放的对白ID | — |

### 配置资产

| 资产 | 路径 | 奖励ID | 链式 | 美术资产已接入 |
|---|---|---|---|---|
| 番茄炒蛋 | `Assets/_Project/Settings/TomatoEggRecipe.asset` | photo_hongqiang | → cucumber_salad | ✅ 食材8种+锅+背景+菜图+奖励照片 |
| 黄瓜凉拌 | `Assets/_Project/Settings/CucumberSaladRecipe.asset` | photo_hongfang | → Courtyard（场景转场） | ✅ 食材8种+锅+背景+菜图+奖励照片 |

### 物品ID → 显示名映射（硬编码于 CookingMiniGame.DisplayName）

| ID | 显示名 | Sprite 文件 | 已接入 |
|---|---|---|---|
| tomato | 番茄 | item_tomato.png | ✅ |
| egg | 鸡蛋 | item_egg.png | ✅ |
| cucumber | 黄瓜 | item_cucumber.png | ✅ |
| ribs | 排骨 | item_ribs.png | ✅ |
| sugar | 糖 | suger.png | ✅ |
| salt | 盐 | salt.png | ✅ |
| vinegar | 醋 | vinegar.png | ✅ |
| chili | 辣椒 | chili.png | ✅ |

---

## 6. Courtyard 庭院·八卦小游戏

### 场景生成

- 菜单：`Tools > 3C Setup > Create Courtyard Scene`
- 脚本：`Assets/_Project/Scripts/Editor/CourtyardSceneSetup.cs`
- 输出：`Assets/_Project/Scenes/Courtyard.unity`

### 场景对象

| 对象 | 位置 | 规格 | 颜色 | 功能 |
|---|---|---|---|---|
| DoorListeningTrigger | (3, -1.5) | 1.2×2 collider | `(0.5, 0.3, 0.2)` 棕 | 门偷听+八卦触发器 |
| ↳ DialogueAudio (AudioSource) | 子物体 | loop=true, playOnAwake=false | — | 距离渐变音频 |
| DoorToLivingRoom | (-8, -1.5) | 1.2×2 collider | 绿色 | F→LivingRoom |
| PlayerSpawn | (-5, -1.5) | — | — | 出生点 |

### DoorListeningTrigger 距离音频机制

- 玩家靠近门 → `AudioSource.volume` 随距离递增（0~0.6）
- `AudioLowPassFilter.cutoffFrequency` 随距离从 200Hz 升到 22000Hz
- 按 F → 音量最大 + 低通全开 → 3秒过渡文字 → 启动八卦小游戏

### 八卦小游戏 UI（BaguaMiniGameView，代码生成布局，已接入立绘/物件/老照片/桌面背景 Sprite）

#### Client（母亲）视图

| 元素 | 位置 | 规格 | 颜色 | 功能 |
|---|---|---|---|---|
| TaskBanner (Text) | (0, 430) | 1400×60, fontSize 28 | `(0.9, 0.85, 0.65)` 暖色 | "点击人物听八卦，把听到的物品拖到对应的人物吧！" |
| DesktopTray (Image) | (0, 60) | 1700×380 | `(0.28, 0.2, 0.12)` 深棕 | 木质桌面托盘 |
| DesktopItem ×N (Draggable) | 按 `ItemPlacement.anchoredPosition` | 120×120 | `(0.6, 0.48, 0.32)` 棕 | 桌面散落物件(正确+干扰) |
| CharacterCard ×3 (Image) | (-560+i×560, -320) | 520×200 | 未配对棕/已配对绿 | 人物卡片 |
| ↳ Portrait (Image) | 卡片左侧 | 130×160 | 灰色或 config.sprite | 立绘区域 |
| ↳ AudioBtn (Button) | 卡片左下 | 60×60 | 未听红/已听绿 | "听" → 播放 storyAudio 或字幕 |
| ↳ DropSlot (Image) | 卡片右侧 | 120×120 | 半透明白/黄 | 拖拽放置槽(听过才激活) |
| SubtitleBar (Image+Text) | 底部(0, 20) | 1600×100 | `(0.05, 0.04, 0.03)` alpha 0.92 | 无音频时显示字幕条 |

#### Host（女儿）视图

| 元素 | 位置 | 规格 | 颜色 | 功能 |
|---|---|---|---|---|
| Role (Text) | (0, 460) | 1500×100, fontSize 42 | 白 | "女儿端 · 八卦旧事" |
| Waiting (Text) | (0, 0) | 1500×100, fontSize 34 | 暖灰 | "等待母亲听故事并配对物品…" |
| OldPhoto (Image) | (0, 60) | 800×500 | config.sprite 或灰 | 老照片 |
| PhotoDropZone ×N | 按 `PhotoZoneConfig.anchoredPosition` | `zone.size` | 半透明白/绿 | 照片投放区 |
| NameTag ×N (Draggable) | (-350+i×350, -380) | 200×80 | `(0.7, 0.58, 0.4)` 棕 | 可拖拽姓名标签 |
| Interrupt Button | (730, -460) | 250×110 | 棕色 | "暂时离开" |

### 交互流程

1. Host 靠近门 → 距离音频渐变（越近越清晰）
2. Host 按 F → 3秒过渡 → 启动八卦小游戏
3. **第一阶段（Client）**：母亲点"听"按钮 → 播放 `storyAudio`（或字幕条，时长 = len/5+1s）→ 播完标记"已听" → 拖拽桌面物品到对应人物卡 → 正确则吸附+缩放动画 → 错误则抖动弹回
4. **第二阶段（Host）**：女儿拖拽姓名标签到老照片对应区域 → 正确则固定 → 错误则弹回
5. 完成后显示照片奖励 → 收集 → `FinishMiniGame` → 转场回 LivingRoom

### BaguaStoryConfig 字段说明

| 字段 | 类型 | 说明 |
|---|---|---|
| `miniGameId` | string | 小游戏唯一ID |
| `entries` | BaguaStoryEntry[] | 人物条目（characterId, displayName, portrait, storyAudio, subtitle, age, title） |
| `itemPlacements` | ItemPlacement[] | 桌面物件（itemId, displayName, sprite, anchoredPosition, isCorrect, characterId） |
| `oldFamilyPhoto` | Sprite | 老照片图片 |
| `photoZones` | PhotoZoneConfig[] | 照片投放区（zoneId, correctCharacterId, anchoredPosition, size） |
| `rewardIds` | string[] | 奖励ID |
| `nextDialogueId` | string | 完成后对白ID |

### 配置资产

| 资产 | 路径 | 美术资产已接入 |
|---|---|---|
| BaguaStoryConfig | `Assets/_Project/Settings/BaguaStoryConfig.asset` | ✅ 人物立绘3/3 + 物件图标8/8 + 老照片 + 桌面背景 + 听按钮 + 女儿端背景 |

### 已有动效

- `ShakeRoutine` — 错误时抖动弹回（6帧，幅度12px）
- `ScalePopRoutine` — 正确吸附缩放弹入（1.4→1.0，0.25s）

---

## 7. LivingRoom 客厅·重访·相册小游戏

### 触发

回到客厅后走到书桌按 F → `DeskViewController` 检查3张照片集齐 → 启动相册小游戏

### 相册小游戏 UI（AlbumMiniGameView，代码生成布局，部分接入贴纸/照片 Sprite）

| 元素 | 位置 | 规格 | 颜色 | 功能 |
|---|---|---|---|---|
| Title (Text) | (0, 460) | 1500×100, fontSize 42 | `(0.9, 0.85, 0.65)` 暖色 | "她叫什么名字？" |
| ClueButton | 左上(120, -60) | 200×80 | `(0.42, 0.32, 0.2)` 棕 | "查看线索" → 弹出线索面板 |
| AlbumBase (Image) | (0, 60) | 1200×600 | `(0.22, 0.18, 0.14)` 深棕 | 拼贴相册底图 |
| StickerZone ×6 (Image) | 按 `entry.stickerZonePosition` | `entry.stickerZoneSize` | 空白半透明/已放入sprite | 贴纸轮廓区 |
| NameTagZone ×5 (Image) | 按 `entry.nameTagZonePosition` | `entry.nameTagZoneSize` | 空白半透明/已填绿 | 姓名名牌区 |
| Sticker Draggable ×5 | (-440+i×220, -320) | 120×160 | sprite 或棕 | 可拖拽人物贴纸 |
| NameTag Draggable ×5 | 同上 | 200×60 | 棕色 | 可拖拽姓名标签 |
| Interrupt Button | (820, -460) | 250×110 | 棕色 | "暂时离开" |
| CompleteButton | (0, -400) | 300×90 | `(0.5, 0.35, 0.15)` 棕 | "完成" → 触发终局 |

### 线索面板（CluePanel）

| 元素 | 位置 | 规格 | 颜色 |
|---|---|---|---|
| CluePanel | 全屏 | 1600×900 | `(0.05, 0.04, 0.03)` alpha 0.95 |
| CluePhoto ×N | (-640+count×320, 50) | 280×350 | 老照片+泛黄 |
| ClueNote ×N | 照片左下偏移 | 260×120 | `(0.95, 0.82, 0.35)` 黄便签 |
| CloseClue Button | (0, -380) | 200×70 | 棕色 |

### 终局序列（ShowFamilyPortraitAndFinish）

1. 清除交互元素
2. 全屏显示 `RealisticFamilyPortrait`（1920×1080）→ CrossFadeAlpha 渐入 1.5s
3. 停留 4s
4. 淡入黑屏 2s → 等待 2.5s
5. 发送 `FinishMiniGame` → 进入 `GameEnded` → 黑屏覆盖

### AlbumConfig 字段说明

| 字段 | 类型 | 说明 |
|---|---|---|
| `miniGameId` | string | 小游戏唯一ID |
| `entries` | AlbumCharacterEntry[] | 人物条目（characterId, displayName, stickerSprite, photoSprite, clueText, stickerZone/NameTagZone pos+size, hasSticker） |
| `realisticFamilyPortrait` | Sprite | 写实风全家福图片 |
| `requiredPhotoIds` | string[] | 启动前置照片ID |

### 配置资产

| 资产 | 路径 | 美术资产已接入 |
|---|---|---|
| AlbumConfig | `Assets/_Project/Settings/AlbumConfig.asset` | ⚠️ 贴纸1/5（仅洪强）+ 线索照片3/6 + 写实全家福 ✅ |

---

## 8. 中断覆盖层（通用）

所有小游戏共用 `MiniGameManager.ShowInterruptedOverlay()` 创建。

| 元素 | 位置 | 规格 | 颜色 | 功能 |
|---|---|---|---|---|
| InterruptedOverlay (Image) | 全屏 | 全屏 | `(0.1, 0.08, 0.06)` alpha 0.98 | 中断遮罩 |
| Text "小游戏已中断" | (0, 100) | 1000×100, fontSize 44 | 白 | 标题 |
| Resume Button (Host only) | (-180, -80) | 300×110 | `(0.42, 0.32, 0.2)` 棕 | "继续" |
| Restart Button (Host only) | (180, -80) | 300×110 | 棕色 | "重新开始" |
| Waiting Text (Client) | (0, -80) | 1000×100, fontSize 28 | 白 | "等待女儿选择继续或重新开始。" |

### GameEnded 覆盖层

`MiniGameManager.ShowGameEndedOverlay()` — 全屏纯黑 Image

---

## 9. 探索阶段常驻 UI

### MemoryAlbumController（左上角记忆相册）

| 元素 | 位置 | 规格 | 颜色 | 功能 |
|---|---|---|---|---|
| MemoryAlbum (RectTransform) | 左上(24, -24) | 动态宽度 | — | 相册容器 |
| PhotoSlot ×N (Button) | (34+index×72+31, -62) | 62×62 | `(0.73, 0.59, 0.38)` 棕 | 已收集照片缩略格 |
| PendingPickup (Button) | 右下角偏移 | 180×150 | `(1, 0.78, 0.28)` 金色 | "新照片 点击收集" → Host可点 |
| PhotoPreview (全屏) | 铺满 | 760×520卡片 | 深底+棕卡 | 照片预览+关闭按钮 |

### 照片标题映射（硬编码于 MemoryAlbumController）

| photoId | 标题 |
|---|---|
| photo_hongqiang | 红墙前的合影 |
| photo_hongfang | 刘洪芳的照片 |
| bagua_old_family_photo | 旧家庭照片 |

---

## 10. 对白系统

### DialogueController（电影感字幕）

| 元素 | 规格 | 颜色 | 功能 |
|---|---|---|---|
| LetterboxTop/Bottom | 各162px高 | `(0, 0, 0)` alpha 0.92 | 电影感上下黑框（cinematic模式） |
| SubtitleBar | 底部180px高 | `(0, 0, 0)` alpha 0.85 | 非cinematic模式字幕条 |
| Speaker (Text) | 1200×40, fontSize 30 | 暖白/灰(内心) | 说话者名字 |
| Body (Text) | 1400×100, fontSize 34 | 白 | 台词正文 |
| ClickOverlay (Button) | 全屏透明 | 透明 | Host点击推进下一条台词 |

### DialogueSequence 字段说明

| 字段 | 类型 | 说明 |
|---|---|---|
| `sequenceId` | string | 序列唯一ID |
| `entries` | DialogueEntry[] | 台词条目（speaker, text, isInternal, audioClip） |
| `cinematicMode` | bool | true=黑框常驻, false=逐条黑底条 |
| `nextMiniGameId` | string | 播完后自动启动的小游戏ID |
| `nextDialogueId` | string | 播完后自动播放的下一段对白ID |

### 对白导入

菜单 `Tools > Dialogue > Import from Text` → 解析 `【角色名】台词` 格式的 .txt 文件 → 生成 `DialogueSequence.asset` 到 `Assets/_Project/Dialogue/`

---

## 11. ScriptableObject 配置资产清单

| 资产类型 | 路径 | 用途 |
|---|---|---|
| PlayerSettings | `Assets/_Project/Settings/PlayerSettings.asset` | 移动速度/交互范围/最大生命 |
| InputSettings | `Assets/_Project/Settings/PlayerInputSettings.asset` | 水平轴名/交互键/退出键 |
| MiniGameSettings | `Assets/_Project/Settings/MiniGameSettings.asset` | 小游戏通用设置 |
| RecipeConfig (番茄炒蛋) | `Assets/_Project/Settings/TomatoEggRecipe.asset` | 食材/调料/提示/奖励/链式 |
| RecipeConfig (黄瓜凉拌) | `Assets/_Project/Settings/CucumberSaladRecipe.asset` | 同上 |
| BaguaStoryConfig | `Assets/_Project/Settings/BaguaStoryConfig.asset` | 人物/字幕/音频/物件/老照片/投放区 |
| AlbumConfig | `Assets/_Project/Settings/AlbumConfig.asset` | 人物贴纸/姓名tag/线索/写实全家福 |
| DialogueSequence | `Assets/_Project/Dialogue/*.asset` | 说话者/台词/内心标记/音频/链式 |

---

## 12. 美术资产接入状况

截至 2026-08-29，队友已提供 112 张美术资产（PNG/JPG），存放于 `Assets/_Project/Art/` 下，已通过 `Tools > 3C Setup > Wire Art Assets` 接线到 ScriptableObject 配置和场景生成脚本。场景背景图、玩家行走序列帧（34帧）在运行时直接加载；小游戏 UI 中部分 Sprite 仍为代码生成的纯色占位（待队友补充后重新运行 Wire Art Assets 即可自动接入）。

### Inspector 挂接点（ScriptableObject 字段）

| 配置类 | 字段 | 类型 | 说明 | 是否已接入 |
|---|---|---|---|---|
| BaguaStoryConfig | `portrait` | Sprite | 人物立绘 | ✅ 3/3（洪秀/洪菊/洪斌） |
| BaguaStoryConfig | `storyAudio` | AudioClip | 人物八卦语音 | ❌ 待音频文件 |
| BaguaStoryConfig | `oldFamilyPhoto` | Sprite | 老照片图片 | ✅ bagua_old_family_photo.png |
| BaguaStoryConfig | `itemPlacements[].sprite` | Sprite | 桌面物件图标 | ✅ 8/8（3正确+5干扰） |
| BaguaStoryConfig | `deskBackground` | Sprite | 八卦桌面背景 | ✅ bg_desk.png |
| BaguaStoryConfig | `listenButtonSprite` | Sprite | 听按钮图标 | ✅ listen.png |
| BaguaStoryConfig | `daughterPhotoBackground` | Sprite | 女儿端认人背景 | ✅ daughter_game2_bg.png |
| AlbumConfig | `stickerSprite` | Sprite | 人物贴纸 | ⚠️ 1/5（仅洪强；洪秀/洪菊/洪芳/洪斌缺） |
| AlbumConfig | `photoSprite` | Sprite | 线索面板老照片 | ⚠️ 3/6（洪强/洪芳/洪梅；洪秀/洪菊/洪斌缺） |
| AlbumConfig | `realisticFamilyPortrait` | Sprite | 终局写实全家福 | ✅ reward_album.png |
| RecipeConfig | `ingredientSprites` | Sprite[] | 食材/调料图标 | ✅ 8/8（番茄/蛋/黄瓜/排骨/糖/盐/醋/辣椒） |
| RecipeConfig | `rewardPhotoSprite` | Sprite | 完成奖励照片 | ✅ reward_tomato_egg.png / reward_cucumber_salad.png |
| RecipeConfig | `containerSprite` | Sprite | 容器（锅）图标 | ✅ wok.jpg |
| RecipeConfig | `cookingBackground` | Sprite | 烹饪背景图 | ✅ bg_cooking.jpg |
| RecipeConfig | `dishPhotoSprite` | Sprite | 菜品完成照片 | ✅ dish_tomato_egg.png / dish_cucumber_salad.png |
| RecipeConfig | `daughterBackground` | Sprite | 女儿端背景 | ✅ daughter_bg.jpg |
| RecipeConfig | `motherCompleteBackground` | Sprite | 母亲完成背景 | ✅ mom_bg_final.png |
| RecipeConfig | `motherCompleteHint` | Sprite | 母亲完成提示图 | ✅ prompt_sugar.png |
| DialogueSequence | `audioClip` | AudioClip | 每条台词的配音 | ❌ 待音频文件 |
| IntroCutsceneController | `aigcVideoClip` | VideoClip | 开场AIGC视频 | ❌ 待视频文件 |
| DeskViewController | `deskBackgroundSprite` | Sprite | 书桌背景图 | ✅ livingroom_bgdesk.png |
| 场景背景 | LivingRoom/Kitchen/Courtyard | Sprite | 三场景全屏背景 | ✅ bg_livingroom / bg_kitchen / bg_courtyard |
| 玩家行走动画 | `WalkFrames/frame_XXXX` | Sprite[] | 34帧序列帧 | ✅ 运行时 Resources.Load 加载 |

---

## 13. 音效接口设计

### 设计原则

- **ScriptableObject 配置驱动**：所有音效引用集中在 `AudioLibrary` 资产中，不散落在各脚本
- **全局单例访问**：`AudioManager` 挂在场景中（或 DontDestroyOnLoad），各处通过 `AudioManager.Play(SfxId)` 调用
- **预留接口**：当前只定义接口和枚举，`Play` 方法体为空（`TODO` 标记），等音频文件到位后填充

### 文件清单

| 文件 | 路径 | 说明 |
|---|---|---|
| `SfxId.cs` | `Assets/_Project/Scripts/Audio/SfxId.cs` | 音效枚举（全部音效ID） |
| `AudioLibrary.cs` | `Assets/_Project/Scripts/Audio/AudioLibrary.cs` | ScriptableObject 配置（AudioClip 字典） |
| `AudioManager.cs` | `Assets/_Project/Scripts/Audio/AudioManager.cs` | 全局播放器（单例，Play/Stop方法） |

### 调用方式

```csharp
// 在任意脚本中调用
AudioManager.Play(SfxId.UiButtonClick);
AudioManager.Play(SfxId.CookIngredientDrop);
AudioManager.PlayOneShot(SfxId.BaguaCorrectMatch);
```

### 音频文件放置路径

```
Assets/_Project/Audio/
├── SFX/                    # 音效文件 (.wav / .mp3)
│   ├── ui_button_click.wav
│   ├── ui_button_hover.wav
│   ├── ui_panel_open.wav
│   ├── ...
├── BGM/                    # 背景音乐
│   ├── bgm_living_room.wav
│   ├── bgm_kitchen.wav
│   └── ...
└── Voice/                  # 配音（对白/八卦语音）
    ├── dialogue_intro_01.wav
    └── ...
```

### AudioLibrary 资产路径

```
Assets/_Project/Settings/AudioLibrary.asset
```

> **当前状态：** ❌ AudioLibrary.asset 尚未创建，`Assets/_Project/Audio/` 下无音频文件。AudioManager.Play() 方法体为空（TODO）。需先创建资产并接入音频文件。

创建方式：在 Inspector 中右键 `Create > Data > Audio Library`，然后将 `Assets/_Project/Audio/SFX/` 下的音频文件拖入对应字段。

---

## 14. 音效需求清单

### 通用 UI

| SfxId | 场景 | 触发条件 | 建议时长 | 优先级 |
|---|---|---|---|---|
| `UiButtonClick` | 全局 | 任何 Button.onClick | 0.1~0.2s | ★★★ |
| `UiButtonHover` | 全局 | Button Hover（预留） | 0.05~0.1s | ★ |
| `UiPanelOpen` | 全局 | 面板/覆盖层出现 | 0.2~0.3s | ★★ |
| `UiPanelClose` | 全局 | 面板/覆盖层关闭 | 0.2s | ★ |
| `UiError` | 全局 | 错误反馈（弹回/无效操作） | 0.3s | ★★ |
| `SceneTransition` | 全局 | 场景转场黑屏开始 | 1~2s | ★★ |

### MainMenu

| SfxId | 场景 | 触发条件 | 建议时长 | 优先级 |
|---|---|---|---|---|
| `MainMenuConnect` | 主菜单 | 点击创建/加入会话 | 0.3s | ★★★ |
| `MainMenuRoomCodeGenerated` | 主菜单 | 房间码生成显示 | 0.3s | ★★ |
| `MainMenuConnected` | 主菜单 | 连接成功 | 0.5s | ★★★ |
| `MainMenuDisconnected` | 主菜单 | 断线/连接失败 | 0.5s | ★★★ |
| `MainMenuKeyTyped` | 主菜单 | 输入房间码按键 | 0.05s | ★ |

### Intro

| SfxId | 场景 | 触发条件 | 建议时长 | 优先级 |
|---|---|---|---|---|
| `IntroMonologueLine` | 开场 | 每句独白渐入 | 0.5s | ★★ |
| `IntroReversalFlash` | 开场 | 时光回溯闪白 | 2s | ★★★ |
| `IntroFadeToBlack` | 开场 | 淡入黑屏转场 | 1.5s | ★★ |

### LivingRoom（书桌）

| SfxId | 场景 | 触发条件 | 建议时长 | 优先级 |
|---|---|---|---|---|
| `DeskAwakening` | 书桌 | 觉醒字幕开始 | 0.5s | ★★ |
| `DeskFadeIn` | 书桌 | 书桌画面淡入 | 2s | ★★ |
| `DeskClose` | 书桌 | 按 X 退出书桌 | 0.2s | ★ |
| `InteractPrompt` | 探索 | 按 F 交互（门/桌/触发器） | 0.15s | ★★★ |
| `DoorLocked` | 探索 | 前置条件不满足 | 0.3s | ★★ |

### 做饭小游戏

| SfxId | 场景 | 触发条件 | 建议时长 | 优先级 |
|---|---|---|---|---|
| `CookDragStart` | 做饭 | 开始拖拽食材/调料 | 0.1s | ★★★ |
| `CookIngredientDrop` | 做饭 | 食材正确放入锅 | 0.3s | ★★★ |
| `CookIngredientWrong` | 做饭 | 食材错误弹回 | 0.2s | ★★ |
| `CookSeasoningCorrect` | 做饭 | 调料正确 | 0.3s | ★★★ |
| `CookSeasoningWrong` | 做饭 | 调料错误弹回 | 0.2s | ★★ |
| `CookHintRequested` | 做饭 | 母亲点"帮我看看" | 0.2s | ★★ |
| `CookHintShown` | 做饭 | 提示文字出现 | 0.3s | ★★ |
| `CookComplete` | 做饭 | 小游戏完成 | 1s | ★★★ |
| `PhotoCollected` | 做饭/八卦 | 照片收集 | 0.5s | ★★★ |
| `MinigameInterrupt` | 做饭 | 中断 | 0.3s | ★ |
| `MinigameResume` | 做饭 | 恢复 | 0.3s | ★ |

### 八卦小游戏

| SfxId | 场景 | 触发条件 | 建议时长 | 优先级 |
|---|---|---|---|---|
| `BaguaDoorApproach` | 庭院 | 靠近门（距离音频已有） | — | — |
| `BaguaDoorOpen` | 庭院 | 按 F 偷听 | 0.5s | ★★ |
| `BaguaTransitionText` | 庭院 | 过渡文字出现 | 3s | ★★ |
| `BaguaAudioButtonStart` | 八卦 | 点"听"按钮 | 0.2s | ★★★ |
| `BaguaAudioComplete` | 八卦 | 音频/字幕播完 | 0.3s | ★★ |
| `BaguaItemDragStart` | 八卦 | 开始拖拽物品 | 0.1s | ★★ |
| `BaguaCorrectMatch` | 八卦 | 配对正确（吸附） | 0.3s | ★★★ |
| `BaguaWrongMatch` | 八卦 | 配对错误（抖动） | 0.2s | ★★ |
| `BaguaNameTagPlaced` | 八卦 | 姓名标签放置正确 | 0.3s | ★★★ |
| `BaguaNameTagWrong` | 八卦 | 姓名标签放置错误 | 0.2s | ★★ |

### 相册小游戏

| SfxId | 场景 | 触发条件 | 建议时长 | 优先级 |
|---|---|---|---|---|
| `AlbumStickerPlaced` | 相册 | 贴纸拖入正确轮廓 | 0.3s | ★★★ |
| `AlbumStickerWrong` | 相册 | 贴纸错误弹回 | 0.2s | ★★ |
| `AlbumNameTagPlaced` | 相册 | 姓名tag放置正确 | 0.3s | ★★★ |
| `AlbumNameTagWrong` | 相册 | 姓名tag错误弹回 | 0.2s | ★★ |
| `AlbumClueOpen` | 相册 | 打开线索面板 | 0.3s | ★★ |
| `AlbumClueClose` | 相册 | 关闭线索面板 | 0.2s | ★ |
| `AlbumComplete` | 相册 | 点击"完成"按钮 | 0.5s | ★★★ |
| `AlbumPortraitFadeIn` | 相册 | 写实全家福渐入 | 1.5s | ★★★ |
| `AlbumFadeToBlack` | 相册 | 淡入黑屏终局 | 2s | ★★★ |

### 对白系统

| SfxId | 场景 | 触发条件 | 建议时长 | 优先级 |
|---|---|---|---|---|
| `DialogueAdvance` | 对白 | 点击推进台词 | 0.1s | ★★ |
| `DialogueShowLetterbox` | 对白 | 黑框出现 | 0.3s | ★ |

### BGM

| BgmId | 场景 | 说明 | 优先级 |
|---|---|---|---|
| `BgmMainMenu` | 主菜单 | 低沉简约 | ★★ |
| `BgmIntro` | 开场过场 | 悬疑→温暖 | ★★ |
| `BgmLivingRoom` | 客厅探索 | 老屋安静氛围 | ★★ |
| `BgmKitchen` | 厨房做饭 | 轻快温暖 | ★★ |
| `BgmCourtyard` | 庭院 | 悬疑/好奇 | ★★ |
| `BgmAlbumEnding` | 相册终局 | 温馨→哀伤 | ★★★ |

---

## 15. 动效需求清单

### 已有动效

| 动效 | 实现位置 | 说明 |
|---|---|---|
| `ShakeRoutine` | CookingMiniGame, BaguaMiniGameView, AlbumMiniGameView | 错误时抖动弹回（6帧，12px） |
| `ScalePopRoutine` | BaguaMiniGameView | 正确吸附缩放弹入（1.4→1.0，0.25s） |
| `CrossFadeAlpha` | IntroCutsceneController, DeskViewController, AlbumMiniGameView, SceneTransitionTrigger | 文字/画面渐入渐出 |
| `CanvasGroup.alpha` | DeskViewController | 书桌画面淡入 |

### 需要新增的动效

| 场景 | 元素 | 建议动效 | 优先级 |
|---|---|---|---|
| MainMenu | CreateButton/JoinButton | Hover缩放+按下回弹 | ★★ |
| MainMenu | 房间码大字显示 | 逐字翻转/弹入 | ★★ |
| MainMenu | 连接成功转场 | 淡出+加载动画 | ★★★ |
| LivingRoom | 门交互提示 | F键提示气泡弹出 | ★★★ |
| Kitchen | 食材拖拽 | 拖拽时缩放+阴影 | ★★ |
| Kitchen | 食材入锅 | 缩小消失+锅冒蒸汽粒子 | ★★★ |
| Kitchen | 锅颜色变化 | 渐变过渡（当前直接变色） | ★ |
| Kitchen | 照片奖励出现 | 卡片翻转出现+光芒粒子 | ★★★ |
| Kitchen | 收集照片按钮 | Hover脉冲呼吸光 | ★★ |
| Courtyard | 人物卡"已听"状态 | 波纹扩散动画 | ★ |
| Courtyard | 老照片出现 | 抖动+泛黄渐入 | ★★ |
| Courtyard | 姓名标签固定 | 飞入+固定动画 | ★★ |
| Album | 贴纸拖入轮廓 | 缩放弹入（参考ScalePop） | ★★★ |
| Album | 线索面板弹出 | 面板缩放弹入+照片逐个淡入 | ★★ |
| Album | 完成按钮 | 脉冲呼吸光效 | ★★ |
| Dialogue | 字幕出现 | 逐字打字机效果 | ★★★ |
| Dialogue | 黑框出现 | 上下黑框滑入动画 | ★★ |
| 通用 | 中断覆盖层 | 淡入+按钮逐个弹入 | ★ |
| 通用 | 所有 Button | 统一 Hover 缩放 + 按下回弹 | ★★★ |
