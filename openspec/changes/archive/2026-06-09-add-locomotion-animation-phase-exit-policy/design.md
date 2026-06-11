# Design: 基础移动动画阶段退出策略

## Context

现在的边界已经基本正确：Animancer 管播放，状态机管流转，Presenter 只把 phase 转成 alias。但当前数据还有一个小刺：`RunEnd` 有单独的 `runEndExitDuration`，而 `MoveStart` 的等待时间还在 `BasicMovementConfigSO` 里。

这会带来两个问题：

- `RunEnd` 看起来像特殊状态，而不是 `MoveStop` 阶段的配置。
- 起步和停止都属于动画阶段节奏，但配置来源不一致。

本设计把四个基础移动阶段统一到同一个 phase config 结构中。

## Goals

- 让 `Idle / MoveStart / MoveLoop / MoveStop` 都有统一配置形状。
- 让 `RunEnd` 等待时间从顶层特例字段变成 `MoveStop` 的退出策略。
- 让 `MoveStart -> MoveLoop` 和 `MoveStop -> Idle` 都能从 phase timing 读取时长。
- 保持状态机不依赖 Animancer、AnimationClip、TransitionAsset 或 alias。
- 保持 Presenter 不参与状态切换、退出策略和打断判断。
- 保持当前 Run-only，不把 Walk 混进来。

## Non-Goals

- 不做任意动作状态数据。
- 不做通用 interrupt/cancel window。
- 不做 clip length 自动同步。
- 不做 Timeline 编辑器。
- 不把 `RunStart / RunLoop / RunEnd` 建成逻辑状态。

## Proposed Data Shape

第一版使用固定四字段，而不是任意数组：

```text
RunLocomotionAnimationConfigSO
  idle: LocomotionAnimationPhaseConfig
  moveStart: LocomotionAnimationPhaseConfig
  moveLoop: LocomotionAnimationPhaseConfig
  moveStop: LocomotionAnimationPhaseConfig
```

```text
LocomotionAnimationPhaseConfig
  aliasKey: string
  exitPolicy: LocomotionAnimationExitPolicy
  exitDuration: float
```

```text
LocomotionAnimationExitPolicy
  Manual
  AfterDuration
```

默认值：

```text
Idle      alias=Idle      exitPolicy=Manual
MoveStart alias=RunStart  exitPolicy=AfterDuration exitDuration=0.08
MoveLoop  alias=RunLoop   exitPolicy=Manual
MoveStop  alias=RunEnd    exitPolicy=AfterDuration exitDuration=0.08
```

固定四字段的原因：

- 当前逻辑阶段只有四个，不需要让设计者维护 phase 数组。
- Unity 普通 Inspector 直接可配，不依赖新编辑器。
- 不会出现数组缺项、重复 phase、排序错乱导致的运行时歧义。
- 后续如果要做动作 Timeline 或 gait 表，再引入数组/表格编辑器。

## Exit Policy Semantics

`Manual` 表示当前 phase 没有“时间到了就能退出”的事实。它不阻止其它状态图条件，例如 `MoveLoop + NoMoveIntent -> MoveStop`。

`AfterDuration` 表示当前 phase 进入后，`phaseTime >= exitDuration` 时产生“当前 phase exit time reached”的事实。它只是一条条件事实，不代表一定切换，最终仍由状态图 transition 的条件和优先级决定。

示例：

```text
MoveStart
  exitPolicy=AfterDuration
  exitDuration=0.08

MoveStart + HasMoveIntent + PhaseExitTimeReached
  -> MoveLoop
```

```text
MoveStop
  exitPolicy=AfterDuration
  exitDuration=0.45

MoveStop + NoMoveIntent + PhaseExitTimeReached
  -> Idle

MoveStop + HasMoveIntent
  -> MoveStart
```

所以 `RunEnd` 中途有输入仍能立即打断，因为 `HasMoveIntent` transition 的优先级高于等待结束回 Idle。

## Runtime Flow

