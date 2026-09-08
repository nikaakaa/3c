## Context

最新验证授权：可以刷新Unity并检查脚本编译；只处理本次BTSMTL代码引入的问题，其他领域报错只记录。内容Build、整根校验和回放仍未恢复主动执行。

执行纠正（2026-09-08）：作者要求当前大改阶段停止主动编译、Build、整根校验和回放；实施先完成代码及接口迁移，验证留到收口。技能入口以实例工作区为中心，通过明确Actor、ActionInstance、generation及调用位置定位作者来源；Character Definition仅是资源／合同引用，不再作为Open Root Tree作者或观察入口。现存依赖RootTree的发现与导航代码是待迁移旧实现，不是应当复刻的目标。

本设计只覆盖BTSMTL技能。PoseGraph由[独立提案](../integrate-pose-flowcanvas-editor-preview/proposal.md)管理；两边均为编译执行加Editor观察，本文不实施Pose迁移。

当前技能入口仍从旧图闭包发现，节点发射直接依赖BaseNode和PropertyPort；运行已有ActionInstance、generation、调用状态和Numeric Target。FlowCanvas已有原生Macro和端口，原生高亮依赖实际Flow／Value调用，不能直接观察项目编译执行器。原生Sequence是Flip Flop，不能替代技能等待完成的顺序语义。

## Goals / Non-Goals

**Goals:**

- 一份正式技能图直接编译，保留技能生命周期与唯一Session执行入口。
- 使用原生节点、端口、Macro及交互，所有写入进入同一能力和事务合同。
- 从真实诊断定位具体技能释放、子图调用及节点状态。

**Non-Goals:**

- 不迁移Pose或AI，不改动画、IK、相机、移动及网络模型。
- 不用FlowCanvas runtime执行技能，不开放无编译合同的事件、反射或协程节点。
- 不新增预览播放器、时间旅行、远程Player调试或图内指令断点。

## Decisions

### 1. 正式图直接进入编译

链路为`技能FlowCanvas图 -> 只读领域遍历 -> Semantic IR -> Numeric Program -> Session Step`。遍历只提供编译输入，不生成旧BaseGraph对象树。节点来源、端口常量和操作语义分开，现有Program Builder继续唯一。

业务取舍：原生runtime能使用更多现成节点，但改变状态恢复要求；镜像作者图改动较少，却维护两份拓扑。本方案保留运行行为，承担领域编译接口的迁移。

### 2. 原生编辑服从技能能力与事务

领域FlowGraph、FlowNode和BinderConnection保存正式数据，Capability唯一声明kind、字段、端口、role及编译支持。复用原生排版、命中、选择、拖线、复制及导航；所有修改先校验，再进入真实owner事务。多个兼容输入显式选择，不默认首端口或偷偷转换类型。

人工单操作Undo和Document整包事务各自只有一个owner，handler不重复记Undo。缺少正式拦截点时只扩展domain-neutral接口并标记3C，不复制编辑器。共用代码修改核对全部消费者，其他领域保持原行为。

业务取舍：受控节点目录少于全套蓝图库，但作者能创建的技能都有明确执行意义。

### 3. 原生Macro、局部状态与Timeline

用户已确认：技能主要由节点组织流程、Timeline承载动作时序，不新增状态机来替代该主链。技能Macro采用单执行入口和多个值输入／输出；父节点跨帧等待子图主体完成，再获取返回值和Success／Failure，停止沿原技能中断协议传播。本批不提供多执行入口、多个控制出口或通过值读取启动技能调用；条件页继续使用纯值节点与条件结果。保留连招阶段属于业务黑板状态及重置规则，不依赖多入口，也不在本批额外实现连招业务。

技能Timeline节点只引用TimelineAsset。私有Timeline自动保存为技能根文件内的子资产，共享Timeline引用独立资产；不在原生节点JSON中再保存一份TimelineData。这样Timeline编辑器仍通过TimelineAsset的真实序列化路径编辑、Undo和保存。TreeClip通过领域无关的节点图资产接口引用TimelineBody页面，保留Root及OnEnable／OnDisable／OnDestroy入口；设置资产图时清空旧内联／共享树来源。独立Timeline尚未迁移的旧树表示继续由其既有编译器负责，技能编译不回到旧树。

作者已选择优先原生规则：顺序、选择、并行的每个子步骤采用一个独立Flow输出端口，不扩展原生FlowOutput为一口多线。步骤采用作者拥有的稳定slot identity，调整步骤次序只改变slot排列，不重建已连接端口身份；迁移把旧子边次序映射为slot顺序。编译仍发射原技能组合语义，不采用原生Flip Flop的轮流触发语义。

