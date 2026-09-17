## 当前执行边界：r5共享准入命名与StateBody系统锚点

第1—7节勾选保留阶段完成记录；下一轮只执行第8节增量。旧任务中的“删除作者OnExit”指删除作者清理图及包装，不删除OnExit系统锚点；准入规则统一以独立GameplayAbilityAdmissionProfile为唯一来源。

r3/r4阶段以GameplayAbilityDefinition为完整技能作者入口，第7节记录该阶段实现；当前新增要求以第8节为准，不重新打开r2任务，也不以历史勾选代替r5交付。任务只列实现、迁移、删除及必要规范修改，不列验证、证据收集、Build验收或手动测试任务。

r4固定分工：Ability决定结束，代码自动停止清理；Timeline保留片段内部混合，动作Slot管理跨播放接替，Pose状态机管理基础转移，动画EventGraph只计算动画变量/判断，不转发所有播放或退出。7.8—7.12替换旧OnExit分派及强制事件图草案，已有勾选不变，不新增验证任务。

## r2历史执行边界

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
- [x] 4.3 已通过公共`btsmtl.generate_assets`和`btsmtl.export_code`及正式FSM API完成Corin Attack Skill范围的重建/输出；恢复状态配置、StateBody、条件、priority、abortPolicy、order、生命周期、layout、共享对象和Definition根绑定，保留业务identity，不依赖旧子资产GUID，不自动合并未导出修改。
- [x] 4.4 已让现有FSM合同、Skill闭包、`BtsmtlSkillGraphOccurrence`和`BtsmtlSkillGraphCompiler`直接消费重建的native FSM；独立Build入口保持原有显式边界，本轮按要求不运行Build，不新增Agent Validator或自动Build。
- [x] 4.5 已根据完整Console调用栈确认m_Name问题归属Slate.CutsceneGroupInspector，不属于本Skill/FSM作者层；不改名、不清缓存、不扩大范围。

## 5. 删除与交付

- [x] 5.1 有效FSM操作已迁出且调用者已脱离Agent；已删除本领域无消费者旧状态存储、Skill/Presentation协议链和一次性转换入口，保留原生FSM及合法非Skill/普通FlowGraph能力。独立`btsmtl.scene_play`预览不属于旧authoring链，继续保留。
- [x] 5.2 已按design D6完成r2规范对账：撤销本change的Document v8发布依赖，保留正确FSM、组合、编译与current后来新增场景；本change只向公共退役结果提供Skill/FSM领域差异，公共工具/协议规范归C# authoring负责。
- [x] 5.3 已整理Skill/FSM正式API、C# export/generate接入和旧Agent依赖边界；事件图运行、Pose变量、通用观察、网络Adapter与Replay均未纳入本change。

## 6. r2正式FSM能力迁出与C#输出接入

- [x] 6.1 盘点BtsmtlSkillGraphAuthoringApplier中的FSM创建、配置、连接、条件、owner、系统入口和身份操作，并将有效能力落到已有模块；不能仅凭目录名删除。
- [x] 6.2 将FSM/State创建、StateBody挂接、参数/layout配置及明确owner操作落实到已有工厂和原生FSM/State模块；公开typed读取/配置入口，拒绝AgentPackage、JToken或私有字段反射作为参数合同。
- [x] 6.3 将系统入口复用、状态/边业务identity恢复、连接/改接、condition/priority/abortPolicy/order落实到正式领域API；不要求local协议ID，不按输出排序改变转移，不重复创建工厂默认内容。
- [x] 6.4 提供FSM及StateBody/Condition/Macro等拥有闭包的正式读取和内部/外部引用区分；内部共享只创建一次，未知字段或引用拒绝完整输出。
- [x] 6.5 将仅存在旧Applier的有效owner/身份/条件角色/顺序约束补入已有NativeStateMachineContract、GraphClosure或对应配置方法，删除重复规则；不新增中央Validator、整包事务或源码同步。
- [x] 6.6 在公共C# authoring入口与输出器接通后提供薄FSM适配，按创建→配置→引用→连接→指定根挂接输出正式API调用；代码未编译时不执行旧同名入口，Build保持独立。
- [x] 6.7 人工作者、编译及C#生成调用者已切到正式Skill/FSM API；旧Applier及其协议调用者已按公共退役范围移除，未改名保留整套Applier。

