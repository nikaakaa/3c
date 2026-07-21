## MODIFIED Requirements

### Requirement: MotionWarp authoring 必须在发布前拒绝不完整配置

Timeline Inspector、Semantic Compiler 与 Agent Validator MUST复用同一套 MotionWarp 校验。source、owner、window、mode、offset、weight、clamp、progress curve、Action Context 与 Action target requirement 任一无效时，artifact 发布 MUST失败。系统 MUST NOT写默认目标、缩短窗口或创建 fallback 配置。

MotionWarp 所属动作 MAY声明 `OptionalSnapshot` 或 `SnapshotRequired`。`None` 与 MotionWarp 的组合 MUST在发布前拒绝。`OptionalSnapshot` 动作无目标时，runtime MUST根据已编译业务策略保留 resolved source MotionCurve，并报告 `NoTargetByOptionalPolicy` 或等价 typed 结果；该结果 MUST NOT被描述为配置失败或静默禁用。

#### Scenario: Warp 所属动作不接受目标

- **WHEN** MotionWarp 所在 Timeline 由 `ActionTargetRequirement.None` 的 Action 启动
- **THEN** 编译 MUST失败并定位 ActionProfile、Timeline 与 MotionWarpClip
- **AND** 系统 MUST NOT在运行时把该错误解释为不 Warp

#### Scenario: 可选目标动作当前没有目标

- **WHEN** MotionWarp 所在 Timeline 由 `OptionalSnapshot` Action 启动
- **AND** 对应 ActionInstance 没有 captured target snapshot
- **THEN** runtime MUST原样保留已仲裁的 source MotionCurve contribution
- **AND** MUST不建立 Warp 跨 Tick 状态或产生 position/yaw correction

#### Scenario: 必需目标动作缺少目标

- **WHEN** MotionWarp 所在 Timeline 由 `SnapshotRequired` Action 启动
- **AND** call site 没有有效目标 declaration 或候选值
- **THEN** authoring、admission 或 artifact validation MUST在 Warp 执行前失败
- **AND** runtime MUST NOT通过 Optional 语义继续动作
