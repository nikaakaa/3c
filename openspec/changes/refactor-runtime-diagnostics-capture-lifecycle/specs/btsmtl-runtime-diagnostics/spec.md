# btsmtl-runtime-diagnostics Specification

## MODIFIED Requirements

### Requirement: Trace 必须使用结构化事件和稳定时序

每条 Capture event 和 Live State mutation MUST 携带 session、program revision、domain、logic tick 或 presentation frame、单调 sequence、runtime instance、source element、事实种类和结构化 payload。Catch-up logic ticks MUST 分别保留；Presentation state MUST NOT 冒充 logic facts。Live State 只保留当前记录，Capture event 才表达可回放的历史顺序。

#### Scenario: 单个 render frame 执行多个 logic tick

- **WHEN** TickSystem 在一个 render frame 内执行多个 catch-up logic tick
- **THEN** Live State MUST 保留每个当前事实的最新逻辑 position
- **AND** active Capture MUST 按 tick 与 sequence 保存各个边界 event 的顺序
- **AND** Presentation state MUST 不被写成 gameplay fact

#### Scenario: 表现帧采样动画

- **WHEN** PresentationFrame 计算 visual Timeline time 并生成动画计划
- **THEN** Live State MUST 将它标记为 Presentation domain 的当前状态
- **AND** 只有 Continuous Capture 才 MAY 将其写入历史
- **AND** diagnostics MUST 不因记录该状态产生新的 gameplay fact

### Requirement: Runtime producer 必须在正式生命周期边界发布 Trace

Graph、RunnableNode、Composite、StateMachine、ConditionRuleGraph、Timeline scheduler、TreeClip、Pipeline Blackboard、Animation Playback Lifecycle 与 Animancer adapter MUST 在各自正式边界发布 Live State 或 Capture facts。Producer MUST 根据 target effective interest 与 Capture detail 决定是否发布，并且 MUST 不为 diagnostics 新增第二套 selection、Timeline 时间、播放生命周期或混合权威。

默认 Capture MUST 只保留正式边界；逐 tick NodeStatus、EdgeEvaluated、ConditionGraphEvaluated、Timeline time、Animation sample/fade 和 presentation interpolation 必须分别受 Live State、Evaluation 或 Continuous Capture 控制。

#### Scenario: State transition

- **WHEN** StateMachine 提交 edge 并激活 target StateNode
- **THEN** StateMachine Live State MUST 显示 condition、source scope、target scope 与 barrier 的当前结果
- **AND** Boundary Capture MUST 保存该 transition 边界
- **AND** Animation channel MUST 只在逻辑层另行提交 AnimationLayerSelection 后显示选择变化

#### Scenario: Composite 条件持续失败

- **WHEN** Composite 在多个 logic tick 内重复判断同一 edge 条件为失败
- **THEN** Live State MAY 保留该 edge 的当前失败状态
- **AND** Boundary Capture MUST 不为每次重复失败追加 event
- **AND** Evaluation Capture 启用时才 MUST 保存每次正式 evaluation

### Requirement: Trace channel 必须控制调试采集成本

系统 MUST 至少提供 Graph、StateMachine、Timeline、Blackboard、Animation 和 Motion channel，并且 MUST 通过 target-level interest 控制 effective channel。关闭某个 channel 或没有任何 active interest 时，runtime MUST 在构造 payload 和解析 source handle 前阻止该 channel 的 diagnostics 工作，并且 MUST NOT 改变 runtime 执行结果。

#### Scenario: 没有 Animation interest

- **WHEN** 当前 target 的 effective interest 未启用 Animation channel
- **THEN** CharacterAnimationPlaybackCommandQueue、AnimationPlaybackLifecycle 和 AnimancerPlaybackAdapter MUST 不构造 Animation diagnostics payload
- **AND** 它们 MUST 继续产生相同正式结果

#### Scenario: 多个视图请求不同 channel

- **WHEN** Graph、Timeline 和 Host Inspector 对同一 target 分别声明不同 diagnostics interest
- **THEN** effective channel MUST 等于所有 active interest 的并集
- **AND** 释放其中一个 interest MUST 不关闭仍被其它视图需要的 channel

