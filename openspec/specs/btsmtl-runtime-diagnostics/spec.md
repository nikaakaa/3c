# btsmtl-runtime-diagnostics Specification

## Purpose
TBD - created by archiving change add-btsmtl-compiled-runtime-debugging. Update Purpose after archive.
## Requirements
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

Diagnostics MUST继续使用稳定authoring source与独立runtime instance identity。通用Tree execution core MUST拥有`TreeAuthoringElementKey`；Diagnostics Source Map MUST从该core key单向生成`RuntimeSourceElementKey`。通用Runnable event MUST额外暴露stable containment route与activation generation，以区分shared Graph多引用和同一Node多次进入。Editor MUST不把route display path、asset path或runtime clone引用当authoring identity，正式Tree lifecycle与Presentation binding MUST不依赖Diagnostics类型。

#### Scenario: 同一状态重复进入

- **WHEN** 同一个 State source 在不同 activation generation 中执行
- **THEN** 两次执行 MUST 使用相同 source identity
- **AND** 两次执行 MUST 使用不同 runtime instance identity

#### Scenario: 两个角色运行同一 Definition

- **WHEN** 两个 Character runtime 使用同一 authoring source
- **THEN** Trace MUST 能按 Character runtime session 分离
- **AND** Debug Session MUST NOT 合并两个角色的 Node、Timeline 或 Blackboard 状态

#### Scenario: shared Graph双引用

- **WHEN** 两个owner运行同一个shared Graph中的同一Node
- **THEN** Trace source element MAY相同
- **AND** authoring route、parent activation与runtime instance MUST不同

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

Graph、RunnableNode、Composite、StateMachine、ConditionRuleGraph、Timeline scheduler、TreeClip、Pipeline Blackboard、Animation Playback Lifecycle 与 Animancer adapter MUST在各自正式边界发布对应 channel 事件。Graph Trace MUST观察逻辑 child 选择、Runnable result 和 stop；StateMachine Trace MUST观察 transition decision、State scope 与 barrier；Animation Trace MUST观察逻辑 selection、Timeline sample、PendingFirstSample、Current、Outgoing、Retired 和 Animancer fade。Producer MUST不为调试新增第二套 selection、Timeline 时间、播放生命周期或混合权威。

#### Scenario: 普通 Selector replacement

- **WHEN** Selector 停止旧 child 并启动 replacement child
- **THEN** Graph channel MUST显示 stop cause、source、replacement 和逻辑顺序
- **AND** Graph channel MUST不伪造动画 owner change 或 Driver

#### Scenario: State transition

- **WHEN** StateMachine 提交 edge 并激活 target StateNode
- **THEN** StateMachine channel MUST显示 condition、source scope、target scope 与 barrier
- **AND** Animation channel MUST只在逻辑层另行提交 AnimationLayerSelection 后显示选择变化

#### Scenario: 逻辑选择动画 producer

- **WHEN** 逻辑层为 Base 提交唯一 AnimationPlaybackId
- **THEN** Animation channel MUST显示 LayerId、playback generation、logic tick 与 selection source
- **AND** diagnostics MUST不比较 Priority 或推断第二个赢家

#### Scenario: Timeline clip membership 变化

- **WHEN** 正式 Timeline scheduler 进入、保持或离开 Track/Clip
- **THEN** Timeline channel MUST从 scheduler 的正式 sample/release 发布事件
- **AND** diagnostics MUST不独立重采样 Timeline

#### Scenario: Animancer 淡出完成

- **WHEN** Animancer 报告 outgoing state fade 完成
- **THEN** Animation channel MUST显示对应 producer 从 Outgoing 进入 Retired
- **AND** 该事件 MUST不反向改变 Tree 或 State 结果

### Requirement: Trace channel 必须控制调试采集成本

系统 MUST 至少提供 Graph、StateMachine、Timeline、Blackboard、Animation、Motion 和 GameplayEffect channel。未被 Live interest 或显式 Capture 请求的 channel MUST 阻止其非必要 payload 构造、source handle 解析和 diagnostics 写入，并且 MUST NOT 改变 runtime 执行结果。

#### Scenario: 关闭 Animation channel

- **WHEN** 当前 Debug Session 未启用 Animation channel
- **THEN** runtime MUST NOT 构建 Animation trace payload
- **AND** CharacterAnimationPlaybackCommandQueue、AnimationPlaybackLifecycle 和 AnimancerPlaybackAdapter MUST 继续产生相同正式结果

