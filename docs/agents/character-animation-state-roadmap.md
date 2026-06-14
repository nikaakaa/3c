# 角色动画状态系统路线规划

本文用于后续实现角色动画状态、分层播放、IK、预测回滚和编辑器能力前先读。目标不是复制 BBB，而是在当前 3C 项目的已有路径上逐步扩展，保证逻辑层、动画层、运动层和工具层边界清晰。

## 当前基线

当前 FullBody base layer 已收束到统一层级角色逻辑状态机：

```text
PlayerFullBodyActionController
  -> FullBodyFramePipeline
  -> FullBodyActionRequestGate
  -> CharacterStateMachineRunner
  -> CharacterStateMachineDefinitionSO
  -> CharacterStateMachineFrame
  -> IBasicLocomotionMotionExecutor / IActionMovementExecutor
  -> BasicLocomotionAnimancerPresenter / ActionAnimationAnimancerPresenter
```

当前已具备的能力：

- 输入快照、移动意图、相机相对方向、移动命令和动画上下文已经分层。
- `CharacterConfigSO` 是角色正式根配置入口，`CharacterStateMachineDefinitionSO` 作为其 StateMachine 子配置提供 FullBody base layer 的统一状态树，默认资产为 `Assets/Configs/3C/Statemachine/DefaultCharacterStateMachine.asset`。
- 默认统一状态树显式包含 `FullBody/Locomotion/Idle`、`MoveStart`、`MoveLoop`、`MoveStop` 和 `FullBody/Action/Dodge`。
- `PlayerFullBodyActionController` 是唯一创建并推进 `CharacterStateMachineRunner` 的运行时 owner；`PlayerLocomotionController` 只作为 FullBody pipeline 下的 Locomotion adapter。
- Locomotion 四阶段和 Dodge 进入/退出 transition 都在同一张状态机配置中可见，不再由 Locomotion 特化状态机、Dodge runtime 或 FullBody 缝合 driver 分别决定。
- `CharacterStateMachineRunner` 只读取 `CharacterStateMachineContext` 中的纯数据事实：移动意图、输入请求、phase can exit、状态时间、请求优先级、当前状态标签和只读 runtime blackboard snapshot。
- `CharacterStateMachineFrame` 统一产出当前状态快照、基础移动输出、通用动作移动输出、动画请求、输入请求消费、Run latch 写入和状态事实。
- `PlayerLocomotionController` 保留输入读取、相机 Look、基础移动帧构建、运动 adapter 和基础移动动画 adapter，不再拥有独立状态切换权威。
- `PlayerLocomotionController` 持有 `CharacterRuntimeBlackboard`，作为第一版 Locomotion facts 写入权威和其它 runtime facts 的受控提交点。
- `FullBodyActionRequestGate` 复用动作侧 planner，把缓冲中的 Dodge 请求和 Locomotion TurnBack intent 映射为统一状态机 `CharacterInputRequestFact`；FullBody 控制器不内联单个动作的方向解析或优先级规则。
- `PlayerFullBodyActionController` 是 FullBody frame pipeline 的装配层，兼容 `Tick` 入口只调用同一条 pipeline。
- `FullBodyFramePipeline` 按 `ReadInput -> UpdateInputBuffer -> GameplayDecision -> BuildMotion -> ExecuteMotion -> PresentationBridge -> WriteSnapshotAndEvents` 编排一帧，统一状态机仍是状态权威。
- `PlayerFullBodyActionController` 只把 `CharacterStateMachineFrame` 和动作 Presenter 的只读播放进度转换为 Action / Animation facts，不直接持有可变黑板实例。
- `CharacterRuntimeBlackboard` 是 typed facts blackboard，第一版包含 Locomotion、Action、Animation、Debug facts；它保存纯数据 snapshot / restore state，不保存 Animancer runtime、UnityEngine.Object、Transform、Camera、CharacterController、InputAction、AnimationClip 或 TransitionAsset。
- Animancer TransitionLibrary alias 表负责基础移动动画资源和过渡参数。
- `RunLocomotionAnimationConfigSO` 作为当前基础移动 Walk/Run 配置资产，按 `BasicMovementPhase + BasicMovementGait` 解析 alias、phase exit policy 和 motion profile。
- `AnimationPhaseTimelineSampler` 把 phase config、phase time 和动画播放进度采样为 `CanExit`。
- `LocomotionMotionProfileSO` 保存基础移动动作的烘焙累计本地 X/Z 位移曲线和 yaw 曲线。
- `LocomotionPhaseMotionProfileBinding` 使用 `phase + gait + alias` 绑定 Profile，并通过 `motionMode` 显式控制 Profile 是否进入运行时消费。
- `AnimationMotionProfileSampler` 把播放进度窗口采样成动画运动贡献。
- `BasicMovementMotionFacts` 把动画运动贡献作为纯数据交给运动层。
- `MovementCommand` 可以携带输入驱动速度和烘焙动画位移，实际移动仍由 `IBasicLocomotionMotionExecutor` 执行。
- 默认基础移动配置已绑定 `MoveStop + Run + RunEnd -> Bake/DefaultRunEndMotionProfile`，并显式设置为 `AdditiveBakedMotion`。
- `DefaultRunEndMotionProfile` 使用 `Corin_RunEnd_Rootmotion` 作为 source，Animancer 仍播放现有 alias 配置的视觉动画。
- `Bake/DefaultRunMotionProfile` 这类 RunLoop 烘焙资产可以保留在配置中，但必须设置为 `Disabled`；在引入 loop/override 策略前不进入默认运行时消费。
- `BasicLocomotionAnimancerPresenter` 根据 `MovementAnimationContext` 中的 phase 和 gait 解析 alias key。
- `BasicLocomotionAnimancerPresenter` 负责 Animancer 播放并暴露只读播放进度，不负责业务仲裁或状态切换。
- `BasicMovementConfigSO` 负责基础移动 Walk/Run 数值。
- Shift 写入 `InputRequestBuffer` 后只作为 Dodge 请求事实进入统一状态机；是否消费请求由 `Locomotion/* -> Dodge` transition 和状态输出决定。
- `Dodge` 状态包含 `Directional` 和 `Backstep` 变体，运动距离/时长、立即转向、输入请求消费、Run latch 写入和动画 key 都通过状态机中的通用动作移动定义跟随状态或变体配置。
- `CharacterStateMachineRunner` 只解释状态输出中的通用动作移动定义，不直接读取 `DodgeActionConfig` 或 Dodge 专用距离/时长结构。
- `ActionAnimationAnimancerPresenter` 只消费统一状态机产出的 `CharacterStateAnimationRequest`，不再通过游离 `ActionAnimationProfileSO` 入口选择 Dodge 动画。
- 旧 `BasicLocomotionStateMachine`、`LocomotionStateGraphConfigSO`、`DodgeActionRuntime`、`DodgeFullBodyActionModule`、`FullBodyHfsmStateTreeBuilder/Driver`、`FullBodyActionSetSO` 和 `FullBodyActionAnimationSetSO` 已从运行时代码和 prefab 入口删除。
- 动作位移通过 `ActionMovementCommand -> IActionMovementExecutor` 进入 `CharacterMotionDriver`，动画 Presenter 不写 Transform，也不调用 `CharacterController.Move`。

