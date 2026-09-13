# character-csharp-authoring Specification

## Purpose

定义通过两个显式作者 MCP 在 C# 创建代码与正式 Graph、Timeline、Pose 和 EventGraph 资产之间转换的完整合同。代码能够重建明确范围的内容，人工资产编辑不会自动生成代码；正式业务 API 继续拥有对象创建、校验、事务和保存语义，旧 Agent 目录包与同步工具不再存在。

## Requirements

### Requirement: 作者 MCP 必须只有导出代码和创建资产两个入口

本作者功能 MUST 仅提供 `btsmtl.export_code` 与 `btsmtl.generate_assets` 两个显式 MCP。前者从明确资产完整导出 C#，后者执行明确的已编译 C# 创建入口并保存生成资产。两个工具 MUST 只调用正式导出和创建能力，不包含独立节点、字段、Timeline 局部修改协议，不新建 server 或恢复 Document 生命周期。原有独立运行产物 Build 与其它业务工具 MUST 保持自身职责，不自动并入或由作者工具触发。

#### Scenario: 发现作者工具

- **WHEN** 正式作者程序集与 MCP 完成加载
- **THEN** 本作者功能 MUST 只提供 `export_code` 与 `generate_assets`
- **AND** MUST 不提供旧 checkout、rebase、dry-run、apply、validate 以及替代的 sync 或节点级工具

### Requirement: 人工修改资产不得自动生成代码

人工编辑 Graph、Timeline、Profile、Curve 和布局及保存资产 MUST 不导出或改写 C#。源码编译、导入、selection、Inspector 重绘和文件变动 MUST 不自动触发任一作者 MCP。只有明确 `export_code` 请求才更新输出代码，只有明确 `generate_assets` 请求才按代码生成资产；系统 MUST 不建立后台同步、源码 Undo、文件监听或编辑操作日志。

#### Scenario: 人工编辑后显式导出

- **WHEN** 作者改变正式资产并随后明确请求 `export_code`
- **THEN** 输出代码 MUST 表达当前资产内容
- **AND** MUST 不追加过去的操作历史

### Requirement: 导出必须读取正式资产并完整输出创建代码

`export_code` MUST 接受精确输入资产、Definition 上下文及明确输出代码路径，直接读取正式 Graph、Timeline、Pose 或 EventGraph 及其完整闭包，输出可编译且调用正式业务 API 的 C# 创建代码。导出 MUST 不读取或解析旧 C#，不先导出 JSON，不生成第二份可编辑领域模型，不 dirty 输入资产。旧源码的循环、条件、变量命名、注释和手写组织 MUST 不作为保持要求。

#### Scenario: 从没有历史源码的正式图导出

- **WHEN** 一个正式图从未有过对应 C# 源文件
- **THEN** `export_code` MUST 仍能输出其完整创建代码
- **AND** MUST 不要求源码位置映射、Document 或历史记录

### Requirement: 输出必须覆盖正式配置和业务顺序

导出 MUST 完整表达生成范围内对象类型、重建必需身份、作者参数与节点值、Blackboard 声明、动态端口配置、Macro 接口、FSM 状态与转移、Graph 连接、Timeline 轨道/片段/Section、外部 binding、资源引用、作者曲线定义和节点位置等布局。系统 MUST 保持显式业务顺序，不以整理源码为由改变转移 order、轨道/片段或端口顺序。输出 MUST 仅包含恢复这些正式内容所需的作者配置；正式 API 可确定恢复的默认值、固定结构和可推导信息 MUST 不重复输出。Timeline 源曲线 MUST 通过 Clip 数据段引用恢复，独立作者曲线 MUST 表达关键帧和插值配置，不输出逐 tick 采样。无法表达或不能确定用途的正式字段 MUST 定位对象和字段并拒绝完整导出，不得静默省略、输出占位或生成默认内容。

#### Scenario: 遇到未支持的正式字段

- **WHEN** 输出适配不能表达某个正式配置字段
- **THEN** 导出 MUST 明确报告对象、字段和原因
- **AND** MUST 不以缺字段代码替换目标源码并声称成功

#### Scenario: 默认值与作者布局