复用Macro接口、调用节点和IGraphAssignable下钻。私有Macro自动创建为技能根子资产，共享Macro显式外部引用，不保存inline副本。删除调用不删除共享定义；私有闭包复制、回收及保存由根事务负责。

端口身份独立于名称和顺序。接口类型变化检查发布闭包中的调用；递归、闭包环和跨领域调用拒绝。Sub Flow不因原生菜单存在自动开放，本批参数化子图统一用Macro。

运行状态按ActionInstance、generation、调用路径和调用执行身份隔离，不写入共享定义。同一调用位置并发执行仍须区分。技能局部状态机、条件页及顺序、选择、并行、循环保留业务含义，不用Macro代替状态机、不用Flip Flop代替顺序等待。Timeline继续独立编辑并保持完成、中断和停止顺序，TreeClip回到对应调用页。

业务取舍：私有子资产增加根事务责任，但避免作者手动管理大量外部文件；共享定义便于复用，代价是接口变更检查整个调用闭包。

### 4. 编译来源与运行观察

映射绑定作者根及revision、产物identity、调用路径、Node／Port／Edge身份，对应operation、value、state slot。一对多发射保留阶段，优化消除显式标记。作为现有产物组只读附件同版本发布，不进入Gameplay snapshot或网络真相。

运行诊断沿既有通道发布紧凑操作及调用执行身份，在完成边界成为只读快照。原生绘制接外部观测源，不StartGraph、不BindPorts、不调用getter、不伪造graph.isRunning；运行代码不依赖编辑器图。

区分经过、Running、Waiting、Completed、Cancelled和Interrupted。实际分支或读取记录才支持亮边；值来自最近采集结果，悬停和刷新不求值。兴趣订阅和存储有界，缺失或覆盖明确标记；关闭、节点观察、值观察分别记录开销。

版本不匹配停止错误高亮。父图显示调用状态，进入共享子图沿精确释放和调用执行定位，breadcrumb保持路径。结束实例显示终态，不自动换成下一次释放。

业务取舍：只按图GUID实现简单但混淆多次调用；完整调用定位多一些诊断数据，却能准确说明这次技能在等待什么。

### 5. 技能预览就是普通运行观察

Unity进入Play后，角色通过普通输入或已有正式调试入口释放技能，原Session及ActionInstance照常执行。窗口只观察某一次释放，不创建预览实例、独立场景或时钟，不以Scene Play协调器作为前置条件。构建显式触发；版本不匹配停止错误叠加，不热换运行产物。

没有有效角色或释放实例时显示等待目标，多个实例时明确选择；子图沿真实调用位置观察，不填假数据。若窗口提供释放按钮，它只向已有角色提交正式技能请求，仍由原准入、成本和中断规则决定结果，本提案不要求新增该按钮。Unity暂停时保留最后完成结果；退出Play、关闭窗口或实例代次变化时解绑。不提供窗口播放、暂停、单步、seek或原生协程断点。

### 6. Document及迁移

v6增加技能Macro接口、owner和调用闭包，保持业务kind、typed字段、逻辑端口、整包hash和五生命周期。直接读写正式技能图，不暴露第三方私有字段、委托或诊断状态。旧包拒绝并重新checkout。

协议升级不等于迁移其他领域：非技能分片保持现有业务模型，共用Codec须证明其无业务变化往返，不能借版本切换强制转换Pose。

Document实现范围由作者指定的「agent工具」任务负责，修改仍归入本change，不新建交接文档。需要贯通的代码链是Document模型与Codec、Exporter、Mapper、Reconciler、现有Mutation dispatcher、资产resolver与索引、Validator及五生命周期说明；不能只修改版本号或只增加导出字段。

原生技能源与Document的对应合同如下。表内为本批必须完成的目标，不表示当前Document已经支持：