当前缺口：

- Walk/Run 已作为基础移动档位接入，普通移动为 Walk，Shift Directional Dodge 完成后的 Run latch 可进入 Run；Shift held 不再直接决定基础移动 Run。
- 已有最小统一 `Dodge` 闭环，但还没有 Roll、Jump、Attack、Hit、Death 的具体状态内容。
- 统一状态机已有最小 transition 条件和输出模型，后续还需要补齐打断窗口、事件窗口、运动窗口和编辑器校验视图。
- 还没有 UpperBody / LowerBody / Additive / Weapon 等并行层级抽象。
- 还没有 IK 目标、权重曲线和动画事件的统一归属。
- 已有第一版角色运行时黑板 snapshot / restore，可随 `CharacterSimulationSnapshot` 进入本地预测回放；后续仍需要把 hitbox、timeline window、upper body 和表现事件 sequence 扩进去。
- 还没有动作 Timeline 编辑器。

## 总体方向

后续角色动画系统分成五层：

```text
输入与事实层
  收集输入、地面、速度、受击、武器、锁定、资源、网络确认等事实

状态仲裁层
  根据事实、优先级、打断规则和时间窗口决定状态切换

状态输出层
  输出动画命令、运动命令、IK 命令、表现事件和可同步快照

动画表现层
  使用 Animancer 根据命令播放 clip、layer、mask、fade、event

工具编辑层
  编辑动作数据、窗口、曲线、IK、事件、调试视图和校验报告
```

