# agent-character-controller-synthesis Specification

## Purpose
定义 Agent 在编辑器内通过 Snapshot、Intent、Macro、Patch IR、Compiler、Validator 和 Report 生成并修复正式 BTSMTL 角色控制器资产的唯一链路。
## Requirements

### Requirement: Agent 生成链路必须是 editor-only authoring 编译链路

系统 MUST 将 Agent 生成角色动作控制器实现为 editor-only authoring 编译链路。Agent JSON、Intent、Macro 和 Patch IR MUST 只服务编辑期生成、修复和评估。运行时 MUST 继续执行正式 BTSMTL asset、CharacterPipeline、ActionRuntime、Timeline 和 SyncFacts。系统 MUST NOT 在 gameplay runtime、服务端或网络同步路径中执行 Agent JSON 或调用 LLM。

#### Scenario: 运行时加载角色

- **WHEN** `CharacterPipelineHost` 创建角色 pipeline
- **THEN** runtime MUST 只读取 `CharacterPipelineDefinition`、RootTree、ActionProfile、Timeline 和输入配置
- **AND** runtime MUST NOT 读取 Agent Intent、Patch IR 或 LLM 输出文件

#### Scenario: 编辑器生成控制器

- **WHEN** 作者或 Codex 请求生成角色动作控制器
- **THEN** editor MAY 读取 Agent JSON 并编译为 BTSMTL graph 修改
- **AND** 编译完成后的正式源数据 MUST 是 BTSMTL asset 和相关 Unity 资产

### Requirement: Agent Snapshot 必须是只读投影

系统 MUST 能从当前 `CharacterPipelineDefinition` 和 BTSMTL graph 导出 Agent Snapshot。默认 Snapshot MUST 是面向 Agent 生成的紧凑只读投影，包含 graph summary、StateMachine、State、Transition 条件、输入配置、ActionProfile、Timeline 和 Action Context 可引用摘要。系统 MAY 提供 full debug snapshot 导出节点、边、端口和 inline/shared ownership 细节，用于排查 compiler 或 graph 结构问题。Snapshot MUST NOT 成为正式配置来源，MUST NOT 保存运行时临时状态，MUST NOT 暴露 Unity YAML 或内部序列化集合布局。

#### Scenario: 导出角色控制器 snapshot

- **WHEN** 用户从 `CharacterPipelineDefinition` 导出 Agent Snapshot
- **THEN** 默认 snapshot MUST 用紧凑字段描述 RootTree、下钻 StateMachine、StateBehaviorSubTree 和 ConditionRuleGraph 的业务摘要
- **AND** 默认 snapshot MUST 描述当前 definition 可用的 input request、ActionProfile、Timeline 和 Action Context 引用
- **AND** 默认 snapshot SHOULD NOT 输出完整节点端口和 property edge dump
- **AND** snapshot MUST NOT 修改任何 graph asset

#### Scenario: 运行时忽略 snapshot

- **WHEN** 项目进入播放或构建 runtime pipeline
- **THEN** snapshot 文件 MUST NOT 参与 runtime 装配
- **AND** 缺失 snapshot MUST NOT 影响角色正常运行

### Requirement: Agent Intent 必须表达角色动作业务意图

系统 MUST 提供面向 Agent 的 `AgentControllerIntent` schema，用于表达角色动作控制器业务意图。Intent SHOULD 使用 input request、action category、nested state machine、state、ActionProfile、Timeline、cancel、hit reaction 等业务概念。Intent MUST 能表达“外层 Attack category 拥有内层 combo StateMachine”，但 MUST NOT 要求作者或 Agent 直接填写 BTSMTL 内部字段、Unity YAML 路径、节点 GUID 或私有序列化字段。

#### Scenario: 描述二连击

- **WHEN** Agent 需要表达轻攻击二连击
- **THEN** Intent MUST 能描述外层 Attack category、内层 Attack1/Attack2、各自 ActionProfile、各自 Timeline 和 combo 条件
- **AND** Intent MUST NOT 把 Attack1/Attack2 强制平铺到外层 Action StateMachine
- **AND** Intent MUST NOT 直接包含 `m_Nodes`、`m_Edges` 或 Unity serialized property path

