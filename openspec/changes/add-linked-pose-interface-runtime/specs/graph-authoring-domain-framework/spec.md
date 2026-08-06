## ADDED Requirements

### Requirement: Linked Pose 能力必须由共享 Capability 完整声明

`GraphAuthoringCapabilityCatalog` MUST 声明 `LinkedPoseCall` 的允许 graph context、Group/Interface/Entry typed payload、由 Interface 投影的动态 ports、connection policy 与 Compiler handler，并声明 Linked Implementation Entry Graph 允许和禁止的 node capability 集合。人工 UI、Document exporter、strict parser、Reconciler、typed Presentation Mutation、Validator 与 Compiler MUST 读取同一声明；任何入口 MUST 不按节点 C# 类型、显示标题或资产路径单独判断 Linked 语义。

#### Scenario: Interface 更新动态端口

- **WHEN** 新 Interface revision 改变 Entry port signature
- **THEN** Canvas、Document、Mutation、Validator 与 Compiler MUST 同时投影新端口
- **AND** 旧 Call 与 Implementation MUST 因 signature 不匹配而被拒绝

#### Scenario: 在 Implementation 中创建 FullBodyIK

- **WHEN** 人工 UI 或 Document 尝试在 Linked Entry Graph 创建 `FullBodyIK`
- **THEN** 共享 Capability context policy MUST 在 Mutation 前拒绝
- **AND** Compiler MUST 继续执行同一规则作为完整性校验

### Requirement: Empty Goals 能力必须使用正式 typed operation

共享 Capability MUST 声明生成 `component.full-body-ik-goals` 的 Empty Goals operation、固定端口、允许 graph context、Rig/lineage/completion 合同与 Compiler handler。Canvas、Document、Mutation、Validator 与 Compiler MUST 复用该声明，MUST 不通过 `PoseBoneIKGoals` 的非法零数量配置、空引用或节点特判表达空目标。

#### Scenario: Empty Implementation 创建零 Goals

- **WHEN** 作者在 `EquipmentHandGoals` Entry 中创建 Empty Goals operation
- **THEN** Capability MUST 投影合法 `component.full-body-ik-goals` 输出
- **AND** Compiler MUST 生成 Ready、GoalCount=0 且 completion 完整的正式 operation

### Requirement: Linked Pose Details 必须只编辑作者合同而不编辑生成布局

Interface、Implementation、Group、selector 与 Call Details MUST 只暴露稳定 identity 引用、Entry 映射、业务 selector 映射及 Capability 声明的作者字段。Generated operation range、stage range、workspace offset、state offset、source index、derived content hash 与 Runtime handle MUST 保持只读或不出现在 authoring 资产中。

#### Scenario: 作者选择 LinkedPoseCall

- **WHEN** Details 显示一个 Call 节点
- **THEN** 作者 MUST 只能选择合法 Group、Interface 与 Entry
- **AND** MUST 不能手工填写 dispatch index 或 Implementation 资源路径

#### Scenario: 作者配置 Equipment selector

- **WHEN** Details 显示 `CharacterEquipmentLinkedPoseSelectionBinding`
- **THEN** 作者 MUST 只能编辑目标 Group、EquipmentSlotId、EquipmentId 与 Implementation identity 映射
- **AND** MUST 不能编辑 generation、dispatch range 或核心 selection source 类型
