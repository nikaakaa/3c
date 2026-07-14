## MODIFIED Requirements

### Requirement: TimelineNode 是普通可执行节点
系统 MUST 提供 `TimelineNode : RunnableNode` 作为 Graph 中请求播放 Timeline 的节点。`TimelineNode` MUST 通过 `TimelineReferenceModule` 引用 Timeline 资产，并可在状态行为 `SubTree` 中创建。`TimelineNode` MUST 只暴露输入控制流 port，不得暴露、持久化或解析输出控制流 port。`TimelineNode` MUST NOT 直接成为 Timeline 播放器，也 MUST NOT 新增 `TimelineStateNode` 或其它特化状态节点。

#### Scenario: 状态行为请求播放 Timeline
- **WHEN** 用户在 `StateNode` 引用的 `SubTree` 或 `StateBehaviorSubTree` 中创建 Timeline 节点
- **THEN** 创建结果 MUST 是 `TimelineNode`
- **AND** 节点 MUST 通过正式管线上下文提交 Timeline 播放请求
- **AND** 节点 MUST 只接受父级行为图输入控制流
- **AND** 系统 MUST NOT 创建 `TimelineStateNode`

#### Scenario: Timeline 播放完成
- **WHEN** `TimelineNode` 引用的 Timeline 播放请求返回成功
- **THEN** `TimelineNode` MUST 返回 `Success`
- **AND** `TimelineNode` MUST NOT tick 子节点或输出控制流目标
