## MODIFIED Requirements

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
