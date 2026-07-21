# Design: MotionWarp 目标姿态与累计轨迹解算

## Context

当前正式链路是：

```text
Timeline MotionCurve contribution
  -> Action channel resolve
  -> TimelineMotionWarp Modifier
  -> ResolvedGameplayMotion
  -> Body Motion Prepare
  -> CharacterMotionRequest
  -> WorldSolver
  -> committed Body
  -> Presentation
```

这条链不需要改变。现有问题发生在`TimelineMotionWarp Modifier`内部：窗口首次active时计算`nominalEnd`、`desiredPosition`、`totalPositionCorrection`和`totalYawCorrection`，后续分别按position/yaw progress增加修正。同时，ActorLocal源MotionCurve的每Tickdelta在应用时按当前Body yaw旋转。只要Warp同时改变yaw，初始化时假设的直线剩余轨迹就与实际逐Tick弯曲轨迹不同。

Epic公开文档将Motion Warping表达为明确动画窗口、Warp Target Transform、可独立启用的translation/rotation、rotation type/method/rate和可选Warp Point；`URootMotionModifier_SkewWarp.WarpTranslation`的公开签名同时消费CurrentTransform、当前DeltaTranslation、TotalTranslation与TargetLocation。该资料证明成熟模型需要目标Transform、窗口内总轨迹和逐Tickmodifier共同参与，但本change不声称复制Unreal私有内部公式：

- https://dev.epicgames.com/documentation/en-us/unreal-engine/motion-warping-in-unreal-engine
- https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/MotionWarping/URootMotionModifier_SkewWarp
- https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/MotionWarping/FMotionWarpingTarget

## Goals

- 让作者能直接表达“目标姿态”和“如何改源轨迹”，而不是靠weight猜最终结果。
- 让位置与yaw来自同一个累计轨迹上下文，消除变化Body yaw造成的二次积分。
- 保持Float32、Fixed、Rollback、Network和WorldSolver消费同一portable业务语义。
- 让普通攻击、原地转向、距离缩放、明确直线移动和以后配对交互使用同一个MotionWarp operation。
- 保持Timeline和Agent只是authoring入口，Runtime不读取Unity动画骨骼或场景Transform。

## Non-Goals

- 不实现运行时骨骼采样或武器接触点。
- 不实现跟随移动目标或每Tick改写Action target snapshot。
- 不把MotionWarp变成Animation Presentation功能。
- 不在WorldSolver之后追赶目标或修正碰撞结果。
- 不用Foot IK遮盖Gameplay根轨迹错误。

## Terms

### Source Window Pose

在MotionWarp窗口内，从源MotionCurve采样得到的累计平面位置和累计yaw，并以窗口StartFrame的源累计姿态为零点。窗口EndFrame定义本次Warp的源终点；它不再隐式使用整个MotionCurve的CurveEndFrame。

### Target Pose

由ActionInstance不可变target snapshot、目标offset空间、position offset、rotation mode和yaw offset共同生成的窗口结束目标姿态。

### Warped Cumulative Pose

Translation Solver和Rotation Solver在同一normalized window time下，从Source Window Pose生成的累计结果。Runtime只用当前与上一次Warped Cumulative Pose之差提交本Tickdelta。

### Limit Policy

当目标要求超过最大平面修正或最大yaw修正时的显式作者选择：

- `ApplyClamped`：使用受限的有效目标姿态，并产生`AppliedClamped`。
- `PreserveSource`：本窗口保持源MotionCurve，不初始化Warp state，并产生`PreservedByLimitPolicy`。

这两种都是编译进Program的业务策略，不是异常fallback。

## Decision 1: 一个Operation，正交Descriptor

保留唯一`TimelineMotionWarp` operation。Descriptor由以下字段组成：

```text
SourceMotionOperation
TranslationMode
TargetOffsetSpace
TargetPlanarOffset
RotationMode
RotationMethod
TargetYawOffsetDegrees
MaximumPlanarCorrection
MaximumYawCorrectionDegrees
MaximumYawRateDegreesPerSecond
LimitPolicy
PositionProgressCurve (按mode需要)
YawProgressCurve (按method需要)
```

不为Scale、Skew、Linear或FaceTarget创建独立Timeline Track、operation code或runtime registry。固定Operation Set通过typed enum选择受支持算法；未知值在artifact读取或Program composition时拒绝。