#### Scenario: 记录 Blackboard 值

- **WHEN** Blackboard channel 启用且变量发生正式写入或清理
- **THEN** Trace MUST 使用受限结构化 debug value snapshot
- **AND** Trace MUST NOT 持有任意 gameplay object reference 或调用未知对象逻辑作为序列化 fallback

#### Scenario: 关闭 GameplayEffect channel

- **WHEN** 当前 Debug Session 未启用 GameplayEffect channel
- **THEN** runtime MUST NOT 构建 tag、attribute、effect lifecycle 或 prediction journal trace payload
- **AND** Gameplay Effect MUST 继续产生相同 tag、attribute、effect 和 sync fact 结果

#### Scenario: 记录 Effect 生命周期

- **WHEN** GameplayEffect channel 启用且 effect 被应用、叠层、抑制、到期或移除
- **THEN** Trace MUST 使用稳定 effect identity、instance identity、context、logic tick 和结构化结果
- **AND** Trace MUST NOT 持有 Effect asset、component asset 或 active runtime object reference

### Requirement: 每个 runtime target 必须拥有按需 Live State 与显式 Capture

每个 Character runtime diagnostics target MUST 注册 metadata、program revision、Source Map 与默认 `None` 的 diagnostics store。Live State MUST 只保存稳定键对应的当前事实；只有作者显式开始 Capture 时才创建独立有界 Capture segment store。Capture 达到容量后 MUST 按完整 tick/frame segment 丢弃最旧数据。target 结束时 runtime MUST 释放 store；Editor MUST 只保留已冻结的 current state 或 Capture snapshot，不得继续持有 runtime target 或可写 store。

#### Scenario: 没有观察者

- **WHEN** target 没有有效 Live interest 且没有 active Capture
- **THEN** effective channel MUST 为 `None`
- **AND** runtime MUST NOT 因 diagnostics 构造高频 Graph、Timeline、Animation、Motion 或 Blackboard payload

#### Scenario: Capture 达到容量

- **WHEN** 新事件进入已经达到容量的 Capture segment store
- **THEN** store MUST 丢弃最旧完整 frame 或 tick segment
- **AND** MUST NOT 留下无法重建的半个 segment
- **AND** gameplay runtime MUST 继续执行

#### Scenario: runtime target 销毁

- **WHEN** CharacterPipeline deactivate 或 dispose
- **THEN** diagnostics store MUST 失效全部 interest 并发布 target lifecycle 终止
- **AND** editor Session MUST 冻结最后一个 source-mapped current state 和 active Capture
- **AND** Ended view MUST 不接收新事件或持有 runtime store

### Requirement: RuntimeDebugSession 必须统一目标、interest、Capture 与只读视图

Editor MUST 使用唯一 `RuntimeDebugSession` 或等价 service 管理 registered target、显式 target、target-level Live interest、共享 provider、Capture 开始/停止、Capture history position 与只读 view。Graph、Timeline 和 Host Inspector MUST 消费该 Session 的同一 target/current state/Capture history，不得各自扫描 runtime service、持有 runtime clone 或重建第二份 diagnostics 数据。

Session MUST NOT 保存全局 runtime instance、全局 Follow 或全局 Pin。Graph、Timeline 等观察页面 MUST 通过各自的 editor-only view binding 保存 source、Follow / Pin 与 runtime instance。

#### Scenario: 选择目标角色

- **WHEN** 用户在 Debug Session 中显式选择一个已注册 Character runtime target
- **THEN** Graph、Timeline 和 Host Inspector MUST 观察同一 target
- **AND** 系统 MUST NOT 通过场景搜索自动选择 fallback target

#### Scenario: Graph 与 Timeline 同时观察

- **WHEN** 同一 Character target 的 Graph 窗口 Follow 一个 Graph，Timeline 窗口 Follow 一个 Timeline playback
- **THEN** 两个窗口 MUST 使用同一 target，并由各自 interest 汇总成 target channel 并集
- **AND** 两个窗口 MUST 保持各自 runtime instance，不得互相覆盖 Follow 或 Pin
- **AND** 两个 overlay MUST 只读取同一 shared provider current state 或同一 Capture history position

#### Scenario: target 结束后查看最后状态

