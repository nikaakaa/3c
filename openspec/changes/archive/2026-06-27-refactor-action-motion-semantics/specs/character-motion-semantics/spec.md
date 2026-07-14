# character-motion-semantics Specification

## ADDED Requirements

### Requirement: Motion 语义使用 Contribution、Intent、Modifier 和 Result
系统 MUST 使用 `MotionContribution`、`MotionIntent`、`MotionModifier` 和 `MotionResult` 表达角色运动链路。`MotionProposal` MUST 被重命名为 `MotionIntent`，系统 MUST NOT 长期保留 `MotionProposal` 兼容别名或并行字段。

#### Scenario: 上游提交运动来源
- **WHEN** Timeline、Graph、输入移动、combat 或 network correction 产生运动来源
- **THEN** 该来源 MUST 进入 `MotionContribution`、modifier 数据或 runtime fact
- **AND** 它 MUST NOT 直接调用 `CharacterController.Move` 或修改 Transform

#### Scenario: MotionStage 生成最终意图
- **WHEN** MotionStage 执行本帧运动结算
- **THEN** 它 MUST 将所有正式 motion 来源合成为 `MotionIntent`
- **AND** `MotionIntent` MUST 表达 Move 前的最终 displacement、velocity 和 yaw intent

### Requirement: CharacterMotionStage 是 motion modifier 和 Move 的唯一边界
系统 MUST 让 `CharacterMotionStage` 成为角色最终运动修正和移动应用的唯一边界。MotionStage MUST 在 Move 前执行 modifier，调用 `CharacterController.Move` 后写入 `MotionResult`。

#### Scenario: 执行 motion modifier
- **WHEN** MotionStage 已经从 resolver 得到 raw `MotionIntent`
- **THEN** MotionStage MUST 在 `CharacterController.Move` 前应用正式 `MotionModifier`
- **AND** modifier 输出 MUST 是新的 `MotionIntent` 或等价的 intent 变化

#### Scenario: 写入运动结果
- **WHEN** MotionStage 完成 `CharacterController.Move`
- **THEN** 系统 MUST 写入 `MotionResult`
- **AND** `MotionResult` MUST 记录请求位移、实际位移、位置、grounded 状态、请求 yaw 和实际 yaw

### Requirement: MotionModifier 来源可以来自 Timeline、Action、World、Combat 或 Network
系统 MUST 允许 motion modifier 数据来自不同业务来源，但所有来源 MUST 汇入同一个 MotionStage 执行链路。Timeline 只表达时间窗口和采样数据；外部目标、世界事实、combat 结果和网络修正 MUST 来自 runtime context。

#### Scenario: Timeline-scoped modifier
- **WHEN** Timeline 表达某段攻击、闪避或翻越允许 motion warp
- **THEN** BTSMTL 内部 TimelinePlaybackScheduler MUST 采样并提交 motion modifier 数据或窗口
- **AND** TimelinePlaybackScheduler MUST NOT 直接改写角色 Transform

#### Scenario: 外部事实影响 modifier
- **WHEN** motion warp 需要当前攻击目标、锁定目标、障碍物点、平台速度或服务器修正
- **THEN** MotionStage MUST 通过正式 runtime context 读取这些事实
- **AND** modifier 数据 MUST 使用 target key 或 fact key 引用外部事实，而不是持有场景对象 fallback

### Requirement: MotionWarp 是 Move 前 modifier
系统 MUST 将 motion warp 实现为 Move 前的 `MotionModifier`。MotionWarp MUST 基于 raw `MotionIntent`、warp window、target fact 和限制参数生成修正后的 `MotionIntent`。

#### Scenario: 攻击吸附目标
- **WHEN** 当前动作 Timeline 采样到 motion warp window 且 runtime context 提供目标位置
- **THEN** MotionWarp MUST 在 Move 前修正 displacement 或 yaw intent
- **AND** 修正结果 MUST 继续通过 `CharacterController.Move` 应用

#### Scenario: 目标缺失
- **WHEN** motion warp window 存在但 target fact 缺失
- **THEN** MotionWarp MUST 按正式缺失策略处理
- **AND** 系统 MUST NOT 使用场景搜索、`Camera.main`、`FindObjectOfType` 或隐藏 fallback 补齐目标

### Requirement: Motion modifier 第一阶段使用固定顺序
系统 MUST 在第一阶段使用固定 motion modifier 顺序，而不是动态插件注册表。固定顺序 MUST 由 MotionStage 或等价 motion pipeline 显式维护，便于调试和网络校验。

#### Scenario: 多个 modifier 同帧存在
- **WHEN** 同一帧同时存在 Timeline warp、combat knockback、world/platform motion 或 network correction
- **THEN** MotionStage MUST 按固定顺序处理
- **AND** 顺序 MUST 能从代码和调试输出中明确追踪

### Requirement: 旧 motion 命名和旧 BBB 数据源必须清理
系统 MUST 清理正式 runtime 和文档中的旧 motion 命名。正式 Character runtime MUST NOT 引用 `BBBNexus.MotionClipData`、`BBBNexus.WarpedMotionData` 或旧 `PlayerSO` motion 配置。

#### Scenario: 清理 MotionProposal
- **WHEN** 本变更实现完成
- **THEN** 正式 runtime 中 MUST 不再存在 `MotionProposal`
- **AND** OpenSpec 当前口径 MUST 使用 `MotionIntent`

#### Scenario: 禁止旧 BBB motion 数据
- **WHEN** 实现 root motion、motion warp、dodge、roll 或 vault
- **THEN** 系统 MAY 参考 BBB 算法
- **AND** 正式代码 MUST NOT 复制或引用 BBB 旧 motion 数据类型作为 runtime 数据源
