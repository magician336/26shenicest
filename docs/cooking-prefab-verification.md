# 做饭小游戏 Prefab 迁移验证指南

## 概述

做饭小游戏已从纯代码生成 UI 迁移为 **Prefab + CookingView 绑定** 模式。本文档指导你在 Unity 编辑器中完成 prefab 导出、验证和手动调整。

---

## 第一步：导出 Prefab

### 前提条件

- RecipeConfig.asset 已创建并配置好 sprite（通过 `Tools > 3C Setup > Create Basic Scene` 或 `Update Recipe Assets`）
- 项目代码已编译无错误

### 操作

1. 在 Unity 编辑器顶部菜单点击 **Tools > MiniGame > Export Cooking Prefab**
2. 等待执行完成，Console 应显示：
   ```
   [CookingPrefabExporter] Prefab 已保存到 Assets/_Project/Resources/MiniGamePrefabs/CookingView.prefab
   [CookingPrefabExporter] CookingView 引用已自动绑定
   ```
3. Project 窗口中会自动选中并高亮 `CookingView.prefab`

### 如果导出失败

| 错误 | 原因 | 解决 |
|------|------|------|
| "未找到 RecipeConfig.asset" | 场景配置未生成 | 运行 `Tools > 3C Setup > Create Basic Scene` |
| 编译错误 | 代码有语法问题 | 检查 Console，修复编译错误后重试 |
| "Prefab 上缺少 CookingView 组件" | 导出工具逻辑异常 | 检查 CookingView.cs 是否在正确目录 |

---

## 第二步：检查 Prefab 结构

1. 双击 `Assets/_Project/Resources/MiniGamePrefabs/CookingView.prefab` 打开 Prefab 编辑模式
2. 确认层级结构如下：

```
CookingView (CookingView 脚本)
├── Background (Image, 全屏拉伸)
├── WaitingText (Text, "等待联机角色…")
├── MotherPanel (空 GameObject)
│   ├── MotherRoleText (Text)
│   ├── MotherInstructionText (Text)
│   ├── MotherContainerZone (Image, 透明, 用于拖拽判定)
│   ├── MotherDroppedNamesText (Text)
│   ├── MotherIngredientSlot_0 (Image + DraggableItem)
│   ├── MotherIngredientSlot_1 (Image + DraggableItem)
│   ├── MotherIngredientSlot_2 (Image + DraggableItem)
│   ├── MotherIngredientSlot_3 (Image + DraggableItem)
│   ├── MotherWaitingText (Text)
│   ├── MotherRecipeNoteText (Text)
│   ├── MotherHintImage (Image)
│   ├── MotherCompleteText (Text)
│   └── MotherDishPhoto (Image)
└── DaughterPanel (空 GameObject)
    ├── DaughterRoleText (Text)
    ├── DaughterWaitingText (Text)
    ├── DaughterInstructionText (Text)
    ├── DaughterDishZone (Image, 透明, 用于拖拽判定)
    ├── DaughterDishPhoto (Image)
    ├── DaughterSeasoningSlot_0 (Image + DraggableItem)
    ├── DaughterSeasoningSlot_1 (Image + DraggableItem)
    ├── DaughterCompleteText (Text)
    ├── RewardPhotoImage (Image)
    │   └── PhotoLabelText (Text)
    ├── CollectButtonRoot (Image + Button + ButtonHoverEffect)
    │   └── CollectButtonLabel (Text)
    ├── CollectGlowImage (Image)
    ├── CollectedText (Text)
    └── InterruptButtonRoot (Image + Button + ButtonHoverEffect)
        └── InterruptButtonLabel (Text)
```

3. 选中根节点 `CookingView`，在 Inspector 中检查 **CookingView** 组件
4. 确认所有 `[SerializeField]` 字段已自动填充（不为 None）：
   - Background → Background
   - WaitingText → WaitingText
   - Mother Panel → MotherPanel
   - Mother Role Text → MotherRoleText
   - ...（所有字段都应有引用）
   - Mother Ingredient Slots 数组 → 4 个元素，每个的 root/image/draggable 都已绑定
   - Daughter Seasoning Slots 数组 → 2 个元素，每个的 root/image/draggable 都已绑定

### 如果引用未绑定

手动拖拽对应 GameObject 到字段中。字段名与 GameObject 名一一对应。

---

## 第三步：在编辑器中调整布局

这是迁移的核心目的——你现在可以在 Prefab 编辑模式中直接调整：

### 调整位置

1. 双击进入 Prefab 编辑模式
2. 选中任意元素（如 `MotherIngredientSlot_0`）
3. 在 Scene 视图中拖拽或 Inspector 中修改 RectTransform 的 `Anchored Position`
4. 退出 Prefab 编辑模式（顶部左箭头）自动保存

### 调整尺寸

- 选中元素 → 修改 RectTransform 的 `Width` / `Height`
- 或在 Scene 视图中用 Rect 工具拖拽边缘

### 调整字体

- 选中 Text 元素 → Inspector 中修改 `Font Size`
- 可替换 Font 字段为项目中的自定义字体

### 调整颜色

- 选中 Image/Text 元素 → Inspector 中修改 `Color`

### 调整层级

- 在 Hierarchy 窗口中拖拽元素调整顺序
- 越靠下的元素渲染在上方

---

