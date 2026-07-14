## MODIFIED Requirements

### Requirement: Motion 语义使用 Contribution、Intent、Modifier 和 Result
系统 MUST 使用 `MotionContribution`、`MotionIntent`、`MotionModifier` 和 `MotionResult` 表达角色运动链路。`MotionProposal` MUST 被重命名为 `MotionIntent`，系统 MUST NOT 长期保留 `MotionProposal` 兼容别名或并行字段。

#### Scenario: 上游提交运动来源
- **WHEN** Timeline、Graph、输入移动、gameplay result 或 network correction 产生运动来源
- **THEN** 该来源 MUST 进入 `MotionContribution`、modifier 数据或 runtime context data
- **AND** 它 MUST NOT 直接调用 `CharacterController.Move` 或修改 Transform

#### Scenario: MotionStage 生成最终意图
- **WHEN** MotionStage 执行本帧运动结算
- **THEN** 它 MUST 将所有正式 motion 来源合成为 `MotionIntent`
- **AND** `MotionIntent` MUST 表达 Move 前的最终 displacement、velocity 和 yaw intent

### Requirement: MotionModifier 来源可以来自 Timeline、Action、World、Combat 或 Network
系统 MUST 允许 motion modifier 数据来自不同业务来源，但所有来源 MUST 汇入同一个 MotionStage 执行链路。Timeline 只表达时间窗口和采样数据；外部目标、world context data、gameplay result 和网络修正 MUST 来自 runtime context。

#### Scenario: Timeline-scoped modifier
- **WHEN** Timeline 表达某段攻击、闪避或翻越允许 motion warp
- **THEN** BTSMTL 内部 TimelinePlaybackScheduler MUST 采样并提交 motion modifier 数据或窗口
- **AND** TimelinePlaybackScheduler MUST NOT 直接改写角色 Transform

#### Scenario: 外部上下文影响 modifier
- **WHEN** motion warp 需要当前攻击目标、锁定目标、障碍物点、平台速度或服务器修正
- **THEN** MotionStage MUST 通过正式 runtime context 读取这些数据
- **AND** modifier 数据 MUST 使用 target key 或 blackboard key 引用外部上下文，而不是持有场景对象 fallback