- **WHEN** 正式创建 API 能确定恢复某个默认字段，而资产另有非默认配置和正式 layout
- **THEN** 导出 MUST 省略冗余默认赋值并保持非默认配置和 layout
- **AND** MUST 不以字段不参与运行计算为理由丢失正式作者内容

#### Scenario: 显式覆盖值恰好等于默认值

- **WHEN** 作者显式覆盖的值等于当前默认值，但覆盖状态会改变正式继承语义
- **THEN** 导出 MUST 保留该覆盖意图
- **AND** MUST 不仅凭值相等省略有意义的作者配置

### Requirement: 导出必须只保留最小重建闭包

输出 MUST 只包含正式 API 恢复目标资产所需的类型、拓扑、业务顺序、owner、根绑定、有效作者配置、正式引用和必要身份。默认值、固定端口/结构、可推导值、编辑器视口/选中态、生成过程、历史日志、诊断 hash 和变量名中的 identity/hash 后缀 MUST 省略。graph、node、edge、variable identity 只有在正式引用、稳定拓扑或 owner 关系需要时才 MUST 保留原值；外部资源需要精确定位时 MUST 保留正式路径和 localFileId。同一外部资源 MUST 在生成代码中声明一次并复用，不能通过压缩字符串或第二份数据模型隐藏字段。

#### Scenario: 默认配置不进入生成源码

- **WHEN** 正式对象的配置等于当前正式创建 API 的默认语义
- **THEN** 生成源码 MUST 依赖正式 API 恢复该默认值
- **AND** MUST 不重复写出默认赋值、派生值或固定结构

#### Scenario: 同一外部资源被多处引用

- **WHEN** 生成范围内多个节点或 Clip 引用同一外部资源
- **THEN** 生成源码 MUST 只创建一个类型化外部引用变量
- **AND** 后续使用 MUST 引用该变量，不重复写路径和 localFileId

#### Scenario: identity没有正式消费者

- **WHEN** 内部 identity 不参与正式引用、稳定拓扑、owner 或清理关系
- **THEN** 导出 MUST 不把该 identity写入源码
- **AND** 正式创建 API MUST 负责分配需要的运行身份

### Requirement: Timeline 导出必须表达 Clip 数据段和独立曲线

Timeline 输出 MUST 表达正式 Clip/数据段引用、RootMotionCurveAsset 源及其源起止区间、时间轴位置及速度、混合、循环或其它作者覆盖；不得复制源 Clip 的逐 tick 数据、烘焙/分析缓存或运行编译结果。MotionCurve 的 PositionX/Y/Z/Yaw MUST 只通过类型化源引用恢复，不得输出 Timeline-local 曲线字面量。Timeline 上独立作者曲线 MUST 保留完整关键帧、切线、权重、插值和正式时间域，不得重采样、有损抽点或用默认值替代非默认曲线。源资源和范围外共享对象 MUST 保持外部引用，不得隐式复制。

#### Scenario: Clip只使用源数据的一段

- **WHEN** Timeline Clip 指定源资源、源区间和时间轴位置
- **THEN** 生成代码 MUST 通过正式 Clip 创建 API 恢复该数据段
- **AND** MUST 不生成每帧或每 tick 的时间映射数组
- **AND** MotionCurve MUST 只生成一次类型化 RootMotionCurveAsset 外部引用

#### Scenario: 独立作者曲线包含非默认关键帧

- **WHEN** Clip 拥有独立作者曲线或覆盖曲线
- **THEN** 生成代码 MUST 保留其关键帧、插值/切线/权重和时间域
- **AND** MUST 不把曲线替换成默认曲线或密集采样结果
- **AND** MUST 不把源运动曲线复制为局部覆盖

#### Scenario: 多个 MotionCurveClip 使用同一源

- **WHEN** 多个 MotionCurveClip 使用同一 RootMotionCurveAsset 的不同区间
- **THEN** 源码 MUST 复用一次类型化源声明，并分别表达片段配置
- **AND** generate_assets MUST 不创建多个曲线源副本

### Requirement: 生成类不得承载调度元数据

