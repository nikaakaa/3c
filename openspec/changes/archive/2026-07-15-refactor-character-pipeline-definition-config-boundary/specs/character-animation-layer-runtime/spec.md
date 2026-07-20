## MODIFIED Requirements

### Requirement: 动画层定义来自管线定义

`CharacterPipelineDefinition` 引用的 `CharacterAnimationPresentationProfile` MUST作为动画 Layer catalog 与 producer resource binding 的唯一 authoring 来源。Compiler MUST将 layer identity、order、Animancer layer index、mask、blend mode、output policy 和 producer binding 编入 `CharacterPresentationProjection`；Runtime MUST只读取匹配 ProgramHash/source revision 的 Projection。Definition、Timeline、Graph、Presenter、旧 SO 或独立 Layer asset MUST不保存另一份 layer 真数据。

#### Scenario: Base layer 要求持续输出

- **WHEN** Corin Base layer 在 Profile 中配置为 RequireOutput 并编入 Projection
- **THEN** 正常激活期间该层 MUST拥有 Current、PendingFirstSample 或明确 Invalid 状态
- **AND** 系统 MUST不静默把该层解释为 Empty

#### Scenario: Optional layer 允许为空

- **WHEN** 某 layer 在 Profile 中显式配置为 AllowEmpty 并编入 Projection
- **THEN** Program MAY输出该层 None command
- **AND** Animancer MUST按正式 transition 将该层淡出到空
- **AND** 系统 MUST不创建 fallback clip

#### Scenario: producer command 引用缺失 layer

- **WHEN** committed producer command 或 Projection binding 的 LayerId 不存在
- **THEN** Program/Projection 组合校验 MUST报告配置错误
- **AND** 对应 command MUST不进入播放生命周期
