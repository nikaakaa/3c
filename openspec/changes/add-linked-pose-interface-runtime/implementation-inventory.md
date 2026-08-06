# Linked Pose 实施入口与所有权清单

## 当前正式入口

- Presentation 作者配置：`CharacterAnimationPresentationProfile`。它唯一引用 root Pose Graph、Rig、Motion Matching、FullBodyIK Profile、producer binding 与 pose source binding；Linked Group、Implementation 和 selector 继续挂在该 Profile 的 Projection 输入闭包内。
- Pose 作者图：`CharacterPresentationPoseGraphAsset`。它保存 root graph、静态 PoseSubgraph、StateMachine layout 与 source slot；Linked Implementation Entry 复用同一 typed graph 数据结构和编译能力，不引入运行时图解释器。
- Projection 编译：`CharacterPresentationProjectionCompiler` 调用 `CharacterPresentationPoseGraphCompiler`，把 root graph、source、Rig、blend、Motion Matching、Equipment visual 与 Foot Analysis 编进唯一 `CharacterPresentationProjection`。Linked descriptor、fragment、selector 与最大布局必须进入同一次编译和发布。
- Native Pose runtime：`PosePlanExecutionRuntime` 创建 `CharacterPoseGraphNativeProgram` 与 `CharacterPoseGraphStagedExecutor`，按同一 staged plan 执行并共用一次 Animancer Evaluate Barrier。Linked Entry 只能作为该 plan 的编译片段被 dispatch。
- Equipment 已提交状态：`CharacterSimulationActorRegistration` 从 committed `EquipmentSlotState` 生成 `EquipmentVisualSelection`，`CharacterSimulationPresentationRuntime.CaptureEquipmentSelections` 是 Presentation 侧只读入口。Equipment selector adapter 从同一 committed slot/id/revision 生成通用 `CharacterLinkedPoseSelectionFrame`，Renderer 和 visual runtime 不拥有 Implementation。
- FinalIK：`CharacterFullBodyIkGoalSetHeader` 承担 frame、Rig、completion、producer lineage 与 availability；`FullBodyIK` 是唯一 IK solver。Linked hand goals 只能输出 `component.full-body-ik-goals` 并汇入这一 solver。
- Presentation Fact：`CharacterPresentationFactSchema` 是正式 FactId 与 kind 入口，`CharacterPresentationFactProjector` 构造 `CharacterPresentationFactFrame`。Linked Interface signature 从该有序 schema 确定性派生 Fact contract identity。

## Root graph 唯一所有权

Root graph 保留并唯一拥有以下能力：持续 Locomotion 的 `PoseStateMachine`、有限 Action 的 `ActionPlaybackInput` 与 `AnimationSlot`、显式 `LocalToComponent` / `ComponentToLocal`、`PredictiveFootPlacement`、全部 Goal Set 的汇聚、唯一 `FullBodyIK`、`OutputPose` 与 final publication。

Linked Implementation Entry 只拥有接口声明的局部 Pose 或 Goal 计算。它不得保存 root Profile、Equipment 对象、runtime handle，也不得创建第二个 Slot、Predictive Foot Placement、FullBodyIK、OutputPose、source backend 或 final writer。

## 并行边界

- `replace-pose-ik-with-finalik-full-body-solver` 已确定 Rig v4、统一 GoalSet、completion/lineage 与唯一 FullBodyIK 合同。Linked 核心可以并行实现；只把最终 Hand Goals 接线和 Corin generated Projection 留到 FinalIK 资产收口后执行。
- Linked Entry 复用已经存在的 Sequence、Blend、StateMachine 与静态 PoseSubgraph 能力，不要求尚未闭合的实验 Motion Matching 或 Blend Space 新能力。
- 当前没有正式 Corin `EquipmentSlotId` / `EquipmentId` 业务映射。先交付通用能力、Equipment selector 类型和 Empty 语义，不创建临时 ID、默认实现或猜测映射。
- `CharacterPoseAuthoringBottomDock.cs` 正由 FinalIK 工作修改。本 change 不在该文件新增独立入口；Canvas/Details 接入通过共享 Capability 和现有 presenter 完成。

## 已排除的旧路径

本 change 不建立 Layer catalog、LayerId、运行时动态 Graph、Content/YooAsset 加载、authoring asset 回读、缺失映射 fallback 或上一实现沿用。Linked authoring 只改变 ProjectionRevision，不扩大 gameplay ContractHash。
