# Design: 基础移动烘焙运动 Profile Facts

## Context
当前基础移动链路已经分成：

```text
PlayerLocomotionController
  -> BasicLocomotionPipeline
  -> BasicLocomotionStateMachine
  -> MovementCommand
  -> IBasicLocomotionMotionExecutor
  -> BasicLocomotionAnimancerPresenter
```

`RunEnd` 的退出已经由 `OnAnimationEnd -> PhaseCanExit` 解决，但它只回答“什么时候能回 Idle”。它没有回答“RunEnd 播放期间胶囊应该怎么按照动画刹车位移移动”。如果不处理，动画脚步会沿着原地或 root 轨迹刹车，角色胶囊却因为无输入速度为 0 而停住，滑步会很明显。

BBB 里有三类可参考内容：

- `RootMotionExtractor`：离线采样动画 root 轨迹，生成速度、旋转和脚相。
- `MotionClipData`：保存烘焙后的速度曲线、旋转曲线、脚相等运行时数据。
- `MotionDriver`：运行时根据曲线或输入计算速度并统一移动 CharacterController。

本项目可以复用 BBB 的采样算法思想，但不能依赖 BBB runtime、namespace、Prefab、SO 或主控链路。复制的局部代码必须改成当前项目自己的模块和命名。

## Goals
- 让 `MoveStop / RunEnd` 的烘焙位移进入统一运动出口，降低急停滑步。
- 保持动画外观层只负责播放和只读进度，不调用移动 API。
- 保持状态机不依赖 Animancer、AnimationClip、Root Motion 或 BBB 类型。
- 使用纯数据 facts 把动画烘焙运动交给逻辑/运动层。
- 第一版支持非循环动作的累计本地位移采样，优先覆盖 `RunEnd`。
- 为后续 `MoveStart`、转身、闪避、翻越、Motion Warping、预测回滚和 Timeline 编辑器保留同一条数据路径。

## Non-Goals
- 不直接启用 `Animator.applyRootMotion` 驱动基础移动。
- 不在 Presenter、状态机或状态类里调用 `CharacterController.Move`。
- 不实现完整 Motion Warping。
- 不实现闪避、翻滚、跳跃、翻越或攻击动作位移。
- 不实现完整 Timeline 编辑器。
- 不同步 Unity 对象、Animancer runtime 对象或 AnimationClip 引用。
- 不复制 BBB 的 `MotionDriver`、`BBBCharacterController` 或状态主线。

## Proposed Data Flow
第一版运行时链路：

```text
AnimancerPresenter
  只读输出 playback progress
        |
        v
LocomotionMotionProfileResolver
  根据 phase + alias 找到 profile
        |
        v
AnimationMotionProfileSampler
  previous normalized time + current normalized time + profile
        |
        v
BasicMovementMotionFacts
  localPlanarDelta / yawDelta / hasMotionContribution
        |
        v
BasicLocomotionPipeline / MovementCommand
  合成输入驱动位移和动画烘焙位移
        |
        v
IBasicLocomotionMotionExecutor
  仍是唯一实际移动出口
```

编辑器烘焙链路：

```text
Designer selects:
  target prefab + animation clip + phase + alias
        |
        v
LocomotionMotionProfileBaker
  参考 BBB RootMotionExtractor 采样 root 轨迹
        |
        v
LocomotionMotionProfileSO
  cumulativeLocalX / cumulativeLocalZ / cumulativeYaw / duration / source metadata
        |
        v
RunLocomotionAnimationConfigSO
  通过 phase + alias 绑定 profile
```

## Proposed Folder Shape
运行时：

```text
Assets/Scripts/Character/Animation/Config/
  LocomotionMotionProfileSO.cs
  LocomotionPhaseMotionProfileBinding.cs

Assets/Scripts/Character/Animation/Model/
  AnimationMotionPlaybackWindow.cs
  AnimationMotionProfileSample.cs

Assets/Scripts/Character/Animation/Solver/
  AnimationMotionProfileSampler.cs
  LocomotionMotionProfileValidator.cs

Assets/Scripts/Character/Movement/Model/
  BasicMovementMotionFacts.cs
  MovementCommand.cs

Assets/Scripts/Character/Movement/Solver/
  BasicLocomotionPipeline.cs
  MovementCommandBuilder.cs

Assets/Scripts/Character/Movement/Runtime/
  PlayerLocomotionController.cs
  CharacterControllerBasicMotionExecutor.cs
```

编辑器：

```text
Assets/Editor/Character/Animation/
  LocomotionMotionProfileBakerWindow.cs
  LocomotionMotionProfileBakeUtility.cs
```

测试：

```text
Assets/Tests/Editor/
  PlayerLocomotionControllerTests.cs
```

## Decisions

### Decision: 保存累计位移曲线，而不是每帧速度曲线
BBB 的 `MotionClipData` 保存 `SpeedCurve` 和 `RotationCurve`。本项目第一版建议保存：

```text
cumulativeLocalX(normalizedTime)
cumulativeLocalZ(normalizedTime)
cumulativeYaw(normalizedTime)
```

