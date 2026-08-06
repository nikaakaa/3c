## ADDED Requirements

### Requirement: Agent 必须通过正式 Presentation Mutation 编辑 Linked Pose authoring

Agent Snapshot 与 Document editable 分片 MUST 完整表达 Linked Implementation、Entry Graph、Call、Profile Group 与 selector binding；Interface 正文、generated Projection、Entry fragments、workspace layout 与 Runtime ABI MUST 保持只读 context。Reconciler MUST 把合法变化降低为与人工作者表面相同的 typed Presentation Mutation，并调用同一 Capability、Validator 与资产事务。Agent MUST 不通过 Equipment Feature、资源路径、SerializedProperty 或私有工具形成第二个 Linked Pose 入口。

#### Scenario: Agent 为新 EquipmentId 装配既有实现

- **WHEN** Agent 修改 Equipment selector binding，把一个正式 EquipmentId 映射到既有 Implementation
- **THEN** Reconciler MUST 生成 typed selector mapping Mutation
- **AND** Validator MUST 检查 Group、Interface signature 与候选 source closure

#### Scenario: Agent 写入 generated fragment offset

- **WHEN** Document 包含 operation range、state offset 或 runtime handle 修改
- **THEN** strict parser 或 Mutation Compiler MUST 拒绝该字段
- **AND** MUST 不把它保存进 authoring asset

### Requirement: Agent Validator 必须理解 Linked Pose 完整闭包

Agent Validator MUST 验证 Interface/Implementation/Entry 覆盖、Fact contract、Call signature 与唯一性、每 Group selector 唯一性、Equipment 精确映射、graph context 白名单、root 唯一 Slot/FullBodyIK/final writer、Empty Goals、source closure 与 Projection ABI，并透传正式 Frontend 与 Projection diagnostics。Validator MUST 不以空 Implementation、默认 Clip、上一选择或跳过失败 Entry 降低错误。

#### Scenario: Agent 生成的 Implementation 包含 AnimationSlot

- **WHEN** Linked Entry Graph 出现 root-owned AnimationSlot
- **THEN** Validator MUST 返回稳定 node context 诊断
- **AND** Compile Report MUST 定位 Implementation、Entry、Graph 与 Node identity

#### Scenario: Agent 为 Group 创建重复 selector

- **WHEN** editable target state 让两个 selector binding 服务同一 Group
- **THEN** dry-run 与 validate MUST 返回稳定 Group 冲突诊断
- **AND** MUST 不按文档顺序保留其中一个
