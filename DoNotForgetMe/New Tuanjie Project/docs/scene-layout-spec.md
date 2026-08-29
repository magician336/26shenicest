# 场景布置规格标准

> 版本: 2.0 | 日期: 2026-08-29
> 适用场景: LivingRoom / Kitchen / Courtyard

## 一、全局常量（SceneSetupBase）

| 常量 | 值 | 说明 |
|---|---|---|
| `SCENE_VIEW_HEIGHT` | 7.0 | 背景图统一缩放高度（世界单位） |
| `CAMERA_ORTHO_SIZE` | 5.0 | 相机正交尺寸（视野高度=10） |
| `CAMERA_FIXED_Y` | -0.5 | 相机固定 Y |
| `BG_CENTER_Y` | -0.5 | 背景图中心 Y |
| `PLAYER_Y` | -2.0 | 玩家 Y 坐标（脚部≈-3.0，站在背景地面上） |
| `SUBTITLE_BAR_Y` | -5.0 | 字幕条 Y（在相机视野底部内可见） |
| `FIXED_ASPECT` | 16/9 | 固定宽高比（Edit Mode 下 Screen.width/height 不可靠） |
| `BG_SORTING_ORDER` | -10 | 背景 SpriteRenderer 渲染层 |
| `BLACK_BG_SORTING_ORDER` | -20 | 黑底板渲染层 |

## 二、角色规格

| 属性 | 值 | 说明 |
|---|---|---|
| 走路帧尺寸 | 2048×1024 | 每帧纹理像素 |
| 角色像素尺寸 | 268×931 | 非透明区域（90.9% 帧高） |
| PPU | 466 | Sprite.Create 的 pixelsPerUnit |
| 角色世界高度 | 2.0 | 931/466 |
| 角色世界宽度 | 0.58 | 268/466 |
| Collider | 0.6×1.8 | 略宽于角色，高度接近角色 |
| 位置 Y | -2.0 | 脚部 ≈ -3.0（站在背景地面上） |

## 三、几何关系

```
相机视野: 高=10 (orthoSize=5 × 2), Y中心=-0.5
    → 顶部 = +4.5, 底部 = -5.5

背景图: 高=7, Y中心=-0.5
    → 顶部 = +3.0, 底部 = -4.0
    → 上方黑边 = 4.5 - 3.0 = 1.5
    → 下方黑边 = -4.0 - (-5.5) = 1.5

字幕条: Y=-5.0, 高=1.6
    → 顶部 = -4.2, 底部 = -5.8
    → 顶部在背景底边(-4.0)之下，不遮挡背景
    → 底部在相机视野底(-5.5)之下，字幕条主体可见

地面: Y=-3.0 (玩家脚踩位置)
玩家: Y=-2.0, 高=2.0, 脚部=-3.0
```

## 四、背景图尺寸与场景宽度

| 场景 | Sprite | 像素尺寸 | Aspect | bgWorldWidth | 墙体位置 |
|---|---|---|---|---|---|
| LivingRoom | bg_livingroom | 1983×793 | 2.501 | 17.51 | ±8.75 |
| Kitchen | bg_kitchen | 2048×683 | 2.999 | 20.99 | ±10.49 |
| Courtyard | bg_courtyard | 1906×557 | 3.422 | 23.95 | ±11.98 |

## 五、相机水平边界

```
halfView = CAMERA_ORTHO_SIZE × FIXED_ASPECT = 5 × 1.778 = 8.89
camMinX = -(bgWorldWidth × 0.5) + halfView
camMaxX =  (bgWorldWidth × 0.5) - halfView
```

| 场景 | bgWorldWidth | camMinX | camMaxX | 说明 |
|---|---|---|---|---|
| LivingRoom | 17.51 | 0 | 0 | 背景比视野窄，不能水平移动 |
| Kitchen | 20.99 | -1.61 | +1.61 | 可小幅水平移动 |
| Courtyard | 23.95 | — | — | 无 constrainX（子页面） |

## 六、触发器位置标准

### LivingRoom（探索场景，有 Player）

| 对象 | 类型 | X | Y | 说明 |
|---|---|---|---|---|
| PlayerSpawn | 出生点 | -3.0 | -2.0 | 画面左侧偏中 |
| DeskViewController | 小游戏触发(书桌) | 2.0 | -2.0 | 画面中右 |
| DoorToKitchen | 场景切换→厨房 | 6.0 | -2.0 | 画面右侧 |

### Kitchen（探索场景，有 Player）

| 对象 | 类型 | X | Y | 说明 |
|---|---|---|---|---|
| PlayerSpawn | 出生点 | -3.0 | -2.0 | 画面左侧偏中 |
| MiniGameTrigger | 小游戏触发(灶台) | 2.0 | -2.0 | 画面中右 |
| DoorToLivingRoom | 场景切换→客厅 | -6.0 | -2.0 | 画面左侧 |
| DoorToCourtyard | 场景切换→庭院 | 6.0 | -2.0 | 需 photo_hongqiang + photo_hongfang |

### Courtyard（子页面，无 Player）

| 对象 | 类型 | 说明 |
|---|---|---|
| CourtyardEavesdropView | 子页面控制器 | 全屏 UI（进入/退出偷听按钮） |

## 七、字幕条规格

| 属性 | 值 |
|---|---|
| Y 位置 | -5.0 |
| 高度 | 1.6 |
| 宽度 | max(bgWorldWidth, viewWidth) + 4 |
| 颜色 | 纯黑 (0,0,0,1) |
| sortingOrder | 5 |
| SubtitleText Y | -5.0, z=-1 |
| SubtitleText sizeDelta | (barWidth, 1.2) |
| SubtitleText sortingOrder | 10 |
| 字体 | 中华薪火体 SDF |
| 字号 | 3.0 (TMP) |

## 八、各场景对象清单

| 场景 | Player | Camera | AudioListener | SubtitleBar | EventSystem | 触发器 |
|---|---|---|---|---|---|---|
| LivingRoom | ✅ Y=-2.0 | ✅ (constrainX=0) | ✅ | ✅ Y=-5.0 | ✅ | DeskVC(2), Door(6), Spawn(-3) |
| Kitchen | ✅ Y=-2.0 | ✅ (constrainX=±1.61) | ✅ | ✅ Y=-5.0 | ✅ | MiniGame(2), Doors(±6), Spawn(-3) |
| Courtyard | ❌ | ✅ (无constrainX) | ✅ | ✅ Y=-5.0 | ✅ | CourtyardEavesdropView |

## 九、已知注意事项

1. **Edit Mode 下不要调用 DontDestroyOnLoad** — 会抛 InvalidOperationException 导致场景生成中断
2. **FIXED_ASPECT 替代 Screen.width/height** — Edit Mode 下 Screen 尺寸不可靠
3. **SimpleWalkAnimation 的 PPU=466** — 3 处 Sprite.Create 调用（包括 !isReadable 回退路径）
4. **角色纹理不可读** — 走的是 !isReadable 回退路径，PPU 必须在 3 处统一
