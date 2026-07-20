# Design: GitHub 持续集成基座

## Context

项目从公司蓝盾环境转到个人 GitHub 仓库后，缺少统一的提交级执行入口。仓库同时包含 Unity 客户端、普通 .NET portable Simulation 工具、Fantasy Server 骨架和 OpenSpec 文档，但它们的成熟度与当前业务价值不同。第一条 CI 必须只覆盖已有、可在干净机器复现、不会绕过项目规则的正式入口。

当前可复现边界是：

1. Git 索引中的仓库结构与 Unity `.meta` 关系。
2. OpenSpec 0.23.0 对 current specs 与 active changes 的严格校验。
3. .NET 8 对 `ThirdPersonSimulation.Core -> ThirdPersonSimulation.Float32` 的 ProjectReference 编译链及其公共 portable 合同。

Unity 客户端的 `.sln`/`.csproj` 由 Editor 生成且不应进入 Git；干净 runner 没有这些文件。Unity Player 构建、EditMode/PlayMode 测试和未来 AI 实机测试都需要启动 Unity，并与当前禁止 batchmode 的规则冲突。Fantasy Server 只是后续网络压力场景骨架，不是当前作品主交付物。

## Goals

- 每次候选提交都自动回答“仓库结构是否健康、OpenSpec 是否合法、portable Simulation 是否仍能编译并满足首批稳定合同”。
- 本地与云端使用同一仓库策略脚本和同一 portable 编译入口。
- 三类检查可以并行，失败结果清晰归属到一个职责。
- 让未来 AI 审查或 AI 测试可以消费稳定 job 结论，而不在本 change 预装 AI 运行时。
- 不引入 Unity、发布、凭据、缓存或自建 runner 的维护面。

## Non-Goals

- 不验证 Unity Editor 能否完整导入工程。
- 不验证 Gameplay 手感、动画表现、场景运行或 Player 启动。
- 不提供 Unity Test Framework、集成测试、性能测试或覆盖率指标。
- 不提供可下载游戏包或部署目标。

## Decisions

### 1. 直接使用 GitHub Actions，不建立平台无关 Pipeline DSL

仓库当前唯一远端是 GitHub，个人项目没有迁移到蓝盾、GitLab 或 Jenkins 的业务需求。GitHub 原生 workflow 能直接显示 PR 检查、提交状态和日志，维护面最小。

平台无关包装会额外引入一层脚本编排、凭据和行为映射，却不能提升 Gameplay 作品交付。若未来真实迁移托管平台，应整体替换 workflow adapter，但继续复用 `Tools/CI` 中的正式脚本；本 change 不保留双平台配置。

### 2. 使用一个 workflow 和三个并行 job

workflow 只承担触发、环境准备和 job 编排：

```text
pull_request / push main / workflow_dispatch
  ├─ repository-policy
  ├─ openspec-validation
  └─ portable-unit-tests
```

三个 job 没有前后依赖，任何一个失败都使 workflow 失败。职责分离能让开发者直接看出是仓库文件、规格还是 portable 单元测试问题；未来 AI 消费结果时也不需要解析一段混合日志。

不为三个 job 建立第四个自定义汇总服务。GitHub workflow conclusion 是唯一总结果，job conclusion 是唯一分类结果。

### 3. 第一阶段统一使用显式 Windows 托管 runner

作品正式目标是 Windows Unity 客户端，仓库 portable 工程目前使用 Windows 风格的 MSBuild source path。第一阶段选择受 GitHub 支持的显式 Windows runner image，减少路径、大小写和 PowerShell 行为差异。

Linux runner 的云端成本通常更低，也更适合证明 portable source 的跨平台性，但当前业务目标不是发布 Linux Player。等 portable runtime 真正成为普通 .NET 服务端依赖后，可以通过独立 change 把同一编译入口扩展为有业务意义的平台矩阵；本 change 不提前增加矩阵。

### 4. 仓库策略只检查 Git 索引，不扫描未跟踪工作文件

CI 评价的是候选提交，不应该因为开发者本地尚未提交的资产或生成文件改变结果。`Test-RepositoryPolicy.ps1` 以 `git ls-files` 为唯一文件清单，执行以下规则：

- `ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json` 与 `Packages/packages-lock.json` 必须被跟踪。
- Assets 下每个被跟踪的非 `.meta` 文件必须存在被跟踪的对应 `.meta`。
- Assets 下每个被跟踪的 `.meta` 必须对应被跟踪文件或至少一个被跟踪的目录后代。
- Unity `Library`、`Temp`、`Obj`、`Logs`、`UserSettings`、客户端 `Build/Builds` 和普通 .NET `bin/obj/artifacts/publish` 不得被跟踪。
- Unity 客户端生成的 `.sln`/`.csproj` 不得被跟踪。
- `3cDemo/Server` 下的正式 `.sln`/`.csproj` 与 `Tools/ThirdPersonSimulation.Portable` 下的正式 `.csproj` 是明确允许项，不通过宽泛扩展名例外放行其它路径。

脚本收集全部违规项后一次输出并返回非零退出码，避免开发者逐次修复一个问题。它不自动删除、生成 `.meta` 或修改索引。

### 5. OpenSpec 工具链固定版本

