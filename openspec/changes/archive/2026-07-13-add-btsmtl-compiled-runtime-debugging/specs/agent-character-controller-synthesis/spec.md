# agent-character-controller-synthesis Specification

## ADDED Requirements

### Requirement: Agent Snapshot schema v4 必须输出稳定 authoring identity

Agent Snapshot MUST 升级为 schema v4，并为 Graph、Node、Edge、Timeline、Track、Clip 和 Blackboard declaration 输出正式稳定 authoring identity。Snapshot path 和列表 index MAY 作为可读定位信息，但 MUST NOT 取代 identity。

#### Scenario: 导出 Full Snapshot

- **WHEN** Agent exporter 导出 CharacterPipelineDefinition Full Snapshot
- **THEN** 每个 Graph、Node、Edge、Timeline、Track 和 Clip MUST 包含稳定 authoring identity
- **AND** snapshot MUST 输出当前 source revision 所需内容

#### Scenario: Timeline Track 重排后导出

- **WHEN** 作者重排 Track 或 Clip 后重新导出 Snapshot
- **THEN** 对应元素 identity MUST 保持
- **AND** index/path MAY 更新

### Requirement: Agent Patch 编译必须维护 identity 生命周期

Agent Patch compiler MUST 在更新现有元素时保持其 authoring identity，在创建新元素时生成新 identity，在复制元素时生成新 identity。系统 MUST 只接受 schema v4，不得保留 v3 兼容解析或按 path 猜测 identity。

#### Scenario: 更新现有 Timeline Clip

- **WHEN** Patch 修改一个由 authoring identity 指定的 Clip 参数
- **THEN** compiler MUST 修改该 Clip
- **AND** Clip identity MUST 保持

#### Scenario: 创建新 Track

- **WHEN** Patch 创建新的 Timeline Track
- **THEN** compiler MUST 为该 Track 生成新 identity
- **AND** validator MUST 拒绝缺失或重复 identity

#### Scenario: 旧 schema 输入

- **WHEN** Patch 或 Snapshot 请求使用 schema v3
- **THEN** service MUST 返回明确 unsupported schema 错误
- **AND** MUST NOT 通过 index、display name 或 path fallback apply
