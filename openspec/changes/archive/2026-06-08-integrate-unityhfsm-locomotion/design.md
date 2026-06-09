## Context
当前 `BasicWASDMovementController` 已经实际承担基础第三人称 Locomotion 主调度入口：直接读取 `InputActionReference`、提交 Look、构造 `BasicLocomotionInputSnapshot`，通过 `BasicLocomotionPipeline` 生成意图、相机相对方向、阶段和运动命令，再提交 `CharacterMotionDriver`、`BasicLocomotionAnimancerPresenter` 和相机 Resolve。

命名上的 `WASD` 已经落后于职责事实：它不只是键盘 WASD，而是基础 Player Locomotion 主链。阶段推进上的 `BasicMovementStateMachine` 仍是手写 switch，可维护性足够当前四阶段，但后续若继续扩展动作层级，会和已安装 UnityHFSM 形成重复路线。

项目已经安装 UnityHFSM `2.3.0`，并已写入 `docs/agents/unityhfsm-usage-guide.md` 作为后续使用指南。

## Goals
- 用 UnityHFSM 接管当前四阶段 Locomotion 状态推进。
- 将 WASD 入口直接迁移为 `PlayerLocomotionController`。
- 保留现有可演示 WASD 行为和数据边界。
- 将具体输入动作读取抽成可替换输入端口。
- 让阶段状态机成为独立、可测试、可替换的 solver/adapter，而不是散在 MonoBehaviour 中。
- 将运动执行抽成可替换端口，为后续 KCC、飞檐走壁、翻越和动画驱动位移预留路径。
- 保持代码命名空间统一，不因为改名产生新的临时 namespace。

## Non-Goals
- 不在本变更中引入完整角色主状态机。
- 不在本变更中接入 KCC 或飞檐走壁动作。
- 不引入配置化 BrainSO、状态 Registry 或 Interceptor SO。
- 不复制 BBB 的运行时类型。
- 不改变现有动画配置来源。
- 不改变相机边界。

## Decisions

### Decision: UnityHFSM 只替换阶段推进，不接管整条 pipeline
第一步只让 UnityHFSM 管理 `Idle / MoveStart / MoveLoop / MoveStop` 阶段。输入、相机相对方向、运动命令、运动执行和动画提交仍由现有 pipeline 顺序负责。

Rationale: 这是最小可验证切口。它能移除手写 switch，又不会同时重构输入、运动、动画和 prefab 绑定。

### Decision: 保留 `BasicMovementPhase`
`BasicMovementPhase` 继续作为运动命令和动画上下文的公开阶段枚举。UnityHFSM 的 state id 第一版可以直接复用该 enum。

Rationale: `MovementCommand`、`MovementAnimationContext` 和 Presenter 已经依赖该枚举。保留它能降低迁移风险，也利于测试对比旧行为。

### Decision: 新运行时入口命名为 `PlayerLocomotionController`
实施时将 `BasicWASDMovementController` 迁移为 `PlayerLocomotionController`。旧 `BasicWASDMovementController` 本来就是临时 demo 入口，不保留兼容包装。

Rationale: 当前入口已经处理相机相对移动、表现上下文和相机 Resolve，WASD 名称会误导后续设计。直接迁移可以避免长期留下两个入口名。

### Decision: 命名空间沿用现有 `ThirdPersonMovement`
`PlayerLocomotionController`、UnityHFSM 阶段适配器、motion executor 端口和相关测试第一版继续使用现有 `ThirdPersonMovement` 命名空间。

Rationale: 当前 movement、camera、animation 已经分别使用 `ThirdPersonMovement`、`ThirdPersonCamera`、`ThirdPersonAnimation`。本变更只收敛入口职责，不扩大为全项目命名空间重构。若后续要统一为更正式的 `Game.Character.*` 或等价命名，需要单独 OpenSpec。

### Decision: 旧 WASD 引用必须迁移，不做兼容包装
实施时必须迁移 scene/prefab 中 `BasicWASDMovementController` 的组件引用到 `PlayerLocomotionController`，并在静态搜索中确认新运行时代码不再引用旧类名。

Rationale: 用户已确认旧入口是临时版本。保留兼容包装会让后续 agent 误以为 WASD 路径仍是可用 API，形成分裂路径。

