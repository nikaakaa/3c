# agent-character-controller-synthesis Delta

## MODIFIED Requirements

### Requirement: Agent Snapshot schema v10 必须输出稳定 authoring identity

Agent Snapshot MUST使用schema v10，并为Graph、Node、Edge、Timeline、Track、Clip、Blackboard declaration、CharacterInputProfile request timing与Timeline animation producer输出正式稳定authoring identity。Snapshot path和列表index MAY作为可读定位信息，但 MUST不取代identity。Snapshot MUST不输出Tree animation Driver、ExecutionLineage、LayerPlan或runtime playback lifecycle。schema v10 Snapshot MUST成为生成v10 Patch的唯一上下文，不提供旧schema镜像输出。Patch IR MUST通过正式typed command读取和修改同一request timing字段，不得按request名称推断类别。

#### Scenario: 导出Full Snapshot

- **WHEN** Agent exporter导出`CharacterPipelineDefinition` Full Snapshot
- **THEN** 每个Graph、Node、Edge、Timeline、Track、Clip、animation producer和input request timing MUST包含稳定authoring identity或正式typed值
- **AND** snapshot MUST标记schema v10
- **AND** snapshot MUST输出当前source revision所需的逻辑、Timeline与输入配置内容

#### Scenario: 修改 Request Timing Class

- **WHEN** Agent Patch把Corin Attack request timing改为Offensive
- **THEN** handler MUST通过CharacterInputProfile正式authoring API写入同一serialized字段
- **AND** dry-run、apply、export与validator MUST使用同一typed command合同