### Requirement: Macro 必须将业务意图展开为受限 Patch IR

系统 MUST 提供 Agent Macro 层，将受限业务意图展开为 Patch IR。二连击 Macro MUST 使用普通 `StateMachineNode`、inline `StateMachineGraph`、`StateNode`、Transition edge 和 ConditionRuleGraph 表达外层 Attack category 与内层 combo 状态机。Macro MUST NOT 新增 Attack 专用 opcode、直接修改 BTSMTL asset 或重新生成平铺 Attack1/Attack2。

#### Scenario: 展开二连击

- **WHEN** Macro 接收 `two_hit_combo` intent
- **THEN** Macro MUST 产出外层 Attack State、Attack state body 内的 StateMachineNode、内层 Attack1/Attack2/Exit 和 combo transition 的 Patch IR
- **AND** combo request 查询 MUST 位于内层 ConditionRuleGraph 或等价纯条件位置
- **AND**具体攻击 state MUST 继续产出 Action Context、Timeline 和 lifecycle 节点

### Requirement: Patch IR 必须是确定性的 graph 编辑指令

系统 MUST 定义 Agent Patch IR 作为确定性的 graph 编辑指令层。Patch IR MUST 使用 stable authoring id、graph path 或 snapshot 引用定位编辑目标。Patch IR MUST 只能表达正式 authoring 操作，例如 ensure state、ensure transition、ensure rule、ensure behavior node、bind asset、link flow 和 link property。Patch IR MUST NOT 直接写 Unity YAML、GUID 映射集合、runtime 状态或旧配置路径。

#### Scenario: 添加状态

- **WHEN** Patch IR 表达添加 `Attack1` 状态
- **THEN** compiler MUST 定位目标 `StateMachineGraph`
- **AND** compiler MUST 通过正式节点创建入口创建 `StateNode`
- **AND** Patch IR MUST NOT 包含直接插入节点集合的操作

#### Scenario: 连接 Transition

- **WHEN** Patch IR 表达 `Attack1 -> Attack2`
- **THEN** compiler MUST 通过正式 flow link 入口创建 Transition edge
- **AND** 合法 Transition MUST 拥有 inline `ConditionRuleGraph`

### Requirement: Compiler 必须调用 BTSMTL 正式 authoring API

系统 MUST 通过 AgentPatchCompiler 将 Patch IR 应用到 BTSMTL graph。Compiler MUST调用现有正式 authoring API、节点/模块配置入口和 Timeline ownership authoring service，至少包括 BaseGraph.CreateNode(Type)、BaseGraph.Link(...)、BaseGraph.LinkProperty(...)、TimelineNode inline/shared 切换和 TimelineData clone。Compiler MUST尊重 CanCreateNodeType(Type)、PropertyPort PortId、Graph 与 Timeline inline/shared ownership 和 graph 类型规则。Compiler MUST NOT自己维护第二套节点、边、端口、Timeline 数据或 Workbench 数据。

#### Scenario: 创建非法节点

- **WHEN** Patch IR 尝试在 StateMachineGraph 中创建 TimelineNode
- **THEN** compiler MUST拒绝该操作并输出 compile report
- **AND** 系统 MUST NOT把非法节点加入正式 graph

#### Scenario: 默认创建 inline TimelineNode

- **WHEN** Patch IR 为状态行为 Graph 创建 TimelineNode 且未显式请求 Shared
- **THEN** compiler MUST创建正式 TimelineNode
- **AND** compiler MUST通过正式 ownership API 创建 inline TimelineData
- **AND** compiler MUST NOT要求或保留外部 TimelineAsset 引用

#### Scenario: 从 template asset 导入 inline Timeline

- **WHEN** Patch IR 为 Inline TimelineNode 提供 TimelineAsset template path
- **THEN** compiler MUST将 template data 克隆到节点 inline TimelineData
- **AND** template path MUST只作为编译期输入
- **AND** 生成节点 MUST NOT保存该 asset 为 runtime source

#### Scenario: 显式绑定 shared Timeline

