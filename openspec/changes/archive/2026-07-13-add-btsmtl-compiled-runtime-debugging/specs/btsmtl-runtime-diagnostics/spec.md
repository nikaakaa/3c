# btsmtl-runtime-diagnostics Specification

## Purpose

定义 BTSMTL 编译无关运行时调试合同：稳定 source identity、runtime instance identity、Debug Source Map、结构化 Trace、channel、有界历史、Debug Session 和 editor-only 只读投影。

## ADDED Requirements

### Requirement: Runtime diagnostics 必须与执行对象布局解耦

系统 MUST 让运行时调试只依赖稳定 source identity、runtime instance identity、Debug Source Map 和结构化 Trace。Editor MUST NOT 通过持有、反射或轮询 runtime `BaseGraph`、`BaseNode`、Track、Clip 或未来 compiled instruction 对象来表达正式调试状态。

#### Scenario: 当前解释执行器运行 Graph

- **WHEN** 当前 Graph runtime 从 authoring data 创建工作副本并执行
- **THEN** runtime MUST 通过正式 Trace contract 发布执行事件
- **AND** Graph editor MUST 通过 source identity 映射事件
- **AND** Graph editor MUST NOT 直接绑定 runtime clone

#### Scenario: 未来编译执行器替换解释执行

- **WHEN** 后续 compiler 将相同 authoring source 编译为其它 runtime IR
- **THEN** compiled executor MUST 继续发布同一种 Trace contract
- **AND** editor diagnostics MUST NOT 因 runtime 对象类型变化重写 Graph/Timeline view contract

### Requirement: Source identity 与 runtime instance identity 必须分离

系统 MUST 为 authoring source element 和每次 runtime execution instance 使用不同身份。Source identity MUST 在 runtime clone 与编译映射中保持；runtime instance identity MUST 区分角色、Graph instance、State activation、Timeline playback 和 TreeClip cycle。

#### Scenario: 同一状态重复进入

- **WHEN** 同一个 State source 在不同 activation generation 中执行
- **THEN** 两次执行 MUST 使用相同 source identity
- **AND** 两次执行 MUST 使用不同 runtime instance identity

#### Scenario: 两个角色运行同一 Definition

- **WHEN** 两个 Character runtime 使用同一 authoring source
- **THEN** Trace MUST 能按 Character runtime session 分离
- **AND** Debug Session MUST NOT 合并两个角色的 Node、Timeline 或 Blackboard 状态

### Requirement: Debug Source Map 必须严格映射执行元素到 authoring source

系统 MUST 为每个 Program revision 提供只读 Debug Source Map。Source Map MUST 将 execution element handle 映射到 Graph、Node、Edge、Timeline、Track、Clip 或 declaration authoring identity，并携带 ProgramId、CompilationRevision 和 SourceContentHash。

#### Scenario: 一个 Node 编译成多个执行元素

- **WHEN** compiler 或解释执行准备阶段为同一 authoring Node 生成多个 execution elements
- **THEN** Source Map MUST 允许这些 handles 映射回同一 Node source
- **AND** editor MUST 能把聚合状态叠加到该 Node

#### Scenario: Source revision 不一致

- **WHEN** 当前 authoring source hash 与 Trace 的 Source Map 不一致
- **THEN** Debug Session MUST 停止 source overlay
- **AND** UI MUST 显示明确 revision mismatch
- **AND** 系统 MUST NOT 按名称、index 或近似 path fallback

### Requirement: Trace 必须使用结构化事件和稳定时序

每条 Trace event MUST 携带 session、program revision、domain、logic tick 或 presentation frame、单调 sequence、runtime instance、source element、event kind 和结构化 payload。Catch-up logic ticks MUST 分别保留；Presentation events MUST NOT 冒充 logic facts。

#### Scenario: 单个 render frame 执行多个 logic tick

- **WHEN** TickSystem 在一个 render frame 内执行多个 catch-up logic ticks
- **THEN** 每个 tick 的 Node、State、Timeline 和 Blackboard events MUST 保留各自 local logic tick
- **AND** Session MUST 按 tick 和 sequence 重建执行顺序

#### Scenario: 表现帧采样动画

- **WHEN** PresentationFrame 计算 visual Timeline time 并生成动画计划
- **THEN** Trace MUST 将该事件标记为 Presentation domain
- **AND** Trace MUST NOT 因记录该事件产生新的 gameplay fact

### Requirement: Runtime producer 必须在正式生命周期边界发布 Trace

Graph、StateMachine、ConditionRuleGraph、Timeline scheduler、TreeClip、Pipeline Blackboard、Animation Registry、Animation Layer Arbitrator 和 LayerRuntime MUST 在各自正式生命周期边界发布对应 channel 的事件。Animation Trace MUST 区分 ordered handoff record、causal component disposition、LayerPlan 与 playback handoff lifecycle。Producer MUST NOT 为调试新增第二套状态机、Timeline 时间、Blackboard 值、causal ledger 或动画仲裁。

