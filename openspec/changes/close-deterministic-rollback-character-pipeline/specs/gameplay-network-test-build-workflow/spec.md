## MODIFIED Requirements

### Requirement: Network Test Product必须使用唯一Editor Build Workflow

DeterministicRollback Product adapter MUST从精确Gameplay Lab Rollback Variant取得Player Scene、Composition、Fixed Program、Presentation Projection、KCC与Collision Artifact identity，再交给唯一`NetworkTestProductBuildWorkflow`完成Player、Relay、manifest、exact closure与原子发布。Adapter MUST不调用Character Compiler、Pose Compiler、Collision Baker、资产迁移器或Gameplay Lab Asset Builder，也不得按selection、场景名或目录扫描猜输入。

#### Scenario: 构建Rollback Product

- **WHEN** 作者显式执行DeterministicRollback Build
- **THEN** adapter MUST先验证精确Variant的全部已发布identity
- **AND** workflow MUST只打包已验证输入并原子发布Product

#### Scenario: Variant引用stale产品

- **WHEN** Variant的Program、Projection、KCC或Collision identity与当前正式闭包不一致
- **THEN** Build MUST在启动Unity Player构建前失败
- **AND** MUST不自动重建或改写Variant
