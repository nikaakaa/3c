# Change: 启用 Corin Idle 脚底锁定表现

## Why

Corin 当前已经安装 Foot Analysis、PredictiveFootPlacement、FullBodyIK 和移动表面锚点能力，但 `CorinFootPlacementProfile` 的 `LockType` 仍为 `Unlocked`。Idle 绑定虽然全程提供 `Foot Placement Weight = 1`，Foot Analysis 也把左右脚的 `PlantConfidence` 生成为稳定接触，运行时却不会捕获脚的 surface anchor，因此 Idle 中脚底会随动画局部轨迹发生轻微漂移。

这次变更把已有能力用于正式 Corin 内容，不新增第二套 IK、脚相位、动画播放器或 Idle 特判。目标是让上半身和骨盆继续保留 Idle 的自然运动，让 FullBodyIK 通过现有 Foot Placement Goal 保持双脚接地。

## What Changes

- 为 Corin 的正式 Foot Placement Profile 选择 `PivotAroundToe` 作为统一脚锁策略。
- 保持 Corin Idle source 的 `Foot Placement Weight` 全程为 `1`，继续消费现有 Foot Analysis 的左右脚接触特征。
- 让 Corin 内容构建验证拒绝 `Unlocked` 的 Idle 脚锁配置、缺失的 Idle Foot Analysis 或不完整的 Idle 权重曲线。
- 让上述内容验证只挂在显式 Character Build 流程，不在选择资产、Inspector、`OnValidate`、预览或重绘期间执行 Foot Analysis、Projection 或 Program Build。
- Profile revision 改变后只标记 Projection/Program 为 Stale；显式 Character Build 负责原子发布受影响 Projection、Float32/Fixed Target Program 与 Unity wrapper。
- 不重新生成没有输入依赖变化的 Foot Analysis artifact，不复制或保留旧的 Unlocked 兼容路径。

## Scope

### In Scope

- Corin Foot Placement Profile 的锁脚内容策略。
- Corin Idle source binding、Foot Placement Weight 和 Foot Analysis identity 的发布校验。
- 显式 Character Build 的依赖校验、stale 状态和原子发布闭环。
- 与当前 FinalIK Grounding-backed PredictiveFootPlacement 和唯一 FullBodyIK Goal 链的接线确认。

### Out of Scope

- 新增 GASP、UE Foot Placement、Animation Rigging、FinalIK component 或第二个 IK solver。
- 新增按 Idle 状态名分支的 runtime 逻辑。
- 新增 source-local LockType 数据、第二份脚锁 Profile 或通用脚相位数据源。
- 修改 Gameplay Body、KCC、Root Motion、Motion Matching、Timeline、Network snapshot 或网络协议。
- 用重新烘焙 Idle 动画替代运行时脚锁；动画清理只作为后续独立内容工作。

## Current Spec Comparison

- `character-foot-placement-presentation` 已定义 `Free/Locked/Sliding` 生命周期、surface-local anchor、Pelvis planner、显式 Profile、Foot Placement Weight 和移动表面处理。本 change 只安装 Corin 内容策略，不修改这些通用运行语义。
- `character-animation-foot-analysis-artifact` 已定义 Editor-only Foot Analysis artifact、稳定 Plant 特征和 Projection Build 精确消费。本 change 继续复用该产物，不在 Runtime 从 AnimationClip 或 Transform 现场重建特征。
- `character-animation-presentation-authoring` 已定义 source binding、Foot Placement Weight 曲线和“作者变更只造成 Stale、必须显式 Build”的规则。本 change 将该规则作为 Corin 内容验证的硬门禁，不创建自动编译旁路。
- 当前 `character-animation-pipeline` 与 `character-presentation-pose-graph` 仍描述旧的 `FootPlacement + LegIK` 链；active `replace-pose-ik-with-finalik-full-body-solver` 正在把它迁移为 `PredictiveFootPlacement Goal Source + FullBodyIK`。本 change 不重复修改旧架构，实施必须等待该 active change 完成并将 current spec 收口，否则停止发布。
- active 楼梯表现 change 中的 Ground/Future Support、Ground Envelope 和真实 `FootPlacementSurface` 仍属于本 change 的输入边界；不得把 Gameplay Ramp、KCC ground 或第二个 Grounding owner 接入脚锁。

## Dependencies And Sequencing

- 前置依赖 `replace-pose-ik-with-finalik-full-body-solver` 完成 Corin Rig v4、Calibration v4、FinalIK Grounding-backed Goal Source、唯一 FullBodyIK 和 current spec 合并。
- 前置依赖当前 Corin Character Build 能从匹配的 Foot Analysis artifact、Presentation Profile、Rig/Calibration 和 FullBodyIK Profile 发布 exact Projection 与目标 Program。
- Profile 内容变更后只执行显式 Character Build；不得通过选中资产、Inspector callback、Preview session、`OnInspectorGUI` 或 `OnValidate` 触发重分析、编译或发布。
- 如果前置 active change 尚未归档，或生成 Projection 与新 Pose/IK contract 不匹配，本 change 不创建兼容 wrapper、旧 LegIK fallback 或临时迁移器。

## Impact

- 新增 current capability spec：`corin-foot-plant-lock`。
- 修改 Corin Foot Placement Profile、Idle Presentation source binding 和与 Character Build 关联的内容验证。
- 受影响的生成物为 Corin Presentation Projection、请求的 Float32/Fixed Target Program、Pose tuning layout 和 Unity wrapper；Foot Analysis artifact 只有在其输入 identity 变化时才重新生成。
- 不新增 Runtime 状态字段；脚锁状态、surface anchor、Pelvis plan、Goal lineage 和诊断继续由现有 Presentation runtime 唯一拥有。

## Success Criteria

- Corin Idle 正式 Profile 不再是 `Unlocked`，而是 `PivotAroundToe`。
- Idle binding 的 Foot Placement Weight 在整个 source 区间保持 `1`，并引用当前匹配的 Foot Analysis identity。
- 显式 Build 能拒绝缺失 Idle、缺失分析特征、权重不完整、Profile/Projection revision 不匹配或旧 IK contract 的输入。
- 选择资产、Inspector、预览和重绘不会执行 Foot Analysis、Projection、Program 或 Unity Build。
- Build 成功时 Projection、目标 Program、tuning layout 和 wrapper 以同一发布组更新；失败时不产生半套新产物。
- Runtime 继续沿唯一 `Presentation Fact -> PoseStateMachine -> source -> Component Pose -> PredictiveFootPlacement Goals -> FullBodyIK -> OutputPose` 链运行，不出现第二个 IK 或脚锁路径。
