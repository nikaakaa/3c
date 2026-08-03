# Change: 重构惯性混合为局部 Pose 节点

## Why

当前工作区已经实现可用的惯性残差算法：它从上一份完成姿势与新目标姿势计算每骨骼位置、旋转、缩放及速度残差，再按曲线和逐骨骼时长把残差衰减到零；连续中断会从当前修正结果重新建立残差。问题不在算法，而在所有权位置。

现有实现把`Inertial`与`CrossFade`、Stored Pose、source retention一起放进隐藏的per-PoseSlot Blend Stack。`refactor-animation-selection-pose-graph-boundary`虽然准备把Blend Stack迁成显式Player节点，但仍规定Blend Stack内部同时拥有CrossFade和Inertial。这会继续产生三个业务问题：

- 作者只能对“某个Selection播放器”做惯性化，不能明确选择Locomotion分支、上半身Action分支或某个局部Pose结果的惯性边界。
- Inertial不需要继续求值旧source，Blend Stack却以多播放器历史、Stored Pose和source retention为核心；把两者合并会让MM pose jump承担不需要的Stack状态和配置。
- 惯性化位于Layer、Additive、Modify Bone与Foot Placement之前还是之后会直接改变画面结果，若算法藏在Player内部，Pose Graph无法解释最终处理顺序。

本change把惯性化改为显式、局部、单输入的`Inertialization` Pose节点。Player只报告“输入姿势发生了哪一次离散切换”这一事实；`Inertialization`节点只处理直接连接到自己的Pose流，独立保存上一份完成输出、速度历史、残差与衰减时钟。Blend Stack只保留需要同时维护多份source的CrossFade、Stored Pose、容量压缩和exact release，不再执行惯性残差。

## What Changes

- Pose Graph新增`Inertialization`节点。节点消费一条Pose Value，输出一条Pose Value，只影响该局部分支，不成为角色级全局后处理器。
- `SelectedPosePlayer`在Selection identity或generation变化时输出普通新source Pose，并附带只读`PoseDiscontinuity`事实；它不计算残差、权重或惯性时钟。
- `PoseDiscontinuity`只表达稳定event identity、前后source endpoint、原因、输入continuity和reset语义，不携带Gameplay State、Action、transition weight或旧Pose副本。
- 每个`Inertialization`节点引用唯一`CharacterPoseInertializationPolicy`。Compiler枚举该节点直接上游Player的全部可达source pair，把`Inertialize | HardCut`、duration、canonical curve、dense per-bone Blend Profile和typed parameter filter物化为完整exact table；Runtime不得使用默认fallback。
- 第一阶段只允许`SelectedPosePlayer -> Inertialization`直接局部边界。节点不得跨`BlendPose`、`LayeredBoneBlend`、`AdditivePose`、`ModifyBone`、Pose Subgraph边界隐式接收其它分支请求，也不得在OutputPose前注册全局request bus。
- `Inertialization`从自己上一份exact completed输出与当前输入计算残差。位置和缩放使用向量残差，旋转使用Quaternion最短弧Log/Exp，速度使用相邻完成帧按真实Presentation delta求导。
- 活跃惯性期间再次收到合法discontinuity时，节点从上一份已修正完成输出相对新目标原子rebase，替换旧Accumulator；不得叠加多个残差、恢复旧source或创建私有Blend Stack。
- 节点只在`Pose -> Pose`且前后均为合法Pose时执行惯性化。Initialization、Body/Presentation Reset、branch replacement、非连续Preview seek、Invalid或NoPose边界必须清理历史并按typed规则硬切或传播状态；不得用Bind Pose、上一帧缓存或Empty伪造目标。
- source-local连续参数按Policy逐项声明`Inertialize | Snap`。Foot Feature沿节点实际每脚骨骼包络连续传播，但Inertial残差不得伪装成Animation producer、AnimationPoseSourceId或Gameplay contact。
- Inertialization只能位于编译Pose Plan的native Pose阶段，必须早于world-aware Foot Placement与最终IK Solver；Compiler拒绝把节点放在Foot Placement之后。
- `BlendStack`删除`AnimationBlendTechnique.Inertial`、Inertial accumulator、Inertial transition rule和相关workspace；只保留CrossFade、Stored Pose、多source clock、capacity与source retirement。
- 现有Inertial数学、NativeArray workspace、Quaternion Log/Exp和连续rebase实现迁入唯一`PoseInertializationRuntime/Job`，不得复制一份新算法后保留旧Stack算法。
- Timeline Preview、MM Query Fixture、Live Debug与正式Runtime复用同一个编译节点实例语义，并按PoseNodeId显示discontinuity、capture/rebase、残差、逐骨骼包络、完成与reset原因。
- Corin目标图只在确有业务需要的局部分支放置节点；不得在OutputPose前自动补建角色级全局Inertialization。

