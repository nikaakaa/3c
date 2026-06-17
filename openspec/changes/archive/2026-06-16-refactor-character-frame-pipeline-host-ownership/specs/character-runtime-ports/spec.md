## ADDED Requirements
### Requirement: Character Frame Pipeline Host Ownership
系统 MUST 提供纯 C# `CharacterFramePipelineHost` 作为每个角色的唯一角色帧运行时持有者。该 host MUST 位于角色级 Pipeline runtime 目录，MUST 持有正式 `CharacterFramePipeline` 和角色帧提交者 Adapter，MUST NOT 作为 MonoBehaviour、第二 pipeline、第二 runner、第二 motion executor 或第二 animation presenter 存在。

#### Scenario: MonoBehaviour 不直接创建 Pipeline
- **WHEN** 检查生产 MonoBehaviour runtime adapter
- **THEN** `PlayerFullBodyActionController` 或等价 Unity adapter MUST NOT 直接 `new CharacterFramePipeline`
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
