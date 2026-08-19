# Change: 补齐并接通完整预测式 Foot IK

## 当前实施状态

本 change 仍是完整预测式 Foot IK 的目标设计，不代表整条 IK 已经验收。当前代码已经收口 Landing Event 实时更新合同，并接入每脚唯一最终 Goal 换代：Landing、Ground Path 与 Envelope 不再冻结旧踏面，Foot Goal 只保存相对同帧原生踝骨的 Committed/Pending 修正并向最新目标连续收敛。Goal 换代诊断已接入采样 CSV，并完成一次直线楼梯自动采样；该样本报告 117 个闭环失败帧、最大踝骨残差 0.06，故不能作为方案二通过证据。Ground Envelope、Swing Foot Motion、Goal 换代后的物理踝骨、Pelvis、支撑锁脚、脚掌朝向、Pivot、FullBodyIK 消费和最终 Physical Bone 写入仍待后续逐步验证。

## Why

当前 Foot Placement 已经有预测落点、可达 Ground Path、摆动脚包络增量和唯一 FBBIK，但楼梯上仍看不出完整预测 IK。原因不是“有没有画线”，而是同一次迈步的落点、步伐骨盆、支撑锁脚、脚掌朝向和转向没有接到同一个 Goal 链。

当前实现还有四个会让后续阶段假过的结构缺口：每只脚会分别查询 Current 与 Incoming Step；完成帧用当帧新查询而不是本事件最后 Accepted 落点晋级 `LastLanding`；骨盆 Pending 弹簧没有从 Committed 状态开始；有限 Action 占脚和跑步关闭朝向没有接到已有正式输入。本 change 同时修正这些前置合同，不把错误上游留给锁脚和 Pivot 遮盖。

GDC《Fitting the World》的完整顺序是：自动脚步数据 -> 预测接触时间和位置 -> Ground Path / Reachability / Envelope -> 动画跑在 Foot Path 上方 -> Locked / Sliding / Unlocked -> 支撑腿与髋部弹簧 -> 脚朝向与接触脚 Pivot -> IK Solver。Shadow《预测脚步IK》的顺序是：准备数据 -> 预测落点 -> 计算盆骨 -> 计算脚路径 -> 应用脚和盆骨。项目已经有前三段数据基础，本 change 收口后半段，但不建立旁路。

`add-character-foot-placement-stride-hips` 中的步伐骨盆代码并入本 change，不再按旧四步单独验收。

## What Changes

