## MODIFIED Requirements

### Requirement: Graph Data Catalog 必须编辑 Blackboard fact projection

Graph Data Catalog MUST在 owner-local Blackboard declaration 上编辑唯一 Fact Projection。ActionWindow projection MUST只保存 WindowType、WindowId、Digest 和 Action Context provenance，并要求 Bool、Frame/Frame；没有Fact Projection的 declaration MUST保持普通本地变量。继承 declaration MUST只读并可定位 owner。系统 MUST NOT新增 Window panel、Window asset、TreeClip projection 副本、cache 或 registry；网络策略 MUST只属于当前 Network Model profile。

#### Scenario: 配置动作窗口

- **WHEN** 作者展开 owner-local `HitWindow` 或 `RecoveryOpen` declaration
- **THEN** Catalog MUST显示并编辑稳定 WindowType、WindowId、Digest 和 Action Context provenance
- **AND** 作者 MUST能定位使用同一 WindowType 的纯条件查询
- **AND** MUST不要求选择 SyncFact

#### Scenario: 配置普通本地状态门

- **WHEN** 作者展开普通 owner-local Bool Frame declaration
- **THEN** 作者 MUST能保持 Fact Projection payload 不存在
- **AND** 该变量 MUST NOT产生 `ActionWindowFact` 或被 `ActionWindowActiveInfoNode` 匹配

#### Scenario: 非法 projection

- **WHEN** declaration 不满足 Bool、Frame/Frame 或 Action Context provenance 约束
- **THEN** Catalog 与 Validator MUST报错
- **AND** 系统 MUST NOT静默改写类型、scope、lifetime、projection 或 owner

## ADDED Requirements

### Requirement: Graph Data Catalog 必须分离基础声明、输入绑定和事实投影

Blackboard条目 Details MUST把基础 declaration、可选 Input Binding 和可选 Fact Projection 显示为三个独立区域。Input Binding MUST只编辑稳定 InputValueId；Fact Projection MUST只编辑业务projection payload。Catalog MUST不显示、搜索、序列化或编辑 Authority、Sync Policy、InputDerived、SyncFact、ReplicatedCue或CorrectionOnly。

#### Scenario: 查看 ActionTarget declaration

- **WHEN** 作者展开绑定`ActionTarget`输入的Character declaration
- **THEN** Details MUST显示基础scope/lifetime与独立Input Binding
- **AND** MUST不把该binding描述为ClientPredicted或InputDerived策略

#### Scenario: 查看 AI declaration

- **WHEN** 作者展开AIController或AITick declaration
- **THEN** Details MUST只显示AI owner、scope/lifetime、类型、默认值和category
- **AND** MUST不填充LocalOnly或None网络默认值

