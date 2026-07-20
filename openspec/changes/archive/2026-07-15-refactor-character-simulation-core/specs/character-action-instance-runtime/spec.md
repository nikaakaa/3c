# character-action-instance-runtime Specification

## MODIFIED Requirements

### Requirement: 动作运行时必须使用 ActionInstance 表达一次动作实例

CharacterSimulationState MUST使用 ActionInstance state slots 表达一次被接受的动作启动，并至少保存 ActionId、ActionInstanceId、PredictionKey、input sequence、start SimulationTick、target snapshot、phase 和 state。外部确认 MUST通过 typed SimulationIngress 中的 instance/prediction identity 匹配，MUST不通过 Graph path、Timeline asset 或 model packet identity确认动作。

#### Scenario: Compiled Graph 激活动作

- **WHEN** Program operation 接受 ActionActivationRequest
- **THEN** MUST在 CharacterSimulationState 创建稳定 ActionInstance

#### Scenario: 外部确认动作

- **WHEN** Driver 提交 Action confirm ingress
- **THEN** Program MUST通过 ActionInstanceId、PredictionKey 或 input sequence 匹配本地实例
- **AND** MUST不读取原始 network packet

### Requirement: ActionRuntime 必须是动作事务层而不是执行编排层

Compiled Action operations MUST只负责 profile 查询、activation 验证、ActionInstance 创建和 lifecycle transition。它们 MUST不调用 Graph runtime、播放 Timeline、调用 WorldSolver、应用 model correction、播放 Cue 或裁决命中。Timeline、Motion 与 GameplayResult 通过 Program operation、world batch 和 typed facts继续处理。

#### Scenario: 动作激活成功

- **WHEN** Action operation 接受 ActionActivationRequest
- **THEN** MUST创建 ActionInstance 并输出正式 Action Context

#### Scenario: 生命周期 ingress

- **WHEN** Graph、Timeline、SimulationIngress 或系统生命周期提交 ActionLifecycleTransition
- **THEN** Action operation MUST按 transition type 更新实例 state、phase 和 reason

#### Scenario: 动作事务校正

- **WHEN** Driver 提交非终止 Correct ingress
- **THEN** MUST只更新 ActionInstance corrected state
- **AND** world restore 或 visual recovery MUST分别由 Driver/SessionRuntime 与 Committer处理
