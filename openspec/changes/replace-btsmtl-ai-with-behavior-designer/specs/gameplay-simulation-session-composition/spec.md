## MODIFIED Requirements

### Requirement: Local Launch Plan必须锁定Control Source与Observation能力

Local Session Preparation MUST为完整Actor roster显式生成不可变Control Source roster。每个entry MUST包含ActorId、Control Source identity、Numeric ABI、Character Program binding与所需runtime capability；需要AI观察的entry MUST同时绑定行为稳定身份/内容版本、任务版本、正式输入目录和Committed Actor Observation schema。Launch Plan和Composition identity MUST包含这些binding。公共Host、Composer、Ingress与Source MUST不按具体AI类型、Actor名称、Tag、第一个可用实现或fallback选择Control Source，Active后 MUST不替换Control Source或Observation provider。

需要批量准备的输入源 MUST在该合同中声明唯一调度与输入生产位置；Source MUST先完成本端批量准备，再从同一角色输入装配取得结果。插件类型、运行对象和任务进度 MUST不进入 portable Launch Plan。

#### Scenario: Local AI Actor准备完成

- **WHEN** AI Actor的行为内容、Character Program与Committed Observation capability全部匹配
- **THEN** Preparation MUST把其AI Control Source作为锁定Actor entry写入Launch Plan
- **AND** Standard Runtime Launcher MUST沿现有target-specific Composer创建唯一Session runtime

#### Scenario: Local插件AI Actor准备完成

- **WHEN** AI Actor的Behavior Designer内容、Character Program、输入所有权与Committed Observation capability全部匹配
- **THEN** Preparation MUST把其插件AI Control Source作为锁定Actor entry写入Launch Plan
- **AND** Standard Runtime Launcher MUST沿现有target-specific Composer创建唯一Session runtime

#### Scenario: Composition缺少Observation capability

- **WHEN** Actor绑定AI Control Source但当前Source、Pipeline或Execution Backend没有声明匹配Committed Observation schema
- **THEN** Preparation MUST在Session Active前失败并报告ActorId、行为身份与缺失capability
- **AND** MUST不替换为Neutral Source或创建Session查询旁路

#### Scenario: 两个本地Bot绑定同一行为资产

- **WHEN** 两个 Actor 复用同一正式插件行为内容
- **THEN** 它们 MUST分别拥有输入身份和插件运行实例
- **AND** MUST不共享可变战术变量、请求序号或输出缓存

## ADDED Requirements

### Requirement: 会话装配必须明确AI输入生产位置和所有权

会话 MUST在 Active 前锁定每个 Actor 的唯一输入来源与生产者，并校验行为内容、输入目录、观察和 Numeric Target。Local Float32/Fixed 的生产者为本机，Unity Authority 的生产者为权威 Worker，Rollback 的生产者为明确配置的真实 Peer；接收端仅装配正式输入或结果消费。未支持插件 AI 的纯 .NET 宿主 MUST拒绝其运行绑定，MUST不创建旁路执行服务或根据连接先后选择生产者。

#### Scenario: Rollback候选未指定Bot生产Peer

- **WHEN** Actor 名单包含 Bot 而其输入所有权未绑定合法真实 Peer
- **THEN** Preparation MUST拒绝创建 Session
- **AND** MUST不把第一个连接者当作默认生产者

#### Scenario: 对局中尝试更换Bot生产者

- **WHEN** Active Session 尝试变更输入所有权或行为版本
- **THEN** 系统 MUST拒绝在原会话内静默替换
- **AND** 新配置 MUST通过正式新会话采用边界生效
