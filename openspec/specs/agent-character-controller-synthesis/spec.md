# agent-character-controller-synthesis Specification

## Purpose
定义 Agent 在编辑器内通过 Snapshot、Intent、Macro、Patch IR、Compiler、Validator 和 Report 生成并修复正式 BTSMTL 角色控制器资产的唯一链路。

## Requirements

### Requirement: Agent必须保持Generated Foot Analysis只读

Agent v15 CharacterController Snapshot/Patch MUST继续只描述正式Graph、StateMachine、Timeline、Marker、registered editable Curve Channel和Profile只读identity。Animation Clip的Foot Placement Weight MUST继续作为完整可编辑curve进入Snapshot/Patch；Projection生成的左右脚sole speed、height、plant confidence、next landing confidence、delay与offset MUST不进入Patch operation、Curve Channel Catalog或可写Snapshot字段。Agent MUST不复制generated payload，也 MUST不创建Foot Analysis专用mutation。

#### Scenario: Agent导出带Foot Analysis的Timeline

- **WHEN** Definition拥有Ready Projection和生成Foot Analysis
- **THEN** Snapshot MUST继续输出Animation Clip的Foot Placement Weight作者curve
- **AND** MUST不把generated LeftPlant或Landing curve伪装为Timeline owner数据

#### Scenario: Agent尝试写Generated channel

- **WHEN** v15 Patch提交未登记的LeftPlant、RightPlant或Landing ChannelId
- **THEN** Lowerer MUST按未知Curve Channel拒绝整个事务
- **AND** MUST不修改Timeline、Projection或Analysis Source

### Requirement: Agent Validator必须透传正式Foot Analysis编译诊断

Agent Validator MUST透传正式Artifact Builder、Artifact Store、Projection binding与Build Transaction诊断，区分Missing、Stale、Corrupt、Source/Rig/Calibration不匹配和stable clip binding缺失。Agent MUST不采样AnimationClip、不写artifact、不输出feature payload，也不得新增Foot Analysis rebuild或generated curve mutation。

#### Scenario: Agent修改Foot Placement Weight

- **WHEN** 合法v15 Patch只修改现有Foot Placement Weight
- **THEN** apply后正式Definition Build MUST重新校验所需artifact并发布Projection
- **AND** Agent MUST不直接读取或修改artifact文件

#### Scenario: Artifact损坏

- **WHEN** Validator发现当前clip expected artifact为Corrupt
- **THEN** Compile Report MUST定位Clip、Source、Rig、Calibration和artifact identity
- **AND** Agent MUST不使用Timeline、Projection或默认feature修复该文件

### Requirement: Agent 生成链路必须是 editor-only authoring 编译链路

系统 MUST将 Agent生成角色动作控制器实现为 editor-only authoring编译链路。Agent JSON、Intent、Macro和 Patch IR MUST只服务编辑期生成、修复和评估。运行时 MUST只执行由正式 BTSMTL asset编译得到的 `CharacterSimulationProgram`，并由 Session Pipeline的 Program Evaluate/Finalize Pass推进 Action operation、Timeline operation与 GameplayFacts。系统 MUST NOT在 Gameplay Runtime、Pipeline Pass、服务端或网络同步路径中执行 Agent JSON或调用 LLM。

#### Scenario: 运行时加载角色

- **WHEN** CharacterPipelineHost向 Session Host注册角色
- **THEN** runtime MUST只读取已发布 ProgramAsset、Projection和 Session composition
- **AND** MUST不读取 Agent Intent、Patch IR、LLM输出文件或运行时 authoring Graph

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

系统 MUST定义schema v15 Agent Patch IR作为CharacterController与AIController唯一确定性的graph编辑指令边界，并使用显式domain discriminator选择根合同。Patch IR MUST使用stable authoring id或前序operation output引用定位编辑目标，只能表达正式authoring操作。CharacterController domain MUST保留State、Action、Timeline、MotionWarp、Marker与Curve typed operation；AIController domain MUST只增加AI Definition、AI Graph、AI Blackboard、Configured Candidate、Observation、Memory与Intent operation。资产引用 MUST作为实际消费该资产的ensure command参数，由对应正式handler原子解析和写入。Patch IR MUST不直接写Unity YAML、GUID映射集合、runtime状态或旧配置路径，也 MUST不提供独立通用`bind_asset_reference`操作。

