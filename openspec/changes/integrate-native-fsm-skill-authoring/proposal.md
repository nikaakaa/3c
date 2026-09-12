## Why

原生FSM作者与Program编译接入已有实现，剩余工作需要跟随[原生C#作者基线r2](../remove-agent-authoring-use-native-csharp/design.md)脱离旧Agent协议。当前旧Applier仍混有真实FSM资产操作，必须先落实到正式领域API，才能完整导出、删除后重建并清理协议依赖。

## What Changes

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

- `btsmtl-flowcanvas-authoring`：接收旧change尚未安装的作者基线与原生FSM增量，保持原稳定能力路径；基线已完成工作不重新实施。

### Modified Capabilities

- `btsmtl-graph-core`：接收旧change中Skill退出BaseGraph作者链的规范增量，保留非Skill领域。
- `graph-authoring-domain-framework`：复用正式节点定义、领域配置/校验和原生编辑规则，不恢复Agent Reconciler或重复校验。
- `graph-authoring-editor-shell`：接收原生GraphEditor与provider作者入口基线，删除被替代Skill窗口路径。

## Impact

实现范围为已有Skill/FSM作者模块、`BtsmtlSkillGraphAuthoringApplier.cs`中有效FSM操作的迁出、领域C#读取/输出适配与精确Corin生成范围。该Applier混有协议和资产能力，只有本FSM任务负责其中有效FSM操作的迁出；C# authoring任务不能在接收迁出结果前整目录删除。共同节点定义与共享端口规则仍由unify-skill-authoring-data-model负责。

公共export/generate工具、输出器及协议退役由remove-agent-authoring-use-native-csharp负责；通用观察、网络、事件图运行和Pose变量闭环不转入本任务。已勾选Document工作保留历史事实，不能据此声明r2完成，也不撤销其已经正确的原生FSM能力。本次只更新本目录规划，不修改代码/资产，不运行Unity/Build，不发送其它窗口执行消息或回执。
