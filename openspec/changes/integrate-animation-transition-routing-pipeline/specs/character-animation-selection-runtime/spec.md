# character-animation-selection-runtime Specification

## MODIFIED Requirements

### Requirement: Pose Graph必须显式选择Animation Player

每个Animation Selection MUST只通过Pose Graph中的`SelectedPosePlayer`、`BlendSpacePlayer`或`BlendStack`节点降低为Pose Value。`SelectedPosePlayer`与`BlendSpacePlayer` MUST只采样当前Selection并发布typed discontinuity；没有compiled Inertialization rule时Selection变化执行明确硬切。`BlendStack` MUST保存该节点自己的多source历史并在exact rule为`StandardBlend`时执行已编译CrossFade，在exact rule为`Inertialization`时只发布typed request并把残差交给下游显式Inertialization consumer。Compiler与Runtime MUST不在Selection Input、AnimationChannel或OutputPose背后自动插入Player、Stack、Inertialization、request bus或fade。

#### Scenario: 稳定动作使用直接Player

- **WHEN** 作者把Action Selection连接到SelectedPosePlayer且没有Inertialization rule
- **THEN** Selection变化 MUST直接替换当前source
- **AND** Runtime MUST不创建隐藏Blend Stack或Inertialization

#### Scenario: 状态机输出使用Standard Blend

- **WHEN** 作者把BaseLocomotion Selection连接到BlendStack且exact rule为StandardBlend
- **THEN** Selection变化 MUST由该节点保存旧player并连续过渡
- **AND** 其它未连接该节点的Selection MUST不承担其workspace或transition

#### Scenario: 动作输出请求Inertialization

- **WHEN** 作者把FullBodyAction Selection连接到BlendStack、下游连接Action Inertialization且exact rule为Inertialization
- **THEN** BlendStack MUST发布compiled request
- **AND** Action Inertialization MUST成为该request的唯一consumer

### Requirement: Marker Sync与Player必须通过正式source usage合同配对

每个MarkerSync输出 MUST精确连接一个`SelectedPosePlayer`、`BlendSpacePlayer`或`BlendStack`。Compiler MUST生成一对一`PlayerSourceUsage`合同，并显式区分`Sample`、`HandoffReference`与`Release`。SelectedPosePlayer或BlendSpacePlayer MUST在切换边界把旧source声明为一次性HandoffReference、把新source声明为Sample，完成映射后按compiled Blend Logic交给直接切换或下游Inertialization；BlendStack在Standard Blend期间 MUST把当前与尚未exact release的历史source声明为Sample，在Inertialization request边界 MUST保留outgoing handoff/sample reference直到consumer capture completion。MarkerSync只为该集合解析时间，随后Player完成source sample与Pose求值。MarkerSync MUST不扫描BlendStack entry、读取weight、选择Blend Logic或延长request后的source寿命；Player MUST不复制marker relation算法。fan-out到多个Player、串联两个MarkerSync或缺少Player consumer MUST编译失败。

#### Scenario: BlendStack保留Walk并接收Run

- **WHEN** Standard Blend期间BlendStack source usage同时包含Retained Walk与incoming Run
- **THEN** 配对MarkerSync MUST按Track marker binding解析两者effective time
- **AND** BlendStack MUST独立计算两者CrossFade weight

#### Scenario: SelectedPosePlayer从Walk切换到Run

- **WHEN** SelectedPosePlayer在Inertialization request边界把Walk声明为HandoffReference
- **THEN** MarkerSync MUST能用Walk最后effective segment映射Run起始effective time
- **AND** consumer capture completion后Walk MUST立即release且后续平滑只属于Inertialization

#### Scenario: BlendStack从共同可见期切换到惯性化

- **WHEN** A到B Standard Blend期间C target选择Inertialization
- **THEN** MarkerSync MUST为边界帧的正式outgoing usage和C解析effective time
- **AND** old relation MUST只在consumer capture completion后detach

#### Scenario: 同一Selection进入两个Player

- **WHEN** 作者需要两个Player各自保留独立播放状态
- **THEN** 作者 MUST为需要同步的每条Player路径分别创建MarkerSync
- **AND** Compiler MUST不共享隐藏relation state

### Requirement: Blend Stack节点必须独占自身时间连续性

