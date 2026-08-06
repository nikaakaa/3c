## ADDED Requirements

### Requirement: Pose Graph 必须使用 LinkedPoseCall 表达唯一动态子图边界

Pose Graph MAY 使用 `LinkedPoseCall` 在 root typed DAG 中调用受限 Linked Pose Interface Entry。节点 MUST 保存稳定 Group、Interface 与 Entry identity，fixed/dynamic ports MUST 由 Interface Capability 精确投影。Root graph MUST 继续唯一拥有 ActionPlaybackInput、AnimationSlot、world-aware Foot Placement、唯一 FullBodyIK、显式 Pose 空间转换、OutputPose 与 final writer；Implementation Entry MUST 不得拥有这些节点。现有 `PoseSubgraph` MUST 继续静态展开，MUST 不被改造成运行时替换节点。

第一版每个 required `Group + Entry` 在 root graph 中 MUST 恰好存在一个 `LinkedPoseCall`。重复 Call 与缺失 Call MUST 在 Validator 与 Projection Build 阶段拒绝。

#### Scenario: Equipment Pose 位于 Action Slot 之前

- **WHEN** root 把 `EquipmentPose` Linked Call 输出连接到正式 AnimationSlot 的 Source Pose
- **THEN** Compiler MUST 保留 Call 的动态 dispatch 并保持 Slot 由 root 唯一拥有
- **AND** 有限 Action MUST 继续通过同一 Slot 覆盖或组合所选持续 Pose

#### Scenario: Hand Goals 进入唯一 FullBodyIK

- **WHEN** root 调用 `EquipmentHandGoals` 并把 typed Goals 连接到 FullBodyIK
- **THEN** Implementation MUST 只生成 Goals 且 root MUST 执行唯一 solver
- **AND** MUST 不在 Implementation 内创建第二套手臂 IK 或 final writer

#### Scenario: 同一 Group Entry 被调用两次

- **WHEN** root graph 包含两个引用相同 Group 与 Entry 的 `LinkedPoseCall`
- **THEN** Validator 与 Compiler MUST 拒绝该图并定位两个 Call
- **AND** Runtime MUST 不定义同一 Entry state 一帧推进两次的隐式语义

### Requirement: LinkedPoseCall 端口与可用性必须显式传播

`LinkedPoseCall` 的 Pose 与 Value 端口 MUST 保留 Interface 声明的空间、lineage、completion 和 availability 合同。Implementation 切换 MUST 通过 Call 边界发布 `PoseDiscontinuity`；Call 下游 MUST 显式处理 Unavailable、Invalid 和 Discontinuity，不得由 Compiler 插入默认 Pose、旧 Implementation 值或隐藏 Blend。

#### Scenario: Implementation Entry 没有发布完整输出

- **WHEN** Entry fragment 结束但 required output completion 缺失
- **THEN** executor MUST 阻止 Call 下游与 FinalPublication
- **AND** MUST 不读取上一 generation 输出页

#### Scenario: 作者连接显式 Inertialization

- **WHEN** Local Pose Call 输出后连接 Inertialization
- **THEN** 切换产生的 Discontinuity MAY 由该节点按正式 Policy 处理
- **AND** diagnostics MUST 同时保留 switch 原因与 Inertialization 状态

### Requirement: 空实现必须通过正式 operation 发布合法零 Goals

Pose Graph Capability 与 Compiler MUST 提供正式 Empty FullBodyIK Goals operation，使 Linked Empty Implementation 能发布 `Availability=Ready`、`GoalCount=0`、当前 frame、Rig identity、completion 与 lineage 完整的 `component.full-body-ik-goals`。`GoalCount=0` MUST 表示没有该 Entry 贡献的额外 Goals，MUST 不表示 Unavailable、Invalid、读取上一帧或跳过 root FullBodyIK。

#### Scenario: 角色处于空手状态

- **WHEN** Equipment selector 选择 Empty Implementation
- **THEN** `EquipmentHandGoals` MUST 产生当前 generation 的合法零 Goals
- **AND** root FullBodyIK MUST 继续消费其它正式 Goal 来源并执行唯一 solver

#### Scenario: Empty Goals 缺失 completion

- **WHEN** Empty Goals operation 没有发布当前 frame 的 completion 或 Rig identity
- **THEN** Validate MUST 阻止 FullBodyIK 与 FinalPublication
- **AND** MUST 不把 `GoalCount=0` 用作绕过 lineage 校验的特殊值
