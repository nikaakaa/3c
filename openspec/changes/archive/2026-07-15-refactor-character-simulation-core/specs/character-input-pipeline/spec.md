# character-input-pipeline Specification

## MODIFIED Requirements

### Requirement: CharacterInputStage 每 tick 产出 CharacterInputFrame

Unity Character Input Adapter MUST在本地采样边界将 CharacterInputProfile/InputAction、Camera-relative direction 和离散 request 转换为 portable CharacterSimulationInput。Adapter MAY保留 CharacterInputFrame 作为 Unity 采样内部结构，但 Driver plan、Kernel 和 Program MUST只消费 portable input contract。

#### Scenario: 采样移动与闪避

- **WHEN** Input Adapter 读取 Move 和 Shift request
- **THEN** MUST按当前 Session NumericProfile 生成带稳定 InputId、target scalar/vector value、request sequence 和 source tick 的 CharacterSimulationInput
- **AND** Local Float32 Adapter MUST不预先量化为未来 Rollback 的 Fixed 格式

### Requirement: CharacterInputHistory 保存预测重放所需输入帧

Input history MUST不再由公共 CharacterInputStage 或 Simulation Core 默认拥有。Local Driver MUST不创建 replay history；需要 prediction/replay 的后续 Network Model MUST在自己的 Driver state 中保存 portable CharacterSimulationInput history。

#### Scenario: Local Driver 提交输入

- **WHEN** Local Driver 完成本 Tick
- **THEN** Core MUST不创建 model history 或假 rollback buffer

### Requirement: GraphContext 读取同一输入帧和请求缓存

Compiled input operation MUST从当前 Actor input 与 CharacterSimulationState request buffer 读取连续值和离散请求。Operation MUST不读取 CharacterGraphContext、Unity InputAction、Camera 或 mutable CharacterPipelineFrame。

#### Scenario: Attack request 被消费

- **WHEN** compiled Action operation 消费当前 Attack request
- **THEN** MUST通过 request identity 更新 CharacterSimulationState buffer

### Requirement: Network Model 必须从正式输入或运动事实构造自己的命令

后续 Network Model adapter MUST只从 CharacterSimulationInput、SimulationTickResult、SimulationWorldSnapshot 或 typed facts 构造自己的 packet/history。Program、Kernel 和 Unity Input Adapter MUST不保存 packet、model policy 或 correction metadata。

#### Scenario: 后续 ServerAuthoritative 构造命令

- **WHEN** model Driver 需要生成 canonical input command
- **THEN** MUST从 portable Actor input 与 Tick identity 映射
- **AND** MUST不读取 authoring node 或 InputAction