#### Scenario: 添加状态

- **WHEN** Patch IR表达添加`Attack1`状态
- **THEN** lowerer MUST生成typed State command
- **AND** handler MUST通过正式节点创建入口创建StateNode
- **AND** Patch IR MUST不包含直接插入节点集合的操作

#### Scenario: 连接Transition

- **WHEN** Patch IR表达`Attack1 -> Attack2`
- **THEN** lowerer MUST生成typed Transition command与typed element reference
- **AND** handler MUST通过正式flow link入口创建Transition edge
- **AND** 合法Transition MUST拥有inline ConditionRuleGraph

#### Scenario: 请求独立资产绑定

- **WHEN** schema v15 Patch包含`bind_asset_reference`
- **THEN** lowerer MUST将其作为未知operation拒绝
- **AND** 系统 MUST不返回成功no-op
- **AND** 资产绑定 MUST改由对应ensure command携带明确引用

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

系统 MUST保持 Agent 生成后的正式结果为普通 BTSMTL Graph、Timeline、ActionProfile，以及由 CharacterPipelineDefinition 引用的 CharacterAnimationPresentationProfile。作者 MUST能在 Graph Editor 调整逻辑，在 Timeline Editor 调整 clip/time，在 CharacterAnimationPresentationProfile Inspector 调整Pose Graph、Blend Library、Rig与producer source binding。Agent Snapshot MAY只读理解Profile与Presentation identity，但Agent Patch MUST不形成第二个Presentation写入口。

#### Scenario: 作者微调生成结果

- **WHEN** Agent 生成普通 Tree branch、Attack State 与 Timeline
- **THEN** 作者 MUST在 Graph Editor 调整 logic rule
- **AND** 在 Timeline Editor 调整 clip/time
- **AND** 在 CharacterAnimationPresentationProfile Inspector 调整Pose Graph、Blend Library、Rig与producer source binding
- **AND** 三个入口 MUST不双写同一字段

#### Scenario: Agent 继续修改

- **WHEN** 作者微调后再次请求 Agent 增加 dodge cancel
- **THEN** Agent MUST基于新的 Graph、Timeline 与只读 producer identity 生成增量 Patch
- **AND** MUST不覆盖作者在CharacterAnimationPresentationProfile、Pose Graph或Blend Library中的修改

### Requirement: Agent Snapshot 与 Validator 必须递归理解嵌套 StateMachine

Agent Snapshot MUST递归输出完整RootTree authoring routes、普通RunnableNode、flow edges、inline/shared Graph、nested StateMachine、logical transitions、Action activation、Timeline与稳定animation producer identity。Presentation section MUST只读输出Pose Graph、Blend Library、Rig identity/revision、AnimationChannel到PoseSlot binding与producer source identity。Validator MUST检查Graph topology、route identity、Timeline identity与Timeline AnimationChannelId，但 MUST不校验或写入Presentation binding、Blend transition或runtime playback lifecycle。

#### Scenario: Corin Snapshot

- **WHEN** 导出 Corin compact Snapshot
- **THEN** Graph section MUST显示 Root Parallel、普通 Runnable、外层 None/Attack/Dodge、内层 Attack1/Attack2 与完整 route
- **AND** Presentation section MUST只读显示Pose Graph、Blend Library、Rig、AnimationChannel到PoseSlot与Timeline producer source identity
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

Agent Patch compiler MUST在更新现有元素时保持其authoring identity，在创建新元素时生成新identity，在复制元素时生成新identity。系统 MUST只接受schema v15，不得保留v14及更早兼容解析或按path、display name、Actor名称、Tag、列表index猜测identity。Typed command lowering MUST在mutation前验证domain、root identity、source revision、authoring identity格式、operation id唯一性和前序operation reference顺序。

#### Scenario: 更新现有Timeline Clip

- **WHEN** Patch修改一个由authoring identity指定的Clip参数
- **THEN** compiler MUST修改该Clip
- **AND** Clip identity MUST保持

#### Scenario: 平移现有Timeline Clip

- **WHEN** `move_timeline_clip`通过Timeline、Track与Clip identity平移现有Clip
- **THEN** handler MUST保持Clip identity并重算该Track的overlap mix
- **AND** MotionCurveClip MUST同步平移绝对CurveEndFrame

#### Scenario: 配置Timeline Clip self ease

