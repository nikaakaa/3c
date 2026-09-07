## ADDED Requirements

### Requirement: 基础采样和离线分析必须分别封存

Capture MUST固定原始证据、基础转换器、采样工具与子 manifest；Analysis MUST固定 Capture manifest/hash、Analyzer 精确身份、算法/Plan 与分析配置。它们 MUST分别有不可变身份与状态。分析失败 MUST不回写已完成基础采样，重新分析 MUST产生新目录。

#### Scenario: 新统计器分析已有采样

- **WHEN** 原始证据完整且满足所选统计器明确的当前输入合同
- **THEN** 分析 MUST生成新的 AnalysisId 并引用原 Capture
- **AND** MUST不修改旧报告或重新启动 Player

#### Scenario: 原始数据无法被新分析器读取

- **WHEN** 数据格式或必需证据不符合所选分析器合同
- **THEN** 分析 MUST返回具体不支持或证据缺失原因
- **AND** MUST不自动转换旧 Schema、补字段或调用兼容 Reader

### Requirement: 测量身份必须与被比较的业务产物身份分离

MeasurementIdentity MUST记录稳定 Metric/Probe 语义、测量工具精确身份、嵌入能力、模式、布局、容量与环境配置。SourceMap 绝对路径、源码行号、CandidateId 和业务方法体 MUST不作为相等准入条件；实际产物身份仍 MUST完整保留用于追溯。业务版本与编译出的游戏二进制是被测对象，允许不同。

#### Scenario: 相同方法移动到另一工作区的另一行

- **WHEN** 探针声明、度量语义和测量工具相同，仅源码位置改变
- **THEN** 两份记录 MUST保持相同测量定义并分别保存准确 SourceMap
- **AND** Comparer MUST不因绝对路径或行号不同而拒绝

### Requirement: 比较必须核对工作负载工具环境和分析合同

比较 MUST由作者指定两份 Analysis 和评价配置，并校对录制输入、初始场景、Variant/roster、相机、时钟、Warmup/窗口、Unity/平台、构建模式、机器/驱动/电源/画质、完整 DiagnosticCapabilitySet、测量工具、Metric 语义及 Analyzer/算法。内容相同的配置文件可以位于不同目录。缺失条件、受污染数据或不一致 MUST给出结构化拒绝原因，不猜测等价或跨机器归一化。

#### Scenario: WPR配置位于不同工作区

- **WHEN** 两份合法采样引用内容相同的 WPR 配置且实际测量工具一致
- **THEN** 比较 MUST按配置内容身份判断
- **AND** MUST不要求物理路径相同

#### Scenario: 采样器升级

- **WHEN** A/B 使用不同精确采样工具或不同织入模式
- **THEN** 比较 MUST拒绝自动性能差值并指出差异
- **AND** 统一测量方式 MUST通过显式重构建/重采样实现，不能修改旧 manifest

### Requirement: 预算评价必须独立于原始性能数值

预算 MUST作为 Analysis/Comparison 的显式输入，并记录其内容身份。不同预算下的通过/失败 MUST不能直接比较。测量条件一致的两份数据 MAY在同一新预算下重新评价，而不改变原始指标。

#### Scenario: 作者收紧GC预算

- **WHEN** 原始采样不变而作者选择更严格预算
- **THEN** 新报告 MUST保留原始数值并记录新的预算结论
- **AND** MUST不把结论变化描述为游戏性能回归

### Requirement: 回放完整性行为差异与性能结果必须分别表达

Replay Gate MUST证明输入、范围、合法运行与证据完整性。两版本行为比较 MUST独立记录 Match、Different 或 NotCompared，并引用明确规则/轨迹证据。相同输入 MUST不自动证明行为相同；性能报告 MUST携带行为差异状态，不能把改变业务结果获得的提速直接称为等价优化。

#### Scenario: 两个版本轨迹不同但都完成回放

- **WHEN** 两个 Replay 都合法完成而 Body Trajectory 不同
- **THEN** Center MUST同时显示回放完成与行为不同
- **AND** MUST不把其中一个状态覆盖成笼统通过

### Requirement: 领域采样框架与分析器必须保持独立所有权

Center 与 Performance MUST只固定 KK 包、Generator、Program、Schema、完整 DiagnosticCapabilitySet 和精确子 manifest。框架 MUST继续拥有领域 Session、cadence、typed lineage、packet、Writer 与基础表生成；领域 Analyzer/Operator/Plan MUST继续拥有离线规则。不在公共工具中复制字段映射、领域 Reader 或第二生命周期。

#### Scenario: 选择Foot采样能力

- **WHEN** 构建配方启用正式 Foot Capability
- **THEN** 候选与采样 MUST记录其完整静态闭包并引用框架生成的基础子产物
- **AND** 公共 Center MUST不重新排列 Foot 字段或重写子 manifest

### Requirement: 托管分配诊断必须提供真实调用栈证据和覆盖情况

唯一采集模块 MUST提供显式托管分配诊断 Profile，通过当前 Unity 支持的原生能力记录分配调用栈。报告 MUST列出范围、字节数、次数、方法/调用路径和未解析覆盖；无法支持或缺证据 MUST明确返回 Unsupported/MissingEvidence。Editor 内分析操作 MUST显式绑定 unity_instance 并在 GUI 回调外调度。每帧分配计数 MUST不能替代回收耗时、内存泄漏或函数归因。

#### Scenario: GC计数超标但没有可解析调用栈

- **WHEN** 已有每帧分配量而缺少完整归因证据
- **THEN** 报告 MUST只确认分配超预算并显示归因缺失
- **AND** MUST不把 CPU 热点函数当成分配来源

#### Scenario: 带调用栈诊断与常规采样比较

- **WHEN** 两份采样的分配调用栈捕获模式不同
- **THEN** 它们 MUST具有不同 MeasurementIdentity
- **AND** MUST不将诊断开销差异当作游戏回归
