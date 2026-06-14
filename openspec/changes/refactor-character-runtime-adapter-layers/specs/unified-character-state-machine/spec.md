## ADDED Requirements
### Requirement: Runtime Adapter 拆分不改变状态机权威
系统 MUST 保持 `PlayerFullBodyActionController` 作为统一角色状态机的唯一正式 runtime owner。为拆分 Runtime Adapter 而新增的 Solver、Diagnostics、Reference Resolver、Factory 或 Pipeline helper MUST NOT 创建第二个 `CharacterStateMachineRunner`，MUST NOT 注册新的 gameplay tick driver，MUST NOT 直接切换状态机。

#### Scenario: 只有 FullBody controller 创建正式 runner
- **WHEN** 检查正式运行时代码
- **THEN** 只有 `PlayerFullBodyActionController` MAY 创建 `CharacterStateMachineRunner`
- **AND** 拆出的 Character Module MUST NOT 调用 `new CharacterStateMachineRunner`

#### Scenario: Pipeline helper 不成为第二状态机
- **WHEN** `FullBodyFramePipeline` 将 action request、diagnostics 或 step helper 拆出
- **THEN** helper MUST 只构建纯数据输入输出
- **AND** helper MUST NOT 保存独立 active state path 作为状态权威
- **AND** helper MUST NOT 绕过 pipeline phase order 推进状态

#### Scenario: Runtime Adapter 只执行被授权输出
- **WHEN** 统一状态机产出 movement、animation 或 fact 输出
- **THEN** 对应 Runtime Adapter MUST 只执行该输出或提交对应纯数据 facts
- **AND** Runtime Adapter MUST NOT 根据自身内部播放状态或 Unity 对象独自决定进入、退出或切换 FullBody 状态

### Requirement: FullBody Pipeline 分层保持一帧顺序
系统 MUST 允许 `FullBodyFramePipeline` 将 request gate input 构建、snapshot 日志和 presentation write-back 辅助逻辑拆到内部 Module，但一帧 phase 顺序仍由 `FullBodyFramePipeline` 或等价正式 pipeline 统一掌握。

#### Scenario: Phase order 不变
- **WHEN** FullBody pipeline helper 被拆出
- **THEN** `ReadInput / UpdateInputBuffer / GameplayDecision / BuildMotion / ExecuteMotion / PresentationBridge / WriteSnapshotAndEvents` 的既有顺序 MUST 保持
- **AND** helper MUST NOT 自行跳过或重排 phase

#### Scenario: Action request resolver 保持纯数据
- **WHEN** FullBody pipeline 构建 Action request gate input
- **THEN** resolver MUST 只读取当前 frame input、状态机 snapshot、runtime blackboard snapshot 和正式 action config
- **AND** resolver MUST NOT 读取 Animancer runtime state、CharacterController、InputAction 或 Camera.main

#### Scenario: 拆分后 replay 仍走正式 pipeline
- **WHEN** rollback/replay 或 synctest 推进角色一帧
- **THEN** 它 MUST 继续使用正式 FullBody pipeline 或其已批准入口
- **AND** MUST NOT 因拆分新增仅测试使用的独立状态推进路径
