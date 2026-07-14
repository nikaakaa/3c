## MODIFIED Requirements

### Requirement: BTSMTLPhase 驱动 BTSMTL RootTree 和 Timeline playback

系统 MUST 使用 `CharacterBTSMTLPhase` 作为 BT、SM 和 Timeline 的统一角色逻辑编排 phase。该 phase MUST 内部持有 `BehaviorTreeRuntime` 和 `TimelinePlaybackScheduler`，并保持 BTSMTL 节点解释链路。Timeline scheduler MUST 在 RootTree 前求值无副作用 Decision TreeClip，使其写入 Frame Blackboard；RootTree 后的统一 WindowFactProjection MUST 将显式 ActionWindow-bound 写入转换为正式 facts；Scheduler Commit MUST 再推进存活 playback 的非决策输出。系统 MUST NOT维护 ActionWindowTrack 专用预采样、timeline decision window cache 或第二个 Window reader。

#### Scenario: RootTree 每帧运行

- **WHEN** runner 调用 pipeline update phase
- **THEN** BTSMTLPhase MUST 先准备 active playback 的 Decision TreeClip Blackboard 输出
- **AND** MUST 再 tick RootTree 完成 Transition、State exit 和 lifecycle
- **AND** MUST 再投影显式 Window candidates
- **AND** MUST 最后收口 cancel 并提交存活 playback 的非决策贡献

#### Scenario: Window 触发同 Tick状态抢占

- **WHEN** active Timeline 的 Cancel Decision TreeClip 写入对应 Bool Frame variable
- **AND** ConditionRuleGraph 选择离开 source State
- **THEN** 同一 Blackboard variable MUST 在该次 Transition 和 OnExit 求值中可见
- **AND** 显式 ActionWindow projection MUST 最多提交一次 fact
- **AND** 被取消 source playback MUST NOT提交本 Tick非决策贡献

#### Scenario: Tree abort 产生 pending stop

- **WHEN** RootTree Composite 等待 child graceful stop
- **THEN** BTSMTLPhase MUST 保持按 Logic Tick推进 RootTree stopping lifecycle
- **AND** replacement child MUST NOT在 StopCompleted 前产生 Timeline request
- **AND** 已停止 source Timeline MUST NOT继续产生 motion、cue、camera 或 animation

### Requirement: Pipeline 输出事实必须继续通过 SyncFacts 边界产生

系统 MUST 保持 `CharacterPipelineOutput.SyncFacts` 作为 pipeline 输出事实边界。Blackboard variable MAY 为 Graph 提供运行时上下文；只有显式合法 fact projection 才能将当前写入转换为 Action、GameplayResult、StateEffect 或 Presentation SyncDomain output。NetworkSendStage MUST 只读取投影后的 SyncFacts，不得直接读取 Blackboard values。

#### Scenario: 投影 Action window

- **WHEN** WindowFactProjection 收到合法 ActionWindow-bound variable candidate
- **THEN** runtime MUST 生成 ActionWindowSample
- **AND** MUST 将其写入 `SyncFacts.Action.WindowSamples`
- **AND** NetworkSendStage MUST 继续从 SyncFacts 收集该事实

#### Scenario: 写入 local-only 临时值

- **WHEN** 节点写入 Projection=None 的本地 Blackboard variable
- **THEN** 该值 MUST NOT自动进入 SyncFacts
- **AND** NetworkSendStage MUST NOT因该变量存在生成 outgoing packet

#### Scenario: 缺失 projection provenance

- **WHEN** ActionWindow-bound 写入缺少显式 Action Context
- **THEN** runtime MUST 拒绝生成 ActionWindowSample 并报告原因
- **AND** MUST NOT将该写入降级为无 ActionInstance 的默认 window fact