```text
PlayerLocomotionController
  读取 BasicMovementConfigSO 得到基础 movement settings
  读取 RunLocomotionAnimationConfigSO 得到 phase timing
  把 timing 写入纯 BasicMovementSettings

BasicLocomotionStateMachine
  读取 BasicMovementSettings
  根据 PhaseExitTimeReached / HasMoveIntent / NoMoveIntent 切 phase

BasicLocomotionAnimancerPresenter
  读取 MovementAnimationContext.Phase
  从 RunLocomotionAnimationConfigSO 解析 alias
  调用 Animancer TryPlay(alias)
```

状态机不能读取 `RunLocomotionAnimationConfigSO`。Controller 是 Unity 装配层，可以把配置资产转换成纯数据后交给状态机。

## State Graph Adjustment

当前条件里有：

```text
MoveStartMinTimeReached
MoveStopMinTimeReached
```

本变更建议收敛为：

```text
PhaseExitTimeReached
```

`LocomotionStateGraphContext` 需要知道当前 phase，并从 `BasicMovementSettings` 查询该 phase 的退出策略和退出时长。

这样默认 transition 变成：

```text
MoveStart -> MoveLoop
  HasMoveIntent
  PhaseExitTimeReached

MoveStop -> Idle
  NoMoveIntent
  PhaseExitTimeReached
```

如果为了兼容旧资产短期保留旧条件名，旧条件 MUST 委托到同一套 phase timing 逻辑，不再直接读取独立的 `MoveStartMinTime / MoveStopExitDuration` 特例字段。

## Validation Rules

- `Idle / MoveStart / MoveLoop / MoveStop` 的 alias key 不能为空。
- `MoveStart` 默认 MUST 使用 `AfterDuration`，且 `exitDuration >= 0`。
- `MoveStop` 默认 MUST 使用 `AfterDuration`，且 `exitDuration >= 0`。
- `Idle` 默认 SHOULD 使用 `Manual`。
- `MoveLoop` 默认 SHOULD 使用 `Manual`。
- 配置校验不得读取或修改 Animancer TransitionAsset 的 fade、speed、normalized start time 或 event。

## Alternatives Considered

### Keep `runEndExitDuration`

实现最少，但会让每个新阶段都继续加顶层字段，例如 `runStartExitDuration`、`turnEndExitDuration`。这会快速变成特例堆叠。

### Use Array With Phase Field

更灵活，也更接近未来 editor 表格。但当前没有自定义 editor，普通 Inspector 下容易出现重复 phase、缺 phase 和排序错误。第一版先用固定四字段，等 gait/action/timeline 需求出现再上移。

### Use Animancer OnEnd

短期最直观，但会让状态机等待 Animancer 回调，未来预测回滚、固定 tick、重采样都会更难。当前项目应把“播多久”作为纯数据事实输入状态机。

## Risks / Trade-offs

- Risk: 设计者仍需手填 `exitDuration`，可能和 clip 长度不一致。
  - Mitigation: 当前先通过手动验证确认；后续单独做 editor 同步和一致性检查。
- Risk: 把 `MoveStart` 时间移到动画 phase config 后，旧移动配置里的 `moveStartMinTime` 语义会变弱。
  - Mitigation: 实施时保留 fallback，旧配置缺 phase timing 时仍使用现有 movement config 数值。
- Risk: 新 `PhaseExitTimeReached` 可能影响已有状态图资产。
  - Mitigation: 实施时更新默认状态图资产和测试；如保留旧条件名，旧条件必须委托到新 timing 逻辑。

## Migration Plan

1. 新增 phase config 和 exit policy 纯数据结构。
2. 将 `RunLocomotionAnimationConfigSO` 的四个 alias entry 替换为四个 phase config。
3. 将 `runEndExitDuration` 迁移到 `moveStop.exitDuration`。
4. 将默认起步时间迁移到 `moveStart.exitDuration`。
5. Controller 把 Run phase timing 转成 `BasicMovementSettings` 纯数据。
6. 状态图条件读取 phase timing。
7. Presenter 只读取 alias。
8. 更新默认资产和测试。

## Open Questions

- `MoveStop.exitDuration` 默认值是否继续保持当前 `0.08`，还是直接按当前 RunEnd clip 长度手动填一次？本 proposal 默认保持现有行为，避免未经手测改变手感。
