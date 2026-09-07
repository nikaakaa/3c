# gameplay-network-test-build-workflow Specification

## Purpose

定义三个 Network Test Product 共享的 Editor Build Workflow，以干净源码、schema v3、精确产物与工具闭包发布不可变 Candidate，并与 Run 实例创建分离。

## Requirements
### Requirement: Network Test Product必须使用唯一Editor Build Workflow

Unity Authority、DotRecast Authority与Deterministic Rollback MUST继续通过唯一Editor-only `NetworkTestProductBuildWorkflow`构建。Build request MUST包含显式CandidateLabel；公共workflow MUST统一Git源码身份、schema v3 manifest、Player构建、runtime artifact、Tool Bundle、Session Plan、staging、exact closure与不可变Candidate发布。每个Product adapter MUST只提供产品身份、Player输入、runtime artifacts、Tool Bundle扩展和Session Plan，不得调用另一adapter helper。公共workflow MUST不引用具体Network Model runtime类型、不按ProductId分支、不反射或fallback发现adapter。

#### Scenario: 构建Rollback Candidate

- **WHEN** 作者以合法CandidateLabel执行DeterministicRollback Build
- **THEN** Rollback adapter MUST提供Player、Relay、GM和对应Session Plan/Tool Bundle描述
- **AND** 公共workflow MUST按同一Candidate合同完成构建和发布

#### Scenario: 构建Authority Candidate

- **WHEN** 作者构建Unity Authority或DotRecast Authority Candidate
- **THEN** 对应adapter MUST提供其精确Server Product和candidate-owned启动adapter
- **AND** 公共workflow MUST不引入Rollback或GM产品分支

#### Scenario: 构建Unity Authority Product

- **WHEN** 作者执行Unity Authority Candidate Build命令
- **THEN** Unity Authority adapter MUST提供Unity worker、两个client、Fantasy server和四进程launch产品描述
- **AND** workflow MUST只写入Unity Authority专属Product根下的新Candidate目录

#### Scenario: 构建DotRecast Authority Product

- **WHEN** 作者执行DotRecast Authority Candidate Build命令
- **THEN** DotRecast adapter MUST提供两个Unity client、普通.NET Authority host和所需server产品描述
- **AND** workflow MUST不修改Unity Authority output或其adapter

#### Scenario: 构建Deterministic Rollback Product

- **WHEN** 作者执行Deterministic Rollback Candidate Build命令
- **THEN** adapter MUST明确声明no-Fantasy-server产品形态和对应Player/launch输入
- **AND** workflow MUST不伪造空server或复用Authority manifest字段

### Requirement: Network Test Build与Run必须完全分离

Build MUST只从干净源码生成并校验不可变Candidate；Run MUST只消费显式Candidate、Tool Bundle、Session Plan和Slot创建Run实例。Run MAY生成本次RunManifest、endpoint、token、PID和日志配置，但 MUST不触发Unity Build、dotnet publish、Program/Projection生成、Candidate修复或候选选择。相同CandidateId再次Build MUST失败；不同Candidate MUST并存。旧同产品覆盖、backup替换、默认当前Product和StopExisting语义 MUST删除。

#### Scenario: Run时缺少有效manifest

- **WHEN** 显式Candidate缺少文件、manifest过期或hash不匹配
- **THEN** Run MUST在创建Run目录和启动进程前失败
- **AND** MUST不重新Build、复制另一Candidate或改写manifest

#### Scenario: 新建Run实例

- **WHEN** Candidate和Slot全部合法
- **THEN** Run MUST只在RunLogs下创建本次实例配置并启动Candidate-owned Orchestrator
- **AND** Candidate目录 MUST保持exact-byte不变

#### Scenario: 重建同一Product

- **WHEN** 同一 Product 已有合法 Candidate，作者再次 Build
- **THEN** 相同 CandidateId MUST在写入前失败；新的 CandidateId MUST在独立 staging 完成全部构建与校验后原子发布
- **AND** staging MUST保留正式路径预算，失败 MUST不损坏任何已发布 Candidate

### Requirement: 外部编译进程必须使用统一受控生命周期

Network Test Build Workflow 调用dotnet或msbuild时 MUST包含`--disable-build-servers`、`/nr:false`与`/p:UseSharedCompilation=false`，并在每次编译完成或失败后立即执行`dotnet build-server shutdown`。workflow MUST捕获command、working directory、exit code、stdout和stderr；非零exit code或shutdown失败 MUST使当前Build明确失败，不得继续发布输出。

#### Scenario: Server项目编译失败

- **WHEN** DotRecast或Fantasy server build返回非零exit code
- **THEN** workflow MUST记录对应Product、command、working directory和stderr
- **AND** MUST执行build-server shutdown
- **AND** MUST不替换正式output root或启动Run

### Requirement: Product Manifest必须证明精确产物闭包

每个Network Test Candidate manifest MUST使用schema v3记录CandidateId、CandidateLabel、SourceCommit、SourceTreeHash、Product/Model/Topology、Program/Pipeline/Projection/World身份、runtime artifacts、Tool Bundles、Session Plan、Player配置和exact file closure。Build完成后workflow MUST从最终Candidate目录重新读取并严格核对全部身份。schema v2、时间BuildId、未声明文件、缺失文件、混合Product或工具hash不匹配 MUST失败，系统 MUST不提供兼容reader。

#### Scenario: Candidate混入另一版GM

- **WHEN** Rollback Candidate中的GM Tool Bundle来自另一Candidate或CommandCatalogHash不匹配
- **THEN** Candidate validation MUST拒绝正式发布或Run
- **AND** MUST不只校验Player/Relay后忽略工具差异

#### Scenario: DotRecast目录混入Unity Authority Worker

- **WHEN** DotRecast Candidate包含未声明的Unity Authority Worker文件、artifact或工具身份
- **THEN** exact closure validation MUST拒绝发布
- **AND** MUST不通过忽略额外文件或修改manifest掩盖混合产物