## Capabilities

### New Capabilities

- `character-pose-inertialization`：定义局部Inertialization Pose节点、discontinuity事实、exact policy、每骨骼残差、连续rebase、参数/脚特征传播、reset、执行阶段与诊断合同。

### Modified Capabilities

- `character-animation-selection-runtime`：Player只输出当前source Pose与离散切换事实；Blend Stack不再拥有Inertial算法。
- `character-animation-layer-runtime`：把时间连续性拆成多source Blend Stack与单Pose局部Inertialization两个显式节点职责。
- `character-animation-presentation-authoring`：增加Inertialization节点与Policy作者边界，删除Blend Policy中的Inertial technique。
- `character-animation-pipeline`：把局部Inertialization Job纳入唯一compiled Pose Plan与同一次PlayableGraph Evaluate。
- `character-foot-placement-presentation`：Foot Placement只消费局部惯性化完成后的最终Pose与每脚特征，不读取旧Stack Inertial contribution。
- `btsmtl-timeline-editor-preview`：Preview按图上局部节点复用正式capture/rebase，不自动全局平滑。

## Dependencies And Sequencing

- 硬依赖`refactor-animation-selection-pose-graph-boundary`建立Animation Selection、SelectedPosePlayer、显式Pose Graph Player与compiled Pose Plan。两个change应作为同一迁移序列实施；不得先把“Inertial仍属于BlendStack”的旧目标完整落地后再保留兼容转换。
- `refactor-animation-playback-to-blend-stack`继续提供已完成的残差数学、历史页、Native workspace和rebase实现作为迁移输入，但它的per-slot Inertial owner、`AnimationBlendTechnique.Inertial`与Stack snapshot合同由本change删除。
- `add-character-presentation-pose-graph`负责通用节点编辑、validator、compiler、native workspace、source map和Corin最终图；本change只增加局部惯性节点及其唯一算法所有权。
- `add-character-motion-matching-pose-source`与`refactor-motion-matching-presentation-module`继续只输出Selection。MM推荐图为`MotionMatchingSelectionInput -> SelectedPosePlayer -> Inertialization`；MM不得拥有私有惯性器、fade或Stack。
- `refactor-timeline-animation-authoring-boundary`的Preview必须消费同一compiled Pose Plan。Timeline、State edge和Program不得保存Inertial duration或残差状态。
- 实施时必须同步修改上述active change中的旧口径和未完成任务，避免多个change继续声明Inertial属于BlendStack。

## Current Spec Comparison

