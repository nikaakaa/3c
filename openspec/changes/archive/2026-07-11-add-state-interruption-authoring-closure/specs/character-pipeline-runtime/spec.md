## MODIFIED Requirements

### Requirement: BTSMTLPhase 驱动 BTSMTL RootTree 和 Timeline playback

系统 MUST 使用 `CharacterBTSMTLPhase` 作为 BT、SM 和 Timeline 的统一角色逻辑编排 phase。该 phase MUST 内部持有 `BehaviorTreeRuntime` 和 `TimelinePlaybackScheduler`，并保持 BTSMTL 节点解释链路。Timeline scheduler MUST 将无副作用 decision facts prepare 与正式 playback commit 分开，使 RootTree 能在当前 Tick 使用 Timeline window，同时保证 graceful/force stopped branch 不提交取消后的非决策贡献。

#### Scenario: RootTree 被初始化

- **WHEN** pipeline 启动
- **THEN** BehaviorTreeRuntime MUST 从 Host 配置 RootTree 创建独立运行实例
- **AND** MUST 使用 CharacterGraphContext 初始化并 spawn 运行树

#### Scenario: RootTree 每帧运行

- **WHEN** runner 调用 pipeline update phase
- **THEN** BTSMTLPhase MUST 先准备 Tick 开始时 active playback 的无副作用决策事实
- **AND** MUST 再 tick RootTree，使 Composite stop、State exit 和 lifecycle facts 完成当前阶段推进
- **AND** MUST 最后先收口 cancel，再正式推进和提交存活 playback

#### Scenario: Tree abort 产生 pending stop

- **WHEN** RootTree Composite 等待 child graceful stop
- **THEN** BTSMTLPhase MUST 保持按 Logic Tick推进 RootTree stopping lifecycle
- **AND** replacement child MUST NOT 在 StopCompleted 前产生 Timeline request
- **AND** 已停止 source Timeline MUST NOT 继续产生 motion、cue、camera 或 animation

#### Scenario: Window 触发同 Tick状态抢占

- **WHEN** active Timeline CancelWindow 在当前决策时间段有效
- **AND** ConditionRuleGraph 选择离开 source State
- **THEN** Window MUST 在该次 Transition 求值中可见
- **AND** Window fact MUST 最多提交一次
- **AND** 被取消 source playback MUST NOT 提交本 Tick非决策贡献

#### Scenario: BTSMTLPhase 释放

- **WHEN** pipeline 被释放
- **THEN** BTSMTLPhase MUST ForceStop 并释放 active Timeline playback 和 RootTree 运行实例
- **AND** MUST NOT 等待 gameplay OnExit 或启动 replacement branch

