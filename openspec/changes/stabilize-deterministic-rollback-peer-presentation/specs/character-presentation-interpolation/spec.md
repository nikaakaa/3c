## ADDED Requirements

### Requirement: Rollback 表现必须在有界预测时间线上提交最终分支

Rollback Presentation MUST继续消费predicted current timeline，MUST不把confirmed horizon变成Body或整体动画的固定表现缓冲。Rollback Output Adapter MUST在同一outer transaction内合并同一Actor的全部replay结果，只向Body Runtime提交最终连续Body分支，并只向Action Playback Runtime提交最终Action branch revision。Prediction lead达到正式模型边界时，Schedule MUST停步而不是让Presentation Runtime消费无界领先的远端时间线。

#### Scenario: Peer 达到预测领先边界

- **WHEN** 当前Peer的completed SimulationTick达到canonical frontier加`MaximumPredictionLeadTicks`
- **THEN** PresentationFrame MAY继续采样当前已提交Body/动画表现状态
- **AND** Rollback Schedule MUST不新增forward Body/Action分支
- **AND** canonical推进后的下一次正式commit MUST从已有连续history继续

#### Scenario: 一个 outer transaction 包含多次 Action 替换

- **WHEN** replay在同一PlaybackId/generation产生多个Select、Sample、Complete或Release候选
- **THEN** Presentation Adapter MUST先合并完整outer transaction
- **AND** MUST只提交最终Action branch revision
- **AND** MUST不逐条显示replay中间动画状态

#### Scenario: 未确认 Action 分支被撤销

- **WHEN** replay撤销已经表现的未确认Select或Sample
- **THEN** Action Playback Runtime MUST按最终branch revision重基
- **AND** Body Runtime、PoseStateMachine与Presentation clock MUST不因该Action重基被整体重置
- **AND** Physical Bones MUST只在后续成功Pose Plan最终发布时写入

#### Scenario: Committed Body 分支替换保持Locomotion连续

- **WHEN** replay替换已经表现的Committed Body与Intent分支
- **THEN** Body branch sequence MUST表示新的history revision
- **AND** Presentation Fact的Pose discontinuity generation MUST保持不变
- **AND** PoseStateMachine、Sequence Player、Root Orientation Warp与Presentation clock MUST继续当前Locomotion连续状态
- **AND** Foot Placement与Motion Matching trajectory MUST只重定向到新Body分支
- **AND** 只有Initialization或显式Selected Stream Reset MAY推进Pose discontinuity generation并执行硬重置
