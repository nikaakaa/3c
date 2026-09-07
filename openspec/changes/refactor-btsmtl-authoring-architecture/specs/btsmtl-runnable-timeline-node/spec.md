## MODIFIED Requirements

### Requirement: TimelineNode 是普通可执行节点

系统 MUST 提供 `TimelineNode : RunnableNode` 作为 Graph 中请求播放 Timeline 的节点。TimelineNode MUST 通过正式 Timeline ownership module 默认持有可立即编辑的 inline TimelineData，并 MAY 显式切换为 shared TimelineAsset。TimelineNode MUST 只暴露输入控制流 port，不得暴露、持久化或解析输出控制流 port。TimelineNode MUST NOT 直接成为 Timeline 播放器，也 MUST NOT 新增 TimelineStateNode 或其它特化状态节点。

#### Scenario: 创建 TimelineNode

- **WHEN** 用户在技能Tree或技能局部State的行为Graph中创建TimelineNode
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

TimelineNode MUST编译为 Runnable operation 与 Timeline playback data。Enter、play/update、loop、complete、stop 和 release MUST映射到 所属ActionInstance的SkillExecutionState typed slot，MUST不由 TimelineNode 或 TimelineRunningTree clone 持有。

#### Scenario: TimelineNode 完成

- **WHEN** compiled Timeline 到达请求终点且 commit lifecycle 完成
- **THEN** 技能解释器 MUST从本实例state slot 产生 Runnable completion


### Requirement: Timeline 请求入口来自正式执行上下文

Compiled TimelineNode operation MUST通过 Operation Execution Context 创建、查询、停止和释放当前 activation 的 Timeline request/state slots。Operation MUST不访问旧播放器、TimelinePlaybackScheduler gameplay runtime、Presentation adapter、scene component 或全局 service。

#### Scenario: Operation 创建 Timeline request

- **WHEN** TimelineNode operation 首次进入
- **THEN** MUST在当前ActionInstance及调用点的技能activation slots 创建 request

#### Scenario: Program 缺失 Timeline 数据

- **WHEN** operation 引用的 compiled Timeline data 不存在
- **THEN** Program build 或 runtime MUST明确失败
- **AND** MUST不搜索 TimelineAsset fallback


### Requirement: TimelineNode 播放状态隔离

Actor、ActionInstance、子图调用点、Timeline activation与loop cycle MUST共同确定播放状态归属。多个调用复用同一Timeline作者数据或Skill Program时 MUST使用各自typed实例状态，不得按模板identity共用播放头。

#### Scenario: 两个角色播放同一Timeline

- **WHEN** 两个Actor使用相同技能Program
- **THEN** 其Timeline MUST位于各自角色／释放状态

#### Scenario: 同角色并发播放

- **WHEN** 同Actor的两个合法释放同时使用同Timeline模板
- **THEN** 两者进度、loop和停止状态 MUST独立并可分别恢复

#### Scenario: 两个角色播放同一 Timeline

- **WHEN** 两个 Actor 使用同一 Program 中的 Timeline data
- **THEN** 它们 MUST使用各自 CharacterSimulationState 中的 playback slot


### Requirement: 保留 Timeline 驱动 Tree 链路

TreeTrack／TreeClip MUST编译为技能Timeline的Decision／Commit内容。Decision MUST在角色代码控制决策前只写当前ActionInstance／调用frame的Frame candidate，不允许Running或副作用。Commit MUST在正式技能阶段执行Enter／Update／Exit／Destroy，父级停止需覆盖嵌套内容。MUST不恢复Timeline.Bind／Evaluate／Unbind自主Gameplay路径。

#### Scenario: Decision穿过Loop边界

- **WHEN** 一个SimulationTick穿过Timeline loop边界
- **THEN** 求值 MUST保留尾段、中间cycle和头段顺序
- **AND** 当前释放的Frame候选 MUST保持唯一且可被同Tick角色控制读取

#### Scenario: Decision TreeClip 穿过 Loop 边界

- **WHEN** 一个 SimulationTick 穿过 Timeline loop 边界
- **THEN** compiled evaluator MUST按尾段、中间 cycle 和头段顺序求值
- **AND** Frame Blackboard MUST保持唯一结果
