# Change: 补齐并接通完整预测式 Foot IK

## Why

当前 Foot Placement 已经有预测落点、可达 Ground Path、摆动脚包络增量和唯一 FBBIK。上楼仍然看不出预测 IK，是因为这条链只做完了参考文章的中间两段，而且同一次迈步里下一落点会换台阶。人眼判断「有没有 IK」看的是：落点球钉在哪一级、人有没有站起来、脚还切不切踢面、站脚还滑不滑。这些对应 GDC 和 Shadow 后半段，不是 CSV 里踝骨相对动画多了 10cm。

GDC《Fitting the World》把整套方法还原为：自动脚步数据 → 预测接触时间和位置 → 两次接触之间采集地形 → Ground Path / Reachability / Envelope → 动画跑在 Foot Path 上方 → Locked / Sliding / Unlocked → 支撑腿与髋部弹簧 → 脚朝向与接触脚 Pivot → IK Solver 落实。Shadow《预测脚步IK》的总顺序是：准备数据 → 预测落点 → 计算盆骨 → 计算脚路径 → 应用脚和盆骨。项目已归档前三段的数据层，缺的是落点在同一次迈步里的稳定合同，以及盆骨、锁脚、朝向、转向枢轴接到同一 Goal 链。

`add-character-foot-placement-stride-hips` 已经按步伐写下了盆骨和支撑脚垂直目标的代码与 spec delta，但 current spec 仍要求盆骨和支撑脚权重为零，Runtime 也仍按旧阶段边界关掉它们。该 change 不再作为独立实施边界。本 change 吸收它的步伐合同，并按参考文章把后半段一次写完整。

## What Changes

- 同一 `LandingEventIdentity` 内，下一落点只允许在同一级踏面上跟随权威预测滑动。换级判定是 `SurfaceIdentity` 不同，或沿 Component Up 的高度差超过 Profile 显式 `MaximumSameEventVerticalJump`。整段楼梯共用一个碰撞表面时，只比 SurfaceIdentity 不够。迈步结束才换踏面。不再只用毫米死区决定「整条 Path 重建还是冻死」。
- 接通已有步伐合同：支撑脚 `LastLanding` 到摆动脚 `NextSwingLanding` 构成当前步伐。盆骨走已有 `PelvisPreSolveTranslation`，主目标按 Pose Root 在步伐水平轴上的进度采样，上坡落地后抬、下坡接触时降，临界弹簧只消化支撑切换，二次净空下限用同帧原生动画盆骨到更低修正脚的距离。
- 摆动脚继续只吃 Ground Envelope 相对落点基线的非负垂直增量，水平与旋转仍来自动画。
- 支撑脚在拥有 `LastLanding` 且不是当前 Swing 时，先做垂直接地，再进入 Locked / Sliding / Unlocked。Locked / Sliding 期间该脚停止采样 Envelope。水平误差按 Profile 公式插值或解锁；解锁混合时间用显式 `UnlockBlendSeconds`，锁入混合时间用该脚 `TimeToLandingSeconds`。空中、无权威 Step、有限 Action 占用该脚时三 Goal 权重为零。
- 支撑脚在 Locked / Sliding 时按 GDC 第 19 页写有限 Pitch / Roll：上坡更趋于水平，下坡更趋于坡面，跑步关闭。Rotation Weight 只在这个合同里非零。
- 转向时不把角色胶囊绕脚转。摆动脚路径相对当前支撑脚 `LastLanding` 旋转，使接触脚位移更小。实体 Origin 和 KCC 仍绕自己转。
- 删除「盆骨与支撑脚必须零权重」「禁止锁脚 / 朝向 / 约束」「预测误差降低 Goal 权重」这些阶段边界。Profile 删除仍残留的预测误差淡出距离，改为显式锁脚阈值、朝向限制和跑步关闭条件。
- 只读摘要、CSV 与已有 Scene Gizmo 增加表面身份、锁脚状态、朝向、步伐线和盆骨标记。Gizmo 不显示文字，不重算。