## 7. GameplayAbilityDefinition完整入口

- [x] 7.1 将现有SkillDefinition职责迁为独立GameplayAbilityDefinition资产，集中技能身份、激活/阻断/取消/目标要求、已有效果引用及后续能力关系；技能专用规则由Ability拥有，显式共享规则保持唯一对象来源。
- [x] 7.2 将AbilityGraph及私有FSM/StateBody/Condition/Macro/Timeline归属Ability资产；角色/装备改为AbilityGrant精确引用和输入绑定，移除SkillDefinitions与SkillGraphs双重登记，保留业务identity与共享资源引用。
- [x] 7.3 建立统一Ability作者入口，在同一页面编辑规则、目标要求、执行图与资源，并显示共享规则来源；沿原生GraphEditor下钻私有内容，创建技能时自动建立内部拥有关系。
- [x] 7.4 将Skill专属类型和调用统一为AbilityGraph、AbilityExecution和AbilityExecutionContext，复用现有释放状态；普通Ability移除手配空ActionContextSlot，由当前执行上下文贯通节点与Timeline，保留合法非Skill业务和原准入分组/取消顺序。
- [x] 7.5 扩展公共C#工具的领域根支持，从GameplayAbilityDefinition完整输出外壳、规则、图、Timeline局部片段混合、Slot引用及声明范围的AbilityGrant挂接；跨动作混合规则保留动画owner引用，子图模式只处理子图及原owner，不依赖旧外壳或子资产GUID，不新增MCP入口。
- [x] 7.6 迁移Corin现有技能、共享Dodge规则和角色授予引用，删除被替代的Skill专属旧字段、类型、资产入口与兼容别名；不改变阈值、输入消费、目标、并发、取消、MovingTurn或Program执行行为。
- [x] 7.7 同步受本次改动影响的正式作者/编译/代码生成接口及规范命名，修订角色装配根与独立Ability资产合同；保留r2公共双工具边界，不扩展成本/冷却/等级或其它业务系统。
- [x] 7.8 将执行根完成、取消规则、外部中断/替换和强制终止请求汇入同一Ability生命周期入口，保留Complete/Cancel/Interrupt/Abort和原有效原因，先记录结束决定再停止内容，不从StateExitCause反推或在回调中重复提交。
- [x] 7.9 将状态退出、子图/Timeline停止、相关窗口和运行资源回收固定在原owner代码内，保留必要内部OnExit与ForceStop流程；移除Ability作者必配的OnExit清理节点/页及创建约束，不依赖作者连线保证释放。
- [x] 7.10 在原生命周期与播放通知中保持明确Ability/执行实例、结果/原因、Tick和producer关联，播放采样保留Timeline时间与局部片段权重，停止由代码自动请求；直接进入正式Presentation/Slot接口，不新增全局总线或动画EventGraph必经转发。
- [x] 7.11 对接现有动作Slot精确源/目标过渡及混合历史接口，保留Timeline局部混合、Slot跨播放规则与Pose基础转移各自唯一owner；旧逻辑停止后不继续窗口/效果/Motion，不因表现淡出等待，不在Ability/事件图中维护混合栈或复制动画算法。
- [x] 7.12 将两个Dodge旧ActionExit分支的有效条件迁入Ability结束规则/请求入口，删除Action Exit Selector、空Action Exit_To_Succeed_Rule、被替代Submit实例/私有包装及空作者OnExit；保持已清理根图、四种终态和正常连段，不重新生成旧壳或默认Complete兜底。

## 8. r5共享准入命名与StateBody系统锚点

