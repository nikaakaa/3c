# character-runtime-ports Specification

## Purpose
TBD - created by archiving change refactor-character-runtime-ports. Update Purpose after archive.
## Requirements
### Requirement: Character Frame Runtime Port
系统 MUST 为角色帧管线提供明确的 runtime port Interface，使 `CharacterFramePipeline` 只依赖角色帧所需能力，而不是直接依赖具体 MonoBehaviour controller。该 Interface MUST 位于 `Assets/Scripts/Character/Pipeline/Contracts/...` 或等价角色级目录。该 Interface MUST 覆盖输入缓冲写入、FullBody submission 构建、输出应用、表现桥接、runtime facts 写入和诊断提交所需的最小能力。该 Interface MUST NOT 暴露 Unity scene object、Animancer runtime object、InputAction 或 `CharacterController`。

#### Scenario: Pipeline 通过端口推进 phase
- **WHEN** `CharacterFramePipeline` 推进一帧
- **THEN** 它 MUST 通过角色帧 runtime port 调用各 phase 所需能力
- **AND** MUST NOT 在方法签名或字段中直接依赖 `PlayerFullBodyActionController`
- **AND** MUST 仍按 `ReadInput -> UpdateInputBuffer -> GameplayDecision -> BuildMotion -> ExecuteMotion -> PresentationBridge -> WriteSnapshotAndEvents` 顺序运行

#### Scenario: 端口契约不泄露 Unity 对象
- **WHEN** 检查角色帧 runtime port 契约
- **THEN** 契约 MUST NOT 引用 `MonoBehaviour`
- **AND** MUST NOT 引用 `Transform`
- **AND** MUST NOT 引用 `CharacterController`
- **AND** MUST NOT 引用 Animancer runtime 类型
- **AND** MUST NOT 引用 InputAction

### Requirement: FullBody runtime 由 CharacterFrame 组合
系统 MUST 通过 `CharacterFrameRuntimeController` 或等价角色级 MonoBehaviour 组合 Action runtime、Locomotion runtime、输入缓冲、输出运行时和诊断端口。正式生产路径 MUST NOT 依赖 `PlayerFullBodyActionController`、`FullBodyRuntimePortAdapter` 或等价旧 Host Adapter 作为角色帧 runtime port。

#### Scenario: CharacterFrameRuntimeController 组合正式端口
- **WHEN** 角色正式 runtime 初始化
- **THEN** `CharacterFrameRuntimeController` MUST 组合 Locomotion、Action、input buffer、output runtime 和 diagnostics 所需依赖
- **AND** MUST 通过 `ICharacterFrameRuntimePort` 或等价角色级端口推进 `CharacterFramePipeline`
- **AND** MUST NOT 通过旧 FullBody Host Adapter 暴露端口

#### Scenario: Action runtime 不包装旧 controller
- **WHEN** `CharacterFramePipeline` 或 submitter graph 需要 Action runtime 能力
- **THEN** 它们 MUST 依赖角色级 runtime port、Action submitter 或等价窄接口
- **AND** 生产实现 MUST NOT 通过 `FullBodyRuntimePortAdapter` 转接旧 controller
- **AND** pipeline 和 submitter MUST NOT 直接接收 `PlayerFullBodyActionController`

#### Scenario: Runner owner 位于正式角色 runtime
- **WHEN** Action runtime 初始化或恢复角色
- **THEN** 唯一正式 `CharacterStateMachineRunner` owner MUST 位于 `CharacterFrameRuntimeController` 组合的状态机 runtime
- **AND** Locomotion adapter、Action request provider、motion executor 和 animation presenter MUST NOT 创建第二个正式 runner

### Requirement: FullBody Submission Runtime Port
系统 MUST 为 FullBody submission builder 提供窄 runtime port，使 builder 只读取构建 state submission 和 frame submission 所需事实。该 port MUST 提供 runner、current snapshot、input buffer、Dodge config、interrupt policy、action resistance 和 Locomotion runtime port 的访问能力，但 MUST NOT 允许 builder 执行 motion、播放 animation、写 Unity scene object 或提交 snapshot/events。

#### Scenario: Submission builder 不依赖 controller 大类
- **WHEN** `FullBodySubmissionBuilder` 构建 state submission 或 frame submission
- **THEN** 它 MUST 接收 FullBody submission runtime port 或等价窄 Interface
- **AND** MUST NOT 在方法签名或字段中直接依赖 `PlayerFullBodyActionController`
- **AND** MUST NOT 直接依赖 `PlayerLocomotionController`

#### Scenario: Submission builder 只提交纯数据
- **WHEN** `FullBodySubmissionBuilder` 完成 frame submission
- **THEN** 它 MUST 返回 `CharacterFrameSubmission` 或等价纯数据提交
- **AND** MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 写 runtime blackboard
- **AND** MUST NOT 更新 snapshot/events

### Requirement: Locomotion Runtime Port
系统 MUST 为 Locomotion 子职责提供窄 runtime port，使 FullBody submission 和 Character output apply 可以通过 Interface 访问 Locomotion facts、frame 构建、motion 执行、动画提交和 runtime facts 写入。第一阶段 MUST 拆分 prepare/build 端口和 output/apply 端口，而不是用一个巨大端口复制完整 `PlayerLocomotionController` Interface。`PlayerLocomotionController` MAY 作为这些 port 的生产 adapter，但 MUST NOT 作为 FullBody owner 选择、状态切换或 frame phase 的第二权威。