删除`PositionWeight`与`YawWeight`。部分修正必须由`ApplyClamped`明确表达，站位差异必须修改target offset，时间分布必须修改progress curve。weight同时改变终点含义，作者无法判断“没有到位”是配置、限制还是权重造成。

## Decision 2: Translation Mode表达轨迹方法

### Disabled

不对齐目标位置。源窗口累计位置保持原始形状，但若Rotation Solver改变累计yaw，源轨迹使用同一累计旋转映射，不能回到按当前Body yaw逐delta积分。

业务用途：原地或带少量原始根位移的转向攻击。

### ScaleToTarget

使用源窗口终点向量和目标终点向量建立一个稳定平面相似变换：旋转源轨迹方向并统一缩放长度，使窗口终点到达有效目标位置。源终点长度接近零时配置无效。

业务用途：动画方向和轨迹形状正确，只需要匹配不同攻击距离。

取舍：最能保留原步频和轨迹比例，但不适合原地动画，也不适合需要明显侧向弯曲的目标。

### SkewToTarget

先用Rotation Solver的累计yaw修正旋转窗口内源累计位置，再计算完整修正后的源终点与目标终点之间的固定残差；残差按Position Progress累计曲线加入同一累计pose。窗口起点保持0，窗口终点严格落在有效目标位置。

业务用途：普通锁定攻击、短距离侧向修正和目标接近。

取舍：比Scale覆盖更多目标方向，但较大的横向残差仍会改变脚与地面的关系，因此必须受最大修正和窗口曲线约束。

### LinearToTarget

明确忽略源MotionCurve的平面累计位置，按Position Progress从窗口起始Body位置移动到有效目标位置；源yaw仍由Rotation Solver处理。它仍逐Tick提交正式Motion request并经过WorldSolver，不是Teleport。

业务用途：作者明确选择的in-place位移动作、短冲刺或特殊交互。

取舍：最可预测，但视觉是否滑步完全依赖动画本身、窗口与后续Pose Warping；因此不能作为普通攻击默认值。

## Decision 3: Offset Space决定目标位置，不决定轨迹算法

`TargetPlanarOffset`的X表示右方向分量，Y表示前/外方向分量。空间定义如下：

- `TargetLocal`：基向量来自target snapshot yaw。适合处决、格挡反击和配对动作。
- `ApproachDirection`：前/外方向为目标位置指向窗口开始Body位置的单位向量，右方向为其稳定垂线。适合普通近战站距。
- `ActorStartLocal`：基向量来自窗口开始Body yaw。适合按角色起始方向定义落点的冲刺。
- `World`：X/Y直接解释为世界X/Z偏移。适合明确场景交互点。

`ApproachDirection`在目标与窗口开始Body平面位置重合时无有效基向量，必须产生typed无效结果；不能借用target yaw或上一次方向作为fallback。

## Decision 4: Rotation Mode与Rotation Method分离

Rotation Mode只决定窗口结束时想朝哪里：

- `Disabled`：保留源窗口累计yaw。
- `FaceTarget`：从有效目标actor position朝向target snapshot position，再增加yaw offset。
- `MatchTargetYaw`：匹配target snapshot yaw，再增加yaw offset。

Rotation Method决定如何到达该yaw：

- `ProgressCurve`：源累计yaw加最短角修正乘Yaw Progress。普通攻击默认使用。
- `ConstantRate`：每个窗口累计时间最多推进`MaximumYawRateDegreesPerSecond`允许的角度；不足部分按Limit Policy处理。
- `ScaleSourceYaw`：把源窗口累计yaw按终点比例缩放到有效目标yaw。源窗口总yaw接近零时配置无效。

Position offset和yaw offset始终只修改Target Pose，不作为额外delta在末尾重复添加。

## Decision 5: 窗口结束就是Warp目标时刻

Warp使用源MotionCurve在MotionWarpClip `StartFrame..EndFrame`之间的累计轨迹。Warp窗口结束时达到有效Target Pose；源MotionCurve在该窗口之后仍可继续提交自己的剩余delta。

这让作者能把窗口结束放在命中帧或主要落脚帧。旧实现按`CurveEndFrame`预计终点，会让短Warp窗口提前摊完整条曲线的终点误差，编辑器时间语义不直观，因此删除。

## Decision 6: 用累计pose差分，不按变化Body yaw重积分

窗口初始化保存：

