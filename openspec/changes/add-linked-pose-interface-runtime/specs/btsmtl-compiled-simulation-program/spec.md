## ADDED Requirements

### Requirement: Linked Pose 编译身份必须只属于 Presentation Projection

Character Simulation Build MUST 从同一 Frontend artifact 与 Presentation authoring revision 编译 root Pose Plan、Linked Interface、Group、selector、Implementation Entry fragments、source closure 与 Native Pose operations，并在发布前执行跨 artifact identity 校验。所有 Linked authoring、signature、Fact contract、Implementation content hash、Rig identity、source closure、layout 与 Runtime ABI MUST 纳入 `ProjectionRevision`。它们 MUST 不改变 gameplay `ContractHash`；`ContractHash` MUST 继续只覆盖既有 gameplay semantic contract 与有限 Action producer contract。

Program、Projection、Native Pose Program 与 generated references MUST 继续在同一 Build Transaction 中原子发布。任一 Linked fragment 失败 MUST 保留完整旧发布组，MUST 不发布缺少候选或 Entry 的部分目录。

#### Scenario: 一个候选 Implementation 编译失败

- **WHEN** Group 候选中的任一 Implementation Entry 包含非法 node context、Fact 依赖或缺失 source
- **THEN** Character Build MUST 拒绝整个新 Projection 发布
- **AND** MUST 不删除失败实现后发布不完整目录

#### Scenario: 只修改 Implementation Graph

- **WHEN** Gameplay Semantic IR 没有变化，但 Implementation content hash 变化
- **THEN** Build MUST 生成新的 Presentation `ProjectionRevision` 与对应 Native Pose fragments
- **AND** gameplay `ContractHash` 与 Numeric Target `ProgramHash` MUST 保持其既有语义

#### Scenario: Linked 目录使用派生 catalog hash

- **WHEN** Projection 为 stale detection 或 diagnostics 生成 Linked catalog hash
- **THEN** 该 hash MUST 从同一 Projection 内容确定性派生
- **AND** MUST 不成为独立发布版本或 gameplay compatibility identity