#### Scenario: FullBody 通过 Locomotion port 取 facts 和 frame
- **WHEN** FullBody submission 需要 Locomotion decision facts 或 motion frame
- **THEN** 它 MUST 通过 Locomotion frame runtime port 调用 prepare/evaluate/build 能力
- **AND** Locomotion runtime port MUST NOT 创建或推进第二个 `CharacterStateMachineRunner`
- **AND** Locomotion runtime port MUST NOT 注册 tick driver

#### Scenario: Locomotion 输出仍由 Character 管线应用
- **WHEN** 当前 frame output 需要执行基础移动或播放基础移动动画
- **THEN** Character output apply 阶段 MUST 通过 Locomotion output runtime port 或等价端口执行
- **AND** Locomotion adapter MUST NOT 在 Character 管线外额外提交 motion executor
- **AND** Locomotion adapter MUST NOT 在 Character 管线外额外提交 base layer animation

#### Scenario: 不创建巨型 Locomotion 端口
- **WHEN** 检查 Locomotion runtime port 契约
- **THEN** prepare/evaluate/build 能力 MUST 与 output/apply 能力分属不同 Interface 或等价窄契约
- **AND** 系统 MUST NOT 新增一个复制完整 `PlayerLocomotionController` 调用面的单一巨大端口

#### Scenario: Locomotion 大类收口优先级
- **WHEN** 实施运行时端口化
- **THEN** `PlayerLocomotionController` 的 direct tick、frame builder facade、snapshot、diagnostic、reference resolve、camera/facing resolve 职责 MUST 被识别为不同变化原因
- **AND** FullBody 主线 MUST 通过 Locomotion runtime port 访问必要职责
- **AND** 实施 MUST NOT 把 camera/facing resolve、rollback snapshot 或 Unity reference resolve 迁入 `LocomotionFrameBuilder`

### Requirement: Model Aggregation Deferred
系统 MUST 将 `CharacterFramePipelineTypes` 和 `CharacterStateMachineTypes` 的模型聚合风险记录为后续独立 change 的收敛方向，但本变更 MUST NOT 在端口化实施中顺手重排状态机配置语义或角色帧总线数据模型。端口化所需新增类型 MUST 保持纯数据，并且不得成为新的副作用总线。

#### Scenario: Frame pipeline types 不变成副作用总线
- **WHEN** 本变更新增或调整角色帧端口数据
- **THEN** 新增数据类型 MUST NOT 引用 motion executor
- **AND** MUST NOT 引用 animation presenter
- **AND** MUST NOT 引用 Unity scene object
- **AND** MUST NOT 承担输出副作用执行职责

#### Scenario: State machine model 不顺手重排
- **WHEN** 本变更迁移 runtime port
- **THEN** 实施 MUST NOT 重定义 `CharacterStateMachineTypes` 中 transition condition、state module、timeline 或 action movement 的配置语义
- **AND** MUST NOT 将通用状态图 model 和角色业务 model 的拆分作为本变更验收条件

### Requirement: Runtime Port Testability
系统 MUST 为角色运行时端口提供生产 adapter 和 EditMode 测试 fake，使端口 Interface 成为可测试面。测试 fake MUST 能验证 phase 顺序、request submission、frame submission、output apply 和 diagnostics commit，而不需要创建完整 Unity 场景对象。

#### Scenario: Fake port 验证 phase 顺序
- **WHEN** EditMode 测试用 fake runtime port 推进 `CharacterFramePipeline`
- **THEN** 测试 MUST 能观察每个 phase 的调用顺序
- **AND** MUST 能确认 GameplayDecision 晚于输入缓冲更新
- **AND** MUST 能确认 ExecuteMotion 早于 PresentationBridge
- **AND** MUST 能确认 WriteSnapshotAndEvents 最后发生

#### Scenario: Static boundary 验证端口隔离
- **WHEN** 运行静态边界测试
- **THEN** 测试 MUST 确认 `CharacterFramePipeline` 不直接引用 `PlayerFullBodyActionController`
- **AND** MUST 确认 `FullBodySubmissionBuilder` 不直接引用 `PlayerFullBodyActionController`
- **AND** MUST 确认 `FullBodySubmissionBuilder` 不直接引用 `PlayerLocomotionController`
- **AND** MUST 确认端口契约不引用 Unity scene object 或 Animancer runtime object

### Requirement: Runtime Port Migration Boundaries
系统 MUST 在运行时端口化过程中保持当前唯一 Character frame pipeline、正式状态图 runtime、统一 motion executor 和 Animancer presenter 权威。实施 MUST NOT 新增 fallback 配置、第二角色控制器、第二 frame pipeline、第二 Locomotion 状态机、第二 Action runtime 或未审批 rollback/playback 语义。

#### Scenario: 不产生分裂路径
- **WHEN** 完成运行时端口化迁移
- **THEN** FullBody gameplay MUST 仍通过 `CharacterFramePipeline` 推进
- **AND** 状态权威 MUST 仍来自同一个 `CharacterStateMachineRunner`
- **AND** motion executor 调用 MUST 仍只发生在 Character output apply 阶段
- **AND** animation presenter 调用 MUST 仍只发生在 Character output apply 阶段

