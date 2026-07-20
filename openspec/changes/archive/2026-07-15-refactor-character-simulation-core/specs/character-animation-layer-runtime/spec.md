# character-animation-layer-runtime Specification

## MODIFIED Requirements

### Requirement: 动画层定义来自管线定义

`CharacterPipelineDefinition` 内联的 CharacterAnimationPresentationDefinition MUST 继续作为动画 Layer catalog 与 producer resource binding 的唯一 authoring 来源。Compiler MUST 将 layer identity、order、Animancer layer index、mask、blend mode、output policy 和 producer binding 编入 `CharacterPresentationProjection`；Runtime MUST 只读取匹配 ProgramHash/source revision 的 Projection。Timeline、Graph、Presenter、旧 SO 或独立 Layer asset MUST 不保存另一份 layer 真数据。

#### Scenario: Base layer 要求持续输出

- **WHEN** Corin Base layer 在 Projection 中配置为 RequireOutput
- **THEN** 正常激活期间该层 MUST 拥有 Current、PendingFirstSample 或明确 Invalid 状态
- **AND** 系统 MUST 不静默把该层解释为 Empty

#### Scenario: Optional layer 允许为空

- **WHEN** 某 layer 在 Projection 中显式配置为 AllowEmpty
- **THEN** Program MAY 输出该层 None command
- **AND** Animancer MUST 按正式 transition 将该层淡出到空
- **AND** 系统 MUST 不创建 fallback clip

#### Scenario: producer command 引用缺失 layer

- **WHEN** committed producer command 或 Projection binding 的 LayerId 不存在
- **THEN** Program/Projection 组合校验 MUST 报告配置错误
- **AND** 对应 command MUST 不进入播放生命周期

### Requirement: 循环动画必须由连续 visual Timeline time 重采样

循环 producer 的 continuous visual time MUST由 committed Timeline logic sample、cycle identity 与 PresentationFrame interpolation计算。AnimationPlaybackLifecycle MUST只关联 selected/current/outgoing producer，不得推进 CharacterSimulationState Timeline clock。逻辑 producer release 后，PresentationRetention MAY继续 animation-only sampling，MUST不执行 Gameplay operation。

#### Scenario: 循环回绕

- **WHEN** committed loop sample 从末尾回绕到开头
- **THEN** animation track MUST使用连续 visual time重采样同一 playback generation

#### Scenario: Source 已停止

- **WHEN** producer Gameplay ownership 已 release 且 Animancer state仍 Outgoing
- **THEN** Presentation MAY继续 animation-only sample
- **AND** TreeClip、Motion、Window 与 Cue fact MUST不再产生

### Requirement: 动画层输入必须是已解析播放选择与正式采样

Animation module MUST只接收 Program Finalize 已解析的 Layer selection command，以及 Presentation sampler 生成的 ProducerSample、Complete 和 Release。Selection MUST表达 LayerId、PlaybackId、generation、SimulationTick、sequence 与 EventId，MUST不携带 Priority、Driver、Tree route 或候选列表。

#### Scenario: Base 收到唯一 Target

- **WHEN** committed batch 为 Base 选择一个 PlaybackId
- **THEN** Animation module MUST只等待和播放该 target

#### Scenario: 同层重复选择

- **WHEN** 同一 Tick result 为同一 LayerId 输出两个不同 target
- **THEN** Finalize MUST报告逻辑冲突并拒绝 Tick

### Requirement: outgoing producer 必须使用纯表现 retention

逻辑 producer release 后，AnimationPlaybackLifecycle MAY持有只读 PresentationRetention，让纯表现 sampler继续生成 outgoing animation sample直到 Animancer fade完成。Retention MUST不恢复 Program membership，也 MUST不运行 TreeClip、Motion、root motion、window、cue fact 或 SyncDomain fact。

#### Scenario: 攻击逻辑结束但动画淡出

- **WHEN** Attack playback Gameplay 已停止且 Animancer state仍 Outgoing
- **THEN** sampler MUST只推进 animation visual sample

#### Scenario: Session dispose

- **WHEN** Actor/Session dispose
- **THEN** lifecycle MUST立即清理 Current、Outgoing、PendingFirstSample 与 retention
