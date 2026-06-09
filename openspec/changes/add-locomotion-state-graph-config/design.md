## Context
当前基础移动主链已经拆成：

- `PlayerLocomotionController`：Unity 生命周期、输入源、运动执行器、动画 presenter 和相机 tick 的组装点。
- `IBasicLocomotionInputSource`：输入端口，Unity Input System 只是当前 adapter。
- `BasicLocomotionPipeline`：输入快照到移动意图、相机相对方向、阶段状态、运动命令的顺序管线。
- `BasicLocomotionStateMachine`：当前 UnityHFSM 四阶段实现。
- `IBasicLocomotionMotionExecutor`：运动执行端口，当前 CharacterController 实现只是 adapter。
- `BasicLocomotionAnimancerPresenter`：动画表现层，消费 `MovementAnimationContext` 并查 Animancer transition key。

这个方向已经避免了旧 WASD demo 的大部分耦合，但状态图仍然写在 `BasicLocomotionStateMachine.Build()` 中，动画 key 仍然写在 presenter 中。现在如果继续加 `RunEndState`、`JumpStartState`、`WallRunState`，会让逻辑状态、动画资源、输入和运动执行重新缠在一起。

BBB 的参考价值是根配置、模块配置、普通 C# `MotionDriver` 和全局优先级思路；它的问题是普通状态内部大量直接 `player.StateMachine.ChangeState(player.StateRegistry.GetState<T>())`。本项目不复制这个互跳风格。

## Goals
- 让当前四阶段 Locomotion 状态图由配置资产描述。
- 让 UnityHFSM 构建集中在 builder 中，而不是散落在 MonoBehaviour 或状态类中。
- 让配置校验能在测试和编辑器前置发现缺状态、缺转移、不可达、重复或条件缺失。
- 让动画表现通过动画集配置映射，不让逻辑层知道 `RunEnd` 这类具体动画 key。
- 保持逻辑层可复用：同一套状态图可以用于不同角色、不同动画集和未来不同 motion executor。
- 保持状态图 tick-ready：状态图只消费调用方传入的 delta/facts，后续可被 simulation tick 调度，但不拥有调度器。
- 保持单驱动权威：后续如果由 tick runner 调度 Locomotion，当前 Unity frame 驱动必须改成同一路径或被关闭，不能双驱动。
- 为后续 KCC、墙跑、翻越和动作接管保留 motion/context 边界，但本变更不实现这些能力。

## Non-Goals
- 不建立完整 `PlayerBrainSO` 或角色级 ability system。
- 不把当前四阶段升级成全角色状态机。
- 不接入 KCC、墙跑、翻越或 root motion 位移权威。
- 不接入 `SimulationTickRunner`、`UnitySimulationTickDriver` 或服务端 tick driver。
- 不把 Move/Look 输入采样放进状态图配置或 builder。
- 不做完整 graph editor。第一版只要求 Inspector 可配置、validator 可测试。
- 不新增全局动画 catalog。当前只处理基础移动动画集。

## Decisions

### Decision: 使用专用 Locomotion 状态图配置，而不是复刻 BBB Brain
新增 `LocomotionStateGraphConfigSO` 或等价资产，只覆盖基础移动四阶段。配置包含初始状态、状态列表和转移列表。

Reason: BBB 的 `PlayerBrainSO.AvailableStates` 只能决定装载哪些状态，不能描述完整转移图。我们当前需要的是“转移规则集中配置并可校验”，不是更大的角色脑。

### Decision: 状态图第一版仍使用 `BasicMovementPhase` 作为状态 ID
配置中的状态 ID 继续使用 `BasicMovementPhase`，不引入新的字符串 ID。

Reason: 当前 `MovementCommand`、`MovementAnimationContext` 和测试都围绕该 enum。继续使用 enum 可以避免拼写错误，也方便未来映射到网络稳定 ID。

### Decision: 转移条件使用小型枚举条件，不使用任意 ScriptableObject 代码插件
第一版条件类型限定为当前需要的事实，例如：

- `HasMoveIntent`
- `NoMoveIntent`
- `MoveStartMinTimeReached`
- `MoveStopMinTimeReached`
- `Always`

每条转移可声明一组条件，全部满足后允许转移。

Reason: 任意 `ConditionSO` 会让配置扩展性很强，但第一版会过早引入多态序列化、生命周期和测试复杂度。当前四阶段只需要少量确定条件。

### Decision: 转移优先级显式配置
转移配置包含 `priority` 或等价排序字段。同一来源状态下，builder 按优先级注册或解析转移。