生成类 MUST 只实现正式执行合同和 C# 类型声明，不得输出绝对 SourceCodePath、重复 EntryTypeName 或仅用于调度的 RecipeType 实例属性。source_code_path、entry_type_name、recipe_type 仍 MUST 由工具请求与现有生成服务处理；服务 MUST 确认请求入口类型、精确源码与当前编译脚本关联，不得新增 manifest、注册表或生成类自报路径来替代该关联。

#### Scenario: 源码路径不依赖开发者机器

- **WHEN** 生成源码被移动到另一台开发机器或项目 checkout
- **THEN** 生成类 MUST 仍只依赖正式执行上下文
- **AND** 工具 MUST 使用调用请求和当前 Unity 脚本关联确认入口，不读取源码内的绝对路径属性

### Requirement: 导出必须处理环与共享引用

导出 MUST 按正式 owner 和对象身份收集生成闭包，区分对象创建、配置、引用绑定和连线阶段。图的执行环 MUST 不造成导出递归不终止，共享对象 MUST 不被每个引用者分别复制；系统入口 MUST 遵守现有工厂创建规则，动态端口必须在连线前形成合法形状。

#### Scenario: 图包含环和共享子图

- **WHEN** 合法图包含执行环且多个节点引用同一内部子图
- **THEN** 代码 MUST 只创建一次共享子图并在所需对象存在后建立连接
- **AND** 执行代码 MUST 恢复原循环和共享关系

### Requirement: 公共机制必须通过领域薄适配消费正式内容

公共导出/生成机制 MUST 只拥有输出上下文、依赖排序、对象变量映射、通用 C# 表达式和最小生成入口。各领域 MUST 从正式对象读取身份、内部 owner、外部依赖及配置，并输出正式创建、配置、连接和根绑定 API 调用；不得建立第二领域模型、字段表或中央 Validator。某个正式领域或内容尚未支持时 MUST 拒绝该根的完整导出，不能省略后报告成功。

#### Scenario: 正式根包含未支持内容

- **WHEN** 根资产包含相应领域适配尚未完整支持的内容
- **THEN** `export_code` MUST 返回对象或字段的未支持诊断
- **AND** MUST 保持已有目标源码，不生成省略内容的替代结果

### Requirement: 生成必须执行明确代码入口并保存指定输出

`generate_assets` MUST 接受精确源码路径、对应已编译的正式创建入口类型、Definition 上下文与输出资产路径。入口 MUST 使用正式创建合同与业务 API，工具 MUST 保存生成结果并返回实际资产路径和诊断。系统 MUST 不接受任意 C# 正文、任意方法调用或反射字段修改参数。源码尚未成功编译或 Unity 处于编译、导入、Play 或切换 Play 时 MUST 拒绝生成，不得执行不匹配的旧编译结果。

#### Scenario: C# 入口已经编译

- **WHEN** 调用方明确指定已成功编译的正式 C# 入口并请求生成
- **THEN** 工具 MUST 创建或替换指定范围资产并返回根输出
- **AND** MUST 不要求先有旧资产或 Agent 工作包

#### Scenario: 源码编译失败

- **WHEN** 请求的源码尚未成功编译
- **THEN** 工具 MUST 报告未就绪或编译错误
- **AND** MUST 不使用上次同名类型的旧结果生成资产

### Requirement: 生成范围必须可删除重建且保持逻辑身份

已导出的 C# MUST 足以在相同外部资源和正式 API 版本下重建其明确生成范围，不依赖旧生成资产正文或旧生成子资产 GUID。代码 MUST 保持正式引用、稳定拓扑或 owner 关系所需的业务图、节点、边、变量及其它元素 identity；无上述用途的身份 MUST 不复制原值，由正式创建能力分配。内部引用 MUST 使用本次创建对象，物理 Unity 实例或文件身份不作为永久保留要求。再次完整生成 MUST 替换其拥有的输出范围，不不断追加重复对象，并通过领域正式 API 明确恢复 Profile、Definition 等根 owner/消费者绑定。重建 MUST 保持正式内容等价，不要求逐字复制原 YAML 或旧源码。

#### Scenario: 删除生成图后重建

- **WHEN** 生成范围内旧资产已不存在而源码及真实外部输入仍存在
- **THEN** `generate_assets` MUST 恢复等价对象、必需逻辑身份、配置、owner 和引用
- **AND** MUST 恢复本次明确指定的根绑定，不只产生孤立图