- **WHEN** Patch IR 明确设置 Timeline ownership 为 Shared 并提供 TimelineAsset path
- **THEN** compiler MUST通过正式 ownership API绑定 shared TimelineAsset
- **AND** 节点 inline TimelineData MUST被清理
- **AND** compiler MUST NOT创建 TimelineStateNode 或旧播放器引用

### Requirement: Node Emitter 必须使用白名单

系统 MUST 使用 Node Emitter 白名单限定第一阶段 Agent 可生成节点。每个 emitter MUST 声明允许的 graph kind、必需参数、可选参数、资产引用和输出 report。未知节点类型、未知字段、未知端口或未登记参数 MUST 被拒绝。系统 MUST NOT 因未知节点自动降级为 placeholder、fallback 节点或字符串脚本。

#### Scenario: 未登记节点

- **WHEN** Agent Patch IR 请求创建未登记节点类型
- **THEN** compiler MUST 报告未知节点错误
- **AND** compiler MUST NOT 创建占位节点

#### Scenario: 参数缺失

- **WHEN** action activation emitter 缺少 ActionProfile 引用
- **THEN** compiler MUST 报告缺少必需参数
- **AND** compiler MUST NOT 使用默认 ActionProfile 或目录搜索结果补齐

### Requirement: 资产解析必须来自当前角色 authoring context

系统 MUST 通过当前 `CharacterPipelineDefinition` 和 Agent Snapshot 解析输入、ActionProfile、Timeline 和 RootTree 引用。Resolver MUST 使用稳定 id 或明确资产引用。Resolver MUST NOT 扫描场景、目录、同名 asset、旧 SO/config 或全局单例作为 fallback。

#### Scenario: 解析 ActionProfile

- **WHEN** Agent Patch IR 引用 `Attack.Light.01`
- **THEN** resolver MUST 从当前 `CharacterPipelineDefinition.ActionProfiles` 中解析对应 ActionProfile
- **AND** 找不到时 MUST 报错
- **AND** resolver MUST NOT 从项目目录按名字搜索替代 profile

#### Scenario: 解析输入 request

- **WHEN** Transition rule 引用 `Attack` request
- **THEN** resolver MUST 从当前 `CharacterInputProfile` 的 action request 定义解析
- **AND** 找不到时 MUST 报错

### Requirement: Validator 必须检查 Agent 生成 graph 的 BTSMTL 语义

系统 MUST提供 Agent graph validator，在 apply 前后检查 Agent 生成结构。Validator MUST检查 graph 类型规则、ConditionRuleGraph 纯条件语义、TimelineNode 位置、Timeline inline/shared ownership、TimelineData serialized owner/path、TreeClip graph ownership、ActionProfile 引用、Input request 引用、Action Context 链路和 AnyState 条件。Validator MUST输出机器可读错误路径和建议修复。

#### Scenario: TimelineNode 位于错误图层

- **WHEN** 生成结果中 TimelineNode 位于 StateMachineGraph
- **THEN** validator MUST报告 graph kind 错误
- **AND** report MUST指出 TimelineNode 应位于 StateNode 的状态行为 Graph

#### Scenario: TimelineNode 存在双真相

- **WHEN** TimelineNode 同时保存 inline TimelineData 和 shared TimelineAsset
- **THEN** validator MUST报告 ownership 冲突
- **AND** validator MUST NOT按优先级静默选择其中一份

#### Scenario: inline Timeline owner path 断裂

- **WHEN** TimelineNode inline TimelineData 无法绑定到 RootTree serialized owner/path
- **THEN** validator MUST报告稳定 node path 与断裂字段
- **AND** 系统 MUST NOT把数据保存到临时 Timeline asset

#### Scenario: Action Context 断链

- **WHEN** 攻击状态播放带 projected Window TreeClip 的 Timeline 但没有 Action Context 来源
- **THEN** validator MUST报告 Action Context 缺失
- **AND** report MUST建议在状态进入或动作开始处创建 action activation 并把 context 传给 TimelineNode

#### Scenario: TreeClip owner path 断裂

- **WHEN** resolved TimelineData 中的 TreeClip inline TimelineRunningTree 缺少稳定 owner/path
- **THEN** validator MUST报告 TimelineNode、track、clip 和 graph identity
- **AND** validator MUST拒绝该 authoring 结果

