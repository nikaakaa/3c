## Context
项目目前已经形成一条基础移动主线：

```text
IBasicLocomotionInputSource
  -> BasicLocomotionInputSnapshot
  -> PlayerLocomotionController
  -> BasicLocomotionPipeline / UnityHFSM
  -> IBasicLocomotionMotionExecutor
  -> BasicLocomotionAnimancerPresenter
```

本地预输入层已经存在：

```text
InputButtonState
  -> InputRequestBuffer
  -> future ActionArbiter / HFSM consumer
```

这两条线现在还由 Unity frame 驱动。后续网络同步、客户端预测和服务端权威需要一个共同事实：第 N 个模拟 tick 上读到了哪些输入、玩法消费了哪些请求、运动和状态推进到了哪里、要写出哪些快照或事件。

## Goals
- 定义项目级 simulation tick 语义。
- 让客户端和服务端共享 tick rate、tick id 和 fixed delta 语义。
- 让 Unity 客户端可以从可变帧率转换到固定 tick。
- 让服务端可以不用 Unity `Update` 也能按同一 tick 合约推进。
- 固定 tick phase 顺序，为输入缓冲、玩法判定、运动执行和快照写入提供统一调度。
- 为 GGPO 风格的输入历史、快照历史、回滚和重放预留接口边界。
- 保持现有 Locomotion 主线唯一，不新增第二控制路径。
- 保持 tick core 为纯 C#，避免 Unity 场景对象、Animancer、Cinemachine、CharacterController 依赖泄漏。

## Non-Goals
- 不直接实现 rollback runtime。
- 不直接实现网络协议、发包、收包或重传。
- 不直接实现服务端角色运动和技能判定。
- 不把输入缓冲改造成动作结果记录器。
- 不在 tick core 中引用 Unity 场景对象。

## Proposed Shape

```text
SimulationTick
  stable integer tick id

SimulationTickRate
  ticksPerSecond -> fixedDeltaSeconds

SimulationTickAccumulator
  deltaSeconds -> 0..N ticks with max catch-up

SimulationTickContext
  tick, fixedDeltaSeconds, local/server role, sequence info

SimulationTickRunner
  ordered phases:
    1. ReadInput
    2. UpdateInputBuffer
    3. GameplayDecision
    4. BuildMotion
    5. ExecuteMotion
    6. WriteSnapshotAndEvents
    7. PresentationBridge

ClientUnityTickDriver
  Unity frame delta -> accumulator -> runner ticks

ServerTickDriver
  server timer/manual pump -> runner ticks
```

## Decisions

### Decision: tick id 使用整数值对象
`SimulationTick` 应该是轻量值对象，内部使用整数 id，支持比较、加减偏移、差值计算和序列化友好的原始值读取。

Rationale: 预测、回滚、输入历史和服务端权威都需要稳定可比较的 tick。直接使用浮点时间会让重放、网络包对齐和窗口判断变复杂。

### Decision: fixed delta 由 tick rate 派生
系统应通过 `ticksPerSecond` 计算固定 `fixedDeltaSeconds`。客户端与服务端必须使用同一 tick rate 配置来源或等价配置快照。

Rationale: 服务端和客户端要跑相同逻辑时，tick rate 是协议级事实。即便暂时还没有协议同步，也必须先把含义固定下来。

### Decision: 客户端用 accumulator，服务端用 driver
Unity 客户端从可变 `deltaTime` 累积出 0..N 个固定 tick，并有最大追帧上限。服务端不依赖 Unity `Update`，后续由 Fantasy timer、主循环或测试手动 pump 推进 tick。

Rationale: 客户端画面帧率和模拟 tick 不应该绑定；服务端也不应该继承 Unity 的生命周期模型。

### Decision: phase 顺序是系统合约
tick runner 必须按固定顺序调用 phase。输入事实和输入缓冲必须早于玩法判定，运动执行必须晚于玩法/状态决策，快照与事件写入必须晚于运动和状态推进。

Rationale: 预测回滚真正麻烦的地方是“重放时顺序必须一致”。先把顺序变成可测试合约，后面才有资格做 rollback。

