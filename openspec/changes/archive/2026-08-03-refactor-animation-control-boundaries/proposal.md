# Change: 重构 Gameplay 与动画表现控制边界

## Why

当前 Corin 的 BTSMTL Locomotion StateMachine 同时决定 Gameplay 移动流程和具体动画 producer。`Idle`、`WalkStart`、`WalkLoop`、`RunStart`、`RunLoop`、`RunEnd`、`MovingTurn`分别运行 Timeline，Program 再把唯一`BaseLocomotion AnimationSelectionFrame`提交给 Pose Graph。Pose Graph只负责`MarkerSync -> Player/BlendStack -> Inertialization -> Composition`，没有自己的动画 StateMachine。

现有运行链已经区分Simulation Tick与Presentation Tick：Gameplay Timeline只在Simulation Tick推进并提交committed raw visual sample，表现层在相邻committed sample之间按presentation delta投影visual time、重采样动画并求值最终Pose。逻辑Sample是时间校准锚点，不是骨骼Pose，也不是要求表现层只在固定Tick更新的逐帧播放命令。本change不新增双Tick或表现插值，也不把同一BTSMTL Timeline在表现层再运行一次；它保留现有时钟语义，并把仍集中在共享Playback总管中的committed history、表现时间投影、Action生命周期、PoseState source与Pose Plan职责拆到正式Module。

这条链保证了逻辑时间、Motion、Window和动画采样使用同一 producer，但也让逻辑层知道全部 locomotion 动画资源和切换结构。替换一段起步动画、合并 Walk/Run、增加停止变体或调整表现 transition，都可能要求修改 Gameplay StateMachine、Timeline和Program producer。若在不删除该选择权的情况下直接增加 Pose StateMachine，上游和下游会同时选择 locomotion 动画，形成两套权威。

Action侧也把“动作取得运动控制权”和“动作遮挡基础姿势”绑定在`HasActionLocomotionOwnership -> ActionOverride`。全身动作期间 Locomotion停止输出，动作结束后只能由Gameplay状态显式恢复到RunLoop或Idle；这与UE中“Locomotion StateMachine持续生成基础Pose，Montage通过Slot暂时覆盖，结束后回到当前基础Pose”的职责模型不同。

本change把持续姿势选择迁入 Pose Graph，把有限Action的准入、生命周期、Timeline、Motion、Window和打断继续留在Gameplay Program。它建立一条正式数据链，不保留现有BaseLocomotion Timeline selection作为fallback或并行配置。

## What Changes

- 新增纯表现`PoseStateMachine`作者模型和编译运行时：
  - `Entry`、`State`、`Transition`和`State Alias`只存在于Pose Graph。
  - 每个State输出普通Pose Value。
  - Transition Rule只读取typed Presentation Fact，不读取BTSMTL Graph、Blackboard mutable state、Action实例或Unity对象。
  - Transition edge显式选择Standard Blend或Inertialization，并复用唯一Transition Routing模块。
- 新增纯表现`SequencePlayer`：
  - Pose State内可以直接播放Profile绑定的Animation Clip，不再要求为Idle、Start、Loop、Stop或Turn创建Gameplay Timeline producer。
  - loop、play rate、marker和source identity属于Presentation source binding。
- 新增UE口径的`Slot`表现节点：
  - Source Pose来自持续求值的Locomotion PoseStateMachine。
  - Action Timeline只提交有限动作的exact playback identity和committed raw visual time锚点。
  - `CharacterActionPlaybackRuntime`只保存Gameplay已经提交的committed raw sample history；动画表现协调链中的`ActionPresentationSampleProjector`在PresentationFrame按presentation delta投影visual time，并在新committed sample到达时按同一playback identity重基线。
  - Slot在无Action时透传Source Pose，在Action活跃时执行显式Blend Logic并覆盖或输出供Layered Blend Per Bone组合的Pose。
  - Slot不判断Action是否允许发生，不推进Timeline，不提交Motion。
