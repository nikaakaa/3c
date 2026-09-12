## Purpose

定义通过两个显式作者MCP在C#创建代码与正式Graph/Timeline资产之间转换的完整合同。代码能够重建明确范围的内容，人工资产编辑不会自动生成代码；现有业务API负责对象创建与校验，图导出直接输出当前结构，彻底退役旧Agent目录包和同步工具。

## ADDED Requirements

### Requirement: 作者MCP必须只有资产导出代码和代码创建资产两个入口

本作者功能 MUST仅提供 `btsmtl.export_code` 与 `btsmtl.generate_assets` 两个显式MCP。前者从明确资产完整导出C#并写出源码，后者执行明确的已编译C#创建入口并保存生成资产。两个工具 MUST只调用正式导出和创建能力，不包含独立节点、字段、Timeline局部修改协议，不新建server或恢复Document生命周期。原有独立运行产物Build与其他业务工具 MUST保持其自身职责，不自动并入或由作者工具触发。

#### Scenario: 发现作者工具

- **WHEN** 正式作者程序集与MCP完成加载
- **THEN** 本作者功能 MUST只提供export_code与generate_assets
- **AND** MUST不提供旧checkout/rebase/dry-run/apply/validate以及替代的sync或节点级工具

### Requirement: 人工修改资产不得自动生成代码

人工编辑Graph、Timeline、Profile、Curve和布局及保存资产 MUST不导出或改写C#。源码编译、导入、selection、Inspector重绘和文件变动 MUST不自动触发任一作者MCP。只有明确export_code请求才更新输出代码，只有明确generate_assets请求才按代码生成资产；系统 MUST不建立后台同步、源码Undo、文件监听或编辑操作日志。

#### Scenario: 作者拖动并保存Timeline片段

- **WHEN** 作者改变片段起止帧并保存当前资产
- **THEN** 资产 MUST通过原正式编辑路径保存
- **AND** C#文件 MUST保持不变，不因该操作新增编译任务

#### Scenario: 人工编辑后显式导出

- **WHEN** 调用方随后明确请求export_code
- **THEN** 输出代码 MUST表达当前资产的片段时间
- **AND** MUST不追加过去拖动的操作历史

### Requirement: 导出必须读取当前正式资产并完整输出创建代码

export_code MUST接受精确输入资产、Definition上下文及明确输出代码路径，直接读取正式Graph/Timeline及其拥有的完整闭包，输出可编译且调用正式业务API的C#创建代码。导出 MUST不读取或解析旧C#，不先导出JSON，不生成第二份可编辑领域模型，不dirty输入资产。旧源码的循环、条件、变量命名、注释和手写组织 MUST不作为保持要求，输出 MUST统一表达当前完整结构。

#### Scenario: 从人工创建的图导出

- **WHEN** 一个正式图从未有过对应C#源文件
- **THEN** export_code MUST仍能输出其完整创建代码
- **AND** MUST不要求源码位置映射、Document或历史记录

#### Scenario: 当前图缺少旧节点

- **WHEN** 作者已经删除旧节点及关联边后请求完整导出
- **THEN** 输出代码 MUST不含这些节点和边
- **AND** MUST不输出先创建再删除的历史语句

### Requirement: 输出必须覆盖全部正式配置和业务顺序

导出 MUST完整表达生成范围内对象类型、稳定身份、参数、节点值、Blackboard声明、动态端口、Macro接口、FSM状态与转移、Graph连接、Timeline轨道/片段/Section/外部binding、资源引用、完整曲线与layout。系统 MUST保持显式业务顺序，不以整理源码为由改变转移order、轨道/片段或端口顺序。无法表达的正式内容 MUST定位对象和字段并拒绝完整导出，不得静默省略、输出占位或生成默认内容。

#### Scenario: 输出含权重切线的曲线

- **WHEN** 当前图包含有权重切线及wrap mode的正式Curve
- **THEN** 代码 MUST表达全部time、value、tangent、weight、WeightedMode与wrap值
- **AND** 重建结果 MUST不把曲线简化为只保留关键点

#### Scenario: 遇到未支持的正式字段

- **WHEN** 输出适配不能表达当前对象某个正式配置字段
- **THEN** 导出 MUST明确报告该对象、字段和原因
- **AND** MUST不以缺字段代码替换目标源码并声称成功

### Requirement: 导出必须处理环与共享引用

导出 MUST按正式owner和对象身份收集生成闭包，区分对象创建、配置、引用绑定和连线阶段。图的执行环 MUST不造成导出递归不终止，共享对象 MUST不被每个引用者分别复制；系统入口 MUST遵守现有工厂创建规则，动态端口必须在连线前形成合法形状。

#### Scenario: 图包含循环与两处共享子图调用

