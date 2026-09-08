# BTSMTL FlowCanvas实施记录

## 实施基线与归属

- 目录：`D:/Unity_Project_1/3C`；开始本轮apply时HEAD：`9c447aa4b5f2ddbc7d114476bccb59910d785e6c`。
- 工作区已有未提交资产、Pose、相机和插件变更，不把整个工作区视为干净基线，不覆盖这些改动。
- Center：`BTSMTL技能FlowCanvas作者与编译接入`，`change_id=2d129d09136d4ac3b77312b9e43d42f8`。
- 精确根：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`。
- 原`refactor-btsmtl-authoring-architecture`继续负责角色C#控制、技能业务、状态及生命周期；本文负责技能FlowCanvas作者入口、直接编译和观察。
- `rebuild-btsmtl-preview-with-scene-play`继续负责正式场景和运行控制；本文仅接观察接口。
- PoseGraph及原生runtime方案独立管理，不迁其资产、执行器或算法。

## 已核对的代码依赖

| 业务 | 现有代码 | 迁移处理 |
|---|---|---|
| 技能定义及入口 | CharacterSkillAuthoringDefinition.EntryGraphAuthoringId | 需接正式FlowCanvas技能闭包，不增加第二入口fallback |
| 技能发现 | CharacterSkillCompilationDiscovery | 当前从CharacterAuthoringGraphOccurrence中找入口及调用；必须解耦旧图遍历 |
| 图与调用发现数据 | CharacterAuthoringSourceCompilationModel | Graph、Node、GraphReference和TreeClip记录直接持有旧类型，不能简单包FlowNode后继续用旧对象树 |
| 业务语义发射 | CharacterSimulationCore/Action/Input等NodeEmitterRegistration | 保留操作含义和参数，迁移读取业务payload的部分 |
| Program写入 | CharacterSimulationProgramBuilder | 保留唯一常量、operation及端口binding写入机制 |
| 技能入口发射 | CharacterSemanticSkillProgramEmitter | 保留技能目录、follow-up、输入和动作关系 |
| 状态与运行 | ActionSkillExecutionRuntime | 保留ActionInstance、predictionKey、generation和调用状态合同 |

这不是全能力清单完成声明；任务1.2仍须结合正式资产闭包完成节点与端口逐项盘点。

## 第一小步：隔离操作写入与旧节点读取

`CharacterSimulationNodeEmitterContext`继续从当前合法旧作者节点读取已校验的未连接输入，随后将明确的来源、值、类型、端口和操作描述交给`CharacterSimulationOperationEmitter`。后者只调用唯一Program Builder，不接收BaseGraph、BaseNode或PropertyPort，也不创建作者图。

输入为来源位置、操作描述及有序常量输入；输出为OperationHandle及同一Builder中的常量／端口binding。常量声明顺序仍为输入默认值在前、节点附加常量在后，operation及binding发布顺序保持原实现。

这是后续FlowCanvas直接编译入口的正式内部边界，已由现有调用者使用，不是旧图转换器。但FlowCanvas图闭包、领域节点、Macro、Document v6和原生观察尚未接入，不能关闭4.1或其他整项任务。

当前改动不改变Document字段、identity、ownership、Reconciler或Validator；Agent合同仍为实际v5，不能只因提案目标为v6就修改运行schema。

## 验证事实

- 正式RunHost基线编译返回`WorkspaceEditorInUse`，没有RunId；保留主验收Editor，没有另建临时运行器。
- 同实例`e852139597e42532`完成脚本refresh／重载后，Console返回0条错误。重载期间CLI断开后原实例已恢复。
- 尚未生成同输入前后回放比较，不能用脚本编译证明语义和性能完全一致。
- 未新增测试代码，未修改Unity YAML，未调用局部资产修复工具。
