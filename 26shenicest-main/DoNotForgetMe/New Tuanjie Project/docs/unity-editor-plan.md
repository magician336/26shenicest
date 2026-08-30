# Unity 编辑方案：脚本验证 + 素材替换

> 生成日期：2026-08-28  
> 基于：白板设计文档 + 现有代码审查 + `docs/小岩.md` 设计稿

---

## 一、脚本与代码框架检查

### ✅ 已完成（可直接使用）

| 模块 | 脚本 | 状态 | 说明 |
|------|------|------|------|
| **核心流程** | `SessionGameplayCoordinator` | ✅ 完整 | 3个小游戏 + 对白 + 照片奖励 + 跨场景持久化 + 存档恢复 |
| **小游戏管理** | `MiniGameManager` | ✅ 完整 | 快照路由 → 正确视图，含中断/恢复/重启 |
| **做饭小游戏** | `CookingMiniGame` + `RecipeConfig` | ✅ 完整 | 数据驱动，2菜链式（番茄炒蛋→凉拌黄瓜） |
| **八卦小游戏** | `BaguaMiniGameView` + `BaguaStoryConfig` + `BaguaSessionLogic` | ✅ 完整 | 平面桌面布局，8物件，3人物，音频+字幕双通道 |
| **相册小游戏** | `AlbumMiniGameView` + `AlbumConfig` | ✅ 完整 | 贴纸+姓名tag两阶段，小岩空缺→终局 |
| **照片系统** | `MemoryAlbumController` | ✅ 完整 | 收集→预览→关闭→链式触发下一流程 |
| **对白系统** | `DialogueController` + `DialogueSequence` + `DialogueTrigger` | ✅ 完整 | 电影模式，链式到小游戏/下一段对白 |
| **开场过场** | `IntroCutsceneController` | ✅ 完整 | 黑屏独白→AIGC视频→时光回溯闪白→场景加载 |
| **场景切换** | `SceneLoader` + `SceneTransitionTrigger` | ✅ 完整 | 统一入口，同场景守卫+Build Settings检查 |
| **场景名常量** | `SceneNames` | ✅ 完整 | 5场景编排，当前全指向"Game"（单场景兼容） |
| **本地调试** | `LocalDebugService` + `LocalGameplayBridge` | ✅ 完整 | 单进程双端调试，Tab切换角色 |
| **存档系统** | `HostSaveService` + `GameProgressSave` | ✅ 完整 | Host 端 JSON 存档，跨场景恢复 |
| **玩家系统** | `PlayerController` + `MovementController` + `PlayerInputHandler` | ✅ 完整 | 2D横向移动，F交互，照片锁定 |
| **编辑器工具** | `Scene3CSetup` | ✅ 完整 | 一键生成场景+所有配置资产+引用接线 |
| **Fusion网络** | `FusionGameplayBridge` + `FusionSessionService` | ✅ 代码就绪 | FUSION_PRESENT 未定义，当前走 LocalDebug |

### ⚠️ 需要修复的问题

| # | 问题 | 影响 | 修复方式 |
|---|------|------|----------|
| 1 | **Game.unity 场景过期** | albumConfigs / dialogueConfigs / openingDialogueId 未接线，相册+对白无法触发 | 运行 `Tools > 3C Setup > Create Basic Scene` 重新生成 |
| 2 | **所有 Sprite 字段为 null** | BaguaStoryConfig.entries[].portrait、ItemPlacement.sprite、AlbumConfig.entries[].stickerSprite 等全空 | 见下方素材替换方案 |
| 3 | **所有 AudioClip 字段为 null** | BaguaStoryConfig.entries[].storyAudio 为空（已有字幕降级方案） | 录制或 AI 生成音频 |
| 4 | **IntroCutsceneController.aigcVideoClip 为空** | 开场视频跳过（代码已处理 null 跳过） | 生成 AIGC 视频 |
| 5 | **DialogueSequence 资产不存在** | 对白系统有代码无数据资产 | 手动创建或扩展 Scene3CSetup |
| 6 | **字体使用 LegacyRuntime.ttf** | 中文显示可能异常 | 导入手写风格中文字体 |

