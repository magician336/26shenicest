# Prefab 化改造操作指南

## 一、改动范围

### 新增文件（9 个）

| 文件路径 | 用途 |
|----------|------|
| `Scripts/MiniGame/Cooking/CookingView.cs` | 做饭小游戏 View 绑定脚本（纯 `[SerializeField]` 字段，无逻辑） |
| `Scripts/MiniGame/Bagua/BaguaView.cs` | 八卦小游戏 View 绑定脚本 |
| `Scripts/MiniGame/Album/AlbumView.cs` | 相册小游戏 View 绑定脚本 |
| `Scripts/Editor/CookingPrefabExporter.cs` | Editor 工具：`Tools > MiniGame > Export Cooking Prefab` |
| `Scripts/Editor/BaguaPrefabExporter.cs` | Editor 工具：`Tools > MiniGame > Export Bagua Prefab` |
| `Scripts/Editor/AlbumPrefabExporter.cs` | Editor 工具：`Tools > MiniGame > Export Album Prefab` |
| `docs/cooking-prefab-verification.md` | 做饭小游戏 prefab 验证指南 |
| `docs/prefab-migration-summary.md` | 完整改造总结文档 |
| `Resources/MiniGamePrefabs/CookingView.prefab` | 做饭 prefab（运行时 `Resources.Load` 加载） |

### 修改文件（6 个）

| 文件路径 | 改动说明 |
|----------|----------|
| `Scripts/MiniGame/Cooking/CookingMiniGame.cs` | 重写：`StartGame()` 加载 prefab → `Render(state)` 原地更新 View 字段 → `EndGame()` 销毁实例；调味校验 `_recipe.IsCorrectSeasoning()` |
| `Scripts/MiniGame/Bagua/BaguaMiniGameView.cs` | 重写：同上模式；双端面板（ClientPanel/HostPanel）切换；照片认人 zone 激活修复；clientComplete 时先更新卡片再隐藏交互 |
| `Scripts/MiniGame/Album/AlbumMiniGameView.cs` | 重写：同上模式；单屏共玩；贴纸→姓名tag→完成动画 |
| `Scripts/Cutscene/CourtyardEavesdropView.cs` | 八卦完成后隐藏"进入偷听"按钮（检查 `collectedPhotoIds`） |
| `CONTEXT.md` | 新增「Prefab 视图架构」术语表 |
| `CODELY.md` | 新增 prefab 迁移项目记忆 + bug 修复经验 |

### 未修改文件

| 文件 | 原因 |
|------|------|
| `MiniGameBase.cs` | 工厂方法保留——Bagua/Album 旧调用已移除但删除会影响其他依赖 |
| `MiniGameManager.cs` | 现有流程兼容新 prefab 模式，无需修改 |

---

## 二、Bug 修复清单

| Bug | 原因 | 修复方式 |
|-----|------|----------|
| `ButtonHoverEffect` 编译错误 | 导出工具缺少 `using DoNotForgetMe.UI` | 添加 using |
| `StretchRect` 空引用 | `CreateUIObject` 已含 RectTransform，又调 `AddComponent<RectTransform>()` 返回 null | 改为 `GetComponent<RectTransform>()` |
| Canvas 销毁失败 | `DestroyImmediate(canvas)` 只删组件，CanvasScaler 依赖它 | 改为 `DestroyImmediate(canvas.gameObject)` |
| Prefab 画面偏下 | `StretchRect` 未设 `anchoredPosition`，Unity 残留偏移 168px | 显式设 `anchoredPosition=Vector2.zero` + `sizeDelta=Vector2.zero` |
| 收集照片按钮无反应 | `_dragCallbacksWired` 在 StartGame 时已 true，收集按钮监听器从未添加 | 拆分为独立的 `_collectButtonWired` 标志 |
| 错误调料能放进去 | 拖拽回调未校验正确调料 | 添加 `_recipe.IsCorrectSeasoning()` 校验 |
| 八卦照片投放区全错 | `HidePhotoView` 隐藏了所有 zone，`RenderPhotoView` 未重新激活 | 添加 `zone.root.SetActive(true)` |
| 八卦白底 | 共享 Background 白色 Image 始终激活 | `SetAllInactive` 中添加 `Background.SetActive(false)` |
| 第三张卡配对不显示 | `clientComplete` 分支 `return` 前未调 `UpdateCharacterCards` | 先调用 `UpdateCharacterCards` 再隐藏交互 |
| `PhotoZoneConfig?` 编译错误 | `FindZoneForCharacter` 返回 nullable struct | 用 `.Value` 访问成员（`?.` 场景除外） |
| 八卦完成后"进入偷听"重复出现 | `CourtyardEavesdropView` 在 Exploration 阶段重新显示按钮 | 检查 `collectedPhotoIds` 标记 `_baguaCompleted` |

---

## 三、Prefab 操作指南

### 3.1 生成 Prefab

在 Unity 编辑器中依次执行以下菜单：

```
Tools > MiniGame > Export Cooking Prefab
Tools > MiniGame > Export Bagua Prefab
Tools > MiniGame > Export Album Prefab
```

**前提条件：**
- RecipeConfig / BaguaStoryConfig / AlbumConfig 已创建并配置好 sprite
- 项目代码已编译无错误

**导出后：**
- Prefab 自动保存到 `Assets/_Project/Resources/MiniGamePrefabs/`
- Console 输出成功日志
- Project 窗口自动高亮 prefab 文件

### 3.2 检查 Prefab 结构

1. 双击 prefab 文件打开 Prefab 编辑模式
2. 确认所有元素存在且引用已绑定