#### Scenario: 内部边没有身份消费者

- **WHEN** 一条边的关系可由本次创建的端点、端口、配置和顺序表达，且无正式稳定身份或 owner 引用要求
- **THEN** 导出 MUST 不复制其旧 GUID
- **AND** 重建 MUST 恢复该边及其顺序，保留端点相同但业务上独立的多条边

#### Scenario: 范围外正式消费者依赖节点身份

- **WHEN** 正式消费者需要按稳定节点身份绑定生成范围内元素
- **THEN** 导出 MUST 保留该必需身份并恢复明确绑定
- **AND** MUST 不因当前图内部没有身份引用而删除它

### Requirement: 外部输入与生成输出必须分开

导出 MUST 区分范围内生成对象与外部资源。原始 AnimationClip、Rig、素材、分析产物和范围外共享 Graph MUST 作为精确类型化引用或入口输入，不隐式复制或删除。Definition、Profile、Prefab 只有明确属于本次生成或绑定范围时才允许修改；系统 MUST 不扫描场景、同名资产或目录猜测缺失依赖。旧 Document 的直接业务能力迁移为正式领域 API，不要求导出一个图就生成整个角色和全部素材。

#### Scenario: 引用范围外共享资源

- **WHEN** 导出图引用范围外 Macro、Graph 或动画素材
- **THEN** 代码 MUST 表达明确外部引用
- **AND** 清理旧生成输出 MUST 不删除这些资源

### Requirement: 导出和生成必须具有明确的非同步语义

`export_code` MUST 以当前资产为输入完整覆盖明确目标代码内容；`generate_assets` MUST 以当前指定代码为输入完整生成其声明范围。未导出的人工编辑 MUST 不被视为已进入源码，显式重新生成 MUST 不自动合并这些改动。两个工具 MUST 不实现 rebase、双向冲突合并或整包 hash 协议，不能宣称代码与资产始终同步。

#### Scenario: 未导出人工调整后重新生成

- **WHEN** 作者调整资产但没有 `export_code`，随后使用旧代码执行 `generate_assets`
- **THEN** 输出 MUST 按指定代码重新创建
- **AND** MUST 不自动把人工调整反推到源码或建立另一条同步路径

### Requirement: 业务规则必须继续由正式模块承担

代码创建与人工编辑 MUST 共用正式 Graph、Timeline、Presentation 及 Curve 规则，整角色诊断 MUST 复用正式编译器。导出只增加输出完整性检查，不复制领域规则。Agent 协议校验与重复业务校验 MUST 删除；仅在正式模块真实缺少时补入所属模块，不得新建中央 Agent 替代 Validator。

#### Scenario: 不合法的片段或连接

- **WHEN** 创建代码要求非法片段配置或不兼容端口连接
- **THEN** 正式业务入口 MUST 按与人工编辑相同的规则拒绝
- **AND** MUST 不依赖 Agent preflight 或 Document Validator

### Requirement: 结果必须如实报告且不触发运行产物 Build

`export_code` MUST 在完整输出检查成功后写目标源码，失败不得以半份输出替换已有文件；响应 MUST 返回代码文件、正式入口和依赖诊断。`generate_assets` MUST 使用实际生成范围的正式编辑和保存能力，响应 MUST 区分创建和保存结果；失败或恢复未完成 MUST 指出影响对象，不报告完整成功。两者 MUST 不自动发布 Program、Projection、Play 或修改源码之外的无关文件。

#### Scenario: 导出中途发现未知内容

- **WHEN** 某字段无法输出导致导出失败
- **THEN** 工具 MUST 返回明确诊断且保留原目标文件
- **AND** MUST 不修改输入图或触发 Build

### Requirement: GameplayAbilityDefinition必须是完整技能生成根

`export_code`从`GameplayAbilityDefinition`导出时 MUST覆盖Ability identity、准入规则、目标要求、效果引用、结束规则、后续Ability关系、唯一私有AbilityGraph及其FSM、Condition、Macro、Timeline和Slot引用。`generate_assets` MUST按同一范围创建或替换Ability主资产、私有AbilityGraph以及声明范围内已有的`AbilityGrant`；只有迁移旧Skill根时才创建明确的角色授予，不得给独立Ability凭空添加授予，也不得只创建孤立Graph或继续依赖旧SkillDefinition/SkillGraphs。普通Ability节点和Timeline MUST通过当前`AbilityExecutionContext`工作，不要求手配空`ActionContextSlot`；公共作者入口仍只能是`btsmtl.export_code`与`btsmtl.generate_assets`。

