## MODIFIED Requirements

### Requirement: 不得恢复Timeline或Preview分裂路径

系统 MUST只有一条直接消费 TimelineData 的正式 Timeline Runtime 路径和一条原生 FlowCanvas Pose 运行路径，两者只通过 committed Body/Intent、EventId 与有限 Action command 连接。完整角色预览 MUST在独立场景 Play 中沿 C# 控制、唯一 Action 服务、ActionInstance 内 Skill Root/Tree/Timeline/TreeClip 和同一正式 Session/Pipeline 产生结果；控制与技能遵守同 Tick 的 Decision候选、Evaluate/WorldResolve/Finalize 和 Commit 边界。动画 MUST继续消费该正式链，编辑器只提供明确输入、作者参数修改和只读观察。系统 MUST删除被替代的独立 Action/Fact/Query 预览 Runtime，不得保留旧 TimelinePlaybackScheduler、Timeline Bind/Evaluate/Unbind、自主 TreeClip runtime、动画反推 root motion、Animancer direct Play 或另一 PlayableGraph。

#### Scenario: Runtime与Preview并存

- **WHEN** 预览场景中的 Corin 正式执行 Attack，Timeline 和 Pose Graph 窗口同时观察
- **THEN** Gameplay 与动画 MUST各自只由该 Actor 的正式执行链推进
- **AND** 打开额外窗口 MUST不新增采样、Gameplay 事实或物理输出

### Requirement: Timeline逻辑采样与表现采样必须分离

Gameplay Timeline sampling MUST 只按 SimulationTick/canonical fraction 发生；Action visual sampling、Presentation TreeClip 和 state-local Pose sampling MUST 只按 PresentationFrame 发生。多个 PresentationFrame MUST NOT 重发逻辑域 TreeClip、Motion、ActionWindow、Cue fact 或 Gameplay Effect mutation。Presentation sample MUST NOT 推进 CharacterSimulationState 的 Timeline clock。

#### Scenario: 两个逻辑Tick之间多次渲染

- **WHEN** 多个表现帧采样同一逻辑提交区间
- **THEN** Pose 和表现域 TreeClip MAY 使用该帧只读表现事实更新
- **AND** Gameplay state 与逻辑事实 MUST 保持不变

### Requirement: Timeline Track / Clip 执行域必须分离

Timeline Track MUST 唯一声明 Logic 或 Presentation；Clip 与同级 Marker MUST 继承所在 Track。Logic MUST 在 SimulationTick 消费逻辑进度并遵守 Commit/Discard；Presentation MUST 在表现帧消费正式表现采样并只产生表现结果。两类轨道 MUST 共用正式播放管理者提供的动作进度，不得创建自主时钟。Presentation TimelineBody MUST 只绑定表现允许的节点、只读事实与表现输出；MUST NOT 执行逻辑域副作用或写 Gameplay fact。

#### Scenario: 表现域 TreeClip 主动退出

- **WHEN** TreeDecision 片段的表现图请求结束当前片段
- **THEN** 原表现驱动 MUST 在当前候选帧执行退出并回收活跃输出
- **AND** 接受和丢弃 MUST 同时作用于片段退出状态与表现输出
- **AND** Logic TimelineBody 的退出与 Gameplay 提交 MUST 保持独立

#### Scenario: Marker 触发表现事件

- **WHEN** 表现游标跨过 Presentation Marker
- **THEN** 当前表现候选帧 MUST 执行一次触发图
- **AND** Marker MUST NOT 持有动态区间或结束另一个 TreeClip
