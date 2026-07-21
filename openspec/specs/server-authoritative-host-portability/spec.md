# server-authoritative-host-portability Specification

## Purpose
定义 ServerAuthoritative Authority Pipeline、Source、Control Transport 和 Launch Request 的 host-neutral 边界，使 Unity 与普通 .NET Host 复用同一模型运行语义。
## Requirements
### Requirement: Authority Pipeline Catalog必须Host-Neutral

Authority Pipeline Pass顺序、config lowering、descriptor构造、Pass factory与product factory MUST位于portable ServerAuthoritative source set。Unity Definition MUST只降低authoring输入，MUST不拥有第二份descriptor或factory catalog。

#### Scenario: Unity Worker编译Authority Pipeline

- **WHEN** Unity Definition提交合法authoring字段
- **THEN** MUST由portable catalog产生descriptor与factory集合
- **AND** 迁移前后PipelineHash MUST相同

### Requirement: Authority Source Runtime必须Host-Neutral

每Actor command queue、authority clock、missing-input policy、每Client checkpoint baseline、snapshot sequence、reliable/full-checkpoint queue与typed Source ports MUST由portable Authority Source runtime唯一拥有。Unity与未来普通.NET Host MUST只提供transport adapter和显式launch输入。

#### Scenario: Source消费Command

- **WHEN** transport将已校验command写入Actor queue
- **THEN** portable Source MUST在outer tick边界消费并生成typed ingress
- **AND** transport MUST不执行Program或missing-input决策

### Requirement: Authority Control Transport必须只承载控制与可靠产品

Host-neutral control transport MUST只交换register、roster、ticket、heartbeat、reliable event、full checkpoint、leave和failure产品。Routine command/snapshot MUST继续使用唯一portable datagram endpoint，MUST不进入control transport或回退KCP gameplay stream。

#### Scenario: 发布Routine Snapshot

- **WHEN** Authority Egress生成routine snapshot
- **THEN** Source MUST通过portable datagram endpoint发送
- **AND** control transport MUST不接收该snapshot

### Requirement: Authority Host必须通过唯一Launch Request调用Portable Composer

Authority Host launch request MUST显式提供Program Runtime、Backend、Authority Pipeline、Source policy/ports、roster、WorldSolver、initial state、Committer、diagnostics和output routes，并调用唯一portable Float32 Composer。缺失或不兼容输入 MUST失败，不得选择默认组件或复制Composer。

#### Scenario: 普通.NET Host准备接入

- **WHEN** 后续Host提供完整portable launch输入
- **THEN** MUST可以在不引用Unity Definition的情况下调用同一launch request
- **AND** 当前change MUST不以空Worker或fallback证明该能力

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

