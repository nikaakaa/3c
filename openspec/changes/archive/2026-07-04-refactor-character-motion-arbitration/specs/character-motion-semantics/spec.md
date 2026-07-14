# character-motion-semantics Specification Delta

## ADDED Requirements

### Requirement: MotionContribution 必须携带仲裁语义
系统 MUST 让 `MotionContribution` 携带正式仲裁语义，包括 motion channel、blend mode、priority、weight、source type、source identity 和是否消费低层 channel。系统 MUST NOT 只依赖字段存在却不参与 resolver 的无效 priority。

#### Scenario: 输入移动提交运动来源
- **WHEN** 输入节点根据移动输入产生本帧位移
- **THEN** 它 MUST 提交 `Locomotion` channel 的 `MotionContribution`
- **AND** 它 MUST NOT 直接把输入移动写成最终 `MotionIntent`

#### Scenario: Timeline root motion 提交运动来源
- **WHEN** Timeline 采样到 root motion delta
- **THEN** 它 MUST 提交 `Action` channel 的 `MotionContribution`
- **AND** contribution MUST 携带 source type、priority、weight 和可追踪 source identity

### Requirement: MotionResolver 必须使用固定 channel 顺序仲裁
系统 MUST 使用固定 channel 顺序把多个 motion 来源仲裁为 `MotionIntent`。第一阶段顺序 MUST 至少覆盖 `Locomotion -> Action -> GameplayResult`，并且 MUST 由 `MotionResolver` 或等价正式 motion pipeline 显式维护。

#### Scenario: 攻击 root motion 覆盖输入移动
- **WHEN** 同一帧存在 `Locomotion` 输入移动和 `Action` root motion
- **AND** action contribution 使用 override 并消费低层 channel
- **THEN** resolver MUST 使用 action root motion 作为主要位移来源
- **AND** 输入移动 MUST NOT 被简单相加到最终位移中

#### Scenario: 受击击退覆盖动作位移
- **WHEN** 同一帧存在 `Action` root motion 和 `GameplayResult` 击退
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
系统 MUST 保持 MotionWarp 为 Move 前 `MotionModifier`，并在固定顺序中运行于 gameplay contribution 仲裁之后、network correction 之前。系统 MUST NOT 将 MotionWarp 伪装成普通 root motion contribution 或直接修改 Transform。

#### Scenario: 攻击吸附发生在 action intent 之后
- **WHEN** action root motion 已经被 resolver 仲裁为 raw `MotionIntent`
- **AND** 当前 Timeline 采样到 MotionWarp window
- **THEN** MotionWarp MUST 基于 raw `MotionIntent`、target context 和窗口限制生成修正后的 intent
- **AND** 修正结果 MUST 继续交给 MotionStage 后续阶段处理

### Requirement: Network correction 必须进入正式 correction phase
系统 MUST 将网络 correction 纳入 `CharacterMotionStage` 的正式 correction phase。系统 MUST NOT 在 motion resolver 前直接 `SetPositionAndRotation` 作为正式纠偏路径。

#### Scenario: 平滑纠偏
- **WHEN** 本帧收到 smooth correction
- **THEN** correction phase MUST 在 gameplay intent 和 motion modifier 之后应用平滑修正
- **AND** 修正量 MUST 写入 debug 或等价 runtime 输出
- **AND** correction acknowledgement MUST 继续通过 network output 收集

#### Scenario: 强制纠偏
- **WHEN** 本帧收到 force correction
- **THEN** correction phase MAY 覆盖最终位置或朝向
- **AND** 该覆盖 MUST 发生在明确的 correction phase
- **AND** 系统 MUST 记录这是 authority correction，而不是普通 action/root motion 来源

### Requirement: Motion debug 必须解释仲裁结果
系统 MUST 提供或预留 motion resolve debug 数据，说明本帧 contribution、channel、blend mode、priority、weight、source identity、modifier delta、correction delta 和最终获胜来源。调试信息 MUST 服务于动作手感和网络纠偏排查。

#### Scenario: 查看攻击帧位移来源
- **WHEN** 本帧同时存在输入移动、攻击 root motion 和 MotionWarp
- **THEN** debug MUST 能显示输入 contribution、action contribution、MotionWarp delta 和最终 `MotionIntent`
- **AND** debug MUST 能说明输入是否被 action 消费

