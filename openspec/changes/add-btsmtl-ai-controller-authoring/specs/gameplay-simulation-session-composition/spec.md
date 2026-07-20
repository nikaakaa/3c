## ADDED Requirements

### Requirement: Local Launch Plan必须锁定Control Source与Observation能力

Local Session Preparation MUST为完整Actor roster显式生成不可变Control Source roster。每个entry MUST包含ActorId、Control Source identity、Numeric ABI、Character Program binding与所需runtime capability；需要AI观察的entry MUST同时绑定AI Program identity、ControllerId与Committed Actor Observation schema。Launch Plan和Composition identity MUST包含这些binding。公共Host、Composer、Ingress与Source MUST不按具体AI类型、Actor名称、Tag、第一个可用实现或fallback选择Control Source，Active后 MUST不替换Control Source或Observation provider。

#### Scenario: Local AI Actor准备完成

- **WHEN** AI Actor的AI Program、Character Program与Committed Observation capability全部匹配
- **THEN** Preparation MUST把其AI Control Source作为锁定Actor entry写入Launch Plan
- **AND** Standard Runtime Launcher MUST沿现有target-specific Composer创建唯一Session runtime

#### Scenario: Composition缺少Observation capability

- **WHEN** Actor绑定AI Control Source但当前Source、Pipeline或Execution Backend没有声明匹配Committed Observation schema
- **THEN** Preparation MUST在Session Active前失败并报告ActorId、ControllerId与缺失capability
- **AND** MUST不替换为Neutral Source或创建Session查询旁路