- 同一 `LandingEventIdentity` 内，每帧合法 SphereCast 都代表当前世界事实。新命中相对当前 Accepted 落点小于 `LandingUpdateDistance` 时复用当前落点与 Path；超过死区时无论 Surface 或高度是否变化，都替换唯一 `NextSwingLanding` 并重建唯一 Ground Path。查询失败或没有合法候选时当前 Path 必须 Rejected，不能继续显示旧踏面；生命周期只保留最后有效落点供事件完成晋级，不再把它发布成当前 Path 输入。
- 每只脚先从 Current / Incoming Step 选出唯一查询事件，再执行至多一次正式 Landing SphereCast。完成事件只把该事件最后一个 Accepted `NextSwingLanding` 原值晋级为 `LastLanding`，不得为晋级再查询一次地面。
- 当前步伐由支撑脚 `LastLanding` 到摆动脚 `NextSwingLanding` 构成。骨盆使用已有 `PelvisPreSolveTranslation`，按 Pose Root 在步伐水平轴上的进度采样；必要位移和弹簧输出先在步伐起点坐标系中重基，再合成唯一骨盆 Goal，避免支撑切换带着旧步伐的相对高度回弹。
- 摆动脚继续只消费 Ground Envelope 相对落点基线的非负垂直增量，水平进度和原生动作仍由动画提供。
- 支撑脚拥有 `LastLanding` 且不是当前 Swing 时，先用非负 plantHeight 接地，再进入 Locked / Sliding / Unlocked。锁入准备只消费同一事件的 `TimeToLandingSeconds`；Locked / Sliding 即使垂直和水平误差为零也保持动画位置权重；Unlocked 冻结上一提交修正和权重作为释放起点，在正式 `UnlockBlendSeconds` 内连续回到原生动画。
- 每只脚的最终 Goal 在写入唯一 GoalSet 前经过一份 Pending/Committed 换代状态。状态只保存上一成功帧输出相对同帧原生动画踝骨的 Component 空间修正与权重；当前 Landing、Path 或 Envelope 可以立即换代，最终修正按正式 `GoalTransitionHalfLifeSeconds` 向本帧最新目标或零修正收敛。换代途中目标再次变化时直接以上一成功输出为新起点，不缓存第二条 Path，不平滑世界落点。离地或有限 Action 占脚属于所有权硬失效，必须当帧清零，不能让换代继续携带旧地面修正。
- 支撑脚只在 Locked / Sliding 时发布有限 Pitch / Roll：移动时使用revision后步伐前向，GroundedStationary没有步伐时使用同一Pending RevisionForward；上坡更趋水平，下坡更趋坡面，跑步达到阈值时关闭。摆动脚旋转权重保持为零。
- 有效可见 yaw 先启动 Foot Placement 唯一 trajectory revision。该 revision 拥有独立 identity、Pending/Committed 虚拟 Body Position、Rotation、Forward 和剩余 yaw；先用当前未改写的 Visible Position相对上一提交Visible Position的世界位移推进Committed虚体，再以稳定主支撑 `LastLanding` 为 Pivot，按`VirtualBodyPosition = LastLanding + RotateAroundUp(VisiblePositionForRevision - LastLanding, pivotDelta)`与`VirtualBodyRotation = pivotRotation * CommittedRevisionRotation`建立本帧虚体，最后按`RawLanding = VirtualBodyPosition + FutureBodyTranslationWorld + VirtualBodyRotation * RootLocalLanding`执行当前唯一落点 SphereCast、Capsule、Reachability、Hull 和 Envelope。之后才判定步伐、骨盆和脚 Goal。不刚体旋转旧 Foot Route、Surface 或 Ground Envelope，不改 KCC、Gameplay Body 或 VisualRoot。
- `GroundedStationary` 两脚都有合法 `LastLanding` 时延续上一提交主支撑；旧主支撑失效时才按较小水平误差、再按稳定 Side 顺序重选。Pivot 主脚保持 Locked，另一脚重新进入 Locked / Sliding / Unlocked，Sole 前后关系不得决定主支撑。
- 有限 Action 占脚只读取同帧 Pose Contribution 中非零 `SourceActionInstanceId` 与对应左右脚权重；跑步关闭朝向只读取 `CharacterPresentationFactFrame.HorizontalSpeed`。不得再造第二 Action、速度或状态来源。
- 删除“盆骨与支撑脚必须零权重”“预测误差降低 Goal 权重”等旧阶段边界，改用正式的落点踏面合同、锁脚阈值、朝向限制和轨迹 revision。
- 诊断与 CSV 增加事件踏面、轨迹 revision、锁脚状态、水平误差、锁入/解锁时间、朝向、步伐和骨盆字段，并对账唯一 FullBodyIK 与 Physical Bone 写入。

## Impact

- Affected specs: `character-foot-placement-presentation`
- 对照但不改 ABI：`character-presentation-pose-graph`、`character-animation-foot-analysis-artifact`、`character-vertical-body-motion`
- Affected code: Foot Placement Runtime、Landing 缓存、唯一 trajectory revision、步伐骨盆模块、锁脚/朝向纯计算、Profile、diagnostics、Gizmo、CSV
- 不修改 Pose Graph 拓扑、Goal ABI、FBBIK 实现、KCC、Gameplay Body、VisualRoot、网络状态
- `add-character-foot-placement-stride-hips` 的有效设计与已写代码并入本 change；其 active change 文档删除，不再独立 apply、archive 或按旧四步验收
- `add-discrete-stair-presentation` 描述的 FootGrounding / Predictive Modifier / Body VisualRoot 与本 change 冲突，不得并行接入

