## MODIFIED Requirements

### Requirement: 保留 Timeline 驱动 Tree 链路

系统 MUST 保留 BTSMTL `TreeTrack`、`TreeClip` 和 `TimelineRunningTree` 能力，但角色管线模式下该能力 MUST 由 `TimelinePlaybackScheduler` 唯一推进。TreeClip MUST 显式声明 `Decision` 或 `Commit` 执行阶段。系统 MUST NOT 通过恢复 `TimelineNode -> Timeline.Evaluate()`、TimelinePlayer autonomous tick 或第二套播放器来运行 TreeClip。

#### Scenario: 新建 TreeClip

- **WHEN** 作者在 Timeline 中创建 TreeClip
- **THEN** Clip 执行阶段 MUST 默认为 `Commit`
- **AND** 作者必须显式选择 `Decision` 才能让该 Tree 在 Prepare 阶段执行

#### Scenario: Decision TreeClip 驱动当前 Tick决策

- **WHEN** active Timeline 当前目标时间落在 Decision TreeClip 范围内
- **THEN** Scheduler MUST 在 RootTree 和 StateMachine 求值前执行该 TimelineRunningTree 一次
- **AND** Decision Tree MUST 只能产生声明式 Pipeline Blackboard 决策输出
- **AND** 输出 MUST 在同一 logic tick 对 ConditionRuleGraph 可见

#### Scenario: Commit TreeClip 持续运行

- **WHEN** retained active Timeline 当前时间落在 Commit TreeClip 范围内
- **THEN** Scheduler MUST 在 RootTree 决策后推进该 TimelineRunningTree
- **AND** Tree runtime MUST 保持 Enter、Update、Exit、Destroy 和 stop 生命周期
- **AND** Commit 输出 MUST NOT 反向改变已经完成的同 Tick Transition

#### Scenario: Timeline 取消 TreeClip

- **WHEN** TimelineNode 因 State exit、Tree abort、reset 或 ForceStop 取消 playback
- **THEN** Scheduler MUST 停止并释放该 playback 拥有的 Tree runtime
- **AND** 被取消的 Commit Tree MUST NOT 继续提交非决策输出
- **AND** 系统 MUST NOT 自动提交 Action lifecycle transition

#### Scenario: Commit TreeClip 自然停止

- **WHEN** Commit TreeClip 自然离开时间范围
- **THEN** Scheduler MUST 请求该 Tree runtime graceful stop
- **AND** Once Timeline MUST 等待自然 stopping runtime 完成后再写回 Succeeded
- **AND** 系统 MUST NOT 使用超时成功或旧 Timeline Evaluate fallback

#### Scenario: Tree runtime 获得正式上下文

- **WHEN** Scheduler 创建 TimelineRunningTree 工作副本
- **THEN** Graph User MUST 是正式管线上下文
- **AND** clip time、cycle、playback 和 owner MUST 通过独立 Clip runtime context 提供
- **AND** 系统 MUST NOT 将 TreeClip 自身作为 Graph User fallback