### Decision: 预输入不是网络发包本身
预输入仍是纯本地请求缓冲。它记录“某个 tick 上玩家按下了某个请求”，等待玩法层在合法窗口内消费。网络层后续要同步的是输入事实或 usercmd 类命令，不是“本地已经播放了某动作”的结果。

Rationale: 这与 Valve Source usercmd 模型和 GGPO 输入历史思想一致：网络同步输入事实，权威或确定性模拟决定结果。

### Decision: GGPO 只作为设计参考
服务端和客户端后续可以借鉴 GGPO 的固定 tick、输入延迟、输入历史、状态快照、从分歧 tick 回滚再重放等模型。本变更不直接引入 GGPO SDK，也不把 GGPO API 写进项目公共接口。

Rationale: GGPO 更适合强确定性的对战回滚框架。当前 Unity/Fantasy 3C demo 需要先建立自己的 tick 合约和模块边界，再决定哪些 rollback 技术可复用。

### Decision: Locomotion 只被调度，不被替换
tick 系统后续可以调度现有 `PlayerLocomotionController.Tick` 或等价的 Locomotion tick adapter，但不得新增绕过 `BasicLocomotionPipeline`、motion executor 或 presenter 的角色移动路径。

Rationale: 当前项目已经把输入、移动意图、状态机、运动执行和动画外观拆开。tick 系统是调度层，不是新的角色控制器。

## Phase Draft

| Phase | 职责 | 当前接入状态 |
| --- | --- | --- |
| ReadInput | 读取本地或网络输入事实 | 本变更定义边界 |
| UpdateInputBuffer | 更新 `InputRequestBuffer` | 后续从本地 step 迁移到 `SimulationTick` |
| GameplayDecision | 状态机、ActionArbiter、玩法仲裁 | 预留 |
| BuildMotion | 生成移动/动作运动命令 | 当前 Locomotion pipeline 已存在 |
| ExecuteMotion | 通过 motion executor 执行位移 | 当前端口已存在 |
| WriteSnapshotAndEvents | 写状态快照、事件、诊断 | 预留 |
| PresentationBridge | 动画、相机、表现桥接 | 当前 presenter/camera 已存在 |

## Server Direction
服务端应使用同一 tick rate 和 tick id 语义。第一阶段只建立 tick contract 和可测试 driver，不做完整服务器角色模拟。

后续服务端 rollback/权威方向：
- 每个玩家输入以 tick id 标记。
- 服务端按 tick 顺序消费输入事实。
- 缺失输入时采用明确策略：等待、使用上次输入、或插入空输入；策略必须单独审批。
- 服务端保存可回滚状态快照时，快照必须是纯数据，不含 Unity 场景对象。
- 当迟到输入改变历史结果时，服务端可从最近快照回滚到分歧 tick 并重放。
- 客户端收到权威结果后，后续再做预测状态校正。

## Stop Conditions
- 实施发现必须修改 Fantasy proto 或协议导出工具。
- 实施发现必须新增完整 rollback、快照历史或网络校正。
- 实施发现必须新增第二套角色控制器或直接移动角色。
- 实施发现 tick core 必须引用 Unity 场景对象、Animancer、Cinemachine 或 CharacterController。
- 实施发现必须绕过当前 Locomotion pipeline、UnityHFSM proposal 或输入缓冲边界。

遇到以上情况必须停止，并新建或更新 OpenSpec proposal 等待审批。

## Validation Plan
- `openspec validate add-simulation-tick-system --strict --no-interactive`
- EditMode 测试：
  - tick id 单调递增、比较和偏移。
  - tick rate 正确生成 fixed delta。
  - accumulator 在不足一 tick、一 tick、多 tick、超大 delta 时行为正确。
  - accumulator 追帧上限生效且余量策略明确。
  - runner 严格按 phase 顺序调用。
  - runner 在同一输入序列下重复执行得到一致 phase 记录。
  - tick core 不需要 Unity scene object。
- 静态验证：
  - tick core 不引用 Animancer、Cinemachine、CharacterController、InputActionReference。
  - tick core 不修改 Fantasy proto。
  - 未新增第二套角色控制入口。
- 手动验证：
  - 打开当前 Sandbox 或等价演示场景，Play Mode 下 WASD/Look 行为不回退。
  - Idle、MoveStart、MoveLoop、MoveStop 表现不回退。