#### Scenario: 生成完整Gameplay Ability

- **WHEN** 作者明确指定GameplayAbilityDefinition源码、Definition上下文和输出范围
- **THEN** 生成 MUST恢复完整Ability外壳、私有执行图和明确授予/输入绑定
- **AND** MUST不创建旧Skill外壳、Action Exit清理图或第二份执行状态

### Requirement: 旧 Agent 协议与工具必须激进删除

系统 MUST 删除旧五个 BTSMTL authoring 工具、Agent Window、Document/Snapshot/Codec/Store/Exporter/Reconciler、专属 Mutation/Session/Validator/Report 和无消费者的协议 DTO、测试及依赖。新导出/生成入口 MUST 不转发旧工具、保留兼容包或把 Agent 框架换名搬迁；正式领域代码和新代码输出器只保留其实际职责。

#### Scenario: 完成切换

- **WHEN** 两个显式作者工具及正式业务 API 已接通
- **THEN** 当前源码与注册项 MUST 不保留旧 Agent 生命周期和兼容入口
- **AND** 正式 Graph、Timeline 和 Pose 人工编辑 MUST 可以独立使用

### Requirement: 旧协议删除必须以作者调用链迁移完成为条件

旧 Agent 公共协议的删除 MUST 以受影响作者调用者已脱离 Agent、正式编辑/生成/保存可用、混合文件内有效领域操作已承接为门槛。有效领域 API 或作者对象不得因目录或类型含 Agent 或 Document 名称而随协议删除。作者退役与独立运行参数桥或动画变量运行闭环 MUST 分别记录，不得把运行闭环尚未完成当作恢复旧作者协议的理由，也不得以作者工具完成宣称运行闭环完成。

#### Scenario: 混合文件仍含正式操作

- **WHEN** 待删除协议文件仍包含被正式编辑调用的有效操作
- **THEN** 删除 MUST 等待该领域正式承接并使作者调用链脱离 Agent
- **AND** MUST 不整目录删除、复制旁路或保留兼容转发完成切换

#### Scenario: 作者退役但运行参数桥仍未替换

- **WHEN** 作者调用者已脱离 Agent 且正式编辑、生成和保存可用，独立运行参数桥仍在其负责领域待处理
- **THEN** 无消费者旧作者协议 MUST 按本次删除范围退役
- **AND** 运行参数桥状态 MUST 单独报告，不纳入作者工具完成声明

### Requirement: 协议退役与导出不得改变既有领域业务

Foot Motion 完整曲线组、Clip 与 Timeline 各自 owner 和时间域、MotionWarp 源引用、Action target、Animation channel、FSM Edge 参数与顺序 MUST 保持正式领域规则；代码导出和执行不得新增同义数据源。生成的 Foot Analysis、Program 和 Projection 不得因导出而变成可写素材，也不得扩大到未授权的 Rig 或 Body Motion 算法。

#### Scenario: 不完整 Foot Motion 数据组

- **WHEN** 创建代码只提交要求整组修改的曲线中的一条
- **THEN** 正式曲线 API MUST 拒绝不完整数据组
- **AND** MUST 不通过省略校验或 JSON 中转完成写入

### Requirement: Timeline 创建源码必须引用 Clip 数据段

Timeline 源码 MUST 表达精确 Clip 数据段引用、源区间、时间轴位置及作者配置的速度、混合等播放参数与覆盖值。已有源 Clip 曲线、逐 tick 采样、烘焙/分析缓存和编译结果 MUST 不复制进生成源码，MUST 由源引用及现有正式系统恢复。源 Clip 与 Timeline 的 owner、各自时间域、正式曲线组规则 MUST 保持不变。范围外源资源 MUST 作为重建输入，范围内共享源定义 MUST 仅创建一次。源码数据量 MUST 由作者对象、引用和配置决定，不因派生采样频率或采样数量而增加。

