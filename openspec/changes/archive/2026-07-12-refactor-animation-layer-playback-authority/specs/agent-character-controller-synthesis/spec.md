## MODIFIED Requirements

### Requirement: Agent Snapshot 与 Validator 必须递归理解嵌套 StateMachine

Agent Snapshot MUST 递归输出 nested StateMachine graph/path/ownership、states、transitions、Action activation、Timeline、presentation leaf 与 HandoffRole。Validator MUST 检查 source/target leaf owner 与 None/Driver 配置可解析，并拒绝父子重复 Attack 状态或 animation domain 依赖。

#### Scenario: Corin Action Snapshot

- **WHEN** 导出 Corin compact Snapshot
- **THEN** 外层 MUST 显示 None、Attack、DodgeBack、DodgeForward
- **AND** Attack body MUST 显示 Attack1、Attack2 与 combo edges
- **AND** 每条 edge MUST 显示 None/Driver role

#### Scenario: 父子重复状态

- **WHEN** Attack1/Attack2 同时存在于父子层
- **THEN** Validator MUST 报告分裂结构
- **AND** Compiler MUST NOT选择 fallback

#### Scenario: leaf owner 断裂

- **WHEN** outer Driver 无法解析最后 source leaf 或 target leaf
- **THEN** Validator MUST 输出 graph/edge/owner 错误路径
- **AND** Compiler transaction MUST 回滚

## ADDED Requirements

### Requirement: Agent Schema 必须表达 HandoffRole 与 LayerOutputPolicy

Agent authoring schema MUST 破坏性升级为 v4，并在 Snapshot、Patch IR、Compiler 与 Validator 中表达 `AnimationHandoffRole`、Driver strategy 与 `AnimationLayerOutputPolicy`。系统 MUST NOT保留 schema v3 parser、缺字段默认值、endpoint HandoffMode 或按状态名称推断配置。

#### Scenario: 导出 Layer

- **WHEN** 导出 CharacterPipelineDefinition
- **THEN** 每层 MUST 输出 LayerId、Animancer index、mask、blend mode 与 OutputPolicy
- **AND** Corin Base MUST 输出 RequireOutput

#### Scenario: 导出 Transition

- **WHEN** 导出 StateMachine edge
- **THEN** Snapshot MUST 输出 HandoffRole
- **AND** Driver MUST 输出 strategy/duration/curve
- **AND** None MUST 不输出有效 strategy payload

#### Scenario: Patch 创建 Driver

- **WHEN** Patch 创建或修改 Driver edge
- **THEN** Patch MUST 提供合法 strategy definition
- **AND** Compiler MUST 调用正式 StateMachineGraph authoring API

#### Scenario: Patch 创建 None

- **WHEN** Patch 配置结构 edge 为 None
- **THEN** Compiler MUST 清理该 edge 的有效 strategy payload

#### Scenario: Patch 修改 Layer

- **WHEN** Patch 创建或修改 animation layer
- **THEN** Patch MUST 显式提供 OutputPolicy
- **AND** Compiler MUST 调用正式 Pipeline Definition API

#### Scenario: 旧 Schema

- **WHEN** Compiler 收到 schema v3
- **THEN** Compiler MUST 报告不支持
- **AND** 系统 MUST NOT补写默认 Role 或 OutputPolicy
