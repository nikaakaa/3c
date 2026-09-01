## MODIFIED Requirements

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

### Requirement: Network Test Build与Run必须完全分离

Build MUST只从干净源码生成并校验不可变Candidate；Run MUST只消费显式Candidate、Tool Bundle、Session Plan和Slot创建Run实例。Run MAY生成本次RunManifest、endpoint、token、PID和日志配置，但 MUST不触发Unity Build、dotnet publish、Program/Projection生成、Candidate修复或候选选择。相同CandidateId再次Build MUST失败；不同Candidate MUST并存。旧同产品覆盖、backup替换、默认当前Product和StopExisting语义 MUST删除。

#### Scenario: Run时Candidate损坏

- **WHEN** 显式Candidate缺少文件、manifest过期或hash不匹配
- **THEN** Run MUST在创建Run目录和启动进程前失败
- **AND** MUST不重新Build、复制另一Candidate或改写manifest

#### Scenario: 新建Run实例

- **WHEN** Candidate和Slot全部合法
- **THEN** Run MUST只在RunLogs下创建本次实例配置并启动Candidate-owned Orchestrator
- **AND** Candidate目录 MUST保持exact-byte不变

### Requirement: Product Manifest必须证明精确产物闭包

每个Network Test Candidate manifest MUST使用schema v3记录CandidateId、CandidateLabel、SourceCommit、SourceTreeHash、Product/Model/Topology、Program/Pipeline/Projection/World身份、runtime artifacts、Tool Bundles、Session Plan、Player配置和exact file closure。Build完成后workflow MUST从最终Candidate目录重新读取并严格核对全部身份。schema v2、时间BuildId、未声明文件、缺失文件、混合Product或工具hash不匹配 MUST失败，系统 MUST不提供兼容reader。

#### Scenario: Candidate混入另一版GM

- **WHEN** Rollback Candidate中的GM Tool Bundle来自另一Candidate或CommandCatalogHash不匹配
- **THEN** Candidate validation MUST拒绝正式发布或Run
- **AND** MUST不只校验Player/Relay后忽略工具差异

#### Scenario: DotRecast Candidate混入Unity Authority Worker

- **WHEN** DotRecast Candidate包含未声明的Unity Authority Worker文件、artifact或工具身份
- **THEN** exact closure validation MUST拒绝发布
- **AND** MUST不通过忽略额外文件或修改manifest掩盖混合产物