OpenSpec job 固定 Node 20.19.0 与 `@fission-ai/openspec` 0.23.0，并执行：

```text
openspec validate --all --strict --no-interactive
```

固定版本保证本地当前已通过的 58 个项目与云端使用相同解析规则。后续升级 OpenSpec 必须显式修改正式版本，先在当前 specs/changes 上完成严格校验，不允许自动漂移到最新版本或失败后调用 runner 全局版本。

### 6. portable unit tests 通过唯一 Tests project 闭合编译与测试

新增 `ThirdPersonSimulation.Tests.csproj`，目标框架固定为 .NET 8，使用 NUnit、NUnit3TestAdapter 与 Microsoft.NET.Test.Sdk 的显式固定版本，并只引用 Core 与 Float32。CI 从 Tests project 执行一次 `dotnet test`，由 ProjectReference 同时编译两个生产工程并运行测试，不再额外重复一个独立 portable build job。Reader 继续作为 artifact inspection 工具，但不是单元测试依赖。

测试必须使用：

```text
dotnet test --disable-build-servers /nr:false /p:UseSharedCompilation=false
```

无论 restore、build 或 test 成功失败，workflow 的独立清理步骤都必须执行 `dotnet build-server shutdown`。CI 不编译 Unity 生成工程，也不把本地已有 `.sln`/`.csproj` 当成正式输入。

### 7. 第一阶段只建立 portable .NET 单元测试

用户已明确要求先建立单元测试 CI。第一阶段只测试不依赖 Unity 的 portable 公共合同，首批范围为：

- CanonicalWriter/Reader 的 primitive round-trip、非有限数值、非法布尔、负长度、截断与 trailing bytes 拒绝。
- StableHash 的格式约束与相同输入稳定性，EventId 对 Program/Actor/Activation/Tick/Sequence/Channel identity 的稳定映射和差异敏感性。
- ProgramCurve 的 key canonical 排序、重复时间拒绝、空曲线 fallback、边界值、区间采样与 codec round-trip/非法 payload 拒绝。

这些测试对应 portable artifact、网络/回放 identity 与 Gameplay motion curve 的正式业务合同，不只验证 getter、构造函数或框架本身。测试 fixture 可以封装公共输入构造，但不得复制生产 hash、codec 或 curve 算法来计算期望值；可读性所需的固定 expected bytes/hash 必须明确保存为合同样本。

Unity Test Framework 当前实际解析为 1.4.6，可运行 EditMode 与 PlayMode，但它需要 Unity Editor 执行。该层必须在后续 change 中通过 UBA 正式接入，并先调整 batchmode 规则；不能让本 change 的 `.NET test` job 偷偷启动 Unity。Gameplay 实机测试仍必须走正式 Player 控制与观察边界。

### 8. 当前不建立 CD

CD 必须消费同一次提交产生、身份明确且已经通过 CI 的发布产物。当前禁止 Unity batchmode，也没有云端 Player build contract，所以不存在可供 CD 消费的正式游戏包。

本 change 不接受“本地手工打包后上传”“云端复用旧 Player”“仅上传 portable Reader DLL”作为游戏 CD。这些方案要么不可复现，要么发布的不是求职 Demo 主产品。后续 CD change 应从 Unity Player build、版本身份、产物保留策略和唯一发布目标一起开始。

### 9. 权限、并发与失败策略

- workflow 默认权限固定为 `contents: read`，不申请写仓库、PR、Release、Package 或 deployment 权限。
- 三个 job 分别设置有限超时，避免 runner 因工具挂起长期占用额度。
- concurrency key 按 workflow 与 ref 计算，`cancel-in-progress` 开启；同一分支只保留最新候选提交。
- 任一正式命令失败即失败，不重试另一个版本、不降级检查、不忽略退出码。
- portable unit tests 的 shutdown 与 TRX 上传属于无条件收尾；它们不能覆盖原始 restore、build 或 test 失败结论。

## Future Extension Boundary

未来 AI 云端测试不应直接修改本 workflow 中现有三个 job 的判定语义。它可以新增独立 `ai-review`、`scenario-exploration` 或 `player-observation` job，并读取当前提交、现有 job 结论与构建产物。只有确定性检查适合作为 required check；AI 结论在建立稳定评价标准前应保持建议性质。

未来 Unity/CD 链路必须是：

```text
同一提交通过基础 CI
  -> 正式 Unity Player build
  -> 产物身份与完整性校验
  -> 可选确定性/AI Player 测试
  -> 唯一发布目标
```

不得让 AI、自建 runner 或发布 workflow 绕开基础 CI，从其它分支、人工目录或旧构建中寻找产物。

## Risks

- Windows hosted runner 的执行成本高于 Linux。通过三个 job 并行、同 ref 取消旧运行、暂不使用矩阵控制成本。
- Git 索引式 `.meta` 检查可能暴露历史资产问题。脚本只报告所有违规，不自动修复；历史问题应在首次启用前一次性清理，不降低规则。
- OpenSpec 固定版本会要求主动升级。换来的是规格校验结果可复现，不受 npm 最新版本变化影响。
- portable unit tests 不能代表 Unity 工程已经完整编译或 Gameplay 已完成集成验证。workflow 与项目文档必须明确该边界，不能把绿色结果描述成完整游戏构建成功。