### Decision: MonoBehaviour 只做 Unity 绑定和生命周期
`PlayerLocomotionController` 可以是场景上的 MonoBehaviour，因为 Unity 序列化字段、相机组件、动画 Presenter、输入 adapter 和执行器适配都需要 Unity 绑定点。但它不能承载具体输入动作读取、阶段规则、运动算法或状态转移规则。

Rationale: 完全消除 MonoBehaviour 不现实，也没有必要。真正要避免的是把输入、意图、状态、运动、动画、相机规则塞进同一个 MonoBehaviour。主逻辑应落在普通 C# 的 pipeline、UnityHFSM adapter、command builder 和 motion executor 中。

### Decision: 输入读取改为端口，Input System 只是当前 adapter
新增或明确一个基础 Locomotion input source 端口，例如 `IBasicLocomotionInputSource` 或等价接口。`PlayerLocomotionController` 每帧只从端口读取 `BasicLocomotionInputSnapshot` 或等价快照，不直接持有 `moveAction`、`lookAction` 这类 `InputActionReference` 字段。

当前 Unity Input System 可通过独立 MonoBehaviour adapter 实现该端口，负责启用/禁用 action、读取 Move/Look、处理空输入兜底。后续接输入服务、网络预测、回放、AI 或 BBB 风格 InputPipeline 时，只替换 input source，不改 Locomotion 状态机和 pipeline。

Rationale: 当前直接在 locomotion controller 上拖每个动作引用是 demo 级写法，会把输入设备、Unity Input System 和 locomotion 主链绑定在一起。BBB 的 `InputPipeline` 值得参考的是“先产生处理后的输入快照，再由后处理管线读快照”，但本变更不复制其完整输入服务。

### Decision: 运动执行改为端口，`CharacterMotionDriver` 降级为当前适配器
新增或明确一个基础 Locomotion motion executor 端口，例如 `IBasicLocomotionMotionExecutor` 或等价接口。pipeline/controller 只提交 `MovementCommand` 到该端口。

当前 `CharacterMotionDriver` 可以继续作为 CharacterController 适配器，但状态机和 pipeline 不得依赖它的 MonoBehaviour 类型。若实施成本允许，应把核心移动计算拆到普通 C# executor，MonoBehaviour 只负责持有 `CharacterController` 和 `Transform`。

Rationale: BBB 的 `MotionDriver` 是普通 C# 类，包住 `CharacterController`、运行时数据和配置，这是值得吸收的方向。未来接 KCC 时，可以新增 KCC executor 适配 `KinematicCharacterMotor`，而不是改写状态机或 pipeline。

### Decision: KCC 和飞檐走壁只预留端口，不进入本次行为
本变更不实现 KCC、不引用 KCC sample、不增加飞檐走壁状态。只要求运动执行端口能承载更复杂 movement mode：普通 CharacterController executor、未来 KCC executor、动画曲线/warp executor 或能力状态输出的特殊 motion request。

Rationale: 飞檐走壁、翻越、贴墙移动会改变地面检测、速度投影、重力、贴附面法线和运动权限。现在接入会扩大范围，应该后续单独建 change。

### Decision: BBB 只作为分层参考，不复制直接互跳状态风格
已观察到 BBB 的结构：

- `BBBCharacterController : MonoBehaviour` 是根组装点，负责 Unity 组件、运行时数据、状态机、输入管线、表现和驱动器的实例化。
- `InputPipeline` 把具体输入源转换成处理后的输入快照，再交给后续 processor。
- `MotionDriver` 是普通 C# 类，负责把输入/曲线/warp 运动转换为 `CharacterController.Move`。
- `PlayerStateRegistry` 根据 `PlayerBrainSO.AvailableStates` 创建状态实例。
- `GlobalInterruptProcessor` 通过 Interceptor SO 做全局高优先级打断。
- 普通状态内部仍大量调用 `player.StateMachine.ChangeState(player.StateRegistry.GetState<T>())`。

本项目只吸收根组装点轻量化、运行时事实集中、运动驱动独立、全局优先级集中处理这些方向。不能复制状态内部直接查 registry 切状态的写法。UnityHFSM transition 应集中读取 context/facts 做流转。