- **WHEN** 合法图的执行连接形成环，且两个节点引用同一内部子图
- **THEN** 代码 MUST只创建一次共享子图并在所需对象存在后建立连接
- **AND** 执行代码 MUST恢复原循环和共享关系

### Requirement: 公共输出必须通过领域薄扩展消费正式内容

公共导出/生成机制 MUST只拥有输出上下文、依赖排序、对象变量映射、通用C#表达式和最小生成入口。各领域 MUST从正式对象读取身份、内部owner、外部依赖及配置，并输出其正式创建/配置/连接/根绑定API调用；不得建立第二领域模型、字段表或中央Validator。某个正式领域或内容尚未支持时 MUST拒绝该根的完整导出，不能省略后报告成功。

#### Scenario: 正式根包含尚未支持的事件图内容

- **WHEN** 根资产包含EventGraph或变量内容而相应领域输出适配尚未完整提供
- **THEN** export_code MUST返回该对象或字段的未支持诊断
- **AND** MUST保持已有目标源码，不生成省略该内容的替代结果

#### Scenario: 领域扩展输出共享对象

- **WHEN** 领域扩展通过正式对象报告相同内部对象被多处引用
- **THEN** 公共输出 MUST复用同一对象变量及领域正式创建调用
- **AND** MUST不复制一份公共领域DTO或新建该领域的运行实现

### Requirement: 生成必须执行明确代码入口并保存指定输出

generate_assets MUST接受精确源码路径、对应已编译的正式创建入口类型、Definition上下文与输出资产路径。入口 MUST使用正式创建合同与业务API，返回明确根输出，工具 MUST保存生成结果并返回实际资产路径和诊断。系统 MUST不接受任意C#正文、任意方法调用或反射字段修改参数。源码尚未成功编译或Unity处于编译、导入、Play或切换Play时 MUST拒绝生成，不得执行不匹配的旧编译结果。

#### Scenario: AI代码已经编译

- **WHEN** AI提供实现正式创建合同的代码并成功编译后明确调用generate_assets
- **THEN** 工具 MUST创建并保存指定范围资产并返回根输出
- **AND** MUST不要求先有旧资产或Agent工作包

#### Scenario: 源码编译失败

- **WHEN** 请求的源码修改后尚未成功编译
- **THEN** 工具 MUST报告未就绪或编译错误
- **AND** MUST不使用上次同名类型的旧结果生成资产

### Requirement: 生成范围必须可删除重建且保持逻辑身份

已导出的C# MUST足以在相同外部资源和正式API版本下重建其明确生成范围，不依赖旧生成资产正文或旧生成子资产GUID。代码 MUST表达并保持业务图、节点、变量及其他正式元素的稳定identity，内部引用使用本次创建的对象；物理Unity实例或文件身份不作为内容永久保留要求。再次完整生成 MUST替换其拥有的输出范围，不不断追加重复对象，并通过领域正式API明确恢复Profile/Definition等根owner/消费者绑定。

#### Scenario: 删除生成图及其私有Timeline后重建

- **WHEN** 生成范围内旧资产已不存在而源码及真实外部输入仍存在
- **THEN** generate_assets MUST恢复等价对象、逻辑身份、配置、owner和引用
- **AND** MUST恢复本次明确指定的根绑定，不只产生一张孤立图

#### Scenario: 从代码移除一个节点后重新生成

- **WHEN** 创建代码不再创建旧节点且调用方明确重新生成该输出范围
- **THEN** 结果 MUST不保留该节点或悬空边
- **AND** MUST不要求另行提交删除日志或Document差异

### Requirement: 外部输入与生成输出必须分开

导出 MUST区分范围内生成对象与外部资源；原始AnimationClip、Rig、素材、分析产物和范围外共享Graph MUST作为精确类型化引用或入口输入，不隐式复制或删除。Definition/Profile/Prefab只有明确属于本次生成/绑定范围时才允许修改；系统 MUST不扫描场景、同名资产或目录猜测缺失依赖。原Document既有Profile/Control/Action/Curve业务的直接API能力仍应保留，不要求导出一个图就生成整个角色和全部素材。

#### Scenario: 图引用范围外共享Macro与动画素材

- **WHEN** 导出图引用范围外Macro及已有AnimationClip
- **THEN** 代码 MUST表达明确外部引用
- **AND** 清理旧生成输出 MUST不删除这两个资源

### Requirement: 导出和生成必须具有明确的非同步语义

export_code MUST以当前资产为输入完整覆盖明确目标代码内容；generate_assets MUST以当前指定代码为输入完整生成其声明范围。未导出的人工编辑 MUST不被视为已进入源码，显式重新生成 MUST不自动合并这些改动。两个工具 MUST不实现rebase、双向冲突合并或整包hash协议，不能宣称代码与资产始终同步。

