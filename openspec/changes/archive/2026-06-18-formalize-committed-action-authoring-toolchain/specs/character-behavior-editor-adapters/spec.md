## ADDED Requirements
### Requirement: Behavior Graph 与 Action Branch Editor 边界
Character Behavior Editor MUST 继续只编辑 behavior source topology，例如 root、composite、Locomotion leaf、CommittedAction leaf、edge 和 editor position。Committed Action Branch Editor MUST 负责 action definition 内的 selector、condition、timeline node 和 timeline payload。两个编辑器 MAY 互相提供打开或定位入口，但 MUST NOT 复制、保存或编译对方的数据。

#### Scenario: Behavior Graph 不保存 Action Branch
- **WHEN** 设计者在 Character Behavior Editor 中保存 graph
- **THEN** 保存内容 MUST 限定为 behavior source topology
- **AND** MUST NOT 保存 selector、condition、TimelineNode、ActionTimeline track、clip、motion payload、animation key、window 或 cue
- **AND** Behavior compiler MUST NOT 编译 Committed Action branch payload

#### Scenario: Branch Editor 不编辑 Source Topology
- **WHEN** 设计者在 Committed Action Branch Editor 中编辑 action branch
- **THEN** 保存内容 MUST 写入 `CharacterActionDefinitionSO` 或批准等价 action definition
- **AND** MUST NOT 修改 behavior source graph 的 root、parallel、Locomotion leaf、CommittedAction leaf 或 edge
- **AND** Action definition compiler MUST NOT 创建 behavior source root 或 Locomotion leaf