### Requirement: 每个 runtime target 必须拥有有界 Trace Buffer

每个 Character runtime diagnostics target MUST 拥有独立的按需 diagnostics store：一个仅保存当前事实的有界 Live State store，以及只在 active Capture 存在时创建的有界 Capture store。target 注册后 MUST 默认不采集；Live State store MUST 支持实时增量消费，Capture store MUST 支持容量范围内的历史回看。达到 Capture 容量后 MUST 按完整 debug segment 丢弃最旧数据，不得增长为无界列表。

target 结束时 runtime MUST 释放可写 store；Editor MUST 仅保留已经复制的不可变 Ended current state 和 Capture snapshot，不得继续持有 runtime target 或可写 store。

#### Scenario: target 只有 Live State 观察

- **WHEN** 作者打开 Graph Live Debug 但没有开始 Capture
- **THEN** target MUST 维护当前 Graph/StateMachine facts 的增量 Live State
- **AND** MUST 不创建滚动 Capture history
- **AND** Graph view MUST 能显示当前 overlay

#### Scenario: runtime target 销毁

- **WHEN** CharacterPipeline deactivate 或 dispose
- **THEN** diagnostics target MUST 终止 active interest 并释放 runtime 持有数据
- **AND** Editor Session MUST 从最后已消费的 current state 和 Capture 生成只读 Ended view
- **AND** Ended view MUST 不接收新事件或持有 runtime store

### Requirement: RuntimeDebugSession 必须统一目标、历史与只读捕获

Editor MUST 使用唯一 RuntimeDebugSession 或等价 service 管理 registered target、显式 target、target-level interest、共享 incremental provider、Live/Frozen/Capture/Ended 状态和 Capture history position。Graph、Timeline 和 Host Inspector MUST 消费该 Session 的同一 target/provider；它们 MUST NOT 各自扫描 runtime service、持有 runtime clone、重建第二份 Trace 或创建平行 Capture。

Session MUST NOT 保存全局 runtime instance、全局 Follow 或全局 Pin。Graph、Timeline 等观察页面 MUST 通过各自的 editor-only view binding 保存 source、Follow / Pin 与 runtime instance。共享的 Capture position 只在存在冻结 Capture snapshot 时有效。

#### Scenario: Graph 与 Timeline 同时观察

- **WHEN** 同一 Character target 的 Graph 窗口 Follow 一个 Graph，Timeline 窗口 Follow 一个 Timeline playback
- **THEN** 两个窗口 MUST 使用同一 target、shared provider 和 effective interest 并集
- **AND** 两个窗口 MUST 保持各自 runtime instance，不得互相覆盖 Follow 或 Pin
- **AND** 两个 overlay MUST 只读取同一 provider revision 或同一 Capture history position

#### Scenario: 查看 Capture 历史位置

- **WHEN** 作者停止 Capture 并设置 history position
- **THEN** Graph、Timeline 和 Host Inspector MUST 观察同一个冻结 Capture view
- **AND** 各窗口的本地 Pin / Follow 选择 MUST 保持各自语义
- **AND** history 操作 MUST NOT 回滚 runtime actor

### Requirement: Diagnostics 必须保持只读且不影响结果

Runtime diagnostics、interest、Live State、Capture、shared provider 和 editor overlay MUST NOT 写入 Graph state、Timeline time、Blackboard、ActionRuntime、Motion、AnimationPlaybackLifecycle、Animancer state、SyncFacts 或作者资产。启用或关闭任意 diagnostics interest 后，相同输入和 tick 序列 MUST 产生相同 gameplay 与 presentation 结果。

#### Scenario: 没有 diagnostics interest

- **WHEN** Play Mode 中没有有效 diagnostics interest
- **THEN** runtime MUST 不保存 diagnostics payload 或 Capture history
- **AND** CharacterPipeline 的 gameplay、motion、Timeline 和 Animation 输出 MUST 与启用前一致

#### Scenario: 在 Capture 历史位置 scrub

- **WHEN** 用户查看冻结 Capture 的旧位置
- **THEN** Graph 和 Timeline MUST 只显示记录状态
- **AND** runtime actor MUST 继续执行或按独立 gameplay pause 状态执行
- **AND** scrub MUST 不回滚 runtime
