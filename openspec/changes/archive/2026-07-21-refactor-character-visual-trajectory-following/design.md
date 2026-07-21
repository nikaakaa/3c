# Design: 角色可见轨迹跟随边界

## Context

当前正式链路是：

```text
Simulation Commit / Model Egress
  -> CharacterPresentationBodyInterval(Position, Rotation)
  -> CharacterBodyPresentationRuntime
       CommittedStream: interpolation + accumulated recovery offset
       SelectedStream: interpolation + per-frame SmoothDamp
  -> VisualRoot
```

Simulation 本身已有 Position、Yaw、Velocity、Grounded 与 Collision。Presentation 丢弃 Velocity/Grounded 后，只能用位置差猜测运动连续性；同时两种 source 各自维护一套视觉滤波。Rollback canonical revision频繁发生时，固定六 Tick offset会不断叠加；SelectedStream即使没有发生纠偏，也会持续落后于正常轨迹。

目标结构是：

```text
Simulation Commit / Model Egress
  -> Body Kinematic Interval(Position, Rotation, Velocity, Grounded)
  -> Body Source Cursor(Committed | Selected)
  -> Target Trajectory Sampler
  -> Visual Trajectory Follower(Direct | BoundedCorrection)
  -> CharacterBodyPresentationFrame
       -> VisualRoot
       -> AnimationSampleTick/Alpha
       -> Camera
```

Source Cursor回答“现在应该看哪个simulation tick”；Follower只回答“target发生真实不连续时，画面如何接上”。两者不得互相选择或修改。

## Goals

- 保持 Rollback remote 的 predicted current timeline，不通过增加固定延迟隐藏纠偏。
- 正常移动、转身和循环动画在渲染帧连续重采样，不被第二次低通滤波拖慢。
- canonical branch replacement保持逻辑即时正确，同时让visual pose以短、可控、有上限的方式收敛。
- Position correction保留速度连续性；Grounded角色不产生垂直悬浮。
- local、rollback simulated和server observed复用同一Follower实现，只选择不同profile。
- 所有调节项都属于Character Presentation authoring，不进入Network Model逻辑。

## Non-Goals

- 不保证零延迟预测同时永远零纠偏；远端输入未知时这在技术上不成立。
- 不通过增加input delay或confirmed render delay换取绝对稳定。
- 不让动画骨骼或root motion反向决定World body。
- 不把视觉速度作为Gameplay速度或碰撞速度。
- 不建立按Network Model命名的Follower子类。

## Decision 1: Body sample保留表现所需运动学信息

`CharacterPresentationBodyState` 修改为：

```text
ActorId
Position
Rotation
LinearVelocity
Grounded
```

Position/Rotation仍是唯一target pose；LinearVelocity只用于目标轨迹切换时建立相对速度，Grounded只用于决定垂直纠偏方式。Collision summary不进入本次Presentation合同，因为当前视觉跟随不需要按碰撞类型分支。

Float32与Fixed converter必须从现有`WorldBodyState`直接投影这些字段。ServerAuthoritative observed sample同样从其正式selected Body投影，不新增packet字段，也不从Transform估算速度。

### Tradeoff

- 收益：Follower不再用相邻位置差猜目标速度，分支切换可以保持一阶连续；Grounded时可避免视觉根上下漂。
- 代价：Body interval值更宽，Presentation history每个sample多保存一个Vector3和一个bool；它仍是小型渲染态数据，不进入Snapshot或网络载荷。

## Decision 2: Source与Trajectory正交配置

Source保留两个值，但重命名为明确的数据语义：

```text
CommittedStream
SelectedStream
```

Trajectory只有两个正式值：

```text
Direct
BoundedCorrection
```

- `Direct`：target连续推进或替换后，visible pose直接等于target pose。
- `BoundedCorrection`：正常连续区间仍直接跟随target；只有branch replacement、显式Reset或检测到合同允许的不连续时，才运行短时纠偏。

这两个轴在runtime创建后都不可切换。`CommittedStream`不再隐含固定六 Tick recovery，`SelectedStream`也不再隐含每帧SmoothDamp。

### Tradeoff

- 收益：同一CommittedStream可以用于Standard Direct与Rollback Bounded；新增网络模型只选择profile，不修改公共Body Runtime。
- 代价：每个Factory调用点都必须显式传入profile，装配字段增加，但错误会在创建时暴露。

## Decision 3: 正常轨迹不做二次平滑

Source Cursor先根据表现时钟得到target：

```text
TargetPosition = interpolate(Previous.Position, Current.Position, alpha)
TargetRotation = interpolate(Previous.Rotation, Current.Rotation, alpha)
TargetVelocity = interpolate(Previous.Velocity, Current.Velocity, alpha)
TargetGrounded = alpha < 1 ? Previous.Grounded && Current.Grounded : Current.Grounded
```

当新区间与上一区间连续、且没有branch revision时：

