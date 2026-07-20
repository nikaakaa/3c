# Change: 建立 GitHub 持续集成基座

## Why

仓库当前只有本地编译和 OpenSpec 校验习惯，没有提交后自动执行的统一检查，也没有针对 portable Simulation 正式合同的单元测试工程。个人开发时，一次提交是否缺少 Unity `.meta`、是否误提交生成目录、是否破坏 canonical codec、稳定身份、Program Curve 或 portable 编译、是否让 active change 或 current spec 失去严格合法性，都要等开发者主动发现。随着 Simulation Core、Session composition 和后续网络模型并行演进，这些问题会在提交之间积累，并把本应局部的错误拖到后续玩法开发阶段。

仓库唯一远端是 GitHub，主分支是 `main`；当前已有三项不依赖 Unity Editor 的正式验证边界：Git 索引可以验证仓库结构，OpenSpec CLI 可以严格验证 specs/changes，`Tools/ThirdPersonSimulation.Portable` 可以用普通 .NET 8 编译并直接测试 Core 与 Float32 公共合同。应在该 portable source set 旁建立唯一 NUnit 测试工程，让第一条持续集成链路同时执行编译和确定性单元测试，而不进入 Gameplay authoring、Unity 场景或表现运行时。

当前项目同时明确禁止 Unity batchmode。Unity 自动导入、Unity Test Framework、Player 构建和自动发布都依赖一个尚未批准的 Unity 云端执行边界。没有正式 Player 构建产物时直接建立 CD，只能产生人工上传、复用旧构建或跳过编译的分裂发布路径。因此本 change 只建立可复现的 GitHub CI 基座，不伪造 CD；正式 Player 构建、AI 实机测试和发布必须在后续 change 中同时解决 Unity 执行环境、构建产物身份与发布目标。

## Dependencies

- 本 change 不依赖任何 Gameplay active change，也不修改 Simulation、Character、Networking、BTSMTL 或 Unity 资产。
- `refactor-simulation-operation-runtime-modules` 与 `refactor-gameplay-session-composition-boundary` MAY继续独立实施；CI 只消费它们提交后的 portable source 与 OpenSpec 文档，不取得业务所有权。
- 后续 Unity 自动构建、自动测试、AI 实机测试或发布 change MUST先明确修改“不运行 Unity batchmode”的项目规则，不能绕过本 change 私建另一条云端入口。

## What Changes