Reason: 随着后续加入跳跃、受击、动作接管，优先级不能藏在数组顺序或代码注册顺序里。即使第一版只有四阶段，也先把优先级作为配置事实。

### Decision: builder 负责从配置构建 UnityHFSM
`BasicLocomotionStateMachine` 不再直接硬编码 `AddState` 和 `AddTransition`。它应接收配置或由 builder 创建内部 UnityHFSM。

Reason: UnityHFSM 是执行内核，项目自己的 builder 才是业务装配边界。后续可在 builder 中统一处理 validator、默认图、诊断路径和测试。

### Decision: 状态图 tick-ready，但不拥有 tick 调度
`LocomotionStateGraphContext` 可以接收调用方传入的 `deltaTime` / fixed delta 和运行时事实。第一版继续保留当前 `Tick(..., float deltaTime, ...)` 形态，后续若接入 simulation tick，可由 adapter 把 `SimulationTickContext.fixedDeltaSeconds` 转为同一入口。

Reason: Locomotion 状态图关心“这一帧/这一 tick 的事实是否满足转移条件”，不关心时间由 Unity `Update`、测试手动推进还是 simulation tick runner 产生。这样状态图可被固定 tick 调度，也不会依赖 `UnitySimulationTickDriver` 或服务端 driver。

### Decision: 输入采样不属于状态图配置
状态图条件只消费 `hasMoveIntent`、阶段时间、settings 等已整理事实，不直接读取 Move/Look action、Input System、输入缓冲或设备状态。

Reason: 输入采样的时机以后会被 tick phase 和输入缓冲影响。如果状态图自己读取输入，就会破坏“输入事实 -> pipeline -> 状态图”的顺序，也会让回放和服务端模拟难以复用。

### Decision: 后续 tick 接入必须保证单驱动权威
如果后续把 Locomotion 放入 `SimulationTickRunner`，只能通过现有 `PlayerLocomotionController` 或等价 adapter 调用同一条 `BasicLocomotionPipeline` 主链；同一角色不能同时由 Unity frame `Update` 和 tick runner 各自推进移动、动画或相机 resolve。

Reason: 双驱动会导致同一帧内移动命令、动画状态和相机状态被推进两次。tick 是调度层，不是第二套 locomotion 实现。

### Decision: 提供默认图兜底，但运行时必须可看见使用了默认图
如果 `PlayerLocomotionController` 没绑定状态图配置，第一版可以使用代码内默认四阶段图，避免场景立即失效；但必须有明确 log 或测试可检查的诊断字段。

Reason: 场景资产迁移会有过渡期。兜底可以降低接入风险，但不能隐藏配置缺失，避免长期回到硬编码路径。

### Decision: 动画集映射独立于状态图
新增 `LocomotionAnimationSetSO` 或等价资产，将抽象动画意图映射到 Animancer transition key。意图至少包含：

- 逻辑阶段：`Idle`、`MoveStart`、`MoveLoop`、`MoveStop`
- 步态：`Walk`、`Run`
- 必要时的脚相或方向扩展预留

Reason: `RunEnd` 是 `MoveStop + LastMovingGait=Run` 的动画变体，不是逻辑状态。动画层可以配置不同角色的 `RunEnd` key，但逻辑层只输出可复用事实。

### Decision: 动画层可以反馈抽象事件，不能反馈具体 clip/key 名
未来如果逻辑需要等待动画结束或开放取消窗口，动画层只能写入抽象事件，例如 `MotionComplete`、`StopRecoverable`、`ActionCancelable`。本变更第一版不实现复杂事件，只在设计上保留边界。

Reason: 工业项目常见做法不是让动画完全沉默，而是让动画通过 notify/event 暴露抽象窗口。关键是逻辑不能依赖 `Corin_RunEnd` 或 `RunEnd` 这种资源名。

### Decision: 编辑器第一版做校验，不做可视化 graph editor
第一版可通过 Inspector 配置数组，并通过 validator 在 EditMode 测试或后续菜单项中校验：

- 初始状态存在
- 每条转移的 from/to 状态存在
- 同来源同目标重复转移可报错或警告
- 从初始状态能到达所有启用状态
- 四阶段默认图必要转移齐全
- 动画集缺少当前四阶段必要映射时报错

Reason: 可视化编辑器有价值，但不是当前最小工业化步骤。先把配置模型和校验打牢。

