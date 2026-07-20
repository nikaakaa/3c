# agent-character-controller-synthesis Specification

## MODIFIED Requirements

### Requirement: 正式资产必须仍由人类可微调

系统 MUST保持 Agent 生成后的正式结果为普通 BTSMTL Graph、Timeline、ActionProfile 和 CharacterPipelineDefinition 内联 Presentation Definition。作者 MUST能在 Graph Editor 调整逻辑，在 Timeline Editor 调整 clip/time，在 CharacterPipelineDefinition Inspector 调整 Layer 与 producer binding，并在 Animancer TransitionLibrary 正式入口调整 transition 与 easing。Agent Snapshot MAY只读理解 Presentation identity，但 Agent Patch MUST不形成第二个 Presentation 写入口。

#### Scenario: 作者微调生成结果

- **WHEN** Agent 生成普通 Tree branch、Attack State 与 Timeline
- **THEN** 作者 MUST在 Graph Editor 调整 logic rule
- **AND** 在 Timeline Editor 调整 clip/time
- **AND** 在 CharacterPipelineDefinition Inspector 调整 Layer 与 producer binding，并从 Inspector 进入 Animancer TransitionLibrary
- **AND** 三个入口 MUST不双写同一字段

#### Scenario: Agent 继续修改

- **WHEN** 作者微调后再次请求 Agent 增加 dodge cancel
- **THEN** Agent MUST基于新的 Graph、Timeline 与只读 producer identity 生成增量 Patch
- **AND** MUST不覆盖作者在 CharacterPipelineDefinition Inspector 或 Animancer TransitionLibrary 中的修改

### Requirement: Agent Snapshot 与 Validator 必须递归理解嵌套 StateMachine

Agent Snapshot MUST递归输出完整 RootTree authoring routes、普通 RunnableNode、flow edges、inline/shared Graph、nested StateMachine、logical transitions、Action activation、Timeline 与稳定 animation producer identity。Presentation section MUST只读输出 Layer catalog、TransitionLibrary identity 与 producer binding。Validator MUST检查 Graph topology、route identity、Timeline identity 与 Timeline LayerId，但 MUST不校验或写入 Presentation binding、Animancer transition 或 runtime playback lifecycle。

#### Scenario: Corin Snapshot

- **WHEN** 导出 Corin compact Snapshot
- **THEN** Graph section MUST显示 Root Parallel、普通 Runnable、外层 None/Attack/Dodge、内层 Attack1/Attack2 与完整 route
- **AND** Presentation section MUST只读显示 Base layer、TransitionLibrary 与 Timeline producer identity/binding
- **AND** Graph Node/Edge MUST不输出动画角色或策略字段

#### Scenario: Timeline identity 断裂

- **WHEN** Graph 引用的 Timeline、Track 或 Clip 缺失稳定 authoring identity
- **THEN** Validator MUST输出对应 Graph/Timeline source 错误
- **AND** Compiler transaction MUST回滚

#### Scenario: 父子重复状态

- **WHEN** Attack1/Attack2 同时存在于父子层
- **THEN** Validator MUST报告分裂结构
- **AND** Compiler MUST不选择 fallback topology

### Requirement: Agent Patch 编译必须维护 identity 生命周期

Agent Patch compiler MUST在更新现有元素时保持其 authoring identity，在创建新元素时生成新 identity，在复制元素时生成新 identity。系统 MUST只接受 schema v6，不得保留旧 schema 兼容解析或按 path 猜测 identity。

#### Scenario: 更新现有 Timeline Clip

- **WHEN** Patch 修改一个由 authoring identity 指定的 Clip 参数
- **THEN** compiler MUST修改该 Clip
- **AND** Clip identity MUST保持

#### Scenario: 创建新 Track

- **WHEN** Patch 创建新的 Timeline Track
- **THEN** compiler MUST为该 Track 生成新 identity
- **AND** validator MUST拒绝缺失或重复 identity