状态机不直接操作 Animancer、CharacterController、Camera、Unity 对象引用。动画层不决定业务状态。运动层不散落在状态和动画事件里。runtime blackboard 只承载 typed facts，不变成 BBB 风格大 `PlayerRuntimeData`，也不成为第二状态机。

## 与 BBB 的关系

BBB 的参考价值：

- Brain 统一装配状态和拦截器。
- Module SO 承载大量动画、运动和阈值数据。
- Interceptor 先处理高优先级意图。
- 动画数据包含 fade、end time、phase、曲线和事件。

不直接沿用 BBB 的部分：

- 不让大量状态内部互相 `ChangeState(GetState<T>())`。
- 不把优先级藏在状态类的调用顺序里。
- 不让动画播放层承担业务打断判断。
- 不依赖 BBB 运行时类型、prefab、namespace 或主链路。

本项目应吸收 BBB 的配置密度和拦截思路，但切换规则要收敛到统一的状态图、仲裁器和打断规则数据。

## 第一阶段：基础移动动画 phase 边界固化

目标是先固定当前 `Idle / MoveStart / MoveLoop / MoveStop` 到基础移动 phase config 的边界，同时保持现有链路不分裂。Walk/Run 是档位事实，不是逻辑 phase。

### 数据目标

不新增项目侧 clip/fade/speed 二次映射资产。基础移动 phase config 只保存 alias 和逻辑退出策略：

```text
Idle      alias=Idle      exitPolicy=Manual
MoveStart + Walk alias=WalkStart exitPolicy=AfterDuration
MoveLoop  + Walk alias=WalkLoop  exitPolicy=Manual
MoveStop  + Walk alias=WalkEnd   exitPolicy=OnAnimationEnd
MoveStart + Run  alias=RunStart  exitPolicy=AfterDuration
MoveLoop  + Run  alias=RunLoop   exitPolicy=Manual
MoveStop  + Run  alias=RunEnd    exitPolicy=OnAnimationEnd
```

实际 clip、fade、transition 参数由 Animancer TransitionLibrary 管理。

### RunEnd 路径

`RunEnd` 属于 `MoveStop`。

期望行为：

```text
MoveLoop + 没输入
  -> MoveStop
  -> 播 last moving gait 对应的 WalkEnd 或 RunEnd

MoveStop + 没输入 + PhaseCanExit
  -> Idle

MoveStop + 有输入
  -> MoveStart
  -> 立刻播当前输入档位对应的 WalkStart 或 RunStart
```

`RunEnd` 的位移补偿路径：

```text
MoveStop + RunEnd playback progress
  -> RunLocomotionAnimationConfigSO.ResolveMotionProfile(MoveStop, Run, RunEnd)
  -> AnimationMotionProfileSampler
  -> BasicMovementMotionFacts
  -> MovementCommand
  -> IBasicLocomotionMotionExecutor
```

关键原则：

- 状态机不直接问 Animancer 当前动画是否播完，只读取 `PhaseCanExit` 纯数据事实。
- 状态机不读取具体动画 key、clip 名或 alias 表。
- 动画层只根据 phase + gait 解析 alias key、请求 Animancer 播放并暴露播放进度快照。
- 动画事实层把 `Manual / AfterDuration / OnAnimationEnd` 采样成 `CanExit`。
- 烘焙运动事实只影响本帧如何移动，不决定是否切状态。
- `RunEnd` 自然结束切到 `Idle` 的同一帧可以消费最后一段烘焙位移。
- 有输入打断 `MoveStop` 的优先级高于无输入回 `Idle`。
- `MoveStop` 中途有输入切 `MoveStart` 后，旧 `RunEnd` 的剩余烘焙位移不再继续推动角色。
- Animancer Presenter 不调用 `CharacterController.Move`，也不打开基础移动完整 `Animator.applyRootMotion`。
- Sprint 不放入 Walk/Run gait；如果它需要资源、打断、耐力或额外输入规则，应进入后续动作/FullBody 状态设计。

