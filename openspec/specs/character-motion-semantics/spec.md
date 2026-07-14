# character-motion-semantics Specification

## Purpose
定义角色运动语义：上游通过 `MotionContribution`、`MotionIntent`、`MotionModifier` 和 `MotionResult` 表达输入移动、Timeline motion curve、gameplay result、motion warp 和网络校正，最终 Transform 应用仍由正式 MotionStage 负责。
## Requirements
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

### Requirement: CharacterMotionStage 是 motion modifier 和 Move 的唯一边界

系统 MUST 让 `CharacterMotionStage` 成为角色最终运动修正与运动执行编排的唯一 gameplay 边界。MotionStage MUST 在执行前完成 modifier 和 correction plan，并把最终 `MotionIntent` 交给正式 Motion Executor。Motion Executor MUST 返回实际执行结果；MotionStage MUST 据此写入 `MotionResult`。MotionStage MUST NOT 直接持有或调用 `CharacterController`、Transform 或具体 KCC 实现。

#### Scenario: 执行 motion modifier

- **WHEN** MotionStage 已经从 resolver 得到 raw `MotionIntent`
- **THEN** MotionStage MUST 在调用正式 Motion Executor 前应用 `MotionModifier`
- **AND** modifier 输出 MUST 是新的 `MotionIntent` 或等价的 intent 变化

#### Scenario: 写入运动结果

- **WHEN** Motion Executor 完成一次世界约束运动步骤
- **THEN** executor MUST 返回正式 Motion Execution Result
- **AND** MotionStage MUST 从该结果写入 `MotionResult`
- **AND** `MotionResult` MUST 记录请求位移、实际位移、位置、grounded 状态、请求 yaw 和实际 yaw

### Requirement: MotionModifier 来源可以来自 Timeline、Action、World、GameplayResult 或 Network
系统 MUST 允许 motion modifier 数据来自不同业务来源，但所有来源 MUST 汇入同一个 MotionStage 执行链路。Timeline 只表达时间窗口和采样数据；外部目标、world context data、gameplay result 和网络修正 MUST 来自 runtime context。

#### Scenario: Timeline-scoped modifier
- **WHEN** Timeline 表达某段攻击、闪避或翻越允许 motion warp
- **THEN** BTSMTL 内部 TimelinePlaybackScheduler MUST 采样并提交 motion modifier 数据或窗口
- **AND** TimelinePlaybackScheduler MUST NOT 直接改写角色 Transform

#### Scenario: 外部上下文影响 modifier
- **WHEN** motion warp 需要当前攻击目标、锁定目标、障碍物点、平台速度或服务器修正
- **THEN** MotionStage MUST 通过正式 runtime context 读取这些数据
- **AND** modifier 数据 MUST 使用 target key 或 blackboard key 引用外部上下文，而不是持有场景对象 fallback

### Requirement: MotionWarp 是 Move 前 modifier

系统 MUST 将 motion warp 实现为运动执行前的 `MotionModifier`。MotionWarp MUST 基于 raw `MotionIntent`、warp window、target context data 和限制参数生成修正后的 `MotionIntent`，并继续通过正式 Motion Executor 执行。

#### Scenario: 攻击吸附目标

- **WHEN** 当前动作 Timeline 采样到 motion warp window 且 runtime context 提供目标位置
- **THEN** MotionWarp MUST 在 motion execution 前修正 displacement 或 yaw intent
- **AND** 修正结果 MUST 继续通过当前 LocalSolver 的正式 Motion Executor 应用

#### Scenario: 目标缺失

- **WHEN** motion warp window 存在但 target context data 缺失
- **THEN** MotionWarp MUST 按正式缺失策略处理
- **AND** 系统 MUST NOT 使用场景搜索、`Camera.main`、`FindObjectOfType` 或隐藏 fallback 补齐目标

### Requirement: Motion modifier 第一阶段使用固定顺序
系统 MUST 在第一阶段使用固定 motion modifier 顺序，而不是动态插件注册表。固定顺序 MUST 由 MotionStage 或等价 motion pipeline 显式维护，便于调试和网络校验。

#### Scenario: 多个 modifier 同帧存在
- **WHEN** 同一帧同时存在 Timeline warp、gameplay result knockback、world/platform motion 或 network correction
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

