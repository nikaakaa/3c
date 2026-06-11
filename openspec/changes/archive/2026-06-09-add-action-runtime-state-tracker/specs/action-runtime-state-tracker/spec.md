## ADDED Requirements

### Requirement: Action 运行时状态快照
系统 MUST 提供纯数据 Action 运行时状态快照，用于表达当前 action state、elapsed seconds、current resistance 和 current tick。快照 MUST NOT 依赖 Unity 场景对象、Animancer、Animator、AnimationClip、CharacterController、Input System 或 BBB 运行时类型。

#### Scenario: 快照承载当前 Action 事实
- **WHEN** 系统创建 Action runtime snapshot
- **THEN** snapshot MUST 保存 current state
- **AND** snapshot MUST 保存 elapsed seconds
- **AND** snapshot MUST 保存 current resistance
- **AND** snapshot MUST 保存 current tick

#### Scenario: 快照安全处理非法数值
- **GIVEN** elapsed seconds、resistance 或 tick 传入负值
- **WHEN** 系统创建 snapshot
- **THEN** snapshot MUST 使用非负值表达这些事实

### Requirement: Action 运行时状态跟踪器
系统 MUST 提供 Action 运行时状态跟踪器，用于保存和推进当前 Action 事实。该 tracker MUST 只负责事实存储和事实更新，不得实现完整状态机、状态图、transition、自动退出、输入消费或动画播放。

#### Scenario: 默认状态为空 Action
- **WHEN** 系统创建 tracker
- **THEN** current state MUST 为 `Action.None` 或等价空 action state
- **AND** elapsed seconds MUST 为 0
- **AND** current resistance MUST 为 0
- **AND** current tick MUST 为 0

#### Scenario: EnterState 更新当前事实
- **WHEN** 外部要求 tracker 进入一个 action state
- **THEN** tracker MUST 更新 current state
- **AND** MUST 更新 current resistance
- **AND** MUST 重置 elapsed seconds

#### Scenario: Tick 推进时间
- **GIVEN** tracker 处于任意 action state
- **WHEN** tracker tick 一个正 delta seconds
- **THEN** elapsed seconds MUST 增加对应时长
- **AND** current tick MUST 更新为本次 tick

#### Scenario: Tracker 不自动退出
- **GIVEN** tracker 处于任意 action state
- **WHEN** tracker tick 任意时长
- **THEN** tracker MUST NOT 因 duration、动画结束或 hidden rule 自动改变 current state

### Requirement: 仲裁上下文输出
系统 MUST 能从 Action runtime tracker 当前事实生成 `ActionInterruptContext`，供现有 `ActionInterruptArbiter` 消费。该上下文 MUST 只包含当前状态 ID、elapsed seconds、current resistance 和 current tick。

#### Scenario: 输出当前 Action 仲裁上下文
- **GIVEN** tracker 当前 state 为 `Action.Attack01`
- **AND** elapsed seconds 为 0.2
- **AND** current resistance 为 30
- **AND** current tick 为 12
- **WHEN** 系统创建 interrupt context
- **THEN** context current state MUST 为 `Action.Attack01`
- **AND** context elapsed seconds MUST 为 0.2
- **AND** context current resistance MUST 为 30
- **AND** context current tick MUST 为 12

### Requirement: 仲裁决策应用
系统 MUST 能将现有 `ActionInterruptDecision` 应用到 Action runtime tracker。accepted decision MUST 更新 tracker 当前 action state；rejected decision MUST 保持当前状态事实不变。

#### Scenario: accepted decision 更新当前 Action
- **GIVEN** tracker 当前 state 为 `Action.None`
- **AND** 一个 accepted decision 的 target state 为 `Action.Dodge`
- **WHEN** tracker 应用该 decision
- **THEN** current state MUST 变为 `Action.Dodge`
- **AND** elapsed seconds MUST 重置为 0

#### Scenario: accepted decision 使用调用方提供的抗性
- **GIVEN** 一个 accepted decision 的 target state 为 `Action.Dodge`
- **AND** 调用方提供 target resistance 为 40
- **WHEN** tracker 应用该 decision
- **THEN** current resistance MUST 为 40

#### Scenario: rejected decision 不改变事实
- **GIVEN** tracker 当前 state 为 `Action.Attack01`
- **AND** tracker elapsed seconds 大于 0
- **AND** 一个 rejected decision
- **WHEN** tracker 应用该 decision
- **THEN** current state MUST 保持 `Action.Attack01`
- **AND** elapsed seconds MUST NOT 被重置
- **AND** current resistance MUST NOT 改变

#### Scenario: 仲裁器 decision 可驱动 tracker
- **GIVEN** tracker 输出当前 interrupt context
- **AND** 存在匹配的 request 和 policy
- **WHEN** `ActionInterruptArbiter` 返回 accepted decision
- **AND** tracker 应用该 decision
- **THEN** tracker current state MUST 进入 decision target state

### Requirement: 现有运行时边界保持
系统 MUST 保持当前 Locomotion、输入缓冲和动画 Presenter 边界。本变更 MUST NOT 接管基础移动，不得改变 `Idle / MoveStart / MoveLoop / MoveStop` 状态图，也不得让 Action runtime tracker 成为 `MoveStop -> MoveStart` 的必需依赖。

#### Scenario: Locomotion 不依赖 Action tracker
- **WHEN** 当前基础移动状态机处理移动输入和停止输入
- **THEN** `Idle / MoveStart / MoveLoop / MoveStop` 流转 MUST 继续由 Locomotion 状态图处理
- **AND** 基础移动状态机 MUST NOT 依赖 Action runtime tracker

#### Scenario: 输入缓冲不被本变更消费
- **WHEN** 本变更实现完成
- **THEN** Action runtime tracker MUST NOT 直接读取或消费 `InputRequestBuffer`
- **AND** 按钮到 action request 的映射 MUST 保留给后续变更

#### Scenario: Presenter 不读取 Action tracker
- **WHEN** 基础移动动画 Presenter 播放移动阶段 alias
- **THEN** Presenter MUST NOT 读取 Action runtime tracker
- **AND** Presenter MUST NOT 通过 Action runtime tracker 决定动画播放

### Requirement: 可测试和可诊断
系统 MUST 提供自动测试和静态边界验证，证明 Action runtime tracker 可初始化、可进入状态、可计时、可输出仲裁上下文、可应用仲裁决策，并且不会引入动画、输入或角色控制旁路。

#### Scenario: 自动测试覆盖 Action 当前事实
- **WHEN** 运行 Action runtime tracker EditMode 测试
- **THEN** 测试 MUST 覆盖默认状态、EnterState、Tick、负值安全处理、snapshot、interrupt context、accepted decision、rejected decision、仲裁器组合和不自动退出

#### Scenario: 静态验证模块边界
- **WHEN** 检查 `Assets/Scripts/Character/Action` 源码
- **THEN** 静态搜索 MUST 能确认该模块不引用 Animancer、AnimationClip、Animator、CharacterController、Cinemachine、Input System 或 `BBBNexus`

#### Scenario: 手动验证现有移动不回退
- **WHEN** 用户在演示场景中测试 WASD、Look、Idle、MoveStart、MoveLoop 和 MoveStop
- **THEN** 行为 MUST 不因新增 Action runtime tracker 发生变化
