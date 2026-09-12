## Why

Skill作者基础已经落地，但状态机仍由自定义FlowCanvas节点承载；原生FSM、Agent适配和Corin清理需要一个独立、可结束的实施范围。本change从旧refactor-btsmtl-flowcanvas-authoring拆出，不再携带通用运行观察、网络Adapter和整角色Replay的长期任务。

## What Changes

- **BREAKING**：状态机子图唯一使用NodeCanvas原生FSM/FSMState/FSMConnection作者适配；Skill根、StateBody、ConditionRule、Macro及Timeline执行体保留FlowCanvas。原生GraphEditor仍是唯一编辑入口，不保存旧状态图镜像。
- 接入状态、转移、入口/出口和生命周期的业务映射，保留priority、abortPolicy、order、条件、identity及停止传播；仅开放已有正式定义和编译映射的插件能力。
- **BREAKING**：扩展现有五个Agent生命周期工具到Document v8，使FSM、条件owner、StateBody和钩子经过唯一Exporter/Reconciler/Mutation/Validator与完整资产事务；拒绝旧包兼容读取，不增加FSM工具链或JSON直编。
- 编译直接消费正式作者资产进入既有IR/Program；原生FSM页提供来源映射，运行只消费Program，不启动插件图或任务。
- 经正式事务迁移精确Corin Skill闭包，清理Attack无消费Setup、重复意图声明和私有空图；保留连段、取消条件、Dodge并行监控、窗口与目标数据。未裁决的阈值归属和正式跑步意图行为不擅自改变。
- 完成根/私有/实际共享owner的Undo、保存重载和失败恢复，定位相关m_Name序列化问题；删除无消费者旧Skill状态机存储、补读与迁移入口。

## Capabilities

### New Capabilities

- `btsmtl-flowcanvas-authoring`：接收旧change尚未安装的作者基线与原生FSM增量，保持原稳定能力路径；基线已完成工作不重新实施。

### Modified Capabilities

- `btsmtl-agent-authoring-document-sync`：FSM、Document v8和完整资产事务。
- `btsmtl-graph-core`：接收旧change中Skill退出BaseGraph作者链的规范增量，保留非Skill领域。
- `graph-authoring-domain-framework`：接收统一Capability/Mutation及领域隔离基线，适配原生FSM。
- `graph-authoring-editor-shell`：接收原生GraphEditor与provider作者入口基线，删除被替代Skill窗口路径。

## Impact

实现范围为Skill作者适配、SkillDocument、Compilation/Skills和精确Corin Skill资产。共同业务参数/规则依赖unify-skill-authoring-data-model；既有转移成果依赖add-skill-transfer-connections，不覆盖两者正确改动。

通用观察完成度由finish-skill-runtime-observation负责；网络Pass/Adapter和正式Skill运行证据由integrate-corin-dump-authoring-replay负责。上述合同是依赖边界，不要求本change顺手重做它们。新任务只处理剩余缺口，旧59项完成记录与26项剩余任务的映射保存在归档交接记录。本次仅规划，不实现、不Build、不运行Unity。