#### Scenario: 不抢其它 active change 的权威
- **WHEN** 端口化涉及 Locomotion playback、snapshot restore 或 rollback replay 相关代码
- **THEN** 本变更 MUST 保持现有语义不变
- **AND** MUST NOT 重定义 playback restore/window 权威
- **AND** MUST NOT 新增 snapshot 历史或网络 rollback driver

### Requirement: Character Frame Pipeline Host Ownership
系统 MUST 提供纯 C# `CharacterFramePipelineHost` 作为每个角色的唯一角色帧运行时持有者。该 host MUST 位于角色级 Pipeline runtime 目录，MUST 持有正式 `CharacterFramePipeline` 和角色帧提交者 Adapter，MUST NOT 作为 MonoBehaviour、第二 pipeline、第二 runner、第二 motion executor 或第二 animation presenter 存在。

#### Scenario: MonoBehaviour 不直接创建 Pipeline
- **WHEN** 检查生产 MonoBehaviour runtime adapter
- **THEN** `CharacterFrameRuntimeController` 或等价 Unity adapter MUST NOT 直接 `new CharacterFramePipeline`
- **AND** 它 MAY 懒创建或持有一个纯 C# `CharacterFramePipelineHost`
- **AND** 它 MUST NOT 为 phase tick 和一帧 tick 分别创建不同 host

#### Scenario: Tick Adapter 复用同一个 Host
- **WHEN** `FullBodyActionTickAdapter` 或等价 simulation tick adapter 推进 phase
- **THEN** 它 MUST 使用同一角色的 `CharacterFramePipelineHost`
- **AND** MUST NOT 直接 `new CharacterFramePipeline`
- **AND** MUST NOT 创建独立于角色 Unity adapter 的第二个 host

#### Scenario: Pipeline 不创建 FullBody 实现
- **WHEN** `CharacterFramePipeline` 推进 `GameplayDecision` 或 `BuildMotion`
- **THEN** 它 MUST 通过角色帧提交者 Interface 请求纯数据提交
- **AND** MUST NOT 直接创建 `FullBodySubmissionBuilder`
- **AND** MUST NOT 直接依赖 FullBody 生产 submitter 具体类

#### Scenario: Host 不是新权威
- **WHEN** `CharacterFramePipelineHost` 推进角色帧
- **THEN** 状态权威 MUST 仍来自同一个 `CharacterStateMachineRunner`
- **AND** motion executor 调用 MUST 仍只发生在 Character output apply 阶段
- **AND** animation presenter 调用 MUST 仍只发生在 Character output apply 阶段

### Requirement: Character Frame Submitter Interfaces
系统 MUST 为角色帧 request submission 和 frame output submission 提供拆分的 Interface。request submitter MUST 只服务 `GameplayDecision` phase，frame output submitter MUST 只服务 `BuildMotion` phase。两类 submitter MUST 只提交纯数据，MUST NOT 直接执行 motion、播放 animation、写 Unity scene object、提交 snapshot/events 或重新拥有 pipeline phase。

#### Scenario: Request Submitter 只提交玩法判定数据
- **WHEN** `GameplayDecision` phase 运行
- **THEN** request submitter MUST 能提交 locomotion decision、action request submission、state decision、当前 timeline facts 或等价纯数据
- **AND** MUST NOT 生成或应用 frame output side effects
- **AND** MUST NOT 调用 motion executor 或 animation presenter

#### Scenario: Output Submitter 只提交帧输出数据
- **WHEN** `BuildMotion` phase 运行
- **THEN** frame output submitter MUST 返回 `CharacterFrameSubmission` 或等价纯数据提交
- **AND** MUST NOT 直接执行 motion
- **AND** MUST NOT 直接播放 animation
- **AND** MUST NOT 写 runtime blackboard、snapshot 或 diagnostics

#### Scenario: Submitter Interface 不泄露 Unity 对象
- **WHEN** 检查角色帧提交者 Interface
- **THEN** Interface MUST NOT 引用 `MonoBehaviour`
- **AND** MUST NOT 引用 `Transform`
- **AND** MUST NOT 引用 `CharacterController`
- **AND** MUST NOT 引用 Animancer runtime 类型
- **AND** MUST NOT 引用 InputAction

### Requirement: Host Ownership Testability
系统 MUST 提供自动测试验证角色帧持有关系，使 host、pipeline、submitter、Unity adapter 和 replay 入口的职责可以通过静态边界测试和 EditMode 行为测试确认。

#### Scenario: 静态边界验证持有关系
- **WHEN** 运行角色帧管线静态边界测试
- **THEN** 测试 MUST 确认只有 `CharacterFramePipelineHost` 或测试显式构造路径可以创建 `CharacterFramePipeline`
- **AND** MUST 确认生产 MonoBehaviour 不直接创建 `CharacterFramePipeline`
- **AND** MUST 确认 `CharacterFramePipeline` 不直接创建 FullBody 生产提交者

#### Scenario: 行为测试验证 phase 顺序不变
- **WHEN** EditMode 测试通过 `CharacterFramePipelineHost` 推进一帧
- **THEN** 输入缓冲更新 MUST 早于 GameplayDecision
- **AND** BuildMotion MUST 早于 ExecuteMotion
- **AND** ExecuteMotion MUST 早于 PresentationBridge
- **AND** WriteSnapshotAndEvents MUST 仍最后发生

### Requirement: Runtime ports 表达兄弟提交者而非 FullBody 归属
角色 runtime port 契约 MUST 支持 Locomotion、Action、UpperBody 或等价行为域作为 Character frame owner 下的 sibling submitters。port 命名、职责和 adapter 边界 MUST NOT 将目标架构表达为 FullBody 拥有 Locomotion。

