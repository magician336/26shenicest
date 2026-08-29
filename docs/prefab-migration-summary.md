# 小游戏 Prefab 化改造总结

## 改造目标

将三个小游戏（做饭、八卦、相册）的 UI 从**纯代码生成**迁移为 **Prefab + View 绑定**模式，使设计师能在 Unity 编辑器中直接拖拽调整布局。

## 决策记录

经过 grilling session 逐项确认，最终方案如下：

| 决策点 | 结论 |
|--------|------|
| 目标架构 | Prefab 做视觉模板，代码按 `[SerializeField]` 绑定数据 |
| 渲染模式 | 原地更新，不再销毁重建 |
| View/Controller 分离 | Prefab 根节点挂纯字段 View 脚本，MiniGame 类管逻辑 |
| Config 职责 | 降级为纯数据（sprite、文字、游戏逻辑），布局数据搬到 prefab |
| 元素数量 | 固定不变，prefab 预放全部槽位 |
| 双端视图（做饭） | 一个 prefab 包含母亲端+女儿端，运行时 `SetActive` 切换 |
| 双端视图（八卦） | 一个 prefab 包含 ClientPanel + HostPanel |
| 字段粒度 | 混合——独特元素逐个字段，重复结构用子组件数组 |
| 交互组件 | `DraggableItem`/`ClickableItem`/`ButtonHoverEffect` 全部预挂在 prefab 上 |
| Prefab 范围 | 只含 `_content`，Canvas + Panel 仍由 `MiniGameManager` 代码创建 |
| Prefab 加载 | `Resources.Load`，存于 `Resources/MiniGamePrefabs/` |
| 导出工具 | 逐个 MenuItem，调用代码生成层级 → `SaveAsPrefabAsset` → 自动绑定引用 |
| 迁移顺序 | 逐个闭环（导出→补绑定→重写代码→验证），从做饭开始 |

## 文件变更清单

### 新增文件（9 个）

| 文件 | 用途 |
|------|------|
| `Scripts/MiniGame/Cooking/CookingView.cs` | 做饭 View 绑定脚本（纯 `[SerializeField]` 字段） |
| `Scripts/MiniGame/Bagua/BaguaView.cs` | 八卦 View 绑定脚本 |
| `Scripts/MiniGame/Album/AlbumView.cs` | 相册 View 绑定脚本 |
| `Scripts/Editor/CookingPrefabExporter.cs` | `Tools > MiniGame > Export Cooking Prefab` |
| `Scripts/Editor/BaguaPrefabExporter.cs` | `Tools > MiniGame > Export Bagua Prefab` |
| `Scripts/Editor/AlbumPrefabExporter.cs` | `Tools > MiniGame > Export Album Prefab` |
| `docs/cooking-prefab-verification.md` | 做饭小游戏 prefab 验证指南 |
| `Resources/MiniGamePrefabs/CookingView.prefab` | 导出的做饭 prefab（运行时 `Resources.Load`） |
| `Resources/MiniGamePrefabs/BaguaView.prefab` | 导出的八卦 prefab |

### 修改文件（5 个）

| 文件 | 改动说明 |
|------|----------|
| `Scripts/MiniGame/Cooking/CookingMiniGame.cs` | 重写为 prefab + View 模式：`StartGame()` 加载 prefab → `GetComponent<CookingView>()`；`Render(state)` 原地更新 View 字段；`EndGame()` 销毁 prefab 实例 |
| `Scripts/MiniGame/Bagua/BaguaMiniGameView.cs` | 同上模式，双端面板（ClientPanel/HostPanel）切换 |
| `Scripts/MiniGame/Album/AlbumMiniGameView.cs` | 同上模式，单屏共玩（不分端） |
| `CONTEXT.md` | 新增「Prefab 视图架构」术语表 |
| `CODELY.md` | 新增 prefab 迁移项目记忆 |

### 未修改文件

| 文件 | 原因 |
|------|------|
| `MiniGameBase.cs` | 工厂方法保留——八卦和相册迁移后不再调用，但删除会影响未迁移的代码 |
| `MiniGameManager.cs` | 无需修改——现有流程（创建 bare GO → AddComponent → Initialize → Setup → StartGame）兼容新 prefab 模式 |

## 架构对比

### 旧架构（已废弃）

```
Render(state) → ClearContent() → 重新 new GameObject + AddComponent 创建所有元素
```

- 每次状态变化销毁重建整个 UI
- 拖拽中途状态刷新会丢失正在拖的元素
- 动画被中断
- 设计师无法在编辑器中调整布局

### 新架构

```
StartGame() → Resources.Load + Instantiate(prefab) → GetComponent<View>()
Render(state) → 通过 _view.XxxField 更新属性（sprite、text、SetActive）
EndGame() → Destroy(prefab 实例)
```

- Prefab 是视觉唯一真相（位置、尺寸、层级、初始颜色）
- Config 降级为纯数据（sprite、文字、游戏逻辑）
- 代码只做数据绑定和状态管理
- 设计师在编辑器中直接调整 prefab 布局

## 子组件结构

### 做饭（CookingView）

