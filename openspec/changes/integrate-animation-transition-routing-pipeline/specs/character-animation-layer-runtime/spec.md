# character-animation-layer-runtime Specification

## MODIFIED Requirements

### Requirement: 动画通道与Pose节点定义必须分离

Timeline、Semantic IR、Program producer contract、selection command与Playback Lifecycle MUST只使用稳定AnimationChannelId表达逻辑仲裁通道。`CharacterAnimationPresentationProfile`引用的Pose Graph MUST唯一声明Selection Input、MarkerSync、Player、request consumer、组合、world-aware节点与Output topology；Blend Policy MUST由对应BlendStack PoseNodeId拥有并选择Blend Logic，Inertialization consumer Policy MUST由对应Inertialization PoseNodeId拥有。Projection Compiler MUST把producer resource、AnimationChannelId、PoseNodeId、compiled Pose Plan、exact Blend Logic、request route、Policy与Rig编入target-neutral `CharacterPresentationProjection`。Runtime MUST不读取旧PoseSlot、Layer catalog、Animancer layer index、Profile layer order、旧LayerId或直接Player专属endpoint matrix，并 MUST不按ProgramHash选择Projection。

#### Scenario: BaseLocomotion要求持续输出

- **WHEN** Corin BaseLocomotion channel绑定到Required Selection Input
- **THEN** 正常激活期间匹配Player MUST拥有Selected source、PendingFirstSample或明确Invalid状态
- **AND** 系统 MUST不静默把该输入解释为Empty

#### Scenario: FullBodyAction允许为空

- **WHEN** FullBodyAction channel提交None且对应Selection Input为AllowEmpty
- **THEN** 图中显式BlendStack MUST按Standard Blend或零时长Standard Blend输出typed NoPose
- **AND** Pose Graph MUST让BaseLocomotion继续通过且不创建fallback clip或惯性target

#### Scenario: producer command引用未知channel

- **WHEN** committed producer command的AnimationChannelId不存在或没有精确Selection Input binding
- **THEN** Program/Projection组合校验 MUST报告配置错误
- **AND** 对应command MUST不进入Lifecycle、Stack、request route或Pose Graph

#### Scenario: Float32与Fixed复用动画Projection

- **WHEN** Float32与Fixed Program由相同SemanticHash和producer contract生成
- **THEN** 两个Presentation contract Adapter MUST加载同一套channel、Pose Plan、request route与producer binding
- **AND** Runtime MUST不按ProgramHash复制、选择或降级Projection

### Requirement: 显式动画Player节点必须拥有各自时间连续性

`SelectedPosePlayer`与`BlendSpacePlayer` MUST只保持当前Selection、输出typed discontinuity并按compiled rule选择明确硬切或发布Inertialization Request。`BlendStack` MUST只对连接到该节点的Selection拥有Standard Blend entry、CrossFade clock、Stored Pose、Per-Bone Blend Profile、request producer state和source retirement。`Inertialization` MUST独占completed Pose history、单Pose residual、衰减与rebase，并只消费compiled request。项目 MUST不为每AnimationChannel、旧PoseSlot或Graph branch自动创建隐藏Stack、request bus或Inertialization；Layered Blend Per Bone、Apply Additive、Foot Placement与Output Pose MUST不重建Player transition。Animancer source backend MUST只创建或复用source playable并把source capture job安装到同一PlayableGraph。

#### Scenario: producer 包含多个 clip

- **WHEN** 同一Timeline producer采样到多个重叠clip
- **THEN** source backend MUST在同一source playable内表达producer内部clip weights
- **AND** 显式BlendStack MUST负责该source与其它source之间的Standard Blend或request发布

#### Scenario: Standard Blend期间再次切换

- **WHEN** 当前BlendStack仍保留A时逻辑选择C且exact rule为StandardBlend
- **THEN** Stack MUST从唯一正式entry/Stored状态push C
- **AND** PlaybackRuntime MUST不建立第二个handoff stack或恢复中间逻辑状态

#### Scenario: Standard Blend期间切换到Inertialization

- **WHEN** 当前BlendStack仍在A到B Standard Blend且C exact rule为Inertialization
- **THEN** Stack MUST发布request给唯一下游consumer
- **AND** consumer MUST从当前Stack completed output建立residual
- **AND** Stack MUST不执行残差算法

#### Scenario: slot概览权重为零但骨骼仍有贡献

- **WHEN** Stack完成帧的OutputWeight为零但dense per-bone output仍至少有一个非零权重
- **THEN** Player availability MUST保持Pose
- **AND** Pose Graph MUST按dense per-bone weight执行空间合成
- **AND** MUST不使用OutputWeight裁掉仍然有效的骨骼姿势

### Requirement: source backend必须只负责采样

显式MarkerSync节点 MUST只提供producer effective sample page与relation snapshot。Animancer source backend MUST只拥有source playable与producer内部clip采样；显式BlendStack MUST唯一拥有Standard Blend clock、curve、Per-Bone weight、Stored、request producer state与release；局部Inertialization MUST唯一拥有completed history、residual、衰减与rebase；Pose Graph MUST唯一拥有组合与最终pose。项目 MUST不新增第二套crossfade weight、Stack内Inertial算法、Animancer automatic synchronization、managed evaluator、全局request bus或第二动画时钟。

#### Scenario: 同步target开始播放

- **WHEN** matched target首份合法sample进入lifecycle
- **THEN** Lifecycle MUST把target的Animation Selection发布到正式Player路径
- **AND** MarkerSync节点 MUST不写入Blend Logic、request、transition progress或source weight

#### Scenario: source retirement由Standard Blend exact completion确认

- **WHEN** source逻辑ownership已释放但Standard Blend Stack仍保留其entry
- **THEN** source MUST继续通过PresentationRetention提供animation-only sample与effective time
- **AND** relation MUST只在正式Retired后脱离

#### Scenario: source retirement由Inertialization capture确认

- **WHEN** BlendStack已发布Inertialization Request但consumer capture completion尚未提交
- **THEN** outgoing source MUST保持正式handoff/sample引用
- **AND** source与Marker relation MUST只在capture completion成功后release
