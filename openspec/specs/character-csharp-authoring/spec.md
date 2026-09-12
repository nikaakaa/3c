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

导出 MUST 完整表达生成范围内对象类型、稳定身份、参数、节点值、Blackboard 声明、动态端口、Macro 接口、FSM 状态与转移、Graph 连接、Timeline 轨道/片段/Section、外部 binding、资源引用、曲线与 layout。系统 MUST 保持显式业务顺序，不以整理源码为由改变转移 order、轨道/片段或端口顺序。无法表达的正式内容 MUST 定位对象和字段并拒绝完整导出，不得静默省略、输出占位或生成默认内容。

#### Scenario: 遇到未支持的正式字段

- **WHEN** 输出适配不能表达某个正式配置字段
- **THEN** 导出 MUST 明确报告对象、字段和原因
- **AND** MUST 不以缺字段代码替换目标源码并声称成功

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

已导出的 C# MUST 足以在相同外部资源和正式 API 版本下重建其明确生成范围，不依赖旧生成资产正文或旧生成子资产 GUID。代码 MUST 表达并保持业务图、节点、变量及其它正式元素的稳定 identity，内部引用使用本次创建的对象；物理 Unity 实例或文件身份不作为内容永久保留要求。再次完整生成 MUST 替换其拥有的输出范围，不不断追加重复对象，并通过领域正式 API 明确恢复 Profile、Definition 等根 owner/消费者绑定。

#### Scenario: 删除生成图后重建

- **WHEN** 生成范围内旧资产已不存在而源码及真实外部输入仍存在
- **THEN** `generate_assets` MUST 恢复等价对象、逻辑身份、配置、owner 和引用
- **AND** MUST 恢复本次明确指定的根绑定，不只产生孤立图

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
