# btsmtl-node-interruption-lifecycle Specification

## Purpose
定义 RunnableNode 的行为结果、运行阶段、自然完成、graceful stop 和 force stop 分层协议，以及 Composite、State 和 Timeline 的统一停止传播边界。
## Requirements
### Requirement: Runnable 行为结果与停止阶段必须分离

系统 MUST 保持 `State.None/Running/Success/Failure` 只表达 Runnable 行为结果，并使用独立 runtime lifecycle phase 表达节点是否 Dormant、Active 或 Stopping。系统 MUST NOT 将 `Stopping` 增加为第五种 BT State，也 MUST NOT 把 lifecycle phase 写回 authoring graph。

#### Scenario: 节点正在异步停止

- **WHEN** RunnableNode 的 stop callback 返回 Running
- **THEN** 节点 lifecycle phase MUST 是 Stopping
- **AND** 节点行为 State MUST NOT 被父 Composite 当作新的第五种结果解释
- **AND** authoring asset MUST NOT 因 phase 变化变脏

### Requirement: RunnableNode 必须区分自然完成、graceful stop 和 ForceStop

自然 Success/Failure MUST 进入自然完成回调。Self、LowerPriority 和 Parent abort MUST 进入可等待 graceful stop。Shutdown、Dispose 和强制 Reset MUST 使用 ForceStop。系统 MUST 删除旧无原因 `StopNode/OnStop` 正式路径，不保留兼容 alias 或 fallback 调用。

#### Scenario: 节点自然成功

- **WHEN** OnUpdate 返回 Success
- **THEN** runtime MUST 调用自然完成回调
- **AND** MUST NOT 将该结果伪装为 Self/LowerPriority stop

#### Scenario: Pipeline Shutdown

- **WHEN** Pipeline 销毁运行树
- **THEN** runtime MUST 使用 ForceStop 递归释放
- **AND** MUST NOT 等待 gameplay OnExit、动画 blend 或网络确认

### Requirement: graceful stop 必须携带结构上下文并可跨 Tick

系统 MUST 通过 `NodeStopContext` 携带稳定的 OriginCause、tick、initiator、当前 source、immediate parent、propagation depth 和可选 replacement edge/node。OriginCause 从 initiator 到全部 descendants MUST 保持不变。`RequestStop` 和后续 stop update MUST 返回 `Running`、`Completed` 或 `Failed`。StopContext MUST 是 transient runtime data，不得成为黑板或网络事实。

#### Scenario: LowerPriority 请求停止低优先级 child

- **WHEN** Selector 发现高优先级 LowerPriority 条件成立
- **THEN** 当前低优先级 child MUST 收到 OriginCause 为 LowerPriorityAbort 的 StopContext
- **AND** context MUST 能关联 source child 和 replacement candidate

#### Scenario: StopContext 传播到深层 SMNode

- **WHEN** LowerPriority abort 经过 Composite 和 SubTree 传播到 StateMachineNode
- **THEN** StateMachineNode 读取的 OriginCause MUST 仍是 LowerPriorityAbort
- **AND** propagation depth 和 immediate parent MUST 反映实际传播路径
- **AND** context MUST NOT 被改写成模糊 ParentAbort

### Requirement: Composite 必须等待 pending child stop

Composite 发起 graceful stop 后，MUST 在 child 返回 Completed 前保持 pending stop。Pending 期间 MUST NOT tick 旧 child 正常逻辑，也 MUST NOT tick replacement。Completed 后 MUST 重新扫描当前条件；Failed 时 MUST 返回明确 Failure 并禁止 replacement。系统 MUST NOT ForceStop 旧 child 后静默继续。

#### Scenario: LowerPriority child 同步停止

- **WHEN** 当前 child stop request 立即 Completed
- **THEN** Selector MAY 在同 Tick重新扫描并 tick 当前最高优先级合法 child

#### Scenario: LowerPriority child 跨 Tick停止

- **WHEN** 当前 child stop request 返回 Running
- **THEN** Selector MUST 等待后续 stop update
- **AND** replacement MUST NOT 提前运行

#### Scenario: 停止期间候选条件变化

- **WHEN** stop 完成时原 replacement 条件已不成立
- **THEN** Selector MUST 重新扫描当前 slots
- **AND** MUST NOT 盲目进入旧候选

### Requirement: 容器节点必须递归传播统一 stop 协议

Composite、Decorator、Root、StateLifecycleNode、SubTreeNode 和其它拥有 active child 的 Runnable MUST 向 active descendants 传播 stop context，并等待需要跨 Tick 的 descendants。Parallel 自身停止时 MUST 等待全部 active child；未运行 child MUST NOT 执行 stop callback。

#### Scenario: Parallel 包含多个 active child

- **WHEN** Parallel 收到 ParentAbort
- **THEN** 所有 active child MUST 收到 stop request
- **AND** Parallel MUST 在全部 child Completed 后才 StopCompleted

### Requirement: Tree 调度层不得越权产生业务生命周期

通用 Tree 调度层 MUST只负责child选择、Runnable stop传播、pending stop barrier和结构执行结果。它 MUST不产生animation、camera、cue、GameplayEffect或network业务输出，也 MUST不因为表现需要改变合法Runnable result。正式Character runtime MUST由Compiler将相同interruption authoring编译为control-flow operation；Program operation在完成State/Action ownership决策后 MAY为每个AnimationChannelId输出唯一presentation producer command，但该输出不属于通用Tree scheduler生命周期，也不得通过`CharacterGraphContext`提交。

#### Scenario: Selector抢占Attack SMNode

- **WHEN** compiled LowerPriority replacement停止Attack StateMachine operation
- **THEN** Program MUST传播stop context并等待descendant stop barrier
- **AND** State/Action operation MUST在完成所有权决策后为每个受影响AnimationChannelId输出唯一producer command
- **AND** 通用Tree scheduler MUST不生成animation release、Driver、handoff record或presentation command

#### Scenario: 空StateBehaviorSubTree

- **WHEN** compiled空StateBehaviorSubTree返回合法State.None
- **THEN** control-flow operation MUST保留该逻辑结果
- **AND** 系统 MUST不因为动画表现合同把它转换成InvalidExecuted或抛出动画异常

#### Scenario: 非Character通用解释执行

- **WHEN** 其它工具显式使用通用Tree scheduler执行相同interruption authoring
- **THEN** scheduler MUST只产生结构生命周期和结果
- **AND** MUST不创建AnimationChannel command、AnimationSlot binding或表现runtime
