# character-animation-transition-routing Specification

## ADDED Requirements

### Requirement: 唯一Pose Plan必须复用已安装Transition Routing模块

Player、BlendStack、Pose Graph Compiler、Pose Plan Runtime、Timeline Preview与Live Diagnostics MUST复用`character-animation-transition-routing-module`已经安装的Blend Logic、exact rule compiler、Frame Input、Frame Output、typed request、generation、lifecycle、permission与reason合同。Character Animation模块 MUST不复制第二套routing enum、request payload、状态机、generation或decision算法；正式Runtime MUST不引用模块Editor Fixture。

#### Scenario: 编译角色Routing Plan

- **WHEN** Projection Compiler从角色Blend Policy降低transition rules
- **THEN** Compiler MUST调用已安装Transition Routing模块的正式Compiler
- **AND** MUST把返回的Plan identity与结构化reason写入Projection结果
- **AND** MUST不在Character Animation Compiler中重新实现exact pair算法

### Requirement: 动画Transition必须使用UE对应的Blend Logic

所有可配置动画transition MUST使用`StandardBlend`或`Inertialization`表达Blend Logic。`StandardBlend` MUST由上游Player或BlendStack执行普通时间混合；`Inertialization` MUST由上游兼容节点发布typed request并由下游显式Inertialization节点消费。零时长`StandardBlend` MUST是唯一硬切表达。Stored Pose MUST只属于BlendStack历史策略，不得成为Blend Logic。系统 MUST不安装未实现的`Custom`枚举、默认fallback或`SelectionTransitionPlayer`包装节点。

#### Scenario: Attack切换到另一个Attack

- **WHEN** FullBodyAction BlendStack exact rule将Attack A到Attack B配置为StandardBlend
- **THEN** BlendStack MUST继续采样两个source并按编译Duration、Curve与Blend Profile混合
- **AND** 下游Inertialization MUST不收到该transition的新request

#### Scenario: Attack切换到需要立即响应的动作

- **WHEN** FullBodyAction BlendStack exact rule将Attack到Dodge配置为Inertialization
- **THEN** BlendStack MUST准备Dodge target并发布typed Inertialization Request
- **AND** 残差 MUST只由下游Inertialization节点计算

#### Scenario: 作者配置硬切

- **WHEN** 作者需要一个零时长transition
- **THEN** Policy MUST使用StandardBlend且Duration为0
- **AND** MUST不保存独立HardCut Blend Logic

### Requirement: BlendStack必须分离Standard Blend、Stored Pose与Inertialization请求

每个BlendStack节点 MUST唯一拥有Standard Blend的live source顺序、CrossFade clock、Per-Bone Blend Profile、capacity、Stored Pose、Marker source usage与exact release。BlendStack MAY按compiled exact rule发布Inertialization Request，但 MUST不保存或计算Pose residual、velocity residual、衰减clock或rebase state。Stored Pose MUST只在Standard Blend历史超出容量、命中正式replace policy或执行到Empty的正式capture边界时产生；MUST不作为Gameplay transition选择。

#### Scenario: Standard Blend历史超过容量

- **WHEN** 新source push使live entry数量超过Max Active Blends且Store Blended Pose已启用
- **THEN** BlendStack MUST把溢出历史累计进唯一Stored Pose
- **AND** MUST在capture completion后release无引用source
- **AND** MUST不发布Inertialization Request

#### Scenario: Inertialization rule被选择

- **WHEN** BlendStack exact lookup返回Inertialization
- **THEN** BlendStack MUST发布request并准备新target source
- **AND** MUST不把旧live entry转换成Stack内部Inertial accumulator

### Requirement: Inertialization Request必须是target-neutral typed事实

`PoseInertializationRequest` MUST保存稳定Request Event identity、Pose Plan completion identity、producer与consumer PoseNodeId、previous/current endpoint、discontinuity或reset reason、Duration、Blend Profile索引和Parameter Filter Set索引。Request MUST不保存Pose副本、AnimationClip、Animancer state、Unity Object、Gameplay State、Action、StateMachine edge、Bone Mask、Foot IK结果或source playable所有权。普通request、reset request与不存在request MUST具有互斥且可验证的状态。

