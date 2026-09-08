# BTSMTL FlowCanvas实施记录

## 实施基线与归属

最新执行约束：用户要求停止当前大改过程中的主动编译、Build、整根校验及回放。下方已发生的检查仅记录历史事实，不再重复。接入入口必须从实例工作区定位，不恢复`Character Definition -> Open Root Tree`；精确Definition路径只保留为已有资源关联及历史检查定位。

- 目录：`D:/Unity_Project_1/3C`；开始本轮apply时HEAD：`9c447aa4b5f2ddbc7d114476bccb59910d785e6c`。
- 工作区已有未提交资产、Pose、相机和插件变更，不把整个工作区视为干净基线，不覆盖这些改动。
- Center：`BTSMTL技能FlowCanvas作者与编译接入`，`change_id=2d129d09136d4ac3b77312b9e43d42f8`。
- 精确根：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`。
- 原`refactor-btsmtl-authoring-architecture`继续负责角色C#控制、技能业务、状态及生命周期；本文负责技能FlowCanvas作者入口、直接编译和观察。
- 当前观察按最新设计消费普通Unity Play中的真实Actor／ActionInstance，不以前述独立Scene Play方案为前置条件，也不创建预览运行控制器。
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

## 实例观察入口接入中的第二小步

诊断事件打开来源现在传递完整RuntimeDebugEventView，依据事件中的CharacterRuntimeId查询既有Registry，核对Session、ProgramRevision及SourceMap handle与source一致，再定位该Host关联的作者资源。删除扫描全部Definition／Timeline的运行事件入口；静态Semantic IR检查器仍使用它显式拥有的Definition，不把它当成运行实例入口。

作者上下文通过通用IRuntimeDebugInstanceContext携带RuntimeInstanceKey，当前图观察面读取该身份并Pin。修正首次Pin后的刷新丢失绑定：Pinned目标不受场景选择自动替换，不匹配时显示PinnedTargetNotAttached并保留原实例。Follow遇到多个执行实例时要求明确选择，不取列表第一项。

目前底层来源资源解析仍读取现有作者存储，不能据此宣布FlowCanvas技能图已迁移；后续正式图和编译入口完成后替换该资源解析，不能再增加旧图镜像。Timeline独立页面、Macro调用导航及完整原生图观察仍待接入，7.2等整项不勾选。

本小步只做调用者搜索和diff静态检查；按用户要求未编译、未Build、未运行验证。运行状态及来源导航不改变Document可写字段或实际v5协议。

- 正式RunHost基线编译返回`WorkspaceEditorInUse`，没有RunId；保留主验收Editor，没有另建临时运行器。
- 同实例`e852139597e42532`完成脚本refresh／重载后，Console返回0条错误。重载期间CLI断开后原实例已恢复。
- 尚未生成同输入前后回放比较，不能用脚本编译证明语义和性能完全一致。
- 正式Validate job `4c215d1c6c344cf7a6b480324e81dcb9`已结束：success=true、compileSuccessCount=1、semanticValidCount=1，未apply或保存资产。sourceRevision=`5f692b49fd460f45732b1fb707a33afda75c4493c89bafeff7eeabe532577479`。用户要求停止编译后不再启动同类检查。
- 未新增测试代码，未修改Unity YAML，未调用局部资产修复工具。