#### Scenario: Locomotion port 提交基础移动事实
- **WHEN** Character frame owner 需要基础移动输入
- **THEN** Locomotion runtime port MUST 能提交移动意图、移动事实、基础移动候选输出或等价 frame data
- **AND** 该 port MUST NOT 要求调用方是 FullBody controller 才能作为正式目标架构成立

#### Scenario: Action port 提交占用声明
- **WHEN** Action runtime 进入 Dodge、Attack 或等价全身动作
- **THEN** Action submitter MUST 能提交 full-body occupancy claim
- **AND** MAY 提交 action motion、action animation 或 input consume 候选输出
- **AND** MUST NOT 直接修改 Locomotion runtime 内部状态来表达压制结果

#### Scenario: Character host 是最终目标 owner
- **WHEN** 后续迁移角色 runtime host
- **THEN** Character-level host MUST 成为正式一帧 owner
- **AND** `PlayerFullBodyActionController` 或等价 FullBody host adapter MUST NOT 作为正式或迁移期 gameplay 入口存在
- **AND** 新增身体层 MUST NOT 依赖旧 FullBody host adapter 作为上级 owner

#### Scenario: Port 不泄漏 Unity 执行对象
- **WHEN** sibling submitter 通过 runtime port 提交 request 或 facts
- **THEN** port result MUST 是纯数据或受控接口结果
- **AND** MUST NOT 泄漏 `Transform`、`CharacterController`、Animancer state、Animator state 或 InputAction 作为仲裁输入权威

### Requirement: Prefab 绑定保持唯一角色帧管线
系统 MUST 确保 Corin prefab 和正式场景装配后仍只有一个角色级 `CharacterFramePipelineHost` 推进正式帧管线。Prefab 迁移 MUST NOT 通过新增 MonoBehaviour、runner、motion executor 或 presenter 绕过当前角色帧管线。

#### Scenario: Prefab 不新增第二管线
- **WHEN** 自动校验 Corin prefab 组件绑定
- **THEN** 生产路径 MUST 通过 `CharacterFrameRuntimeController -> CharacterFrameRuntimeHost -> CharacterFramePipeline`
- **AND** prefab MUST NOT 挂载新的正式 pipeline runner
- **AND** FullBody、Locomotion、Action MUST 仍只是 request 或 frame output 的提交者或 runtime adapter

#### Scenario: Scene 不覆盖出分裂路径
- **WHEN** 自动校验正式场景中的 Corin 实例
- **THEN** scene override MUST NOT 启用独立 Locomotion tick driver 作为正式 FullBody 并行路径
- **AND** scene override MUST NOT 新增第二个 action motion executor 或第二个 animation presenter 作为正式出口

#### Scenario: Runtime 引用迁移不改变管线持有关系
- **WHEN** prefab 迁移完成后检查生产代码和序列化组件
- **THEN** `CharacterFramePipeline` MUST 仍只由 `CharacterFramePipelineHost` 持有
- **AND** prefab MUST NOT 序列化或挂载新的 pipeline owner 组件
- **AND** FullBody tick adapter MUST 仍复用 controller 的同一个 host

### Requirement: CharacterFrameRuntimeController 持有正式 Runtime Host
系统 MUST 提供 `CharacterFrameRuntimeController` 或等价角色级 MonoBehaviour 作为正式 runtime host owner。该 controller MUST 位于 Character/Pipeline 或等价角色级目录语义下，MUST 持有唯一 `CharacterFrameRuntimeHost`，并且 MUST 通过角色级 runtime port 和 sibling submitters 推进 `CharacterFramePipeline`。

#### Scenario: Runtime host 不由 FullBody controller 创建
- **WHEN** 生产路径创建 `CharacterFrameRuntimeHost`
- **THEN** 创建职责 MUST 位于 `CharacterFrameRuntimeController` 或等价角色级 owner
- **AND** `PlayerFullBodyActionController` MUST NOT 直接 `new CharacterFrameRuntimeHost`
- **AND** `PlayerFullBodyActionController` MUST NOT 决定正式 submitter graph 中有哪些 sibling submitter

#### Scenario: Controller 不泄漏 Unity 对象进 Pipeline
- **WHEN** `CharacterFrameRuntimeController` 推进 `CharacterFramePipeline`
- **THEN** pipeline MUST 只接收 `ICharacterFrameRuntimePort` 或等价角色级 runtime port
- **AND** submitter graph MUST 只通过 Interface 访问 runtime 能力
- **AND** pipeline、submitter graph 和 frame model MUST NOT 保存 `MonoBehaviour`、`Transform`、`CharacterController`、Animator、Animancer runtime 或 InputAction

#### Scenario: Host 不是第二权威
- **WHEN** `CharacterFrameRuntimeController` 持有 runtime host
- **THEN** 状态权威 MUST 仍来自当前角色唯一 `CharacterStateMachineRunner`
- **AND** motion executor MUST 仍只有一个正式出口
- **AND** animation presenter MUST 仍只有一个正式出口

### Requirement: Character Runtime Port 组合兄弟 Adapter
角色级 runtime port MUST 能组合 Locomotion adapter、Action adapter、input buffer adapter、output runtime adapter 和 diagnostics adapter。该 port MUST NOT 通过 FullBody controller 的大操作面板表达所有能力，也 MUST NOT 要求新增身体域了解 FullBody-specific port。