- **WHEN** `configure_timeline_clip_ease`为现有Clip提交显式SelfEaseInFrame与SelfEaseOutFrame
- **THEN** lowerer与handler MUST拒绝负数、超出Duration或与当前overlap冲突的值
- **AND** Snapshot MUST同时输出self、other与effective ease帧数
- **AND** compiler MUST不按path、display name或列表index猜测目标Clip

#### Scenario: 创建新Track

- **WHEN** Patch创建新的Timeline Track
- **THEN** compiler MUST为该Track生成新identity
- **AND** validator MUST拒绝缺失或重复identity

#### Scenario: 创建新Marker occurrence

- **WHEN** Patch在现有AnimationTrack创建新的sync marker
- **THEN** compiler MUST为该occurrence生成新MarkerAuthoringId
- **AND** 相同MarkerId的其它occurrence identity MUST保持

#### Scenario: 替换完整Curve Channel

- **WHEN** Patch按owner stable identity与registered ChannelId提交完整curve
- **THEN** owner、Track与Clip identity MUST保持
- **AND** Keyframe MUST不生成持久AuthoringId
- **AND** preflight MUST拒绝与当前owner revision不一致的陈旧curve写入

#### Scenario: 旧schema输入

- **WHEN** Patch或Snapshot请求使用v6至v13 schema
- **THEN** service MUST返回明确unsupported schema错误
- **AND** MUST不通过converter、index、display name或path fallback apply

### Requirement: Agent Patch Compiler内部必须使用唯一类型化命令计划

系统 MUST将schema v15 `AgentPatchOperation`只作为editor-only JSON边界DTO，并通过唯一operation catalog与AgentPatchCommandLowerer一次降低为immutable typed command plan。CharacterController与AIController domain MUST复用同一lowering、planning symbol、preflight、资产事务和handler catalog基础；领域handler只消费各自正式authoring API。Dry-run与apply MUST消费同一typed command plan；后续Planner与Handler MUST不再次按原始`op`字符串解释宽DTO，也不得建立AI专用Patch compiler或第二事务。

#### Scenario: 同一Patch执行dry-run和apply

- **WHEN** AgentPatchAuthoringService收到合法schema v15 Patch并请求apply
- **THEN** service MUST先lower一次typed command plan并完成无副作用preflight
- **AND** apply MUST在资产级事务中消费相同plan
- **AND** MUST不重新解析出另一组operation语义

#### Scenario: 后序operation引用前序输出

- **WHEN** 后序typed command通过operation id引用前序command计划创建的State、Node、Edge或Marker
- **THEN** dry-run MUST通过窄planning symbol验证输出kind与owner scope
- **AND** apply MUST把前序实际创建对象注册到同一operation id
- **AND** 系统 MUST不创建虚拟Graph或Timeline clone来解析该引用

#### Scenario: 未知operation进入lowering

- **WHEN** Patch包含schema v15 catalog未登记的operation
- **THEN** lowerer MUST在任何资产mutation前返回结构化unknown operation错误
- **AND** MUST不选择fallback handler或动态反射实现

### Requirement: Agent Compiler模块必须按authoring职责聚合

`AgentPatchCompiler` MUST保持唯一Compiler facade，但单次Definition、Snapshot、Resolver、Graph Index、operation symbol、diff与touched owner MUST由每次调用独占的compile session拥有。StateMachine、StateBehavior、Node/Asset、GraphLink与ConditionRule MUST由按共享authoring不变量聚合的handler处理。Compiler MUST不拥有Undo、dirty、rollback或SaveAssets；这些资产事务职责 MUST继续只属于`AgentPatchAuthoringService`。

#### Scenario: 连续编译两个Definition

- **WHEN** 同一Compiler实例连续dry-run两个不同`CharacterPipelineDefinition`
- **THEN** 第二次调用 MUST创建新的compile session
- **AND** MUST不读取第一次调用的Resolver、Index、operation output或touched owner

#### Scenario: Apply修改多个inline与shared owner

- **WHEN** typed command plan修改多个可达Graph serialized owner
- **THEN** compile session MUST报告实际touched owner
- **AND** application service MUST在唯一Undo事务内统一dirty、验证和保存
- **AND** handler MUST不直接调用`AssetDatabase.SaveAssets`

### Requirement: 通用Agent Validator与业务样例覆盖必须分层

