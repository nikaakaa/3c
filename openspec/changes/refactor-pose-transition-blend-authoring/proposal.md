# Change: 重构Pose Transition混合配置与曲线编辑

## Why

当前Pose StateMachine的Transition Details虽然显示`Blend Logic`、`Duration`、`Blend Curve`和`Blend Profile`，但作者无法完成一条真实可控的混合配置：

- `Blend Curve`与`Blend Profile`只是可自由输入的identity字符串，UI没有强类型资产选择、曲线编辑入口或可解析目录；界面中的`linear`、`uniform`不能证明指向任何正式数学数据。
- Standard Blend运行时只使用`elapsed / duration`计算统一权重，完全不读取Transition保存的curve/profile。
- Inertialization编译器读取Transition的duration，却从下游`CharacterPoseInertializationPolicy`替换curve/profile；同一条Transition的数学配置因此有两个作者owner。
- `CharacterAnimationBlendCurve`虽然保存Hermite key，但没有可用Inspector或Graph Details控件；“数据类型存在”不能等于“作者能够编辑”。
- 当前内置默认key的两端切线为0，实际是Ease In Out形状，却被Corin Transition字符串标成`linear`，显示语义和实际执行不一致。

现行spec已经要求PoseState Transition edge保存Blend Logic、duration、curve与Blend Profile，并要求Standard Blend与Inertialization进入固定Routing Plan。当前代码没有兑现该合同。本change不新增另一套动画系统，而是把现有声明、UI、Document、Compiler和Runtime收敛为一条可操作链。

## What Changes

- 引入统一Transition blend authoring模型：
  - `Blend Logic`只保留`Standard Blend | Inertialization`；Duration为0的Standard Blend继续明确表示Hard Cut。
  - 新增`Blend Mode`，正式提供`Linear`、`Ease In`、`Ease Out`、`Ease In Out`与`Custom`。
  - `Custom`必须引用强类型`CharacterAnimationBlendCurveAsset`；其它模式由Compiler生成唯一canonical curve，不能同时保存未生效Custom数据。
  - `Blend Profile`直接引用强类型`CharacterAnimationBlendProfile`资产；显式Uniform Profile仍是正式配置，不增加空引用默认值。
- 提供真正可编辑的Custom Blend Curve资产：
  - 资产以Unity CurveField提供关键帧与切线编辑，固定时间和值域为`[0,1]`。
  - 正式验证要求首尾为`(0,0)`与`(1,1)`、时间严格递增、值单调不降、数值有限且曲线不越界。
  - Build把作者曲线编译为现有target-neutral canonical Hermite segments；Runtime、Projection和Native Program不保存Unity `AnimationCurve`或Unity对象。
  - Slot、显式BlendStack与直接Player Inertialization Policy同步使用同一`Blend Mode + Custom Curve Asset`合同，删除各Policy中的第二种inline key作者格式。
- 重做共享StateMachine Transition Details：
  - 使用Capability声明的强类型资产字段，而不是文本框。
  - 只有`Blend Mode = Custom`时显示Custom Curve；其它模式不显示无效字段。
  - Gameplay StateMachine继续不显示Pose blend字段。
  - 每次修改只提交唯一typed Presentation Mutation、Undo、dirty与Projection stale，不自动Compile或Build。
- 贯通唯一编译和运行链：
  - Projection Compiler把每条edge的Mode/Custom Curve与Blend Profile编译为canonical curve index与dense per-bone profile index。
  - Standard Blend按每骨骼duration multiplier和canonical curve计算target权重，不再固定线性uniform。
  - Inertialization继续由显式下游Inertialization节点执行，但duration、curve与Blend Profile来自触发它的Transition edge；节点Policy只拥有残差响应与Pose Parameter处理，不能覆盖edge时间数学。
  - 直接Player discontinuity没有Transition edge时，仍由该Inertialization节点的exact policy拥有时间数学；Compiler必须按上游类型证明唯一owner，不能按缺失字段fallback。
- 更新Document v3与正式资产：
  - Pose Transition JSON使用`blendLogic`、`durationSeconds`、`blendMode`、条件式`customBlendCurveAssetId`与`blendProfileAssetId`。
  - 删除可自由输入的`blendCurveId`与`blendProfileId`旧形状，不提供双读、兼容alias或旧字段fallback。
  - Asset Catalog只暴露匹配类型的Curve/Profile正式资产；UI与Document引用同一资产identity与typed Mutation。
  - Corin按当前真实执行结果迁移：Standard边保留实际线性行为，Inertialization边显式选择实际使用的曲线形状与现有完整Blend Profile；不能按旧字符串名称猜迁移结果。