采样时用当前 normalized time 的累计值减去上一帧累计值，得到本帧 delta。

Reason: 累计曲线更适合不同帧率、tick、预测回放和中途打断，不需要依赖当前帧的速度积分精度。速度曲线可以作为调试或未来平滑数据保留，但第一版运动权威使用累计差值。

### Decision: Profile 通过 phase + alias 绑定，不复制动画播放表
`RunLocomotionAnimationConfigSO` 已经持有 phase alias 和 exit policy，Animancer TransitionLibrary 继续负责 clip、fade、speed 等播放参数。Motion Profile 只保存烘焙运动数据。

第一版用一个可扩展 binding：

```text
LocomotionPhaseMotionProfileBinding
  Phase
  AliasKey
  Profile
```

Resolver 必须同时匹配 phase 和 alias，避免 `MoveStop` 播 `RunEnd` 却采样到其它动画的运动曲线。

### Decision: 逻辑层读取 facts，不读取 Profile 资产细节
`PlayerLocomotionController` 或等价组装层可以解析 profile，并调用 sampler。`BasicLocomotionStateMachine` 不读取 profile，不引用 `ThirdPersonAnimation`，不引用 `AnimationCurve`。

Movement 逻辑读取的是：

```text
BasicMovementMotionFacts
  HasAnimationMotion
  LocalPlanarDelta
  YawDelta
  SourcePhase
  SourceAliasKey
```

`MoveStop` 的规则仍然是状态图规则；烘焙运动只影响本帧如何移动，不决定是否切状态。

### Decision: 动画外观层不直接移动角色
`BasicLocomotionAnimancerPresenter` 继续关闭基础移动的 `Animator.applyRootMotion`。它只暴露播放进度，不调用 `CharacterController.Move`、不写 Transform、不输出 MovementCommand。

Reason: 如果 Presenter 直接移动角色，会产生第二条位移路径，后续预测回滚、KCC 替换、动作位移和 Motion Warping 都会变复杂。

### Decision: MotionExecutor 统一合成输入位移和动画位移
第一版 `MoveStop` 没有输入，输入驱动速度为 0，烘焙运动 delta 让胶囊按 `RunEnd` 轨迹继续刹车。

```text
MoveStop + no input:
  input planar velocity = 0
  animation local delta = RunEnd profile delta
  executor 执行 animation delta + gravity

MoveStop + input:
  状态机先切 MoveStart
  RunEnd profile 不再继续贡献
  executor 回到输入驱动或起步 profile
```

如果未来 `MoveStart` 也有 profile，可以用同一套 facts 做起步位移。

### Decision: 第一版不做完整方向重定向和 Warping
RunEnd profile 默认按角色当前朝向把 local delta 转成 world delta。第一版不把 stop 轨迹 warp 到新目标点，不做复杂脚锁定和地形修正。

Reason: 先解决“动画里有位移但胶囊不动”的主要滑步，再单独规划 Motion Warping、足底 IK 和地形适配。

### Decision: BBB 代码只可复制算法，不可复制主链路
允许参考并改写：

- root transform 采样方式
- 累计位移、偏航提取方式
- 脚相判定方式
- curve 平滑工具

不得引入：

- `BBBNexus` namespace
- `BBBCharacterController`
- BBB `PlayerRuntimeData`
- BBB `MotionDriver`
- BBB PlayerSO runtime 依赖
- BBB 状态类互跳主线

## Risks / Trade-offs
- 如果 profile 和实际 Animancer transition clip 不一致，运动会错位。缓解：校验 `phase + alias`，记录 source clip 名和 guid，并提供静态/Editor 校验。
- 使用 normalized time 窗口采样时，如果动画被从中途播放或被重播，需要重置 previous normalized time。缓解：phase/alias 变化时清空上一帧窗口。
- 当前没有完整 Timeline 编辑器，第一版配置可能偏手工。缓解：先提供最小 Baker/Inspector 和测试，后续再做可视化工具。
- 累计曲线不解决脚底接触锁定。缓解：后续 IK window 和 foot phase 单独提案。

## Migration Plan
1. 先只新增 Profile 数据和 sampler 测试，不接入运动执行。
2. 再把 `PlayerLocomotionController` 采样到的 motion facts 传入 pipeline。
3. 再扩展 `MovementCommand` 和 executor，确认所有实际移动仍在 motion executor 内。
4. 再给 `MoveStop / RunEnd` 绑定 profile，并用手动 Play Mode 验证急停滑步减少。
5. 最后补编辑器 Baker 和资产校验，防止 profile 与 alias/clip 不一致。

## Open Questions
- 第一版 Baker 输出是否同时保存速度曲线用于调试？建议保存累计曲线作为权威，速度曲线可暂不实现。
- `MoveStart` 是否同时绑定 profile？建议本变更先只绑定 `MoveStop / RunEnd`，结构允许后续扩展。
- RunEnd 中途被输入打断时是否要补偿剩余位移？建议第一版不补偿，直接切到新 phase 的运动来源。

