# gameplay-network-test-build-workflow Specification

## Purpose
定义三个 Network Test Product 共用的显式适配、原子构建、精确产物闭包与 Build/Run 分离流程。
## Requirements
### Requirement: Network Test Product必须使用唯一Editor Build Workflow

Unity Authority、DotRecast Authority与Deterministic Rollback Network Test Product MUST通过唯一Editor-only `NetworkTestProductBuildWorkflow`编排构建。每个Product MUST由显式adapter提供product identity、Player scene/assets、server project或no-server形态、output root、manifest字段和launch script；workflow MUST统一进程执行、临时目录、原子替换、exact file closure与manifest校验。公共process、asset/identity与server manifest/hash能力 MUST由独立utility拥有。公共workflow MUST不引用具体Network Model runtime类型，具体Product adapter MUST不调用另一adapter的helper，也 MUST不按reflection、字符串查找或fallback发现adapter。

#### Scenario: 构建Unity Authority Product

- **WHEN** 作者执行Unity Authority Build命令
- **THEN** Unity Authority adapter MUST提供Unity worker、两个client、Fantasy server和四进程launch产品描述
- **AND** workflow MUST只写入Unity Authority专属output root

#### Scenario: 构建DotRecast Authority Product

- **WHEN** 作者执行DotRecast Authority Build命令
- **THEN** DotRecast adapter MUST提供两个Unity client、普通.NET Authority host和所需server产品描述
- **AND** workflow MUST不修改Unity Authority output或其adapter

#### Scenario: 构建Deterministic Rollback Product

- **WHEN** 作者执行Deterministic Rollback Build命令
- **THEN** adapter MUST明确声明no-Fantasy-server产品形态和对应Player/launch输入
- **AND** workflow MUST不伪造空server或复用Authority manifest字段

### Requirement: Network Test Build与Run必须完全分离

Build command MUST只生成并校验正式Product输出；Run command MUST只消费已经存在且manifest、product identity、scene list与exact file closure全部匹配的输出。Run MUST不隐式触发Build、重新编译Program、修复目录或选择其它Product。相同Product的新Build MAY原子覆盖自己的旧输出；不同Product MUST使用互斥目录且不得互相覆盖。候选与备份目录 MUST位于同一Network output parent并使用固定短identity，避免临时路径扩张破坏Windows Player深层文件读取；manifest唯一性 MUST按Product root下的完整路径判断。

#### Scenario: Run时缺少有效manifest

- **WHEN** Product output不存在、manifest schema过期或exact file closure不匹配
- **THEN** Run MUST在启动进程前明确失败
- **AND** MUST不自动Build、复制其它Product产物或继续启动部分进程

#### Scenario: 重建同一Product

- **WHEN** Unity Authority Product已有上一版完整输出并再次Build
- **THEN** workflow MUST先在临时目录完成全部构建和校验
- **AND** 临时目录 MUST保留不低于正式Product目录的深层文件路径预算
- **AND** 成功后 MUST原子替换Unity Authority正式目录
- **AND** 失败时 MUST不留下被部分覆盖的正式Product

### Requirement: 外部编译进程必须使用统一受控生命周期

Network Test Build Workflow 调用dotnet或msbuild时 MUST包含`--disable-build-servers`、`/nr:false`与`/p:UseSharedCompilation=false`，并在每次编译完成或失败后立即执行`dotnet build-server shutdown`。workflow MUST捕获command、working directory、exit code、stdout和stderr；非零exit code或shutdown失败 MUST使当前Build明确失败，不得继续发布输出。

#### Scenario: Server项目编译失败

- **WHEN** DotRecast或Fantasy server build返回非零exit code
- **THEN** workflow MUST记录对应Product、command、working directory和stderr
- **AND** MUST执行build-server shutdown
- **AND** MUST不替换正式output root或启动Run

### Requirement: Product Manifest必须证明精确产物闭包

每个Network Test Product manifest MUST记录product identity、build schema、Program/Pipeline/Host identity、Player scene清单、server产品形态、launch script和exact file closure。Build完成后workflow MUST从正式候选目录重新读取manifest并核对文件集合；未声明文件、缺失文件、混合其它Product identity或旧schema MUST失败。

#### Scenario: DotRecast目录混入Unity Authority Worker

- **WHEN** DotRecast候选输出包含未声明的Unity Authority Worker文件或identity
- **THEN** exact closure validation MUST拒绝发布
- **AND** MUST不通过忽略额外文件或修改manifest掩盖混合产物

