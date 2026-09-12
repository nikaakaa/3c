## Context

见[proposal](proposal.md)。本change接收旧变更的FSM新增工作及作者事务缺口；旧实现、提交和历史诊断见[归档交接](../archive/2026-09-12-refactor-btsmtl-flowcanvas-authoring/split-handoff.md)，不再复制59项已完成任务。

当前Skill使用FlowCanvas自定义StateMachine role，未接入NodeCanvas FSM。盘点时Attack有5个状态、20条转移，15条指向同一Exit；连接已保存condition、priority、abortPolicy、order，manifest仍标v7。实施前重读实际Unity/Document/源码状态，不从截图恢复旧steps，不覆盖其它窗口未提交改动。

## Goals / Non-Goals

**Goals:** 让作者与Agent完整编辑同一原生FSM资产；直接编译进现有Program；迁移Corin并删除无消费旧存储；完成相关资产事务与来源映射。

**Non-Goals:** 不重做共同业务定义；不接管通用观察实例/采样系统、网络Adapter、场景预览或整角色Replay；不恢复Character RootTree；不新增测试。本次仅写规划，不运行Unity、Build或Play。

## Decisions

### D1 状态机换正式资产，执行体保留原业务

| 作者内容 | 唯一存储与owner |
|---|---|
| Skill根 | 独立FlowGraph主资产，Definition只引用 |
| StateMachine | 原生FSM，唯一属于调用它的节点，私有内容仍保存于Skill或共享Macro实际文件 |
| State | FSMState业务适配，唯一引用StateBody |
| Transition | FSMConnection业务适配，唯一保存condition、priority、abortPolicy、order；私有条件由Edge拥有 |
| StateBody、ConditionRule、Macro、TimelineBody | FlowCanvas正式图及原接口，不再镜像状态机拓扑 |
| Sequence/Selector/Parallel/Timeline | 保留现有组合、等待、并行和停止合同，不硬改为FSM状态 |

同一GraphEditor负责创建、导航、原生Inspector、Blackboard provider、复制和Undo。共同业务定义来自数据层change；FSM只增加宿主适配，不维护第二份规则。业务取舍：继续自定义状态图迁移成本较低；本次选择原生FSM以统一状态机作者体验，承担插件扩展与资产类型适配成本，但不接管插件运行。

### D2 原生名字不代表相同执行语义

Entry/Prime只在单一无条件入口时直接表达默认目标；条件/多分支入口保留正式路由，不复制第二份Prime配置。Any保持既有条件准入，Exit表示退出当前FSM调用。OnFSMEnter/Exit是整个FSM的钩子，不是转移端点，也不等于每个State的OnEnter/Root/OnExit；无业务时不创建空包装。

原生FSM无ConditionTask的边表示OnFinish，不能替代BTSMTL无条件边；主体完成仍显式用state-root-completed。原生Stacked/Clean不等于abortPolicy，未登记语义拒绝。ActionList/ConditionTask只允许正式typed定义与编译binding；引用执行体/条件图时只保存该引用，不同时保存另一份同义任务列表。停止源主体、执行OnExit/StateExitContext、处理Action/Timeline取消、进入目标的顺序保持既有Program合同。

插件FSMNode固定outConnectionType，FSM限制变量拖拽。连接参数、能力过滤和provider入口通过正式插件扩展点接入并登记补丁，不用图外可写转移表或旁路Inspector绕过。Inspector禁止做迁移、闭包重建、Build等重操作。

### D3 Agent只扩展原来的五个工具

`Unity正式资产 ↔ checkout/apply Document工作副本`；显式Build仍直接读取资产，生成IR/Program，再进入现有Simulation Pipeline/WorldSolver。Agent不直接编译JSON，不创建FSM局部MCP工具或第二Mutation服务。

Document v8表达稳定业务kind、逻辑端点、FSM/StateBody/钩子引用；条件owner为`kind=edge, graphId, edgeId, referenceKey=condition`，order在同一来源下唯一，非转移边拒绝Transfer字段。catalog、Exporter、Mapper、Reconciler、Validator、OwnerCollector、Applier共用正式业务定义与引用合同。新local实体由同一apply反向导出为stable identity，禁止插件类型名、委托和运行状态进入editable。

根、私有页和实际修改的共享owner进入唯一Undo/保存/反向导出事务；失败恢复原资产及package，仅回收本次创建对象，不发布半迁移结果。五个入口一次切换v8，旧v7及更早reader不保留。先保留DocumentDirty/Conflict差异并裁决，再重新checkout。内部改基类本可不升版本，但这次同时改变owner/order和钩子外部合同，因此用明确版本拒绝旧目标，代价是重新生成工作包。

### D4 编译只汇入现有Program

