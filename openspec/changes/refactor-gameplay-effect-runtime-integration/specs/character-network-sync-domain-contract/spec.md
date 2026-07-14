## MODIFIED Requirements

### Requirement: 网络 SyncDomain 必须表达输出同步语义

系统 MUST 使用 SyncDomain 对 Character gameplay facts 进行稳定业务分类，使 recording、debug 和具体 Network Model 可以识别 Motion、Action、GameplayResult、GameplayEffect 与 Presentation。SyncDomain MUST NOT 定义 packet kind、prediction/correction 算法、snapshot 策略、endpoint 或 transport。Graph 节点路径、SubTree membership 和 Timeline membership MUST NOT 成为同步单位。

#### Scenario: 同一事实进入当前模型

- **WHEN** CharacterPipeline 产生 GameplayEffect lifecycle fact
- **THEN** fact MUST 保持 GameplayEffectSyncDomain、EffectId/BehaviorId 和 EffectInstanceId
- **AND** 是否生成 ServerAuthoritative packet MUST 由该模型 policy 决定

### Requirement: NetworkReceiveStage 必须按 SyncDomain 注入网络结果

CharacterNetworkReceiveStage 或等价输入 stage MUST 只接收 Character/gameplay 语义输入，例如 `ActionLifecycleTransition`、ExternalPoseCorrection、ExternalPoseSample、GameplayResult、GameplayEffectLifecycleFact、GameplayAttributeValueFact 和 GameplayCueFact。Model packet MUST 先由 model-owned adapter 转换；stage MUST NOT 引用 packet、endpoint、history 或 transport。

#### Scenario: 服务端确认 Gameplay Effect

- **WHEN** ServerAuthoritative adapter 收到 GameplayEffect packet
- **THEN** MUST 先转换为 model-neutral GameplayEffectLifecycleFact 或 GameplayAttributeValueFact
- **AND** input stage MUST 只把语义事实交给 CharacterGameplayEffectInputMapper

#### Scenario: 运动校正

- **WHEN** model adapter 收到 MotionCorrection
- **THEN** MUST 转换为 ExternalPoseCorrection
- **AND** 最终应用 MUST 仍由 CharacterMotionStage 完成

### Requirement: 旧 GameplayState、StateEffect 和 ActionCue 合同必须删除

系统 MUST 删除 `GameplayStateEffectFact`、`StateEffectSyncDomainInput`、`StateEffectSyncDomainOutput`、`GameplayBehaviorKind.State`、`ActionCueEvent`、旧 `StateId` 与旧 `PayloadDigest` 解析路径。正式合同 MUST 分别命名为 `GameplayEffectLifecycleFact`、`GameplayAttributeValueFact`、`GameplayEffectSyncDomainInput`、`GameplayEffectSyncDomainOutput`、`GameplayBehaviorKind.Effect` 与 `GameplayCueFact`。具体 Network Model MUST 同步删除 StateEffect 和 ActionCue 专用 domain、fact kind、packet kind、payload、resolver、history 与 debug 名称。系统 MUST NOT 保留旧类型别名、兼容构造函数、枚举别名、双写或 fallback 解析。

#### Scenario: 迁移完成后扫描旧合同

- **WHEN** Runtime 与模型映射完成 Gameplay Effect 迁移
- **THEN** 正式运行时路径 MUST 不再引用 GameplayStateEffectFact、StateEffectSyncDomain、ActionCueEvent 或 ServerAuthoritativeStateEffect
- **AND** GameplayEffectLifecycleFact MUST 不再使用 StateId 或 PayloadDigest 表达效果语义

#### Scenario: Timeline 产生表现 Cue

- **WHEN** Timeline 或 GE 产生可表现 cue
- **THEN** 两者 MUST 统一产生 `GameplayCueFact`
- **AND** 系统 MUST NOT 根据来源模块恢复 ActionCue 与 EffectCue 两套网络合同
