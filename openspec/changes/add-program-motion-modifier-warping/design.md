# Design: Program Motion Modifier 与 Motion Warping

## Context

当前Motion运行时已经具备正确的前半段和后半段：

```text
Locomotion operation -----------\
Timeline MotionCurve operation --+-> MotionContribution accumulator
GameplayResult operation -------/             |
                                                v
                                  CharacterMotionRequest
                                                |
                                                v
                                        WorldSolver batch
                                                |
                                                v
                                      actual Body Result
```

`Float32MotionAccumulator.Resolve()`与`FixedMotionAccumulator.Resolve()`在同一个方法中完成：

1. 按固定channel扫描contribution。
2. 按Additive、WeightedBlend和Override解析channel。
3. 按ConsumeLowerChannels合成低层结果。
4. 计算velocity并构造`CharacterMotionRequest`。

这使“仲裁后的Gameplay motion还需要经过目标约束修正”没有合法插槽。

仓库中的`MotionWarpTrack`当前自行按Timeline时间采样为`TimelineMotionWarpWindow`，但没有Semantic emitter、Program operation或runtime consumer。它既不是authoring-only数据，也不是正式runtime能力，必须整体替换，不能在其上补一个旁路consumer。

Action链已经提供目标快照：

```text
Blackboard ActionTargetSnapshot
  -> ActivateActionInstance
  -> ActionInstance.TargetSnapshot
  -> Float32 / Fixed committed Character State
```

MotionWarp应消费该不可变业务事实，而不是重新寻找目标。

## Goals

- 恢复“提交、统一仲裁、统一后处理、唯一执行者”的Motion管线。
- 让MotionWarp成为Program内可编译、可回放、可快照的Gameplay行为。
- 保持MotionCurve为动画派生原始位移的唯一事实来源。
- 保持WorldSolver为碰撞和实际Body移动的唯一权威。
- 让Float32与Fixed执行同一Warp业务规则。
- 让Timeline作者只配置源曲线、目标姿态和修正进度，不接触Solver或网络模型。
- 删除已有半接入Warp路径和自由字符串目标策略。

## Non-Goals

- 不负责如何选中敌人或生成ActionTargetSnapshot。
- 不在ActionRuntime中实现Motion算法。
- 不让Timeline直接写Body/Transform。
- 不让Solver识别Action、Timeline或MotionWarpClip。
- 不让Presentation修正Gameplay位置。
- 不做持续追踪移动目标。
- 不做垂直运动、攀爬、翻越、IK或Motion Matching。

## Decision 1: Modifier属于Target Motion域，不是Session Pass

### Decision

正式阶段固定为：

```text
Collect Contributions
  -> Resolve Locomotion Channel
  -> Resolve Action Channel
  -> Apply Action Channel Modifiers
  -> Resolve GameplayResult Channel
  -> Apply GameplayResult Channel Modifiers
  -> Compose Channels
  -> Build CharacterMotionRequest
```

第一版只有Action channel的MotionWarp modifier。Pipeline仍保留通用的固定Modifier阶段，但不增加runtime注册表、反射发现或按Network Model选择的handler。

Modifier输入是结构化`ResolvedMotionChannel`，至少包含：

```text
Channel
Displacement
Yaw
HasDelta
ClaimsLowerChannels
ResolvedOwnerSource
ParticipatingSources
ActionContext
```

Modifier输出是同一channel的修正结果和结构化trace。它不能直接提交第二份`MotionContribution`，否则会再次进入priority仲裁并混淆“谁产生动作位移”和“谁修正动作位移”。

### Tradeoff

- 收益：贡献仲裁和轨迹修正职责清楚，所有Solver与网络模型共享结果。
- 代价：Float32/Fixed Motion accumulator需要拆分为多个明确步骤。
- 不选择Session级Pass：Session只应调度actor evaluate和world batch，不应理解Action channel或Timeline source。
- 不选择Solver extension：Unity、KCC、DotRecast将被迫复制动作规则，新增Solver会修改Gameplay语义。
- 不选择“Warp再提交一个高priority contribution”：它把Modifier伪装成竞争来源，无法可靠关联原始MotionCurve，也会让priority成为后处理顺序。

