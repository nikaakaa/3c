# 2026-09-01 近地旋转采用实验（拒绝）

## 单变量

保留当前 Foot 位置、Contact Anchor、Residual、Correction Response、查询、Pelvis 与 Solver 链，只把旋转权重从 `FormalFootPlacementWeight * LockWeight` 改为：

```text
heightWeight = clamp01((0.18 - ownerLocalFootPivotY) / (0.18 - 0.08))
rotationWeight = FormalFootPlacementWeight * heightWeight
```

`0.08 / 0.18` 来自已恢复的 ZZZ Corin 实例。该实验只验证近地旋转采用，不代表 ZZZ 完整 Foot 求解。

## 对照

- 基线：`Diagnostics/FootPlacementRuns/20260901-111208-798-b1a8446ff183468d9eb63f531a00f08d`
- 候选：`Diagnostics/FootPlacementRuns/20260901-122052-732-2960e8c0f0c04f75980af233629242f3`
- 输入：`43357ff3cd384e5cba75d2c31175b116`，1044 Fixed 帧
- Proof：逐帧输入与 Body 无分歧，`DivergentFrameCount=0`；Program 与 Projection 身份因实验代码变化而不同
- 采样：两包均 1043 输出帧、2086 脚行

## 结果

| 指标 | 基线 | 候选 |
| --- | ---: | ---: |
| 七维参考分 | 84.2 | 83.1 |
| 接触平面穿透事件 | 19/84 | 10/84 |
| 穿透最大深度 P90 | 23.202 mm | 16.999 mm |
| Toe 最大深度 P90 | 23.202 mm | 16.999 mm |
| 最大穿透 | 142.815 mm | 142.947 mm |
| 接触未贴合 | 13/60 | 18/60 |
| Stable Swing 跳变 | 143/344 | 156/344 |
| Path Revision 跳变 | 199/667 | 226/667 |
| Release 反向证据 | 1/59 | 10/59 |
| Landing 腿姿态 | 26/58 | 27/58 |

高度权重在 2086 行全部执行，公式最大误差 `1.256e-7`；985 行旋转权重高于基线。动画 Sole、Current Support、Selected Target 与 Response Output 逐行不变。Final Goal 位置最大变化 70.159 mm，Pelvis 最大变化 18.323 mm，Solved Knee 最大变化 78.676 mm。

原 Toe 穿透靶点得到改善：Left357 的 Toe 深度 `11.854 -> 3.011 mm`，Left358 `25.842 -> 18.158 mm`；Left861/862 同型。但没有消除全部穿透，最差值也没有改善。

## 失败原因

旋转围绕当前 Sole 中心反解 Ankle。中心已经到接触面时，提前放平会同时缩小 Heel/Toe 的正负高度差，因此能减少 Toe 穿透。

Landing 初期中心仍被世界 Residual 保留在接触面上方时，动画倾斜原本让一端更接近地面；提前放平会把这一端抬高，使整脚最小间隙变大。Left422 的两端间隙从 `124.384 / 148.830 mm` 变为 `136.950 / 136.263 mm`，连续超 1 cm 从 83.3 ms 变为 100 ms，直接新增持续踏空命中。Left602 与 Left782 同型。

因此，单独按动画 Foot Pivot 高度采用旋转，把“中心位置接管”和“足底朝向接管”拆成了不协调的时序：Toe 穿透下降，但 Landing 踏空、Swing/Path 时序与下游 Knee/Pelvis 均出现回归。

## 裁决

拒绝并撤销本候选，保留完整候选采样与 Proof。后续若继续研究旋转，必须让采用时机同时看到实际 Footprint 相对 Contact Plane 的阶段，不能在中心 Residual 尚高时无条件抬起当前最低端；也不能用平移硬贴地掩盖交接连续性。
