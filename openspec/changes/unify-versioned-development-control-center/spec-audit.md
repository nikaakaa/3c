# 现有规范、实现与交接对账

## 审计范围

依据 2026-09-04 的 current specs、active changes 和实际代码；active `add-gameplay-performance-capture-workflow` 正被其他任务修改。本提案只新增自己的目录，不改写其文本或任务状态。

本地 OpenSpec CLI 为 1.11.0，current `repository-ci-foundation` 固定 CI 使用 0.23.0。提案交付同时检查当前 CLI 和固定版本；不把更换 CLI 当作绕过严格校验的方式。

## 关联任务：并行开发能力

用户要求纳入 `codex://threads/01a06b8d-a09e-7b51-9f21-2f719aff24fd`。已通过任务记录与其测试 worktree 代码核对：

- 用户明确拒绝常驻第二 Editor，原因是内存不足；随后确认本机按需后台命令行构建方向。
- `de40f6f7b` 已完成项目偏好/Editor SessionState 隔离；应复用，不回退。
- `D:/Unity_Project_1/3C-parallel-test` 已有独立工作区和 `18d289aec` 命令行接缝，调用现有 Performance Workflow；不是另一套 Player 编译实现。
- 新工作区发现 UTF-16 源码换行破坏、正式编译源码被 Build 忽略规则排除、主工程修复未提交等依赖问题，相关修复仍由该任务收口。本提案要求完整来源闭包，不把遗漏文件补丁复制成长期同步方案。
- 已证明测试工作区产生独立 Editor 程序集；尚未证明完整 Player 打包/运行。首次导入内存上涨后中止了一次构建，后续仍在尝试，不能写成“并行 Player 验证通过”。
- 当前本任务提供的 AGENTS 与 project.md 仍写“永远不要运行 Unity batchmode”，与关联任务确认的执行方向有冲突。提案明确本机受控构建例外，审批实施时统一规则；本轮不启动 Unity，不借任务记录绕过当前执行禁令。CI 中的 batchmode 禁令不受此例外影响。

## current 冲突

| current 文件与 Requirement | 已有约定 | 本提案处理 |
|---|---|---|
| `client-build-artifact-layout` / 客户端正式产物必须收敛到唯一 Build 根 | 商业与 Network 都放客户端 Build，Library 只作缓存 | 商业 Content/Player 保持；开发测试正式产物转到项目唯一外置库，明确不是第二商业发布根 |
| 同文件 / Network Test Product 必须保留现行 Build/Network 合同 | 固定 ProductRoot、schema v2、原子替换 | 重命名并替换为开发候选共享产物库规则；删除 Build/Network 活动消费者 |
| `gameplay-network-test-build-workflow` / 唯一 Editor Build Workflow、Build/Run 分离、Product Manifest | Network 专属公共构建、同产品覆盖、没有统一工具来源 | 提升共同 Development Workflow；保留显式 adapter、Build/Run 分离和严格闭包，候选不可覆盖 |
| `network-test-runtime-product-boundary` / Runtime Artifact 列表、公共 Workflow、隔离闭包、同一 Manifest | schema v2、当前 Product、原子替换 | schema v4 开发候选合同引用运行 artifact 与工具；保留 Model/Product 分离与三种产品业务 |
| `deterministic-rollback-relay-product` / Runtime Manifest、Server Closure、Run进程 | 构建期固定实例配置、schema v2、运行期禁止生成配置、只有三进程 | 静态业务身份不变；本Run配置由Host生成，声明独立GM/Host工具角色，共五进程且只有两个Unity Client |
| `repository-ci-foundation` / 被跟踪正式文件 | 生成目录禁止跟踪、精确工程允许项 | 保持；新增精确最终工具路径约束，不增加 CI job，不上传产物 |

## active 重叠与归属

| active change | 需要交接的内容 | 继续由原 change 拥有的内容 |
|---|---|---|
| `add-versioned-network-test-orchestration` | CandidateId 的 label/commit 键、Build/Network 路径、Network 专属 Host、Center、工具允许项、Relay实例配置与工具进程边界；其 `network-test-session-orchestration` 完整 delta 由本提案目标文本承接 | Relay/GM 业务协议、Authority/Server 产品语义及已经正确的角色适配 |
| `add-gameplay-performance-capture-workflow` | 独立顶层 Controller、Library 正式产物、内嵌 summary 成功条件、performance.* 顶层调度、绝对路径比较、工程允许项；完整性能能力 delta 在本提案内保存 | 现有采集/回放具体实现的修复；待完成 DiagnosticCapabilitySet 与框架接入由该 change 对接，不在此复制 |
| `add-compile-time-performance-instrumentation` | 绑定 BuildIdentity 的比较身份、SourceMap 与测量身份混合；完整能力 delta 在本提案内保存 | 正确织入语义、AOT 支持、Disabled 零闭包与调用跨度实现 |
| `extract-generated-diagnostic-sampling-package` | 不接管 | 独立 KK 包版本与解析、Generator/Host 合同；本提案只固定包/Program/Schema 身份并引用结果 |
| `add-schema-driven-diagnostic-analysis` | 不接管 | Schema Reader、Operator、Plan、四种规则结果、独立离线报告 |
| `refactor-character-pose-graph-architecture` 等 Gameplay 改动 | 不接管 | 业务 Runtime/Metric Owner；本提案不能为了统一工具反改正确玩法代码 |

