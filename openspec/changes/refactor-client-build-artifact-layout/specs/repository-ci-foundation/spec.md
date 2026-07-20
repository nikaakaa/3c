## MODIFIED Requirements

### Requirement: 仓库策略必须只验证被跟踪的正式文件

系统 MUST提供唯一只读 PowerShell 仓库策略脚本，并以 Git 索引中的被跟踪文件为判定输入。脚本 MUST验证 Unity Project/Packages 关键文件、Assets 文件与 `.meta` 配对、禁止的生成路径和客户端生成 project/solution 文件。脚本 MUST将客户端 `Build`、旧 `Builds`、旧 `Bundles` 与 `HybridCLRData` 作为禁止跟踪的生成根，并 MUST为正式 Fantasy Server solution/project 与 portable Simulation project 使用精确允许路径，MUST不使用宽泛扩展名 fallback。脚本 MUST汇总全部违规后返回非零退出码，MUST不自动修改工作区或索引。

#### Scenario: 被跟踪的 Unity 资产缺少 meta

- **WHEN** 候选提交包含被跟踪的 Assets 文件但没有被跟踪的同名 `.meta`
- **THEN** repository policy job MUST列出该资产并失败
- **AND** MUST不在云端生成 `.meta` 后继续

#### Scenario: 客户端生成根被跟踪

- **WHEN** 候选提交包含位于客户端 `Build`、`Builds`、`Bundles` 或 `HybridCLRData` 下的文件
- **THEN** repository policy job MUST列出每个违规路径并失败
- **AND** MUST不因为文件是 version、manifest、DLL 或调试信息而接受

#### Scenario: 本地存在未跟踪开发文件

- **WHEN** 开发者本地工作区存在尚未纳入候选提交的普通文件或本机构建缓存
- **THEN** 仓库策略 MUST不把该文件作为候选提交违规
- **AND** CI 的判断 MUST只反映 Git 索引中的正式内容

#### Scenario: 正式服务端工程被跟踪

- **WHEN** 被跟踪的 `.sln` 或 `.csproj` 位于明确允许的 `3cDemo/Server` 正式路径
- **THEN** 仓库策略 MUST接受该文件
- **AND** MUST不因此允许 Unity 客户端生成工程进入仓库

