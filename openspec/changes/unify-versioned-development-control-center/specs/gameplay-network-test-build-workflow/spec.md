## MODIFIED Requirements

### Requirement: Network Test Product必须使用唯一Editor Build Workflow

Unity Authority、DotRecast Authority 与 DeterministicRollback MUST通过由原 Network Workflow 提升的唯一 `DevelopmentCandidateBuildWorkflow` 构建，与 Local Fixed 性能产品共享源码、工具、构建、文件校验和产物发布能力。每个产品 MUST由显式 adapter 提供产品身份、Player/附加 artifact、工具引用、配方和角色计划；公共工作流 MUST不引用具体 Network Model、不按 ProductId 分支或反射发现 adapter。交互 Editor 与后台 executeMethod MUST调用同一服务；原 Network/Performance 独立公共构建器 MUST删除。

#### Scenario: 构建Unity Authority Product

- **WHEN** 作者通过显式 Unity Authority 配方构建
- **THEN** adapter MUST声明已有 Server、Worker 与 Client 所需输入和角色
- **AND** 统一工作流 MUST只发布该产品的新候选

#### Scenario: 构建Deterministic Rollback Product

- **WHEN** 两种产品分别提交 Build 请求
- **THEN** 各自 adapter MUST提供真实产品能力，公共工作流 MUST统一管理版本与发布
- **AND** Local Fixed MUST不被迫携带 Relay、GM 或空 server 字段

#### Scenario: 构建DotRecast Authority Product

- **WHEN** 作者通过DotRecast Authority adapter提交构建
- **THEN** 它 MUST提供Unity Client、普通.NET Authority Host和正式Server产品描述
- **AND** 公共工作流 MUST不改写Unity Authority的候选或adapter

### Requirement: Network Test Build与Run必须完全分离

Build MUST只从固定干净源码与依赖构建不可变候选。Run MUST只消费明确 CandidateId、manifest/hash、工具、角色计划和 Slot，允许在本次 Runs/<RunId> 下生成 endpoint、token、进程与日志配置，但 MUST不编译、publish、生成 Program/Projection、修改候选或选择其他版本。旧同产品覆盖、backup 替换、当前版本猜测与 StopExisting MUST删除。

#### Scenario: Run时缺少有效manifest

- **WHEN** 候选不存在、Schema不支持、文件或工具引用不匹配
- **THEN** Run MUST在启动角色前失败
- **AND** MUST不自动构建、迁移或从其他工作区补文件

#### Scenario: 重建同一Product

- **WHEN** 相同 CandidateId 已经存在
- **THEN** 发布器 MUST验证已存在精确闭包后返回已有产物或报告内容冲突
- **AND** MUST不覆盖或合并目录

### Requirement: 外部编译进程必须使用统一受控生命周期

统一 Build Workflow 调用 dotnet/msbuild MUST包含 `--disable-build-servers`、`/nr:false` 与 `/p:UseSharedCompilation=false`，在完成或失败后立即执行 `dotnet build-server shutdown`。命令、工作区、退出码、stdout/stderr 与关闭结果 MUST进入 Build 记录。后台 Unity MUST受全机单构建和内存租约约束，完成或失败后退出；任何构建或关闭失败 MUST阻止发布，不终止其他任务进程。

#### Scenario: Server项目编译失败

- **WHEN** Server或工具构建返回非零退出码
- **THEN** Build MUST保留具体命令与错误并立即 shutdown 构建服务
- **AND** MUST不发布候选或隐式开始 Run

### Requirement: Product Manifest必须证明精确产物闭包

Network 产品 MUST使用开发候选 schema v4，记录 CandidateId、SourceId/Commit/Tree、Product/Model/Topology、构建配方/工具、业务 Program/Pipeline/Projection/World、runtime artifacts、精确工具引用与角色计划。最终 manifest/hash MUST封存全部候选文件与外部不可变依赖，读取时核对路径、文件集合和身份。旧 schema、混合产品、缺失/未声明文件 MUST失败，不提供兼容 Reader。

#### Scenario: DotRecast目录混入Unity Authority Worker

- **WHEN** DotRecast候选包含未声明的Unity Authority Worker
- **THEN** 发布 MUST失败并列出文件
- **AND** MUST不忽略文件或放宽闭包掩盖混合产物
