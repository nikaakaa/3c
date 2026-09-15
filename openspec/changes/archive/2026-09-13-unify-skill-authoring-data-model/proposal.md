## Why

原业务节点与FlowCanvas节点仍可能分别维护同义参数、默认值、端口和引用规则；仅把这些内容转成Agent包没有消除重复定义。按2026-09-13 r2公共基线，本change只完成共同业务定义及共享metadata，让原节点、FlowCanvas、人工编辑、C#代码创建和编译使用同一规则，旧Agent协议由专门任务退役。

## What Changes

- 从原业务节点的有效参数与规则抽出共同定义，FlowCanvas按正式定义适配字段、端口和引用。每个实例各自保存值；不把原节点整体嵌入新节点，不创建另一份图或领域DTO。
- 本任务唯一维护GraphAuthoringCapabilityCatalog.cs及共享字段/端口定义，集中固定、条件和动态端口的正式描述；各领域继续拥有自己的节点能力、业务规则和正式创建/配置方法。
- 补足共享metadata或所属业务API中缺失的正式字段读取、参数类型、默认值、引用读取及创建/配置入口描述，供人工UI、编译器与C#输出器只读消费。不得为代码输出另建节点、字段、owner模型或中央Validator。
- 采用remove-agent-authoring-use-native-csharp/design.md r2：export_code显式从当前资产完整输出C#；generate_assets显式执行已编译正式入口，重建并保存明确范围。人工编辑不自动导出，重新生成不自动合并未导出的调整。
- 两个MCP、代码输出/生成机制、Agent Store/Reconciler/五工具/重复协议校验删除，由C# authoring任务唯一负责。本任务不另发Document版本，不维持退役协议兼容，也不重复修改其两份JSON binding文件。
- 保留已完成的TransferPayload、真实端口和合法组合步骤。FSM资产、转移、order、生命周期及迁移归FSM任务；事件执行和变量生产归事件图任务；真实字段冲突只在本设计记录，不自动覆盖旧Step/Edge业务值。
- 当前目录曾记录的v8发布、包重建及等待checkout验证属于已被r2替代的阶段工作，历史证据保留在implementation.md，不再进入本轮执行清单。

## Capabilities

### New Capabilities

- btsmtl-skill-authoring-model：原业务节点与FlowCanvas共用参数、字段、逻辑端口、引用和业务规则，人工编辑与正式代码创建得到一致业务结果。

### Modified Capabilities

- graph-authoring-domain-framework：新增共享metadata完整提供正式读取/配置入口、唯一端口结果和领域隔离的消费要求；不重复C# authoring任务负责的旧Agent规范删除与通用代码导出合同。

## Impact

- 本任务的实现边界：共享GraphAuthoringCapabilityCatalog.cs及字段/端口定义；共同Skill业务参数与能力投影；这些定义对原节点、FlowCanvas、人工UI和编译的接入。
- 各领域保留自己的正式能力模块与写入/保存责任。共享层只提供统一描述，不承接领域运行、全图保存事务或源码同步。
- BtsmtlSkillNodeAuthoringBinding.cs、TimelineAuthoringClipBinding.cs的JSON退役及公共代码输出器属于C# authoring任务；本任务仅提供其消费的正式业务定义与必要API，不同时修改这两份文件。
- 现行Agent专属规范与本目录旧协议delta冲突，删除本目录旧btsmtl-agent-authoring-document-sync delta，协议整体退役由remove-agent-authoring-use-native-csharp负责；本轮不修改current或其它任务文档。
- 规划阶段只更新本目录规划和历史入口；执行阶段按tasks修改共享业务定义与metadata，并在4.3对匹配的现行共享spec做增量对账；不修改资产、不新增测试或验证任务，不把文档对齐当成实现完成。
