# Character Pipeline 运行业务基准

## 1. 文档目的

本文固定 Character Pipeline 大改后的运行验收口径。代码结构可以迁移，动画所有权可以从 Gameplay Timeline 移到 Pose Graph，但玩家实际得到的移动、闪避、转身、攻击和性能结果不得低于最近一次已闭环版本。

业务基准提交：

- Commit：`ccc103305027d5555db2f5ab64d3bb5f87d6b217`
- 提交说明：`闭环瑕疵修复`
- 时间：`2026-07-23T21:08:16+08:00`

本文不要求恢复旧 Animation Selection、BaseLocomotion producer、ActionOverride 表现路由或旧 Pose Graph。它只固定这些旧实现背后的业务结果，并规定新正式链路怎样表达同一结果。

## 2. 输入业务

### 2.1 普通移动

- `MoveAxis`由普通方向输入提供。
- 只有方向输入时，角色使用 Walk 业务。
- 普通方向输入不是 Run 或 Sprint。
- 无输入时进入 Idle。
- 普通移动链为：

```text
Idle
  -> WalkStart
  -> WalkLoop
  -> WalkEnd
  -> Idle
```

### 2.2 Shift

- 键盘`Left Shift`绑定的是`Dodge`。
- `Dodge`请求优先级为`100`，缓冲时间为`0.2s`。
- Shift 不得被 Pose Graph、Presentation Fact 或速度阈值解释为 Run 输入。

### 2.3 攻击

- 攻击只通过`Attack`请求进入有限 Action。
- 攻击动画、连段、取消、恢复窗口、伤害和攻击 RootMotion 继续由正式 Action、Timeline、Window、Motion与生命周期控制。
- 攻击 Slot 结束后回到当时正式 Locomotion Pose，不写死回 Idle、Walk 或 Run。

## 3. Walk 与 Run 的真实业务含义

### 3.1 Walk

Walk 是普通方向输入的默认移动方式。只按方向键时应播放 WalkStart、WalkLoop和WalkEnd，不应因为当前速度短暂高于某个表现阈值而自动变成 Run。

### 3.2 Run

当前输入配置没有独立 Run 或 Sprint 按键。基准提交中的 Run 主要表达“有方向的前闪避完成后保持冲势”的 Locomotion 状态，不是 Shift 按住期间的普通步态。

基准提交中的`RunStart`没有正式入边。前闪避恢复不经过 RunStart，而是直接进入 RunLoop：

```text
DodgeForward
  -> ActionOverride
  -> RunLoop
```

`ActionOverride`是旧动画表现接管状态，新系统不得恢复它。新系统必须保留的是“DodgeForward结束后，Gameplay Locomotion正式进入RunLoop”这个业务结果。

### 3.3 新系统的正式表达

新系统不得用`HorizontalSpeed > 6.68`推断 Run。速度是状态执行后的物理结果，不是 Walk、Run或Dodge恢复意图的来源。

正式链路应为：

```text
DodgeForward写入DirectionalDodgeRunIntent
  -> Action完成或合法Recovery退出
  -> Gameplay Locomotion消费Intent并进入RunLoop
  -> committed Locomotion Mode投影给Presentation
  -> PoseStateMachine选择RunLoop Pose Source
```

`DirectionalDodgeRunIntent`只决定 Gameplay Locomotion 模式，不直接点名动画资产。Pose Graph仍只消费正式 committed fact，不读取`Dodge`动作名，不恢复旧 ActionOverride 表现路由。

## 4. 闪避业务

### 4.1 闪避方向

- Shift按下且没有有效`MoveAxis`时进入`DodgeBack`。
- Shift按下且有有效`MoveAxis`时进入`DodgeForward`。
- `DodgeForward`沿当前输入方向冲出。
- 闪避方向不得由角色当前动画朝向替代输入方向。

### 4.2 前闪避恢复

`DodgeForward`进入时写入：

```text
HasDirectionalDodgeRunIntent = true
```

基准业务要求：

- 前闪避动作有效期间，Dodge Timeline拥有有限Action动画和闪避运动。
- 前闪避结束后，若没有更高优先级Action接管，应进入RunLoop。
- RunLoop进入时不得过早清除`HasDirectionalDodgeRunIntent`；该意图要保留到MovingTurn完成分流或Idle等终止状态消费。
- 不得因为前闪避末端速度下降、插值速度落入Walk区间或Action Slot淡出而选择Walk。

### 4.3 后闪避恢复

`DodgeBack`不建立前冲Run意图。动作结束后由正式Gameplay Locomotion结果决定回Idle或普通Walk，不得复用前闪避的Run恢复。

## 5. MovingTurn与TurnBack业务

### 5.1 触发

MovingTurn是Gameplay状态，不只是一个动画转场。

