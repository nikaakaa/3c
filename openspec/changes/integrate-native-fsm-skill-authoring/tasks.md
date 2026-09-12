## r2执行边界

2026-09-13：公共基线改为remove-agent-authoring-use-native-csharp/design.md r2。下列已勾选项保留当时实现事实；尤其3.1/3.2是旧Document阶段记录，不再作为需要发布v8/五工具的目标，也不表示旧协议代码已删除。新的领域API迁出与C#完整输出/重建见第6节；不撤销已经正确的原生FSM、条件/顺序、生命周期与Program链。

本次只改规划。公共export_code/generate_assets、通用输出器和协议退役由C# authoring任务负责，共同节点定义/端口规则仍归数据统一任务；本change唯一负责旧BtsmtlSkillGraphAuthoringApplier内有效FSM操作迁出。依赖次序为6.1—6.5 → 公共入口接通并消费领域API（6.6）→ 4.3/4.4 → 6.7/5.x；4.5按实际作者错误独立处理。不新增测试代码或手动验证任务，不向其它窗口发送消息。

## 1. 来源与依赖

- [x] 1.1 将精确Corin Definition、源码、资产和Document登记为本change的唯一迁移输入；不覆盖其它窗口改动或从截图重建已迁移数据。
- [x] 1.2 接入共同业务定义和转移专项成果，统一FSM字段、逻辑端点、引用、编译binding及必要插件扩展；已正确规则不重写。

## 2. 原生FSM作者接入

- [x] 2.1 实现原生FSM/State/Connection领域适配和唯一owner；普通执行图保持FlowCanvas，不保存状态图镜像。
- [x] 2.2 接入原生GraphEditor创建、转移Inspector、StateBody/Condition导航、provider、复制与Undo，统一到同一Mutation调用链；Inspector不做迁移/Build。
- [x] 2.3 映射Entry/Prime、Any、Exit、FSM整体钩子与State生命周期，统一条件、顺序、停止、OnExit、Action、Timeline语义；拒绝未登记插件任务及栈调用。

## 3. 历史Document接入与既有编译

- [x] 3.1 扩展catalog与Document v8严格合同，支持FSM、edge owner、order、钩子和逻辑端点；不新增MCP工具。
- [x] 3.2 接通Exporter/Mapper/Reconciler/Validator/OwnerCollector/Applier及唯一Mutation，支持typed计划、身份、反向导出、整包Undo、owner保存重载和失败恢复；不新增事务服务或测试。
- [x] 3.3 去除编译读取全量FlowNode/BinderConnection假设，消费共同业务合同发射既有IR/Program；不重建旧状态图中转。
- [x] 3.4 提供原生State/Connection的Program SourceMap与只读绘制接入；无StartGraph、任务Execute或作者状态写入，不承担通用观察收尾。

## 4. Corin迁移与清理

- [x] 4.1 实现identity、逻辑端点、条件、priority、abortPolicy、order与资产引用的重映射；不重排已正确并列转移。
- [x] 4.2 移除Attack Setup、意图副本和私有空图；保留Dodge并行、窗口、ActionTarget及仍被引用的阈值，不改变正式控制意图时机。
- [ ] 4.3 通过公共export_code/generate_assets及正式FSM API完成明确Skill范围完整输出与重建，核对状态/配置/条件/priority/abortPolicy/order/生命周期/layout/共享对象及指定Definition根引用；交付往返和删除生成输出后重建记录，保留业务identity，不依赖旧子资产GUID，不自动合并未导出修改。
- [ ] 4.4 让已有FSM/Skill领域校验与Character编译器、独立显式Build直接消费重建资产，核对Program/Projection和来源身份并交付当前结果；不新增Agent Validator或自动Build，完整运行轨迹仍归Corin闭环。
- [ ] 4.5 定位并修复属于本作者层的m_Name重名问题；不全局改名或清缓存。

## 5. 删除与交付

- [ ] 5.1 在有效操作已迁出且调用者脱离Agent后，按公共退役范围删除本领域无消费者旧状态存储、协议适配/补读和一次性转换；交付源码消费者清单，保留原生FSM和合法非Skill/普通FlowGraph能力，不整目录或整文件搬迁。
- [ ] 5.2 按design D6归并剩余四份领域delta，撤销本change的Document v8发布依赖，保留正确FSM、组合、编译与current后来新增场景；只向公共退役结果提供本领域规范差异，公共工具/协议规范归C# authoring负责。
- [ ] 5.3 整理本领域API、C#输出/生成接入、完整往返与旧Agent依赖归零结果，明确历史完成和未完成项；不承担事件图运行、Pose变量、通用观察、网络Adapter或Replay。

## 6. r2正式FSM能力迁出与C#输出接入

- [ ] 6.1 逐方法盘点BtsmtlSkillGraphAuthoringApplier的FSM创建/配置/连接/条件/owner/系统入口/身份及校验，交付保留能力→已有模块→协议删除项清单和真实冲突；不能仅凭目录名删除。
- [ ] 6.2 将FSM/State创建、StateBody挂接、参数/layout配置及明确owner操作落实到已有工厂和原生FSM/State模块；交付公开typed读取/配置入口及调用链，拒绝AgentPackage/JToken或私有字段反射作为参数合同。
- [ ] 6.3 将系统入口复用、状态/边业务identity恢复、连接/改接、condition/priority/abortPolicy/order落实到正式领域API；交付新对象可恢复既有业务身份的结果，不要求local协议ID，不按输出排序改变转移，不重复创建工厂默认内容。
- [ ] 6.4 提供FSM及StateBody/Condition/Macro等拥有闭包的正式读取和内部/外部引用区分，交付完整字段/生命周期/layout/动态接口及根绑定覆盖表；内部共享只创建一次，未知字段或引用明确拒绝完整输出。
- [ ] 6.5 将仅存在旧Applier的有效owner/身份/条件角色/顺序约束补入已有NativeStateMachineContract、GraphClosure或对应配置方法，删除重复规则；交付人工编辑和生成共用约束的调用链，不新增中央Validator、整包事务或源码同步。
- [ ] 6.6 在公共C# authoring入口与输出器接通后提供薄FSM适配，按创建→配置→引用→连接→指定根挂接输出正式API调用；交付完整源码、精确外部依赖、未映射字段拒绝与明确生成范围结果，代码未编译时不执行旧同名入口，Build保持独立。
- [ ] 6.7 人工作者/编译及生成调用者全部切到正式领域API后，配合公共退役清理旧Applier协议部分；交付本领域对Agent Document/DTO/Session/Reconciler/重复Validator的直接及间接依赖归零扫描，不能把整个Applier改名保留。
