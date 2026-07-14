# Design: Corin 状态/Timeline 编排

## RootTree

目标形状：

```text
CorinPlayableRootTree
  Runtime Loop
    Gameplay Parallel
      Locomotion StateMachine
      Action StateMachine
```

RootTree 不显示具体 `Attack1` 的 HitWindow、Cue 或 Result 提交节点。
RootTree 也不显示具体 `Attack1` 的 Activate、Play Timeline 或 Lifecycle 节点。当前平铺的 `Activate Attack`、`Play Attack Timeline`、`Submit Attack Window`、`Submit Attack Cue`、`Submit Loopback Result` 必须从主图删除，并迁移到正确层级：

- action 激活和生命周期：Action StateMachine 的 `Attack1` / `Attack2` inline state body。
- 动画、window、cue、motion warp 时间内容：Timeline 轨道。
- GameplayResult：后续正式 gameplay solver、loopback debug solver 或服务端裁决。

这里删除的是 Corin RootTree 的污染路径，不是删除所有底层节点类型。底层节点若仍存在，只能作为状态 body 或 runtime adapter 的原语。

## Locomotion StateMachine

状态列表：

```text
Idle
WalkStart
WalkLoop
WalkEnd
RunStart
RunLoop
RunEnd
MovingTurn
```

第一阶段 transition 口径：

```text
Idle -> WalkStart: move > walkThreshold && move < runThreshold
Idle -> RunStart: move >= runThreshold
WalkStart -> WalkLoop: StateRootCompleted && move > walkThreshold && move < runThreshold
WalkLoop -> WalkEnd: move <= stopThreshold
WalkEnd -> Idle: StateRootCompleted
WalkLoop -> RunStart: move >= runThreshold
RunStart -> RunLoop: StateRootCompleted && move >= runThreshold
RunLoop -> RunEnd: move <= stopThreshold
RunEnd -> Idle: StateRootCompleted
RunLoop -> MovingTurn: move angle delta >= turnThreshold
MovingTurn -> RunLoop: StateRootCompleted && move >= runThreshold
```

如果 `move angle delta` 读取节点尚未存在，`MovingTurn` 可以先只建状态和 Timeline body，但进入 transition 必须等正式条件节点补齐，不能用 fallback 字符串或临时 Bool。

## Action StateMachine

状态列表：

```text
None
Attack1
Attack2
```

第一阶段 transition 口径：

```text
None -> Attack1: HasInputRequest("Attack")
Attack1 -> Attack2: combo 条件 && HasInputRequest("Attack")
Attack1 -> None: StateRootCompleted
Attack2 -> None: StateRootCompleted
```

`Attack1` 和 `Attack2` 状态 body：

```text
OnEnter:
  Activate ActionInstance(ActionProfile)
Root:
  TimelineNode(Action Context)
OnExit:
  Submit ActionLifecycleTransition
```

## Timeline 资产

Timeline 资产允许存在，因为 Timeline 是可预览、可复用时间内容。

Locomotion Timeline：

- 可按状态创建 `CorinIdleTimeline`、`CorinWalkStartTimeline` 等。
- 如果没有明确动画 clip，实施阶段必须停下来说明缺口，不能创建 fallback 配置。

Action Timeline：

- `Attack1` 和 `Attack2` 可先复用同一攻击动画 clip，前提是明确这是正式选定资源。
- 每个攻击 Timeline 必须包含 ActionWindowTrack 和 ActionCueTrack。

## Inline-first

State body 使用 StateNode 内联 `StateBehaviorSubTree`。不创建：

```text
CorinAttack1SubTree.asset
CorinRunStartSubTree.asset
```

只有明确复用时才 Extract Shared。

## 失败条件

实施阶段遇到以下情况必须停：

- 缺少目标状态可用动画资源，且用户没有选择正式替代资源。
- 当前 BTSMTL 资产序列化无法安全创建/迁移 inline StateMachineGraph。
- 缺少必要 transition 条件节点，导致只能用临时 Bool/fallback 表达。
- 为了闭环必须把具体动作 body 继续平铺在 Corin RootTree。