- 新增唯一 GitHub Actions CI workflow，在 `pull_request`、`main` 分支 `push` 与显式手动触发时运行。
- 将基础检查拆成三个互不依赖、可并行、名称稳定的 job：仓库策略检查、OpenSpec 严格校验、portable Simulation 单元测试。任一 job 失败时整次 CI 失败。
- 新增唯一仓库策略脚本，以 Git 索引而不是本地未跟踪文件为输入，检查 Unity Project/Packages 关键文件、Assets 文件与 `.meta` 配对、禁止提交的 Unity/.NET 生成路径，以及客户端生成 `.sln`/`.csproj`；正式服务端工程与 portable 工具工程保留为明确允许项。
- OpenSpec job 固定 Node 与 `@fission-ai/openspec` 版本，并执行 `openspec validate --all --strict --no-interactive`，不依赖 runner 的全局工具状态。
- 新增 `ThirdPersonSimulation.Tests` .NET 8 NUnit 工程，只引用 portable Core 与 Float32，不引用 UnityEngine、UnityEditor、场景、资产或生成的 Unity project。
- 首批测试固定覆盖 canonical primitive 读写与非法 payload、StableHash/EventId 稳定身份、ProgramCurve 排序/重复时间拒绝/边界采样/codec round-trip；测试只调用公共合同，不通过 `InternalsVisibleTo`、反射或复制生产算法读取实现细节。
- portable unit test job 从唯一 Tests project 执行 `dotnet test`，通过 ProjectReference 同时完成 Core 与 Float32 编译；命令必须携带 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`，并在成功或失败后都执行 `dotnet build-server shutdown`。
- portable unit test job 生成 TRX 结果并以短期 GitHub Artifact 保存，测试失败时仍保留结果供定位；TRX 不成为第二份测试真相或发布产物。
- workflow 使用只读仓库权限、每个 job 的明确超时和按 ref 的并发取消；同一分支出现更新提交时取消旧运行，不保留过期结果继续占用云端资源。
- CI 日志与 job 结论使用 GitHub Actions 正式结果面；portable test 的 TRX 使用 GitHub Artifact 短期保存，不增加第二套报告数据库、通知服务或自建 runner。
- 更新 `openspec/project.md`，记录 GitHub CI 与 portable .NET 单元测试的正式边界，以及当前没有 Unity 自动构建、Unity Test Framework、AI 实机测试和 CD 的事实。

## Non-Goals

- 不启动 Unity Editor，不运行 Unity batchmode，不编译 Unity 生成的客户端 `.sln`/`.csproj`，不执行 Asset import 或 Player build。
- 不新增或运行 Unity EditMode、PlayMode、Unity Player 测试、集成测试、性能测试或覆盖率任务。
- 不构建 Fantasy Server、Hotfix、YooAsset Bundle、HybridCLR、Addressables 或其它发布产物。
- 不创建 GitHub Release，不上传人工生成的 Player，不部署网站、对象存储或试玩平台，因此本 change 不宣称提供 CD。
- 不接入 AI 代码审查、AI 测试生成、AI 实机操作、视觉检查或自动修复。
- 不增加自建 runner、Unity License secret、通知 secret、缓存、矩阵、多平台 runner 或失败 fallback。
- 不修改 GitHub 远端分支保护、required checks 或仓库权限；这些属于仓库治理的外部配置，不在本地基础 change 中隐式执行。

## Current Spec Comparison

- 当前 53 个 current spec 中没有 repository CI、构建编排或发布能力，本 change 新增独立 `repository-ci-foundation` capability，不修改 Gameplay、Simulation、Presentation、Networking 或 BTSMTL requirements。
- `btsmtl-compiled-simulation-program` 已要求普通 .NET assembly 能读取 portable Program，`Tools/ThirdPersonSimulation.Portable` 已提供 Core、Float32 与 Reader 的正式编译入口。本 change 只自动调用该入口，不把它改成第二份 source set，也不改变 Program ABI、artifact 或 runtime。
- `btsmtl-gameplay-semantic-ir` 要求普通 .NET 项目复用同一 portable Core source set。本 change 的 portable unit tests 可持续暴露该 source set 的编译与合同破坏，不执行 Unity authoring Frontend，也不生成 `.csir` 或 `.csim`。
- `openspec/project.md`、根 `AGENTS.md` 与 `openspec/AGENTS.md` 都明确禁止 Unity batchmode。本 change 与该约束一致；因此它不能同时提供 Unity 自动测试、Player build 或 CD。
- 当前五个 active change 均通过 strict validation。本 change 将现有手动校验提升为提交级检查，不改变它们的依赖、实施顺序或任务状态。
- 当前 specs 与本提案没有语义矛盾，也没有需要删除、重命名或合并的旧 CI spec。

## Impact

- 新增：`.github/workflows/ci.yml`。
- 新增：`Tools/CI/Test-RepositoryPolicy.ps1`。
- 新增：`Tools/ThirdPersonSimulation.Portable/ThirdPersonSimulation.Tests` 及其 NUnit 单元测试。
- 更新：`openspec/project.md` 中的当前状态、技术栈与执行约束。
- 云端资源：每次 PR、`main` push 或手动触发启动三个可并行 Windows job；同一 ref 只保留最新运行；portable unit test job 保存短期 TRX artifact。
- Gameplay/runtime/资产：无修改。
- 发布：无 Player 产物、无 Release、无部署。