- **WHEN** 已附着 target 注销
- **THEN** Session MUST 标记该 snapshot 为 Ended
- **AND** Graph 与 Timeline MAY 继续显示最后一次 source-mapped overlay
- **AND** 作者显式附着新 target 或清除 Session 前，Ended snapshot MUST 保持只读

### Requirement: Debug Target 自动附着必须基于显式角色或唯一精确匹配

Graph 或 Timeline 进入 Live Debug 时 MUST 用当前 source identity 与 content hash 解析 target。场景选择包含 CharacterPipelineHost 或其子对象时，该 Host MUST 被视为作者的显式 target 意图。没有显式 Host 时，系统只可在唯一 registered target 的 Source Map 包含当前 source 且 content hash 精确匹配时自动附着。

系统 MUST NOT 按 target 注册顺序、显示名称、场景遍历顺序、Graph 名称、Timeline 名称、asset path 或近似 source path 自动选择 target。

#### Scenario: 场景中显式选择角色

- **WHEN** 作者选择 CharacterPipelineHost 或其子对象并进入当前 Graph 或 Timeline 的 Live Debug
- **THEN** 系统 MUST 尝试附着该 Host 对应的 registered target
- **AND** source map 与 content hash 精确匹配时 MUST 附着该 target
- **AND** 不匹配或未注册时 MUST 显示明确原因
- **AND** 系统 MUST NOT 改选另一个角色

#### Scenario: 没有显式 Host 且只有一个匹配角色

- **WHEN** 场景选择不包含 Host，且恰有一个 registered target 与当前 source identity/content hash 精确匹配
- **THEN** Session MUST 自动附着该 target
- **AND** UI MUST 显示该 target 是唯一精确匹配结果

#### Scenario: 多个匹配角色

- **WHEN** 场景选择不包含 Host，且多个 registered target 与当前 source 精确匹配
- **THEN** Session MUST NOT 自动选择其中任意一个
- **AND** UI MUST 显示候选 target 并等待作者显式选择

#### Scenario: source revision 不一致

- **WHEN** 已选择 target 不包含当前 source 或 Source Map content hash 与当前作者内容不同
- **THEN** overlay MUST 停止绘制该 source
- **AND** UI MUST 分别显示 source 缺失或 revision mismatch
- **AND** 系统 MUST NOT 使用名称、index 或近似 path fallback

### Requirement: 每个 Live Debug 视图必须拥有本地 runtime instance binding

每个 Graph 或 Timeline Live Debug 页面 MUST 持有 editor-only 的 source binding。binding MUST 只在该页面内保存 Follow 或 Pin instance，并从共享 Session provider 的 current state 或 Capture history view 解析正式 runtime instance。binding MUST NOT 写入 authoring asset、runtime target 或其它视图的 selection。

#### Scenario: 一个 Graph 多次 activation

- **WHEN** 同一 Graph source 在共享 snapshot 中有多个 State activation 或 Graph runtime instance
- **THEN** Graph 窗口 MUST 在自己的 binding 中显示 Follow 或可 Pin 的实例选择
- **AND** Timeline 窗口的 playback binding MUST 不被改变

#### Scenario: 一个 Timeline 多次 playback

- **WHEN** 同一 Timeline source 在共享 snapshot 中有多个 Timeline playback instance
- **THEN** Timeline 窗口 MUST 在自己的 binding 中显示 Follow 或可 Pin 的 playback 选择
- **AND** Graph 窗口的 instance binding MUST 不被改变

### Requirement: Diagnostics 必须保持只读且不影响结果

Runtime diagnostics、Debug Session 和 editor overlay MUST NOT 写入 Graph state、Timeline time、Blackboard、ActionRuntime、Motion、AnimationPlaybackLifecycle、Animancer state、SyncFacts 或作者资产。关闭或打开 diagnostics 后，相同输入和 tick 序列 MUST 产生相同 gameplay 与 presentation 结果。

#### Scenario: 在历史位置 scrub

- **WHEN** 用户停止 Capture 并查看旧 capture position
- **THEN** Graph 和 Timeline MUST 只显示记录状态
- **AND** runtime actor MUST 继续执行或按独立 gameplay pause 状态执行
- **AND** scrub MUST NOT 回滚 runtime

#### Scenario: 退出 Play Mode

- **WHEN** Unity 退出 Play Mode
- **THEN** 所有 Session MUST 解绑 runtime target
- **AND** authoring asset MUST 不包含 runtime overlay、selection、interest、Capture 或 trace state