#### Scenario: Locomotion 能力通过角色级 port 暴露
- **WHEN** Locomotion submitter 需要移动意图、Locomotion facts 或候选输出
- **THEN** 它 MUST 通过角色级 runtime port 的 Locomotion 能力或窄 `ILocomotionFrameRuntimePort` 获取
- **AND** MUST NOT 读取 `PlayerFullBodyActionController` 私有字段
- **AND** MUST NOT 通过 FullBody output runtime 执行压制

#### Scenario: Action 能力通过角色级 port 暴露
- **WHEN** Action submitter 需要 action request、state snapshot、policy 或 action motion facts
- **THEN** 它 MUST 通过角色级 runtime port 的 Action 能力或窄 action port 获取
- **AND** MUST NOT 要求 Locomotion submitter 成为其内部子模块
- **AND** MUST NOT 调用 Locomotion motion 或 animation output side effects

### Requirement: Character 级 Tick Adapter
系统 MUST 提供 `CharacterFrameRuntimeTickAdapter` 或等价角色级 tick adapter 作为 simulation tick 的正式 phase handler。旧 FullBody/Locomotion tick adapter MAY 作为退役诊断组件存在，但 MUST NOT 注册或推进 Corin 正式 gameplay tick。

#### Scenario: Tick adapter 注册 Character phases
- **WHEN** simulation tick driver 启用
- **THEN** 角色级 tick adapter MUST 注册 `ReadInput`、`UpdateInputBuffer`、`GameplayDecision`、`BuildMotion`、`ExecuteMotion`、`PresentationBridge` 和 `WriteSnapshotAndEvents`
- **AND** 每个 phase MUST 调用同一个 `CharacterFrameRuntimeController`
- **AND** MUST NOT 为 FullBody 和 Locomotion 分别注册独立 gameplay phase owner

#### Scenario: Tick adapter 防止双驱动
- **GIVEN** frame update 和 simulation tick driver 都存在
- **WHEN** 角色级 tick adapter 接管角色
- **THEN** `CharacterFrameRuntimeController` 的 frame auto update MUST 被关闭或跳过
- **AND** FullBody controller 与 Locomotion controller 的 direct auto update MUST 不作为正式 gameplay driver
- **AND** 冲突 MUST 可通过自动测试或装配校验发现

### Requirement: Character runtime port 去 FullBody 化
系统 MUST 将正式 Character runtime port Interface 收敛为角色级 frame 能力合同。该 Interface MUST NOT 通过继承 `IFullBodySubmissionRuntimePort`、`IFullBodyOutputRuntimePort` 或等价 FullBody port 来暴露 FullBody 内部操作面板。FullBody port MAY 保留在 FullBody adapter 内部。

#### Scenario: Character port 不继承 FullBody ports
- **WHEN** 检查正式 Character runtime port Interface
- **THEN** Interface MUST 只暴露角色帧 pipeline 所需能力
- **AND** MUST NOT 继承 FullBody submission runtime port
- **AND** MUST NOT 继承 FullBody output runtime port
- **AND** MUST NOT 要求 UpperBody、HitReact 或 Aim submitter 了解 FullBody port 面板

#### Scenario: FullBody adapter 内部保留领域端口
- **GIVEN** Action 仍需要 runner、snapshot、interrupt policy、action resistance 或 output runtime
- **WHEN** FullBody adapter 构建自己的提交或输出
- **THEN** 它 MAY 使用 FullBody-specific narrow ports
- **AND** 这些 ports MUST NOT 成为 Character-level host 的正式 Interface
- **AND** 这些 ports MUST NOT 被新身体域当作上级 owner

### Requirement: 旧 FullBody host adapter 退役
`PlayerFullBodyActionController` 或等价 MonoBehaviour MUST 从正式角色帧 owner、Unity 装配、配置解析、调试 view 和旧 Tick 兼容 Adapter 中退役。正式生产路径 MUST 通过 `CharacterFrameRuntimeController` 组合 Character-level runtime host，不得保留旧 FullBody host 作为可调用 gameplay 入口。

#### Scenario: 旧 Tick 入口不存在正式转发
- **WHEN** 兼容代码、prefab 或 scene 被检查
- **THEN** `PlayerFullBodyActionController.Tick` MUST NOT 作为正式或迁移期 gameplay 入口存在
- **AND** 旧 FullBody MonoBehaviour MUST NOT 构造第二条 phase 顺序
- **AND** 旧 FullBody MonoBehaviour MUST NOT 成为新增身体域的上级 owner

#### Scenario: 正式 host 位于 Character 层
- **WHEN** 生产路径创建角色帧 host
- **THEN** 正式 host MUST 位于 Character pipeline/runtime 语义下
- **AND** MUST 组合角色级 submitters、arbiter、composer 和 applier
- **AND** MUST NOT 由 FullBody controller 私有字段决定哪些身体域参与正式仲裁

### Requirement: 端口退役可测试
系统 MUST 提供自动测试证明 FullBody port 降级不会产生分裂路径。测试 MUST 覆盖 Character pipeline 不直接依赖 FullBody concrete adapter、新身体域不依赖 FullBody controller、旧 Tick 入口只转发到同一 host。