### Requirement: MotionContribution 必须携带仲裁语义
系统 MUST 让 `MotionContribution` 携带正式仲裁语义，包括 motion channel、blend mode、priority、weight、source type、source identity 和是否消费低层 channel。输入移动、Timeline motion curve、gameplay result 和 correction 等来源都 MUST 使用同一贡献合同或正式 modifier 合同。系统 MUST NOT 只依赖字段存在却不参与 resolver 的无效 priority。

#### Scenario: 输入移动提交运动来源
- **WHEN** 输入节点根据移动输入产生本帧位移
- **THEN** 它 MUST 提交 `Locomotion` channel 的 `MotionContribution`
- **AND** 它 MUST NOT 直接把输入移动写成最终 `MotionIntent`

#### Scenario: Timeline 动画轨不提交运动来源
- **WHEN** Timeline 采样到 `AnimationTrack`
- **THEN** 它 MUST 只提交动画表现贡献
- **AND** 它 MUST NOT 从 `AnimationClip` 字段提交 root motion contribution

#### Scenario: Timeline motion curve 提交运动来源
- **WHEN** Timeline 采样到 MotionCurve clip
- **THEN** 它 MUST 按 clip 配置提交正式 `MotionContribution`
- **AND** contribution MUST 携带 channel、blend mode、priority、weight、space 和可追踪 source identity
- **AND** contribution MUST NOT 绕过 MotionResolver 直接覆盖最终位移

### Requirement: MotionContribution 必须区分位移 Delta 与低层 Channel 占用

MotionContribution 与 TimelineMotionCurveContribution MUST分别表达本 tick 是否包含位移 delta，以及是否通过 Override + ConsumeLowerChannels 占用并消费低层 channel。零 delta Override claim MUST可以成为当前 channel winner 并清空已累计低层 motion；零 delta Additive 或 WeightedBlend MUST不产生 channel claim。

#### Scenario: 攻击 Recovery 保持原地

- **WHEN** Attack MotionCurve 已到达累计曲线终点
- **AND** MotionCurveClip 仍在正式占权区间且配置 ConsumeLowerChannels
- **THEN** Timeline MUST提交零 delta Action channel claim
- **AND** MotionResolver MUST阻止 Locomotion contribution 在该 tick 生效

#### Scenario: 零 Additive Contribution

- **WHEN** Additive 或 WeightedBlend contribution 的 displacement 与 yaw 都为零
- **THEN** MotionResolver MUST忽略该 contribution
- **AND** MUST不消费低层 channel

### Requirement: MotionCurveClip 必须分开曲线结束与占权结束

MotionCurveClip MUST显式保存满足 `StartFrame < CurveEndFrame <= EndFrame` 的 CurveEndFrame。累计位置与 yaw 曲线 MUST在 StartFrame 到 CurveEndFrame 之间采样；CurveEndFrame 到 EndFrame 之间 MUST保持曲线终值，并按 Override/ConsumeLowerChannels 配置继续提交零 delta claim。缺失或非法 CurveEndFrame MUST作为配置错误，系统 MUST不按 EndFrame 猜测或兼容补齐。

#### Scenario: Corin Attack 曲线早于 Recovery 结束

- **WHEN** Attack1/Attack2 的位移曲线分别在 49/48 帧结束
- **AND** 动作 recovery 在 80 帧结束
- **THEN** 曲线 delta MUST保持原有 49/48 帧时序
- **AND** Action channel claim MUST持续到 80 帧

### Requirement: MotionResolver 必须使用固定 channel 顺序仲裁
系统 MUST 使用固定 channel 顺序把多个 motion 来源仲裁为 `MotionIntent`。第一阶段顺序 MUST 至少覆盖 `Locomotion -> Action -> GameplayResult`，并且 MUST 由 `MotionResolver` 或等价正式 motion pipeline 显式维护。

#### Scenario: 攻击 motion curve 覆盖输入移动
- **WHEN** 同一帧存在 `Locomotion` 输入移动和 `Action` motion curve
- **AND** action contribution 使用 override 并消费低层 channel
- **THEN** resolver MUST 使用 action motion curve 作为主要位移来源
- **AND** 输入移动 MUST NOT 被简单相加到最终位移中