- 新增唯一`CharacterPresentationFactFrame`：
  - 从committed Simulation/Body/Intent构造`Grounded`、水平速度、加速度、移动方向、朝向误差、垂直速度、稳定运动阶段等typed fact。
  - Fact只驱动Pose选择，不写回Gameplay State或World Body。
- 拆分有限Action与持续Pose source的输入ABI：
  - Gameplay Program不再为BaseLocomotion选择Idle/Walk/Run具体producer。
  - 有限Action Timeline只提交`ActionAnimationPlaybackCommand`与committed raw sample，并由`CharacterActionPlaybackRuntime`输出`ActionAnimationPlaybackFrame`。
  - SequencePlayer、BlendSpacePlayer和Motion Matching统一使用不含`AnimationPlaybackId`、`AnimationChannelId`或`ProgramProducerIndex`的state-local Pose source sample。
  - 删除把Action、Motion Matching、Blend Space和普通Pose source压进同一个`AnimationSelectionFrame`、`SelectionInput`和binding index的旧公共ABI。
- 拆分动画表现协调与有限Action Playback生命周期：
  - `CharacterAnimationPresentationRuntime`只组织一帧动画表现事务，按编译顺序推进和求值Fact、PoseState、source provider、Action Slot、Transition Routing、Pose Graph与最终发布。
  - `CharacterActionPlaybackRuntime`只消费Gameplay已经确认的有限Action playback command，保存完整playback identity、权威raw visual sample、PendingFirstSample/Selected/Retained/Retired生命周期和精确释放。
  - `AnimationSlot`唯一拥有Action与Source Pose之间的transition、weight、source usage和release permission；Playback Runtime不计算Pose混合。
  - Locomotion SequencePlayer、BlendSpacePlayer和Motion Matching state-local selection不创建`AnimationPlaybackId`，也不进入Action Playback Runtime。
  - 删除`CharacterAnimationPlaybackRuntime`旧总管命名与实现，不保留转发壳、兼容adapter或双运行路径。
- 重写动画表现帧的状态与事务：
  - Gameplay持久Action command inbox与帧内Pose request workspace分离，不再共用一种command或队列。
  - Action生命周期改为按完整playback identity保存的registry，不再按AnimationChannel反查Pose Runtime推断Retained与Retired。
  - committed raw sample history、表现时间投影与Marker effective-time state分别拥有，不把render外推时间伪装成Gameplay权威时间。
  - 整帧Fact、provider、Action、Slot、Marker、Transition与Pose Plan采用同一个begin/commit/rollback事务，失败时不得部分消费command或推进生命周期。
  - Slot usage消失、Action retirement permission、source backend物理释放与最终Retired形成带completion identity的三段握手。
- 收紧编译计划与运行Module：
  - Projection分别编译Action Playback Input plan、state-local source provider plan、AnimationSlot plan和Transition Routing plan；Runtime只装载计划，不重新调用compiler。
  - PoseState target先发布provider demand，只有首Pose ready后才提交transition；Pending保持当前合法source，Invalid阻止frame publication。
  - Slot无Action端正式建模为持续更新的`SourcePoseEndpoint`，不再用`Empty`同时表达“没有Action”和“没有Pose”。
  - `AnimationPosePlayableGraphRuntime`不接替旧Playback成为新总管；Pose Plan执行、PoseState/source provider、Action Slot与physical source registry使用分离的Module和窄接口。
- 收紧authoring、Preview与diagnostics：
  - PoseState的inline subgraph采用root-owned graph catalog与stable GraphId引用，删除Unity会递归展开的嵌套序列化结构。
  - Action producer authoring只允许有限Timeline Action；Motion Matching与Blend Space只作为PoseState内部source provider。
  - Timeline Action Preview、PoseGraph Fact Preview和MM Query Fixture使用不同输入adapter但复用同一个正式Animation Preview Runtime。
  - Action lifecycle snapshot、Pose Plan snapshot和Marker relation snapshot分别发布，再由统一只读Debug View组合。