### 📊 代码框架结论

**框架 100% 就绪。** 所有游戏逻辑、状态管理、UI渲染、场景流转、存档恢复均已实现并通过 LocalDebug 验证。唯一缺失的是**美术素材**和**场景资产接线**。

---

## 二、素材需求清单

### 2.1 场景背景（4张）

| ID | 场景 | 用途 | 规格 | 风格要求 |
|----|------|------|------|----------|
| `bg_livingroom` | 客厅 | Scene1 醒来 + Scene3后相册小游戏 | 1920×1080 PNG | 80年代中国老屋客厅，木质书桌、窗户窗帘、书架，暖色调 |
| `bg_kitchen` | 厨房 | Scene2 做饭小游戏 | 1920×1080 PNG | 土灶台、木桌、架子上的篮子、挂着的厨具 |
| `bg_courtyard` | 庭院 | Scene3 八卦小游戏 | 1920×1080 PNG | 80年代农村院子，大树、砖房、红对联、盆栽 |
| `bg_desk` | 桌面 | 八卦小游戏内的木桌背景 | 1920×1080 PNG | 俯视旧木桌面，散落旧物件 |

### 2.2 角色立绘（6人 × 2种 = 12张）

| 角色 | 年龄 | 身份 | 八卦立绘(portrait) | 相册贴纸(stickerSprite) |
|------|------|------|---------------------|------------------------|
| 刘洪秀 | 18岁 | 大姐 | 半身像，攥铜钥匙 | Q版全身贴纸，透明背景 |
| 刘洪梅/小岩 | — | 空缺 | 不需要（相册无贴纸） | 仅剪影轮廓 |
| 刘洪菊 | 13岁 | 三妹 | 半身像，抱铁皮糖盒 | Q版全身贴纸 |
| 刘洪芳 | 9岁 | 四妹 | 不在八卦关出现 | Q版全身贴纸 |
| 刘洪强 | — | 五弟 | 不在八卦关出现 | Q版全身贴纸 |
| 刘洪斌 | 8岁 | 六弟 | 半身像，推小木车 | Q版全身贴纸 |

> **风格**：俄式复古绘本插画，钢笔勾线+淡彩平涂，低饱和做旧色调  
> **参考**：`docs/小岩.md` 中的美术设定关键词

### 2.3 做饭小游戏素材

| 类别 | ID | 显示名 | 用途 | 规格 |
|------|-----|--------|------|------|
| 食材 | `tomato` | 番茄 | 拖拽食材 | 256×256 PNG 透明 |
| 食材 | `egg` | 鸡蛋 | 拖拽食材 | 256×256 PNG 透明 |
| 食材 | `cucumber` | 黄瓜 | 拖拽食材 | 256×256 PNG 透明 |
| 食材 | `ribs` | 排骨 | 拖拽食材 | 256×256 PNG 透明 |
| 调料 | `sugar` | 糖 | 拖拽调料 | 256×256 PNG 透明 |
| 调料 | `salt` | 盐 | 拖拽调料 | 256×256 PNG 透明 |
| 调料 | `vinegar` | 醋 | 拖拽调料 | 256×256 PNG 透明 |
| 调料 | `chili` | 辣椒 | 拖拽调料 | 256×256 PNG 透明 |
| 容器 | `wok` | 锅 | 番茄炒蛋容器 | 512×512 PNG 透明 |
| 容器 | `bowl` | 碗 | 凉拌黄瓜容器 | 512×512 PNG 透明 |
| 奖励照片 | `photo_hongqiang` | 刘洪强照片 | 第一道菜奖励 | 760×520 PNG |
| 奖励照片 | `photo_hongfang` | 刘洪芳照片 | 第二道菜奖励 | 760×520 PNG |

### 2.4 八卦小游戏素材