## Dependency

建立在已归档的 Ground Path 与 Swing Foot Motion，以及工作区已有、尚未完整验收的步伐骨盆代码之上。必须直接消费 `LastLanding`、`NextSwingLanding`、Accepted Ground Envelope 和唯一 GoalSet，不复制落点、包络或骨骼 writer。

## Current Spec Comparison

- current `character-foot-placement-presentation` 仍要求只生成 Swing 脚垂直 Goal、Pelvis 与支撑脚权重为零，并禁止 Foot Lock、Pelvis、脚底旋转。本 change 删除这条阶段边界，安装完整步伐骨盆、支撑锁脚、朝向和 Pivot revision。
- current `Landing Prediction必须形成独立世界事实` 把 Raw Landing 固定为 `VisiblePosition + FutureBodyTranslation + VisibleRotation * RootLocalLanding`，并禁止任何未来朝向 Plan。本 change 明确修改该 Requirement：先用当前Visible世界位移推进上一提交虚体，再把该临时Pose绕稳定 `LastLanding` 重投影成唯一 revision Pose，由它计算Raw Landing；这不是 Gameplay 或 KCC 的未来朝向。Future Body Translation 仍保持原世界向量且不随 yaw 旋转，旧 Route、Surface 与 Envelope 也不参与该几何计算。
- current 同一 Landing Event 只按距离死区复用或重建 Path。本 change 保留这一实时合同，并明确 SurfaceIdentity 或高度变化不是冻结条件；不可走和当前查询失败只由 Ground Path typed rejection 表达。
- current Swing 阶段明确禁止跨帧 Goal 平滑。本 change 用唯一最终 Goal 换代替换该阶段边界：世界事实仍实时更新，只有写给骨骼的相对修正与权重连续收敛。
- current 诊断不显示 Pelvis。本 change 增加步伐线、骨盆标记和同一 Completion 的 Physical Pelvis 对账。
- current `character-presentation-pose-graph` 已要求 Foot Placement 输出 Pelvis 与双脚 Goal、唯一 FBBIK；本 change 只让已有 slot 成为有效目标，不改端口类型。
- current `character-vertical-body-motion` 拥有 Gameplay 垂直积分和 KCC Grounded。本 change 不读取或改写 VerticalVelocity，不用骨盆补 KCC 没上台阶。
- current `character-animation-foot-analysis-artifact` 继续只发布 root-local 脚部事实。本 change 不把分析改成锁脚状态机，只消费 TimeToLanding、IsSwing 和 Landing Event。
- `openspec/project.md` 本轮已删除“误差软阈值降权、硬阈值释放”的旧表述，并明确当前只安装Swing Foot Motion；归档本 change 时还必须把实时唯一 Path、最终 Goal 换代和唯一trajectory revision的实现证据收口到current truth。
- `add-character-foot-placement-stride-hips` 的 delta 尚未进入 current spec，且其代码仍未接入 Runtime Goal 组装。本 change 吸收其有效内容并删除独立 active 文档，归档时只合并本 change。

## Reference Alignment