- 原子迁移Corin：
  - 删除Locomotion StateMachine中按动画命名的Timeline播放和`ActionOverride`表现让渡。
  - 保留或重建真正影响Gameplay/Motor的移动模式、输入准入和Motion控制，不以动画名称表达。
  - 把现有Locomotion AnimationTrack、marker和资源绑定迁入Presentation source binding。
  - 把Corin Pose Graph改为`Presentation Facts -> Locomotion PoseStateMachine -> FullBodyAction Slot -> Composition -> Inertialization/FootPlacement -> Output`。
  - Attack、Dodge和后续Hit/Death等有限Action继续由BTSMTL Action StateMachine与Timeline驱动。
- 分离动作运动权与姿势覆盖：
  - Action是否覆盖Motor由Action/Motion arbitration决定。
  - Action是否覆盖全身Pose由Slot和Pose Graph topology决定。
  - 全身Action活跃时Locomotion PoseStateMachine继续按committed Body事实求值。
- 删除旧路径：
  - 删除Corin `BaseLocomotion` Timeline producer selection、对应AnimationChannel binding、selection lifecycle和Profile producer binding。
  - 删除`HasActionLocomotionOwnership -> ActionOverride -> 恢复RunLoop/Idle`表现路由。
  - 删除以Gameplay state edge表达Locomotion动画transition的资产数据。
  - 不提供旧BaseLocomotion Selection到PoseStateMachine的adapter、fallback或双写。

## Impact

- Affected specs:
  - `character-animation-control-boundary`（新增）
  - `character-animation-transition-routing-module`
  - `character-presentation-pose-graph`
  - `character-animation-selection-runtime`
  - `character-animation-layer-runtime`
  - `character-animation-presentation-authoring`
  - `character-animation-pipeline`
  - `character-pipeline-definition-authoring`
  - `character-pipeline-runtime`
  - `character-presentation-interpolation`
  - `character-state-interruption-authoring`
  - `character-animation-foot-analysis-artifact`
  - `character-foot-placement-presentation`
  - `character-equipment-presentation`
  - `btsmtl-timeline-editor-preview`
  - `agent-character-controller-synthesis`
  - `character-state-timeline-authoring-loop`
  - `character-action-authoring-closure`
  - `character-motion-semantics`
- Affected active changes:
  - 0任务的`integrate-animation-transition-routing-pipeline`以`BaseLocomotion Selection`和`FullBodyAction BlendStack`为接入拓扑，已由本change完整吸收并删除，不再保留第二份接入计划。
  - `add-character-presentation-blend-space`保留BlendSpacePlayer能力，但Corin纯Timeline演示与上游Selection假设必须改为PoseState内部source。
  - `add-character-motion-matching-pose-source`和`refactor-motion-matching-presentation-module`保留MM查询、选择、history和普通Pose source输出，但BaseLocomotion channel winner与Gameplay producer绑定必须改为PoseState内部表现provider。
  - `add-character-animation-virtual-bones`继续要求完整Pose Bone page流经Player、Slot、Stored Pose和Inertialization，不改变Bone ABI。
  - `add-character-presentation-pose-graph`、`refactor-animation-playback-to-blend-stack`与`refactor-inertial-blending-to-local-pose-node`继续提供Pose Plan、显式Player/Stack和局部Inertialization基座；本change只增加PoseState与Slot owner并删除BaseLocomotion Selection入口。
  - `upgrade-character-animation-authoring-workspace`继续提供统一Workspace、Preview、Pose Watch与Live Debug；本change在同一工作区安装真实Animation State Machine和Slot入口。
  - `refactor-timeline-animation-authoring-boundary`继续提供Timeline typed Marker/Curve与Analysis能力，但其作者范围收窄为有限Action和真实Gameplay Timeline；Locomotion Pose source改由Profile source editor拥有。
  - `refactor-agent-authoring-to-synced-json-document`提供Document package和事务基础；`refactor-pose-graph-to-btsmtl-authoring-domain`再把PoseStateMachine、Pose source、Slot和有限Action Timeline升级为受Capability约束的Document v3 typed mutation，不开放任意Presentation对象写入。