```
CookingView
├── Background (Image)
├── WaitingText (Text)
├── MotherPanel
│   ├── MotherRoleText, MotherInstructionText
│   ├── MotherContainerZone (DropZone)
│   ├── MotherIngredientSlot_0~3 (Image + DraggableItem)
│   ├── MotherWaitingText, MotherRecipeNoteText, MotherHintImage
│   └── MotherCompleteText, MotherDishPhoto
└── DaughterPanel
    ├── DaughterRoleText, DaughterWaitingText, DaughterInstructionText
    ├── DaughterDishZone, DaughterDishPhoto
    ├── DaughterSeasoningSlot_0~1 (Image + DraggableItem)
    ├── DaughterCompleteText, RewardPhotoImage, PhotoLabelText
    ├── CollectButtonRoot (Button + ButtonHoverEffect)
    ├── CollectGlowImage, CollectedText
    └── InterruptButtonRoot (Button + ButtonHoverEffect)
```

重复结构使用 `IngredientSlotView`（root + image + draggable）。

### 八卦（BaguaView）

```
BaguaView
├── Background, WaitingText
├── SubtitleBarRoot + SubtitleText
├── ClientPanel（母亲端：桌面配对）
│   ├── TaskBannerText, DesktopTrayImage
│   ├── DesktopItemSlot_0~7 (Image + DraggableItem)
│   ├── CharacterCard_0~2 (portrait + name + audioBtn + dropSlot + filledItem)
│   └── ClientWaitingText
├── HostPanel（女儿端：照片认人）
│   ├── HostRoleText, HostWaitingText
│   ├── PhotoBackgroundImage, PhotoInstructionText
│   ├── PhotoZone_0~2 (Image + PhotoNameDropZone)
│   ├── NameTagSlot_0~2 (Image + DraggableItem)
│   └── InterruptButtonRoot
├── CompleteText, RewardPhotoImage, PhotoLabelText
├── CollectButtonRoot, CollectedText
```

重复结构：`BaguaItemSlotView`、`CharacterCardView`、`PhotoZoneView`、`NameTagSlotView`。

### 相册（AlbumView）

```
AlbumView
├── AlbumBaseImage, TitleText, InstructionText
├── StickerZone_0~5 (Image)
├── NameTagZone_0~5 (Image + Text)
├── StickerDraggable_0~4 (Image + DraggableItem)
├── NameTagDraggable_0~4 (Image + DraggableItem + Text)
├── ClueButtonRoot, InterruptButtonRoot, CompleteButtonRoot
├── CluePanelRoot (照片 + 便签 + 关闭按钮)
├── FamilyPortraitImage, BlackScreenImage
```

重复结构：`StickerZoneView`、`NameTagZoneView`、`StickerDraggableView`、`NameTagDraggableView`、`CluePhotoView`。

## 导出工具工作流

每个小游戏一个独立 MenuItem：

1. 加载对应的 ScriptableObject Config
2. 创建临时 Canvas（1920×1080 ScaleWithScreenSize）
3. 代码构建完整 UI 层级（位置、尺寸、sprite 从 Config 读取）
4. 添加 View 组件，用 `SerializedObject` 自动绑定所有 `[SerializeField]` 引用
5. `PrefabUtility.SaveAsPrefabAsset` 保存到 `Resources/MiniGamePrefabs/`
6. 销毁临时 Canvas
7. 在 Project 窗口高亮 prefab 供检查

## Bug 修复记录

| Bug | 原因 | 修复 |
|-----|------|------|
| `ButtonHoverEffect` 编译错误 | 导出工具缺少 `using DoNotForgetMe.UI` | 添加 using |
| `StretchRect` NullReferenceException | `CreateUIObject` 已含 RectTransform，代码又 `AddComponent<RectTransform>()` 返回 null | 改为 `GetComponent<RectTransform>()` |
| Canvas 销毁失败 | `DestroyImmediate(canvas)` 只删组件，CanvasScaler 依赖它 | 改为 `DestroyImmediate(canvas.gameObject)` |
| Prefab 画面偏下 | `StretchRect` 未显式设置 `anchoredPosition`，Unity 留下残余偏移（MotherPanel 偏移 168px） | 添加 `anchoredPosition = Vector2.zero` + `sizeDelta = Vector2.zero` |
| 收集照片按钮无反应 | `_dragCallbacksWired` 在 `StartGame()` 时已为 true，导致收集按钮的 `onClick` 监听器从未添加 | 拆分为独立的 `_collectButtonWired` 标志 |
| 调料放错也能放进去 | 调料拖拽回调未校验 `_recipe.IsCorrectSeasoning()` | 添加正确调料校验，错误调料弹回 + 错误音效 |
| `PhotoZoneConfig?` 编译错误 | `FindZoneForCharacter` 返回 nullable struct，直接访问成员报错 | 用 `.Value.zoneId` 访问（`?.` 场景除外） |

## 使用方式

### 导出 Prefab

在 Unity 编辑器中依次执行：
1. **Tools > MiniGame > Export Cooking Prefab**
2. **Tools > MiniGame > Export Bagua Prefab**
3. **Tools > MiniGame > Export Album Prefab**

### 调整布局

1. 双击 prefab 进入编辑模式
2. 在 Scene 视图中拖拽元素或 Inspector 中修改 RectTransform
3. 退出编辑模式自动保存

### 运行验证

按 Play 运行，进入对应小游戏验证功能正常。详细步骤见 `docs/cooking-prefab-verification.md`。

## Git 提交

```bash
git init
git add -A
git commit -m "feat: migrate all three mini-games to prefab + View binding pattern"
```
