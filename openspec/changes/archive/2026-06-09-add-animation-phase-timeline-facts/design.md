## Context
当前基础移动链路已经是：

```text
PlayerLocomotionController
  -> BasicLocomotionPipeline
  -> BasicLocomotionStateMachine
  -> MovementCommand
  -> MovementAnimationContext
  -> BasicLocomotionAnimancerPresenter
```

`RunLocomotionAnimationConfigSO` 目前保存 `Idle / MoveStart / MoveLoop / MoveStop` 的 alias、退出策略和退出时长。`BasicLocomotionStateMachine` 通过状态图条件判断 `MoveStart -> MoveLoop` 和 `MoveStop -> Idle`，`BasicLocomotionAnimancerPresenter` 只按 alias 请求 Animancer 播放。

问题是：`RunEnd` 如果要“播完再 Idle”，不应该继续靠设计者手填一个和 clip 长度一致的秒数，也不应该让 Presenter 注册 Animancer `OnEnd` 后直接切状态。需要一个中间事实层，把动画播放进度转换成纯数据事实，再交给逻辑层判断。

## Goals
- 支持 `MoveStop / RunEnd` 通过 `OnAnimationEnd` 产生 `CanExit`。
- 保留 `AfterDuration`，让 `MoveStart` 和已有配置继续工作。
- 让 Locomotion 状态图读取 `PhaseCanExit`，而不是直接读取 Animancer。
- 让 Presenter 只暴露只读播放进度，不决定状态切换。
- 为后续 marker、window、IK、Timeline 编辑器、预测回滚留同一条数据路径。

## Non-Goals
- 不做可视化 Timeline 编辑器。
- 不实现 `OnMarker` 运行时退出。
- 不实现 attack cancel window、hitbox window、combo window。
- 不实现 fullbody、upperbody、lowerbody 层级命令。
- 不实现 IK 曲线或目标解析。
- 不改 Root Motion 权威。
- 不新增 BBB 运行时依赖。

## Decisions

### Decision: Timeline Fact 是动画数据层，不是逻辑状态
`CanExit` 来自动画事实采样，但状态机仍然决定是否切换。

```text
动画播放层:
  AnimancerPresenter 播放 alias，并暴露当前 phase 的播放进度

动画事实层:
  TimelineFactSampler 根据 phase config + phaseTime + playback progress 产出 CanExit

逻辑层:
  LocomotionStateGraph 根据 NoMoveIntent + PhaseCanExit 切 Idle
  LocomotionStateGraph 根据 HasMoveIntent 立即从 MoveStop 切 MoveStart
```

这样 `RunEnd` 的规则是：

```text
MoveStop + 没输入 + CanExit -> Idle
MoveStop + 有输入 -> MoveStart
```

### Decision: Presenter 不注册 OnEnd 驱动 Locomotion
BBB 的 `PlayerStopState` 使用 Animancer `OnEnd` 回调回 Idle，中途输入由状态逻辑打断。这个做法适合参考，但不适合作为当前项目的长期边界。

本项目需要预测回滚和可测试纯数据状态，Animancer `OnEnd` 回调不能成为逻辑状态切换的权威。Presenter 可以读取当前 `normalizedTime` 和 `isEnded`，但不能调用状态机，也不能输出“我要切 Idle”的命令。

### Decision: sampler 支持 Manual / AfterDuration / OnAnimationEnd
第一版 sampler 只输出一个事实：

```text
PhaseCanExit
```

规则：

```text
Manual:
  CanExit = false

AfterDuration:
  CanExit = phaseTime >= exitDuration

OnAnimationEnd:
  CanExit = playbackProgress.IsEnded
```

`OnMarker` 暂不实现。后续 Timeline 编辑器落地时，`OnMarker` 和窗口事实会复用 sampler 的入口，而不是另起一条系统。

### Decision: 播放进度快照是纯数据
播放进度快照只允许包含：

```text
phase
aliasKey
normalizedTime
isEnded
hasValidPlayback
```