#### Scenario: BlendStack发布动作惯性请求

- **WHEN** Action BlendStack从source endpoint切换到非Empty target endpoint并选择Inertialization
- **THEN** request MUST包含两个endpoint、稳定event identity和compiled duration/profile
- **AND** consumer MUST从自己的Pose history读取上一完成输出

#### Scenario: 请求携带Gameplay对象

- **WHEN** 编译payload尝试把Action、StateMachine edge或AnimationClip引用写入request
- **THEN** Projection Build MUST失败
- **AND** Runtime MUST不接收对象解释型request

### Requirement: Pose Graph Compiler必须静态建立唯一request route

Pose Graph Compiler MUST从兼容request producer到显式Inertialization consumer建立固定`PoseInertializationRouteDescriptor`。第一阶段兼容producer MUST包括`SelectedPosePlayer`、`BlendSpacePlayer`与`BlendStack`。每个包含Inertialization rule的producer MUST解析到一个唯一consumer；缺少consumer、多个consumer、consumer位于上游、跨越request不透明节点、跨FootPlacement或Output、Rig或Pose scope不一致、consumer缺少Policy时 MUST构建失败。Runtime MUST只读取compiled operation index，不得注册全局request bus、按名称搜索consumer或自动注入Inertialization。

#### Scenario: Action BlendStack直接连接Action Inertialization

- **WHEN** Action BlendStack存在Inertialization exact rule且其Pose输出进入一个显式Action Inertialization节点
- **THEN** Compiler MUST生成该producer到consumer的唯一route
- **AND** Projection MUST保存稳定operation index与scope

#### Scenario: 一个请求可以到达两个consumer

- **WHEN** Pose Graph拓扑让同一个request producer可达两个Inertialization节点
- **THEN** Compiler MUST报告ambiguous consumer
- **AND** MUST不按图顺序、距离或名称选择其中一个

#### Scenario: 请求跨过Layered Blend Per Bone

- **WHEN** 作者尝试让Action request隐式穿过Layered Blend Per Bone到达全身consumer
- **THEN** Compiler MUST报告request不透明边界
- **AND** MUST不把Action请求自动扩大为全身请求

### Requirement: Inertialization节点必须只消费request并拥有唯一残差状态

每个Inertialization节点 MUST持续记录其输入Pose的previous/current completed history。没有request时节点 MUST透传当前Pose并维护history；收到合法request时 MUST从上一份corrected completed output相对当前target Pose建立唯一position、rotation、scale和velocity residual，并按request duration、Blend Profile与consumer Policy衰减。活跃期间再次收到合法request时 MUST从当前corrected output原子rebase并替换旧accumulator。节点 MUST不读取BlendStack entry、Animation Selection、Gameplay State或作者source-target matrix。

#### Scenario: 惯性期间再次受打断

- **WHEN** consumer已有活跃residual并收到新的合法Inertialization Request
- **THEN** consumer MUST从上一份corrected completed output相对新target rebase
- **AND** MUST只保留一个accumulator

#### Scenario: 没有请求但输入Pose继续变化

- **WHEN** 上游执行Standard Blend且consumer本帧没有新request
- **THEN** consumer MUST透传当前raw input并更新completed history
- **AND** MUST不根据endpoint变化自行猜测Inertialization

#### Scenario: target为NoPose

- **WHEN** request target不是合法Pose
- **THEN** consumer MUST拒绝建立残差并执行typed reset或Invalid传播
- **AND** MUST不使用Bind Pose或上一帧Pose伪造target

### Requirement: request capture与source release必须原子提交

Inertialization transition边界 MUST在incoming target首份合法Pose、request payload、consumer route和capture plan全部准备完成后才能提交。边界帧outgoing source MAY继续作为正式Sample或HandoffReference；旧Stack entry、Marker relation和source retention MUST只在consumer capture所在completion成功后exact release。任一步失败 MUST使该frame保持typed Invalid，并且不得先释放旧source、留下半创建target、静默改用Standard Blend或推进consumer history。

