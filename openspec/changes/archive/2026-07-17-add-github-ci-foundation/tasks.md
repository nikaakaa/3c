# Tasks

## 1. 建立仓库策略入口

- [x] 1.1 创建 `Tools/CI` 目录并确定唯一仓库策略脚本路径 `Tools/CI/Test-RepositoryPolicy.ps1`。
- [x] 1.2 在仓库策略脚本中定位 Git 根目录，并在无法定位仓库时明确失败。
- [x] 1.3 使用 `git ls-files` 建立唯一被跟踪文件清单，不读取未跟踪文件作为判定输入。
- [x] 1.4 校验 Unity `ProjectVersion.txt`、`Packages/manifest.json` 与 `Packages/packages-lock.json` 均被跟踪。
- [x] 1.5 校验 Assets 下被跟踪的普通文件均有被跟踪的同名 `.meta`。
- [x] 1.6 校验 Assets 下被跟踪的 `.meta` 均对应被跟踪文件或具有被跟踪后代的目录。
- [x] 1.7 拒绝被跟踪的 Unity 生成目录、客户端构建目录和普通 .NET 输出目录。
- [x] 1.8 拒绝被跟踪的 Unity 客户端生成 `.sln` 与 `.csproj`。
- [x] 1.9 只为 `3cDemo/Server` 的正式 solution/project 与 `Tools/ThirdPersonSimulation.Portable` 的正式 project 建立精确允许项。
- [x] 1.10 汇总全部违规路径后统一输出，并以稳定非零退出码结束。
- [x] 1.11 保持脚本只读，不生成 `.meta`、不删除文件、不修改 Git 索引。

## 2. 建立 GitHub Actions workflow 外壳

- [x] 2.1 创建唯一 workflow 文件 `.github/workflows/ci.yml`。
- [x] 2.2 配置 `pull_request`、`main` push 与 `workflow_dispatch` 三种正式触发入口。
- [x] 2.3 配置 workflow 级 `contents: read` 权限，不申请任何写权限。
- [x] 2.4 配置按 workflow/ref 分组的 concurrency，并取消同 ref 的旧运行。
- [x] 2.5 为 `repository-policy`、`openspec-validation` 与 `portable-unit-tests` 建立三个无依赖并行 job。
- [x] 2.6 为三个 job 选择同一个显式受支持 Windows runner image。
- [x] 2.7 为三个 job 分别设置有限超时。
- [x] 2.8 为三个 job 使用稳定显示名，保证 PR 检查和未来消费者可以按职责识别。

## 3. 接入仓库策略检查

- [x] 3.1 在 `repository-policy` job 中 checkout 当前候选提交。
- [x] 3.2 在 `repository-policy` job 中使用 PowerShell 7 调用唯一仓库策略脚本。
- [x] 3.3 保留仓库策略脚本的原始退出码，使任一违规直接使 job 失败。

## 4. 接入 OpenSpec 严格校验

- [x] 4.1 在 `openspec-validation` job 中 checkout 当前候选提交。
- [x] 4.2 在 `openspec-validation` job 中固定安装 Node 20.19.0。
- [x] 4.3 在 `openspec-validation` job 中固定使用 `@fission-ai/openspec` 0.23.0。
- [x] 4.4 执行 `openspec validate --all --strict --no-interactive`，不调用 runner 全局 OpenSpec 或其它版本 fallback。
- [x] 4.5 保留 OpenSpec 原始退出码，使任一 invalid spec/change 直接使 job 失败。

## 5. 建立并接入 portable Simulation 单元测试

- [x] 5.1 创建 `Tools/ThirdPersonSimulation.Portable/ThirdPersonSimulation.Tests/ThirdPersonSimulation.Tests.csproj`。
- [x] 5.2 将测试工程固定为 .NET 8，并显式固定 NUnit、NUnit3TestAdapter 与 Microsoft.NET.Test.Sdk 版本。
- [x] 5.3 只引用 `ThirdPersonSimulation.Core` 与 `ThirdPersonSimulation.Float32`，不引用 Reader、UnityEngine、UnityEditor 或 Unity 生成工程。
- [x] 5.4 建立 CanonicalData 测试 fixture，覆盖 primitive round-trip 与非法 boolean、double、length、truncated、trailing payload。
- [x] 5.5 建立 SimulationIdentity 测试 fixture，覆盖 StableHash 格式/稳定性与 EventId identity 输入差异。
- [x] 5.6 建立 ProgramCurve 测试 fixture，覆盖 canonical key order、duplicate time、fallback、boundary、interpolation 与 codec round-trip/非法 payload。
- [x] 5.7 保证测试只调用生产公共合同，不新增 `InternalsVisibleTo`、反射访问或生产算法副本。
- [x] 5.8 在 `portable-unit-tests` job 中 checkout 当前候选提交。
- [x] 5.9 在 `portable-unit-tests` job 中固定安装 .NET 8 SDK。
- [x] 5.10 从唯一 Tests project 执行 `dotnet test`，通过 ProjectReference 闭合 Core 与 Float32 编译。
- [x] 5.11 为 `dotnet test` 添加 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`。
- [x] 5.12 生成 TRX 结果，并在测试成功或失败时以短期 GitHub Artifact 上传。
- [x] 5.13 不编译 Unity 生成 `.sln`/`.csproj`、Fantasy Server、Reader 或其它非本 change 项目。
- [x] 5.14 新增无条件清理步骤，在 restore、build 或 test 成功失败后执行 `dotnet build-server shutdown`。
- [x] 5.15 保证 shutdown 与 artifact 上传结果不覆盖原始 restore/build/test 失败结论。

## 6. 更新项目上下文

- [x] 6.1 在 `openspec/project.md` Current State 中记录 GitHub CI 的三个正式 job 与 portable NUnit 测试范围。
- [x] 6.2 在 `openspec/project.md` Tech Stack 中记录 GitHub Actions、固定 OpenSpec/Node、.NET 8 与 NUnit portable test 边界。
- [x] 6.3 在 `openspec/project.md` Conventions 中明确绿色 CI 不代表 Unity Editor import、Unity Test Framework、Player build 或 Gameplay 实机测试通过。
- [x] 6.4 记录当前没有 CD、Unity 云端构建、EditMode/PlayMode、AI 审查或 AI 实机测试，不创建占位配置。

## 7. 完成自动校验

- [x] 7.1 在当前 Git 索引上执行 `Tools/CI/Test-RepositoryPolicy.ps1` 并修复全部正式规则错误。
- [x] 7.2 执行固定版本 OpenSpec 的 `validate --all --strict --no-interactive`。
- [x] 7.3 使用 `dotnet test --disable-build-servers /nr:false /p:UseSharedCompilation=false` 运行 portable Tests project。
- [x] 7.4 portable 测试结束后立即执行 `dotnet build-server shutdown`。
- [x] 7.5 检查最终 diff，确认没有 Unity batchmode、Unity Test Framework、Player build、Release、deployment、secret、cache、自建 runner 或 AI 占位入口。
- [x] 7.6 执行 `openspec validate add-github-ci-foundation --strict --no-interactive`。
