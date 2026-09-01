# Change: 建立版本化 Network Test 候选与并行会话编排

## Why

当前三个 Network Test Product 分别发布到一个固定目录，同产品下一次 Build 会原子替换上一版；`BuildId`由时间生成，只说明构建时刻，不能证明产物来自哪个 Git 提交。Run 又直接消费固定目录、固定端口和仓库中的启动脚本。并行任务即使分别完成代码，也无法同时保留精确 Player，更无法证明 GM、Relay、Player 和日志属于同一份源码候选。

已经完成的 `add-rollback-gm-console`只提供单场双端的独立文本 GM。它的协议、命令版本、Build/Session/Instance 身份和 exact closure 已闭合，但 GM 端口、Relay 查询端口、token 与 SessionId 在 Build 时写入产品，Run 只允许固定一场。用户现在需要的是比单场 GM 更高一层的本地 Test Orchestrator：从多个不可变候选中显式选择版本，为每次运行分配正式槽位，启动精确匹配的工具和进程，并让不同候选同时接受独立 GM 查询。

## What Changes

- 将 Network Test Product manifest 从 schema v2 升级为 schema v3，引入显式 `CandidateId + CandidateLabel + SourceCommit + SourceTreeHash`。CandidateLabel 由作者提供，Git 提交和树哈希由 Build 读取；时间只保留为展示元数据，不再充当版本。
- Network Test Build 在准备前后都要求精确 Git worktree 干净。若 Character Program、Projection、Scene 或其它被跟踪输入在 Prepare 阶段发生变化，Build 在 Player 构建前失败，要求作者先提交正式生成物后重新构建。
- 三个 Network Test Product 统一发布到 `Build/Network/<Product>/<CandidateId>`。候选一旦发布即不可覆盖；旧固定目录中的 schema v2 产物不再读取或迁移。
- schema v3 Product manifest 新增版本化 Tool Bundle 与产品 Session Plan。候选携带精确 Test Orchestrator、产品启动 adapter、GM 工具或其它开发工具及其版本、配置身份和文件哈希，不再依赖仓库当前启动脚本。
- 新增普通 .NET 8 `ThirdPerson.NetworkTest.Orchestrator`。每次 Session 由一个独立 Orchestrator 进程拥有，消费显式 Candidate 和 Session Slot，创建 `RunManifest`、运行配置、状态与日志，按产品 Session Plan 启动并只回收本次进程。
- 新增正式 Session Slot Catalog。槽位显式声明互不重叠的本机端口和窗口布局；槽位被占用时直接失败，不搜索备用端口。Deterministic Rollback 在本 change 中至少提供两个可并行槽位；Authority 产品迁移到同一候选与工具合同，但不宣称本轮已经支持多场 Authority 并行。
- Rollback 的 Gameplay endpoint、GM endpoint、Relay 查询 endpoint、token、RunId 与运行 SessionId 迁入 Run 目录。候选只保存静态模型、程序、拓扑、工具和容量策略；Player 不获得 GM 凭据，GM 继续只通过 Relay 窄查询桥读取一场事实。
- 为 GM 增加 `GmToolIdentity`：ToolId、ToolVersion、ProtocolVersion、CommandCatalogHash 与 ToolBundleHash。候选只启动自身携带且精确匹配的 GM，不使用全局最新工具，也不提供旧协议兼容。
- 将 Launcher 的固定三行 Build/Run 区升级为 Test Control Center：显式输入 CandidateLabel、列出已校验候选、选择 Candidate 和 Slot、启动/停止本次 Session、打开匹配 GM 和日志、显式删除未被运行引用的候选。
- 删除固定 ProductRoot Run、`StopExisting`、构建期运行 token/端口、仓库外部启动脚本依赖和 schema v2 reader，不保留 latest 指针、目录扫描修复或兼容入口。

## Scope

本 change 只建立本机版本化 Network Test 候选、工具身份和会话编排。Deterministic Rollback 保持两个 Unity Peer、一个 Dedicated Relay 与一个独立只读 GM 的既有 Gameplay 拓扑，只允许多个完整 Session 并行。Unity Authority 与 DotRecast Authority 保持现有角色和进程拓扑，本轮只迁移候选、工具和编排合同。

不实现 Git worktree 创建、分支管理、Unity batchmode、云端 Build Farm、远程机器编排、四客户端、图形化 GM 业务面板、玩法 GM、`scenario.run`、canonical GM 帧命令、客户端 IK/骨骼/FPS 查询、Replay 评分或 Performance Capture 调度。Performance Player 与 WPR/WPA Controller 继续属于独立 `Library/Performance` 工作流。

## Impact

- 新增能力 `network-test-session-orchestration`。
- 修改 `client-build-artifact-layout`、`gameplay-network-test-build-workflow`、`network-test-runtime-product-boundary`，把固定覆盖目录和 schema v2 替换为版本化候选与 schema v3。
- 修改 `server-authoritative-host-product-boundary` 与 `dotrecast-authoritative-server-backend` 中按时间 `BuildId`、固定当前目录和同产品替换语义。
- 修改 `deterministic-rollback-relay-product` 与 `deterministic-rollback-two-client-demo`，把构建期 endpoint/token/session 配置拆为 Candidate 静态身份和 Run 实例配置。
- 为尚未安装到 current specs 的 `rollback-gm-console`增加工具版本与 Run 绑定要求。本 change 以已完成的 `add-rollback-gm-console`实现为前置；实施前必须由用户验收并归档该基础 change，或先把本提案严格 rebase 到其最终已安装规格。
- 为 `repository-ci-foundation`增加 `Tools/ThirdPersonNetworkTest/ThirdPerson.NetworkTest.Orchestrator.csproj` 的精确允许项，不增加 CI job、Player Build 或集成测试。
- active `add-gameplay-performance-capture-workflow`已经完成并拥有 Launcher Performance 区。本 change 只迁移 Network Test 区，不复制其 Controller、MCP、Capture、Toolchain 或产物模型。
- active Foot、IK 与 Pose Graph change 不被本提案修改；它们未来只能以干净 checkpoint commit 进入 Candidate Build，不能用编译宏从混合工作区伪造独立版本。

## 与现行规格的对比

- `client-build-artifact-layout`当前强制三个固定 Network Product 根；本 change 改为固定 Product 根下的不可变 Candidate 子目录，RunLogs 继续留在 `Build/Network/RunLogs`。
- `gameplay-network-test-build-workflow`当前允许同产品新 Build 替换旧产物；本 change 改为同 Candidate 拒绝覆盖、不同 Candidate 并存。
- `network-test-runtime-product-boundary`当前固定 schema v2 且没有源码与工具身份；本 change 升级为 schema v3，旧 reader 直接删除。
- `server-authoritative-host-product-boundary`和`dotrecast-authoritative-server-backend`当前把 `BuildId=yyyyMMdd-HHmmss`作为产品身份；本 change 用 Git 可证明的 Candidate 身份替代，构建时间只作信息。
- `add-rollback-gm-console`当前设计明确 Build 发布端口和 token、Run 不生成配置；本 change 将运行配置迁入 Run 实例，同时保留 GM 命令、权限、异步查询和 Player 无工具凭据边界。

