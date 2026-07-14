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

系统 MUST将 TimelineNode 的 RunnableNode 生命周期映射到 Timeline 逻辑播放请求生命周期。TimelineNode MUST使用所属 BaseGraph.User 中的正式管线上下文提交、查询和取消 resolved TimelineData 请求；请求 MUST捕获独立 playback identity 与 generation，但 MUST不捕获动画 owner scope。TimelinePlaybackScheduler MUST从 resolved authoring TimelineData 创建独立 runtime data clone；TimelineNode MUST不自己创建 runtime clone、直接推进 Timeline 时间、绑定旧播放器、评估 PlayableGraph 或释放动画播放生命周期。

#### Scenario: 开始播放 inline Timeline

- **WHEN** Inline TimelineNode 第一次被 tick
- **THEN** 节点 MUST使用自己的 resolved TimelineData 提交独立播放请求
- **AND** 节点 MUST保存该请求的稳定 handle
- **AND** scheduler MUST为请求创建隔离的 TimelineData 工作副本
- **AND** runtime MUST不修改节点内的 authoring TimelineData

#### Scenario: 开始播放 shared Timeline

- **WHEN** 两个 TimelineNode 引用同一个 shared TimelineAsset 并开始播放
- **THEN** 两个请求 MUST分别从同一 source TimelineData 创建独立工作副本
- **AND** 一个请求的 time、Track runtime、TreeClip runtime 或取消 MUST不污染另一个请求
- **AND** shared TimelineAsset MUST不保存 runtime 状态

#### Scenario: 停止或重置未完成请求

- **WHEN** TimelineNode 在逻辑播放尚未完成时被停止或 reset
- **THEN** 节点 MUST通过正式管线上下文取消未完成请求
- **AND** 节点 MUST清理自己的逻辑请求 handle
- **AND** 节点 MUST不修改 inline 或 shared authoring TimelineData

### Requirement: Timeline 请求入口来自正式执行上下文
系统 MUST NOT 让 `TimelineNode` 直接消费旧播放器或表现层 adapter。角色管线模式下，Timeline 播放请求、播放状态和动画输出 MUST 通过正式管线上下文、BTSMTL 内部 TimelinePlaybackScheduler 和 PresentationStage 传递。系统 MUST NOT 在 `TimelineNode`、`TreeRunner` 或场景对象搜索中保存 fallback 引用。

#### Scenario: 上下文提供 Timeline 请求入口
- **WHEN** `BaseGraph.User` 提供 Timeline 请求接口
- **THEN** `TimelineNode` MUST 使用该接口提交和查询播放请求
- **AND** `TimelineNode` MUST NOT 从上下文获取旧播放器或表现层 adapter 直接播放

#### Scenario: 上下文缺失 Timeline 请求入口
- **WHEN** `BaseGraph.User` 没有提供 Timeline 请求接口
- **THEN** `TimelineNode` MUST 返回 `Failure`
- **AND** 系统 MUST NOT 自动创建、`GetComponent` 查找或全局搜索播放器对象

### Requirement: TimelineNode 播放状态隔离

系统 MUST 隔离 TimelineNode 的播放请求状态。多个 TimelineNode 无论解析到各自 inline TimelineData 还是同一个 shared TimelineAsset，都 MUST 提交独立请求，并由 TimelinePlaybackScheduler 维护独立 TimelineData 工作副本与 active record。

#### Scenario: 同一个 inline TimelineNode 再次激活

- **WHEN** 同一个 TimelineNode 在后续 State activation 中再次提交自己的 inline TimelineData
- **THEN** 新请求 MUST 获得新的 playback handle 和 runtime clone
- **AND** 前一次播放的 time、cycle 和 TreeClip runtime state MUST NOT被复用

#### Scenario: 多节点复用 shared Timeline

- **WHEN** 两个 TimelineNode 显式引用同一个 shared TimelineAsset
- **THEN** 每个节点 MUST 拥有不同请求 handle
- **AND** 每个 active record MUST 拥有自己的播放时间和状态
- **AND** 一个节点的取消 MUST NOT影响另一个节点