## Decision 2: Modifier顺序由Operation Set固定

### Decision

Operation Set声明支持的Modifier operation及canonical顺序。Program layout在composition时建立：

```text
MotionModifierRange[channel]
MotionModifierDescriptor[]
  OperationIndex
  SourceMotionOperationIndex
  TimelineOwner
  ActionContextOwner
  StateSlotRange
```

运行时只遍历当前channel已编译descriptor span。新增Modifier类型需要显式升级Operation Set并为Float32/Fixed提供实现，不允许动态assembly扫描、字符串handler、ScriptableObject resolver或network-specific注册。

### Tradeoff

- 收益：执行顺序进入Program identity，Fixed replay不会受加载顺序影响。
- 代价：新增Modifier需要版本升级，而不是运行时即插即用。
- 这是Gameplay规则，不是插件UI。可扩展性的边界是“新增operation而不修改Solver/Session/Graph runtime”，不是让任意代码在运行时插入动作位移。

## Decision 3: MotionWarp显式绑定一个Action MotionCurve

### Decision

`MotionWarpClip`保存源`MotionCurveClip`的稳定authoring identity。Editor只允许从同一Timeline中选择合法source。Compiler将它解析为`ProgramReferenceKind.MotionSource`或等价typed reference，Target lowering转成source operation index。

第一版source必须满足：

- 类型为`MotionCurveClip`。
- 与Warp属于同一Timeline owner。
- channel为`Action`。
- blend mode为`Override`。
- Warp起止帧位于source的`StartFrame..CurveEndFrame`内。
- source在该区间具有唯一Action Context。
- 同一source的Warp窗口不重叠。

Runtime只在source是当前Action channel resolved owner时应用Warp。若source因更高priority的Action motion失去仲裁，Warp也不执行；它不能修正另一个winner。

### Tradeoff

- 收益：作者能明确回答“修哪段位移”，重排Track/Clip后identity仍稳定。
- 代价：第一版不支持对多个Additive source的合成结果整体Warp。
- 不选择时间重叠推断：同一时间可有多个MotionCurve，推断结果会随编辑顺序变化。
- 不选择CurveId：CurveId描述曲线业务资源，不等于Timeline中某次使用的clip identity。
- 不选择全Action channel无条件Warp：会把受击、附加位移或未来其它Action motion一起拉向目标。

## Decision 4: MotionWarpClip只表达累计目标修正

### Authoring model

旧字段和采样入口整体替换为：

```text
MotionWarpClip
  SourceMotionClipId
  PositionMode = Disabled | MatchTargetPlanarPosition
  RotationMode = Disabled | FaceTarget | MatchTargetYaw
  TargetLocalPlanarOffset
  TargetYawOffsetDegrees
  PositionWeight [0, 1]
  YawWeight [0, 1]
  MaxTotalPositionCorrection >= 0
  MaxTotalYawCorrectionDegrees [0, 180]
  PositionProgressCurve
  YawProgressCurve
```

两条ProgressCurve都是normalized cumulative curve，要求：

- key/value有限。
- 时间域为`[0, 1]`。
- 首值为0，末值为1。
- 单调不下降。
- 采样结果clamp到`[0, 1]`。

MotionWarpClip不再使用Base Clip ease-in/ease-out作为Gameplay修正权重，也不支持Mixable。窗口本身决定何时修正，ProgressCurve决定修正如何分布，Weight决定总修正比例。这样同一个结果没有三套叠乘曲线。

### Pose target

目标平面位置：

```text
DesiredPosition = TargetSnapshot.Position
                + Rotate(TargetSnapshot.Yaw, TargetLocalPlanarOffset)
```

