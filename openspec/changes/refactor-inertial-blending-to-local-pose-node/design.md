# Design: 局部 Pose Inertialization

## Context

工作区的`AnimationBlendStackRuntime`和`AnimationSlotBlendJob`已经完成以下算法：

- 保存上一份完成Pose和相邻帧速度；
- 对新target计算每骨骼position、rotation log、scale residual；
- 计算linear、angular和scale velocity residual；
- 按canonical curve、duration和dense per-bone multiplier衰减；
- 以Quaternion Exp恢复旋转；
- 活跃惯性再次切换时从当前修正输出rebase；
- 连续传播Pose Parameter和每脚feature aggregate。

该实现把算法放在per-PoseSlot Stack内部。最新Pose Graph方向已经删除隐藏slot owner，但仍计划让显式Blend Stack同时负责CrossFade、Stored和Inertial。局部惯性化要求继续拆分：CrossFade需要旧source继续采样，Inertialization只需要上一份完成输出和新target；两者的状态、寿命与作者放置语义不同。

## Goals

- 让作者在Pose Graph中明确看到惯性化作用在哪条局部分支。
- 复用并迁移现有残差算法，不复制第二实现。
- 让MM、Timeline和BTSMTL只负责Selection，不理解惯性状态。
- 让Blend Stack只拥有多source历史、Stored Pose与source retirement。
- 让Compiler静态证明Inertialization的直接上游、pair coverage、Rig和执行阶段。
- 保持Preview、Runtime、Replay与Live Debug使用同一节点状态机。

## Non-Goals

- 不照搬UE完整Animation Attribute request传播系统。
- 不允许一个Output前全局节点隐式接收所有分支切换。
- 不支持对Empty、NoPose或Invalid构造惯性目标。
- 不增加独立Gameplay动画状态机、Chooser或第二播放器。
- 不改变现有残差数学为弹簧或Half-Life模型。

## Terms

### Pose Discontinuity

Player输出的只读表现事实，表示它的直接Pose流从一个离散source endpoint切换到另一个endpoint：

```text
EventIdentity
Reason
PreviousEndpoint
CurrentEndpoint
PreviousContinuityIdentity
CurrentContinuityIdentity
ResetSequence
```

它不是transition request，不包含duration、curve、Blend Profile、residual或weight。普通连续sample不产生新event。

### Inertialization Node

单Pose输入、单Pose输出的局部状态节点。它读取直接上游Player的Pose和Discontinuity，按自己的exact Policy决定HardCut或Inertialize，并拥有上一份完成输出、速度历史、单一Accumulator与clock。

### Inertialization Policy

节点本地作者资产。每个reachable endpoint pair必须物化一条exact rule：

```text
HardCut

或

Inertialize
  Duration
  CanonicalCurve
  DensePerBoneBlendProfile
  PoseParameterFilter
```

authoring default只用于Compiler完整物化，Runtime没有default lookup。

## Decision 1: 独立局部节点，不保留Stack内Inertial

正式分工：

```text
SelectedPosePlayer
  当前source采样 + discontinuity事实

BlendStack
  多个live source + CrossFade + Stored Pose + release

Inertialization
  单Pose history + residual + rebase

Layered/Additive/Modify/FootPlacement
  空间组合与程序化处理
```

同一个选择分支只能明确选择一种连续化路径：

```text
Selection -> SelectedPosePlayer -> Inertialization

或

Selection -> BlendStack

或

Selection -> SelectedPosePlayer
```

Runtime不得在三种路径之间自动替换。

### Tradeoff

- 收益：每个模块的删除测试清楚；删除Inertialization只失去残差平滑，删除BlendStack只失去多source CrossFade/Stored历史。
- 代价：原先一个Transition Rule同时选择CrossFade/Inertial的中心matrix需要拆成Player图拓扑与节点本地Policy。

## Decision 2: 第一阶段只消费直接上游Player的Discontinuity

`Inertialization`的Pose输入必须直接来自一个`SelectedPosePlayer`，或者来自Compiler证明等价的无状态直通子图；正式v1不允许跨Blend、Layered、Additive、ModifyBone或Subgraph边界传播Discontinuity。

Compiler必须同时证明：

- Pose与Discontinuity来自同一个Player runtime identity；
- Player全部reachable endpoint已被Policy覆盖；
- 当前节点位于native Pose阶段；
- 节点下游不存在回到Selection/Player的环；
- 节点早于FootPlacement和world-aware IK阶段。

### Tradeoff

- 收益：局部作用域可静态解释，上半身请求不会污染下半身或最终全身。
- 代价：不能像UE全局Inertialization receiver一样一次接收多个上游请求；该能力只有出现明确业务后才能用typed scope扩展。

## Decision 3: Player只发布事实，Policy属于Inertialization节点

`SelectedPosePlayer`在以下情况发布新Discontinuity：

