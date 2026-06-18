# character-behavior-submission-contracts Specification

## Purpose
定义角色行为提交的类型化纯数据合同，区分 request、output、cue、diagnostic 和 state write 边界，作为 CharacterFramePipeline 收集候选输出的共享语言。
## Requirements
### Requirement: Behavior Submission 类型化合同
系统 MUST 提供类型化 behavior submission 合同，至少区分 request、output、cue、diagnostic 和 state write payload。系统 MUST NOT 使用一个无边界万能 payload 承载所有行为输出。

#### Scenario: Request 与 Output 分离
- **WHEN** behavior 合同表达 action request candidate
- **THEN** 该 payload MUST 属于 request submission
- **AND** MUST NOT 同时承载 motion 或 animation output candidate

#### Scenario: Cue 与 Diagnostic 分离
- **WHEN** behavior 合同表达表现触发意图
- **THEN** 该 payload MUST 属于 cue submission
- **AND** diagnostic submission MUST 只表达调试信息
- **AND** diagnostic MUST NOT 作为 gameplay fact 或 presentation cue 被执行

### Requirement: Behavior Submission Consumer 边界
每类 typed behavior submission MUST 声明允许 consumer、owner 和错误处理方式。系统 MUST NOT 让 submission 被未授权阶段消费，也 MUST NOT 静默丢弃必须消费的 submission。

#### Scenario: Output Submission 只进入 Composer
- **WHEN** behavior leaf 提交 motion、animation 或 body claim output
- **THEN** 该 payload MUST 只被 behavior submission composer、frame arbitration input 或批准的等价角色帧计划入口消费
- **AND** MUST NOT 被 leaf、runner 或 condition evaluator 直接应用

#### Scenario: Diagnostic 不影响 Gameplay
- **WHEN** behavior leaf 提交 diagnostic submission
- **THEN** diagnostic consumer MUST 只记录或暴露诊断
- **AND** MUST NOT 改变 request accepted、frame plan、blackboard fact 或 presentation cue

### Requirement: RequestPass 与 OutputPass 边界
系统 MUST 明确 behavior evaluation pass 边界。RequestPass MAY 收集请求候选和诊断，MUST NOT 输出最终 motion / animation / cue apply 意图；OutputPass MAY 输出运动、动画、claim、cue 和事实候选，MUST NOT 重新接受或拒绝 action request。

#### Scenario: RequestPass 不提交最终输出
- **WHEN** runner 或 collector 处于 RequestPass
- **THEN** behavior leaf MUST NOT 提交最终 motion candidate
- **AND** MUST NOT 提交最终 animation candidate
- **AND** MUST NOT 提交 input consume apply 结果

#### Scenario: OutputPass 不做请求仲裁
- **WHEN** runner 或 collector 处于 OutputPass
- **THEN** behavior leaf MUST NOT 重新决定 action request accepted/rejected
- **AND** MUST 只消费 RequestPass 已产生或 frame context 已确认的纯数据结果

### Requirement: Behavior Submission Set 纯数据聚合
系统 MUST 提供 `CharacterBehaviorSubmissionSet` 或等价聚合模型，用于按 source、pass 和稳定顺序保存 typed submissions。该集合 MUST NOT 执行副作用，也 MUST NOT 成为 body arbiter、frame plan 或 output applier 的替代品。

#### Scenario: 多源稳定收集
- **GIVEN** fake Locomotion leaf 和 fake Action leaf 在同一 tick 输出 submission
- **WHEN** fake runner 收集结果
- **THEN** submission set MUST 按定义顺序保存两个 source 的结果
- **AND** MUST 保留 source node id、source step 和 pass

#### Scenario: 聚合不执行副作用
- **WHEN** submission set 被创建或查询
- **THEN** 它 MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 写 runtime blackboard

### Requirement: 状态所有权明确
系统 MUST 明确 behavior node private state、Locomotion runtime state、Action lifecycle state、animation playback state、confirmed blackboard facts、rollback restore state 和 editor graph state 的 owner。跨 owner 的沟通 MUST 通过 submission、snapshot、frame plan 或批准的纯数据接口。

#### Scenario: Action 不写 Locomotion 私有状态
- **WHEN** Action source 需要表达 full-body claim 压制基础移动
- **THEN** 它 MUST 通过 output submission / body claim 表达
- **AND** MUST NOT 直接修改 Locomotion runtime state

#### Scenario: 确认事实只归黑板
- **WHEN** behavior output submission 包含 window fact candidate
- **THEN** 它 MUST 仍然只是候选
- **AND** confirmed gameplay fact MUST 只在 frame plan 采用并由 output applier 写入后进入 `CharacterRuntimeBlackboard`

### Requirement: Fake Runner 只验证合同
本变更 MAY 提供 fake runner、fake collector 或 fake leaf evaluator 用于测试 typed submission 合同。Fake runner MUST NOT 作为生产 runtime 入口注册到 `CharacterRuntimeCore`、`CharacterFrameRuntimeHost`、prefab、scene 或 rollback replay 主线。

#### Scenario: Fake runner 收集提交
- **WHEN** EditMode 测试运行 fake runner
- **THEN** fake runner MAY 收集 fake leaf submissions
- **AND** MUST 只验证合同行为

#### Scenario: Fake runner 不进入生产
- **WHEN** 检查正式 runtime 装配
- **THEN** fake runner MUST NOT 被 `CharacterRuntimeCore` 默认创建
- **AND** MUST NOT 被 prefab 或 scene 作为 gameplay tick 入口引用

### Requirement: Behavior Submission 合同可测试和可验证
系统 MUST 提供自动测试和静态边界验证，证明 submission 合同是纯数据、pass 边界清楚、状态 owner 明确且没有 Unity / Editor / Ref runtime 泄漏。

#### Scenario: 自动测试覆盖合同
- **WHEN** 运行 behavior submission contract EditMode 测试
- **THEN** 测试 MUST 覆盖 typed payload、pass boundary、source order、empty set 和 state ownership

#### Scenario: 静态边界验证
- **WHEN** 检查 behavior submission contract 源码
- **THEN** 静态测试 MUST 确认它不引用 `MonoBehaviour`、`Transform`、`Animator`、`CharacterController`、`InputAction`、GraphView、`TreeRunner` 或 `TimelinePlayer`
- **AND** MUST 确认它不调用 output applier、motion executor、animation presenter 或 blackboard writer
