## ADDED Requirements

### Requirement: 动作决策、逻辑位姿校正和表现采样必须使用独立合同

系统 MUST 将动作事务 decision、MotionSyncDomain 位姿 correction 和 PresentationFrame 显示处理视为三个独立职责。ActionProfile、ActionMotionPolicy、GameplayBehaviorProfile 和 Action Context MUST NOT 保存同时表示 Reject、逻辑位姿应用方式与表现平滑的统一 correction policy。

#### Scenario: 服务端拒绝预测攻击

- **WHEN** 客户端收到某个 ActionInstance 的 Reject decision
- **THEN** ActionRuntime MUST 通过 terminal `ActionLifecycleTransition(Reject)` 关闭该实例
- **AND** 系统 MUST NOT 查询 Smooth、Force 或 CancelOnReject 配置决定是否关闭

#### Scenario: 服务端纠正角色位置

- **WHEN** 客户端收到 MotionSyncDomain correction
- **THEN** CharacterMotionStage MUST 通过唯一正式 correction phase 应用逻辑位姿误差
- **AND** ActionProfile、GameplayBehaviorProfile 与当前 ActionInstance MUST NOT 决定该误差应用算法

#### Scenario: 表现帧消费校正结果

- **WHEN** logic tick 已产生 correction application result
- **THEN** PresentationFrame MAY 根据正式 result 维持当前视觉贴合行为
- **AND** PresentationFrame MUST NOT 修改 ActionRuntime、逻辑 Transform 或 correction acknowledgement

### Requirement: 当前 direct correction 算法不得升级为跨层作者策略

本 change MUST 保持 CharacterMotionStage 当前 partial/full direct correction 数值行为，但 MUST NOT 新增 CharacterMotionCorrectionDefinition、Action correction policy、Behavior correction policy 或等价作者配置。当前算法 MUST 只有 MotionStage 内部这一条执行路径，不得形成 Profile 读取失败后退回代码常量的 fallback。

#### Scenario: 作者查看动作和行为配置

- **WHEN** 作者查看 ActionProfile 或 GameplayBehaviorProfile
- **THEN** Inspector MUST NOT 暴露 partial fraction、单 tick clamp、full-application threshold 或 Reject correction 选项
- **AND** profile runtime MUST NOT 携带这些值

#### Scenario: 作者查看 PipelineDefinition

- **WHEN** 作者查看 CharacterPipelineDefinition
- **THEN** Inspector MUST NOT 新增当前 direct correction 算法的 tuning 分区
- **AND** 后续 owner replay 与 visual recovery 参数 MUST 等待独立 change 定义

#### Scenario: Runtime 应用 correction

- **WHEN** CharacterMotionStage 处理 incoming correction
- **THEN** 它 MUST 只执行当前唯一 correction phase
- **AND** 它 MUST NOT 先查询已删除的 profile policy 或隐式 fallback 配置

### Requirement: Motion correction application 必须输出正式结果而不是依赖 debug

CharacterMotionStage MUST 输出正式 `MotionCorrectionApplicationResult` 或等价结果，至少包含 applied、application extent、input sequence、server tick、目标位姿与实际应用 delta。Application extent MUST 只描述本 tick 未应用、部分应用或完整应用的事实，不得成为作者策略。Motion debug 和 Presentation logic sample MAY 消费该结果；运行时行为 MUST NOT 从 debug snapshot 反向读取。

#### Scenario: Debug 关闭时发生校正

- **WHEN** runtime diagnostics 被关闭且本 tick 应用了 correction
- **THEN** MotionCorrectionApplicationResult MUST 仍完整产生
- **AND** Presentation MUST 得到与开启 debug 时相同的 application extent

#### Scenario: 生成调试记录

- **WHEN** runtime diagnostics 开启且 correction application result 有效
- **THEN** motion debug MUST 从正式 result 展示 extent、target、delta 和 tick
- **AND** debug record MUST NOT 成为 MotionStage 或 Presentation 的配置输入

### Requirement: Correction acknowledgement 必须是独立同步事实

系统 MUST 使用独立 `MotionCorrectionAcknowledgement` 运行时事实和 `GameplayMotionCorrectionAcknowledgement` packet payload 表达 Ack。Ack MUST 只携带确认所需的 input sequence 与 server tick，MUST NOT 复用 incoming Correction 对象或回显 position/rotation。Ack 只有在 correction 确实应用后才可产生。

#### Scenario: 成功应用 correction

- **WHEN** CharacterMotionStage 已将 incoming correction 应用到逻辑位姿
- **THEN** 它 MUST 产生对应 MotionCorrectionAcknowledgement SyncFact
- **AND** outgoing adapter MUST 通过正式 MotionCorrectionAck behavior binding 解析发送策略

#### Scenario: correction 未应用

- **WHEN** correction 因缺少正式运行目标或其它运行错误未被应用
- **THEN** 系统 MUST NOT 产生成功 acknowledgement
- **AND** adapter MUST NOT 通过 correction policy 或 packet presence 伪造 Ack

### Requirement: Action motion 合同不得携带 actor correction 语义

Action motion output MUST 只描述某个 ActionInstance 产生的 motion source、input sequence、logic tick 和 prediction 归属。`ActionMotionSourceType.Correction`、ActionMotionSample correction id 和 ActionMotionDigest correction id MUST 删除。Action lifecycle transition 用于关联 Correct decision 的 correction id MAY 保留。

#### Scenario: Timeline 输出攻击位移

- **WHEN** 攻击 Timeline 提交 MotionCurve 或 RootMotion action motion sample
- **THEN** sample MUST 携带 ActionInstanceId、input sequence、logic tick 和 source type
- **AND** sample MUST NOT 携带 actor motion correction id

#### Scenario: 角色收到位置 correction

- **WHEN** MotionSyncDomain 收到角色位置 correction
- **THEN** correction MUST 进入 CharacterMotionStage 的正式 correction phase
- **AND** 系统 MUST NOT 伪造 `ActionMotionSourceType.Correction` 或把 correction 归属到当前 ActionInstance