#### Scenario: 静态测试阻止 FullBody 泄漏
- **WHEN** 运行 runtime port 静态测试
- **THEN** 测试 MUST 确认 Character-level port 不继承 FullBody ports
- **AND** MUST 确认 `CharacterFramePipeline` 不引用 `PlayerFullBodyActionController`
- **AND** MUST 确认新增身体域不引用 FullBody 私有 runtime 状态作为仲裁输入权威

### Requirement: Character Runtime Core 纯 C# Owner
系统 MUST 提供 `CharacterRuntimeCore` 或批准的等价纯 C# 对象作为正式角色运行时 owner。该 owner MUST 组合正式 `CharacterFrameRuntimeHost`、正式 runtime port、Locomotion runtime module、Action runtime module、snapshot/restore 和 diagnostics 状态。MonoBehaviour MAY 创建、配置或持有该 core，但 MUST NOT 自身成为正式 runtime state、runner 或 lifecycle 的 owner。

#### Scenario: Core 无 Unity 对象构造
- **WHEN** EditMode 测试使用纯 C# fixture 构造正式角色 runtime core
- **THEN** core MUST 不要求 GameObject、Transform、MonoBehaviour 或 scene instance 才能创建
- **AND** core MUST 能持有一个正式 `CharacterFrameRuntimeHost`
- **AND** core MUST 能暴露正式 runtime port

#### Scenario: Mono Adapter 只拼装依赖
- **GIVEN** `CharacterFrameRuntimeController` 或批准的等价 Mono adapter 已显式绑定 config、input、motion executor 和 animation presenter
- **WHEN** adapter 初始化正式角色 runtime
- **THEN** adapter MUST 创建或接收一个 `CharacterRuntimeCore`
- **AND** MUST 将 Unity-facing dependencies 注入 core
- **AND** MUST NOT 创建第二个正式 `CharacterFramePipeline`、状态机 runner 或 lifecycle runtime

#### Scenario: Runtime Port 不反查 Mono Owner
- **WHEN** `CharacterFramePipeline` 通过正式 runtime port 运行 phase
- **THEN** port MUST 由 `CharacterRuntimeCore` 或 core-owned adapter 提供
- **AND** MUST NOT 通过 `CharacterFrameRuntimeController` 再查找 `PlayerLocomotionController` 或 `FullBodyActionRuntime` 来获得正式状态

#### Scenario: Replay 复用同一 Core
- **GIVEN** 独立 Rollback Debug Rig 的 replay adapter 已显式引用目标角色 runtime
- **WHEN** replay 执行 capture、restore 或 tick
- **THEN** replay MUST 复用目标角色的 `CharacterRuntimeCore` 或等价正式 owner
- **AND** MUST NOT 创建第二个 core、第二个 runner、第二个 motion executor 或第二个 animation presenter

### Requirement: Mono Adapter 运行时状态禁入
正式角色 runtime 状态 MUST 从 MonoBehaviour 字段迁出。`CharacterFrameRuntimeController`、`PlayerLocomotionController`、`FullBodyActionRuntime` 或批准的等价 Mono adapter MAY 保留序列化引用、Unity 生命周期入口和兼容 facade，但 MUST NOT 持有正式 `LocomotionRuntimeStateStore`、`CharacterRuntimeBlackboard`、`CharacterStateMachineRuntime`、`ActionLifecycleRuntime` 或 `CharacterFrameRuntimeHost` 作为 authoritative state。

#### Scenario: Locomotion 状态不由 Controller 持有
- **WHEN** 自动静态测试扫描正式 production runtime 代码
- **THEN** `PlayerLocomotionController` MUST NOT new 或持有正式 `LocomotionRuntimeStateStore`
- **AND** MUST NOT new 或持有正式 `CharacterRuntimeBlackboard`
- **AND** Locomotion state MUST 由 core-owned Movement/Locomotion runtime module 持有

#### Scenario: Action 状态不由 Mono Runtime 持有
- **WHEN** 自动静态测试扫描正式 production runtime 代码
- **THEN** `FullBodyActionRuntime` MUST NOT new 或持有正式 `CharacterStateMachineRuntime`
- **AND** MUST NOT new 或持有正式 `ActionLifecycleRuntime`
- **AND** Action state MUST 由 core-owned Action runtime module 持有

#### Scenario: Controller 不持有正式 Host
- **WHEN** `CharacterFrameRuntimeController` 在 Play Mode 初始化
- **THEN** 它 MUST 通过 `CharacterRuntimeCore` 推进正式 tick
- **AND** MUST NOT 自身持有 authoritative `CharacterFrameRuntimeHost`
- **AND** MUST NOT 直接 new 第二个 pipeline host 作为 fallback

### Requirement: CharacterFrame runtime 必须是唯一正式驱动入口

正式角色运行时 MUST 由 `CharacterFrameRuntimeController` 及其正式 tick adapter 驱动 Locomotion 与 Action。旧 tick adapter 不得注册、推进或旁路驱动正式 runtime。

#### Scenario: 正式 prefab 只有 CharacterFrame 驱动链

- **GIVEN** Corin 正式 prefab 被加载或静态扫描
- **WHEN** 测试检查 runtime 驱动组件
- **THEN** prefab 存在且只存在一条 `CharacterFrameRuntimeController` 正式驱动链
- **AND** `FullBodyActionTickAdapter` 与 `LocomotionTickAdapter` 不作为可驱动组件挂载

#### Scenario: 退役 adapter 不参与运行时推进

