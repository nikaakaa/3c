## MODIFIED Requirements

### Requirement: 系统输入与 Motion 输出必须由模型边界显式映射

ExecutionPlan的 canonical input与 Session Step/Egress输出的 Motion fact/body sample MUST保留 ActorId、SimulationTick、source clock、Program identity和稳定 EventId。具体 Network Model MUST在自己的 Source、Ingress/Egress Pass与 Committer adapter中把这些 identity映射为 command、snapshot或 acknowledgement policy；Character Core MUST不保存通用网络 behavior binding、packet kind或 correction application policy。Gameplay Effect fact MUST继续使用自己的 EffectId/BehaviorId，不得配置固定 Effect behavior槽位。

#### Scenario: 模型发送 canonical input

- **WHEN** ServerAuthoritative Source/Egress Pass将当前 control input映射为 outgoing command
- **THEN** 模型 MUST使用 Step source、ActorId、input sequence和 Program identity选择自己的 stream policy
- **AND** CharacterSimulationInput MUST不保存 packet kind或模型 policy

#### Scenario: 模型消费 Motion 输出

- **WHEN** Egress/Committer adapter消费带稳定 EventId的 Motion fact或 body sample
- **THEN** 模型 MUST显式决定 Publish、Replace、Retire或 Suppress及对应 packet映射
- **AND** Character Core MUST不生成模型专属 correction acknowledgement