每个编译后的BlendStack节点 MUST拥有唯一runtime identity、active player顺序、Standard Blend clock、Stored Pose、Per-Bone Blend Profile、source retention、request producer state与exact release。节点 MUST只消费Animation Selection与node-local Blend Policy，输出普通Pose Value并按compiled rule选择Standard Blend或发布Inertialization Request；MUST不读取或执行Inertial residual、consumer history、Gameplay State、Motion Matching query、下游Bone Mask、Foot Placement或Output topology。

#### Scenario: A到B尚未完成又选择C并继续Standard Blend

- **WHEN** 同一BlendStack节点在A到B Standard Blend期间收到C Selection且exact rule仍为StandardBlend
- **THEN** 节点 MUST按编译Policy保留或压缩当前历史并开始到C的连续过渡
- **AND** 不得要求BTSMTL重新提交A或B的Gameplay逻辑

#### Scenario: A到B尚未完成又选择C并请求惯性化

- **WHEN** 同一BlendStack节点在A到B Standard Blend期间收到C Selection且exact rule为Inertialization
- **THEN** 节点 MUST发布request并让下游consumer从当前Stack completed output建立残差
- **AND** Stack MUST不创建自己的residual accumulator

#### Scenario: Selection转为Empty

- **WHEN** AllowEmpty BlendStack从live Selection转为Empty
- **THEN** 节点 MUST只使用Standard Blend或零时长Standard Blend降低branch output
- **AND** MUST不对Empty发布Inertialization Request或使用Bind Pose伪造target

### Requirement: Blend Policy必须按节点物化完整transition

每个BlendStack节点 MUST引用唯一`CharacterAnimationBlendPolicy`。Compiler MUST枚举该节点全部可达Selection endpoint，将authoring default与exact override物化为完整source-target/Empty table，并把`StandardBlend | Inertialization`、duration、canonical curve、dense Blend Profile与request route编入Projection。Standard Blend duration为0 MUST表达硬切；Stored Pose MUST只从Stack Policy编译；Inertialization target MUST为合法Pose且必须拥有唯一compiled consumer route。Runtime MUST只按稳定identity exact lookup；缺失pair、重复override、未知source、非法Blend Logic、缺失route或Rig不匹配 MUST失败且不得fallback。

#### Scenario: Action Stack缺少Attack到Empty规则

- **WHEN** Compiler发现Action BlendStack可达Attack与Empty但没有可物化的合法pair
- **THEN** Projection Build MUST失败并定位该BlendStack节点和endpoint

#### Scenario: Action Stack配置Attack到Dodge惯性化

- **WHEN** exact override将Attack到Dodge配置为Inertialization
- **THEN** Compiler MUST写入Blend Logic、duration、profile和唯一consumer route
- **AND** MUST不要求Inertialization Policy复制该source-target pair

#### Scenario: 作者配置Stored Pose作为Blend Logic

- **WHEN** authoring payload试图把Stored Pose写为transition技术
- **THEN** Policy validation MUST失败
- **AND** MUST不把它转换为Standard Blend或Inertialization

### Requirement: Selection Preview必须执行正式Pose Plan

Timeline Preview与Motion Matching Query Fixture MUST把Editor输入降低为正式Animation Selection并执行匹配Projection的`CharacterPresentationPosePlan`。直接Player且没有Inertialization rule时Preview MUST显示硬切；BlendStack exact rule为Standard Blend时 MUST复用正式entry、CrossFade clock与Stored语义；兼容Player或BlendStack exact rule为Inertialization时 MUST通过正式compiled route激活下游consumer并复用history、residual、source release与rebase。Preview不得创建简化player、固定per-slot Stack、全局request bus、隐藏Inertialization、临时PlayableGraph或Animancer direct Play路径。

#### Scenario: Timeline Preview seek到另一个producer

- **WHEN** 作者在Preview中非连续seek
- **THEN** Preview MUST向正式Player与Inertialization consumer传播typed reset
- **AND** MUST不为了平滑预览而后台插入BlendStack或伪造request

#### Scenario: Preview执行Action惯性切换

- **WHEN** Action BlendStack exact rule选择Inertialization
- **THEN** Preview MUST通过正式request route激活Action consumer
- **AND** MUST显示匹配Runtime的capture、release和residual状态