### 细任务

- [x] 删除 `LocomotionAnimationSetSO` 和相关二次映射模型。
- [x] 删除默认基础移动动画集资产。
- [x] 移除 prefab / scene 中的 `animationSet` 绑定。
- [x] Presenter 从 Run phase config 解析标准 Animancer alias key。
- [x] `MoveStart -> MoveLoop` 和 `MoveStop -> Idle` 读取 phase exit timing。
- [x] `MoveStop / RunEnd` 可通过 `OnAnimationEnd` 采样 `PhaseCanExit`，不再依赖手填动画长度。
- [x] `MoveStop / RunEnd` 可通过烘焙 Motion Profile 采样急停位移，减少胶囊停住但脚步继续刹车的滑步。
- [x] 默认 `RunEnd` Profile 使用 Rootmotion 参考 clip 烘焙，视觉播放仍由 Animancer alias 表管理。
- [x] 烘焙位移通过 `MovementCommand -> IBasicLocomotionMotionExecutor` 生效，不新增第二套移动路径。
- [x] 保留 `MoveStop -> MoveStart` 的高优先级立即切换。
- [x] 为“项目不再引用二次动画映射资产”写 EditMode 测试。
- [ ] 手动验证：无输入急停播放 `RunEnd` alias，中途输入立即起步。
- [ ] 手动验证：无输入急停时胶囊随 RunEnd 烘焙位移继续刹车，旧 RunEnd 位移不会在中途输入后继续生效。

## 第二阶段：动作状态配置

目标是把闪避、跳跃、落地、攻击、受击、死亡等动作引入统一状态配置，而不是散落在 MonoBehaviour 或 Animancer 播放代码里。当前 `Action.Dodge` 已先作为最小 FullBody 垂直切片落地，后续应把它沉淀进通用动作状态配置和 timeline/window 数据，而不是扩成第二套角色控制器。

### 数据目标

```text
CharacterActionStateDefinition
  StateId
  LayerId
  AnimationKey
  Priority
  Tags
  EntryConditions
  ExitPolicy
  InterruptPolicy
  MotionPolicy
  EventTimeline
```

```text
CharacterStateTransitionDefinition
  FromState
  ToState
  Priority
  Conditions
  ForceInstantly
```

```text
InterruptPolicy
  Mode
  InterruptibleAfter
  AllowedInterruptTags
  AllowedInterruptStates
  ForcedByTags
```

第一版不要做大而全，只做最小可测闭环：

- Shift pressed 生成 Dodge 请求，有方向时 Directional 冲刺，无方向时 Backstep 后闪。
- Dodge 可从 `FullBody/Locomotion/*` 进入，并通过统一状态机 transition 与请求消费输出处理输入请求。
- Directional 完成后设置 Run latch；Backstep 完成后不强制进入 Run。
- Death 可强制打断任何状态。
- Attack 只能在 cancel window 后被 Dodge 打断。

### 细任务

- [x] 完成 `Action.Dodge` 最小 FullBody 垂直切片，接入输入缓冲、仲裁、tracker、动作位移和动作动画 Profile。
- [x] 配置 `Action.Dodge.Directional` / `Action.Dodge.Backstep` 稳定动画 key，并保持动作逻辑不引用具体可琳 clip。
- [x] Shift held 不再直接驱动基础移动 Run；Directional 完成后通过 Run latch 进入 Run。
- [ ] 定义通用 `CharacterStateId`。
- [ ] 定义 `CharacterStateTag`。
- [ ] 定义 `CharacterLayerId`。
- [ ] 定义 `CharacterActionStateDefinition`。
- [ ] 定义 `CharacterStateTransitionDefinition`。
- [ ] 定义 `InterruptPolicy`。
- [ ] 写状态配置校验器。
- [ ] 写状态图 builder，不让状态类互相查找。
- [ ] 写纯逻辑状态机测试。
- [ ] 写强制打断测试。
- [ ] 写 cancel window 测试。