- Affected code:
  - `CharacterPoseAuthoringContracts`
  - Pose Graph authoring workspace、validator、compiler与Projection schema
  - Presentation Fact projection与runtime frame
  - Sequence source binding与source sampling backend
  - PoseStateMachine runtime、state workspace和transition runtime
  - Action Slot runtime与现有BlendStack/Transition Routing接线
  - `CharacterAnimationPlaybackRuntime`拆分为动画表现帧协调器与有限Action Playback生命周期运行时
  - Action command inbox、逐playback lifecycle registry、committed sample history与表现时间投影
  - Action-only binding index、state-local Pose source identity、provider plan与source readiness barrier
  - Pose Plan execution、PoseState/source provider、AnimationSlot与physical source registry的Module所有权
  - PoseState root-owned graph catalog、Projection typed plan与禁止runtime compile
  - Animation Preview输入adapter、Action/Pose diagnostics与Marker状态分层
  - Corin RootTree、nested Locomotion资产、Presentation Profile、Pose Graph和generated Projection
  - Animation lifecycle、diagnostics与Timeline Preview

## Business Tradeoffs

### 方案一：维持当前Gameplay精确选择全部动画

- 优点：Gameplay、Motion和动画天然共用同一个Timeline时间；网络回放和调试链最直接。
- 代价：动画资源替换会影响Gameplay图；Locomotion状态数量跟动画片段数量一起增长；动画师不能独立调整持续表现。

### 方案二：全部动画都交给PoseStateMachine

- 优点：表现层自治最大，作者体验最接近UE AnimGraph。
- 代价：Attack、Dodge、Hit等有限动作的窗口、Motion、取消和权威时间会与Gameplay分裂；项目现有Action Timeline价值被破坏。

### 方案三：持续Locomotion由PoseStateMachine选择，有限Action由Gameplay Timeline驱动

- 优点：持续表现可独立演进，有限动作仍保持权威时间、窗口和Motion闭环；对应UE的Locomotion StateMachine加Montage/Slot职责模型。
- 代价：需要新增Presentation Fact、PoseStateMachine、SequencePlayer和Slot，并原子迁移现有Corin Locomotion。

本change采用方案三。

### Playback Runtime边界

#### 方案一：保留共享Animation Playback Runtime作为全部动画总调度

- 优点：现有`Present()`调用链和Preview复用改动较少。
- 代价：类名继续暗示Locomotion、Motion Matching与Action共享playback身份；Action生命周期、PoseState推进、source采样和最终Pose求值仍集中在同一对象，后续扩展UpperBody Slot或多个Pose State provider时容易重新形成隐藏总管。

#### 方案二：删除Playback Runtime并让Slot直接消费Gameplay command

- 优点：对象数量最少，Action到Slot接线最短。
- 代价：Slot会同时拥有Gameplay command顺序、权威visual time、PendingFirstSample、纯表现retention和Pose transition，无法独立复用Timeline Preview，也会把有限动作实例寿命重新塞进Pose节点。

#### 方案三：独立Animation Presentation协调器与Action Playback Runtime

- 优点：对应UE中`AnimInstance/AnimInstanceProxy`的整帧求值边界与`Montage Instance`的有限播放实例边界；Locomotion Player只按PoseState relevance存在，Action实例仍能和Timeline、Window、Motion、Cue保持同一identity与权威时间；Slot只处理Pose插入和释放许可。
- 代价：必须拆分现有`CharacterAnimationPlaybackRuntime`的字段、workspace、Preview入口、diagnostics与释放调用链，并一次性更新所有调用方。