目标yaw：

- `Disabled`：不修正yaw。
- `MatchTargetYaw`：`TargetSnapshot.Yaw + TargetYawOffsetDegrees`。
- `FaceTarget`：从DesiredPosition指向TargetSnapshot.Position的平面朝向，再加TargetYawOffsetDegrees。若方向长度为0则配置/运行失败，不猜当前yaw。

第一版忽略目标和源轨迹的Y分量，只修正XZ与yaw。原始MotionCurve的Y仍可照常进入Solver。

### Tradeoff

- 收益：作者能独立调整“位置什么时候追上”和“朝向什么时候转完”，并有明确安全上限。
- 代价：字段比旧WeightCurve多，但每个字段只负责一种可观察业务含义。
- 不保留旧WeightCurve + EaseIn + EaseOut：三条曲线共同决定一个权重，无法直观看出最终累计修正。
- 不支持vertical warp：垂直位移通常需要台阶、跳跃、攀爬与碰撞能力共同设计，放进第一版会扩大Solver合同。

## Decision 5: 窗口进入时固定目标和总修正

### Decision

Warp首次进入有效窗口时读取：

- committed Body position/yaw。
- 对应ActionInstance的immutable target snapshot。
- source MotionCurve在窗口起点到CurveEndFrame的剩余累计root transform。
- authoring目标模式、offset、weight和clamp。

随后计算并保存：

```text
WindowStartBodyPose
NominalAuthoredEndPose
DesiredTargetPose
ClampedTotalPositionCorrection
ClampedTotalYawCorrection
LastPositionProgress
LastYawProgress
PlaybackGeneration
ActionInstanceId
```

每Tick：

```text
positionDelta = TotalPositionCorrection * (currentPositionProgress - lastPositionProgress)
yawDelta      = TotalYawCorrection      * (currentYawProgress - lastYawProgress)
warpedAction  = resolvedAction + correctionDelta
```

同一窗口不根据target live Transform重算。Source Timeline stop、Action terminal、playback generation变化、seek或cycle reset必须按同一typed lifecycle清理或重建状态。

### Tradeoff

- 收益：输入完全来自Program state，Float32/Fixed、server authority和rollback可重放。
- 代价：目标在动作开始后移动时，本次动作仍朝捕获位置完成。
- 不选择每render frame追踪：Gameplay位移属于logic tick，render target会导致服务器和客户端结果不同。
- 不选择每Tick查scene target：它把target registry和Unity对象引入portable Kernel。
- 后续若业务需要追踪目标，应另行增加“何时更新ActionTargetSnapshot”的Gameplay规则，而不是让Warp私自读取live对象。

## Decision 6: 目标要求属于Action admission

### Problem

如果Warp直到Timeline采样时才发现目标为空，动作已经成功激活、动画已经开始，系统只能静默不Warp或中途报错。两者都不是完整业务语义。

### Decision

将ActionProfile的自由字符串`TargetPolicy`替换为：

```text
ActionTargetRequirement
  None
  SnapshotRequired
```

Compiler把该字段写入Action catalog。唯一portable admission evaluator把candidate target snapshot纳入request，并在`SnapshotRequired`且`HasTarget=false`时返回稳定typed reason `TargetSnapshotRequired`。

`CanActivateActionInfoNode`与`ActivateActionInstanceNode`必须读取同一Blackboard snapshot declaration或显式None，调用同一evaluator。二者不能一个只看profile、另一个再读target。

MotionWarp source所在ActionProfile必须声明`SnapshotRequired`。Compiler验证所有可能启动该Timeline的call site，不允许Warp依赖未声明目标的动作。

### Tradeoff