## Impact

- Affected specs: `character-foot-placement-presentation`
- 对照但不改 ABI：`character-presentation-pose-graph`、`character-animation-foot-analysis-artifact`、`character-vertical-body-motion`
- Affected code: Foot Placement Runtime、Landing 缓存、步伐盆骨模块、锁脚 / 朝向纯计算、Profile、diagnostics、Gizmo、CSV
- 不修改 Pose Graph 拓扑、Goal ABI、FBBIK 实现、KCC、Gameplay Body、VisualRoot、网络状态
- `add-character-foot-placement-stride-hips` 并入本 change，不再单独 apply 或按旧四步验收
- `add-discrete-stair-presentation` 仍描述已删除的 FootGrounding / Predictive Modifier，并把离散台阶做到 Body VisualRoot。本 change 不走那条路；它若继续必须先改写，不得与唯一 Foot Placement 链并行

## Dependency

建立在已归档的 Ground Path 与 Swing Foot Motion，以及工作区已有、尚未接通的步伐盆骨代码之上。必须直接消费 `LastLanding`、`NextSwingLanding`、Accepted Ground Envelope 和唯一 GoalSet，不得复制落点、包络或第二骨骼写入。

## Current Spec Comparison

- current `character-foot-placement-presentation` 要求「当前阶段必须只生成 Swing 脚垂直 Goal」，Pelvis 与支撑脚权重必须为零，并禁止叠加 Foot Lock、Constraint、Pelvis、脚底旋转。本 change 删除这条阶段边界，改为完整预测 IK 的步伐盆骨、锁脚、朝向与转向路径。
- current 同一文件要求同一 Landing Event 用距离死区决定复用或重建 Path，且「预测点漂移不得降低 Position Weight」。死区复用保留；本 change 增加「同一事件不得换级」，换级看 SurfaceIdentity 或垂直跳变，并继续禁止用预测误差改权重。不可走仍只由 Ground Path typed rejection 表达。
- current 诊断要求「不得显示 Pelvis 结果」。本 change 改为显示步伐线和盆骨标记，因为参考文章用这些线验收盆骨，不是用 CSV。
- current `character-presentation-pose-graph` 已要求 Foot Placement 输出 Pelvis 与双脚 Goal、唯一 FBBIK 一次求解。本 change 只让已有 slot 变成有效目标，并允许支撑脚在朝向合同成立时使用旋转权重。不改端口类型。
- current `character-vertical-body-motion` 拥有 Gameplay 垂直积分和 KCC Grounded。本 change 不得读取或改写 VerticalVelocity，不得用盆骨补偿 KCC 没上台阶。
- current `character-animation-foot-analysis-artifact` 继续只发布 root-local 脚部事实。本 change 不把分析改成锁脚状态机，只消费已有 TimeToLanding、IsSwing 和 Landing Event。
- `openspec/project.md` Presentation 段仍写「误差从软阈值平滑降低 Foot Goal 约束并在硬阈值释放」。这与 current spec「预测漂移不降权重」矛盾，归档本 change 时必须改成：同事件同表面滑动、换表面才换踏面、不可走只走 typed rejection。
- `add-character-foot-placement-stride-hips` 的 spec delta 尚未进入 current spec。本 change 覆盖并扩展该 delta，归档时只合并本 change。
- `add-discrete-stair-presentation` 与本 change 冲突：它把台阶连续感放进 Body VisualRoot，并点名旧 Grounding。不得把它当成预测 IK 的下一步。

## Reference Alignment