`AgentGraphValidator` MUST只检查对任意Character Definition成立的Graph kind、Condition纯度、Timeline ownership、serialized owner/path、TreeClip ownership、Action Context、Input/ActionProfile引用、authoring identity和正式Compiler语义。它 MUST不读取Definition名称，不得硬编码Corin、状态display name、连招数量、cancel key或具体transition集合。具体Macro的业务覆盖 MUST由Synthesis/Macro coverage evaluator在对应样例范围内检查typed command plan，MUST不进入普通`validate` action。

#### Scenario: 验证非Corin角色

- **WHEN** 作者验证一个使用不同Action状态名和不同连招层数的合法角色
- **THEN**通用Validator MUST只按正式authoring语义判断
- **AND** MUST不要求`None/Attack/DodgeBack/DodgeForward`或`Attack1/Attack2`

#### Scenario: 评估two_hit_combo Macro

- **WHEN** Synthesis Evaluator评估`two_hit_combo`
- **THEN** Macro coverage evaluator MUST检查该Macro的typed plan包含外层Attack、内层combo、两个攻击leaf、Timeline、combo与exit命令
- **AND**该检查 MUST只影响当前样例coverage report
- **AND**普通Graph validate MUST不执行该业务规则

### Requirement: Agent Snapshot schema v15 必须输出稳定 authoring identity

Agent Snapshot MUST使用schema v15和显式domain discriminator。CharacterController Snapshot MUST继续输出Graph、Node、Edge、Timeline、Track、Clip、AnimationSyncMarker、Curve owner、Blackboard declaration、CharacterInputProfile request timing与Timeline animation producer稳定identity。AIController Snapshot MUST输出AIControllerDefinition、AIControllerTree、Graph/Node/Edge、Node Capability、AI Blackboard declaration、显式候选Actor Perception binding、Character input/request binding与generated AI Program identity。Snapshot path和列表index MAY作为可读定位信息，但 MUST不取代identity；Snapshot MUST不输出runtime mutable state、AI candidate state或Perception缓存。v15 Snapshot MUST成为生成v15 Patch的唯一上下文，不提供v14镜像输出。

#### Scenario: 导出Full Snapshot

- **WHEN** Agent exporter导出CharacterPipelineDefinition Full Snapshot
- **THEN** 每个Graph、Node、Edge、Timeline、Track、Clip、AnimationSyncMarker和animation producer MUST包含稳定authoring identity
- **AND** snapshot MUST标记schema v15与明确domain
- **AND** snapshot MUST输出当前source revision所需的逻辑与Timeline作者内容

#### Scenario: Timeline元素重排后导出

- **WHEN** 作者重排Track、Clip或SyncMarker后重新导出Snapshot
- **THEN** 对应元素和producer identity MUST保持
- **AND** index与可读path MAY更新

### Requirement: Agent 不得形成第二个动画表现 authoring 入口

Agent Patch compiler MUST只编辑正式Graph、StateMachine、Timeline与Blackboard authoring。它 MUST不创建或修改CharacterAnimationPresentationProfile、Pose Graph、Blend Library、Rig、Presentation Driver、动画transition或Priority。若需Agent编辑Animation Presentation，必须由独立capability定义唯一authoring service。

#### Scenario: Patch 请求创建动画 Driver

- **WHEN** Agent Patch 包含旧 Presentation Driver、HandoffRole 或 Tree lifecycle animation site operation
- **THEN** compiler MUST返回 unsupported operation
- **AND** MUST不转换成默认 transition 或写入 Graph/Timeline

#### Scenario: Patch 请求配置动画表现

- **WHEN** Agent Patch包含配置Pose Graph、Blend Library、Rig或producer source binding的payload
- **THEN** schema/compiler MUST将其作为未知操作拒绝
- **AND** Animation Presentation MUST只能由正式Profile、Pose Graph与Blend Library编辑入口修改

### Requirement: Agent Snapshot 必须完整投影 MotionWarp authoring

Agent Snapshot MUST输出MotionWarp Track/Clip subtype、stable identity、SourceMotionClipId、resolved source path、position/rotation mode、target offset、weight、clamp和两条canonical progress curve。Snapshot MUST只投影正式Timeline资产，不输出Preview target或runtime mutable state。

#### Scenario: 导出带 MotionWarp 的 Timeline