#### Scenario: State transition

- **WHEN** StateMachine 请求 source exit、等待 target、提交 transition 并释放 source owner
- **THEN** StateMachine channel MUST 发布对应结构化事件
- **AND** 事件 MUST 引用正式 transition instance、source/target runtime instance 和 edge source identity

#### Scenario: Timeline clip membership 变化

- **WHEN** 正式 Timeline scheduler 进入、保持或离开 Track/Clip
- **THEN** Timeline channel MUST 从 scheduler 的正式 membership 发布事件
- **AND** diagnostics MUST NOT 独立重新采样 Timeline 来猜测 runtime membership

### Requirement: Trace channel 必须控制调试采集成本

系统 MUST 至少提供 Graph、StateMachine、Timeline、Blackboard、Animation 和 Motion channel。关闭某个 channel MUST 阻止该 channel 的非必要 payload 进入 buffer，并且 MUST NOT 改变 runtime 执行结果。

#### Scenario: 关闭 Animation channel

- **WHEN** 当前 Debug Session 未启用 Animation channel
- **THEN** runtime MUST NOT 构建 Animation trace payload
- **AND** Registry、Arbitrator、LayerRuntime 和 Presenter MUST 继续产生相同正式结果

#### Scenario: 记录 Blackboard 值

- **WHEN** Blackboard channel 启用且变量发生正式写入或清理
- **THEN** Trace MUST 使用受限结构化 debug value snapshot
- **AND** Trace MUST NOT 持有任意 gameplay object reference 或调用未知对象逻辑作为序列化 fallback

### Requirement: 每个 runtime target 必须拥有有界 Trace Buffer

每个 Character runtime diagnostics target MUST 拥有独立有界 Trace Buffer。Buffer MUST 支持实时消费、暂停观察和容量范围内的历史回看；达到容量后 MUST 按明确顺序丢弃最旧完整 debug frame，不得增长为无界列表。

#### Scenario: Buffer 达到容量

- **WHEN** 新事件进入已经达到容量的 Trace Buffer
- **THEN** Buffer MUST 丢弃最旧完整 frame 或 tick segment
- **AND** MUST NOT 留下无法重建的半个 segment
- **AND** gameplay runtime MUST 继续执行

#### Scenario: runtime target 销毁

- **WHEN** CharacterPipeline deactivate 或 dispose
- **THEN** Trace Buffer MUST 发布 target lifecycle 终止并释放持有数据
- **AND** editor Session MUST 自动进入 detached 状态

### Requirement: RuntimeDebugSession 必须统一目标、实例和历史选择

Editor MUST 使用唯一 `RuntimeDebugSession` 或等价 service 管理显式 target、runtime instance、channel、follow/pin、实时/暂停和历史位置。Graph、Timeline 和 Host Inspector MUST 消费该 Session 的同一 view model，不得各自扫描场景或读取 runtime service。

#### Scenario: 选择目标角色

- **WHEN** 用户在 Debug Session 中显式选择一个已注册 Character runtime target
- **THEN** Graph、Timeline 和 Host Inspector MUST 观察同一 target
- **AND** 系统 MUST NOT 通过场景搜索自动选择 fallback target

#### Scenario: 同一 source 有多个实例

- **WHEN** Session 中同一 authoring Graph 或 Timeline 存在多个 runtime instances
- **THEN** UI MUST 显示实例选择
- **AND** 当前 overlay MUST 明确标记 Follow Selection 或 pinned instance
- **AND** 系统 MUST NOT 静默选择第一个实例

### Requirement: Diagnostics 必须保持只读且不影响结果

Runtime diagnostics、Debug Session 和 editor overlay MUST NOT 写入 Graph state、Timeline time、Blackboard、ActionRuntime、Motion、Animation Registry、SyncFacts 或作者资产。关闭或打开 diagnostics 后，相同输入和 tick 序列 MUST 产生相同 gameplay 与 presentation 结果。

#### Scenario: 在历史位置 scrub

- **WHEN** 用户暂停 Debug Session 并查看旧 debug frame
- **THEN** Graph 和 Timeline MUST 只显示记录状态
- **AND** runtime actor MUST 继续执行或按独立 gameplay pause 状态执行
- **AND** scrub MUST NOT 回滚 runtime

#### Scenario: 退出 Play Mode

- **WHEN** Unity 退出 Play Mode
- **THEN** 所有 Session MUST 解绑 runtime target
- **AND** authoring asset MUST 不包含 runtime overlay、selection、buffer 或 trace state