唯一触发条件是：

```text
RunLoop
  + 存在有效MoveAxis
  + Move方向与当前Body朝向夹角达到MovingTurnAngleThreshold=135°
  + Attack Action Context未激活
  + Dodge Action Context未激活
```

基准提交中，前闪避后的正式链是：

```text
DodgeForward
  -> RunLoop
  -> MovingTurn
  -> RunLoop
```

普通Walk不进入MovingTurn，继续使用正式Locomotion转向。是否进入MovingTurn必须以Gameplay StateMachine提交的状态为准，Presentation不得只靠插值后的`FacingError`重新猜一次。

当前正式状态机必须同时满足：

- 只有`RunLoop -> MovingTurn`使用`move_has + turn_facing_angle(135°) + !AttackContext + !DodgeContext`。
- RunEnd重新收到输入时先回到RunLoop，再由上述唯一门禁决定是否进入MovingTurn，不复制第二套条件。
- MovingTurn只以`state_root_completed`释放，不再读取Facing Error或释放角阈值。
- `MovingTurn -> RunLoop`、`MovingTurn -> WalkLoop`与`MovingTurn -> WalkEnd`共享Timeline完成事实，再分别读取Run意图和输入是否存在。
- Presentation的RunStart、RunEnd可以在观察到已提交MovingTurn事实后进入Turn Pose，但不构成第二Gameplay入口。

### 5.2 Body权威与短Root Motion

MovingTurn采用Gameplay Timeline独占Body Root Motion的正式边界：

- MovingTurn Graph只保留60Hz有限Inline Timeline，范围为0–28帧。
- 同一MotionCurve在前25帧完成固定180° yaw，后3帧保持180°收束。
- 29个贡献的累计X/Z为`(-0.9001478, 0.4623734)`，直接使用Root Motion Baker输出的Unity米制值。
- Timeline以`Local / Locomotion / Override / Priority 100 / ConsumeLowerChannels`提交Body运动，并经现有Accumulator、World Solver与KCC提交实际结果。
- MovingTurn期间不得同时运行输入朝向节点或Pose `RootOrientationWarp`，也不得按实际输入夹角缩放作者yaw。
- Pose或Graph运行内存不进入Rollback snapshot和网络协议。
- Action的MotionWarp继续只服务有限Action，不得被MovingTurn复用。

原始正式配对：

| 内容 | 资产 |
|---|---|
| Root Motion作者来源 | `Corin_TurnBack_WithWeaponRootmotion.anim`前28帧 |
| Pose表现 | Turn Sequence |
| Timeline范围 | `0..28` frame |
| 时长 | `0.4666667s` |
| 采样率 | `60Hz` |

正式累计终点：

| 通道 | 终点 |
|---|---:|
| X | `-0.9001478m` |
| Z | `0.4623734m` |
| Yaw | `180°` |

该曲线不做厘米到米的二次缩放，不清零横向分量，也不按目标角改写累计轨迹。

### 5.3 Pose衔接与脚部处理

正式顺序是：

```text
Gameplay Timeline提交短Root Motion
  -> World Solver与KCC提交Body
  -> Turn Pose Sequence按PresentationDelta播放
  -> 0.12秒进入Inertialization
  -> 0.30秒退出Inertialization
  -> FootPlacement处理地面接触
  -> FinalPose
```

当前FootPlacement不是FootLock，不得把两者混称。若后续仍出现支撑脚绕点滑动，应新增独立FootLock Pose能力；它只约束骨架脚部，不得重新取得Body运动所有权。

## 6. 动画表现业务

### 6.1 Locomotion

PoseStateMachine需要表达这些可见状态：

1. Idle
2. WalkStart
3. WalkLoop
4. RunStart
5. RunLoop
6. RunEnd
7. Turn

状态名称不是选择依据。选择依据必须来自正式committed Gameplay/Body/Intent事实。

其中：

- Walk与Run必须由正式Locomotion Mode区分。
- Turn必须与正式MovingTurn Gameplay状态对齐。
- Start、End和Turn等有限Pose必须按自己的播放完成事实退出。
- WalkLoop和RunLoop可以继续共享`Locomotion.Gait` Marker Group。
- 惯性混合只处理Pose连续性，不得改变Gameplay状态、RootMotion或恢复目标。
- RunStart、RunLoop与RunEnd进入Turn使用0.12秒Inertialization；Turn退出到RunLoop、WalkLoop或Idle使用0.30秒。
- Idle、WalkLoop与RunLoop进入时保留连续播放相位；WalkStart、RunStart、RunEnd与Turn等有限状态进入时重置。
- Gameplay提交新的Locomotion Mode后，PoseStateMachine必须在当前Presentation帧重新选择Transition，即使上一条Transition仍在等待惯性捕获或释放。
- 新Transition通过唯一Transition Routing提升selection、request和native control generation；Native Pose Program对旧Pose历史执行rebase，旧generation完成信号失效。
- `WalkEnd -> Idle`和任意Locomotion Pose进入`MovingTurn`不得等待上一条Transition完整握手结束，否则会分别表现为停步卡在WalkEnd，以及Turn动画晚于RootMotion开始。

