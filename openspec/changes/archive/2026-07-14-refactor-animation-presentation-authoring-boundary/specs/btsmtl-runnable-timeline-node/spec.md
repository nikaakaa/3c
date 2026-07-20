# btsmtl-runnable-timeline-node Specification

## MODIFIED Requirements

### Requirement: TimelineNode 生命周期映射 Timeline 播放

系统 MUST将 TimelineNode 的 RunnableNode 生命周期映射到 Timeline 逻辑播放请求生命周期。TimelineNode MUST使用所属 BaseGraph.User 中的正式管线上下文提交、查询和取消 resolved TimelineData 请求；请求 MUST捕获独立 playback identity 与 generation，但 MUST不捕获动画 owner scope。TimelinePlaybackScheduler MUST从 resolved authoring TimelineData 创建独立 runtime data clone；TimelineNode MUST不自己创建 runtime clone、直接推进 Timeline 时间、绑定旧播放器、评估 PlayableGraph 或释放动画播放生命周期。

#### Scenario: 开始播放 inline Timeline

- **WHEN** Inline TimelineNode 第一次被 tick
- **THEN** 节点 MUST使用自己的 resolved TimelineData 提交独立播放请求
- **AND** 节点 MUST保存该请求的稳定 handle
- **AND** scheduler MUST为请求创建隔离的 TimelineData 工作副本
- **AND** runtime MUST不修改节点内的 authoring TimelineData

#### Scenario: 开始播放 shared Timeline

- **WHEN** 两个 TimelineNode 引用同一个 shared TimelineAsset 并开始播放
- **THEN** 两个请求 MUST分别从同一 source TimelineData 创建独立工作副本
- **AND** 一个请求的 time、Track runtime、TreeClip runtime 或取消 MUST不污染另一个请求
- **AND** shared TimelineAsset MUST不保存 runtime 状态

#### Scenario: 停止或重置未完成请求

- **WHEN** TimelineNode 在逻辑播放尚未完成时被停止或 reset
- **THEN** 节点 MUST通过正式管线上下文取消未完成请求
- **AND** 节点 MUST清理自己的逻辑请求 handle
- **AND** 节点 MUST不修改 inline 或 shared authoring TimelineData