## 第三阶段：动画分层

目标是支持 FullBody、UpperBody、LowerBody、Additive、Weapon 等层，给后续瞄准、射击、近战、武器、表情和受击表现留空间。

### 层级建议

```text
FullBody
  Locomotion 局部子树
  Action.Dodge
  后续 Jump / Roll / Attack / Hit / Death 等只能作为 FullBody/Action/* 子状态扩展

LowerBody
  移动循环、起步、急停、转身

UpperBody
  瞄准、射击、装填、上半身攻击

Additive
  后坐力、呼吸、受击轻抖、瞄准偏移

Weapon
  武器骨骼、刀光、枪械机构、配件动画

Face
  表情、眨眼、口型
```

### 关键数据

```text
AnimationLayerConfig
  LayerId
  AnimancerLayerIndex
  AvatarMask
  DefaultWeight
  BlendMode
  SyncSourceLayer
```

```text
LayerConflictRule
  RequestingLayer
  BlocksLayers
  CanBlendWithLayers
  WeightPolicy
```

### 细任务

- [ ] 建立 `CharacterAnimationLayerConfigSO`。
- [ ] 给 Animancer Presenter 增加 layer 解析，不改业务状态。
- [ ] 支持 FullBody 覆盖 UpperBody / LowerBody。
- [ ] 支持 UpperBody 与 LowerBody 并行。
- [ ] 支持 Additive 权重曲线。
- [ ] 支持 Weapon 层跟随动作状态。
- [ ] 写层冲突规则测试。
- [ ] 写同一帧多层动画命令排序测试。

## 第四阶段：动作事件与窗口

目标是统一攻击判定、可取消窗口、输入缓存消费、运动窗口、特效、音效和 IK 开关。

### 窗口类型

```text
Startup
Active
Recovery
Cancel
InputBufferConsume
Hitbox
Hurtbox
Motion
IK
VFX
SFX
Camera
```

### 数据目标

```text
ActionTimelineWindow
  Type
  StartNormalizedTime
  EndNormalizedTime
  PayloadId
```

```text
ActionTimelineEvent
  Type
  NormalizedTime
  PayloadId
```

### 细任务

- [ ] 定义窗口数据。
- [ ] 定义事件数据。
- [ ] 写 timeline evaluator。
- [ ] 状态机只读取窗口事实，不直接播放 VFX/SFX。
- [ ] 表现事件进入事件队列。
- [ ] 输入缓存只在允许窗口内消费。
- [ ] 攻击判定只在 hitbox window 内开启。
- [ ] 写窗口边界测试。
- [ ] 写同一帧进入多个窗口的排序测试。

## 第五阶段：IK

目标是让 IK 成为状态输出的一部分，而不是单独在动画脚本里临时处理。

### IK 类型

```text
FootIK
HandIK
AimIK
LookAtIK
WeaponIK
InteractionIK
```

### 数据目标

```text
IKRequest
  IKType
  TargetId
  Weight
  PositionWeight
  RotationWeight
  Priority
  Space
```

```text
IKWindow
  IKType
  StartNormalizedTime
  EndNormalizedTime
  WeightCurve
  TargetPolicy
```

### 细任务

- [ ] 定义 IK 命令数据，不引用场景对象。
- [ ] 定义 IK target provider 接口。
- [ ] 动作 timeline 输出 IKRequest。
- [ ] IK runtime 根据 target id 解析 Transform。
- [ ] FullBody 动作可关闭 FootIK。
- [ ] Aim 状态可输出 AimIK。
- [ ] 交互动作可输出 HandIK。
- [ ] 写 IK 请求优先级测试。
- [ ] 写 IK 窗口权重采样测试。

## 第六阶段：预测回滚

目标是让状态、动画时间、事件和动作窗口可以进入可同步、可预测、可回滚的数据流。