### 规范安装步骤

这是一份待审批的替代提案，不表示旧 active 已经被撤销。其审批范围必须包含上述公共职责交接；未批准交接时，不实施冲突模块。

实施的第一个文档闭环是：将重叠 delta 的最终所有权移到本 change；从原 active 中移除对应重复 capability 文件或精确重叠 Requirement，给原任务留下迁移目标记录，不把未完成工作勾成完成。未重叠的领域要求保留。旧 Host/Controller 的 repository-ci ADDED 允许项同步移除，由本 change 最终工程清单替换。

目前 current 尚无 `network-test-session-orchestration`、`gameplay-performance-capture-workflow`、`compile-time-performance-instrumentation`，所以本 change 对它们使用完整 ADDED，而不是对不存在的 current Requirement 写 MODIFIED。交接后只能由本 change 安装这三项；禁止原 active 再分别添加同名 capability。若审批前它们已被用户归档或同步为 current，必须先重新对账，将本 change 改为对真实 current 的 MODIFIED/REMOVED，不能用重复 ADDED 覆盖。

本提案不修改 archive、不推定用户已验收、不自动归档任何 change。原 active 其余 delta 在后续安装时也必须按已经安装的 current 重新核对，不能把旧路径或旧工具白名单写回来。

## 必须明确的语义变化

1. **正式开发产物移出 worktree**：旧“只有客户端 Build 才是正式根”缩小到商业产物；开发实验有唯一项目级产物库。不是 Build/Network 与外置库双写。
2. **公共运行所有者变化**：Network Orchestrator 提升为 Development Run Host；Performance Worker 只拥有采集领域资源，不再独立拥有顶层 Run 与进程终止策略。
3. **Capture 与 Analysis 分离**：完整基础数据可以成功封存，而后续分析失败。旧“summary 失败则整份 Capture Faulted”的语义删除；必需原始数据或基础转换失败仍不能成为可比较 Capture。
4. **精确产物身份与测量身份分离**：保留实际文件来源，同时移除物理路径、行号、业务 BuildId 对比较条件的污染；探针集合/语义、工具、模式或布局变化仍拒绝跨工具差值。
5. **预算与测量分离**：同一原始指标可以按新预算重新评价；不把不同预算的通过/失败直接当作性能回归。
6. **统一历史不等于兼容旧运行**：旧无源码身份的采样不能补一个当前 commit 后进入正式候选。历史证据保留原内容并标记不可运行，不建立旧 schema 活动 reader。

## 其他任务需要确认的已有冲突

- KK 外部消费 delta 首条声明 0.5.2，但后文仍存在“0.4 Schema/迁移/闭包”字样。版本与 ABI 的最终值由该接入 change 确认，本提案不猜测。
- Performance active 当前仍描述 Generated Started/CommittedSample/Stopped Handler；KK 接入文本已要求 private static partial DiagnosticEvent、禁止事件 DTO/Bridge。接入应消费最终框架生命周期 API，本提案只要求能力清单与子 manifest 的公共边界。
- current Network specs 仍要求 schema v2，而 project.md 已描述 schema v3 候选链。这是未归档 active 与 current 安装状态不一致，不证明磁盘上存在合法候选。

## 实现证据定位

- `NetworkTestCandidateIdentity.cs`：干净提交、label + commit 身份。
- `NetworkTestControlCenter.cs` / `NetworkTestCandidateCatalog.cs`：当前工程目录枚举与后台校验。
- `Tools/ThirdPersonNetworkTest/Program.cs`：项目目录下槽位租约与 Job Object。
- `ThirdPersonPerformanceCaptureWorkflow.cs`：Library 产物、单独 BuildId 与 Run 调度。
- `PerformanceCapturePublisher.cs`：绝对 WPR 路径、instrumentation_identity、预算等比较条件。
- `PerformanceInstrumentationPointManifest.cs` / `PerformanceInstrumentationManifestIndex.cs`：SourceMap 路径/行号随 manifest 文本进入插桩身份。
- `NetworkTestToolBundlePublisher.cs`：发布器中的 OrchestratorToolVersion 与 adapter 版本常量，应回归各模块自有描述。

## 提案校验记录

- 本 change 使用本机 OpenSpec 1.11.0 严格校验通过；已按其检查要求保留 current MODIFIED Requirement 的原有 Scenario 名称并更新目标行为。
- 本 change 使用仓库固定 OpenSpec 0.23.0 严格校验通过；同版本全库快照为 115 项通过、0 项失败。
- 本机 1.11.0 全库快照为 107 项通过、9 项失败，本 change 通过。其他失败项为 add-character-pose-correction、add-discrete-stair-presentation、add-rollback-gm-console、add-versioned-network-test-orchestration、refactor-character-ik-maintenance-boundaries，以及 character-action-animation-authoring-workspace、character-animation-blend-stack、character-animation-transition-routing-module、character-motion-matching-presentation-module。并行任务持续变化，这些是本次命令快照，不代表本提案引入的问题，也不擅自修改其他文档。
- CLI 结构校验不证明 active 重叠可以同时安装；仍须执行本文的规范所有权交接，也不证明 Player/采样已经实机通过。
