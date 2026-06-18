## ADDED Requirements

### Requirement: Dodge Timeline 作为运行时权威
`Action.Dodge` 的正式运行时 motion、animation key、duration frame、timeline window 和 cue request MUST 来自 selected ActionTimeline 或批准的等价 timeline definition。旧 Directional / Backstep variant 字段 MAY 作为迁移输入存在，但 MUST NOT 作为正式 runtime motion 或 animation 权威。

#### Scenario: Directional 内容来自 Timeline
- **GIVEN** Dodge selector 选择 Directional timeline
- **WHEN** CommittedActionBranchEvaluator 在 tick N 评估
- **THEN** Directional Dodge 的 motion spec MUST 来自 Directional timeline 的 motion clip
- **AND** animation key MUST 来自 Directional timeline 的 animation clip
- **AND** resolver MUST NOT 从旧 Directional variant 字段补齐 runtime motion 或 animation

#### Scenario: Backstep 内容来自 Timeline
- **GIVEN** Dodge selector 选择 Backstep timeline
- **WHEN** CommittedActionBranchEvaluator 在 tick N 评估
- **THEN** Backstep Dodge 的 motion spec MUST 来自 Backstep timeline 的 motion clip
- **AND** animation key MUST 来自 Backstep timeline 的 animation clip
- **AND** resolver MUST NOT 从旧 Backstep variant 字段补齐 runtime motion 或 animation

### Requirement: Dodge Variant Selector
`Action.Dodge` MUST 使用 Action selector / condition 或批准的等价 committed action node 选择 Directional 或 Backstep timeline。选择条件 MUST 只读取纯数据 movement intent、facing、request context 或 runtime snapshot，MUST NOT 读取 Unity input object 或 scene object。

#### Scenario: 有移动意图选择 Directional
- **GIVEN** Dodge request 已被 action request 仲裁接受
- **AND** 当前 movement intent 有有效方向
- **WHEN** Dodge selector 评估
- **THEN** selector MUST 选择 Directional timeline
- **AND** Backstep timeline MUST 不输出 motion、animation、fact 或 cue

#### Scenario: 无移动意图选择 Backstep
- **GIVEN** Dodge request 已被 action request 仲裁接受
- **AND** 当前 movement intent 没有有效方向
- **WHEN** Dodge selector 评估
- **THEN** selector MUST 选择 Backstep timeline
- **AND** Directional timeline MUST 不输出 motion、animation、fact 或 cue

### Requirement: Dodge Timeline 使用 Frame 时间权威
`Action.Dodge` 的 timeline runtime MUST 使用 frame index、duration frame 和 window frame range 作为时间权威。Seconds MAY 在 editor、importer 或 inspector 中显示，但 MUST 在进入 runtime definition 前转换为 frame。

#### Scenario: Runtime 不读取 Seconds 权威
- **GIVEN** Dodge timeline definition 已被编译或加载到 runtime
- **WHEN** CommittedActionBranchEvaluator 在 tick N 评估
- **THEN** duration、window 和 timeline sampling MUST 基于 frame 字段
- **AND** runtime MUST NOT 读取 seconds 字段作为推进 timeline 的权威来源

### Requirement: Dodge 无隐藏 Fallback
如果 `Action.Dodge` 的 selector、condition、Directional timeline 或 Backstep timeline 缺失或非法，正式 gameplay MUST 报告配置错误或拒绝动作输出。系统 MUST NOT 使用旧 variant 字段、Resources、代码默认 timeline、场景对象或全局单例补齐缺失配置。

#### Scenario: 缺失 Directional timeline 报错
- **GIVEN** Dodge 配置缺失 Directional timeline
- **WHEN** 有移动意图的 Dodge 请求被处理
- **THEN** 系统 MUST 报告配置错误或拒绝动作输出
- **AND** MUST NOT 使用旧 Directional variant 字段继续运行

#### Scenario: 缺失 Backstep timeline 报错
- **GIVEN** Dodge 配置缺失 Backstep timeline
- **WHEN** 无移动意图的 Dodge 请求被处理
- **THEN** 系统 MUST 报告配置错误或拒绝动作输出
- **AND** MUST NOT 使用旧 Backstep variant 字段继续运行

### Requirement: Dodge 行为回归保持
Dodge timeline 迁移 MUST 保持 Directional、Backstep、Run latch、input consume、interrupt resistance、animation-end 等待、动作结束后再次触发和 rollback restore 的现有行为语义。

#### Scenario: Directional Run latch 保持
- **GIVEN** Directional Dodge 完成帧仍有移动输入
- **WHEN** behavior submission 被最终 frame plan 采用并应用
- **THEN** Run latch 行为 MUST 与迁移前一致
- **AND** 后续移动输入 MUST 继续使用 Run 档位

#### Scenario: Backstep 不写 Run latch
- **GIVEN** Backstep Dodge 完成
- **WHEN** final frame output 被应用
- **THEN** 系统 MUST NOT 因 Backstep 写入 Run latch
- **AND** 行为 MUST 与迁移前一致

#### Scenario: Restore 后 frame 一致
- **GIVEN** rollback restore 到 Dodge timeline 中间帧
- **WHEN** 下一 tick 继续评估
- **THEN** selected timeline frame、motion output 和 animation intent MUST 与 restore state 对应
- **AND** MUST NOT 依赖 evaluator 实例保存状态

### Requirement: Dodge 仍走统一 Behavior Submission
Dodge timeline 迁移后，Dodge MUST 继续通过 Action domain、BehaviorSubmission、CharacterFrameSubmission 或 CharacterFramePlan 进入唯一角色帧管线。Dodge MUST NOT 新增第二角色控制器、第二 runner、第二 motion executor、第二 animation presenter 或直接 Transform 写入路径。

#### Scenario: Dodge 输出进入统一提交
- **WHEN** Dodge timeline 在 tick N 输出 motion 和 animation
- **THEN** 输出 MUST 进入 Action behavior submission 或批准的等价角色帧提交
- **AND** 最终是否采用 MUST 由 CharacterFramePlan 或等价计划决定
- **AND** Dodge timeline MUST NOT 直接应用 motion 或播放 animation