Rationale: 直接互跳会让每个状态知道其它状态类型，动作越多耦合越强。后续飞檐走壁、KCC、闪避、攻击接管应通过 context、transition 和优先级决策扩展，而不是状态彼此硬引用。

## Proposed Shape

```text
PlayerLocomotionController : MonoBehaviour
  - 替代当前 BasicWASDMovementController
  - 负责 InputSource、CameraController、MotionExecutor、Presenter 的 Unity 绑定和 tick 顺序
  - 不直接序列化 move/look InputActionReference
  - 不直接维护阶段 switch
  - 不直接调用 CharacterController.Move 或 KinematicCharacterMotor

IBasicLocomotionInputSource
  - 输出 BasicLocomotionInputSnapshot 或等价输入快照
  - 当前 Unity Input System adapter 在这里读取 InputActionReference
  - 未来输入服务、网络预测、回放和 AI 输入也走同一端口

BasicLocomotionPipeline
  - 输入快照 -> 意图 -> 世界方向 -> 阶段 -> 命令
  - 阶段来源改为 UnityHFSM adapter
  - 不依赖具体 MonoBehaviour motion driver

BasicLocomotionStateMachine
  - 拥有 UnityHFSM StateMachine<BasicMovementPhase>
  - 暴露 Phase、PhaseTime、ActivePath、Reset/Tick
  - 通过 context/facts 读 hasMoveIntent、deltaTime、settings
  - 不依赖 UnityEngine 表现对象

IBasicLocomotionMotionExecutor
  - 当前 CharacterController 实现执行 MovementCommand
  - 未来 KCC 实现执行同一类或扩展后的 motion request
  - 对 pipeline 暴露 CurrentSpeed、LastWorldDirection 等只读诊断信息
```

## Risks / Trade-offs
- 直接删除旧 `BasicWASDMovementController` 类名会导致未迁移 prefab/scene 脚本引用丢失。缓解方式：实施任务必须包含场景/prefab 引用迁移和验证，不用兼容包装掩盖问题。
- UnityHFSM 的 `OnLogic()` 在 transition 后仍会运行新 active state 的 logic。缓解方式：阶段状态的 onLogic 不做副作用，只通过 transition 条件推进阶段，或在适配器中集中更新时间。
- `needsExitTime` 可表达 MoveStart/MoveStop 最小时长，但若误用会产生 pending transition 卡住。缓解方式：本次四阶段可先用条件 transition 表达时间门槛，不启用复杂 exit-time 模式；测试覆盖时间门槛。
- 从直接拖 `InputActionReference` 迁移到 input source 端口会增加一个 adapter。缓解方式：接口只覆盖当前必需的 Move/Look/deltaTime 快照，不提前设计完整输入服务。
- 从 `CharacterMotionDriver` MonoBehaviour 过渡到 motion executor 端口会增加一个接口层。缓解方式：接口只覆盖当前必需的 `ExecuteBasicMovement`、`CurrentSpeed`、`LastWorldDirection`，不提前设计完整 KCC API。
- 活跃 OpenSpec `refactor-wasd-to-locomotion-pipeline` 仍未完成手动验证。缓解方式：本变更必须保持其已定义的 pipeline 顺序和边界，并在实施时复用其静态验证。

## Validation Strategy
- EditMode 测试直接实例化 UnityHFSM Locomotion 状态机，验证四阶段流转、`PhaseTime` 和 active path。
- EditMode 测试使用 fake input source，确认 `PlayerLocomotionController` 不需要直接绑定 `InputActionReference`。
- EditMode 测试验证 `BasicLocomotionPipeline` 在相同输入序列下输出与旧阶段语义一致。
- EditMode 测试使用 fake motion executor，确认 controller/pipeline 可以提交 `MovementCommand` 而不依赖具体 `CharacterMotionDriver` MonoBehaviour。
- 静态搜索：
  - `BasicWASDMovementController` 不出现在新运行时代码中。
  - `PlayerLocomotionController` 不引用 `InputActionReference` 或 `UnityEngine.InputSystem`。
  - 新状态机/adapter 不引用 Animancer、Camera、Cinemachine、CharacterController 或 KCC。
  - `CharacterController.Move` 只出现在当前 CharacterController motion executor/adapter。
  - 新增代码不引用 `BBBNexus` 或 `Ref/BBB`。
- Unity MCP 跑定向 EditMode 测试。