### 可同步快照

```text
AnimationStateSnapshot
  Tick
  FullBodyStateId
  UpperBodyStateId
  LowerBodyStateId
  StateElapsedTicks
  AnimationKeyId
  NormalizedTimeFixed
  LayerWeights
  MotionToken
  EventSequence
```

### 原则

- 不同步 Animancer 对象。
- 不同步 Unity Object 引用。
- 不同步场景实例引用。
- 使用稳定 ID、tick、定点或可控精度数据。
- 表现事件要有 sequence，避免回滚后重复播放音效和特效。
- 本地预测可以先播表现，服务器纠正时按快照重采样状态。

### 细任务

- [ ] 给状态 ID 和动画 key 建立稳定映射。
- [ ] 定义 `AnimationStateSnapshot`。
- [ ] 定义 `AnimationCommandSnapshot`。
- [ ] 定义事件去重 sequence。
- [ ] 状态机支持从 snapshot 恢复。
- [ ] timeline evaluator 支持按 tick 采样。
- [ ] 运动命令和动画命令共用 tick。
- [ ] 写 snapshot round-trip 测试。
- [ ] 写重复事件去重测试。
- [ ] 写回滚后窗口重新采样测试。

## 第七阶段：编辑器

编辑器不要先行变成运行时核心。先保证数据和测试稳定，再做工具。

### 编辑器顺序

```text
普通 Inspector
  先能配，能校验，能运行

轻量窗口编辑器
  编辑 cancel / hitbox / motion / IK window

调试面板
  显示当前状态、层、窗口、IK、事件、pending transition

Timeline 编辑器
  多轨编辑动画、窗口、事件、曲线、IK 和预览
```

### Timeline 轨道

```text
Animation Track
Layer Weight Track
Motion Track
Cancel Track
Hitbox Track
Input Buffer Track
IK Track
VFX Track
SFX Track
Camera Track
Debug Marker Track
```

### 细任务

- [ ] 先写 asset validator。
- [ ] 做 inspector 校验提示。
- [ ] 做窗口列表编辑。
- [ ] 做动作预览采样，不写入运行时状态。
- [ ] 做当前状态调试面板。
- [ ] 做 timeline 轨道编辑。
- [ ] 做一键生成校验报告。
- [ ] 做 clip length、event window、exit duration 一致性检查。

## 不做的事

- 不新增未审批的独立角色控制器。
- 不让状态类到处直接切别的状态。
- 不让动画 Presenter 决定业务状态。
- 不让 IK 直接绕过状态输出。
- 不让 Root Motion 到处直接写 Transform 或 CharacterController。
- 不让编辑器数据结构反过来绑死运行时。
- 不把 BBB 运行时作为依赖。

## OpenSpec 要求

以下内容进入实现前必须走 OpenSpec：

- 新增 `OnMarker`、cancel window、hitbox window、IK window 或其它 Timeline Fact。
- 新增项目侧动画事件、动作窗口或动画反馈逻辑层机制。
- 引入动作状态配置和打断规则。
- 引入动画 layer 配置。
- 引入动作 timeline/window 数据。
- 引入 IK 命令管线。
- 引入预测回滚快照。
- 引入会写运行时数据资产的编辑器。

每个 OpenSpec 必须：

- 中文说明。
- 任务颗粒度细。
- 包含 EditMode 测试。
- 包含手动验证步骤。
- 不绕过当前系统另开路径。

## 推荐推进顺序

```text
1. 基础移动动画 alias 边界固化
2. 最小 AnimationPhaseTimelineFact：CanExit
3. 最小基础移动 Motion Profile Facts：RunEnd 急停位移
4. 最小动作状态配置
5. 最小打断规则
6. FullBody / UpperBody / LowerBody 层配置
7. 动作 timeline window
8. IK 命令数据
9. 可同步动画状态快照
10. 轻量编辑器
11. Timeline 编辑器
```

最重要的第一步是小：先确认现有移动链路只消费逻辑事实、Timeline Fact 和 Motion Fact，动画资源只由 Animancer alias 表管理，避免出现第二张动画映射表。
