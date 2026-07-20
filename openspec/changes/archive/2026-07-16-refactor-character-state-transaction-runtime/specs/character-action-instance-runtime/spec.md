## MODIFIED Requirements

### Requirement: Timeline 必须只保留类型化 ActionInstance 引用

需要跨Tick验证Action Context的Timeline MUST在自己的typed retention state中保存最小`ActionInstanceReference`，至少包含ActionId、ContextId、ActionInstanceId与PredictionKey。Timeline MUST通过Action state port解析当前typed ActionInstance并校验引用，不得复制完整ActionInstance、保存opaque bytes或持有Action runtime具体实现。

#### Scenario: Attack Timeline 跨 Tick 继续运行

- **WHEN** Attack1 Timeline启动并保留当前Action Context
- **THEN** Timeline state MUST保存typed ActionInstanceReference
- **AND** 后续Tick MUST通过该引用校验同一Action instance仍然active

#### Scenario: Action Context 已结束

- **WHEN** retained reference对应的ActionInstance已经terminal或被替换
- **THEN** Timeline MUST按正式ActionContextEnded stop生命周期退出
- **AND** MUST不从历史bytes副本恢复旧Action状态

### Requirement: 动作运行时必须使用 ActionInstance 表达一次动作实例

CharacterSimulationState MUST使用typed ActionInstance state表达一次被接受的动作启动，并至少保存ActionId、ActionInstanceId、PredictionKey、input sequence、start SimulationTick、target snapshot、phase、state、last transition、transition tick、source tick与reason。Action activation request与target snapshot也 MUST使用正式typed state kind。外部确认 MUST通过typed SimulationIngress中的instance/prediction identity匹配，MUST不通过Graph path、Timeline asset或model packet identity确认动作。系统 MUST不保存独立Action lifecycle bytes或Action context镜像；active context MUST由Program级Action index与唯一typed ActionInstance解析。

#### Scenario: Compiled Graph 激活动作

- **WHEN** Program执行ActivateActionInstance operation
- **THEN** MUST在当前State Transaction创建稳定typed ActionInstance

#### Scenario: 外部确认动作

- **WHEN** Model Ingress Pass提交Action confirm ingress
- **THEN** Program MUST通过ActionInstanceId、PredictionKey或input sequence匹配本地typed实例
- **AND** MUST不读取原始network packet

#### Scenario: 动作生命周期变化

- **WHEN** ActionInstance从Predicted进入Confirmed或Terminal状态
- **THEN** phase、state、last transition与reason MUST在同一typed ActionInstance中原子更新
- **AND** MUST不写入第二份lifecycle state