去除Occurrence对全部节点为FlowNode、全部边为BinderConnection的假设。不同图适配各自真实资产，向同一只读编译输入提供业务参数、引用与来源；不先重建旧FlowGraph。保留状态/边identity、逻辑端点、调用路径、条件、priority、abortPolicy和有效order；不按位置、随机UID或插件数组顺序重排。必要资产对象重建在同一事务完整重映射并报告。

本change只提供原生State/Connection到Program SourceMap及只读绘制接口。实例选择、导航会话、订阅生命周期和采样性能归观察change；两者共用正式诊断，不为显示高亮启动FSM。网络与Motion/Body/WorldSolver执行链不变。

### D5 Corin按消费者清理，不借迁移改规则

| 对象 | 处理 |
|---|---|
| Attack Startup Branches、OnEnter Action Setup、Clear意图写入 | 删除实例、边、无人读取的图内声明及失去引用的私有空条件/layout，Root直连状态机 |
| Attack连段与15条Exit边 | 保留所有条件/排序/中止/identity；摘要显示主体完成、可闪避且收到请求、移动取消、连段目标 |
| Dodge Setup/Enter/Set | 确认全部typed消费者后删除无消费写入副本，保留Body/Exit Monitor并行 |
| Hit/Recovery/IFrame和ActionTarget | 按Timeline、Frame Fact、targetSnapshot、输入绑定和编译引用保留，不以缺少Get判定无消费 |
| StopThreshold | 保留现有有效值和引用；Attack的声明仍被两个Dodge使用，未决定新归属前不删除或临时替换 |
| 正式跑步意图 | 保留ControlModule在Idle清理、DodgeForward完成后置位的时机，不新增攻击开始清理行为 |

StopThreshold若统一ControlModule阈值可保持移动/取消一致；Skill私有取消阈值可独立调手感。二者是未来业务变更，不是本次迁移的隐藏选项，本change不以选择未定永久阻塞FSM交付；清理验收明确排除仍有消费者的正式声明。Corin控制文件中的MovingTurn来源和60Hz时长改动不覆盖。

m_Name问题单独取得完整错误、实际类型和继承链后修复；目前命中的普通SkillStepPort字段不足以确认MonoBehaviour错误来源，不全局改名或清缓存。若证实属于其它领域，记录精确归属，不扩大FSM修复范围。

### D6 规范继承与归并

旧change的六份delta经逐文件对账：观察delta归观察change，其余五份完整继承本change，以免归档丢失尚未安装的Requirement/Scenario；这不表示全部旧代码需要再写一遍。新增能力btsmtl-flowcanvas-authoring尚无current文件，保持原稳定路径，不再另造同义能力名。

| 现行差异 | 处理 |
|---|---|
| current Document为v7，旧在途提案也含版本增量 | 本change唯一负责最终FSM/v8合同；合并时逐Requirement保留有效非Skill场景，不能用旧delta覆盖后续规范 |
| graph-core/domain/editor-shell已有current但与旧delta不全相同 | 只归并Skill适用范围和已交付原生入口，保留后来加入的其它领域要求；原Scene/Pose表面不借本次重做 |
| 外层None/Attack/Dodge状态机、禁止全部Sequence、BTSMTL/Pose共享旧StateMachine View | 与当前独立Skill/C#控制、有效组合流程和原生FSM目标冲突，交付时明确修订范围，不恢复旧RootTree |
| Skill Program的Pipeline要求 | 保留为下游合同；网络Adapter实现与载荷证据由Corin闭环change负责，不进入本tasks |
| 旧delta仍写旧Baseline外壳、UI/版本号 | 归并前对current重新对账；归档本身不安装这些目标或证明实现完成 |

## Risks / Trade-offs

- [已有Edge与旧Step不一致] → 输出逐字段与并列顺序冲突，禁止自动选一侧。
- [资产类型迁移丢身份或共享owner] → 先生成完整typed迁移计划，再同一事务创建/重映射/删除；失败恢复全部owner。
- [数据层窗口继续修改] → 消费其正式提交与定义，不覆盖其规划或代码；实际同段冲突明确交由用户裁决。
- [观察或网络收尾拖长FSM任务] → 本change只交付原生作者、Agent、编译、来源映射与迁移；下游各自按任务收口。

## Migration Plan

先冻结真实来源并接收共同定义/转移成果，再完成FSM作者、Document与编译全链支持，然后通过唯一事务迁移资产及清理，最后执行既有Validator/精确Build、来源与Console核对，删除已替代存储和一次性转换。正式支持完整前不开放可编辑FSM；迁移失败恢复，不提供兼容执行。实际运行轨迹证据由Corin闭环接收，不能用旧job或Clean替代。