- **WHEN** Agent导出包含MotionWarp的Character Definition
- **THEN** Snapshot MUST能唯一定位Warp与source MotionCurve
- **AND** Track重排后两者identity关系 MUST保持不变

### Requirement: Agent Patch 必须通过类型化命令修改 MotionWarp

Agent Patch MUST提供创建MotionWarp Track/Clip、配置source与typed参数及删除Clip的确定性命令。Lowerer MUST生成唯一immutable command plan，dry-run与apply MUST复用该plan；Handler MUST调用Timeline正式authoring API，MUST不直接编辑YAML、不按名称猜source，也 MUST不创建第二套MotionWarp配置。

#### Scenario: Agent 创建目标攻击 Warp

- **WHEN** Patch引用一个已存在或同事务创建的MotionCurveClip symbol
- **THEN** Lowerer MUST解析为stable source identity
- **AND** Handler MUST创建合法MotionWarpClip并保持source关系

### Requirement: Agent Validator 必须复用 MotionWarp 正式校验

Agent Validator MUST检查source identity、Timeline owner、窗口、Action channel、Override语义、mode、offset、weight、clamp、progress curve、Action Context与ActionTargetRequirement，并与Inspector和Semantic Compiler复用同一校验服务。任何错误 MUST定位到Graph、Timeline、Track、Clip、ActionProfile与相关source。

#### Scenario: Agent 配置缺少目标的 Warp

- **WHEN** Patch为`ActionTargetRequirement.None`动作增加MotionWarp
- **THEN** dry-run MUST失败并报告目标要求矛盾
- **AND** apply MUST不修改任何资产

### Requirement: Agent 必须完整修改 Action target authoring

Agent schema v15 CharacterController Patch MUST提供类型化operation创建或配置`ActionTargetSnapshot` Blackboard declaration、保存InputDerived InputValueId、绑定准入与激活节点，以及设置ActionProfile的`None`、`OptionalSnapshot`或`SnapshotRequired`。Lowerer、Handler与Validator MUST调用正式authoring API，MUST不直接编辑YAML、不按显示名猜引用，也 MUST不形成第二个Action target配置入口。

#### Scenario: 为攻击建立目标链

- **WHEN** Patch创建InputDerived ActionTargetSnapshot declaration并绑定Attack Profile、CanActivate与Activate
- **THEN** dry-run MUST验证所有引用属于当前Definition且类型匹配
- **AND** apply MUST通过同一immutable typed plan原子写入正式资产

#### Scenario: 查询与激活引用不同目标变量

- **WHEN** reachable `CanActivateAction`与`ActivateActionInstance`引用不同declaraction
- **THEN** Validator MUST报告准确Graph、Node与declaration identity
- **AND** artifact MUST不发布

#### Scenario: 可选目标攻击配置 MotionWarp

- **WHEN** ActionProfile声明`OptionalSnapshot`且Timeline MotionWarp配置完整
- **THEN** Agent Validator MUST接受该组合
- **AND** Snapshot MUST完整投影requirement、target references与Warp source


### Requirement: Agent Snapshot必须只读投影Body Motion Profile

Agent compact/full CharacterController Snapshot MUST从显式`CharacterPipelineDefinition`引用只读输出Body Motion Profile stable identity、content revision、GravityAcceleration、MaximumFallSpeed、semantic version、required AirborneVerticalMotion capability与正式Compiler配置状态。Snapshot MUST不输出runtime VerticalVelocity、pending integration plan或Solver mutable state。Agent schema v15 Patch MUST不增加Profile字段修改、任意SerializedProperty或第二Profile写入口；MCP bridge MUST不增加Body Motion专用mutation action。

#### Scenario: 导出Corin Character Snapshot

- **WHEN** Agent从Corin CharacterPipelineDefinition导出Snapshot
- **THEN** Snapshot MUST能说明当前Body Motion Profile与两个正式参数
- **AND** MUST显示Program是否要求AirborneVerticalMotion
- **AND** Patch catalog MUST不提供修改Profile的操作

### Requirement: Agent v15 CharacterController 必须完整读写 Timeline Marker 与 Curve Channel

