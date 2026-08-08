## ADDED Requirements

### Requirement: Target Program 必须只按 InputValueId 建立 Blackboard 输入绑定

Float32与Fixed Target Program lowering MUST遍历具有合法可选InputValueId的Blackboard declaration，并将Program input catalog kind、declaration identity与typed Character State address编译为唯一Blackboard input binding。Program layout MUST不读取SyncPolicy整数、Blackboard key、Network Model或Host配置来决定binding。两个Target MUST在Timeline Decision和Graph control之前应用同一binding顺序与失败语义。

#### Scenario: 双Target降低ActionTarget binding

- **WHEN** validated Semantic IR包含ActionTargetSnapshot declaration及其InputValueId
- **THEN** Float32与Fixed Program MUST各自建立相同业务identity的typed binding
- **AND** 两者 MUST不要求InputDerived枚举field

#### Scenario: declaration没有InputValueId

- **WHEN** Program catalog中的普通Blackboard declaration没有InputValueId field
- **THEN** Target lowering MUST不为其建立binding
- **AND** MUST不按缺失SyncPolicy猜测None或InputDerived

### Requirement: Blackboard catalog schema变化必须使旧Program失效

删除`ProgramCatalogFieldId.SyncPolicy`及改变可选InputValueId/Projection编码时，Semantic IR artifact、Target Program artifact、Program/Layout format、Compiler version与Float32/Fixed Target ABI MUST按实际合同提升。旧artifact、旧wrapper与旧Program catalog MUST在正式decode、build或Session preparation门禁失败；系统 MUST不保留旧field id、兼容reader、字段默认补值或运行时双ABI。

#### Scenario: Session加载旧Float32 Program

- **WHEN** Composition加载仍含SyncPolicy catalog field的旧Float32 Program
- **THEN** Session preparation MUST在创建runtime前拒绝Target ABI或artifact identity
- **AND** MUST不忽略该field继续运行

#### Scenario: Fixed Program重新发布

- **WHEN** Fixed Target从新Semantic IR重新生成Program
- **THEN** ProgramHash与LayoutHash MUST按新catalog合同重新计算
- **AND** 旧Fixed wrapper MUST不与新Program并存或被fallback选择