### 6.2 Action Slot

- Locomotion PoseStateMachine持续产生基础Pose。
- Attack和Dodge通过AnimationSlot覆盖基础Pose。
- Slot退出后回到当帧正式Locomotion Pose。
- “当帧正式Locomotion Pose”必须已经包含前闪避后的RunLoop业务结果。
- Slot不得用动作名决定回RunLoop，也不得把速度阈值当作动作恢复规则。

### 6.3 IK与后处理

- Virtual Bone、TwoBoneIK、FootPlacement、Inertialization、Layer、Additive和Mask只处理Pose。
- 它们不得生成或修改Rollback Gameplay状态。
- 它们不得修改MovingTurn Gameplay Timeline提交的Body Root Motion。
- Pose Graph和Graph运行内存不进入Rollback snapshot或网络协议。

## 7. 修复前根因与正式结果

### 7.1 前闪避Run意图曾被过早清除

修复前`RunLoop`进入时立即清除`HasDirectionalDodgeRunIntent`，MovingTurn无法知道自己来自普通Walk还是前闪避恢复后的Run。正式结果是保留该意图直到`Idle`等终止状态消费，并由MovingTurn的两条释放边显式选择Walk或Run。

### 7.2 MovingTurn曾允许Walk直接进入并按Facing Error释放

修复前MovingTurn同时从Walk与Run进入，并用Facing Error释放，导致普通Walk被强制播放固定180°动作且状态可能提前退出。正式Document只保留RunLoop唯一入口，并只在Timeline完成后按`HasDirectionalDodgeRunIntent`与输入状态分流。

### 7.3 Presentation曾用速度猜Walk与Run

速度无法表达“普通输入是Walk、前闪避恢复是Run”的业务含义，也会被Action Motion干扰。正式PoseStateMachine只消费Gameplay提交的`presentation.movement-mode`，不再用速度重建第二套步态状态。

### 7.4 Turn表现曾与Gameplay状态分裂

修复前Turn Pose用`FacingError`重新猜测转身，可能与Gameplay StateMachine不同步。正式Turn Pose只跟随committed MovingTurn mode，播放固定Turn Sequence，不再捕获或缩放第二份目标角。

### 7.5 Yaw与位移曲线曾被拆成两个运行时所有者

修复前Gameplay输入朝向与Pose RootOrientationWarp分别修改Body和视觉yaw，X/Z又使用另一套运动结果，导致方向歪斜和脚步不一致。正式链由同一Gameplay Timeline独占0–28帧X/Z/yaw Root Motion，Pose只播放Turn Sequence，不再建立第二运动或朝向路径。

### 7.6 性能回归与修复结果

用户曾在同场景观测约`26 FPS`。Profiler把两个Local Fixed Actor的主要热点定位到`ThirdPerson.Presentation.Animation`：修复前约`14.8 ms`，其中Native Graph求值约`9.7 ms`。

本次修复没有关闭任何Pose能力，而是删除无效重复工作：

- 不活跃Sequence Player不再每帧清空整套203骨骼Pose缓存。
- Pose operation不再无条件清空所有贡献槽，只在新增实际贡献时清对应槽。
- Selected Pose Player、BlendStack、Native Pose Graph和Final Pose Writer四个既有`IAnimationJob`使用Burst执行。
- 修复后Animation段约`6.2–6.5 ms`，Native Graph约`1.7–1.9 ms`，Editor整帧约`13 ms`。

性能修复不得通过关闭正式Pose能力、跳过状态链或降低Rollback tick掩盖。

### 7.7 Action Slot快速重入

`SourcePoseEndpoint`表示Slot透传当帧Locomotion Pose。此时Action Blend Stack自身可以合法发布`NoPose`，因为它没有Action贡献；这个`NoPose`不等于上游Locomotion Pose缺失。

当下一段Action在`MaxBlendInTimeToReplaceNewest`窗口内进入时：

- 上一完成Stack值为Pose，必须捕获Stored Pose后再替换历史。
- 当前端点是Source Pose且上一完成Stack值为NoPose，必须直接替换无输出历史，不得伪造Stored Pose捕获。
- 当前端点是Action source或上一完成值为Invalid，仍必须拒绝缺少completed Pose的历史压缩。

该分支只修正Slot内部Action贡献历史，不改变Action Timeline、连招准入、RootMotion、Locomotion Source Pose或Inertialization Routing。