Agent Snapshot/Patch schema MUST只接受`agent-character-controller-synthesis.v15`。CharacterController Snapshot MUST按Timeline与Track稳定identity输出sync mode、sync group、Finite/Cyclic topology、SyncRole、call site playback mode，以及每个marker的AuthoringId、MarkerId和frame；还 MUST按Curve owner stable identity输出Catalog登记的ChannelId、time domain、value domain、unit、wrap mode与完整Keyframe字段。Patch MUST保留typed configure、ensure、move和delete marker操作，并 MUST使用唯一`configure_timeline_curve_channel`按`OwnerAuthoringId + ChannelId + Full Curve`原子替换typed curve。Lowerer MUST生成immutable command plan，dry-run与apply MUST消费同一plan，handler与Timeline Editor MUST只调用Timeline正式authoring API和Curve Channel MutationAdapter，Validator MUST复用Marker Sync及各curve领域唯一校验服务。Marker MUST保持离散Point Marker语义。v14及更早reader、converter、operation alias和兼容分支 MUST删除。

#### Scenario: Agent导出循环producer

- **WHEN** Agent导出包含WalkLoop AnimationTrack的Full Snapshot
- **THEN** Snapshot MUST包含track stable identity、MarkerGroup模式、Locomotion.Gait、Cyclic topology和全部marker
- **AND** MUST不只输出显示名、asset path或数组index

#### Scenario: Agent导出有限producer

- **WHEN** Agent导出由Once TimelineNode调用的Finite AnimationTrack
- **THEN** Snapshot MUST输出frame 0到DurationFrame的marker序列
- **AND** MUST输出对应call site stable identity与Once模式

#### Scenario: Agent新增重复语义marker

- **WHEN** v15 Patch为Finite track确保第二个LeftPlant marker
- **THEN** dry-run MUST按不同MarkerAuthoringId接受该occurrence
- **AND** apply MUST通过同一plan创建稳定identity
- **AND** 再次导出 MUST按frame稳定显示两个LeftPlant occurrence

#### Scenario: Agent导出typed curve channel

- **WHEN** Agent导出包含MotionCurve Clip的Timeline
- **THEN** Snapshot MUST按owner stable identity输出Position X/Y/Z、Yaw、Weight与Ease channel
- **AND** 每个channel MUST包含稳定ChannelId、time/value domain、unit、wrap mode和完整key字段
- **AND** MUST不把字段名或Inspector path作为channel identity

#### Scenario: Agent修改weighted curve

- **WHEN** v15 Patch通过`configure_timeline_curve_channel`修改一个registered channel
- **THEN** Patch MUST提交完整curve并保留time、value、in/out tangent、in/out weight、WeightedMode与wrap mode
- **AND** handler MUST只调用该descriptor的正式MutationAdapter

#### Scenario: Agent提交未知curve channel

- **WHEN** v15 Patch提交Catalog未登记的ChannelId
- **THEN** lowerer MUST在mutation前拒绝
- **AND** MUST不按字段名、显示名或AnimationCurve类型猜测目标

#### Scenario: 旧schema输入

- **WHEN** Agent收到v13或更早Snapshot、Patch或operation payload
- **THEN** 工具 MUST明确拒绝schema不匹配
- **AND** MUST不升级、转换、猜测字段或调用旧parser

### Requirement: Agent 必须阻止 Marker Sync 数据分裂

Agent MUST只把Marker Sync作者数据写入AnimationTrack，不得写入CharacterAnimationPresentationProfile、TimelineNode、Blackboard、StateMachine edge、ActionProfile、FootPhase资产或generated Projection。Patch target MUST使用stable authoring identity或前序operation output；名称、路径、breadcrumb和列表index MUST不作为fallback。Agent Validator MUST覆盖Unspecified、None残留、marker identity、Finite/Cyclic边界、call site一致性、group directed pair contract与animation output coverage。

#### Scenario: Patch给None track保留marker

- **WHEN** Patch把AnimationTrack配置为None但同时提交SyncGroupId或marker
- **THEN** dry-run MUST拒绝整个事务
- **AND** apply MUST不产生部分资产修改

#### Scenario: Patch使用显示名寻址

- **WHEN** Patch只提供`RunLoop`显示名而没有Timeline/Track stable identity
- **THEN** lowerer MUST报告target identity缺失
- **AND** MUST不搜索第一个同名track

#### Scenario: Patch尝试配置TimelineNode覆盖

- **WHEN** Patch尝试在TimelineNode上保存sync group或marker override
- **THEN** operation catalog MUST将其作为未知或非法字段拒绝
- **AND** MUST不修改Program或PresentationCommand schema补偿
