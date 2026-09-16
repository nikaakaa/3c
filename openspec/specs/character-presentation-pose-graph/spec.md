# character-presentation-pose-graph Specification

## Purpose

定义 Character Presentation Pose Graph 的作者数据、资源绑定、原生 FlowCanvas 运行入口、ScenePlay 观察和 Live Debug 边界。Pose Graph 是表现领域的正式图，不是 Character Gameplay Program 的下游编译产物。

## Requirements

### Requirement: Pose Graph 是表现领域唯一作者拓扑

`CharacterAnimationPresentationProfile` MUST 通过正式引用装配 Pose Graph、PoseStateMachine、state-local source、AnimationSlot、Rig、Mask、Blend/Inertialization Policy、Foot Placement、PoseBoneIKGoals、FullBodyIK 和 Final Publication。Definition、Ability、Timeline、Control 或独立 EditorWindow MUST 不保存这些配置的可写副本。

#### Scenario: 打开 Pose Graph

- **WHEN** 作者从正式 Profile 打开 Pose Graph
- **THEN** 窗口 MUST 读取该 Profile 和 Graph 的正式 binding
- **AND** MUST 不从旧 Program、Projection 或场景对象猜测节点、Rig 或资源

### Requirement: Pose Graph 使用原生 FlowCanvas 数据和实例

Pose Graph MUST 使用正式 Graph、Node、Connection、typed port 和稳定 authoring identity。运行时由表现宿主创建 Actor-local FlowCanvas Graph instance 并 Manual 驱动；共享资产 MUST 不保存运行状态、播放时间、IK 历史或窗口状态。

#### Scenario: 同一 Graph 多角色使用

- **WHEN** 两个 Actor 绑定同一个 Pose Graph
- **THEN** 两个 Actor MUST 分别创建图实例
- **AND** 节点状态、Source、Constraint、Final Pose 和 Tuning MUST 完全隔离

### Requirement: 持续 Locomotion 与有限 Action 分工明确

持续 Locomotion MUST 由 committed Body/Intent 形成 Presentation Fact，再由 PoseStateMachine 选择 state-local source。有限 Action MUST 通过正式 Action playback 和 AnimationSlot 进入 Pose 流。Pose Graph MUST 不拥有 Gameplay 准入、Timeline 窗口、MotionWarp 规则、命中结果或 World 真值。

#### Scenario: 移动没有 Action

- **WHEN** Actor 只有 Body/Intent，没有有限 Action playback
- **THEN** Pose Graph MUST 只处理 Locomotion Fact 和 state-local source
- **AND** MUST 不创建空 Ability、空 Timeline 或默认 Action producer

### Requirement: 作者修改必须经过正式 Mutation

Graph、Node、Connection、Slot、Mask、Profile、Rig、Player 和 Policy 的修改 MUST 经过领域 Capability、typed Mutation、Validator、Undo 和 revision。窗口布局、Selection、Preview Camera 和 Pose Watch 只属于 window-local state，不得写入运行资产。

#### Scenario: 无效连接

- **WHEN** 作者创建不兼容的 Pose port connection
- **THEN** Mutation MUST 拒绝并报告 source/target port identity
- **AND** MUST 不保存半条边或隐式转换节点

### Requirement: ScenePlay 是唯一 Pose 预览入口

Pose Graph 页面 MAY 提供作者编辑、目标选择和只读观察按钮，但正式运行 MUST 由 ScenePlay 加载场景、创建 Session/Actor、接收输入并驱动原生图。页面 MUST 不拥有 Preview evaluator、独立时钟、Fact Fixture、Query Fixture、临时 PlayableGraph 或第二套 Pose Runtime。

#### Scenario: 观察真实 Actor

- **WHEN** ScenePlay 已发布 Actor 的 Pose binding 和 Committed Result
- **THEN** Pose 页面 MUST 显示该 Actor 的正式 Pose/Source/Constraint 事实
- **AND** 页面 MUST 不重新采样、重建或修改 Actor Runtime

### Requirement: Live Debug 只能显示正式事实

Live Debug MUST 通过 ScenePlay/Runtime Debug binding 显示 PoseState、Source、Transition、Slot、Constraint、Goal、FBBIK、Final Publication 和 completion trace。不得从 Animancer weight、Transform 或当前作者游标反推出第二份事实。
