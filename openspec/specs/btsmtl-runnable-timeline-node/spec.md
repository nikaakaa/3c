# btsmtl-runnable-timeline-node Specification

## Purpose
定义 Graph 驱动 Timeline 的正式节点链路：`TimelineNode : RunnableNode` 默认拥有 inline `TimelineData`，仅在作者显式选择复用时引用 shared `TimelineAsset`；运行上下文来自所属 `BaseGraph`，不新增 Timeline 状态节点、场景对象 fallback 或并行端口协议。
## Requirements
### Requirement: Timeline 资产不承载节点生命周期

系统 MUST 将 Timeline authoring data 与 Unity asset 身份分离。普通 C# 可序列化 `TimelineData` MUST 作为 tracks、clips、scale 和采样结构的唯一数据模型；`TimelineAsset` MUST 只作为显式 shared 复用和 Project 直接打开的 ScriptableObject 外壳持有一份 TimelineData。TimelineData 与 TimelineAsset MUST NOT 继承 `RunnableNode`，也 MUST NOT 直接承担 Graph 节点生命周期。

#### Scenario: Graph 播放 inline Timeline

- **WHEN** Graph 中的 TimelineNode 使用默认 Inline ownership
- **THEN** TimelineNode MUST 解析自己持有的 TimelineData 并提交正式播放请求
- **AND** 系统 MUST NOT 要求作者先创建 TimelineAsset
- **AND** Graph MUST NOT 直接 tick authoring TimelineData

#### Scenario: Graph 播放 shared Timeline

- **WHEN** TimelineNode 显式选择 Shared Asset ownership
- **THEN** resolved TimelineData MUST 来自 TimelineAsset 持有的数据
- **AND** TimelineNode MUST NOT 同时保留一份生效的 inline TimelineData
- **AND** Graph MUST NOT 直接 tick TimelineAsset

### Requirement: TimelineNode 是普通可执行节点

系统 MUST 提供 `TimelineNode : RunnableNode` 作为 Graph 中请求播放 Timeline 的节点。TimelineNode MUST 通过正式 Timeline ownership module 默认持有可立即编辑的 inline TimelineData，并 MAY 显式切换为 shared TimelineAsset。TimelineNode MUST 只暴露输入控制流 port，不得暴露、持久化或解析输出控制流 port。TimelineNode MUST NOT 直接成为 Timeline 播放器，也 MUST NOT 新增 TimelineStateNode 或其它特化状态节点。

#### Scenario: 创建 TimelineNode

- **WHEN** 用户在 StateNode 的状态行为 Graph 中创建 TimelineNode
- **THEN** 创建结果 MUST 自动拥有一份 inline TimelineData
- **AND** 用户 MUST 能立即下钻编辑 tracks、clips 和 TreeClip
- **AND** 创建流程 MUST NOT要求 TimelineAsset 已存在

#### Scenario: 显式复用 Timeline

- **WHEN** 用户对 inline Timeline 执行 Extract Shared 或选择已有 TimelineAsset
- **THEN** TimelineNode MUST 切换为 Shared Asset ownership
- **AND** owner 内的 inline 真数据 MUST 被清理
- **AND** UI MUST 明确显示 Shared Asset

#### Scenario: Timeline 播放完成

- **WHEN** TimelineNode resolved TimelineData 的播放请求返回成功
- **THEN** TimelineNode MUST 返回 Success
- **AND** TimelineNode MUST NOT tick 子节点或输出控制流目标

### Requirement: TimelineNode 生命周期映射 Timeline 播放

TimelineNode MUST编译为 Runnable operation 与 Timeline playback data。Enter、play/update、loop、complete、stop 和 release MUST映射到 CharacterSimulationState slot，MUST不由 TimelineNode 或 TimelineRunningTree clone 持有。

#### Scenario: TimelineNode 完成

- **WHEN** compiled Timeline 到达请求终点且 commit lifecycle 完成
- **THEN** Kernel MUST从 state slot 产生 Runnable completion

### Requirement: Timeline 请求入口来自正式执行上下文

Compiled TimelineNode operation MUST通过 Operation Execution Context 创建、查询、停止和释放当前 activation 的 Timeline request/state slots。Operation MUST不访问旧播放器、TimelinePlaybackScheduler gameplay runtime、Presentation adapter、scene component 或全局 service。

#### Scenario: Operation 创建 Timeline request

- **WHEN** TimelineNode operation 首次进入
- **THEN** MUST在当前 CharacterSimulationState activation slots 创建 request

#### Scenario: Program 缺失 Timeline 数据

- **WHEN** operation 引用的 compiled Timeline data 不存在
- **THEN** Program build 或 runtime MUST明确失败
- **AND** MUST不搜索 TimelineAsset fallback