```text
PlaybackGeneration
ActionInstanceReference
WarpStartBodyPosition/Yaw
SourceWindowStartPosition/Yaw
ResolvedTargetPosition/Yaw
EffectiveLimitResult
PreviousWarpedCumulativePosition/Yaw
PreviousPosition/YawProgress
SourceOperation
```

每Tick执行：

```text
source = SampleSourcePose(currentTime) - SampleSourcePose(windowStart)
rotation = RotationSolver(source.yaw, targetYaw, currentTime)
position = TranslationSolver(source.position, sourceEnd, targetPosition, rotation, currentTime)
currentWarpedPose = Compose(position, rotation)
warpedSourceDelta = currentWarpedPose - previousWarpedPose
rawSourceDelta = resolved owner在本Tick实际进入Action channel的source部分
modifierCorrection = warpedSourceDelta - rawSourceDelta
previousWarpedPose = currentWarpedPose
```

`modifierCorrection`只应用到同一个Action resolved channel。这样源owner的raw delta被精确替换成warped delta，而同channel其它合法Additive结果继续保留。Runtime不得把warped delta叠加在未扣除的raw source上，也不得覆盖整个channel；修正结果已经是warp-start world basis中的delta，不能再作为ActorLocal contribution按当前Body yaw旋转一次。

Warp source必须是`Action + Override + ActorLocal`且使用无Ease的单位Gameplay motion weight。目标到达语义不能再被MotionCurve Gameplay权重缩放；AnimationClip/Animancer的CrossFade仍由Presentation独立处理。Authoring validator与Semantic发布必须同时拒绝非单位源，Runtime不增加权重fallback。

当一个逻辑Tick跨过Warp StartFrame或EndFrame时，resolved owner包含的完整Tick delta可能同时覆盖窗口内外。Modifier必须重新采样该Tick与Warp窗口交集的source delta，只用`warped intersection delta - raw intersection delta`修正channel。窗口外部分保留在原resolved channel中，不能因整Tick owner替换而丢失。

WorldSolver若阻挡某Tick请求，下一Tick仍只提交相邻作者累计pose的差，不追补前一Tick被碰撞裁掉的位移。这样墙体保持最终权威，也不会在窗口后半段积累越来越大的追赶速度。

## Decision 7: Float32与Fixed共享语义，不共享数值实现

Portable层拥有enum、descriptor校验、lifecycle和state semantic。Float32与Fixed分别实现：

- 平面向量长度、单位向量与稳定垂线。
- 最短yaw差和累计角采样。
- Scale、Skew、Linear累计pose算法。
- Progress、ConstantRate和ScaleSourceYaw算法。
- typed state读写与Trace数值格式。

两个Target必须消费相同字段、相同窗口边界和相同Limit Policy，但不得让Fixed调用Float、Unity `Vector`、`Mathf`或Float曲线求值。Descriptor和state变化必须提升Operation Set、两个Target ABI、Program/Layout format和State codec identity，并重新生成全部artifact。

## Decision 8: Clamp与PreserveSource必须可观察

初始化阶段先计算未限制Target Pose和相对nominal source end所需修正：

- 未超限：`Applied`。
- `ApplyClamped`且超限：生成受限Target Pose并记录`AppliedClamped`，Trace包含原始需要量、限制值和有效目标。
- `PreserveSource`且超限：不建立active Warp state，原样保留resolved source并记录`PreservedByLimitPolicy`。

不得把`PreserveSource`记录成配置失败，也不得在`ApplyClamped`后仍报告“达到原目标”。Unknown mode、非法curve、零向量基、Scale零源距离等仍属于配置或runtime invariant错误，不进入Limit Policy。

## Decision 9: Editor只显示当前模式真正消费的字段

Timeline Inspector继续以MotionWarpClip为唯一入口：

- 总是显示Source MotionCurve、Translation Mode、Rotation Mode和窗口。
- 位置启用时显示Offset Space、Planar Offset、Maximum Correction与Limit Policy。
- Skew/Linear显示Position Progress channel；Scale不显示无效position curve。
- Rotation启用时显示Rotation Method、Yaw Offset和Maximum Yaw Correction。
- ProgressCurve显示Yaw Progress；ConstantRate显示Maximum Yaw Rate；ScaleSourceYaw不显示无效yaw curve/rate。

字段切换不会自动生成、修复或删除曲线key。模式所需数据缺失时正式validation拒绝；模式不消费的旧数据在迁移时删除，不进入hash。

