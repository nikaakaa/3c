## MODIFIED Requirements

### Requirement: TimelineNode 生命周期映射 Timeline 播放

系统 MUST 将 `TimelineNode` 的 `RunnableNode` 生命周期映射到 Timeline 逻辑播放请求生命周期。`TimelineNode` MUST 使用所属 `BaseGraph.User` 中的正式管线上下文提交、查询和取消请求，并让请求捕获当前正式 animation owner scope。节点逻辑请求和统一动画 Registry 中的 owner-scoped contribution 生命周期 MUST 明确分离。`TimelineNode` MUST NOT 自己实例化 runtime Timeline、绑定旧播放器、调用 `Timeline.Evaluate(deltaTime)`、评估 `PlayableGraph` 或直接释放 Registry entries。

#### Scenario: 开始播放

- **WHEN** `TimelineNode` 第一次被 tick
- **THEN** 节点 MUST 使用引用的 Timeline 资产提交一个独立播放请求
- **AND** 节点 MUST 保存该请求的稳定 handle
- **AND** 请求 MUST 通过正式上下文捕获当前 animation owner scope
- **AND** 节点 MUST NOT 在自身内部创建 runtime Timeline 实例

#### Scenario: 持续播放

- **WHEN** `TimelineNode` 处于 Running
- **THEN** 节点 MUST 通过请求 handle 查询管线维护的逻辑播放状态
- **AND** 节点 MUST 根据状态返回 `Running`、`Success` 或 `Failure`
- **AND** 节点 MUST NOT 直接推进 Timeline 时间或操作统一 Registry

#### Scenario: Timeline 逻辑播放成功

- **WHEN** Timeline playback request 返回 Succeeded
- **THEN** `TimelineNode` MUST 返回 Success
- **AND** 该 Success MUST NOT 直接删除仍归当前 state activation owner 所有的 CompletedHeld contribution
- **AND** contribution 的退场 MUST 由 owner transition、standalone owner release 或 pipeline dispose 决定

#### Scenario: 停止或重置未完成请求

- **WHEN** `TimelineNode` 在逻辑播放尚未完成时被停止或 reset
- **THEN** 节点 MUST 通过正式管线上下文取消未完成请求
- **AND** 节点 MUST 清理自己的逻辑请求 handle
- **AND** state-owned 当前合法表现 sample 是否保持到 transition MUST 由统一 lifecycle 合同决定

#### Scenario: 独立 TimelineNode 被释放

- **WHEN** 不属于 State activation 的 TimelineNode 被停止、reset 或 dispose
- **THEN** 正式上下文 MUST 释放其 standalone playback owner
- **AND** 节点 MUST NOT 通过 Presenter fallback 或超时回收隐藏该释放
