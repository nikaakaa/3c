## Why

FlowCanvas接入后，原业务节点与新画布节点重复定义同一种业务。例如原移动节点与FlowCanvas移动节点各自保存速度、曲线和时长，各自维护字段与校验，修改一条规则需要改两处。本change只解决这些重复定义：原业务规则抽出一份，FlowCanvas使用它，Agent、Inspector和编译随之使用同一来源。

## What Changes

- 逐对核对原业务节点与FlowCanvas节点的参数、默认值、校验、逻辑端口、引用和编译行为。先辨认同义内容与实际差异，保留已正确的原业务规则。
- 从原业务模块抽出共同参数类型和节点定义。同义节点必须使用同一定义；FlowCanvas只负责画布适配，不再声明第二套业务参数和规则。
- 仍有正式消费者的原节点改用共同定义，每个节点实例保留自己的参数值；原节点没有消费者时删除。迁移后删除重复字段、校验、目录声明和同义编译分支。
- 保留FlowCanvas正式图、Node/Edge identity、连接、布局、原生Macro接口和保存方式，不建立自有图拓扑或第二份可写图。
- 原生编辑、Document读写、复制引用与编译改为消费共同定义。这里只调整受参数抽取影响的消费代码，不重做Agent工具、事务服务、运行执行器或完整引用架构。
- 本次不自行改变Document公开kind、字段、端点、owner或版本，当前基线为v7。状态转移、原生FSM、最终Document v8与清理由integrate-native-fsm-skill-authoring负责，既有Step/Edge成果由转移专项提供；若该计划先正式落地，本项按其已发布的唯一合同接入，不恢复旧版或双读。布局hash改造也不在本change中实施。

## Capabilities

### New Capabilities

- btsmtl-skill-authoring-model：原业务节点与FlowCanvas节点共用参数和规则，两侧作者行为与编译解释一致，各节点实例独立保存值。

### Modified Capabilities

- graph-authoring-domain-framework：共同业务定义独立于画布宿主，旧作者目录与FlowCanvas目录只能投影同义定义，不能分别维护业务规则。
- btsmtl-agent-authoring-document-sync：Document作为共同定义的消费者，保持实施基线的唯一公开形状及Mutation/事务，不自行决定版本迁移，不以统一导出掩盖两套业务定义。

## Impact

- 原业务定义：Runtime/Character/Pipeline/Graph、Input、Motion及对应BTSMTL节点模块；共同参数和规则继续归所属业务。
- 画布接入：Runtime/Character/Control/Authoring/FlowGraphs及相关旧作者适配。保留框架基类与明确端口映射，删除重复业务声明。
- 消费代码：既有Capability、Inspector、SkillDocument和Compilation/Skills；复用现有业务lowering与Program Builder，保留运行语义。
- 资产：仅迁移受参数存储位置变化影响的精确节点实例，保留参数值、UID、端点和引用；不借此迁移状态机拓扑。
- 文档：原节点定义、FlowCanvas与metadata工作的交接见design；原生FSM和最终协议迁移归integrate-native-fsm-skill-authoring，转移专项保留其阶段成果及对应接收记录。只更新规划，不修改代码、资产或现行spec正文，不归档未完成change。
