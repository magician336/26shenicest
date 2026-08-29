# 领域术语表

## 角色与端

| 术语 | 代码层 | 设计/医学层 | 说明 |
|------|--------|------------|------|
| **女儿** | `SessionRole.Host` | 家属端 | 探索阶段操作者，负责推进剧情、给提示、调味、照片认人 |
| **母亲** | `SessionRole.Client` | 患者端 | 益智关操作者，负责选食材、听故事、配对物品 |

代码层统一使用 `Host` / `Client`；策划文档和医学语境使用"家属端" / "患者端"。

## 游戏阶段

| 术语 | 枚举值 | 说明 |
|------|--------|------|
| **探索** | `Exploration` | 自由移动，可触发小游戏、收集照片 |
| **小游戏** | `MiniGame` | 做饭、八卦或全家福相册进行中 |
| **小游戏中断** | `MiniGameInterrupted` | Host 暂停了小游戏，可选择继续或重新开始 |
| **终局** | `GameEnded` | 游戏主体已结束，等待终局内容（黑屏+文字+AIGC视频） |

## 做饭小游戏

| 术语 | 说明 |
|------|------|
| **菜谱** (`RecipeConfig`) | 一道菜的完整配置：食材、调料、提示、奖励、链式下一道菜 |
| **菜谱改痕** (`recipeNote`) | 菜谱边的手写笔记，揭示口味偏好的真相。仅女儿端可见 |
| **链式菜谱** (`nextRecipeId`) | 完成一道菜后自动启动下一道菜的机制 |

## 八卦小游戏

| 术语 | 说明 |
|------|------|
| **听过** (`heardStoryIds`) | 母亲按了声音按钮且音频/字幕自然播放完毕。听过后对应人物的虚线区域激活 |
| **配对** (`matchedCharacterIds`) | 正确物品拖入正确人物的虚线槽。配对后物品吸附填入，不可再拖出 |
| **投放** (`assignedPhotoZoneIds`) | 女儿将姓名标签拖到照片区域的正确位置 |
| **ItemPlacement** | 桌面物件的统一配置：itemId、displayName、sprite、anchoredPosition、isCorrect、characterId |
| **虚线区域** | 人物卡上的物品放置槽。未听过故事时灰色不可拖入；听过后高亮可拖入 |
| **字幕条** | 无音频时替代声音的信息来源，底部弹出，按时长（字数/5+1秒）自动消失 |

## 全家福相册小游戏

| 术语 | 说明 |
|------|------|
| **相册** (`AlbumConfig`) | 全家福相册配置：6个人物轮廓、贴纸素材、姓名tag、线索文本、写实风全家福 |
| **贴纸阶段** (`AlbumStep.PlaceStickers`) | 将5枚人物贴纸拖入6个轮廓框（小岩位置始终空缺）。逻辑匹配（characterId 校验） |
| **姓名tag阶段** (`AlbumStep.PlaceNameTags`) | 将5个姓名标签拖到已填入贴纸的名牌区域。逻辑匹配 |
| **完成按钮** | 5枚贴纸+5个姓名tag全部正确后出现，点击后全屏展示写实风全家福，淡入黑屏进入终局 |
| **小岩空缺** | 刘洪梅/小岩没有贴纸和姓名tag，轮廓始终空缺，引出终局 |
| **线索查看器** | 左上角常驻入口，点照片弹出信息页（老照片+黄色便签线索文本），双端可查看 |
| **同屏共玩** | 小游戏3不分 Host/Client 视图，双方看到完全相同的界面，都能拖拽，Host 权威判定 |
| **间隔提取** | 八卦认3人→相册认5人，间隔更长，有意的医学设计 |

## 照片奖励

| 术语 | 说明 |
|------|------|
| **待收集照片** (`pendingPhotoId`) | 小游戏完成后入队的奖励照片，Host 需点击收集 |
| **预览中照片** (`previewPhotoId`) | 收集后全屏预览的照片，Host 关闭预览后继续流程 |

## Prefab 视图架构

| 术语 | 说明 |
|------|------|
| **View 脚本** (`CookingView`) | 纯字段 MonoBehaviour，挂在 prefab 根节点上，通过 `[SerializeField]` 暴露所有 UI 元素引用。不含任何游戏逻辑 |
| **Controller** (`CookingMiniGame`) | 游戏控制器，持有 View 引用，通过 View 的公开属性更新 UI 元素（sprite、text、SetActive）。不直接创建 UI 元素 |
| **Prefab 模板** | 视觉唯一真相：元素位置、尺寸、层级关系、初始颜色、字体大小。设计师在编辑器中直接调整 |
| **原地更新** (`Render`) | 不再销毁重建 UI。`StartGame()` 实例化一次 prefab，`Render(state)` 通过 View 字段更新属性，`EndGame()` 销毁实例 |
| **食材槽位** (`IngredientSlotView`) | 可拖拽槽位的子组件结构：root（GameObject）、image（Image）、draggable（DraggableItem）。用于母亲端食材和女儿端调料的重复结构 |
| **双端面板** | 一个 prefab 包含 MotherPanel + DaughterPanel，运行时按 `SessionRole` 用 `SetActive` 切换 |

### 职责分工

| 关注点 | 归属 | 说明 |
|--------|------|------|
| 元素位置、尺寸、层级 | Prefab | 设计师在编辑器中拖拽调整 |
| 初始颜色、字体大小 | Prefab | 设计师在 Inspector 中设置 |
| Sprite、文字内容、游戏逻辑 | Config (ScriptableObject) | 程序通过代码在 Render 时填充 |
| 交互组件 (DraggableItem 等) | Prefab | 预挂在 prefab 上，代码只管回调绑定和数据填充 |
| Canvas + Panel | MiniGameManager (代码) | 共享基础设施，不属于某个小游戏 |
| Prefab 加载 | `Resources.Load` | 按 miniGameId 拼路径加载，存于 `Resources/MiniGamePrefabs/` |

## 网络与权限

| 术语 | 说明 |
|------|------|
| **Host 权威** | 所有游戏逻辑判定只在 Host 端发生。Client 通过 `GameplayIntent` 发送意图，Host 处理后广播 `GameplaySnapshot` |
| **Intent** (`GameplayIntent`) | 玩家操作的意图封装，由 `GameplayIntentType` 枚举区分 |
| **Snapshot** (`GameplaySnapshot`) | Host 广播的游戏状态快照，包含做饭/八卦/相册子状态和照片信息 |