**做饭 prefab 结构：**
```
CookingView (CookingView 脚本)
├── Background (Image, 全屏)
├── WaitingText (Text)
├── MotherPanel
│   ├── MotherRoleText / MotherInstructionText
│   ├── MotherContainerZone (透明 DropZone)
│   ├── MotherIngredientSlot_0~3 (Image + DraggableItem)
│   ├── MotherWaitingText / MotherRecipeNoteText / MotherHintImage
│   └── MotherCompleteText / MotherDishPhoto
└── DaughterPanel
    ├── DaughterRoleText / DaughterWaitingText / DaughterInstructionText
    ├── DaughterDishZone / DaughterDishPhoto
    ├── DaughterSeasoningSlot_0~1 (Image + DraggableItem)
    ├── DaughterCompleteText / RewardPhotoImage / PhotoLabelText
    ├── CollectButtonRoot (Button + ButtonHoverEffect)
    ├── CollectGlowImage / CollectedText
    └── InterruptButtonRoot (Button + ButtonHoverEffect)
```

**八卦 prefab 结构：**
```
BaguaView (BaguaView 脚本)
├── Background / WaitingText / SubtitleBarRoot+SubtitleText
├── ClientPanel（母亲端：桌面配对）
│   ├── TaskBannerText / DesktopTrayImage
│   ├── DesktopItemSlot_0~7 (Image + DraggableItem)
│   ├── CharacterCard_0~2 (portrait + name + audioBtn + dropSlot + filledItem)
│   └── ClientWaitingText
├── HostPanel（女儿端：照片认人）
│   ├── HostRoleText / HostWaitingText
│   ├── PhotoBackgroundImage / PhotoInstructionText
│   ├── PhotoZone_0~2 (Image + PhotoNameDropZone)
│   ├── NameTagSlot_0~2 (Image + DraggableItem)
│   └── InterruptButtonRoot
├── CompleteText / RewardPhotoImage / PhotoLabelText
└── CollectButtonRoot / CollectedText
```

**相册 prefab 结构：**
```
AlbumView (AlbumView 脚本)
├── AlbumBaseImage / TitleText / InstructionText
├── StickerZone_0~5 (Image)
├── NameTagZone_0~5 (Image + Text)
├── StickerDraggable_0~4 (Image + DraggableItem)
├── NameTagDraggable_0~4 (Image + DraggableItem + Text)
├── ClueButtonRoot / InterruptButtonRoot / CompleteButtonRoot
├── CluePanelRoot (照片 + 便签 + 关闭按钮)
├── FamilyPortraitImage / BlackScreenImage
```

### 3.3 检查引用绑定

1. 选中 prefab 根节点
2. Inspector 中查看 View 组件（CookingView / BaguaView / AlbumView）
3. 确认所有 `[SerializeField]` 字段不为 None
4. 如有未绑定的字段，手动拖拽对应 GameObject 到字段中

### 3.4 调整布局

在 Prefab 编辑模式中直接操作：

| 操作 | 方法 |
|------|------|
| 调整位置 | 选中元素 → Scene 视图拖拽 或 Inspector 修改 Anchored Position |
| 调整尺寸 | 选中元素 → 修改 Width / Height 或 Scene 视图用 Rect 工具拖拽 |
| 调整字体 | 选中 Text → 修改 Font Size 或替换 Font 字段 |
| 调整颜色 | 选中 Image/Text → 修改 Color |
| 调整层级 | Hierarchy 窗口中拖拽元素顺序（越靠下渲染越上） |

### 3.5 运行验证

1. 按 Play 运行
2. 进入对应小游戏（做饭走厨房触发器，八卦走庭院门，相册走客厅相册触发器）
3. 用 Tab 键切换角色测试双端

**做饭验证项：**
- [ ] 母亲端食材拖拽入锅
- [ ] 错误调料弹回 + 错误音效
- [ ] 正确调料入菜
- [ ] 完成后照片卡片翻转动画 + 收集按钮
- [ ] 双端切换正常

**八卦验证项：**
- [ ] 点击声音按钮播放故事
- [ ] 听过后虚线槽激活
- [ ] 物品拖入正确卡变色 + 物品填入
- [ ] 三组配对后切到照片认人
- [ ] 姓名标签拖入正确投放区
- [ ] 完成后照片奖励 + 收集按钮
- [ ] "进入偷听"按钮不再出现

**相册验证项：**
- [ ] 贴纸拖入正确轮廓
- [ ] 姓名标签拖入正确名牌
- [ ] 完成按钮出现
- [ ] 写实全家福渐入 → 黑屏 → 终局
- [ ] 线索面板可开关

### 3.6 重新导出

如果 Config 变了（新增 sprite、改了文字），重新运行导出工具即可。导出工具会覆盖已有 prefab。

> **注意：** 重新导出会覆盖你在 prefab 上的手动调整。如果已手动调过布局，不要重新导出。

---

## 四、架构说明

### 旧架构（已废弃）
```
Render(state) → ClearContent() → new GameObject + AddComponent 重建所有元素
```
- 每次状态变化销毁重建
- 拖拽中断、动画丢失
- 设计师无法在编辑器中调整

### 新架构
```
StartGame() → Resources.Load + Instantiate(prefab) → GetComponent<View>()
Render(state) → 通过 _view.XxxField 更新属性（sprite、text、SetActive）
EndGame() → Destroy(prefab 实例)
```

### 职责分工

| 关注点 | 归属 |
|--------|------|
| 元素位置、尺寸、层级、颜色、字体 | Prefab（设计师在编辑器调） |
| Sprite、文字内容、游戏逻辑 | Config（ScriptableObject） |
| 交互组件（DraggableItem 等） | Prefab（预挂） |
| Canvas + Panel | MiniGameManager（代码创建） |
| Prefab 加载 | `Resources.Load` |
