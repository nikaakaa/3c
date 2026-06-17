## ADDED Requirements
### Requirement: Locomotion Frame Runtime 模块化
系统 MUST 将 Locomotion prepare/evaluate/build 的运行时实现拆分为明确的 frame runtime modules。`ILocomotionFrameRuntimePort` MUST 保持为 FullBody submission builder 访问 Locomotion 子职责的唯一入口；该入口背后的实现 MUST NOT 长期由 `PlayerLocomotionController` 独自承载。纯 `LocomotionFrameBuilder` MUST 继续只处理纯数据构建，不得接管 Unity 引用解析、运动执行、动画表现或状态机 runner ownership。

#### Scenario: FullBody 只看 frame runtime port
- **WHEN** `FullBodySubmissionBuilder` 需要 Locomotion decision 或 motion frame
- **THEN** 它 MUST 只调用 `ILocomotionFrameRuntimePort`
- **AND** MUST NOT 引用 `PlayerLocomotionController`
- **AND** MUST NOT 读取 Locomotion controller 的 Unity scene object

#### Scenario: Frame runtime 迁出 controller
- **WHEN** Locomotion frame runtime 执行 prepare/evaluate/build
- **THEN** 具体实现 MUST 位于 `LocomotionFrameRuntime`、adapter 或等价模块中
- **AND** `PlayerLocomotionController` MUST 只负责装配和兼容入口委托
- **AND** controller MUST NOT 继续复制完整 frame runtime 操作面板

#### Scenario: Pure builder 不执行副作用
- **WHEN** `LocomotionFrameBuilder` 构建 decision 或 motion frame
- **THEN** 它 MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 引用 `MonoBehaviour`、`Transform`、`CharacterController`、Animancer runtime type 或 InputAction

#### Scenario: Runtime state restore 保持一致
- **WHEN** Locomotion runtime state 被 capture 后 restore
- **THEN** run latch、last moving gait、current intent、current frame、phase time、previous direction 和 pending TurnBack intent MUST 与迁移前保持等价
- **AND** rollback/replay tests MUST 能证明 restore 后同输入序列结果一致

### Requirement: Locomotion Frame Runtime 职责分层
系统 MUST 将 Locomotion frame runtime 分为 adapter、runtime coordinator、runtime state store、facts providers 和 pure frame builder。每层 Module 的 Interface MUST 只暴露下一层所需的 plain facts 或 result，不得把 Unity host、output side effects 或状态机 runner ownership 泄漏进 pure builder。

#### Scenario: Facts provider 输出 plain facts
- **WHEN** frame runtime provider 解析 input、camera、facing、phase 或 motion profile
- **THEN** provider MUST 输出 plain data facts
- **AND** pure builder MUST NOT 接收 `Transform`、`Camera`、`CharacterController` 或 input runtime object
- **AND** provider MUST NOT 执行动作位移或动画表现

#### Scenario: Runtime coordinator 只编排 frame 构建
- **WHEN** `LocomotionFrameRuntime` 执行本帧 Locomotion 构建
- **THEN** 它 MUST 编排 prepare/evaluate/build
- **AND** MUST NOT 提交最终角色输出
- **AND** MUST NOT 调用 `CharacterFramePipeline`
- **AND** MUST NOT 创建独立 Locomotion tick 主线

#### Scenario: State store 是唯一 Locomotion 局部状态来源
- **WHEN** run latch、last moving gait、previous direction 或 pending TurnBack intent 被读取或写入
- **THEN** 访问 MUST 经过 Locomotion runtime state store 或等价集中 Module
- **AND** controller MUST NOT 同时保存第二份 authoritative value
- **AND** rollback capture/restore MUST 使用同一状态来源

### Requirement: Locomotion Frame Runtime 不得恢复分裂主线
系统 MUST 保持 FullBody submission builder 通过 `ILocomotionFrameRuntimePort` 向 Locomotion 提交数据请求。Locomotion frame runtime MUST NOT 重新成为独立最终输出管线，也不得绕过统一角色帧 pipeline。

#### Scenario: Locomotion 只提交 frame 数据
- **WHEN** FullBody 状态机需要 Locomotion 数据
- **THEN** Locomotion frame runtime MUST 返回 decision/motion frame 数据
- **AND** MUST NOT 自己写入最终 `CharacterFrameSubmission`
- **AND** MUST NOT 自己调用 final output applier

#### Scenario: Direct tick 不回到正式主线
- **WHEN** 项目保留 Locomotion direct tick 诊断或测试入口
- **THEN** 该入口 MUST 标记为非正式提交主线
- **AND** MUST NOT 与 unified character frame pipeline 竞争 authoritative output
