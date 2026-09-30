# 3C 正式入口与证据规则

本文件记录已有回放系统的使用方式，不保存 IK 算法、阈值或历史验收结论。执行时以当前源码、工具 schema 和项目 `AGENTS.md` 为准；只有用户明确要求 OpenSpec 工作流时才按该工作流读取相关文档。不写死历史任务 ID、实例、trace ID 或 schema 版本。

## 正式代码入口

从当前工作区确定仓库根目录，Unity 项目为 `3cDemo/Client/3C_Client`。下文 `Client/` 仅为该目录的缩写。

- 工具参数和返回字段：`Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Diagnostics/CharacterFixedInputTraceMcpTool.cs`。
- 回放、起始状态、时钟和 Proof：同目录 `CharacterFixedInputTraceWorkflow.cs`。
- 联合诊断采集：同目录 `CharacterGameplayDiagnosticCapture.cs`。
- Foot 采样与分析入口：同目录 `CharacterFootDiagnosticSampling.cs`，通过 `DiagnosticSamplingWorkflowRegistry` 和 `DiagnosticAnalysisWorkflowRegistry` 选择正式实现。
- 运行时输入消费：`Client/Assets/GameScripts/Main/Runtime/Simulation/Core/Fixed/Diagnostics/FixedCharacterInputTraceModule.cs`。

工具和采样合同会变化，先读对应入口，再准备请求。产品构建需要时发现当前正式工具及资产引用，不沿用已移除的构建工具名或旧产品路径，不创建第二套执行入口。

## Unity 与构建

实际操作遵循 [3C Unity 操作](../../3c-unity-mcp/SKILL.md)，使用目标实例的显式参数。禁止通过 `set_active_instance` 切换全局实例。

不停止用户录制或其他任务拥有的 Play 会话。只有本次已授权且需要的构建或回放才执行；不因读取 skill 而编译。系统文件工具修改脚本后，依目标实例实际状态完成正式刷新、编译和错误检查，不套用 MCP 写脚本的自动刷新假设。

