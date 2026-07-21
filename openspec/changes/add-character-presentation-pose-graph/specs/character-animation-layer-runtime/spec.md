## RENAMED Requirements

- FROM: `### Requirement: 动画层定义来自管线定义`
- TO: `### Requirement: 动画通道与Pose Slot定义必须分离`
- FROM: `### Requirement: 动画层输入必须是已解析播放选择与正式采样`
- TO: `### Requirement: 动画通道输入必须是已解析播放选择与正式采样`

## MODIFIED Requirements

### Requirement: 动画通道与Pose Slot定义必须分离

Timeline、Semantic IR、Program producer contract、selection command与Playback Lifecycle MUST只使用稳定`AnimationChannelId`表达逻辑仲裁通道。`CharacterAnimationPresentationProfile`引用的Pose Graph MUST唯一声明稳定`PoseSlotId`、channel-to-slot一对一binding与OutputPolicy；Blend Library MUST按PoseSlotId声明Stack Policy和transition matrix。Projection Compiler MUST把producer resource、AnimationChannelId、PoseSlotId、compiled Stack、Rig与Pose Program编入target-neutral `CharacterPresentationProjection`。Runtime MUST不读取`CharacterAnimationLayerDefinition`、Animancer layer index、Profile layer order或旧LayerId，并 MUST不按ProgramHash选择Projection。

#### Scenario: BaseLocomotion要求持续输出

- **WHEN** Corin BaseLocomotion channel绑定到RequireOutput BaseLocomotionSlot
- **THEN** 正常激活期间该slot MUST拥有Current Stack Entry、PendingFirstSample或明确Invalid状态
- **AND** 系统 MUST不静默解释为Empty或默认Idle

#### Scenario: FullBodyAction允许为空

- **WHEN** FullBodyAction channel提交None且对应slot为AllowEmpty
- **THEN** 该slot Stack MUST按exact source-Empty transition输出typed NoPose
- **AND** Pose Graph MUST让BaseLocomotion继续通过，不创建fallback clip

#### Scenario: command引用未知channel

- **WHEN** committed producer command的AnimationChannelId不存在或没有精确PoseSlot binding
- **THEN** Program/Projection contract校验 MUST报告配置错误
- **AND** command MUST不进入Lifecycle、Stack或Pose Graph

### Requirement: 动画通道输入必须是已解析播放选择与正式采样

Animation module MUST只接收Program Finalize已解析的每channel selection command，以及Presentation sampler生成的ProducerSample、Complete和Release。Selection MUST表达AnimationChannelId、PlaybackId、generation、SimulationTick、sequence与EventId，MUST不携带PoseSlotId、Bone Mask、Priority、Driver、Tree route或候选列表。Projection MUST在Presentation边界把channel精确映射到slot；Pose Graph MUST不重新选择producer。

#### Scenario: 同一channel收到两个target

- **WHEN** 同一Tick result为同一AnimationChannelId输出两个不同target
- **THEN** Program Finalize MUST报告冲突
- **AND** Projection、Lifecycle和Pose Graph MUST不选择任一target

#### Scenario: 两个channel各有target

- **WHEN** 同一Tick为BaseLocomotion和FullBodyAction分别输出一个合法target
- **THEN** 两个command MUST进入各自Lifecycle和PoseSlot Stack
- **AND** 最终空间组合 MUST只由Pose Graph决定
