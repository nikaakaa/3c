# 角色动画状态系统路线规划

本文用于后续实现角色动画状态、分层播放、IK、预测回滚和编辑器能力前先读。目标不是复制 BBB，而是在当前 3C 项目的已有路径上逐步扩展，保证逻辑层、动画层、运动层和工具层边界清晰。

## 当前基线

当前基础移动链路已经存在：

```text
PlayerLocomotionController
  -> BasicLocomotionPipeline
  -> BasicLocomotionStateMachine
  -> MovementCommand
  -> MovementAnimationContext
  -> BasicLocomotionAnimancerPresenter
```

当前已具备的能力：

- 输入快照、移动意图、相机相对方向、移动命令和动画上下文已经分层。
- `BasicLocomotionStateMachine` 负责 `Idle / MoveStart / MoveLoop / MoveStop` 阶段流转。
- `LocomotionAnimationSetSO` 负责移动阶段到 Animancer key 的映射。
- `BasicLocomotionAnimancerPresenter` 负责 Animancer 播放，不负责业务仲裁。
- `BasicMovementConfigSO` 负责基础移动数值。

当前缺口：

- 动画播放参数还不够完整，缺少统一的 fade、speed、start time、exit duration。
- 状态机的 `MoveStopMinTime` 还不是由 `WalkEnd / RunEnd` 动画配置驱动。
- 还没有统一的动作状态数据、打断窗口、事件窗口、运动窗口。
- 还没有 FullBody / UpperBody / LowerBody / Additive / Weapon 等层级抽象。
- 还没有 IK 目标、权重曲线和动画事件的统一归属。
- 还没有面向预测回滚的纯数据状态快照。
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

状态机不直接操作 Animancer、CharacterController、Camera、Unity 对象引用。动画层不决定业务状态。运动层不散落在状态和动画事件里。

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

## 第一阶段：移动动画参数可配置

目标是先把当前 `Idle / MoveStart / MoveLoop / MoveStop` 的动画参数补齐，同时保持现有链路不分裂。

### 数据目标

新增或扩展移动动画配置项：

```text
LocomotionAnimationEntry
  Key
  FadeDuration
  Speed
  NormalizedStartTime
  ExitDurationMode
  ExitDurationOverride
```

`ExitDurationMode` 建议第一版支持：

```text
ClipLength
Override
```

最终逻辑层只读取解析后的纯数据：

```text
ResolvedAnimationTiming
  ExitDuration
```

### RunEnd 路径

`RunEnd` 属于 `MoveStop`。

期望行为：

```text
MoveLoop + 没输入
  -> MoveStop
  -> 播 RunEnd 或 WalkEnd

MoveStop + 没输入 + PhaseTime >= 当前 Stop 动画 ExitDuration
  -> Idle

MoveStop + 有输入
  -> MoveStart
  -> 立刻播 RunStart 或 WalkStart
```

关键原则：

- 状态机不问 Animancer 当前动画是否播完。
- 状态机读取动画配置解析出的 `ExitDuration`。
- 动画层只根据 phase、gait 和 key 播动画。
- 有输入打断 `MoveStop` 的优先级高于无输入回 `Idle`。

### 细任务

- [ ] 设计 `LocomotionAnimationEntry` 纯数据结构。
- [ ] 扩展 `LocomotionAnimationSetSO`，让每个移动动画 key 对应一份 entry。
- [ ] 保留旧字段迁移路径，避免现有资产立刻失效。
- [ ] 提供 `ResolveTiming(phase, gait, lastMovingGait)`。
- [ ] 将 `MoveStopMinTimeReached` 改为读取当前 stop 动画 exit duration。
- [ ] 保留 `MoveStop -> MoveStart` 的高优先级立即切换。
- [ ] 为 `WalkEnd` 和 `RunEnd` 分别写 EditMode 测试。
- [ ] 为“中途有输入立刻切到 MoveStart”写 EditMode 测试。
- [ ] 为缺少 Clip 且未配置 override 的情况写校验测试。
- [ ] 给手动验证步骤：无输入急停完整回 Idle，中途输入立即起步。

## 第二阶段：动作状态配置

目标是把闪避、跳跃、落地、攻击、受击、死亡等动作引入统一状态配置，而不是散落在 MonoBehaviour 或 Animancer 播放代码里。

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

- Dodge 可打断 MoveStop。
- Death 可强制打断任何状态。
- Attack 只能在 cancel window 后被 Dodge 打断。

### 细任务

- [ ] 定义 `CharacterStateId`。
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
  跳跃、闪避、翻滚、受击、死亡、全身攻击

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

- 扩展移动动画配置并改变 `MoveStop` 退出时间来源。
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
1. 移动动画参数可配置
2. RunEnd / WalkEnd exit duration 驱动 MoveStop
3. 最小动作状态配置
4. 最小打断规则
5. FullBody / UpperBody / LowerBody 层配置
6. 动作 timeline window
7. IK 命令数据
8. 可同步动画状态快照
9. 轻量编辑器
10. Timeline 编辑器
```

最重要的第一步是小：先让现有移动链路的数据更完整，证明逻辑层可以只读动画配置结果，而不是读取 Animancer 运行时状态。
