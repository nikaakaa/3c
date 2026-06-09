## Context
当前基础移动运行路径是：

```text
Unity Update
  -> PlayerLocomotionController.Update
  -> inputSource.ReadInput(Time.deltaTime)
  -> PlayerLocomotionController.Tick(input)
  -> BasicLocomotionPipeline
  -> IBasicLocomotionMotionExecutor
  -> BasicLocomotionAnimancerPresenter
  -> cameraController.Resolve
```

已完成的 tick 地基是：

```text
UnitySimulationTickDriver
  -> SimulationTickAccumulator
  -> SimulationTickRunner
  -> fixed phase order
```

两者现在还没有接起来。接入时最危险的问题不是“怎么调用 Tick”，而是双驱动：如果 `PlayerLocomotionController.Update()` 还在每帧跑，同时 `UnitySimulationTickDriver` 也按 fixed tick 跑 Locomotion，就会出现双输入、双位移、双动画表现。

## Goals
- 让基础 Locomotion 可以由 simulation tick runner 驱动。
- 保持 `PlayerLocomotionController` 作为唯一基础移动入口。
- 保持 `BasicLocomotionPipeline` 和 motion executor 为现有移动主线。
- 防止 Unity frame Update 与 simulation tick 同时驱动同一 Locomotion。
- 让 tick driver 保持通用，不直接依赖 Locomotion 具体类。
- 在场景中建立一个明确的 tick driver 组装点。
- 用测试和手动验证证明 WASD/Look 行为不回退。

## Non-Goals
- 不迁移完整相机系统到 tick core。
- 不把 Animancer 播放内部逻辑塞进 tick core。
- 不做离散动作输入消费。
- 不做网络 usercmd、server tick、prediction 或 rollback。
- 不实现状态图配置化。

## Proposed Shape

```text
Scene
  UnitySimulationTickDriver (runAutomatically = true)
    Runner
      ExecuteMotion phase:
        LocomotionTickAdapter
          -> PlayerLocomotionController.Tick(fixed-delta snapshot)

PlayerLocomotionController
  autoUpdate = false when driven by adapter
  Tick(input) unchanged as official Locomotion entry
```

## Decisions

### Decision: 使用 adapter 注册 phase，而不是让 driver 直接认识 Locomotion
新增 `LocomotionTickAdapter` 或等价组件，持有 `UnitySimulationTickDriver` 和 `PlayerLocomotionController` 引用，在启用时向 runner 的 `ExecuteMotion` phase 注册 handler。

Rationale: `UnitySimulationTickDriver` 是项目级调度器，不能直接依赖具体移动实现。adapter 是 Unity 组装层，负责把通用 tick context 转换为当前 Locomotion 调用。

### Decision: `PlayerLocomotionController.Tick` 继续是唯一移动入口
adapter 不重新实现输入解析、移动意图、状态切换、运动命令或动画表现，只调用现有 `PlayerLocomotionController.Tick`。

Rationale: 当前主线已经拆好了端口。tick 接入要改变“谁调度”，不是改变“移动怎么算”。

### Decision: 自动 Update 必须显式关闭
`PlayerLocomotionController` 需要新增 `AutoUpdate` 或等价开关。tick adapter 接管时必须关闭该开关；未接管时默认保持当前 frame Update 行为，降低迁移风险。

Rationale: 防双驱动是本变更第一优先级。默认保持旧行为可以避免所有场景立刻需要迁移。

### Decision: fixed delta 进入输入快照
adapter 每个 simulation tick 读取当前 Move/Look，并使用 `SimulationTickContext.FixedDeltaSecondsFloat` 构造 `BasicLocomotionInputSnapshot` 或传给现有 input source。

Rationale: Locomotion 状态机和运动命令应使用 simulation fixed delta，而不是 Unity frame delta。

### Decision: 表现仍在现有 Controller 内被调用
第一版继续让 `PlayerLocomotionController.Tick` 内部调用 presenter 和 camera resolve。先不把动画和相机拆到独立 `PresentationBridge` handler。

Rationale: 这是最小接入。表现细分到独立 phase 可以后续再做，避免本变更同时重排动画和相机生命周期。

### Decision: 与状态图配置变更保持并行边界
`add-locomotion-state-graph-config` 会改状态机构建和动画映射。本变更只改 tick 调度入口，不改状态图配置。

Rationale: 两个变更都可能触碰 `PlayerLocomotionController`，但职责不同。实施时如遇冲突，按最新实际代码合并，不新增绕路。

## Runtime Flow

```text
Unity frame Update
  -> UnitySimulationTickDriver.Update
  -> accumulator emits 0..N SimulationTickContext
  -> runner.Run(context)
       ReadInput: empty for now
       UpdateInputBuffer: empty for now
       GameplayDecision: empty for now
       BuildMotion: empty for now
       ExecuteMotion: LocomotionTickAdapter.Tick
           inputSource.ReadInput(context.FixedDeltaSecondsFloat)
           PlayerLocomotionController.Tick(snapshot)
       WriteSnapshotAndEvents: empty for now
       PresentationBridge: empty for now
```

## Stop Conditions
- 需要修改 Fantasy proto 或服务端代码。
- 需要实现 rollback、snapshot history 或 prediction correction。
- 需要新增第二套 player movement controller。
- 需要绕过 `PlayerLocomotionController.Tick` 直接调用 `BasicLocomotionPipeline` 或 motion executor。
- 需要让 `UnitySimulationTickDriver` 直接引用 Locomotion 具体类型。
- 需要把状态图配置化混进本变更。
- 发现 `PlayerLocomotionController.Update` 与 tick adapter 无法避免双驱动。

遇到以上情况必须停止并回到 OpenSpec。

## Validation Plan
- EditMode 测试：
  - `PlayerLocomotionController` 默认仍可由 frame Update 驱动。
  - 关闭 `AutoUpdate` 后 frame Update 不读取输入、不执行 motion。
  - `LocomotionTickAdapter` 在 `ExecuteMotion` phase 调用 controller 一次。
  - adapter 使用 fixed delta 而不是 frame delta。
  - adapter 启用时不会造成 controller frame Update 双驱动。
  - driver 每帧累积多个 tick 时 Locomotion 调用次数等于 emitted tick 数。
- 静态验证：
  - tick core 不引用 Locomotion、Animancer、Cinemachine、CharacterController。
  - 没有新增第二套 player controller。
  - 未修改 Fantasy proto。
- Play Mode 手动验证：
  - `Sandbox` 进入 Play Mode 后 WASD 移动方向、速度和停止行为不回退。
  - Look 输入和相机跟随不回退。
  - Idle、MoveStart、MoveLoop、MoveStop 表现不回退。
  - Console 无双驱动 error 或 missing reference error。