- current `character-animation-layer-runtime`规定每PoseSlot Blend Stack唯一拥有Stored/Inertial。本change删除该绑定：Blend Stack只拥有多source历史，Inertialization节点独占单Pose残差历史。
- current `character-animation-presentation-authoring`规定Blend Library transition rule显式选择CrossFade或Inertial。本change把CrossFade保留在Blend Policy，把Inertial规则迁到具体Inertialization节点引用的Policy；两者不得共享一张同时决定两种算法的matrix。
- current `character-animation-pipeline`仍描述`ResolvedAnimationPoseRequest -> PoseSlot -> Blend Stack`固定链；依赖change先升级为Selection与显式Pose Plan，本change再把局部Inertialization作为正式Pose节点插入唯一计划。
- current `character-foot-placement-presentation`从slot内live/Stored/Inertial aggregate读取最终每脚输入。本change改为从完整Pose Plan的节点贡献读取，Inertialization只发布局部节点贡献和连续Foot Feature，不再存在Stack Inertial伪贡献。
- current `btsmtl-timeline-editor-preview`要求每PoseSlot固定Stack。本change与选择边界change共同改为按作者图执行；没有Inertialization节点时Preview必须显示真实硬切或CrossFade结果。
- active `refactor-animation-selection-pose-graph-boundary`仍把CrossFade/Inertial/Stored全部列为Blend Stack职责，与本change直接冲突，必须在实施前同步改写proposal、design、tasks和spec delta。
- active `add-character-presentation-pose-graph`、`refactor-animation-playback-to-blend-stack`与`add-character-motion-matching-pose-source`仍有多处旧Inertial owner描述，必须随本change清理，不能把archive历史当作当前目标。
- `openspec/project.md`仍以固定per-slot Blend Stack拥有Inertial为当前工作区口径；本change完成后必须改为显式Player、局部Inertialization与完整Pose Plan。

## Business Tradeoffs

### 采用局部单Pose节点

- 收益：Locomotion、上半身Action和其它分支可以独立决定是否惯性化以及处理顺序；MM pose jump无需保留旧播放器，频繁选择的CPU和source lifetime更清楚。
- 代价：作者必须显式放置节点并配置完整pair policy；图中漏放会直接显示硬切，不再由隐藏Stack掩盖错误。

### Blend Stack只保留CrossFade与Stored Pose

- 收益：Blend Stack重新成为“需要同时观察多个动画source”的深模块，Marker共同可见期、source retention和Stored压缩都有明确业务原因。
- 代价：原有Stack Inertial实现要进行破坏性迁移，snapshot、Projection payload、Profile和Corin资产都必须重建。

### 第一阶段只允许直接Player局部边界

- 收益：不引入UE式全图request bus，不会把上半身切换请求错误地应用到整个身体；Compiler可以静态证明请求来源和节点作用域。
- 代价：暂不支持对任意Layered/Additive合成结果统一惯性化；如果以后出现明确角色业务，需要另行扩展typed composite discontinuity，而不是放宽为隐藏传播。

## Breaking Changes

- 删除`AnimationBlendTechnique.Inertial`及所有Blend Stack Inertial rule、payload、state、workspace、snapshot和diagnostic字段。
- 删除Blend Stack从已完成slot pose捕获Inertial residual的入口。
- 新增`CharacterPoseNodeKind.Inertialization`、对应typed port、compiled operation、Policy payload、runtime state与source map。
- `PoseValue`增加版本化`PoseDiscontinuity`事实；旧Pose Value schema和Projection不得兼容读取。
- `CharacterAnimationBlendPolicy`只允许CrossFade/Stored配置；已有Inertial override必须迁移为具体节点的`CharacterPoseInertializationPolicy`，不提供自动runtime converter。
- MM、Timeline和普通Player不得直接调用旧Stack Inertial push；它们只产生Selection和普通Player discontinuity。
- Corin与独立MM验证图必须重新编译；缺少必须Policy、pair、Rig或Profile时Build直接失败。

## Non-Goals

- 不实现全角色全局Inertialization节点自动注入。
- 不实现跨Graph branch传播和合并多个惯性请求的request bus。
- 不实现弹簧频率、阻尼比、Half-Life或每轴独立物理模型。
- 不实现Inertial到Empty、Invalid或Bind Pose的伪目标。
- 不修改Gameplay MotionCurve、Body、WorldSolver、root motion权威或网络协议。
- 不让Animancer、Animator、Timeline、MM搜索器或Foot Placement拥有第二套惯性算法。

## 后续动画职责重构关系

本change建立的branch-local Inertialization节点、PoseDiscontinuity、residual、capture/release和无隐藏全局惯性器规则继续作为唯一能力。后续`refactor-animation-control-boundaries`只让PoseState Transition与AnimationSlot通过Transition Routing提交typed request，不得把惯性数学复制进StateMachine或Slot。若本change晚于该后续change归档，归档时 MUST保留PoseState/Slot接入语义，不得恢复仅支持旧BaseLocomotion Selection边界的描述。
