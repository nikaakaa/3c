## MODIFIED Requirements

### Requirement: Network correction 必须进入正式 correction phase

系统 MUST 将 incoming network correction 纳入 `CharacterMotionStage` 的正式 correction phase，并输出 `MotionCorrectionApplicationResult`。系统 MUST NOT 从 ActionProfile、Action Context、GameplayBehaviorProfile 或 debug snapshot 读取 correction application strategy，也 MUST NOT 在 motion resolver 前直接 `SetPositionAndRotation` 作为正式纠偏路径。本 change MUST 保持当前 MotionStage 单一路径的数值行为，但 MUST NOT 将该直接纠偏算法新增为 Pipeline authoring policy。

#### Scenario: 本 tick 部分应用误差

- **WHEN** MotionStage 按当前单一实现只应用了 authoritative position/yaw error 的一部分
- **THEN** correction phase MUST 在 gameplay intent 和 motion modifier 之后应用该 delta
- **AND** 实际 delta、application extent、input sequence 和 server tick MUST 写入正式 result
- **AND** 成功应用后 MUST 产生独立 MotionCorrectionAcknowledgement SyncFact

#### Scenario: 本 tick 完整应用误差

- **WHEN** MotionStage 按当前单一实现应用了完整 authoritative position/yaw error
- **THEN** correction phase MUST 记录完整应用的实际 delta 和 application extent
- **AND** 系统 MUST 记录这是 authority correction，而不是普通 action motion 来源

#### Scenario: 作者查看 correction 配置

- **WHEN** 作者查看 ActionProfile、GameplayBehaviorProfile 或 CharacterPipelineDefinition
- **THEN** 这些资产 MUST NOT 暴露当前 partial fraction、单 tick clamp 或 full-application threshold
- **AND** 系统 MUST NOT 用新的 direct correction profile 替代已删除的 ActionCorrectionPolicy