#### Scenario: 未导出人工调整后重新生成

- **WHEN** 作者调整资产但没有export_code，随后明确使用旧代码执行generate_assets
- **THEN** 输出 MUST按指定代码重新创建
- **AND** MUST不自动把人工调整反推到源码或建立另一条同步路径

### Requirement: 业务规则必须继续由正式模块承担

代码创建与人工编辑 MUST共用正式Graph、Timeline、Presentation及Curve规则，整角色诊断 MUST复用正式编译器。导出只增加输出完整性检查，不复制领域规则。Agent协议校验与重复业务校验 MUST删除；仅在正式模块真实缺少时补入所属模块，不得新建中央Agent替代Validator。

#### Scenario: 不合法的片段或连接

- **WHEN** 创建代码要求非法片段配置或不兼容端口连接
- **THEN** 正式业务入口 MUST按与人工编辑相同的规则拒绝
- **AND** MUST不依赖Agent preflight或Document Validator

### Requirement: 导出与生成结果必须如实报告且不触发运行产物Build

export_code MUST在完整输出检查成功后写目标源码，失败不得以半份输出替换已有文件；其响应 MUST返回代码文件、正式入口和依赖诊断。generate_assets MUST使用实际生成范围的正式编辑/保存能力，响应 MUST区分创建和保存结果；失败或恢复未完成 MUST指出影响对象，不报告完整成功。两者 MUST不自动发布Program/Projection、Play或修改源码之外的无关文件，原人工Undo与保存继续使用。

#### Scenario: 导出中途发现未知内容

- **WHEN** 某字段无法输出导致导出失败
- **THEN** 工具 MUST返回明确诊断且保留原目标文件
- **AND** MUST不修改输入图或触发Build

#### Scenario: 资产生成后保存失败

- **WHEN** 根对象创建完成但正式保存失败
- **THEN** 工具 MUST报告保存未完成及受影响路径
- **AND** MUST不把对象创建成功当作完整生成成功

### Requirement: Agent authoring旧协议与工具必须激进删除

系统 MUST删除旧五个BTSMTL authoring工具、Agent Window、Document/Snapshot/Codec/Store/Exporter/Reconciler、专属Mutation/Session/Validator/Report和无消费者的协议DTO、测试及依赖。新导出/生成入口 MUST不转发旧工具、保留兼容包或把Agent框架换名搬迁；正式领域代码和新代码输出器只保留其实际职责。

#### Scenario: 完成切换

- **WHEN** 两个显式作者工具及正式业务API已经接通
- **THEN** 当前源码与注册项 MUST不保留旧Agent生命周期和兼容入口
- **AND** 正式Graph/Timeline人工编辑 MUST可以独立使用

### Requirement: 旧协议删除必须以作者调用链迁移完成为条件

旧Agent公共协议的删除 MUST以受影响作者调用者已脱离Agent、正式编辑/生成/保存可用、混合文件内有效领域操作已承接为门槛。有效领域API或作者对象不得因目录/类型含Agent或Document名称而随协议删除。作者退役与独立运行参数桥或动画变量运行闭环 MUST分别记录，不得把运行闭环尚未完成当作恢复旧作者协议的理由，也不得以作者工具完成宣称运行闭环完成。

#### Scenario: 混合文件仍含正式FSM操作

- **WHEN** 待删除协议文件仍包含被正式编辑调用的有效FSM操作
- **THEN** 删除 MUST等待该领域正式承接并使作者调用链脱离Agent
- **AND** MUST不整目录删除、复制旁路或保留兼容转发完成切换

#### Scenario: 作者退役完成但运行参数桥仍未替换

- **WHEN** 作者调用者已脱离Agent且正式编辑/生成/保存可用，独立运行参数桥仍在其负责领域待处理
- **THEN** 无消费者旧作者协议 MUST按本次删除范围退役
- **AND** 运行参数桥状态 MUST单独报告，不纳入作者工具完成声明

### Requirement: 协议退役与导出不得改变既有领域业务

Foot Motion完整曲线组、Clip与Timeline各自owner和时间域、MotionWarp源引用、Action target、Animation channel、FSM Edge参数与顺序 MUST保持正式领域规则；代码导出和执行不得新增同义数据源。生成的Foot Analysis/Program/Projection不得因导出而变成可写素材，也不得扩大到未授权的Rig/Body Motion算法或角色内容。

#### Scenario: 不完整Foot Motion数据组

- **WHEN** 创建代码只提交要求整组修改的曲线中的一条
- **THEN** 正式曲线API MUST拒绝不完整数据组
- **AND** MUST不通过省略校验或JSON中转完成写入
