# repository-ci-foundation Specification

## Purpose
定义仓库 GitHub CI 对候选提交执行并行 Repository Policy、OpenSpec 严格校验和 portable 单元测试的只读基础合同。
## Requirements
### Requirement: GitHub CI 必须覆盖正式候选提交

系统 MUST以唯一 GitHub Actions workflow 响应 pull request、`main` 分支 push 与显式手动触发。每次运行 MUST checkout 对应候选提交并生成 GitHub 原生 workflow/job 结论，MUST不从本地目录、其它分支或旧运行复用待检查代码。

#### Scenario: Pull request 更新提交

- **WHEN** pull request 的 head commit 发生变化
- **THEN** CI MUST针对新的候选提交启动运行
- **AND** MUST不把旧 commit 的成功结论视为新 commit 已通过

#### Scenario: 主分支收到提交

- **WHEN** 提交进入 `main`
- **THEN** CI MUST对该提交执行同一组正式检查

#### Scenario: 开发者手动触发

- **WHEN** 开发者通过 GitHub Actions 显式触发 workflow
- **THEN** CI MUST检查所选 ref 的当前提交
- **AND** MUST不改变检查内容或放宽失败规则

### Requirement: 基础检查必须按职责并行且统一决定 CI 结果

workflow MUST提供名称稳定且互不依赖的 repository policy、OpenSpec validation 与 portable unit tests job。三个 job MUST可以并行执行；任一 job 失败时，本次 workflow MUST失败。workflow MUST不通过忽略退出码、调用较弱命令或执行替代 job 把失败改为成功。

#### Scenario: OpenSpec 校验失败但 portable unit tests 成功

- **WHEN** portable unit tests 成功而 OpenSpec validation 返回失败
- **THEN** 本次 workflow MUST失败
- **AND** repository policy 与 portable unit tests 的独立结果 MUST仍可查看

#### Scenario: 同一分支快速提交两次

- **WHEN** 同一 ref 的旧 CI 尚未结束且出现更新提交
- **THEN** 系统 MUST取消旧运行并保留新提交运行
- **AND** MUST不让旧运行继续占用资源并产生候选结论

### Requirement: 仓库策略必须只验证被跟踪的正式文件

系统 MUST提供唯一只读 PowerShell 仓库策略脚本，并以 Git 索引中的被跟踪文件为判定输入。脚本 MUST验证 Unity Project/Packages 关键文件、Assets 文件与 `.meta` 配对、禁止的生成路径和客户端生成 project/solution 文件。脚本 MUST为正式 Fantasy Server solution/project 与 portable Simulation project 使用精确允许路径，MUST不使用宽泛扩展名 fallback。脚本 MUST汇总全部违规后返回非零退出码，MUST不自动修改工作区或索引。

#### Scenario: 被跟踪的 Unity 资产缺少 meta

- **WHEN** 候选提交包含被跟踪的 Assets 文件但没有被跟踪的同名 `.meta`
- **THEN** repository policy job MUST列出该资产并失败
- **AND** MUST不在云端生成 `.meta` 后继续

#### Scenario: 本地存在未跟踪开发文件

- **WHEN** 开发者本地工作区存在尚未纳入候选提交的普通文件
- **THEN** 仓库策略 MUST不把该文件作为候选提交违规
- **AND** CI 的判断 MUST只反映 Git 索引中的正式内容

#### Scenario: 正式服务端工程被跟踪

- **WHEN** 被跟踪的 `.sln` 或 `.csproj` 位于明确允许的 `3cDemo/Server` 正式路径
- **THEN** 仓库策略 MUST接受该文件
- **AND** MUST不因此允许 Unity 客户端生成工程进入仓库

### Requirement: OpenSpec validation 必须固定工具版本并严格覆盖全部当前项目

OpenSpec job MUST固定 Node 20.19.0 与 `@fission-ai/openspec` 0.23.0，并执行 `openspec validate --all --strict --no-interactive`。job MUST不依赖 runner 预装的全局 CLI，MUST不在失败时改用其它 OpenSpec 版本、非 strict 模式或部分 change 校验。

#### Scenario: Active change 不满足 strict schema

- **WHEN** 任一 active change 或 current spec 无法通过固定版本 strict validation
- **THEN** OpenSpec validation job MUST失败并保留原始诊断
- **AND** MUST不因为其它 change/spec 合法而返回成功

#### Scenario: npm 发布了新版 OpenSpec

- **WHEN** registry 中存在高于 0.23.0 的 OpenSpec 版本
- **THEN** CI MUST继续使用正式固定版本 0.23.0
- **AND** MUST不自动漂移校验语义

### Requirement: Portable unit tests 必须从唯一 Tests project 闭合编译与确定性合同

portable unit tests job MUST使用 .NET 8，从唯一 `ThirdPersonSimulation.Tests.csproj` 执行固定版本 NUnit 测试，并通过正式 ProjectReference 闭合 Float32 与 Core 编译。首批测试 MUST覆盖 canonical primitive/invalid payload、StableHash/EventId identity 与 ProgramCurve canonical/evaluation/codec 合同；MUST只调用生产公共合同，MUST不通过 `InternalsVisibleTo`、反射或生产算法副本建立测试路径。`dotnet test` MUST使用 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`，并且无论 restore、build 或 test 成功失败都 MUST执行 `dotnet build-server shutdown`。job MUST生成 TRX 并短期上传，MUST不依赖 Unity 生成的 solution/project 文件。

#### Scenario: Portable Core 出现编译错误

- **WHEN** Tests project 的 ProjectReference 链在 Core 或 Float32 任一工程出现编译错误
- **THEN** portable unit tests job MUST失败并报告原始编译错误
- **AND** MUST在失败后执行 build server shutdown

#### Scenario: Canonical 合同测试失败

- **WHEN** canonical payload、stable identity 或 ProgramCurve 的任一批准测试失败
- **THEN** portable unit tests job MUST失败并保留 TRX 结果
- **AND** MUST不通过更新测试期望值、忽略测试或降低断言把本次运行改为成功

#### Scenario: 干净 runner 没有 Unity 生成工程

- **WHEN** GitHub runner 只 checkout 被跟踪文件且不存在 Unity 生成的客户端 `.sln`/`.csproj`
- **THEN** portable unit tests MUST仍能从被跟踪的 Tests project 执行
- **AND** MUST不启动 Unity 生成缺失工程或 Unity Test Framework

### Requirement: 基础 CI 必须保持只读、有限并且没有发布副作用

workflow MUST只申请读取仓库内容所需的权限，为各 job 设置有限超时，并按 workflow/ref 取消过期运行。workflow MUST只运行本 spec 批准的 portable .NET 单元测试，MUST不使用 Unity batchmode、Unity EditMode/PlayMode、Unity Player 测试、集成测试、性能测试或覆盖率；MUST不创建 Player、Release、deployment 或 package，不上传人工构建，不读取 Unity License 或发布 secret，也 MUST不通过自建 runner、旧产物或其它 pipeline fallback 补齐这些能力。

#### Scenario: 基础 CI 全部通过

- **WHEN** repository policy、OpenSpec validation 与 portable unit tests 全部成功
- **THEN** workflow MUST只报告基础 CI 成功
- **AND** MUST不把结果描述为 Unity import、Unity Test Framework、Gameplay 实机测试、Player build 或 CD 成功

#### Scenario: Portable unit tests 长时间无响应

- **WHEN** portable unit tests 超过 job 的正式超时
- **THEN** GitHub Actions MUST终止该 job 并使 workflow 失败
- **AND** MUST不启动另一个无超时 runner 重试