### Requirement: 保留 Timeline 驱动 Tree 链路

系统 MUST 保留 BTSMTL `TreeTrack / TreeClip / TimelineRunningTree` 能力。TreeClip MUST 位于 resolved TimelineData 的正式 Track/Clip 集合中，默认拥有 inline TimelineRunningTree，并 MAY 显式引用 shared Tree asset。TimelineNode 只负责提交 resolved TimelineData 播放请求，不替代 Timeline 内嵌 Tree，也不得把 TreeClip 数据复制到 Graph 节点旁路。

#### Scenario: inline Timeline 中运行 TreeClip

- **WHEN** TimelineNode 的 inline TimelineData 包含 TreeClip
- **THEN** TimelinePlaybackScheduler MUST 从 runtime TimelineData clone 求值对应 TimelineRunningTree
- **AND** 该链路 MUST 使用正式 Clip runtime context
- **AND** authoring TimelineRunningTree MUST 不保存 runtime state

#### Scenario: shared Timeline 中运行 TreeClip

- **WHEN** shared TimelineAsset 的 TimelineData 包含 TreeClip
- **THEN** 每个 playback request MUST 克隆隔离的 TreeClip 与 TimelineRunningTree runtime
- **AND** TreeClip MUST NOT依赖 TimelineNode 静态 membership 推导 Action Context

### Requirement: TimelineNode 不参与状态机同层状态
系统 MUST 保持 `TimelineNode` 为状态内部行为节点。`TimelineNode` MUST NOT 被解释为 `StateMachineGraph` 的同层 State，也 MUST NOT 成为 Transition 端点。

#### Scenario: Idle 状态播放 Timeline
- **WHEN** Idle `StateNode` 需要播放 Timeline
- **THEN** 用户 MUST 在 Idle 引用的状态行为 `SubTree` 中创建 `TimelineNode`
- **AND** 状态机同层 Transition MUST 仍只连接控制节点、`StateNode` 和 `Exit`

### Requirement: Timeline 动作事实必须来自 Timeline 轨道采样

系统 MUST 让 Timeline 中具有时间范围的动作事实通过 Decision `TreeClip` 写入显式 scope variable。Timeline 时间范围 MUST 来自 TreeClip，区间逻辑 MUST 来自 inline/shared TimelineRunningTree，当前 Tick active 真值 MUST 来自 Bool Frame/Frame Pipeline Blackboard declaration。系统 MUST NOT 保留 `ActionWindowTrack`、`ActionWindowClip` 或其它与 TreeClip 并行的 Window Track。需要 ActionInstance 身份的 Window variable MUST 通过显式 ActionWindow fact projection 使用 Timeline playback request 携带的 Action Context 生成 `ActionWindowSample`；Timeline asset membership、clip membership 或 ambient active action MUST NOT 自动补齐动作归属。Timeline、TreeClip、Blackboard declaration 与 ActionProfile MUST NOT 保存完整网络策略；当前 Network Model adapter MUST 使用 ActionInstance 对应的稳定 ActionId 从 model profile 解析 effective policy。

#### Scenario: Attack1 产出 HitWindow

- **WHEN** `Attack1` 状态播放带 Action Context 的攻击 Timeline
- **AND** `Attack1Hit` Decision TreeClip 在当前目标时间 active
- **THEN** TreeClip MUST 写入 `Attack1Hit=true` 的 Bool Frame variable
- **AND** 该 declaration 的显式 ActionWindow projection MUST 生成带 ActionInstanceId 的 `ActionWindowSample`
- **AND** Window authority、history 和 replication policy MUST 从当前 Network Model profile 解析

#### Scenario: 普通 locomotion Timeline

- **WHEN** `RunLoop` 状态播放不带 Action Context 的 locomotion Timeline
- **THEN** Timeline MAY 产出 animation contribution 或 motion contribution
- **AND** Timeline MUST NOT 自动创建 ActionInstance
- **AND** ActionWindow-bound variable 缺少 Action Context 时 MUST 报告配置或运行错误，不得伪造动作归属