- Selection source identity改变；
- 同source的SelectionGeneration提升并代表pose jump；
- Preview正式连续播放中的离散source seek；
- 编译合同声明的其它Pose source jump。

以下情况必须发布Reset而不是普通可惯性化切换：

- Initialization；
- Presentation Reset；
- Committed branch replacement；
- Selected stream reset；
- Rollback replacement要求清理表现历史；
- Preview非连续scrub/seek；
- Projection replacement。

Player不携带duration或curve。Inertialization节点用PreviousEndpoint与CurrentEndpoint exact lookup自己的Policy，因此同一个Selection可以被两个Player/节点分支以不同局部策略表现。

### Tradeoff

- 收益：Selection与Player保持source-neutral，惯性选择完全属于图上的表现边界。
- 代价：每个Inertialization节点需要自己的完整pair table，不能依赖Program或Timeline edge上的fade配置。

## Decision 4: 节点使用上一份完成输出作为唯一源历史

节点维护双页历史：

```text
CompletedOutputPose[n-1]
CompletedOutputVelocity[n-1]
CompletedParameters[n-1]
CompletedFootFeatures[n-1]
```

当收到合法Inertialize discontinuity时：

1. 当前上游Player先采样新target Pose。
2. 节点读取上一份exact completed输出与速度。
3. 计算上一输出相对新target的TRS和速度残差。
4. 原子提交新的Accumulator identity和clock。
5. 旧source在capture completion后即可exact release。
6. 后续帧只采样新target并衰减Accumulator。

首份Pose没有上一完成输出，只建立history，不执行惯性化。

### Tradeoff

- 收益：连续中断使用用户真正看到的修正结果，不会跳回旧Clip或未合成历史。
- 代价：节点必须在同一compiled plan中拥有exact completion和双页历史，不能做成无状态数学函数。

## Decision 5: 保留现有残差数学

每骨骼捕获：

```text
PositionResidual = Previous.Position - Target.Position
RotationResidual = Log(Previous.Rotation * Inverse(Target.Rotation))
ScaleResidual = Previous.Scale - Target.Scale

LinearVelocityResidual = Previous.LinearVelocity - Target.LinearVelocity
AngularVelocityResidual = Previous.AngularVelocity - Target.AngularVelocity
ScaleVelocityResidual = Previous.ScaleVelocity - Target.ScaleVelocity
```

运行时：

```text
BaseResidual(t) = PoseResidual + t * VelocityResidual
OutputPosition = TargetPosition + ResidualWeight(t) * BaseResidual(t)
OutputRotation = Exp(ResidualWeight(t) * RotationBase(t)) * TargetRotation
```

ResidualWeight及其时间导数继续由canonical monotone Hermite curve和duration计算，并校正曲线端点导数，保证残差从1连续落到0。逐骨骼duration继续使用dense Blend Profile multiplier。

本change不引入频率、阻尼、Half-Life、轴向限制或速度clamp配置；以后若替换数学模型，必须升级Policy与Projection schema，不能在Runtime按数值猜测算法。

## Decision 6: 连续中断只保留一个Accumulator

活跃Accumulator期间收到新的合法Inertialize discontinuity时：

- 先使用上一份已完成修正输出作为Previous；
- 以新Player Pose作为Target；
- 重新计算全部残差；
- 提升Accumulator generation；
- 清除旧残差和旧clock；
- 不保存Accumulator stack。

同一PresentationFrame出现两个不同discontinuity属于输入冲突，节点进入typed Invalid，不依赖提交顺序选择一个。

## Decision 7: Parameter和Foot Feature显式处理

Pose Parameter分为：

- `Inertialize`：连续表现标量，如某些source-local视觉权重；按同一节点output envelope衰减残差。
- `Snap`：业务选择或离散含义参数；立即使用target值。

Policy必须为节点可达Parameter layout完整物化filter，不能按名称、默认类型或缺失配置猜测。

Foot Feature继续使用左右脚实际Bone envelope传播capture到target的连续值，并保存真实target source contribution。Accumulator只作为节点内部连续性状态，不生成伪producer、伪clip、伪contact或AnimationPoseSourceId。

Foot Placement读取节点之后最终Pose Graph形成的每脚输入，不反向影响Inertialization。

## Decision 8: Empty、Invalid与Reset不惯性化

- `Pose -> NoPose`：节点不生成残差，按compiled HardCut/NoPose语义清理history；需要淡出时使用BlendStack或显式BlendPose权重。
- `NoPose -> Pose`：建立新history，不从Bind Pose或零Pose惯性进入。
- `Invalid`：传播typed Invalid并清理未提交Accumulator。
- `Reset`：清理history、Accumulator、clock、parameter residual和foot feature history，从下一份合法Pose重新锚定。
- duration为0的exact Inertialize rule在编译时规范化为HardCut，不保留零时长Accumulator。

