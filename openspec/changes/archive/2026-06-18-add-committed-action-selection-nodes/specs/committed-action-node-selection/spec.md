## ADDED Requirements

### Requirement: Committed Action Selection Nodes
CommittedActionBranch MUST 支持 Selector、Condition 和 Timeline 三类最小节点或批准的等价节点，使一个 accepted committed action 能在内部根据只读上下文选择具体 timeline。该选择 MUST 发生在 Action lifecycle 已经 accepted action 之后，MUST NOT 替代 Action request / interrupt 仲裁。

#### Scenario: Selector 选择 Timeline
- **GIVEN** CommittedActionBranch root 是 selector
- **AND** 第一个 child condition 通过并指向 timeline A
- **WHEN** CommittedActionBranchEvaluator 评估 tick N
- **THEN** 它 MUST 只评估 timeline A 的输出
- **AND** MUST 返回 timeline A 对应的 CommittedActionBranchOutcome

#### Scenario: Selection 不决定请求准入
- **WHEN** CommittedActionBranch selector 评估 condition
- **THEN** 它 MUST 只决定当前 accepted action 的内部 timeline
- **AND** MUST NOT 接受或拒绝新的 action request

### Requirement: Condition 只读上下文
Action condition node MUST 只读取纯数据上下文，例如 request facts、movement intent、locomotion facts、runtime blackboard snapshot、active action id、source step 或批准的等价数据。Condition node MUST NOT 写状态、写黑板、消费输入、执行 motion 或播放 animation。

#### Scenario: Directional condition 读取移动意图
- **GIVEN** 当前 accepted action 是 Dodge
- **AND** condition 需要判断是否存在有效移动意图
- **WHEN** condition evaluator 运行
- **THEN** 它 MUST 从只读 movement / locomotion facts 判断
- **AND** MUST NOT 读取 Unity InputAction 或场景对象

#### Scenario: Condition 无副作用
- **WHEN** condition 评估失败
- **THEN** 系统 MUST NOT 因该失败写入 blackboard fact
- **AND** MUST NOT 消费 input buffer
- **AND** MUST NOT 改变 action lifecycle active state

### Requirement: Selector 评估顺序确定
Selector node MUST 按 runtime definition 中稳定 child 顺序评估，并选择第一个条件满足且可输出的 child。Selector MUST NOT 依赖非确定性集合枚举、Unity instance id 顺序或 editor view 顺序。

#### Scenario: 第一个通过 child 获胜
- **GIVEN** selector 有 child A 和 child B
- **AND** child A 与 child B 的 condition 都通过
- **WHEN** selector 评估
- **THEN** child A MUST 被选择
- **AND** child B MUST 不产生 timeline outcome

#### Scenario: 没有 child 通过
- **GIVEN** selector 的所有 child condition 都失败
- **WHEN** selector 评估
- **THEN** CommittedActionBranchOutcome MUST 不包含 timeline 输出
- **AND** MUST 包含明确 diagnostics 或等价 rejected selection result
- **AND** MUST NOT 使用隐藏 fallback timeline

### Requirement: 未选中 Timeline 不输出
未被当前 selector 选择的 TimelineNode MUST NOT 输出 motion、animation、active window fact 或 cue request。CommittedActionBranchOutcome MUST 只反映选中路径。

#### Scenario: 未选中 cue 不触发
- **GIVEN** timeline A 被选中
- **AND** timeline B 在同一 frame 有 cue clip
- **WHEN** selector 评估
- **THEN** output MUST 只包含 timeline A 的 cue request
- **AND** timeline B 的 cue request MUST NOT 出现在 outcome 中

### Requirement: Action Selection Nodes 可测试和可验证
系统 MUST 提供自动测试和静态边界验证，证明 Action selection nodes 是纯数据、确定性且不绕过角色帧管线。

#### Scenario: 自动测试覆盖选择语义
- **WHEN** 运行 Action selection EditMode 测试
- **THEN** 测试 MUST 覆盖 selector 顺序、condition true/false、未选中 timeline 不输出和无 fallback 行为

#### Scenario: 静态边界验证
- **WHEN** 检查 Action selection runtime 源码
- **THEN** 静态测试 MUST 确认它不引用 `MonoBehaviour`、`Transform`、`Animator`、`InputAction` 或 GraphView
- **AND** MUST 确认它不直接写 `CharacterRuntimeBlackboard`
