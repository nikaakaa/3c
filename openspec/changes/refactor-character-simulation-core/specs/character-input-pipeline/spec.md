# character-input-pipeline Specification

## MODIFIED Requirements

### Requirement: CharacterInputStage 每 tick 产出 CharacterInputFrame

Unity Character Input Adapter MUST每个本地采样 Tick 将 CharacterInputProfile/InputAction 转换为 portable CharacterSimulationInput。Adapter MAY保留 CharacterInputFrame 作为 Unity 采样内部结构，但 Kernel/Program MUST只消费 portable input contract。

#### Scenario: 采样移动与闪避

- **WHEN** Input Adapter 读取 Move 和 Shift request
- **THEN** MUST生成带稳定 InputId、量化值、request sequence 和 source tick 的 CharacterSimulationInput

### Requirement: CharacterInputHistory 保存预测重放所需输入帧

Input history MUST不再由公共 CharacterInputStage 默认拥有。Local Driver MAY不保存 history；需要 prediction/replay 的后续 Network Model MUST在其 Driver 内保存 portable CharacterSimulationInput history。

#### Scenario: Local Driver 提交输入

- **WHEN** Local Driver 完成本 Tick Step
- **THEN** Core MUST不强制创建 replay history

### Requirement: GraphContext 读取同一输入帧和请求缓存

Compiled input operation MUST从当前 CharacterSimulationInput 与 SimulationState request buffer 读取连续值和离散请求。Operation MUST不读取 CharacterGraphContext 中的 Unity InputAction、Camera 或 mutable frame object。

#### Scenario: Attack request 被消费

- **WHEN** compiled Action operation 消费当前 Attack request
- **THEN** MUST通过 request identity 更新 SimulationState buffer

### Requirement: Network Model 必须从正式输入或运动事实构造自己的命令

Network Model adapter MUST只从 CharacterSimulationInput 或 SimulationOutput typed facts/body result 构造自己的 packet/history。Program、Kernel 和 Input Adapter MUST不保存 packet 或 model policy。

#### Scenario: 现有 ServerAuthoritative adapter 构造 command

- **WHEN** adapter 需要生成当前 model command
- **THEN** MUST从 portable input/output 映射
- **AND** MUST不读取 authoring node 或 InputAction