- 收口Pose State进入与同步所有权：
  - 删除Transition上的`Target Reset`；State以唯一`Always Reset on Entry`决定其内部全部Player在进入时重新初始化还是保留既有播放状态。
  - 删除Sequence Player作者payload中的`Reset On Entry`；Player只执行所属State的进入命令，不能拥有第二份生命周期配置。
  - 删除Transition上的`Source Sync`作者开关；Sequence、Blend Space与Pose Source Binding继续唯一声明Sync Group、角色和Marker，Projection根据source/target共同Sync Group自动生成Source Sync Plan。
  - Transition JSON严格删除`targetResetPolicy`与`sourceSyncMode`，State JSON新增必填`alwaysResetOnEntry`；不保留旧字段reader或迁移fallback。
- 更新当前spec、`openspec/project.md`、Document当前合同与`btsmtl-agent-authoring`技能说明，删除“字符串ID已经形成正式曲线配置”的失实描述。

## Impact

- 影响Animation blend作者合同、Pose StateMachine State/Transition资产、共享Graph Authoring Capability/Details、Presentation Mutation、Document v3 Presentation codec/exporter/reconciler/validator、Projection curve/profile catalog、PoseStateMachine plan、Transition Routing adapter、Native Pose Program、Standard Blend与Inertialization运行时和诊断。
- 影响`CharacterAnimationBlendPolicy`、`CharacterPoseInertializationPolicy`中的曲线作者格式，但不改变BlendStack、AnimationSlot和Inertialization各自的运行职责。
- Corin Pose StateMachine、Blend Policy、Inertialization Policy与生成的Presentation Projection/Native Pose Program需要一次正式迁移与显式Build。
- Corin现有Sequence Player全部为`Reset On Entry=true`，因此迁移为全部State显式`Always Reset on Entry=true`；旧Transition上的Preserve/Reset差异本来已被Player级Reset覆盖，迁移不把这份失效配置伪装成业务差异。
- 不改变Gameplay StateMachine、Transition Rule Bool语义、Timeline、MotionCurve、MotionWarp、AnimationClip内部曲线、Simulation Program、Rollback状态或网络协议。
- 不把BlendStack加入StateMachine `Blend Logic`。Motion Matching/GASP式多entry连续化仍必须通过显式BlendStack节点表达；Locomotion StateMachine使用edge Standard Blend或Inertialization。
- 不自动Compile、Build、保存或因选中资产执行重操作。

## 与现行Spec及Active Change对比

- `character-animation-presentation-authoring`与`character-presentation-pose-graph`已经要求PoseState edge拥有Blend Logic与数学配置；本change补足可编辑模型，并删除当前实现中无解析字符串与下游Policy覆盖。
- `character-animation-selection-runtime`已经要求canonical curve与dense Blend Profile进入固定Plan；本change让PoseState Standard Blend首次真正消费它们，并让Inertialization消费同一edge payload。
- `character-pose-inertialization`当前只明确“直接Player endpoint pair”由节点Policy覆盖；本change保留该场景，同时新增StateMachine Transition作为时间数学owner的精确分支，并要求Compiler证明二者互斥。
- `graph-authoring-domain-framework`要求Capability声明字段值类型、可见条件和约束；当前StateMachine Details把IdentityReference降级为TextField。本change修复这一实现违约，不增加Pose专用Inspector。
- `btsmtl-agent-authoring-document-sync`要求Presentation JSON来自同一Capability并进入唯一typed Mutation；当前Transition JSON仍输出未解析curve/profile字符串。本change替换该旧形状。
- `fix-pose-state-machine-authoring-interactions`只修框选、拖动和layout闭环，不改变Transition blend语义；本change依赖它的共享StateMachine表面，不复制交互实现。
- `refactor-animation-control-boundaries`已经确定持续Locomotion由PoseStateMachine负责，并只剩MovingTurn内容收口任务；本change不恢复Timeline locomotion，不修改其Gameplay/Pose职责边界。
- 现行`character-presentation-pose-graph`仍要求Transition保存`target reset policy`与`SourceSyncMode`，但这与UE的State级`Always Reset on Entry`、source-local Sync Group/Marker归属不一致；本change删除这两个edge作者字段，并让Projection只从State与source binding编译运行计划。