```text
VisiblePose = TargetPose
VisibleVelocity = TargetVelocity
```

不对每帧正常移动再运行SmoothDamp。网络稀疏采样的平滑由SelectedStream相邻tick区间重采样承担；Rollback的平滑由CommittedStream逐tick区间重采样承担。

### Tradeoff

- 收益：角色不会因为“持续追逐一个一直移动的target”而产生软、飘、落后；移动速度和转身时机更贴近模拟。
- 代价：如果上游只提交稀疏且不连续的target，问题会直接暴露为stream合同错误或纠偏事件，不能再由长期低通掩盖。

## Decision 4: 有界纠偏使用相对误差状态，不累计offset

发生真实不连续时，Follower在当前presentation sample time计算：

```text
positionError = PreviousVisiblePosition - NewTargetPosition
relativeVelocity = PreviousVisibleVelocity - NewTargetVelocity
yawError = shortest(PreviousVisibleYaw - NewTargetYaw)
relativeYawVelocity = PreviousVisibleYawVelocity - NewTargetYawVelocity
```

随后以profile的half-life推进临界阻尼误差状态，输出：

```text
VisiblePosition = TargetPosition + decayedPositionError
VisibleYaw = TargetYaw + decayedYawError
```

每次新revision都从“当前真实visible状态减去新target状态”重新建立误差，不能把新差值加到旧offset上，也不能重置一个固定六 Tick计时器。

Profile必须显式提供：

```text
PositionHalfLifeSeconds
MaximumHorizontalErrorMeters
PositionSettleDistanceMeters
YawHalfLifeSeconds
MaximumYawErrorDegrees
YawSettleDegrees
```

超过Maximum的误差在同一帧先投影到允许边界，剩余误差再收敛，因此visual pose不会无限远离canonical target。小于settle threshold的误差直接归零，避免微小revision造成持续漂移。

Corin Rollback profile的首轮值作为可调起点：Position half-life 0.04秒、最大水平误差0.18米、settle 0.005米；Yaw half-life 0.035秒、最大误差12度、settle 0.25度。这些值必须写入资产，不作为代码默认或fallback。

### Tradeoff

- 收益：保留低延迟预测，纠偏时间短且最大视觉误差可证明；连续revision不会无限叠加尾巴。
- 代价：超过上限的较大纠偏仍会在第一帧发生部分跳变。这是“低延迟、有限偏差、不能穿帮太远”的明确业务取舍，不可能同时消除。

## Decision 5: Grounded与Airborne分开处理垂直误差

当新target为Grounded时，visible Y直接使用target Y，只对水平平面误差运行Follower；不得让旧分支的Y offset造成悬浮或陷地。Airborne区间可以对完整三维position error运行相同有界纠偏，以保持跳跃或击飞轨迹连续。

Follower不做地面查询，也不读取Collider。Grounded只来自已提交Body sample。

### Tradeoff

- 收益：地面动作的视觉脚底不会因为网络纠偏慢慢上下回落。
- 代价：地面高度发生较大权威变化时Y会直接校正；若以后需要台阶专用视觉策略，应基于正式surface信息另开change，不能在这里猜地形。

## Decision 6: Profile属于Presentation，组合只选择引用

现有`CharacterRemotePresentationProfile`迁移并重命名为通用`CharacterBodyPresentationProfile`，由`ThirdPersonClient.Runtime`拥有。它保存Trajectory模式与对应参数，Runtime构造时转成不可变settings。

正式装配为：

```text
Standard Local / Preview
  -> CommittedStream + Direct profile

DeterministicRollback local与remote simulated actor
  -> CommittedStream + BoundedCorrection profile

ServerAuthoritative observed actor
  -> SelectedStream + BoundedCorrection profile
```

Network Model代码可以在Unity composition/actor registration处传入profile引用，但不得实现Follower、保存visual velocity、调用SmoothDamp或写VisualRoot。Factory不得通过具体Model类型、Actor名称或camera ownership推断profile。

现有remote profile脚本与资产单路迁移，保留`.meta` identity；旧类名、旧settings类型和旧字段在调用点迁移完成后删除，不保留`MovedFrom`、wrapper或兼容读取。

### Tradeoff

- 收益：视觉手感可以独立调节，Rollback、ServerAuthoritative和后续模型复用一份实现；模型协议不被视觉参数污染。
- 代价：测试Scene/Host需要显式引用正确profile。缺失引用会直接阻止runtime创建，这是刻意的fail-fast。

## Decision 7: Body纠偏不改变动画时间权威

`CharacterBodyPresentationFrame.AnimationSampleTick/Alpha`继续来自Source Cursor的target presentation time，不来自Follower的visible error或收敛进度。

因此：