## Runtime Shape
```text
PlayerLocomotionController : MonoBehaviour
  - 绑定 BasicMovementConfigSO
  - 绑定 LocomotionStateGraphConfigSO
  - 绑定 LocomotionAnimationSetSO 或由 presenter 绑定
  - tick 顺序不变

BasicLocomotionPipeline
  - 输入快照 -> 意图 -> 相机相对方向 -> 状态机 tick -> MovementCommand
  - 不知道具体动画 key
  - 不知道 CharacterController 或 KCC

BasicLocomotionStateMachine
  - 持有 UnityHFSM StateMachine<BasicMovementPhase>
  - 读取 builder 生成的状态和转移
  - 暴露 Phase / PhaseTime / ActivePath / Reset / Tick

LocomotionStateGraphBuilder
  - 读取 LocomotionStateGraphConfigSO
  - 校验后注册 state 和 transition
  - 根据 context/facts 判断条件

LocomotionStateGraphContext
  - hasMoveIntent
  - phaseTime
  - deltaTime/fixedDeltaSeconds 由调用方传入
  - BasicMovementSettings
  - 后续可扩展 grounded/actionRequest 等事实
  - 不持有 SimulationTickRunner / UnitySimulationTickDriver / InputAction

BasicLocomotionAnimancerPresenter
  - 消费 MovementAnimationContext
  - 根据 LocomotionAnimationSetSO 解析 Animancer key
  - 不修改逻辑状态
  - 不调用运动 API
```

## BBB 对比
- BBB：`PlayerSO` 聚合模块，`PlayerBrainSO` 配状态名单和全局打断器。
- BBB：`PlayerStateRegistry` 用 enum switch 创建状态实例。
- BBB：很多状态内部直接 `ChangeState(GetState<T>())`。
- 本变更：吸收“配置根”和“状态装配名单”的思路，但转移规则集中在 graph config/builder 中。
- 本变更：状态不互相查 registry，不让动画 key 变成逻辑状态。

## Risks / Trade-offs
- Risk: 配置资产增加后，当前简单四阶段看起来更重。
  - Mitigation: 第一版只支持必要字段和少量条件，默认图可自动生成。
- Risk: 使用默认图兜底会让配置缺失被长期忽略。
  - Mitigation: 兜底必须有诊断输出，测试覆盖缺失配置路径，场景绑定任务必须补齐资产。
- Risk: 动画集配置和现有 Animancer Transition Library 边界重复。
  - Mitigation: 动画集只负责从抽象意图到 key 的映射，实际 transition 仍由 Animancer library 管。
- Risk: 后续条件增长时，小型枚举条件不够用。
  - Mitigation: 等出现跳跃、墙跑、动作接管后，再单独 proposal 扩展为 ConditionSO 或 typed condition registry。
- Risk: OpenSpec 中已有 animation config change 未完全完成。
  - Mitigation: 本变更按当前代码事实设计，不恢复旧 `BasicLocomotionAnimationConfigSO` 路线。

## Stop Conditions
实施时如果出现以下需求，必须停止并重新提案：

- 需要新增完整角色脑、全局 ability system 或 BBB 运行时依赖。
- 需要让状态类直接互相 `ChangeState`。
- 需要让动画 key 或 clip 名参与逻辑状态判断。
- 需要状态图 builder/state machine 直接引用 `SimulationTickRunner`、`UnitySimulationTickDriver` 或 tick accumulator。
- 需要状态图配置直接采样 Move/Look 输入。
- 需要接入 KCC、墙跑、跳跃、闪避、攻击或 root motion 位移权威。
- 需要新增绕过 `PlayerLocomotionController` 的第二移动入口。

## Validation Strategy
- EditMode 测试默认图构建后初始状态为 `Idle`。
- EditMode 测试配置图保持当前六条基础转移语义。
- EditMode 测试缺失初始状态、缺失目标状态、重复转移、不可达状态会被 validator 发现。
- EditMode 测试 `PlayerLocomotionController` 可绑定配置图并由 fake input/fake motion executor 驱动。
- EditMode 测试状态机使用固定 delta 兼容的输入事实推进时四阶段语义不变。
- EditMode 测试动画集能将 `MoveStop + Run` 映射到 `RunEnd` key，且逻辑状态仍为 `MoveStop`。
- 静态搜索确认状态机/builder 不引用 Animancer、CharacterController、KCC、Camera、Cinemachine 或 InputAction。
- 静态搜索确认状态机/builder 不引用 `SimulationTickRunner`、`UnitySimulationTickDriver` 或 tick accumulator。
- 静态搜索确认 presenter 不调用 `ChangeState`、不调用 `CharacterController.Move`。
- Unity MCP 跑定向 EditMode 测试。
