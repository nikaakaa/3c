## MODIFIED Requirements

### Requirement: 正式资产必须仍由人类可微调

系统 MUST保持 Agent 生成后的正式结果为普通 BTSMTL Graph、Timeline、ActionProfile，以及由 CharacterPipelineDefinition 引用的 CharacterAnimationPresentationProfile。作者 MUST能在 Graph Editor 调整逻辑，在 Timeline Editor 调整 clip/time，在 CharacterAnimationPresentationProfile Inspector 调整 Layer 与 producer binding，并在 Animancer TransitionLibrary 正式入口调整 transition 与 easing。Agent Snapshot MAY只读理解 Profile 与 Presentation identity，但 Agent Patch MUST不形成第二个 Presentation 写入口。

#### Scenario: 作者微调生成结果

- **WHEN** Agent 生成普通 Tree branch、Attack State 与 Timeline
- **THEN** 作者 MUST在 Graph Editor 调整 logic rule
- **AND** 在 Timeline Editor 调整 clip/time
- **AND** 在 CharacterAnimationPresentationProfile Inspector 调整 Layer 与 producer binding，并从该 Inspector 进入 Animancer TransitionLibrary
- **AND** 三个入口 MUST不双写同一字段

#### Scenario: Agent 继续修改

- **WHEN** 作者微调后再次请求 Agent 增加 dodge cancel
- **THEN** Agent MUST基于新的 Graph、Timeline 与只读 producer identity 生成增量 Patch
- **AND** MUST不覆盖作者在 CharacterAnimationPresentationProfile Inspector 或 Animancer TransitionLibrary 中的修改

### Requirement: Agent 不得形成第二个动画表现 authoring 入口

本 change 中 Agent Patch compiler MUST只继续编辑正式 Graph、StateMachine、Timeline 与 Blackboard authoring。它 MUST不创建或修改 CharacterAnimationPresentationProfile、Presentation Driver、Pipeline 自有 transition 表、Animancer TransitionLibrary 或动画 Priority。后续若需要 Agent 编辑 Animancer 原生 transition 或 Profile，必须通过独立 capability 定义唯一 authoring service。

#### Scenario: Patch 请求创建动画 Driver

- **WHEN** Agent Patch 包含旧 Presentation Driver、HandoffRole 或 Tree lifecycle animation site operation
- **THEN** compiler MUST返回 unsupported operation
- **AND** MUST不转换成默认 transition 或写入 Graph/Timeline

#### Scenario: Patch 请求配置动画层

- **WHEN** Agent Patch 包含 `configure_animation_layer` 或 animation layer payload
- **THEN** schema/compiler MUST将其作为未知操作拒绝
- **AND** Presentation Layer catalog MUST只能由 CharacterAnimationPresentationProfile Inspector 修改