- **GIVEN** 项目仍保留退役 adapter 类型用于迁移或诊断
- **WHEN** runtime 初始化与 frame tick 执行
- **THEN** 退役 adapter 不注册到正式 tick 流
- **AND** 退役 adapter 不调用 Locomotion 或 Action 的推进 API

### Requirement: 运行时端口不得依赖旧 Host Adapter

正式 Action 运行时端口 MUST 不依赖 `PlayerFullBodyActionController` 或旧 Host Adapter。状态、请求、动画播放和诊断数据必须通过当前正式端口与视图暴露。

#### Scenario: Action 不需要旧 Host Adapter

- **GIVEN** Action runtime 被构建
- **WHEN** Action 请求、状态推进和动画状态被访问
- **THEN** 调用链不需要 `PlayerFullBodyActionController`
- **AND** 不需要从旧 Host Adapter 读取配置或状态

#### Scenario: 冲突诊断不要求保留旧驱动组件

- **GIVEN** 测试需要发现 prefab/scene 中的重复驱动或旧组件
- **WHEN** 静态扫描执行
- **THEN** 扫描可以识别旧组件名或旧字段名
- **AND** 不要求旧组件作为可挂载正式 runtime 类继续存在

### Requirement: FullBody Host Adapter
系统 MUST 删除 `PlayerFullBodyActionController` 或等价 FullBody MonoBehaviour host adapter。正式角色帧 runtime port MUST 由 `CharacterFrameRuntimeController` 或等价角色级 owner 组合状态机 runtime、Locomotion runtime、Action runtime、output runtime 和 diagnostics dependencies。生产路径 MUST NOT 使用 `FullBodyRuntimePortAdapter` 包装 `PlayerFullBodyActionController` 暴露 pipeline 所需能力。

#### Scenario: 旧 controller 类型被删除
- **WHEN** 检查生产运行时代码、测试 fixture、prefab 和 scene
- **THEN** `PlayerFullBodyActionController` 类型 MUST 不再作为正式组件、字段、属性、构造参数或端口依赖存在
- **AND** `CharacterFramePipeline`、submitter graph 和 builder MUST NOT 直接或间接依赖该类型

#### Scenario: Runner owner 迁入状态机运行时
- **WHEN** 角色 runtime 初始化或恢复状态
- **THEN** 当前角色唯一 `CharacterStateMachineRunner` MUST 由 `CharacterStateMachineRuntime` 或等价状态机运行时模块拥有
- **AND** Locomotion adapter、Action runtime、motion executor 和 animation presenter MUST NOT 创建第二个正式 runner
- **AND** runner owner MUST NOT 通过 FullBody controller MonoBehaviour 表达

#### Scenario: Output dependencies 不经过 controller 大面板
- **WHEN** 角色帧 output apply 需要 input buffer、motion executor、animation presenter、Locomotion output、facts writer 或 diagnostics
- **THEN** runtime port MUST 通过明确 dependencies host、output runtime 或窄端口提供这些能力
- **AND** MUST NOT 通过 `PlayerFullBodyActionController` 的公开属性或内部类访问
- **AND** MUST NOT 创建 fallback executor、fallback presenter 或隐藏默认配置

### Requirement: Submitter Graph 依赖窄端口
`CharacterFrameSubmitterGraph` MUST 只依赖 Character、Locomotion、Action、StateMachine 和 Output 的窄端口。它 MUST NOT 依赖 `PlayerFullBodyActionController`、`FullBodyRuntimePortAdapter` 或单个 FullBody 集成端口来读取所有 runtime 状态。

#### Scenario: Submitter Graph 不包装 FullBody controller
- **WHEN** 构建角色 runtime port 与 submitter graph
- **THEN** Locomotion submitter MUST 通过 Locomotion runtime port 获取 Locomotion 所需数据
- **AND** Action submitter MUST 通过 action runtime/state facts 窄端口获取 action 所需数据
- **AND** submitter graph MUST NOT 通过 `PlayerFullBodyActionController` 或 `FullBodyRuntimePortAdapter` 访问 runner、Dodge config、Locomotion snapshot、output runtime 或 diagnostics

### Requirement: PlayerFullBodyActionController 删除验证
系统 MUST 提供自动测试验证 `PlayerFullBodyActionController` 已从正式 runtime 边界删除。测试 MUST 覆盖代码引用、prefab/scene 绑定、runtime port 组合和 rollback fixture 迁移。

#### Scenario: 静态边界验证无旧 controller
- **WHEN** 运行 runtime port 静态边界测试
- **THEN** 测试 MUST 确认生产 runtime 代码不定义 `PlayerFullBodyActionController`
- **AND** MUST 确认生产 runtime 代码不引用 `PlayerFullBodyActionController`
- **AND** MUST 确认 Corin prefab/scene 不挂载该组件

#### Scenario: 行为测试仍走角色级端口
- **WHEN** 运行 Character frame runtime controller 定向 EditMode 测试
- **THEN** 测试 MUST 通过角色级 runtime port 推进 Locomotion 和 Dodge
- **AND** MUST 证明状态、motion、animation、facts 和 snapshot 仍来自同一条 `CharacterFramePipeline`

### Requirement: 正式 Prefab Runtime Adapter 收敛
系统 MUST 让 Corin 正式 prefab 只挂载一个 gameplay runtime assembly adapter。该 adapter MUST 负责创建、持有或绑定 `CharacterRuntimeCore`，并将输入、运动、动画、facing、tick 和配置等 Unity-facing adapters 注入 core dependencies。`PlayerLocomotionController`、`FullBodyActionRuntime` 或等价迁移期 facade MUST NOT 作为正式 prefab 组件表达 Locomotion 或 Action owner。

