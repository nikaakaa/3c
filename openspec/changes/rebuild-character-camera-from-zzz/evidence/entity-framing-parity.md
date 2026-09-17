# 实体取景对账：工程几何分支 vs ZZZ cameraLockBossConfig（3.1）

修订 v1，2026-09-17。来源：`replication-guide/analysis/基础镜头.md` 的 `cameraLockBossConfig` 完整字段。结论：**来源存在且完整；工程当前为通用几何取景分支，ZZZ 的极角曲线驱动未还原**。按"不造近似公式、保留未验证边界"原则记录差异。

## 字段对账

| ZZZ cameraLockBossConfig | 值 | 工程现状 | 判定 |
|---|---|---|---|
| LT_MAX_RANGE | 50 | radius 无上限 clamp | 差异：工程缺最大距离上限 |
| LT_LOOKATOFFSETRATIO | 0.2 | TwoPointFrame 用 stage.HeightRatio（作者填） | 语义近似：比率来源不同（来源常量 vs 工程作者字段） |
| LT_BOSS_ELEVATION_REGION | (0.2, 1) | evaluatedPitch 由 PitchLimit clamp | 语义近似：来源按仰角区域，工程按 pitch 限幅 |
| LT_LOOKATOFFSETTIME / EXITTIME | 0.2 / 0.5 | BlendIn/BlendOut（Node 字段或 RotationTransitionSeconds） | 语义近似：由作者时长承载 |
| LT_BOSS_POLAR_OFFSET_X | (60°, 90°) | 无对应（工程无极角区间概念） | **未还原** |
| LT_BOSS_POLAR_OFFSET_Y | (0.5, 0.7) | offset = 屏幕偏移插值（Main/SubHorizontalOffset） | 部分近似：屏幕 Y 偏移区间未参数化 |
| LT_BOSS_POLARCURVE / DISBOSS_POLARCURVE | 3 键曲线 | 无对应（工程用线性 Lerp） | **未还原** |
| LT_POLARLERPRATIO / POLARLERPTIME | 0.5 / 2s | pivot = Lerp(main, secondary, HeightRatio) | **差异**：来源按时间 2 秒 Lerp 极角，工程按目标比率即时插值 |
| maxLookAtRelativeHeight / Cancel 曲线 | 高度限制曲线 | 无对应 | **未还原** |
| cancelTimeThreshold | 0 | 目标失效按 TargetInvalid 即时退出 | 语义一致（阈值为 0 即即时） |
| elevationInterpToSpeed | 5 | History 连续平滑（非速度参数化） | 实现方式不同 |
| activateRange | true | radius = max(LocateRadius, fit) | 语义一致 |

## 结论

1. 工程几何分支（包围盒中心 + FOV 半角拟合）是通用合法实现，继续保留。
2. ZZZ 的极角区间 + 曲线驱动 + 2 秒 Lerp 是**未还原的来源行为**；若要还原属新实现任务（极角取景 stage），不在本 change 内擅自重写现有公式。
3. 工程缺 LT_MAX_RANGE 类最大距离上限；补与不补属 3C 自有行为决策，未擅改。