- 收益：动作是否能开始在Transition/AI decision阶段已经明确，不会播到一半才发现目标缺失。
- 代价：`CanActivateActionInfoNode`需要增加与Activate相同的target snapshot引用，相关Corin无目标动作要显式保持`None`。
- 不选择WarpClip自己的`TargetKey`：Action已经拥有目标快照，再保存一份key会产生两个目标真值。
- 不选择“缺目标时不修正”：作者配置了Warp却得到普通Root Motion，错误难以发现且网络端表现不一致。

## Decision 7: 跨TickWarp状态进入Program State

### Decision

MotionWarp operation声明固定typed state slot。Float32与Fixed分别以自身Numeric Target类型保存同一逻辑字段，State codec、Snapshot、StateHash和rollback history全部覆盖。

以下数据必须持久：

- playback/action/window generation。
- 窗口起始body pose。
- 计算后的总position/yaw correction。
- 上一次累计progress或已应用累计correction。
- active/initialized标志和source operation identity索引。

以下数据仍是同Step transient：

- raw contributions。
- resolved channel scratch。
- 当前modifier output。
- final CharacterMotionRequest。

### Tradeoff

- 收益：replay、seek和rollback不会重复应用或漏应用修正。
- 代价：每个MotionWarp operation增加固定state slots和artifact体积。
- 不选择runtime对象缓存：它不会进入Snapshot/Hash，rollback恢复后状态会漂移。
- 不选择只用Timeline normalized time推导：窗口起始body和总修正依赖进入时的committed state，不能只从静态时间恢复。

## Decision 8: Float32与Fixed共享业务顺序，不共享数值对象

### Decision

portable层负责：

- modifier eligibility顺序。
- source/action context关系。
- target requirement和reject reason。
- lifecycle/generation规则。
- authoring模式与字段合法性。

Float32和Fixed narrow target module负责：

- vector/yaw运算。
- progress curve采样。
- clamp和delta计算。
- typed state读写。

两个Target必须从同一Semantic descriptor降低并产生相同trace字段。不得复制一套Float32业务if/else再写一套Fixed业务if/else。

### Tradeoff

- 收益：数值格式不同，但动作规则只有一份。
- 代价：需要明确的portable descriptor和target math port，而不是简单共享一个Unity `Vector3`类。
- 不选择让Fixed调用Float再量化：会破坏确定性目标。
- 不选择只实现Float32：会使网络模型决定是否支持某个角色动作。

## Decision 9: WorldSolver只看最终Request

### Decision

Warp后的Action channel与其它channel按现有规则合成，最终只产生一个`CharacterMotionRequest`。Solver input/output schema不增加MotionWarp、Target或Action字段。

如果墙体阻挡请求：

- actual Body Result由Solver决定。
- Finalize提交actual result。
- Warp不在Solver后补偿。
- diagnostics同时显示desired request和actual result。

### Tradeoff

- 收益：Unity CharacterController、Deterministic KCC和未来DotRecast backend无需理解动作。
- 代价：Warp只保证无阻挡条件下的目标收敛，不保证穿过碰撞到达目标。
- 这是正确业务边界。目标对齐与碰撞冲突时，碰撞优先；若未来需要绕障，属于导航/移动策略，不属于MotionWarp。

## Decision 10: Preview必须使用显式快照和正式Session

### Decision

Timeline Authoring Preview增加一个editor-only preview target snapshot输入，保存在窗口session state，不写入Timeline资产。完整预览流程：

```text
Preview Target + Preview ActionTargetSnapshot
  -> isolated Preview Session
  -> compiled Program
  -> Action admission/instance
  -> MotionCurve + MotionWarp
  -> Preview WorldSolver
  -> body/animation presentation
```

Live Debug只读取正式runtime trace，显示窗口、source、target、progress、request和actual result。纯AnimationClip采样模式没有Action Context、Program和Solver，必须明确显示MotionWarp不可用。

### Tradeoff

- 收益：作者看到的是正式Gameplay结果，且可以在target provider实现前调clip参数。
- 代价：完整Warp预览需要一个合法Preview Definition/Program和显式目标值。
- 不选择在Timeline窗口直接移动preview GameObject：那会成为第二套算法，无法代表Fixed或Solver结果。