- Body纠偏不会减慢、重启动画或生成第二个动画clock。
- 同一PlaybackId/generation的replay sample替换继续由现有AnimationPlaybackLifecycle更新采样目标。
- producer发生真实变化时仍由现有Animancer Play/Fade从当前视觉图接管。
- Follower不得根据动画clip、state名称、Action或Tag选择参数。

### Tradeoff

- 收益：Gameplay与动画生命周期边界不被视觉位置平滑反向污染，攻击结束时刻和所有Peer保持一致。
- 代价：短时Body纠偏期间脚步与世界位移可能有轻微相位差；本change用短half-life和小误差上限控制它，不引入Stride Warping或Motion Matching扩大范围。

## Decision 8: Diagnostics必须能区分输入轨迹与可见轨迹

每个Body Presentation trace至少包含：

```text
ActorId
SourceMode / TrajectoryMode
PreviousTick / CurrentTick / SampleAlpha
TargetPosition / TargetRotation / TargetVelocity / TargetGrounded
VisiblePosition / VisibleRotation / VisibleVelocity
PositionError / YawError
CorrectionVelocity / CorrectionActive
CorrectionClamped / Settled
BranchRevision / ResetReason
```

Diagnostics只读，不参与Follower、Simulation、Network或动画选择。

## Error Semantics

以下情况必须直接失败：

- Body state的ActorId、Position、Rotation或Velocity非法。
- Factory未收到正式Body Presentation Profile。
- Profile mode未知，或BoundedCorrection参数非有限值、非正值、settle大于maximum。
- Committed transaction不连续或Selected append不连续且没有显式Reset。
- Source tick回退但没有合法branch replacement/Reset语义。
- Network adapter尝试提交已经SmoothDamp过的visual body作为canonical interval。

不得自动切换Direct、使用硬编码参数、读取Transform估算速度或吞掉不连续。

## Lifecycle

创建顺序：

```text
Profile validation
  -> Source Cursor state
  -> Visual Trajectory Follower
  -> Body Runtime
  -> Animation / Camera modules
  -> Presentation Runtime published
```

分支替换顺序：

```text
capture previous visible pose/velocity
  -> sample old branch at current presentation tick
  -> atomically replace committed intervals
  -> sample new branch at same presentation tick
  -> retarget follower once
  -> continue presentation clock
```

Reset/Dispose必须同时清空source cursor、follower error/velocity和diagnostics identity，不得保留跨Session视觉状态。

## Alternatives Considered

### 将remote表现改为Confirmed cursor

优点是几乎没有预测分支纠偏；代价是当前4 Tick confirmation delay在60Hz下产生约67毫秒基础延迟，加上输入delay和网络波动后会明显晚于玩家操作。当前目标是动作游戏低延迟预测，因此不选。

### 单纯增大现有SmoothDamp时间

优点是改动小；代价是角色更慢、更飘，且没有解决连续revision累计与正常轨迹被二次滤波的问题。不选。

### 单纯缩短固定六 Tick recovery

优点是能减少拖尾；代价是仍然没有速度连续性、误差上限与Grounded语义，连续revision仍会重复叠加。不选。

### 先改输入预测器或增加远端输入量化

优点是可以减少revision数量；代价是会修改Gameplay输入语义、协议和网络模型，而且不能消除真实迟到输入。它可以作为后续独立优化，但不能替代Presentation正确处理不连续。

### 让动画或Motion Matching吸收位置纠偏

优点是理论上能得到更好的脚步贴合；代价是当前项目不是Motion Matching，且会让动画反向拥有逻辑位移。超出本change并违反World body唯一权威，不选。

## Implementation Order

1. 固化所有Body interval生产点、profile资产与Factory调用点清单。
2. 扩展Body kinematic sample与Float32/Fixed/observed converter。
3. 建立SourceMode、TrajectoryMode和通用Body Presentation Profile。
4. 实现独立Target Sampler与Visual Trajectory Follower。
5. 将Committed branch replacement迁到Follower retarget。
6. 将Selected正常插值迁为直接target轨迹，只在不连续时启用Follower。
7. 迁移Standard、Preview、Rollback与ServerAuthoritative profile装配。
8. 保持AnimationSampleTick/Alpha与现有playback lifecycle不变并清理旧状态。
9. 扩展diagnostics、删除旧类型/字段、更新current truth并执行静态编译与OpenSpec严格校验。

## Stop Conditions

实施中出现以下情况必须停止并说明tradeoff：

- Fixed或Float32正式Body result无法提供Velocity/Grounded，必须从Transform或渲染差分猜测。
- SelectedStream存在无法通过interval continuity或显式Reset表达的合法分支替换，必须读取Network私有history才能判断。
- 修正动画瞬切必须改变逻辑producer selection、Timeline事实或BTSMTL状态转换，而不是现有playback lifecycle的sample replacement。
- 需要新增第二个VisualRoot writer、Network Model内Follower或confirmed fallback才能完成。
- 现有profile资产无法在保留引用identity的前提下安全单路迁移。