这样避免为“没有姿势”编造骨骼目标，也避免正常视觉残差跨网络/分支重置继续存活。

## Decision 9: 执行计划与单次Evaluate

编译计划：

```text
Phase A Selection
Phase B Source Capture
Phase C Native Pose
  SelectedPosePlayer
  Inertialization
  BlendStack
  Blend/Layered/Additive/ModifyBone
Phase D World-Aware Pose
  FootPlacement Planner/Query/Solver
Phase E Final Publication
```

具体阶段名称可以与`CharacterPresentationPosePlan`最终schema统一，但约束不变：Inertialization与source capture、其它native Pose节点在同一个PlayableGraph evaluation中完成；不能额外Evaluate，也不能在FootPlacement后写回第二次骨骼。

## Decision 10: Diagnostics按节点发布

每个Inertialization节点的只读snapshot至少包含：

```text
PoseNodeId
InputPlayerNodeId
DiscontinuityEventIdentity
PreviousEndpoint / CurrentEndpoint
RuleIdentity
HardCut / Capture / Continue / Rebase / Complete / Reset / Invalid
Elapsed / Duration
Selected Bone residual与envelope
Accumulator generation
History completion identity
Output completion identity
Reset reason
```

RuntimeDebugSession、Timeline Preview与MM Query Fixture只读取该snapshot；Inspector不得重新采样Clip、重算残差或自行推导状态。

## Authoring Model

Corin建议目标图：

```text
AnimationSelectionInput(BaseLocomotion)
  -> SelectedPosePlayer
  -> Inertialization(LocomotionInertialPolicy)
  -> BasePose

AnimationSelectionInput(FullBodyAction)
  -> BlendStack(ActionCrossFadePolicy)
  -> ActionPose

BasePose + ActionPose
  -> LayeredBoneBlend
  -> ModifyBone
  -> FootPlacement
  -> OutputPose
```

未来MM只替换Base Selection Input：

```text
MotionMatchingSelectionInput
  -> SelectedPosePlayer
  -> Inertialization(LocomotionInertialPolicy)
```

如果某个Action需要快速取消且不需要旧source继续采样，可以单独使用：

```text
ActionSelectionInput
  -> SelectedPosePlayer
  -> Inertialization(ActionCancelPolicy)
  -> LayeredBoneBlend
```

这不是要求Corin每条分支都放节点；图上没有节点就不支付history和residual workspace。

## Migration

1. 在Selection/Pose Value合同中增加PoseDiscontinuity与reset reason。
2. 增加Inertialization authoring node、Policy、validator与compiled payload。
3. 把现有Stack Inertial residual workspace与Job代码移动到独立node runtime/job。
4. 让SelectedPosePlayer发布source jump事实，不计算transition。
5. 让Inertialization节点消费直接Player事实并完成capture/continue/rebase。
6. 从BlendStack删除Inertial technique、push、state、workspace和snapshot。
7. 把Blend Policy收窄为CrossFade/Stored；迁移Inertial配置到节点Policy。
8. 更新source release：Inertial capture完成即可释放旧source，Accumulator不拥有source retention。
9. 更新Pose contribution、Parameter和Foot Feature传播。
10. 更新Preview、MM Fixture、Replay、RuntimeDebugSession和Pose Graph overlay。
11. 重写Corin与独立MM验证图的局部节点和Policy资产。
12. 删除旧Projection schema、旧serialized Inertial字段和全部兼容读取。
13. 同步清理相关active change和`openspec/project.md`旧口径。

迁移期间不得让Stack Inertial与Node Inertial同时存在。实现提交必须以完整编译边界一次替换旧owner；若需要双写snapshot或旧Projection converter，必须停止并重新拆分提交。

## Rejected Alternatives

### 继续把Inertial留在显式BlendStack节点

图上能看到Stack，但仍无法把惯性化放在独立局部分支，MM还要承担多source Stack语义。拒绝。

### 在OutputPose前放唯一全局Inertialization

节点数量少，但任何上半身或局部分支切换都会影响全身，并且容易把IK后的world-space结果再次拖动。拒绝。

### 完整复制UE的全图Inertial Request Bus

可让多个StateMachine、Linked Layer和Blend节点向下游receiver发请求，但需要定义跨Layer、Additive、Subgraph的request合并、scope、最短时长与骨骼域传播。对于当前求职Demo范围，隐藏耦合与调试成本高于收益。拒绝。

### 每根骨骼独立Inertialization节点

会放大图规模和workspace数量。dense per-bone Profile已经能表达局部持续时间，Bone Mask和Layered Blend负责空间域。拒绝。

### 直接使用Animancer或Animator的fade/inertial能力

会恢复第二transition权威，破坏compiled Pose Plan、exact identity和统一diagnostics。拒绝。
