## MODIFIED Requirements

### Requirement: 不得恢复Timeline或Preview分裂路径

系统 MUST只有一条 Gameplay Timeline Program operation 路径和一条 Presentation Pose Plan 路径，两者只通过 committed Body/Intent、EventId 与有限 Action command 连接。完整角色预览 MUST在独立场景 Play 中沿 C# 控制、唯一 Action 服务、ActionInstance 内 Skill Root/Tree/Timeline/TreeClip 和同一正式 Session/Pipeline 产生结果；控制与技能遵守同 Tick 的 Decision候选、Evaluate/WorldResolve/Finalize 和 Commit 边界。动画 MUST继续消费该正式链，编辑器只提供明确输入、作者参数修改和只读观察。系统 MUST删除被替代的独立 Action/Fact/Query 预览 Runtime，不得保留旧 TimelinePlaybackScheduler、Timeline Bind/Evaluate/Unbind、自主 TreeClip runtime、动画反推 root motion、Animancer direct Play 或另一 PlayableGraph。

#### Scenario: Runtime与Preview并存

- **WHEN** 预览场景中的 Corin 正式执行 Attack，Timeline 和 Pose Graph 窗口同时观察
- **THEN** Gameplay 与动画 MUST各自只由该 Actor 的正式执行链推进
- **AND** 打开额外窗口 MUST不新增采样、Gameplay 事实或物理输出