#### Scenario: Timeline 创建时间窗口

- **WHEN** 作者需要在 Timeline 某个帧范围发布可读条件
- **THEN** 作者 MUST 创建 Decision TreeClip 并写入显式 Blackboard declaration
- **AND** Timeline Editor MUST NOT 提供 ActionWindowTrack 或 ActionWindowClip

### Requirement: TimelineNode 完成状态必须保持请求语义

系统 MUST 保持 `TimelineNode` 通过正式 Timeline playback request 获取播放状态，并在播放成功时返回 `Success`。TimelineNode MUST NOT 直接驱动 StateMachine transition，也 MUST NOT 在自身内部解释 action lifecycle。自然播放完成、graceful stop 和 ForceStop MUST 使用 RunnableNode 的正式分层生命周期，不得共用无原因 OnStop 路径。

#### Scenario: Timeline 播放完成

- **WHEN** Timeline playback request 返回 `Succeeded`
- **THEN** TimelineNode MUST 返回 `Success`
- **AND** MUST 进入自然完成回调而不是 cancel 回调
- **AND** 状态机 transition 是否发生 MUST 由 StateMachine condition rule 决定

#### Scenario: Timeline 被 graceful stop

- **WHEN** Self、LowerPriority、Parent abort 或 State exit 请求停止正在运行的 TimelineNode
- **THEN** TimelineNode MUST 通过正式 playback request 取消未完成 Timeline
- **AND** Node stop status MUST 在取消请求建立后返回 Completed
- **AND** TimelineNode MUST NOT 提交 Action lifecycle transition

#### Scenario: Timeline 被 ForceStop

- **WHEN** Pipeline Shutdown、Dispose 或强制 Reset 释放 TimelineNode
- **THEN** TimelineNode MUST 立即取消并释放 active playback handle
- **AND** MUST NOT 等待 Timeline 完成、动画 blend 或网络确认

### Requirement: TimelineNode 播放模式必须属于请求语义

`TimelineNode` MUST 拥有正式播放模式 authoring 数据，并在提交 Timeline playback request 时携带该模式。默认模式 MUST 是 `Once`，保持现有一次性播放完成语义。循环模式 MUST 是 `Loop`，表示同一个 Timeline playback request 在 duration 边界回绕并继续运行。系统 MUST NOT 要求作者用普通 `LoopNode` 包住 `TimelineNode` 来表达 Timeline 动画循环。

#### Scenario: 一次性 Timeline 保持现有完成语义

- **WHEN** `TimelineNode` 播放模式是 `Once`
- **AND** Timeline playback request 返回 `Succeeded`
- **THEN** `TimelineNode` MUST 返回 `Success`
- **AND** 状态是否离开 MUST 继续由 StateMachine condition rule 决定

#### Scenario: 循环 Timeline 保持 Running

- **WHEN** `TimelineNode` 播放模式是 `Loop`
- **AND** Timeline playback request 到达 Timeline duration
- **THEN** request MUST 在 Timeline runtime owner 内回绕并保持 `Running`
- **AND** `TimelineNode` MUST 继续返回 `Running`
- **AND** 节点 MUST NOT 通过自身重启或普通 `LoopNode` 重启来获得下一轮播放

#### Scenario: 循环 Timeline 被状态离开取消

- **WHEN** `Loop` 模式的 `TimelineNode` 因状态离开、父级 stop 或 reset 被停止
- **THEN** 节点 MUST 通过正式播放请求入口取消对应 request
- **AND** 节点 MUST 清理自己的 request handle
- **AND** request MUST NOT 自然返回 `Succeeded`

#### Scenario: 循环 Timeline duration 非法

- **WHEN** `TimelineNode` 播放模式是 `Loop`
- **AND** 引用 Timeline 的 duration 小于等于 0
- **THEN** 系统 MUST 报告配置错误或让该播放请求失败
- **AND** 系统 MUST NOT 自动改为 `Once`、注入默认时长或创建 fallback Timeline