本change采用方案三。这里的对应只用于职责学习，不引入完整`UAnimMontage`资产、Section系统或UE运行时依赖。

## Dependencies And Sequencing

- `add-animation-transition-routing-module`的实现已经完成，本change直接把它作为PoseStateMachine transition与AnimationSlot内部共用的底层服务接入。独立Fixture不是实施前置验收；新系统的正式Pose闭环同时覆盖真实接入。为保证OpenSpec delta合并顺序，最终归档时只需先归档模块capability，再归档本change。
- `add-character-presentation-pose-graph`、`refactor-animation-playback-to-blend-stack`、`refactor-inertial-blending-to-local-pose-node`、`upgrade-character-animation-authoring-workspace`与`refactor-timeline-animation-authoring-boundary`若尚未归档，必须先按各自完成态合入current specs，或者在晚归档时按本change已安装模型重基线；不得用旧delta覆盖PoseState、Slot或Profile Pose source口径。
- `integrate-animation-transition-routing-pipeline`已经删除，其未实施任务不得恢复。
- `add-character-presentation-blend-space`必须直接消费本change安装的state-local Pose source ABI和PoseState readiness，不得继续引用旧`CharacterAnimationPlaybackRuntime`、Gameplay producer、SelectionInput或`AnimationPlaybackId`。
- `refactor-motion-matching-presentation-module`必须把唯一外层协调器改为`CharacterAnimationPresentationRuntime`，并只通过PoseState relevance、state-local selection batch和Pose completion与MM Module交换数据。
- `add-character-motion-matching-pose-source`必须删除Program-owned MM `AnimationPlaybackId`、`AnimationChannelId`与`ProgramProducerIndex`结论，直接读取本change安装的state-local source identity与provider plan。
- `refactor-agent-authoring-to-synced-json-document`先完成Document v2基础闭包；`refactor-pose-graph-to-btsmtl-authoring-domain`再统一拥有Document v3 typed字段、Capability Catalog、共享UI、Pose IR与Presentation mutation边界，本change不得维护平行Document schema或Graph节点switch。
- 本change的代码、Compiler、Runtime、Preview、diagnostics和旧路径清理完成后，先实施`add-action-animation-authoring-workspace`；Action Workspace稳定后，20–23、27.1–27.5、27.13–27.15、28与Pose Graph重构的Corin资产任务合并为一次Document v3迁移。
- 迁移后只执行一次精确Definition Build，发布同源Float32、Fixed与Projection；唯一串行关系见`openspec/character-pipeline-serial-execution.md`。

## 2026-08-01 MovingTurn相反方向交接闭环

- `CharacterInputProfile`为Vector2输入显式声明数字方向冲突策略；策略属于输入作者配置，不由Gameplay StateMachine或UI推断。
- Corin `MoveAxis`采用“最近激活方向胜出”：W仍按住时按下S必须直接得到向后输入，A/D同理；全部方向键松开仍必须得到零输入。
- Float32与Fixed Unity Input Adapter在本地设备采样边界执行同一解析，再把结果写入portable `CharacterSimulationInput`。Rollback、网络和Program只消费已经解析的正式输入值，不增加snapshot字段或第二输入合同。
- 不通过降低MovingTurn角度门槛、增加RunEnd宽限、复制RunEnd到MovingTurn转场或按固定方向优先级掩盖相反键重叠。

## 2026-08-02 MovingTurn连续CrossFade抢占闭环

- Standard Blend开始后target立即成为Pose StateMachine逻辑active State，source与target继续共同采样到完成或被替换。
- active transition期间从target出边消费最新Presentation Fact；连续MovingTurn复用既有Transition Routing与Inertialization从当前最终Pose接管。
- 保持MovingTurn 28帧Gameplay根运动窗口、71帧Pose Clip正文与0.3秒退出CrossFade职责分离，不新增Gameplay冷却、自循环边、第二混合栈或协议字段。