#### Scenario: 受击击退覆盖动作位移
- **WHEN** 同一帧存在 `Action` motion curve 和 `GameplayResult` 击退
- **AND** gameplay result contribution 使用 override
- **THEN** resolver MUST 让 gameplay result 高于 action 生效
- **AND** 最终 `MotionIntent` MUST 能追踪到击退来源

### Requirement: MotionResolver 必须支持有限 blend mode
系统 MUST 支持有限 `MotionBlendMode`，第一阶段至少包含 `Additive`、`WeightedBlend` 和 `Override`。系统 MUST NOT 在第一阶段引入任意公式编辑器、脚本表达式或动态插件注册表来决定 motion 混合。

#### Scenario: 同层多个 additive 来源
- **WHEN** 同一 channel 中存在多个 `Additive` contribution
- **THEN** resolver MUST 按 weight 累加有效位移和 yaw
- **AND** 结果 MUST 可从 debug 数据中追踪每个来源的贡献量

#### Scenario: 同层多个 override 来源
- **WHEN** 同一 channel 中存在多个 `Override` contribution
- **THEN** resolver MUST 按 priority 选择生效来源
- **AND** 同 priority 情况 MUST 使用稳定规则处理，避免同一输入在不同机器得到不同结果

### Requirement: MotionWarp 必须保持为 Move 前 modifier
系统 MUST 保持 MotionWarp 为 Move 前 `MotionModifier`，并在固定顺序中运行于 gameplay contribution 仲裁之后、network correction 之前。系统 MUST NOT 将 MotionWarp 伪装成普通 motion contribution 或直接修改 Transform。

#### Scenario: 攻击吸附发生在 action intent 之后
- **WHEN** action motion curve 已经被 resolver 仲裁为 raw `MotionIntent`
- **AND** 当前 Timeline 采样到 MotionWarp window
- **THEN** MotionWarp MUST 基于 raw `MotionIntent`、target context 和窗口限制生成修正后的 intent
- **AND** 修正结果 MUST 继续交给 MotionStage 后续阶段处理

### Requirement: Network correction 必须进入正式 correction phase

系统 MUST 将 incoming network correction 纳入 `CharacterMotionStage` 的正式 correction phase，并输出 `MotionCorrectionApplicationResult`。可参与世界约束执行的 correction delta MUST 合入唯一 execution intent；需要显式重定位的完整 correction MUST 通过 Logic Pose Port 应用。系统 MUST NOT 从 ActionProfile、Action Context、GameplayBehaviorProfile 或 debug snapshot 读取 correction application strategy，也 MUST NOT 绕过 MotionStage 直接修改 logic Transform。当前 correction 数值行为 MUST 只有 MotionStage 编排的这一条执行路径，不得成为 Pipeline authoring policy或 Motion Executor policy。

#### Scenario: 本 tick 部分应用误差

- **WHEN** MotionStage 按当前单一实现只应用 authoritative position/yaw error 的一部分
- **THEN** correction phase MUST 在 gameplay intent 和 motion modifier 之后将该 delta 合入 execution intent
- **AND** Motion Executor 返回的 actual result MUST 决定实际 delta 与 application extent
- **AND** input sequence 和 server tick MUST 写入正式 result
- **AND** 成功应用后 MUST 产生独立 MotionCorrectionAcknowledgement SyncFact

#### Scenario: 本 tick 完整应用误差

- **WHEN** MotionStage 按当前单一实现应用完整 authoritative position/yaw error
- **THEN** correction phase MUST 通过 Logic Pose Port 执行正式重定位
- **AND** MUST 记录完整应用的实际 delta 和 application extent
- **AND** 系统 MUST 记录这是 authority correction，而不是普通 action motion 来源
- **AND** 同一 correction MUST 不再由 Motion Executor 重复应用

#### Scenario: 作者查看 correction 配置

- **WHEN** 作者查看 ActionProfile、GameplayBehaviorProfile 或 CharacterPipelineDefinition
- **THEN** 这些资产 MUST NOT 暴露当前 partial fraction、单 tick clamp 或 full-application threshold
- **AND** 系统 MUST NOT 新增 direct correction profile、executor policy 或 backend-specific correction 配置