#### Scenario: Prefab 只有一个 runtime assembly adapter
- **WHEN** 自动校验 `Assets/Prefabs/Character/可琳.prefab` 和 `Assets/Prefabs/Character/可琳_Humanoid.prefab`
- **THEN** 每个 prefab MUST 只有一个正式 gameplay runtime assembly adapter
- **AND** 该 adapter MUST 绑定同一个 `CharacterRuntimeCore`
- **AND** prefab MUST NOT 同时挂载 `PlayerLocomotionController` 和 `FullBodyActionRuntime` 作为正式 gameplay runtime facade

#### Scenario: Unity-facing adapters 保留为窄 seam
- **WHEN** 自动校验 Corin 正式 prefab 的 MonoBehaviour 清单
- **THEN** prefab MAY 保留输入、输入缓冲、motion executor、Animancer presenter、facing/camera basis、presentation interpolation 和 tick registration adapter
- **AND** 这些 adapters MUST 只满足各自 Unity seam 的 Interface
- **AND** MUST NOT 自行持有正式 runtime state、runner、lifecycle 或 frame pipeline host

#### Scenario: 不通过减少 Mono 数量破坏 seam
- **WHEN** 实施 prefab 收敛
- **THEN** 系统 MUST NOT 把 Animancer runtime、CharacterController、Transform、InputAction 或 scene object 放入 pure C# core/module
- **AND** MUST NOT 为了合并 MonoBehaviour 而让 runtime assembly adapter 直接执行运动、播放动画或消费输入

### Requirement: 迁移期 Facade 从正式装配退场
`PlayerLocomotionController` 和 `FullBodyActionRuntime` MUST 从正式代码面删除，旧测试、旧 fixture、旧 assembler 和旧 debug rig MUST NOT 继续依赖这些迁移期 facade。Locomotion 与 Action runtime state MUST 只由 `CharacterRuntimeCore` 组合的 module 持有，并通过窄 Unity-facing adapters 装配。

#### Scenario: Locomotion facade 不在正式 prefab 上
- **WHEN** 自动校验 Corin 正式 prefab 和正式 scene override
- **THEN** `PlayerLocomotionController` MUST NOT 作为正式 gameplay 组件存在
- **AND** Locomotion runtime state MUST 仍由 `CharacterRuntimeCore` 组合的 `LocomotionRuntimeModule` 或批准等价 Module 持有
- **AND** 移动输入、facing 和 motion executor MUST 通过窄 Unity-facing adapters 注入

#### Scenario: Action facade 不在正式 prefab 上
- **WHEN** 自动校验 Corin 正式 prefab 和正式 scene override
- **THEN** `FullBodyActionRuntime` MUST NOT 作为正式 gameplay 组件存在
- **AND** Action runtime state MUST 仍由 `CharacterRuntimeCore` 组合的 `FullBodyActionRuntimeModule` 或批准等价 Module 持有
- **AND** Action request、lifecycle、claim、motion 和 animation 输出 MUST 继续通过角色帧管线推进

#### Scenario: 旧兼容代码不保留
- **WHEN** 自动静态测试扫描 production runtime、测试 fixture 和 prefab 装配
- **THEN** `PlayerLocomotionController` 和 `FullBodyActionRuntime` 类型 MUST 不存在
- **AND** 旧测试 MUST 改为直接使用 `CharacterFrameRuntimeController`、`CharacterRuntimeCore`、`LocomotionRuntimeModule` 或 `FullBodyActionRuntimeModule`
- **AND** 系统 MUST NOT 保留注册旧 tick、创建第二 core、创建第二 runner、创建第二 motion executor、创建第二 animation presenter 或第二 pipeline host 的兼容入口

### Requirement: Prefab 装配 Allowlist 验证
系统 MUST 提供自动测试验证 Corin 正式 prefab 和正式 scene runtime 装配。测试 MUST 以 allowlist 方式区分 runtime assembly adapter、Unity-facing adapter、迁移期 facade 和 debug tooling，并在出现第二出口、debug tooling 或旧 facade 时失败。

#### Scenario: Prefab 脚本清单可验证
- **WHEN** 运行 EditMode prefab binding 测试
- **THEN** 测试 MUST 解析两个 Corin prefab 的 MonoBehaviour 脚本清单
- **AND** MUST 确认脚本清单只包含批准的 runtime assembly adapter 和 Unity-facing adapters
- **AND** MUST 对未分类 runtime 脚本报错

#### Scenario: 唯一副作用出口
- **WHEN** 运行 EditMode prefab binding 测试
- **THEN** 每个正式 prefab MUST 只有一个 motion executor adapter
- **AND** MUST 只有一个 animation presenter adapter
- **AND** MUST 没有第二 pipeline runner、第二 state runner 或第二 runtime host owner

#### Scenario: Debug tooling 不挂正式角色
- **WHEN** 运行 EditMode prefab/scene boundary 测试
- **THEN** 正式角色 prefab 和正式 scene instance MUST NOT 挂载 rollback debug runner、history recorder、hidden replay adapter 或 synctest runner
- **AND** rollback debug rig MUST 继续通过显式 target 引用连接正式角色 runtime