| 正式作者源 | Document必须表达和保持的内容 |
|---|---|
| CharacterPipelineDefinition.SkillGraphs及SkillDefinition.EntryGraphAuthoringId | 明确技能根引用与稳定入口身份；不得从旧RootTree重新推测技能入口 |
| BtsmtlSkillFlowGraph及BtsmtlSkillMacroGraph | 图身份、角色、真实owner、节点与连线身份、布局；导入和反向导出直接访问原生对象 |
| Macro接口及原生调用节点 | 单执行入口、多值输入输出、稳定参数ID和明确调用引用；重命名不重建端口ID，类型变化校验所有受影响调用 |
| 组合节点的步骤列表 | 稳定步骤ID、次序、条件页、优先级及中断策略；排序不重建已连接端口 |
| 原生Variable与BlackboardDeclarations | Variable唯一保存名称、类型和默认值；元数据只补充作用域、生命周期、输入绑定及窗口投射，不另存一份变量值 |
| Timeline节点及TimelineAsset | 节点引用真实资产；私有Timeline由技能根文件拥有，共享Timeline引用独立资产，不在节点正文复制TimelineData |
| TreeClip.AssetTree | 引用TimelineBody原生页面并保留生命周期入口；技能闭包不经旧内联树中转 |

能力目录必须覆盖已支持的原生逻辑、结构、输入、动作、黑板和移动节点，并与唯一端口形状合同一致。Document只表达业务kind、typed属性、逻辑端口和资源身份；C#类型名、私有字段名、编译索引及运行观察状态不进入可编辑正文。

人工编辑与Document应用使用同一能力校验和Mutation处理链。Document先对整包做预检，再修改根、私有页面及实际受影响的共享owner；保存或反向导出失败时恢复整个事务。当前原生编辑事务辅助代码不能被当作另一套正式Document服务保留。

v6完成标准包括严格拒绝v5及更早包、显式checkout、无业务变化的零修改dry-run、同hash的apply及重新checkout、完整owner回滚和非技能分片语义保持。实际资产执行仍遵守先代码与编辑器、最后迁移的顺序；源码完成和往返执行证据分别记录。

按精确技能闭包先生成迁移计划，核对身份、布局和资源，真实冲突由作者决定。根、私有Macro及实际修改共享owner进入完整事务；失败完整恢复。旧入口仅在确有未迁移消费者时保留并列明。

## Risks / Trade-offs

- [旧节点类型耦合编译] → 先拆正式输入，再接新图，不用旧对象树作为转换中间层。
- [原生编辑绕过Mutation] → 审计所有创建、字段、连线、接口、clipboard及Undo入口，必要扩展只做一次。
- [共享定义与执行实例混淆] → 区分资产owner、节点identity与调用执行identity。
- [观察影响执行] → 只消费完成快照，采集沿既有有界通道。
- [没有真实释放实例] → 显示等待目标，不创建假角色或预览技能实例。
- [共用修改影响Pose] → 审计消费者，本文不得替代Pose文档的决定。

## Migration Plan

用户已明确要求先完成代码和编辑器，再迁移资产。以下为依赖关系，不代表允许在编辑器和观察代码尚未收口时提前执行资产apply；迁移执行与产物发布统一放在最后。

1. 固定技能基线、能力清单及原BTSMTL／Scene Play任务归属。
2. 完成原生作者、能力与事务入口，迁Macro及局部状态／规则页面。
3. 直接编译新技能图，保持Numeric Target、ActionInstance及恢复合同。
4. 完成v6与技能资产事务迁移，显式构建产物。
5. 接入来源映射、Play生命周期、精确释放实例观察和子图导航，不新增运行控制器。
6. 用现有CLI完成编译、Document往返、技能回放及诊断；不新增测试代码，不把手工验收写入tasks。
7. 删除无消费者旧技能路径，安装规范和说明；不迁移Pose资产。

失败回退整个技能源码、资产和产物批次，不保留运行fallback。涉及用户改动的回退需作者决策。

## 规范与文档归属对账

| 文档 | 本变更关系 |
|---|---|
| refactor-btsmtl-authoring-architecture | 原变更拥有C#控制、技能业务和状态拆分；本文拥有技能FlowCanvas作者、直接编译入口及观察 |
| rebuild-btsmtl-preview-with-scene-play | 可选独立受控场景不属于本观测前置条件；本文不接管该生命周期 |
| integrate-pose-flowcanvas-editor-preview | 独立管理Pose作者UI和真实动画帧观察；共享显示钩子复用，技能按某次释放定位 |
| btsmtl-graph-core | 技能退出旧BaseGraph和inline图要求；其他领域原约束保留 |
| Domain Framework／Editor Shell | 只迁技能，不强制其他领域切换 |
| Document与project旧版本描述 | 安装时统一v6协议，保持非技能业务语义 |

原混合变更已更名为本变更，Pose规范增量及任务移出本目录。已有原型或未完成资产清理不算技能迁移完成证据。
