## ADDED Requirements

### Requirement: Agent Snapshot必须只读解释Blend Space表现配置

Agent v17 CharacterController compact/full Snapshot的Presentation section MUST只读输出BlendSpace asset identity、content revision、mode、axis ParameterId与sample count、producer source binding、BlendSpacePlayer NodeId和Projection compile status。Snapshot MAY透传Missing、Stale、Corrupt、Rig、marker topology与parameter mismatch诊断；MUST不输出Runtime weight/time、generated Foot Analysis payload或Unity序列化布局。

#### Scenario: 导出使用Locomotion Blend Space的独立演示Definition

- **WHEN** Agent从独立Blend Space演示CharacterPipelineDefinition导出Snapshot
- **THEN** Presentation摘要 MUST能说明producer绑定的BlendSpace identity和Pose Graph BlendSpacePlayer
- **AND** MUST不把BlendSpace Sample伪装成Timeline Track或Clip

### Requirement: Agent Patch与MCP不得获得Blend Space写入口

Agent schema v17 Patch operation catalog、lowerer、handler、validator与MCP bridge MUST不增加BlendSpace asset、sample、axis、phase、Profile binding或Pose Graph mutation。正式修改 MUST只通过Character Animation Authoring Workspace和Presentation Authoring Service。`manage_btsmtl_agent_authoring` MUST继续只提供`export_snapshot`、`dry_run_patch`、`apply_patch`与`validate`四个action。

#### Scenario: Patch尝试移动Blend Space Sample

- **WHEN** v17 Patch提交未登记的BlendSpace sample mutation operation
- **THEN** Lowerer MUST在mutation前以unknown operation拒绝
- **AND** MCP bridge MUST不改用SerializedProperty、YAML、反射或其它action
