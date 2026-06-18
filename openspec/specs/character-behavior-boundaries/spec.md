# character-behavior-boundaries Specification

## Purpose
记录 Character Behavior authoring graph、runtime execution tree、behavior submission runner 与 CommittedActionBranch 的正式命名和边界，避免旧 graph、chain 或 ActionBranch 术语成为新的扩展入口。
## Requirements
### Requirement: Authoring Graph 与 Runtime Tree 命名分离
系统 MUST 区分编辑器 authoring graph 与正式 runtime execution tree。`CharacterBehaviorGraphDefinition` MUST 只用于资产、可视化编辑、节点连线和编译输入；runtime gameplay MUST 消费 `CharacterBehaviorExecutionTree` 或批准的等价编译结果。

#### Scenario: Graph 不作为 gameplay runner
- **WHEN** 检查正式角色 gameplay runtime
- **THEN** runtime MUST NOT 直接运行 GraphView 节点或 authoring graph 对象
- **AND** MUST 运行编译后的 execution tree、submission tree 或批准的等价纯数据模型

#### Scenario: Runtime Tree 不支持任意图语义
- **WHEN** 检查 runtime execution tree 合同
- **THEN** 它 MUST 表达单父、有序 child 和受控 parallel
- **AND** MUST NOT 因名称包含 graph 而支持任意循环边、共享 runtime node 或隐式合流

### Requirement: Behavior Submission Runner 不得称为 Graph 或 Chain
角色帧提交器的正式组合 MUST 使用 `CharacterBehaviorSubmissionRunner` 或已经批准的等价 behavior submission runner 名称。submitter 组合 MUST NOT 被命名为 graph 或 chain 并作为后续行为编辑器或行为 runtime 的扩展入口。

#### Scenario: Submission runner 名称反映职责
- **WHEN** Locomotion submitter 和 Action submitter 以顺序组合参与 frame pipeline
- **THEN** 该组合 MUST 被命名为 behavior submission runner 或等价职责名称
- **AND** MUST NOT 继续以 graph 名称暗示它是行为节点图
- **AND** MUST NOT 保留旧 `CharacterFrameSubmitterChain` 作为正式类型

#### Scenario: 后续行为入口不复用旧 submitter graph
- **WHEN** 新增 behavior execution tree runtime
- **THEN** 它 MUST 有自己的 runner / submitter 名称
- **AND** MUST NOT 把旧 submitter graph/chain 当成行为树 runtime

### Requirement: CommittedActionBranch 是 committed behavior 领域实现
`CommittedActionBranch` 命名 MUST 表示请求驱动、可仲裁、可中断、可完成并可能提交 body/channel claim 的 committed behavior 领域实现。系统 MUST NOT 将 Action 命名成所有 behavior 之外的顶层二分；Locomotion、Committed Action、UpperBody 和 Presentation 都是 behavior source。

#### Scenario: CommittedActionBranch 不作为顶层行为树根
- **WHEN** 检查 `CommittedActionBranch` 或等价类型
- **THEN** 它 MUST 位于 Action / Committed behavior module 内
- **AND** MUST NOT 被文档或代码作为 Character behavior tree 的唯一根节点

#### Scenario: Locomotion 仍是 behavior
- **WHEN** 统一行为提交树评估本帧行为
- **THEN** Locomotion MUST 作为 behavior leaf、subtree 或等价 behavior source 参与提交
- **AND** MUST NOT 被定义成 behavior tree 之外的特殊例外

### Requirement: Rename 过程保留兼容验证
命名收束 MUST 保留运行行为等价，并通过自动测试证明没有新增第二 pipeline、第二 runner、第二 motion executor、第二 animation presenter 或第二 blackboard write path。

#### Scenario: Rename 不改变帧输出
- **WHEN** 执行 Graph/Tree/Submitter 命名迁移
- **THEN** Locomotion 与 Dodge 的定向测试输出 MUST 保持等价
- **AND** pipeline phase 顺序 MUST 保持不变

#### Scenario: 旧名称不作为新扩展入口
- **WHEN** 检查新增代码和测试 fixture
- **THEN** 它们 MUST NOT 以旧 `SubmitterGraph` 或等价误导命名注册新行为域
- **AND** 旧名称若存在 MUST 标注为迁移 adapter 或被删除
