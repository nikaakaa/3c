# gameplay-behavior-policy-model Specification

## Purpose
定义 Gameplay 行为身份目录：Action、通用 Stream/Event 与 Gameplay Effect 使用稳定 BehaviorId、BehaviorKind、Tag 和调试元数据进入同一 Program catalog；网络策略继续归具体 Network Model，不由 BehaviorProfile 执行或隐式推导。

## Requirements

### Requirement: Gameplay Behavior 必须提供稳定作者身份

系统 MUST使用 `IGameplayBehaviorProfile` 或等价合同表达稳定 BehaviorId、BehaviorKind、display name、debug category 与 gameplay tags。Transaction MUST由 `ActionProfile` 提供，Effect MUST由 `GameplayEffectDefinition` 提供，通用 Stream/Event MAY由 `GameplayBehaviorProfile` 提供。Behavior identity MUST不替代 Graph operation、ActionInstance、GameplayFact、PresentationCommand 或 World request。

#### Scenario: 注册普通移动行为

- **WHEN** `CharacterPipelineDefinition` 注册 `Movement.Locomotion.Move`
- **THEN** 该行为 MUST具有稳定 BehaviorId 与 `GameplayBehaviorKind.Stream`
- **AND** 普通移动 MUST继续由 input、Program operation 与 motion contribution执行

### Requirement: Transaction 与 Effect 身份不得复制

`ActionProfile.ActionId` MUST同时作为 Transaction BehaviorId；`GameplayEffectDefinition.EffectId` MUST同时作为 Effect BehaviorId。系统 MUST不要求同一 Action 或 Effect 再创建 generic `GameplayBehaviorProfile`，统一 registry MUST拒绝跨三类来源的重复 BehaviorId。

#### Scenario: Effect 与通用 Behavior 重名

- **WHEN** GameplayEffectDefinition 与 GameplayBehaviorProfile 声明相同 BehaviorId
- **THEN** `CharacterPipelineDefinition` 配置校验 MUST报告重复身份
- **AND** Compiler MUST不按资产顺序选择其中一个

### Requirement: Behavior identity 必须进入不可变 Program catalog

Compiler MUST把 Action、generic Behavior 与 Gameplay Effect 的 BehaviorKind、display、debug category 与 tags 编译进目标 Program catalog。Target runtime MAY按明确 operation读取其需要的 Action或Effect catalog；generic Behavior catalog MUST不凭自身存在自动创建 operation、状态、fact 或网络消息。

#### Scenario: 编译 Corin 行为目录

- **WHEN** Character Frontend 编译 Corin Definition
- **THEN** Program catalog MUST保存全部已注册行为的稳定 identity 与元数据
- **AND** 未被任何 operation引用的 generic Behavior MUST不产生隐藏执行路径

### Requirement: BehaviorKind 只分类作者身份

`GameplayBehaviorKind` MUST只表达 Transaction、Stream、Effect 与 Event 的业务分类。它 MUST不直接决定 packet kind、prediction、authority、history、snapshot、correction、replication 或 Presentation 执行，也 MUST不恢复已删除的 SyncDomain 分类层。

#### Scenario: Stream 行为进入 ServerAuthoritative

- **WHEN** ServerAuthoritative Source发送角色输入并复制 body observation
- **THEN** command、snapshot 与 acknowledgement语义 MUST来自该模型的 Source和Pass
- **AND** MUST不由 `GameplayBehaviorKind.Stream` 自动选择网络消息

### Requirement: Network Model 策略必须保持模型专属

具体 Network Model MUST在自己的 Definition、Source 与 Pipeline Pass中显式保存并执行协议、history、correction与replication策略。当前 ServerAuthoritative 模型 MUST以显式 `GameplayFactKind` coverage和 Program ProducerId coverage决定可靠事实与producer输出；系统 MUST不虚构通用 Behavior policy resolver、逐Action policy表或逐Effect policy表。

#### Scenario: ServerAuthoritative 校验复制覆盖

- **WHEN** Model Definition 与当前 Program 建立 compatibility identity
- **THEN** `ServerAuthoritativeReplicationPolicy` MUST校验所需 GameplayFactKind 与全部 Program ProducerId coverage
- **AND** 缺失覆盖 MUST使配置失败，不得从 BehaviorProfile推导默认策略

### Requirement: Behavior authoring 不得暴露模型执行参数

ActionProfile、GameplayBehaviorProfile 与 GameplayEffectDefinition Inspector MUST只编辑 gameplay identity、tags和各自业务规则。Tick rate、packet cadence、history capacity、correction tolerance、reliable fact kinds 与 producer coverage MUST只出现在具体 Network Model authoring中。

#### Scenario: 编辑 Gameplay Effect

- **WHEN** 作者选中一个 GameplayEffectDefinition
- **THEN** Inspector MUST显示 Effect identity与Gameplay Effect规则
- **AND** MUST不显示 ServerAuthoritative packet或history参数
