## Why

原生FSM、Program编译和C#图输出已在任务表记录完成，但完整技能外壳仍与图分散保存。本change继续采用[原生C#作者基线r2](../remove-agent-authoring-use-native-csharp/design.md)的两个显式工具，在已有成果上补齐完整Ability作者入口。

r3补充用户已确认的作者目标：`GameplayAbilityDefinition`是一个完整技能的入口，集中拥有技能规则和执行图。现有SkillDefinition、ActionProfile、ActionContext与角色图列表分散表达同一技能，新C#生成也只登记图；本阶段收敛这些所有权和命名，不把只重建Graph称为完整Ability。

## What Changes

- **BREAKING**：将完整技能作者入口收敛为独立`GameplayAbilityDefinition`资产；其唯一`AbilityGraph`和私有FSM、条件、Macro、Timeline归该Ability拥有。角色通过`AbilityGrant`引用、授予并绑定输入，不再分别维护SkillDefinitions与SkillGraphs两套登记。
- 技能专用的激活、阻断、取消、目标要求及效果引用收进Ability配置；真正需要共同维护的激活规则才显式共享，专用与共享只允许一个有效来源。输入映射与已有授予参数归角色/装备，当前目标、释放代次和执行状态归运行上下文。
- Skill专属命名统一为Ability：作者使用GameplayAbilityDefinition/AbilityGraph，授予使用AbilityGrant，一次释放使用AbilityExecution/AbilityExecutionContext。它们分别迁移现有职责，不额外复制一套运行状态；AbilityGrant不是执行实例，运行也不照搬UE UObject。
- 普通Ability不要求作者创建空ActionContextSlot；节点和Timeline绑定当前AbilityExecutionContext。保留已确认的非Skill调用上下文，只有实际无消费者的旧Action/Skill专属配置、类型、入口和名字才删除，不保留兼容别名。
- 完整Ability的C#输出必须包含外壳、规则、私有执行内容和明确授予/根挂接；子图输出只恢复该子图及owner，不创建另一个Ability，也不能依赖角色中残留的旧外壳。

- **BREAKING**：状态机子图唯一使用NodeCanvas原生FSM/FSMState/FSMConnection作者适配；Skill根、StateBody、ConditionRule、Macro及Timeline执行体保留FlowCanvas。原生GraphEditor仍是唯一编辑入口，不保存旧状态图镜像。
- 接入状态、转移、入口/出口和生命周期的业务映射，保留priority、abortPolicy、order、条件、identity及停止传播；仅开放已有正式定义和编译映射的插件能力。
- **BREAKING**：撤销未交付的Document v8发布、五工具和整包同步依赖，消费公共`btsmtl.export_code`与`btsmtl.generate_assets`两个显式入口。本change只提供FSM正式读取/配置/校验API及领域输出接入；公共工具、通用C#输出器和旧协议删除由C# authoring change负责。
- 从`BtsmtlSkillGraphAuthoringApplier.cs`逐项迁出仍有效的FSM创建、配置、引用、转移顺序、owner、系统入口及业务身份恢复能力，落到已有Skill/FSM模块；不把整个文件改名搬迁，不新建中央Validator或FSM Document。
- 完整输出指定FSM及StateBody/Condition/Macro等拥有闭包，按创建、配置、引用、连接和根挂接产生正式API调用；内部共享对象只创建一次，范围外资产保持精确外部引用，缺少字段/生命周期输出明确拒绝。
- 生成代码可替换物理资产并在删除旧输出后重建，保留业务identity、order、配置、布局和明确Definition/根引用，不依赖旧生成子资产GUID。人工修改不自动导出，重新生成不合并未导出的修改，不新增源码同步。
- 编译直接消费正式作者资产进入既有IR/Program；原生FSM页提供来源映射，运行只消费Program，不启动插件图或任务。
- 经已有领域资产API迁移/重建明确Corin Skill范围，保留已正确清理、连段、取消条件、Dodge并行监控、窗口和目标数据；未裁决阈值及正式跑步意图规则不改。
- 人工编辑继续使用原领域Undo/保存，生成与失败报告复用原资产模块，Build独立显式调用。先迁出真实能力、迁离调用者，再删除无消费者协议依赖；本次文档调整不代表代码已删除。

## Capabilities

### New Capabilities

- `btsmtl-flowcanvas-authoring`：保留原稳定能力路径，补充GameplayAbilityDefinition完整入口、AbilityGrant、执行上下文和完整Ability生成范围；既有原生FSM不重新实施。

### Modified Capabilities

- `btsmtl-graph-core`：接收旧change中Skill退出BaseGraph作者链的规范增量，保留非Skill领域。
- `graph-authoring-domain-framework`：复用正式节点定义、领域配置/校验和原生编辑规则，不恢复Agent Reconciler或重复校验。
- `graph-authoring-editor-shell`：接收原生GraphEditor与provider作者入口基线，删除被替代Skill窗口路径。

## Impact

实现范围为已有Skill/FSM作者模块、`BtsmtlSkillGraphAuthoringApplier.cs`中有效FSM操作的迁出、领域C#读取/输出适配与精确Corin生成范围。该Applier混有协议和资产能力，只有本FSM任务负责其中有效FSM操作的迁出；C# authoring任务不能在接收迁出结果前整目录删除。共同节点定义与共享端口规则仍由unify-skill-authoring-data-model负责。

公共export/generate工具、输出器及协议退役由remove-agent-authoring-use-native-csharp负责；通用观察、网络、事件图运行和Pose变量闭环不转入本任务。已勾选Document工作保留历史事实，不能据此声明r2完成，也不撤销其已经正确的原生FSM能力。本次只更新本目录规划，不修改代码/资产，不运行Unity/Build，不发送其它窗口执行消息或回执。

r3进一步涉及原CharacterSkillAuthoringDefinition、ActionProfile的Skill消费、角色能力登记、原生作者入口及完整Ability的领域C#输出。复用现有Program激活/取消/效果/状态机制；不借命名迁移重做成本、冷却、等级、网络或效果系统。任务只列实际实现和清理，不列验证、证据收集或手动验收任务；已有完成记录保留为对应阶段事实。