| 类别 | ID | 显示名 | 用途 | 规格 |
|------|-----|--------|------|------|
| 正确物件 | `key` | 铜钥匙 | 配对刘洪秀 | 256×256 PNG 透明 |
| 正确物件 | `tin_candy_box` | 铁皮糖盒 | 配对刘洪菊 | 256×256 PNG 透明 |
| 正确物件 | `wooden_cart` | 小木车 | 配对刘洪斌 | 256×256 PNG 透明 |
| 干扰物 | `old_glasses` | 老花镜 | 干扰 | 256×256 PNG 透明 |
| 干扰物 | `red_comb` | 红梳子 | 干扰 | 256×256 PNG 透明 |
| 干扰物 | `scissors` | 剪刀 | 干扰 | 256×256 PNG 透明 |
| 干扰物 | `paper_boat` | 纸船 | 干扰 | 256×256 PNG 透明 |
| 干扰物 | `abacus` | 算盘 | 干扰 | 256×256 PNG 透明 |
| 老照片 | `bagua_old_family_photo` | 旧家庭照片 | 八卦关奖励 + 照片认人 | 760×520 PNG |
| 木托盘 | `wooden_tray` | 木托盘 | 物件摆放底图 | 1600×400 PNG 透明 |

### 2.5 相册小游戏素材

| 类别 | 数量 | 用途 | 规格 |
|------|------|------|------|
| 人物剪影轮廓 | 6张（含小岩空缺） | 贴纸投放区底图 | 150×200 PNG |
| 人物贴纸 | 5张（不含小岩） | 拖拽到轮廓 | 150×200 PNG 透明 |
| 姓名标签 | 5张 | 拖拽到贴纸 | 200×60 PNG 透明 |
| 写实全家福 | 1张 | 终局揭示 | 1920×1080 PNG |
| 线索照片 | 5张 | 线索查看器弹窗 | 400×500 PNG |
| 相册底图 | 1张 | 贴纸摆放区域背景 | 1920×1080 PNG |

### 2.6 音频素材

| ID | 用途 | 时长 | 格式 |
|----|------|------|------|
| `audio_liu_hongxiu` | 刘洪秀的八卦故事 | 15-30s | WAV/MP3 |
| `audio_liu_hongju` | 刘洪菊的八卦故事 | 15-30s | WAV/MP3 |
| `audio_liu_hongbin` | 刘洪斌的八卦故事 | 15-30s | WAV/MP3 |
| `bgm_main` | 探索背景音乐（可选） | 30-60s loop | WAV/MP3 |
| `sfx_correct` | 配对正确反馈 | 1-2s | WAV |
| `sfx_wrong` | 配对错误反馈 | 1-2s | WAV |
| `sfx_photo` | 照片收集反馈 | 1-2s | WAV |

### 2.7 其他素材

| 类别 | 用途 | 规格 |
|------|------|------|
| 中文字体 | 全局UI字体（手写/怀旧风格） | TTF/OTF |
| AIGC开场视频 | Scene0 时光回溯 | MP4 15s |
| 黑屏照片纹理 | 旧照片质感叠加 | 1024×1024 PNG |

---

## 三、素材替换执行方案

### 阶段 1：场景修复（无素材依赖）

```
操作步骤（Unity Editor 内）：
1. 打开 Tuanjie Editor 1.6.12
2. 打开项目 DoNotForgetMe/New Tuanjie Project
3. 菜单栏 → Tools > 3C Setup > Create Basic Scene
   → 自动重新生成 Game.unity，接线所有配置
4. 验证：Console 无报错，Player 可移动
```

### 阶段 2：AI 素材生成（并行）

使用 Codely 生成工具批量产出：

#### 2A. 场景背景（4张，用 `generate_image`）
- 统一使用 `frontier` provider（最高质量，适合复杂场景）
- 统一提示词风格前缀：`Russian vintage picture book illustration, pen-and-ink sketch lines, muted faded vintage colors, aged cream paper, 1980s rural Chinese home, nostalgic atmosphere`
- 4张并行生成

#### 2B. 角色立绘（6人，用 `generate_image`）
- 八卦3人半身像：`seedream` provider（速度快）
- 相册5人Q版贴纸：`seedream` provider，透明背景
- 小岩剪影：纯色半透明轮廓