### 7.8 Pose Transition阻塞新Gameplay事实

原PoseStateMachine只在没有Active Transition时选择下一条Transition。WalkEnd已经完成且Gameplay已经提交Idle时，如果`WalkLoop -> WalkEnd`仍在等待惯性捕获或释放，Idle事实不会被消费；同样，Gameplay进入MovingTurn并开始RootMotion后，Turn Pose也可能等待上一条Locomotion Transition结束。

正式链路改为：

```text
committed movement-mode
  -> 当前Presentation帧重新选择Pose Transition
  -> 唯一Transition Routing提交更高selection generation
  -> Inertialization请求提升request generation并标记RebaseRequired
  -> Native Pose Program按新control generation切入目标Pose
  -> 旧capture/release completion因generation不匹配失效
```

该修复只恢复连续Transition抢占，不新增第二Routing入口，不修改Gameplay StateMachine、MovingTurn RootMotion曲线、IK或FootPlacement。

## 8. 大改后的唯一目标链

```text
Input
  -> Gameplay Request与MoveAxis
  -> BTSMTL Gameplay StateMachine
  -> Action/Locomotion/Motion唯一仲裁
  -> committed Body + Locomotion Mode + Action Sample
  -> Presentation Fact Projection
  -> PoseStateMachine
  -> state-local Pose Source
  -> AnimationSlot
  -> Inertialization/Blend/Layer
  -> TwoBoneIK/FootPlacement
  -> FinalPose
```

MovingTurn运动链：

```text
Gameplay进入MovingTurn
  -> 0–28帧Gameplay MotionCurve
  -> 累计(-0.9001478m, 0.4623734m, 180°)
  -> World/KCC提交Body
```

MovingTurn表现链：

```text
committed MovingTurn Locomotion Mode
  -> Turn Pose State
  -> Turn Sequence按PresentationDelta播放
  -> 0.12秒进入Inertialization
  -> Timeline完成后0.30秒退出Inertialization
  -> Pose后处理
  -> FinalPose
```

## 9. 修复顺序

1. 恢复本文业务基准，不先调Blend、IK或FootPlacement。
2. 删除MovingTurn输入运动与Pose RootOrientationWarp的重复所有权，让0–28帧Gameplay Timeline独占X/Z/yaw Root Motion。
3. 用`135°`作为唯一RunLoop进入门槛，并只以`state_root_completed`释放。
4. 让`DirectionalDodgeRunIntent`重新通过Gameplay正式进入RunLoop。
5. 新增或接通唯一committed Locomotion Mode事实。
6. 删除PoseStateMachine用速度定义Walk与Run的规则。
7. 让Turn Pose只跟随正式MovingTurn状态，按PresentationDelta播放，并用0.12秒进入与0.30秒退出。
8. 对账Action Slot退出后回到正确的RunLoop、Walk或Idle。
9. 在普通Walk、前闪避、TurnBack、攻击连段和双端场景分别采集CPU Profiler。
10. 动画与运动闭合后，再按实际脚滑证据决定是否新增独立FootLock能力。

## 10. 本地验收口径

### 普通移动

- 只按方向键播放Walk，不播放Run。
- 停止输入能够进入WalkEnd并回Idle。

### 前闪避

- Shift加方向进入DodgeForward。
- 角色沿输入方向冲出。
- 动作结束后立即进入RunLoop，不落到Walk。

### 后闪避

- Shift无方向进入DodgeBack。
- 动作结束后不继承前闪避Run意图。

### TurnBack

- RunLoop满足135°反向输入阈值且Action Context均未激活时稳定进入MovingTurn；WalkLoop不进入。
- MovingTurn动画与Gameplay Timeline Body Root Motion在同一已提交状态内开始。
- Body完整消费0–28帧X/Z/yaw Root Motion，并只在Timeline完成后释放。
- Pose不再通过RootOrientationWarp第二次修改yaw。
- Turn进入读取0.12秒Inertialization，退出读取0.30秒；RunLoop、WalkLoop与Idle不被强制重置到frame 0。
- 动画结束前不被RunEnd、Walk或其它Locomotion Pose覆盖。
- 前闪避链中的MovingTurn结束后回RunLoop。
- 普通Walk链中的MovingTurn结束后回WalkLoop。

### 攻击

- 五段攻击和闪避继续使用正确RootMotion。
- Action退出后回到当帧正式Locomotion Pose。
- 不出现旧Action残留、SourceIncomplete或参考姿势闪回。

### 性能

- 在相同Unity质量、窗口、Development设置和双端拓扑下对比。
- 不接受稳定停留在约26 FPS。
- 必须用Profiler明确最大CPU耗时模块后再修改。
