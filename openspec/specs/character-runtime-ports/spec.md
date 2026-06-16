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

### Requirement: FullBody Host Adapter
系统 MUST 将 `PlayerFullBodyActionController` 或等价 MonoBehaviour 收窄为 FullBody runtime host adapter。该 host adapter MUST 负责 Unity 引用装配、配置解析、唯一 `CharacterStateMachineRunner` owner 和兼容 tick 入口。生产路径 MUST 使用 `FullBodyRuntimePortAdapter` 或等价包装 adapter 暴露 pipeline 所需端口，`PlayerFullBodyActionController` MUST NOT 作为 `CharacterFramePipeline` 和 `FullBodySubmissionBuilder` 的宽 Interface。

#### Scenario: 兼容 Tick 入口进入端口化管线
- **WHEN** 旧兼容入口调用 `PlayerFullBodyActionController.Tick`
- **THEN** 该入口 MUST 构造或提供包装 runtime port adapter
- **AND** MUST 通过同一个 `CharacterFramePipeline` 推进一帧
- **AND** MUST NOT 维护与 tick phase adapter 不同的状态推进顺序

#### Scenario: 包装 adapter 隔离 controller 操作面板
- **WHEN** `CharacterFramePipeline` 或 `FullBodySubmissionBuilder` 需要 FullBody runtime 能力
- **THEN** 它们 MUST 依赖 runtime port Interface
- **AND** 生产实现 MUST 通过 `FullBodyRuntimePortAdapter` 或等价包装 adapter 转接 `PlayerFullBodyActionController`
- **AND** pipeline 和 builder MUST NOT 直接接收 `PlayerFullBodyActionController`

#### Scenario: Runner owner 不迁移
- **WHEN** FullBody host adapter 初始化或恢复角色
- **THEN** 它 MUST 继续是正式 `CharacterStateMachineRunner` owner
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
系统 MUST 在运行时端口化过程中保持当前唯一 Character frame pipeline、唯一统一状态机 runner、统一 motion executor 和 Animancer presenter 权威。实施 MUST NOT 新增 fallback 配置、第二角色控制器、第二 frame pipeline、第二 Locomotion 状态机、第二 Action runtime 或未审批 rollback/playback 语义。

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

