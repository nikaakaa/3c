## RENAMED Requirements

- FROM: `### Requirement: Agent Document必须完整表达 Action target authoring`
- TO: `### Requirement: Agent Document必须完整表达 Action target输入绑定`

## MODIFIED Requirements

### Requirement: Agent Document必须完整表达 Action target输入绑定

Character Document package MUST表达`ActionTargetSnapshot` Blackboard declaration、独立Input Binding及其InputValueId、准入与activation引用，以及ActionProfile的`None`、`OptionalSnapshot`或`SnapshotRequired`。Reconciler、handler与Validator MUST调用正式Blackboard declaration与Input Binding authoring API，不得按显示名猜引用、依赖InputDerived策略或形成第二个Action target入口。

#### Scenario: 为攻击建立目标链

- **WHEN** Document新增带Input Binding的ActionTargetSnapshot declaration并绑定Attack Profile、CanActivate与Activate
- **THEN** dry-run MUST验证全部引用属于当前Definition且类型匹配
- **AND** Mutation Plan MUST分别表达基础declaration与Input Binding
- **AND** MUST不要求blackboard authority或sync policy字段

## ADDED Requirements

### Requirement: Agent Character Document不得输出 Blackboard 网络策略元数据

Character Snapshot exporter、Document model、Package Mapper、Reconciler、Mutation、Validator与Report MUST使用同一新Blackboard schema。它们 MUST不输出、接收、推导或默认填充Authority、SyncPolicy、InputDerived、SyncFact、ReplicatedCue或CorrectionOnly。事实是否被网络模型消费 MUST只通过只读Network Model context或diagnostics表达，不得写回editable Blackboard。

#### Scenario: Agent编辑Attack HitWindow

- **WHEN** Agent修改Attack1Hit的WindowType、WindowId或Digest
- **THEN** Reconciler MUST只生成Fact Projection mutation
- **AND** MUST不生成网络策略或packet配置mutation

#### Scenario: Agent提交旧Blackboard字段

- **WHEN** editable package包含blackboard authority或syncPolicy
- **THEN** strict parser MUST返回精确文件与entity path
- **AND** MUST不把旧值映射为Input Binding、Fact Projection或Network Model配置