需要 .NET 编译时确认实际 csproj，使用 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false`；结束或失败后立即 `dotnet build-server shutdown`，保留原构建退出码。编译成功不代替回放结果。

用户授权从采样做历史函数对照时，历史源码可在项目 `Temp/` 中用当前 Unity 自带的 Roslyn 独立编译，保留其原逻辑和现有程序集的内部访问合同，不切换共享工作区的运行代码。此处已复现 CodeDom/Mono 编译器将 Roslyn 产物中的 `in` / `ref readonly` 视为可写引用而报错；不能为迁就该编译器改写历史逻辑。改用 Unity 自带 Roslyn 已通过同源编译检查，函数的实际执行与 A/B 结果仍需单独记录。

## 输入与模式

正式工具为 `character.fixed_input_trace`，每次调用在外层绑定目标 `unity_instance`。名称可能由宿主规范化；按实际发现的工具调用。

| action | 用途 |
| --- | --- |
| `list_traces` / `inspect_trace` | 发现并检查精确输入、路径、帧数与 Tick Rate |
| `replay_start` + `trace_id` | 普通固定输入回放 |
| `diagnostic_replay_start` + `trace_id` | 固定输入回放并采集 Foot 与 Presentation 诊断 |
| `schedule_record_start` + `trace_id` | 捕获输入对应的 Live Presentation Schedule |
| `schedule_replay_start` + `trace_id` | 按已绑定的 Presentation Schedule 回放 |
| `compare_replays` | 用明确的 `baseline_proof_path`、`candidate_proof_path` 比较已保存 Proof |
| `status` | 查询本次进度、失败与输出 |
| `record_start` / `record_stop` | 用户授权且确实需要新输入时录制 |
| `stop` | 停止本任务拥有且需要结束的操作 |

A/B 使用同一个精确 `trace_id`，不省略为 latest，也不为测试临时生成另一条路线。普通回放不自动采集 Foot；诊断回放与两种 Schedule 模式要求对应采样能力已编译，缺失时不能把普通回放当成诊断成功。

普通与诊断回放由 Workflow 管理起始状态和 Fixed Tick 驱动；诊断模式还负责采样开始、窗口开关与结束发布，不另起手动采样器或自行推 Tick。涉及真实表现帧间隔时核对 Schedule 模式及绑定；普通同输入通过不证明所有帧率下表现一致。

输入 Record 负责输入及自身完整性；程序与表现身份从当前 Proof 和采样产物读取。加载被拒绝时保留原因，不手改 hash、schema 或状态强行通过。

## 状态与证据读取

输入消费结束不等于采样与分析完成。先核对本次操作身份、`failure`、`workflow_status` 和输入覆盖，再检查所选模式需要的产物。返回值可能保留上次路径，不能仅凭路径非空认定成功。

| 返回字段 | 读取用途 |
| --- | --- |
| `trace_directory`、`last_trace_path`、`traces` | 定位输入记录；选择已确认的条目 |
| `presentation_schedule_path` | 本次表现调度 |
| `replay_proof_path`、`runtime_trace_summary_path` | 回放一致性与运行轨迹证据 |
| `replay_comparison` | 当前 Proof 比较结果；检查是否确有两侧基线 |
| `foot_sampling_available`、`foot_sampling`、`foot_sampling_finalizing` | 诊断模式的能力与采样生命周期 |
| `samples_path`、`manifest_path` | 当前采样输出和正式文件清单 |
| `ground_contacts_path`、`ground_envelope_path`、`ground_surfaces_path` | 清单中地面相关证据 |

通过当前采样 manifest 查找文件角色与位置，不硬编码旧 `facts.json`、旧 `FootPlacementRuns` 布局或旧分析器类名。采样、分析有各自状态；需要分析结果时继续沿 `CharacterFootDiagnosticSampling` 的正式分析入口核对结果目录、报告和失败信息，不能把采样完成写成分析通过。

已有持久输出直接保留，不因失败或汇报复制整个包。只有重建会覆盖仍需使用的临时 Proof/Schedule 时，才保护受影响文件并核对原字节；不改变正式加载路径，不擅自压包或提交诊断数据。程序消费者需要的 manifest 不能当作额外归档删除。

`baseline_available=false` 或 `baseline-created` 只表示建立基线。自动选择的历史 Proof 必须与本次明确基线对应；输入与 Body 轨迹一致不代表脚部、相机或性能已经达标。

## 诊断与比较

- 先读摘要、覆盖和来源，再按异常帧、左右脚及对象读取必要明细。采样行数不默认等于输入帧数乘二。
- 沿运行时输入、发布事实、原始采样、分析公式、诊断结果检查首次偏离，区分业务回归和诊断错误。相近字段名不能替代单位、坐标系、有效性和对象身份。
- 重新分析沿正式注册的分析工作流执行；当前 `CharacterFootDiagnosticSampling.AnalyzeExistingCapture` 是内部入口，不能据此虚构 CLI/MCP 命令，也不通过注入脚本或复制公式执行。先发现外部正式入口。
- 旧原始字段足够且分析入口可用时才能重分析；确认输出是否覆盖旧结果，必要时保留受影响证据。字段不足则同输入补采，不用默认值补列。
- A/B 对齐输入、起始状态、世界、运行变体与时钟。分析 Plan、规则、schema 或采样器改变时，先建立同口径再解释差值。
- 按任务范围比较跳变、锁定漂移、穿透、可达性、骨盆位移、求解残差或查询成本；不强制所有任务使用固定领域指标。阈值来自正式配置与已确认目标。
- 允许只读计算已有结果的差异、分布和峰值，但不生成冒充正式产物的 facts/diagnoses。复用指标进入已授权的正式诊断模块。

撤销本步时只恢复可归属本步的源码和相应产物，并按通用回放流程核对恢复结果；不覆盖其他任务改动。交付分别说明回放一致性、领域质量与尚未覆盖项。
