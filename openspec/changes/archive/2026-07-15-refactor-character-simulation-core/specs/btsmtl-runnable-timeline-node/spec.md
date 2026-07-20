# btsmtl-runnable-timeline-node Specification

## MODIFIED Requirements

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