## Decision 10: Agent基于唯一v15原子升级

`extend-agent-authoring-for-ai-controller`完成后，Character domain的MotionWarp Snapshot/Patch增加全部新字段。既有`ensure_motion_warp_clip`、source绑定、typed配置和generic curve channel继续复用，不增加MotionWarp MCP action。

实施必须更新：

```text
Snapshot DTO/exporter
Patch DTO/schema
operation lowerer
immutable command
handler
validator
snapshot/report emitter
MCP bridge schema说明
btsmtl-agent-authoring skill
```

v14和旧MotionWarp字段不做converter。Corin资产迁移只通过`export_snapshot -> dry_run_patch -> apply same JSON -> export_snapshot -> validate`完成。

## Corin Authoring Strategy

Attack1到Attack5使用：

```text
TranslationMode: SkewToTarget
OffsetSpace: ApproachDirection
RotationMode: FaceTarget
RotationMethod: ProgressCurve
LimitPolicy: ApplyClamped
```

每段的offset、最大修正、窗口和两条进度曲线必须根据自身源MotionCurve、AnimationTrack、命中TreeClip/Cue和帧率单独确定。迁移规则固定为：

- 窗口不从主MotionCurve第0帧机械开始。
- 窗口结束不晚于该段主要命中或落脚完成阶段。
- 后摇MotionCurve不绑定Warp。
- yaw可以早于position完成，但两条曲线不能复用同一通用线性模板。
- 没有足够作者事实确定具体命中帧时，保留合法保守窗口并在实现结果中明确缺口，不伪造动画事件。

## Migration And Deletion

1. 盘点全部MotionWarp资产及非默认weight，确认不只有Corin假设。
2. 安装新authoring类型、正式mutation与validator。
3. 更新Semantic IR、两个Target descriptor/state/runtime和Agent v15。
4. 使用正式Agent事务迁移全部可达MotionWarp资产。
5. 重新生成Semantic IR、Float32/Fixed Program和Projection identity。
6. 删除旧enum值、旧字段、旧state semantic、旧codec reader、旧Trace字段和旧Agent payload。
7. 搜索确认只有一条`TimelineMotionWarp -> Motion accumulator -> WorldSolver`执行路径。

不创建一次性migrator、不直接编辑Unity YAML、不保留旧字段双读或默认值兼容。

## Risks And Tradeoffs

### 模式数量增加作者负担

正交字段比旧版两个enum更多，但每个字段只回答一个问题。Inspector按当前mode隐藏无效字段，Corin提供可直接微调的正式样例。将所有选择压成一个大enum会产生`SkewFaceTargetApproach`之类组合爆炸，后续每增加旋转方法都要复制位置模式。

### LinearToTarget容易滑步

它是明确业务能力，不是普通攻击默认值。删除它会迫使in-place特殊动作恢复脚本位移；保留它则必须在Inspector与Trace明确显示源平面轨迹被替换。Pose Warping属于后续表现change。

### 不做动态Target Follow

Snapshot让Local、Authority和Rollback重放读取同一事实。Follow会要求每Tick目标observation进入history并定义丢失、切换与authority合法性，不能偷偷塞进本次手感修复。

### 不做Bone Warp Point

Gameplay Program不能运行时读取Unity骨骼。正确方案需要把动画接触点预烘焙为portable curve，再由Warp descriptor引用；把Transform或Animator塞进Modifier会破坏当前Simulation边界。

### 两个Numeric Target同步修改成本较高

只修Float32会让同一Semantic IR在Fixed产生不同业务结果并破坏Rollback。该成本是当前可插拔Target合同的必要代价，不能用Fixed暂时忽略新模式规避。

## Open Questions Resolved

- “位置偏移”是不是一种Warp算法：不是，它只定义Target Pose。
- “转向偏移”是不是一种旋转方法：不是，它只在目标yaw上加偏移。
- “直接移动到”是否支持：支持`LinearToTarget`，但仍按窗口逐Tick经过WorldSolver，不是Teleport。
- 普通攻击默认用什么：`SkewToTarget + ApproachDirection + FaceTarget/ProgressCurve`。
- 只调整距离用什么：`ScaleToTarget`。
- 原地只转向用什么：`Translation Disabled`加Rotation Solver。
- 配对动作现在是否支持Bone点：不支持，本change只做Root级目标姿态。
- 目标移动时是否跟随：不跟随，继续使用Action激活快照。