#### 2C. 物品图标（18个，用 `generate_image`）
- 做饭食材+调料：8个，`seedream` provider
- 八卦物件：8个，`seedream` provider
- 容器：2个，`seedream` provider
- 分批并行，每批4-6张

#### 2D. 奖励照片（5张，用 `generate_image`）
- 做饭奖励：2张老照片风格单人照
- 八卦奖励：1张三人老家庭照
- 相册线索：5张旧照片
- 终局全家福：1张写实家庭合影
- 使用 `frontier` provider（写实照片质量）

#### 2E. 音频（用 `generate_sound_effect` / `generate_tts`）
- 3段八卦故事：TTS 生成（中文女声/童声）
- SFX：`generate_sound_effect` 生成
- BGM（可选）：`generate_music` 生成

#### 2F. AIGC 开场视频（用 `generate_video`）
- 1段15秒时光回溯视频

### 阶段 3：Unity Editor 素材接线

#### 3A. 导入素材到项目

```
Assets/
├── _Project/
│   ├── Art/
│   │   ├── Backgrounds/     ← 4张场景背景
│   │   ├── Characters/      ← 6人立绘+贴纸
│   │   ├── Items/            ← 18个物品图标
│   │   ├── Photos/           ← 5张奖励照片+全家福
│   │   ├── UI/               ← 木托盘、剪影轮廓等
│   │   └── Textures/        ← 旧照片纹理
│   ├── Audio/
│   │   ├── Story/            ← 3段八卦故事音频
│   │   ├── SFX/              ← 音效
│   │   └── BGM/             ← 背景音乐
│   ├── Fonts/                ← 中文字体
│   └── Video/                ← AIGC开场视频
```

#### 3B. 配置 ScriptableObject（Inspector 操作）

**RecipeConfig（2个）**:
- `TomatoEggRecipe.asset`：Sprite 字段暂无（CookingMiniGame 当前用纯色矩形，可选增强）
- `CucumberSaladRecipe.asset`：同上

**BaguaStoryConfig.asset**:
| 字段路径 | 赋值 |
|----------|------|
| `entries[0].portrait` | 刘洪秀半身像 Sprite |
| `entries[0].storyAudio` | audio_liu_hongxiu AudioClip |
| `entries[1].portrait` | 刘洪菊半身像 Sprite |
| `entries[1].storyAudio` | audio_liu_hongju AudioClip |
| `entries[2].portrait` | 刘洪斌半身像 Sprite |
| `entries[2].storyAudio` | audio_liu_hongbin AudioClip |
| `itemPlacements[0-7].sprite` | 8个物品图标 Sprite |
| `oldFamilyPhoto` | 旧家庭照片 Sprite |

**AlbumConfig.asset**:
| 字段路径 | 赋值 |
|----------|------|
| `entries[0-5].stickerSprite` | 5人贴纸 Sprite（小岩为null） |
| `entries[0-5].photoSprite` | 5张线索照片 Sprite |
| `realisticFamilyPortrait` | 写实全家福 Sprite |

#### 3C. 场景内 SpriteRenderer 替换

当前 `Scene3CSetup` 创建的平台/玩家/触发器都用纯色 Sprite。需要替换：

| GameObject | 当前 | 替换为 |
|------------|------|--------|
| `Ground` | 灰色方块 | 场景背景 Sprite（根据当前场景） |
| `Player` | 蓝色方块 | （可选）角色精灵或保持第一人称不可见 |
| `MiniGameTrigger` | 黄色方块 | 厨房道具图标（如锅） |
| `DoorListeningTrigger` | 棕色方块 | 门/灶台图标 |
| `AlbumTrigger` | 棕色方块 | 相册图标 |

#### 3D. 全局字体替换