它不能携带 `AnimancerState`、`AnimationClip`、`TransitionAsset`、`UnityEngine.Object` 或场景实例引用。读取 Animancer 的代码只存在于 Presenter 内。

### Decision: Movement 状态图只读取 movement facts
`BasicLocomotionStateMachine` 不依赖 `ThirdPersonAnimation` 命名空间。Controller 或 pipeline 组装层负责把动画 sampler 输出转换为 movement 可读的纯数据，例如：

```text
BasicMovementPhaseFacts
  PhaseCanExit
```

状态图 context 增加 facts 后，`PhaseCanExit` 条件只读取该事实，不知道事实来自 `AfterDuration`、`OnAnimationEnd` 还是未来 marker。

### Decision: 文件夹按现有分层延伸
建议新增路径：

```text
Assets/Scripts/Character/Animation/Model/
  AnimationPhasePlaybackProgress.cs
  AnimationPhaseTimelineFacts.cs

Assets/Scripts/Character/Animation/Solver/
  AnimationPhaseTimelineSampler.cs

Assets/Scripts/Character/Movement/Model/
  BasicMovementPhaseFacts.cs

Assets/Scripts/Character/Movement/Solver/
  LocomotionStateGraphConditionEvaluator.cs

Assets/Scripts/Character/Animation/Runtime/
  BasicLocomotionAnimancerPresenter.cs
```

如果实现时发现需要让 Movement 层直接引用 Animation 层，必须停止并重新调整设计；Movement 的纯逻辑状态机应只读取 movement facts。

## Future Timeline Editor Architecture
本变更只做运行时事实层。未来编辑器应该写入数据资产，而不是变成运行时核心。

```text
Editor Authoring
  TimelineEditor / Inspector
        |
        v
Data Assets
  AnimationTimelineDefinition
  Markers / Windows / Events / IK Curves
        |
        v
Compiler / Validator
  校验 clip、marker、window、payload、层级冲突
        |
        v
Runtime Sampler
  根据 phase time、tick 或 normalized time 采样 facts
        |
        v
Logic Layer
  StateMachine / ActionArbiter 根据 facts 切状态
        |
        v
Presentation Layer
  AnimancerPresenter 播放，不仲裁
```

后续扩展时，`CanExit` 会变成一组 facts：

```text
CanExit
CanCancel
HitActive
InputBufferOpen
FootPlant
IKActive
EventTriggered
```

## Risks / Trade-offs
- 如果 `OnAnimationEnd` 配置了但没有播放进度来源，`CanExit` 应保持 false，避免静默跳过动画。测试和配置校验需要覆盖这个场景。
- 从 Presenter 读取播放进度会比 Animancer `OnEnd` 回调晚到状态机一个 frame，这是可以接受的最小代价；未来 tick 化时可改为按确定性动画时间采样。
- 当前存在活跃变更 `add-locomotion-animation-phase-exit-policy`，本变更需要基于它的 phase config 结构继续做，实施前应确认该变更已通过用户验证。

## Migration Plan
1. 先保留 `AfterDuration` 行为和测试，确保当前 MoveStart、MoveStop 行为不回退。
2. 增加 `OnAnimationEnd` 策略和 sampler，不改默认资产前先用测试证明行为。
3. 增加 Presenter 播放进度快照，只读暴露，不参与状态切换。
4. 增加 `PhaseCanExit` 条件，把默认 `MoveStart -> MoveLoop` 和 `MoveStop -> Idle` 切到 facts。
5. 将 `DefaultRunLocomotionAnimationConfig` 的 `MoveStop` 配置迁移为 `OnAnimationEnd`。
6. 手动验证 `RunEnd` 无输入播完回 Idle，中途输入立即起步。

## Open Questions
- `MoveStart` 第一版继续使用 `AfterDuration`，还是也允许 `OnAnimationEnd`？建议实现支持，但默认继续用 `AfterDuration`。
- `OnAnimationEnd` 的判定是否使用 Animancer `NormalizedEndTime` 还是 `NormalizedTime >= 1`？建议第一版以 Presenter 暴露的 `isEnded` 为唯一输入，具体读取方式留在 Presenter 内。
