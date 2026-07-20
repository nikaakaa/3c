## ADDED Requirements

### Requirement: 具体Authority Host Profile必须由Host Product拥有

neutral Simulation Core 与 ServerAuthoritative Model MUST只定义通用Program、ABI、Pipeline、Solver capability、protocol和Host product identity合同，MUST不枚举、构造或降低`UnityAuthorityWorker`、`DotRecastAuthorityScene`或未来具体Host Profile。Unity Authority Product与DotRecast Authority Product MUST分别拥有自己的Host Profile、launch lowering、solver capability声明与manifest fields。新增Authority backend MUST通过新增Product adapter接入，不得修改neutral Core或既有Product实现。

#### Scenario: 生成Unity Authority Worker产品

- **WHEN** Unity Authority Product准备worker launch和build manifest
- **THEN** Unity Product adapter MUST提供worker Host identity、Unity solver capability和launch lowering
- **AND** neutral Core MUST只校验通用identity/ABI/capability合同

#### Scenario: 生成DotRecast Authority产品

- **WHEN** DotRecast Product准备普通.NET scene host
- **THEN** DotRecast Product adapter MUST提供scene Host identity、DotRecast solver capability和launch lowering
- **AND** MUST不修改Core枚举、Core factory或Unity Product代码

#### Scenario: Client prediction与Authority使用不同Solver

- **WHEN** Client使用Unity prediction solver而Authority使用DotRecast solver
- **THEN** compatibility MUST分别校验prediction solver与authority backend所需能力
- **AND** MUST不要求两端SolverId相同或让客户端选择authority Host Profile

### Requirement: Host Product Identity迁移必须拒绝旧Core Profile

Host product identity、handshake和build manifest MUST消费Product-owned Profile。迁移后旧Core Profile schema、factory与reader MUST删除；旧manifest或混合新旧identity MUST明确失败，不得转换、猜测或选择默认Product。

#### Scenario: 旧UnityAuthority Core Profile进入新启动器

- **WHEN** launch或manifest仍携带已删除的Core-owned Profile schema
- **THEN** Product composition MUST在创建Session前失败并报告schema/product identity
- **AND** MUST不映射为当前Unity Authority Product