#### Scenario: 旧 schema 输入

- **WHEN** Patch 或 Snapshot 请求使用旧 schema
- **THEN** service MUST返回明确 unsupported schema 错误
- **AND** MUST不通过 index、display name 或 path fallback apply

## ADDED Requirements

### Requirement: Agent Snapshot schema v6 必须输出稳定 authoring identity

Agent Snapshot MUST使用 schema v6，并为 Graph、Node、Edge、Timeline、Track、Clip、Blackboard declaration 与 Timeline animation producer 输出正式稳定 authoring identity。Snapshot path 和列表 index MAY作为可读定位信息，但 MUST不取代 identity。Snapshot MUST不输出 Tree animation Driver、ExecutionLineage、LayerPlan 或 runtime playback lifecycle。

#### Scenario: 导出 Full Snapshot

- **WHEN** Agent exporter 导出 CharacterPipelineDefinition Full Snapshot
- **THEN** 每个 Graph、Node、Edge、Timeline、Track、Clip 和 animation producer MUST包含稳定 authoring identity
- **AND** snapshot MUST输出当前 source revision 所需的逻辑与 Timeline 内容

#### Scenario: Timeline Track 重排后导出

- **WHEN** 作者重排 Track 或 Clip 后重新导出 Snapshot
- **THEN** 对应元素和 producer identity MUST保持
- **AND** index/path MAY更新

### Requirement: Agent 不得形成第二个动画表现 authoring 入口

本 change 中 Agent Patch compiler MUST只继续编辑正式 Graph、StateMachine、Timeline 与 Blackboard authoring。它 MUST不创建或修改 Presentation Driver、Pipeline 自有 transition 表、Animancer TransitionLibrary 或动画 Priority。后续若需要 Agent 编辑 Animancer 原生 transition，必须通过独立 capability 定义唯一 authoring service。

#### Scenario: Patch 请求创建动画 Driver

- **WHEN** Agent Patch 包含旧 Presentation Driver、HandoffRole 或 Tree lifecycle animation site operation
- **THEN** compiler MUST返回 unsupported operation
- **AND** MUST不转换成默认 transition 或写入 Graph/Timeline

#### Scenario: Patch 请求配置动画层

- **WHEN** Agent Patch 包含 `configure_animation_layer` 或 animation layer payload
- **THEN** schema/compiler MUST将其作为未知操作拒绝
- **AND** Presentation Layer catalog MUST只能由 CharacterPipelineDefinition Inspector 修改

## REMOVED Requirements

### Requirement: Agent Snapshot schema v5 必须输出稳定 authoring identity

**Reason**: 本 change 破坏性删除 v5 的 Presentation Driver 与动画 Priority 字段，新结构必须使用 v6，不能让旧 JSON 以同一版本被误判为兼容。

#### Scenario: 拒绝旧 schema

- **WHEN** exporter 或 compiler 收到 schema v5 数据
- **THEN** 系统 MUST明确拒绝旧版本
- **AND** MUST不补默认 Presentation 字段或执行兼容迁移

### Requirement: Agent Schema v5必须表达完整Tree Presentation

**Reason**: 完整 Tree Presentation Driver 是已废弃架构。Agent 不应把逻辑 Tree site 复制成动画表现配置。

#### Scenario: 删除旧 Presentation section

- **WHEN** 检查已删除的 schema v5 Presentation section
- **THEN** schema MUST不包含 Driver binding、NodeEnter、NodeRelease 或 EdgeCommit animation operation

### Requirement: Agent Validator必须检查通用Presentation binding语义

**Reason**: 通用 Presentation binding 已删除，正式转场由 Animancer 原生 transition 数据拥有。

#### Scenario: 删除旧 Driver 校验

- **WHEN** Agent Validator 校验 Graph
- **THEN** 它 MUST不查找 Tree Driver、MissingDriver 或 Structural policy