- GDC 第 4–11 页：每脚预测接触时间和位置；脚的向前运动来自动画；最终脚不得低于 Foot Path。
- GDC 第 13–16 页：Locked 锁位置但允许旋转，Sliding 允许小幅滑动，Unlocked 在误差过大时解除。
- GDC 第 17 页：支撑腿决定髋部，上下坡使用不同处理，必要位移直接应用，临界阻尼弹簧消化支撑切换；原文没有给出具体弹簧公式，项目在 design 中补齐输入和坐标重基。
- GDC 第 19 页：根据移动方向限制 Pitch / Roll；上坡更水平，下坡更贴坡；跑步关闭。
- GDC 第 21–28 页：转向枢轴靠近接触脚。项目让唯一虚拟 Body Pose先跟随当前Visible世界位移，再绕稳定接触脚重投影；双脚站住时延续上一提交主支撑，失效后才稳定重选，不转胶囊、不旋转旧 Route。
- GDC 第 29–36 页：两点正确不等于中间安全；Capsule、排序、Edge、Reachability 和上侧 Hull 已存在，本 change 只消费其 Accepted Envelope。
- Shadow：先算盆骨再应用脚；步伐是支撑落点到摆动预测点；Foot Path 是相对地面基线的增量；落地脚暂时收到落点。`Set Mesh` 是 UE 方案，项目使用 `PelvisPreSolveTranslation`。
- 依卞：落点用 Sphere 减少台阶边缘误命中；预测落点在落地前继续更新。SurfaceIdentity 与高度只进入诊断和 Path 几何，不再阻止当前合法预测替换旧落点。

## 实施边界

本 change 的完成定义不是“生成了落点线”，而是同一表现帧内以下事实全部对上：

```text
原生 Component Pose
-> Foot Placement Pending Goal Set
-> PelvisPreSolveTranslation
-> 唯一 FullBodyIK
-> 唯一 Final Writer
-> Physical Pelvis / Ankle
```

每只脚仍只有一个 `LastLanding`、一个当前可用的 `NextSwingLanding`、一个 Ground Path、一个 Envelope 和一个最终 Goal 换代状态。所有新增的换代、锁脚、骨盆、弹簧和 trajectory revision 状态都进入 Foot Placement Pending/Committed 页，外层 `Seal` 才推进，`Discard` 或 Fault 不得留下半帧结果。

### 明确采用

- 同一事件内始终接受本帧最新合法预测；只在 `LandingUpdateDistance` 死区内复用，SurfaceIdentity 与高度变化不得冻结旧 Path。
- 最终脚 Goal 只平滑相对原生踝骨的修正与权重，不平滑 Landing、Surface、Ground Path 或 Envelope。
- 转向建立唯一 trajectory revision，重新查询和重建地形事实，不刚体旋转旧 Path。
- revision 先旋转虚拟 Body Position 与 Rotation，再计算 Raw Landing；KCC Future Body Translation 保持原世界向量。
- 双脚站立延续稳定主支撑，Pivot 主脚锁住，另一脚允许滑动或释放。
- 支撑脚非负接地、三态锁脚、平地也保持有效锁脚 Position Weight。
- 锁入使用 Landing Event 时间，解锁从上一提交 Goal 连续释放，不把目标一帧切回原生动画。
- 骨盆相对高度在支撑切换时重基，再进入临界阻尼弹簧。
- 所有结果进入同一 GoalSet、一次 FullBodyIK 和一次 final writer。

### 明确不采用

- 不用预测误差降低 Position Weight，不冻结实时落点，不用误差阈值伪造换级。
- 不用 Set Mesh、VisualRoot、KCC 或 Gameplay Body 实现骨盆。
- 不增加第二脚下 Trace、第二 Grounding、第二 IK 或第二物理骨骼 writer。
- 不在攻击、跳跃、空中帧旁路接传统 IK；无合同就发布零权重。
- 不把 Virtual Ground、专用上下楼动画、跑步上下楼特化或实体绕支撑脚转向塞进本 change。

## 归档前置条件

归档前必须完成 `verification.md` 的七个阶段，并且每个阶段同时满足 Scene 观察和同一 Completion 的 CSV 对账。当前已有采样只能证明摆动脚的 `FinalGoalPositionWeight`、`FinalIkSucceeded`、最终物理踝骨残差和 Ground Path 没有 Invalid Segment，不能替代骨盆、锁脚、朝向或 Pivot revision 的端到端证据。未完成这些证据时，不得把本 change 标记为完成，也不得提前合并 spec delta。