### Requirement: Compile Report 必须支持 Agent 自修复

系统 MUST 输出 `AgentCompileReport`。Report MUST 包含 schema 错误、引用解析错误、编译错误、语义错误、计划 diff、已应用 diff、指标和建议修复。Report MUST 使用机器可读路径定位 Intent、Patch operation、graph、node、edge 或 asset。Report SHOULD 同时包含简短中文说明，方便作者理解。

#### Scenario: Patch 编译失败

- **WHEN** compiler 拒绝某条 Patch operation
- **THEN** report MUST 标出 operation id、错误类型、原因和建议修复
- **AND** Agent MUST 能基于 report 生成下一轮修复 Patch

#### Scenario: 编译成功但语义校验失败

- **WHEN** Patch apply 成功但 validator 发现 Action Context 断链
- **THEN** report MUST 标出相关状态、TimelineNode 和缺失的 context 关系
- **AND** report MUST 区分编译成功与语义失败

### Requirement: Agent 评估必须区分结构、语义和业务覆盖

系统 MUST 提供第一阶段 Agent 生成评估口径。评估 MUST 至少统计 schema 合法率、编译成功率、语义合法率、引用解析成功率、修复轮数、diff size 和业务覆盖度。评估 MUST 使用受控样例任务衡量 Agent 生成链路稳定性，MUST NOT 要求运行时执行 Agent JSON。手感、动画时长和端到端战斗体验 MAY 由作者在 Unity 中验证，但 MUST NOT 作为 OpenSpec task 的手动验证项。

#### Scenario: 评估二连击生成

- **WHEN** 评估样例要求生成二连击
- **THEN** 评估 MUST 检查是否生成 Attack1、Attack2、对应 ActionProfile、TimelineNode、combo transition 和退出 transition
- **AND** 评估 MUST 检查生成结构是否通过 validator

#### Scenario: 统计修复轮数

- **WHEN** Agent 根据 compile report 进行多轮修复
- **THEN** 评估 MUST 记录从首次输出到合法 graph 的轮数
- **AND** 该指标 MUST 独立于最终 runtime 手感评价

### Requirement: 正式资产必须仍由人类可微调

系统 MUST保持 Agent 生成后的正式结果为普通 BTSMTL Graph、Timeline、ActionProfile 和 CharacterPipelineDefinition 内联 Presentation Definition。作者 MUST能在 Graph Editor 调整逻辑，在 Timeline Editor 调整 clip/time，在 CharacterPipelineDefinition Inspector 调整 Layer 与 producer binding，并在 Animancer TransitionLibrary 正式入口调整 transition 与 easing。Agent Snapshot MAY只读理解 Presentation identity，但 Agent Patch MUST不形成第二个 Presentation 写入口。

#### Scenario: 作者微调生成结果

- **WHEN** Agent 生成普通 Tree branch、Attack State 与 Timeline
- **THEN** 作者 MUST在 Graph Editor 调整 logic rule
- **AND** 在 Timeline Editor 调整 clip/time
- **AND** 在 CharacterPipelineDefinition Inspector 调整 Layer 与 producer binding，并从 Inspector 进入 Animancer TransitionLibrary
- **AND** 三个入口 MUST不双写同一字段

#### Scenario: Agent 继续修改

- **WHEN** 作者微调后再次请求 Agent 增加 dodge cancel
- **THEN** Agent MUST基于新的 Graph、Timeline 与只读 producer identity 生成增量 Patch
- **AND** MUST不覆盖作者在 CharacterPipelineDefinition Inspector 或 Animancer TransitionLibrary 中的修改

### Requirement: Agent Snapshot 与 Validator 必须递归理解嵌套 StateMachine

Agent Snapshot MUST递归输出完整 RootTree authoring routes、普通 RunnableNode、flow edges、inline/shared Graph、nested StateMachine、logical transitions、Action activation、Timeline 与稳定 animation producer identity。Presentation section MUST只读输出 Layer catalog、TransitionLibrary identity 与 producer binding。Validator MUST检查 Graph topology、route identity、Timeline identity 与 Timeline LayerId，但 MUST不校验或写入 Presentation binding、Animancer transition 或 runtime playback lifecycle。

