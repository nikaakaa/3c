# btsmtl-runtime-diagnostics Specification

## Purpose
定义 Authoring identity、Program source map、Simulation/Presentation Trace、RuntimeDebugSession 与编辑器视图之间的只读诊断链路。
## Requirements
### Requirement: Runtime diagnostics 必须与执行对象布局解耦

Runtime diagnostics MUST只依赖稳定 source identity、Program revision、operation handle、Actor/activation identity、SimulationTick、Debug Source Map 和 structured Trace。Editor MUST不持有或轮询 Character/World state mutable view、pending evaluation、runtime clone 或 WorldSolver object。

#### Scenario: Graph Editor 跟随 Runtime

- **WHEN** Editor 显示 compiled operation 的当前状态
- **THEN** MUST通过 Source Map 和 Trace 反查 authoring element

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

Compiler MUST为 operation、state slot、scope、Timeline segment、TreeClip、Action/Effect definition 和 presentation producer 生成严格 Source Map。断裂、歧义或 duplicate identity MUST使 Program build 失败。

#### Scenario: 定位 Timeline Window

- **WHEN** Trace 包含 ActionWindow EventId
- **THEN** Source Map MUST唯一定位原 Timeline/TreeClip/declaration

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

SimulationKernel、Pipeline Runtime/Pass、Session Source、WorldSolver adapter和 Committer MUST分别在自己的正式边界发布 Trace。Trace MUST记录 BackendId、PipelineHash、PassId、product、outer source tick、内部 SimulationStep、成功、失败、restore、replay与 OutputDisposition；Egress disposition MUST不能通过 Publish、Replace、Retire或 Suppress隐藏 Trace。Trace MUST不反向驱动 Character/World/Pipeline state、Source policy或 Presentation result。

#### Scenario: 一次 Motion 执行

- **WHEN** Program Evaluate Pass生成 request且 WorldSolve Pass取得 Solver result
- **THEN** Trace MUST区分 operation、Pass、request、solver result、Finalize、published body sample与 OutputDisposition
- **AND** MUST保留当前 PipelineHash和内部 Step provenance

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

每个 Character Session diagnostics target MUST注册 metadata、Program revision、Pipeline/Backend identity、Source Map与默认 `None` 的 diagnostics store。Live State MUST只保存稳定键对应的当前事实；只有作者显式开始 Capture时才创建独立有界 Capture segment store。Capture达到容量后 MUST按完整 outer tick、SimulationStep或 presentation frame segment丢弃最旧数据。target结束时 runtime MUST释放 store；Editor MUST只保留已冻结的 current state或 Capture snapshot，不得继续持有 runtime target、Pass runtime或可写 store。

#### Scenario: Session Pipeline Runtime 结束

- **WHEN** CharacterPipelineHost deactivate或 Session runtime handle dispose
- **THEN** diagnostics store MUST失效全部 interest并发布 target lifecycle终止
- **AND** Editor Session MUST冻结最后一个 source-mapped current state、Pipeline identity和 active Capture
- **AND** Ended view MUST不接收新事件或持有 runtime store

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

Runtime diagnostics、Debug Session 和 editor overlay MUST NOT 写入 Program state、Timeline operation time、Blackboard slots、Action instance state、Motion state、AnimationPlaybackLifecycle、Animancer state、GameplayFacts 或作者资产。关闭或打开 diagnostics 后，相同输入和 tick 序列 MUST 产生相同 gameplay 与 presentation 结果。

#### Scenario: 在历史位置 scrub

- **WHEN** 用户停止 Capture 并查看旧 capture position
- **THEN** Graph 和 Timeline MUST 只显示记录状态
- **AND** runtime actor MUST 继续执行或按独立 gameplay pause 状态执行
- **AND** scrub MUST NOT 回滚 runtime

#### Scenario: 退出 Play Mode

- **WHEN** Unity 退出 Play Mode
- **THEN** 所有 Session MUST 解绑 runtime target
- **AND** authoring asset MUST 不包含 runtime overlay、selection、interest、Capture 或 trace state