## Decision 11: Agent复用唯一authoring API

### Decision

Agent v9扩展现有Snapshot/Patch链：

- Snapshot输出MotionWarp track/clip subtype、stable identity、source MotionCurve identity和所有typed参数。
- Patch增加ensure/configure/delete MotionWarp track/clip命令。
- handler调用Timeline正式authoring API选择source并写字段。
- emitter registry允许MotionWarp类型。
- validator复用Compiler的source、window、action context、target requirement和curve规则。
- dry-run/apply继续消费同一immutable typed command plan。

不为MotionWarp创建独立JSON importer、YAML writer、MCP资产修改器或第二Inspector。

### Tradeoff

- 收益：人工与Agent编辑同一资产、走同一校验。
- 代价：Snapshot与Patch DTO增加明确字段，但不需要新运行时模块。
- v9可做向前扩展时保持v9；如果现有schema把Timeline clip payload定义为封闭union而无法无歧义扩展，apply时必须整体升版并删除旧reader，不能偷偷接受两种payload。

## Data Flow

```text
ActionTargetSnapshot producer（本change之外）
  -> Blackboard typed value
  -> CanActivateAction / ActivateActionInstance
  -> immutable ActionInstance.TargetSnapshot

MotionCurveClip authoring
  -> TimelineMotionCurve Semantic operation
  -> raw Action MotionContribution
  -> Action channel resolution

MotionWarpClip authoring
  -> TimelineMotionWarp Semantic operation
  -> source operation reference + typed warp descriptor
  -> Action channel modifier
  -> warped Action channel

Locomotion channel + warped Action channel + GameplayResult channel
  -> CharacterMotionRequest
  -> WorldSolver batch
  -> actual Body Result
  -> Finalize / Snapshot / GameplayFact / Presentation
```

## Failure Semantics

以下必须在artifact发布前失败：

- source identity缺失、指向非MotionCurve或跨Timeline。
- Warp窗口超出source root motion区间。
- source不是Action/Override。
- 同一source Warp窗口重叠。
- progress curve无效、非单调、起止值错误或包含非有限值。
- weight、offset或clamp非法。
- Warp所在Action没有显式Action Context。
- ActionProfile未声明`SnapshotRequired`。
- Float32或Fixed Target缺少MotionWarp capability。

以下必须在runtime fail-stop并输出结构化diagnostics：

- 已通过编译的Warp执行时没有有效ActionInstance或target snapshot。
- 同一Actor/Action channel/Tick出现多个eligible Warp。
- state generation与Timeline/Action generation不一致。
- source operation不是当前resolved owner却仍尝试应用。
- Snapshot恢复得到非法Warp state。

source正常输掉Action通道仲裁不是错误：Warp跟随source一起不执行，并记录`SourceNotResolved` trace。

## Migration And Cleanup

1. 先冻结当前Motion result基线、版本identity和旧MotionWarp字段清单。
2. 安装新的authoring model、Semantic operation与两个Target能力。
3. 拆分channel resolve与Request build，证明无Modifier路径等价。
4. 安装Warp state、算法、Preview、Trace和Agent。
5. 迁移仓库中现有MotionWarp资产；当前盘点若仍为0个，则直接删除旧字段而不保留migrator。
6. 将ActionProfile字符串TargetPolicy迁为typed requirement；所有现有profile显式写`None`或`SnapshotRequired`。
7. 重新生成全部正式artifacts和产品manifest。
8. 删除旧DTO、Sample、reader、schema、Inspector字段和未消费代码。

由于当前Corin资产没有MotionWarpClip，也没有正式target provider，本change只保证基础设施、authoring、Preview和Program闭环。Corin实际攻击Warp必须由后续target provider/change配置，不能在本change里用场景查找或临时Blackboard写入冒充完成。