### Requirement: Motion debug 必须解释仲裁结果
系统 MUST 提供或预留 motion resolve debug 数据，说明本帧 contribution、channel、blend mode、priority、weight、source identity、modifier delta、correction delta 和最终获胜来源。调试信息 MUST 服务于动作手感和网络纠偏排查。

#### Scenario: 查看攻击帧位移来源
- **WHEN** 本帧同时存在输入移动、攻击 motion curve 和 MotionWarp
- **THEN** debug MUST 能显示输入 contribution、action motion contribution、MotionWarp delta 和最终 `MotionIntent`
- **AND** debug MUST 能说明输入是否被 action 消费

### Requirement: Timeline 必须支持直接 MotionCurve 位移轨
系统 MUST 支持 Timeline 通过正式 MotionCurve 轨道直接输出 motion contribution。该轨道 MUST 表达位移曲线、yaw 曲线、空间、channel、blend mode、priority、weight 和是否消费低层 channel。轨道 MUST NOT 直接调用 `CharacterController.Move`、修改 Transform、驱动 Animator root motion 或绕过 MotionResolver。

#### Scenario: 攻击前踏使用手画曲线
- **WHEN** 攻击 Timeline 的 MotionCurve clip 覆盖当前播放时间
- **THEN** TimelinePlaybackScheduler MUST 采样该 clip 的位移和 yaw 曲线
- **AND** Scheduler MUST 提交正式 `MotionContribution`
- **AND** `CharacterMotionStage` MUST 通过 MotionResolver 仲裁后应用最终移动

#### Scenario: 本地空间曲线
- **WHEN** MotionCurve clip 配置为 Local space
- **THEN** MotionResolver MUST 按角色当前 rotation 把 displacement 转为世界位移
- **AND** 该行为 MUST 与其它 local motion contribution 使用同一解释规则

#### Scenario: 世界空间曲线
- **WHEN** MotionCurve clip 配置为 World space
- **THEN** MotionResolver MUST 直接使用该 displacement
- **AND** Timeline 轨道 MUST NOT 自己读取场景对象或 camera 作为方向 fallback

### Requirement: Timeline 位移来源必须可追踪
系统 MUST 让 Timeline 产生的直接 motion curve 和 motion warp 在 debug 数据中保持可区分来源。debug source identity MUST 至少能表达 Timeline source、track、clip 或曲线模式，以及关联的 ActionInstance（如果存在）。动画派生位移若进入 Timeline 运行时，MUST 以 MotionCurveTrack 或等价正式 motion fact 来源被追踪，而不是隐藏在 AnimationClip 字段中。

#### Scenario: 同帧存在多个 Timeline 位移来源
- **WHEN** 同一帧存在 MotionCurveTrack 和 MotionWarp window
- **THEN** motion debug MUST 能显示 motion curve contribution
- **AND** motion debug MUST 能显示 MotionWarp modifier delta
- **AND** 作者 MUST 能判断最终 motion intent 由哪个 channel 和 priority 获胜

### Requirement: MotionWarp 必须保持为目标对齐 modifier
系统 MUST 保持 MotionWarp 为 Move 前 modifier。MotionWarpTrack MUST 只表达时间窗口、目标 key、权重和限制参数；目标位置和目标 yaw MUST 来自正式 runtime context。MotionWarpTrack MUST NOT 直接保存场景对象引用、输出固定 displacement，或伪装成普通 motion contribution。

#### Scenario: 目标 key 有效
- **WHEN** Timeline 采样到 MotionWarp window 且 runtime context 提供目标 key
- **THEN** MotionWarpModifier MUST 基于 raw MotionIntent 计算 position/yaw 修正
- **AND** 修正后的 intent MUST 继续通过 CharacterMotionStage 应用

#### Scenario: 目标 key 缺失
- **WHEN** Timeline 采样到 MotionWarp window 但 runtime context 不提供目标 key
- **THEN** MotionWarpModifier MUST 跳过该 window 或按正式错误策略报告
- **AND** 系统 MUST NOT 使用场景搜索、默认目标、Camera.main 或隐藏 fallback 补齐目标