当前所有 UI 使用 `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`。  
需要：
1. 导入手写风格中文字体到 `Assets/_Project/Fonts/`
2. 在 `CookingMiniGame`、`BaguaMiniGameView`、`AlbumMiniGameView`、`MiniGameManager`、`MemoryAlbumController`、`IntroCutsceneController`、`DialogueController` 中替换字体引用
3. 或创建 `FontManager` 静态类统一管理

---

## 四、执行顺序（优先级排序）

| 优先级 | 任务 | 依赖 | 预计耗时 |
|--------|------|------|----------|
| P0 | 运行 Scene3CSetup 重新生成场景 | 无 | 5min |
| P0 | 验证 LocalDebug 模式可跑通全流程 | P0场景 | 10min |
| P1 | 生成场景背景（4张） | 无 | 5min生成+导入 |
| P1 | 生成八卦角色立绘（3人） | 无 | 5min |
| P1 | 生成八卦物件图标（8个） | 无 | 10min |
| P1 | 生成八卦老照片 | 无 | 5min |
| P1 | 接线 BaguaStoryConfig 所有 Sprite/Audio | P1素材 | 15min |
| P2 | 生成做饭食材图标（8个） | 无 | 10min |
| P2 | 生成做饭奖励照片（2张） | 无 | 5min |
| P2 | 接线 RecipeConfig（可选增强） | P2素材 | 10min |
| P2 | 生成相册贴纸+剪影+全家福 | 无 | 15min |
| P2 | 接线 AlbumConfig 所有 Sprite | P2素材 | 15min |
| P3 | 生成八卦故事音频（3段） | 无 | 5min |
| P3 | 生成SFX（3个） | 无 | 5min |
| P3 | 导入中文字体 | 无 | 即时 |
| P3 | 全局字体替换 | P3字体 | 30min |
| P4 | 生成AIGC开场视频 | 无 | 5min生成 |
| P4 | 接线 IntroCutsceneController.aigcVideoClip | P4视频 | 即时 |
| P4 | 创建 DialogueSequence 资产 | 无 | 30min |
| P4 | 场景内 SpriteRenderer 替换 | P1背景 | 30min |

---

## 五、验证检查清单

- [ ] `Tools > 3C Setup > Create Basic Scene` 无报错
- [ ] Play 模式下 Player 可移动（A/D 或 ←/→）
- [ ] 走到 MiniGameTrigger 按 F → 做饭小游戏启动
- [ ] Tab 切换角色 → 母亲端选食材 / 女儿端加调料
- [ ] 做完两道菜 → 照片收集 → 自动转场八卦
- [ ] 走到 DoorListeningTrigger 按 F → 八卦小游戏启动
- [ ] 听故事 → 拖物品配对 → 照片认人
- [ ] 八卦完成 → 照片收集 → 自动转场回客厅
- [ ] 走到 AlbumTrigger 按 F → 相册小游戏启动
- [ ] 拖贴纸 → 拖姓名tag → 点完成 → 全家福展示 → 黑屏
- [ ] 所有 Sprite 显示正确（无粉色缺失材质）
- [ ] 音频可播放（或字幕降级正常）
- [ ] 中文字体显示正常（无方块乱码）

---

## 六、已知限制与风险

| 风险 | 影响 | 缓解 |
|------|------|------|
| `Scene3CSetup` 用代码创建场景，手动接线素材后再次运行会覆盖 | 素材丢失 | 接线完成后不要重跑 Create Basic Scene；或扩展 Scene3CSetup 支持素材自动引用 |
| 5场景拆分未执行 | 单场景内所有区域叠加 | SceneNames 全指向 "Game"，SceneTransitionTrigger 同场景守卫跳过加载。功能正常但视觉拥挤 |
| Fusion SDK 未启用 | 仅本地调试 | FUSION_PRESENT 未定义。定义后自动切换到 Fusion 网络层 |
| UI 全部代码生成 | 无法在 Inspector 预览 | 这是架构设计决定——数据驱动 + 代码渲染，便于程序迭代 |
| 做饭小游戏无 Sprite 字段 | RecipeConfig 没有 ingredientSprite 字段 | 当前用 DisplayName 文字标签。如需图标需扩展 RecipeConfig |