#### Scenario: 同一 Clip 数据段用于多个片段

- **WHEN** 多个 Timeline 片段使用同一源 Clip 的不同区间
- **THEN** 源码 MUST 复用源 Clip 引用，分别表达源区间、时间轴位置与作者覆盖
- **AND** MUST 不为每个片段输出源曲线或逐 tick 数组

#### Scenario: 作者独立编辑 Timeline 曲线

- **WHEN** Timeline 存在不能仅通过源数据段恢复的独立作者曲线或覆盖曲线
- **THEN** 源码 MUST 保留其作者关键帧、必要切线/权重、插值与时间域
- **AND** MUST 不输出重采样数组、不进行有损抽点，也不修改源 Clip 的曲线

#### Scenario: 只有派生样本而缺少正式源定义

- **WHEN** 导出无法取得恢复该内容所需的源数据段或独立作者关键帧
- **THEN** MUST 报告具体正式内容缺口并保持旧目标源码
- **AND** MUST 不把密集样本或新建的旁路素材冒充作者配置输出

### Requirement: 创建源码必须采用链式 builder 表达作者配置

生成 C# MUST 以偏函数式的链式 builder 组织节点及配置、Timeline 片段和连接，直接调用正式创建能力。源码 MUST 表达当前最终的作者选择、非默认值和有意义的显式覆盖，不依赖作者编辑历史，不区分 AI 与人工编辑来源。固定端口、默认值和派生内容 MUST 由正式系统恢复。每个节点的位置 MUST 仅表达一次，MUST 不复制选中态、视口与重绘缓存。Builder MUST 不建立第二份持久化领域模型、操作日志或执行路径。

#### Scenario: 创建带有 Timeline 的节点

- **WHEN** 导出包含位置、非默认参数和 Clip 片段的业务节点
- **THEN** 源码 MUST 以节点创建和链式配置表达位置、参数及嵌套 Timeline 配置
- **AND** MUST 不展开工厂固定结构和源 Clip 派生数据

#### Scenario: 链式创建包含循环与共享对象

- **WHEN** 图包含循环或被多个节点引用的共享子图
- **THEN** 源码 MUST 允许先声明对象再连接并只创建一次共享对象
- **AND** MUST 不为组成单一调用链改变拓扑或业务顺序

### Requirement: 创建源码必须只表达最小重建闭包

源码 MUST 只表达正式资产重建必需的对象、配置、拓扑、身份、根绑定与精确外部输入。源码 MUST 不包含变量名中的 GUID/hash 后缀、不参与重建的 GUID、历史/操作顺序日志、诊断 hash 或生成过程信息。共享内部对象与重复外部资源 MUST 复用引用；真实外部输入的 GUID/localFileId 或正式相对路径定位信息 MUST 按精确解析需要保留，不得一并清除。未知字段 MUST 不被当作无用字段。

#### Scenario: 同一素材被多处引用

- **WHEN** 多个节点使用同一外部子资源
- **THEN** 源码 MUST 声明一次精确资源引用并复用
- **AND** MUST 保留区分子资源所必需的身份，不复制素材本身

#### Scenario: 输出局部对象名称

- **WHEN** 导出器为图、节点或连接分配局部变量
- **THEN** MUST 使用可读名称和必要的局部重名序号
- **AND** MUST 不将业务 GUID 再编码进变量名称

### Requirement: 工具调度元数据必须与资产创建内容分离

源码 MUST 不硬编码本机绝对 SourceCodePath，MUST 不携带仅用于调度的 recipe 和重复入口类型名属性。类型声明及正式执行合同 MUST 足以表达创建入口；精确源码路径、入口类型与 recipe 由工具请求/响应及现有编译关联承担，不写入生成资产，不建立另一个持久化注册协议。工具 MUST 保持精确源码与当前已编译类型的关联，源码未成功编译或关联不明确时 MUST 拒绝执行旧结果。

#### Scenario: 项目移动到另一台机器

- **WHEN** 项目位置变化且相同生成源码已在当前项目成功编译，调用方给出当前精确源码与入口
- **THEN** 工具 MUST 通过当前编译关联定位并执行该入口
- **AND** MUST 不要求编辑源码中的旧机器绝对路径