## 第四步：运行验证

### 4.1 进入做饭小游戏

1. 打开 Kitchen 场景（或 Game 场景）
2. 按 Play 运行
3. 使用 Tab 键切换角色为 **Client（母亲）**
4. 走到锅/灶台触发器位置，按 F 进入做饭小游戏

### 4.2 验证母亲端（Client）

| 检查项 | 预期结果 |
|--------|----------|
| 背景显示 | 显示 CookingBackground 精灵 |
| 角色文字 | "母亲端 · 请做番茄炒蛋" |
| 指令文字 | "把需要的食材拖进锅里" |
| 食材槽位 | 4 个食材图标可见（番茄、鸡蛋、黄瓜、排骨） |
| 拖拽食材 | 拖拽食材到锅区域，食材缩小消失 + 蒸汽特效 |
| 拖到锅外 | 拖到非锅区域，食材回到原位 + 错误音效 |
| 食材入锅后 | 对应食材槽位消失，锅上方显示已放入食材名 |
| 完成后 | 背景切换为 MotherCompleteBackground，显示完成文字和菜品照片 |

### 4.3 验证女儿端（Host）

1. 按 Tab 切换为 **Host（女儿）**

| 检查项 | 预期结果 |
|--------|----------|
| 背景显示 | 显示 DaughterBackground 精灵 |
| 角色文字 | "女儿端 · 查看菜谱改痕，为菜调味" |
| 等待状态 | 母亲未放食材时显示"等待母亲把食材放入锅中…" |
| 调味阶段 | 母亲放完食材后显示菜图 + 2 个调料图标 |
| 调料拖拽 | 拖调料到菜图区域，发送调味意图 |
| 中断按钮 | 右下角显示"暂时离开"按钮 |
| 完成状态 | 显示照片奖励（卡片翻转动画）+ 收集按钮（呼吸光） |
| 收集照片 | 点击收集按钮，触发 FinishMiniGame |

### 4.4 验证双端切换

1. 在母亲端操作时按 Tab 切到女儿端
2. 确认面板切换正确（MotherPanel 隐藏，DaughterPanel 显示）
3. 切回母亲端，确认状态保持

### 4.5 验证 Prefab 修改生效

1. 停止 Play
2. 打开 CookingView.prefab
3. 把某个食材槽的位置改一个明显的值（如 X+200）
4. 保存 Prefab
5. 再次 Play 进入做饭小游戏
6. 确认食材槽位置已更新

---

## 第五步：常见问题排查

### Q: Play 时 Console 报 "未找到 MiniGamePrefabs/CookingView prefab"

**A:** Prefab 未导出或路径不对。确认 `Assets/_Project/Resources/MiniGamePrefabs/CookingView.prefab` 存在。如不存在，运行 `Tools > MiniGame > Export Cooking Prefab`。

### Q: 食材拖拽没有反应

**A:** 检查 prefab 中食材槽的 `DraggableItem` 组件是否存在且 `Image.raycastTarget = true`。

### Q: 食材拖到锅上没有触发回调

**A:** 检查 `MotherContainerZone` 的引用是否绑定。它需要在 prefab 中存在且引用到 CookingView 的 `_motherContainerZone` 字段。

### Q: 调料拖拽没有反应

**A:** 检查 `DaughterDishZone` 引用是否绑定，以及调料槽的 `DraggableItem` 是否存在。

### Q: 完成后照片不显示

**A:** 检查 `RewardPhotoImage` 引用是否绑定，以及 RecipeConfig 的 `rewardPhotoSprite` 是否配置了精灵。

### Q: 背景不显示

**A:** 检查 `Background` 引用是否绑定，以及 RecipeConfig 的 `cookingBackground` / `daughterBackground` 是否配置了精灵。

### Q: 修改了 Prefab 但运行时没变化

**A:** 确认你修改的是 `Resources/MiniGamePrefabs/CookingView.prefab` 而不是场景中的实例。运行时加载的是 Resources 目录下的 prefab。

---

## 文件清单

| 文件 | 用途 |
|------|------|
| `Scripts/MiniGame/Cooking/CookingView.cs` | View 绑定脚本，纯字段，挂在 prefab 根节点 |
| `Scripts/MiniGame/Cooking/CookingMiniGame.cs` | 游戏控制器，通过 CookingView 更新 UI |
| `Scripts/Editor/CookingPrefabExporter.cs` | Editor 工具，生成 prefab 并自动绑定引用 |
| `Resources/MiniGamePrefabs/CookingView.prefab` | 导出的 prefab 文件（运行时 Resources.Load） |

---

## 架构说明

### 旧架构（已废弃）
```
Render(state) → ClearContent() → 重新 new GameObject + AddComponent 创建所有元素
```
每次状态变化都销毁重建整个 UI，拖拽中途状态会丢失。

### 新架构
```
StartGame() → Resources.Load + Instantiate(prefab) → GetComponent<CookingView>()
Render(state) → 通过 _view.XxxField 更新属性（sprite、text、SetActive）
EndGame() → Destroy(prefab实例)
```
- Prefab 是视觉唯一真相（位置、尺寸、层级、初始颜色）
- Config 降级为纯数据（sprite、文字、游戏逻辑）
- 代码只做数据绑定和状态管理
- 设计师在编辑器中直接调整 prefab 布局
