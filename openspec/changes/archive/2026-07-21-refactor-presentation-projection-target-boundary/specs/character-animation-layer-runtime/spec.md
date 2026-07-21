## MODIFIED Requirements

### Requirement: 动画层定义来自管线定义

`CharacterPipelineDefinition` 引用的 `CharacterAnimationPresentationProfile` MUST作为动画 Layer catalog 与producer resource binding的唯一authoring来源。唯一Presentation Projection Compiler MUST将layer identity、order、Animancer layer index、mask、blend mode、output policy和producer binding编入target-neutral `CharacterPresentationProjection`；Runtime MUST只读取匹配`CharacterPresentationSemanticContract`、Gameplay SourceRevision与ProjectionRevision的Projection。ProgramHash、NumericProfile与Target ABI只属于目标Program和Session compatibility，MUST不进入Projection payload或动画层选择。Definition、Timeline、Graph、Presenter、旧SO或独立Layer asset MUST不保存另一份layer真数据。

#### Scenario: Base layer 要求持续输出

- **WHEN** Corin Base layer在Profile中配置为RequireOutput并编入Projection
- **THEN** 正常激活期间该层 MUST拥有Current、PendingFirstSample或明确Invalid状态
- **AND** 系统 MUST不静默把该层解释为Empty

#### Scenario: Optional layer 允许为空

- **WHEN** 某layer在Profile中显式配置为AllowEmpty并编入Projection
- **THEN** Program MAY输出该层None command
- **AND** Animancer MUST按正式transition将该层淡出到空
- **AND** 系统 MUST不创建fallback clip

#### Scenario: producer command 引用缺失 layer

- **WHEN** committed producer command或Projection binding的LayerId不存在
- **THEN** Program/Projection contract校验 MUST报告配置错误
- **AND** 对应command MUST不进入播放生命周期

#### Scenario: Float32与Fixed复用动画层Projection

- **WHEN** Float32与Fixed Program由相同SemanticHash和producer contract生成
- **THEN** 两个Presentation contract Adapter MUST加载同一套Layer与producer binding
- **AND** Runtime MUST不按ProgramHash复制、选择或降级Projection