### Requirement: TimelineNode 播放状态隔离

每个 Actor、Graph activation、Timeline activation 和 loop cycle MUST使用独立 compiled identity/state slot。Shared Timeline authoring MUST不导致不同角色或不同 activation 共享播放状态。

#### Scenario: 两个角色播放同一 Timeline

- **WHEN** 两个 Actor 使用同一 Program 中的 Timeline data
- **THEN** 它们 MUST使用各自 CharacterSimulationState 中的 playback slot

### Requirement: 保留 Timeline 驱动 Tree 链路

TreeTrack/TreeClip MUST编译为 Timeline decision/commit operation。Decision MUST在 RootTree operation 前只写 Frame Blackboard，Commit MUST在 RootTree operation 后执行 Enter/Update/Exit/Destroy 生命周期。系统 MUST不恢复 Timeline.Bind/Evaluate/Unbind 自主播放路径。

#### Scenario: Decision TreeClip 穿过 Loop 边界

- **WHEN** 一个 SimulationTick 穿过 Timeline loop 边界
- **THEN** compiled evaluator MUST按尾段、中间 cycle 和头段顺序求值
- **AND** Frame Blackboard MUST保持唯一结果

### Requirement: TimelineNode 不参与状态机同层状态
系统 MUST 保持 `TimelineNode` 为状态内部行为节点。`TimelineNode` MUST NOT 被解释为 `StateMachineGraph` 的同层 State，也 MUST NOT 成为 Transition 端点。

#### Scenario: Idle 状态播放 Timeline
- **WHEN** Idle `StateNode` 需要播放 Timeline
- **THEN** 用户 MUST 在 Idle 引用的状态行为 `SubTree` 中创建 `TimelineNode`
- **AND** 状态机同层 Transition MUST 仍只连接控制节点、`StateNode` 和 `Exit`

### Requirement: Timeline 动作事实必须来自 Timeline 轨道采样

Compiled Timeline gameplay segment MUST产生 ActionWindow、MotionContribution 和 typed facts。Animation/Cue resource MUST通过 Presentation command 与 Projection 定位，MUST不进入 gameplay state。

#### Scenario: Attack Cancel Window

- **WHEN** compiled Decision TreeClip 命中 Cancel Window segment
- **THEN** MUST写入正式 Frame Blackboard declaration 并投影 ActionWindow fact

### Requirement: TimelineNode 完成状态必须保持请求语义

Compiled TimelineNode MUST根据当前 request slot 的 Running、Succeeded、Cancelled 或 Failed 状态更新 Runnable lifecycle。它 MUST不直接驱动 StateMachine transition，也 MUST不在自身解释 Action lifecycle；自然完成、graceful stop 与 ForceStop MUST继续使用统一 Runnable stop 语义。

#### Scenario: Once Timeline 完成

- **WHEN** request 到达 Succeeded
- **THEN** TimelineNode operation MUST返回 Success
- **AND** State transition MUST由 ConditionRuleGraph决定

#### Scenario: 播放中的 Action Context 终止

- **WHEN** 带 Action Context 的 Running Timeline 所保留 ActionInstance 已终止或被同 Context 的新实例替换
- **THEN** Kernel MUST在 Decision 或 Commit 采样前将该 request 进入 `ActionContextEnded` 停止
- **AND** MUST通过统一 Timeline/TreeClip stop barrier 释放 producer 与 camera
- **AND** MUST不再产出旧实例的 window、motion、cue、result 或其它 gameplay fact
- **AND** stop barrier 完成后 TimelineNode MUST进入 Runnable 完成终态，使 State transition 仍只由 ConditionRuleGraph决定
- **AND** Timeline MUST不提交、补写或推导 Action lifecycle

#### Scenario: ForceStop

- **WHEN** Session/Actor dispose 强制停止 active Timeline
- **THEN** operation MUST立即释放 request/state slots
- **AND** MUST不等待动画 fade 或网络确认

### Requirement: TimelineNode 播放模式必须属于请求语义

TimelineNode authoring MUST继续保存 Once/Loop mode，Compiler MUST将 mode 写入 Timeline operation data。Once 到达 duration MUST产生 Succeeded；Loop MUST在同一 request/activation slots 中回绕并保持 Running。系统 MUST不要求普通 LoopNode，也 MUST不在 runtime 注入默认 duration。

#### Scenario: Loop Timeline 回绕

- **WHEN** Loop request 到达合法 duration
- **THEN** compiled operation MUST推进 cycle 并保持 Running

#### Scenario: Loop duration 非法

- **WHEN** Loop Timeline duration 小于等于零
- **THEN** Program 编译 MUST失败