- GDC 2016 第 4–11 页：每脚预测接触时间和位置；脚向前来自动画；最终脚不得低于 Foot Path。
- GDC 第 13–16 页：Locked 锁位置允许旋转，Sliding 小幅滑动，Unlocked 误差过大时解除。
- GDC 第 17 页：支撑腿决定髋部，上下坡不同处理，必要位移一次加上，弹簧去弹跳；禁止每帧跟最低脚、最高脚或两脚平均。
- GDC 第 19 页：按移动方向限制 Pitch / Roll；上坡更水平，下坡更贴坡；跑步关闭。
- GDC 第 21–28 页：转向枢轴靠近接触脚。项目用摆动路径绕支撑落点旋转表达，不转胶囊。
- GDC 第 29–36 页：两点正确不等于中间安全；Capsule 采集、排序、Edge、Reachability、上侧 Hull。已归档，本 change 只消费。
- Shadow：先算盆骨再应用脚；步伐是支撑落点到摆动预测点；脚吃 Path 相对基线的增量；踩实后收到落点。`Set Mesh` 是 UE 做法，项目用 `PelvisPreSolveTranslation`。
- 依卞：落点用 Sphere，不用细线，避免台阶边缘滑级；落地后继续更新终点，但本项目把「更新」限制在同一表面身份内。

## 实施边界

本 change 的完成定义不是“生成了落点线”，而是同一表现帧内以下事实都能对上：

```text
原生 Component Pose
-> Foot Placement Pending Goal Set
-> PelvisPreSolveTranslation
-> 唯一 FullBodyIK
-> 唯一 Final Writer
-> Physical Pelvis / Ankle
```

每只脚的预测数据仍只有一个 `LastLanding`、一个 `NextSwingLanding`、一个 Ground Path 和一个 Envelope。步伐模块只消费这四类事实；锁脚、朝向和转向不另建查询器。所有新状态都进入 Foot Placement 的 Pending/Committed 页，外层 `Seal` 才能推进，`Discard` 或 Fault 不得留下半帧结果。

### 明确采用的实现

- 用同一 `LandingEventIdentity` 和稳定 `SurfaceIdentity` 管住预测落点；同一踏面可以实时滑动，换踏面必须等事件完成。
- 用支撑落点到摆动预测点的一条步伐线计算骨盆，并把必要位移、弹簧输出和同帧净空下限合成唯一 `PelvisPreSolveTranslation`。
- 支撑脚先做非负垂直接地，再按水平误差进入 `Locked`、`Sliding`、`Unlocked`；锁入只使用原有 `TimeToLandingSeconds` 时基，解锁使用正式 `UnlockBlendSeconds`。
- 支撑脚只在锁脚状态下发布受限 Pitch/Roll；摆动脚不发布旋转 Goal，跑步达到阈值时关闭支撑脚朝向。
- 转向只旋转摆动落点和 Envelope 相对支撑落点的路径，不改 KCC、Gameplay Body 或 VisualRoot。

### 明确不采用的实现

- 不用预测误差降低 Position Weight，也不把误差硬阈值当成整条 Path 的冻结开关；不可走只由 Ground Path 的 typed rejection 表达。
- 不用 `Set Mesh`、VisualRoot 平移或 KCC 垂直补偿实现骨盆；它们会把 Presentation 结果写进另一所有权。
- 不增加第二个脚下 Trace、第二个 Grounding、第二个 IK Solver 或第二个物理骨骼 writer。
- 不在攻击、跳跃、空中帧旁路接传统 IK；无有效 Foot Placement 合同时三类 Goal 归零。
- 不把楼梯专用动画、步距缩短、Virtual Ground 或实体绕支撑脚旋转塞进本 change；这些会改变业务输入或 Body 所有权，另立 change。

## 归档前置条件

归档前必须完成 `verification.md` 的七个阶段，并且每个阶段都同时满足 Scene 观察和同一 `Completion` 的 CSV 对账。当前已有采样只能证明摆动脚的 `FinalGoalPositionWeight`、`FinalIkSucceeded`、最终物理踝骨残差和 Ground Path 没有 Invalid Segment；不能替代骨盆、锁脚、朝向或 Pivot 的端到端证据。未完成这些证据时，不能把本 change 标记为完成，也不能把本 change 的 spec delta 提前合并进 current spec。