#### Scenario: incoming target首帧尚未准备完成

- **WHEN** BlendStack选择Inertialization但incoming target没有合法Pose sample
- **THEN** 旧输出与Pending target identity MUST保持
- **AND** request、release和consumer accumulator MUST不提交

#### Scenario: consumer capture成功

- **WHEN** 唯一PlayableGraph completion完成target sample和consumer residual capture
- **THEN** Runtime MUST原子提交request consumption、target ownership、旧entry release和Marker detach
- **AND** 后续帧 MUST不再采样已release的旧source

### Requirement: 混合技术连续打断必须使用确定语义

Standard Blend到Standard Blend MUST继续使用BlendStack历史；Standard Blend进行中切换到Inertialization MUST从当前Stack completed output建立consumer residual并在capture后释放旧Stack历史；Inertialization到Inertialization MUST从当前corrected output rebase；Inertialization活跃期间上游开始Standard Blend时，上游 MUST从当前live target执行普通Blend，既有consumer residual MUST继续按原时钟衰减，且系统 MUST不把下游corrected Pose反馈成上游Stored Pose、不恢复已release source或创建第二accumulator。Pose到Empty MUST只使用Standard Blend。

#### Scenario: CrossFade中触发惯性切换

- **WHEN** A到B Standard Blend尚未完成且新target C选择Inertialization
- **THEN** consumer previous history MUST代表当前A/B Stack完成输出
- **AND** Stack MUST在capture completion后释放A/B旧历史

#### Scenario: 惯性残差尚未结束又开始普通混合

- **WHEN** A到B Inertialization仍活跃且B到C选择Standard Blend
- **THEN** 上游 MUST执行B到C Standard Blend
- **AND** 既有residual MUST继续衰减而不产生新request
- **AND** Runtime MUST不建立下游到上游Pose反馈环

### Requirement: request管线必须位于Foot Placement之前

所有Inertialization request producer与consumer MUST位于其作用Pose branch的Foot Placement之前。Pose composition MAY只在compiled route之外消费已经完成的consumer输出；Foot Placement MUST只消费Standard Blend或Inertialization完成后的最终合成Pose与每脚feature。Request、residual和Stored Pose MUST不修改Gameplay Body、Motion Curve、Root Motion或WorldSolver。

#### Scenario: Action与Locomotion分别惯性化

- **WHEN** Corin Locomotion和Action分支各自拥有branch-local Inertialization
- **THEN** 两个consumer MUST在Layered Blend Per Bone之前完成各自Pose
- **AND** 最终Foot Placement MUST消费两个分支合成后的结果

#### Scenario: Inertialization放在Foot Placement后面

- **WHEN** 作者把request consumer放到Foot Placement输出之后
- **THEN** Pose Graph Compiler MUST拒绝该拓扑
- **AND** MUST不把IK修正记录为动画惯性history

### Requirement: Preview与诊断必须复用正式request route

Timeline Preview、Motion Matching Query Fixture、Pose Preview和正式Runtime MUST执行同一compiled request route、request event、history capture、source release、residual与rebase语义。Live snapshot MUST能显示Blend Logic、request producer/consumer、event identity、准备/消费/completion状态、source release completion、Stack live/Stored状态、residual progress、rebase count与reset reason。Editor MUST不根据authoring default伪造Live request或创建简化dispatcher。

#### Scenario: Preview执行Action惯性切换

- **WHEN** Preview中的Action BlendStack exact rule选择Inertialization
- **THEN** Preview MUST通过正式route激活Action Inertialization
- **AND** MUST复用正式source release与rebase语义

#### Scenario: Live revision与打开的图不匹配

- **WHEN** snapshot的Pose Graph或Projection revision与作者窗口不一致
- **THEN** request route Live显示 MUST标记Stale并清空旧值
- **AND** Editor MUST不从当前Policy重建虚假Live状态
