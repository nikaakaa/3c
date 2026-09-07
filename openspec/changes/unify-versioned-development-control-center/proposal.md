# Change: 统一开发测试 Center、版本记录与性能实验

## 2026-09-04 基线更新

`add-rollback-gm-console` 与 `add-versioned-network-test-orchestration` 已按用户指令归档；schema v3 Candidate、Tool Bundle、Slot 和 Run 现为 current specs。该状态不证明磁盘上的旧产物已经被重建。本提案仍是未实施的 schema v4／共享产物库方案，本次只更新继承关系和场景，不修改任务进度，也不执行后台 Unity 构建。

## Why

作者需要同时在多个 worktree 修改角色、动画或工具，并能回答：这次运行用了哪份代码、哪套采样工具、哪段输入；修改前后是否仍执行相同操作；性能差异是否来自业务改动。

现有 Network Test Control Center 已有源码提交、不可变候选、工具包和运行槽位代码，Performance Workflow 已实际完成一份独立 IL2CPP Player 的 Smoke、Replay、Capture。但两条链分别拥有构建身份、输出目录、进程和状态，不能形成统一实验记录。当前主工作区 Network 目录仍是旧 schema 1/2 产物，未找到新版 RunManifest/RunStatus；只读 validate 拒绝旧 Rollback 产物。任务清单勾选不能作为新版端到端完成证据。

当前性能比较还把 WPR 绝对路径、含源码位置的插桩 manifest 身份作为相等条件；不同 worktree 或源码行号移动会阻断比较。游戏、织入器、采样器、基础数据转换器、统计器和领域分析器的版本责任也没有统一。

## What Changes

- 将既有 Network Test Control Center 扩展为唯一 Development Control Center，在 Launcher 中统一选择工作区、版本、场景、运行和报告。Local Fixed Gameplay 性能产品与三个网络产品使用显式产品适配器；未实现的操作明确不提供，不伪造网络回放或网络性能报告。
- 从现有 Network Build Workflow 提取唯一 Development Candidate Build Workflow，复用性能产品的 IL2CPP、符号与场景构建实现。保留干净 Git 提交规则，固定源码、外部依赖、构建配置和嵌入工具，再按内容生成候选身份。名称仅用于阅读，不承担精确身份。
- 各模块拥有自己的工具版本、协议、数据格式和度量定义。发布器自动生成精确工具包身份；Center 只读取并固定版本，不替模块维护版本常量。外部 KK 采样包继续由独立仓库拥有。
- 为同一项目的全部 worktree 配置一个位于 worktree 外的正式开发产物库。开发候选、运行、采样、分析和比较在同一库中形成引用关系；商业 Content/Player 构建继续保持现行目录。
- 纳入“并行开发能力”任务已确认的内存约束：不常驻第二个 Editor；本机后台 Unity 构建全机一次一个，构建结束退出，内存不足排队或中止本次拥有的构建。复用该任务的命令行入口与工作区状态隔离改动，不重做一套构建器。
- 将既有 Network Orchestrator 提升为唯一 Development Run Host，统一运行状态、机器资源租约和本次进程组。现有 Performance Controller 收敛为采集执行模块；保留既有 Player Agent、回放端口、WPR/xperf/WPA 实现，不新增第二采样器。
- 固定每次运行使用的游戏、嵌入采样能力、外部工具、场景、配置、机器和前序 Gate。同机功能运行按显式槽位并行，正式性能采样独占受管理的重负载运行窗口。
- 分离基础采样、离线分析和比较的不可变结果。统计工具更新可以在数据满足其明确输入合同时产生新报告，不修改原始数据；采样方式变化需要重新采样，织入变化需要重新构建。
- 比较精确工作负载、测量工具、度量定义、机器环境和分析输入合同；源码位置只用于跳转。预算单独评价，不把预算阈值变化当作原始指标变化。
- 增加同一采集模块中的托管分配归因模式，保留函数调用栈、字节数、次数和证据覆盖情况；与常规性能采样使用不同测量身份。本提案不修改尚未定位的业务 GC 分配代码。
- **BREAKING**：替换项目内 `Build/Network` 和 `Library/Performance` 的开发正式产物职责，删除旧候选 reader、旧独立顶层控制器/菜单/MCP 实现及相应配置。经引用核对后删除失效旧产物；需要保留的证据只作为不可执行历史，不能补造源码身份或升级成合法候选。

## Impact

- 新增能力：`development-artifact-versioning`、`development-control-center`、`development-run-orchestration`、`versioned-performance-analysis`。
- 修改 current specs：`client-build-artifact-layout`、`gameplay-network-test-build-workflow`、`network-test-runtime-product-boundary`、`deterministic-rollback-relay-product`、`repository-ci-foundation`。
- 修改已经安装的 `network-test-session-orchestration`：以 RENAMED＋MODIFIED 继承六项 current Requirement；继续协调尚未安装的 `gameplay-performance-capture-workflow` 与 `compile-time-performance-instrumentation` delta，本 change 保存其目标文本。审批前不修改原 active change，实施时按 `spec-audit.md` 先完成规范所有权交接，禁止分别归档两份重叠 ADDED。
- 保持独立：商业构建、Gameplay/Network Model、KK Generated Diagnostic Sampling Runtime/Host/Generator、领域 Plan/Operator、GM 业务命令。
- 代码边界：Editor 的 ProductBuild、NetworkProducts、Performance、Launcher；共享 tooling-contracts；既有 Network Orchestrator 与 Performance Controller；性能织入包的身份生成；本地仓库策略精确工具项目允许项。
- 不增加 CI job、云端构建、远程机器调度、GPU/全量 Heap 采集、常驻第二 Editor、Computer Use 或新测试代码。仅本机显式后台构建使用 batchmode；现行规则中的全禁文字必须在审批后同步为这项有界例外，CI 禁令保持。

## 决策与现有规范对账

`design.md` 给出同级方案及业务取舍。本提案选取：干净提交、显式本机共享产物库、唯一运行 Host、同机正式采样排队、精确固定测量工具。它们是待审批的目标，不是当前已经完成的能力。

`spec-audit.md` 逐条列出 current 与 active 的冲突及处理。特别需要审阅：开发产物移出客户端 Build/Library；两个顶层运行生命周期合并；Capture 成功与离线分析成功分离。现行正确的产品适配、进程所有权、回放端口、编译期诊断关闭和领域分析边界继续保留。

## 完成结果

作者能在一个 Center 选择两个 worktree 发布的精确候选，对同一录制输入分别运行；看到源码、工具、行为结果、采样状态及有理由支持或拒绝的性能比较。报告能够在 worktree 删除后继续定位其封存输入与产物。运行证据决定是否可用，不以文件存在、编译通过或任务勾选替代。