- [x] 8.1 将ActionProfile类型、配置API、Inspector/菜单及GameplayAbilityDefinition.AdmissionProfile统一为GameplayAbilityAdmissionProfile；保留独立共享规则资产，移除准入内联/覆盖副本及旧类型别名。
- [x] 8.2 迁移Corin准入资产到Abilities/AdmissionProfiles及明确新文件名，更新AbilityGrant、角色/Equipment、CanActivate/ActionTarget与全部正式序列化引用；保留Dodge单一共享对象、原分组identity和业务行为，删除废弃资产、.meta及无人使用的旧目录。证据：CorinAttack/CorinDodge准入资产git mv为Abilities/AdmissionProfiles/CorinAttackAdmissionProfile.asset与CorinDodgeAdmissionProfile.asset，GUID随.meta保留，Dodge单一共享对象继续被DodgeBack/DodgeForward引用；资产m_Name同步更新；旧Pipeline/Actions目录、Attack/Dodge目录.meta与Actions.meta全部删除；Definition与Ability资产引用走GUID无需改写；生成Root.cs路径字符串与corin-source-manifest.json的assetPath/owner同步为新路径与AdmissionProfile命名，全仓旧名旧路径仅剩本change delta迁移场景描述。
- [x] 8.3 统一依赖发现、语义编译、Program Catalog与Fixed/Float32所有准入消费者的命名和身份映射；公共export_code/generate_assets及生成源码输出新Profile合同和范围内唯一共享对象，移除旧入口，不新增并行catalog或执行状态。证据：CompileDiscovery经Ability.AdmissionProfile取唯一Profile；语义依赖目录DeclareAction按ActionId声明身份；Float32/Fixed ExecutionServices以ActionAdmissionProfile按operation与actionId双索引；export_code适配器输出EnsureAdmissionProfileRoot与ConfigureAdmissionProfile且Dodge仅输出唯一共享对象；现行Attack.Float32Data的m_OperationSetVersion为character-gameplay-operations/16，全仓运行/编辑器代码无ActionProfile旧命名。
- [x] 8.4 保留StateBody工厂已正确生成的OnEnter/Root/OnExit三系统锚点，补齐不可删除、OnExit不进作者菜单、正式图合同及C#重建/编译映射；仅由运行代码在State stop barrier调用OnExit清理，保留ForceStop释放，不恢复Action Exit/Submit作者清理链。证据：PopulateAnchors与EnsureRequiredAnchors固定生成/补齐三锚点；RequireRemovable、Delete/Cut命令、Clear与Copy均拒绝系统锚点；锚点节点[DoNotList]且CapabilityCatalog按IsAnchor过滤作者菜单；GraphClosure校验锚点不重复且RequiredAnchors齐备；FlowEmitter以Single校验并按ProgramControlFlowKind Enter/Enter/Exit与order 0/1/2声明OnEnter/Root/OnExit；运行时ContinueStateStop在stop barrier执行OnExit子图，ForceStopState只ForceStop释放；Corin三张Ability资产已无Action Exit Selector/Submit清理链。
- [x] 8.5 同步btsmtl-sm-node-authoring、character-pipeline-definition-authoring、btsmtl-flowcanvas-authoring及所有受影响正式spec条款，清除两锚点例外、旧ActionProfile命名和准入内联要求，保留非Ability合法场景及既有完成记录。

## 9. r6 TreeClip控制流接通

- [ ] 9.1 定位TimelineBody图被编译跳过的根因：closure已将TreeClip AssetTree收进正式闭包，但Compile未为其生成operations（Float32/Fixed产物中图身份1eac26e4、Root handle、hook entry全部0命中），修复为TimelineBody图按正式reference路径完整编译，不留跳过分支。
- [ ] 9.2 编译期Root entry登记：TimelineClip caller时除OnEnable/OnDisable/OnDestroy三个hook外，将Root（技能入口）声明为图entry invocation，SourceMap按clipId登记Root handle供运行时查询；重导出Corin Attack后产物须含TimelineBody图身份、Root、hook entry。
- [ ] 9.3 运行期每帧驱动：AbilityTreeClipHook增加Root条目；CharacterTimelineTreeClipService.Consume的Update分支对活跃TreeClip的Root entry调用TickPersistent，Fixed/Float32两侧InvokeTreeClip同步支持；Root连的控制流（Child边）每帧执行，Root链Success即clip主体完成事实；边界hook保持一次性。
- [ ] 9.4 回滚同构验证：TreeClip Root轮询状态全部存C#显式状态槽（runnable lifecycle与cursor），Local Fixed回滚重放与State的TickState同构；dotnet build带--disable-build-servers零错误后shutdown；端到端连段窗口由用户验收。
- [ ] 9.5 spec收口：btsmtl-runnable-timeline-node删除RootTree operation旧挂靠条款，写入现行合同（TreeClip编译为TimelineBody operations、Root轮询根每帧tick、hook一次性边界、锚点不可删不进作者菜单）；清理其他spec的RootTree operation残留表述。