#### Scenario: Corin Snapshot

- **WHEN** 导出 Corin compact Snapshot
- **THEN** Graph section MUST显示 Root Parallel、普通 Runnable、外层 None/Attack/Dodge、内层 Attack1/Attack2 与完整 route
- **AND** Presentation section MUST只读显示 Base layer、TransitionLibrary 与 Timeline producer identity/binding
- **AND** Graph Node/Edge MUST不输出动画角色或策略字段

#### Scenario: Timeline identity 断裂

- **WHEN** Graph 引用的 Timeline、Track 或 Clip 缺失稳定 authoring identity
- **THEN** Validator MUST输出对应 Graph/Timeline source 错误
- **AND** Compiler transaction MUST回滚

#### Scenario: 父子重复状态

- **WHEN** Attack1/Attack2 同时存在于父子层
- **THEN** Validator MUST报告分裂结构
- **AND** Compiler MUST不选择 fallback topology

### Requirement: Agent Patch 编译必须维护 identity 生命周期

Agent Patch compiler MUST在更新现有元素时保持其 authoring identity，在创建新元素时生成新 identity，在复制元素时生成新 identity。系统 MUST只接受 schema v6，不得保留旧 schema 兼容解析或按 path 猜测 identity。

#### Scenario: 更新现有 Timeline Clip

- **WHEN** Patch 修改一个由 authoring identity 指定的 Clip 参数
- **THEN** compiler MUST修改该 Clip
- **AND** Clip identity MUST保持

#### Scenario: 创建新 Track

- **WHEN** Patch 创建新的 Timeline Track
- **THEN** compiler MUST为该 Track 生成新 identity
- **AND** validator MUST拒绝缺失或重复 identity

#### Scenario: 旧 schema 输入

- **WHEN** Patch 或 Snapshot 请求使用旧 schema
- **THEN** service MUST返回明确 unsupported schema 错误
- **AND** MUST不通过 index、display name 或 path fallback apply

### Requirement: Agent Snapshot schema v6 必须输出稳定 authoring identity

Agent Snapshot MUST使用 schema v6，并为 Graph、Node、Edge、Timeline、Track、Clip、Blackboard declaration 与 Timeline animation producer 输出正式稳定 authoring identity。Snapshot path 和列表 index MAY作为可读定位信息，但 MUST不取代 identity。Snapshot MUST不输出 Tree animation Driver、ExecutionLineage、LayerPlan 或 runtime playback lifecycle。

#### Scenario: 导出 Full Snapshot

- **WHEN** Agent exporter 导出 CharacterPipelineDefinition Full Snapshot
- **THEN** 每个 Graph、Node、Edge、Timeline、Track、Clip 和 animation producer MUST包含稳定 authoring identity
- **AND** snapshot MUST输出当前 source revision 所需的逻辑与 Timeline 内容

#### Scenario: Timeline Track 重排后导出

- **WHEN** 作者重排 Track 或 Clip 后重新导出 Snapshot
- **THEN** 对应元素和 producer identity MUST保持
- **AND** index/path MAY更新

### Requirement: Agent 不得形成第二个动画表现 authoring 入口

本 change 中 Agent Patch compiler MUST只继续编辑正式 Graph、StateMachine、Timeline 与 Blackboard authoring。它 MUST不创建或修改 Presentation Driver、Pipeline 自有 transition 表、Animancer TransitionLibrary 或动画 Priority。后续若需要 Agent 编辑 Animancer 原生 transition，必须通过独立 capability 定义唯一 authoring service。

#### Scenario: Patch 请求创建动画 Driver

- **WHEN** Agent Patch 包含旧 Presentation Driver、HandoffRole 或 Tree lifecycle animation site operation
- **THEN** compiler MUST返回 unsupported operation
- **AND** MUST不转换成默认 transition 或写入 Graph/Timeline

#### Scenario: Patch 请求配置动画层

- **WHEN** Agent Patch 包含 `configure_animation_layer` 或 animation layer payload
- **THEN** schema/compiler MUST将其作为未知操作拒绝
- **AND** Presentation Layer catalog MUST只能由 CharacterPipelineDefinition Inspector 修改
