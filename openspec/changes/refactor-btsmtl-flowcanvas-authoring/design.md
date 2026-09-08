## Context

本设计只覆盖BTSMTL技能。PoseGraph由[独立提案](../refactor-character-pose-graph-architecture/proposal.md)及[FlowCanvas文档](../refactor-character-pose-graph-architecture/flowcanvas-experiment.md)管理；本文不决定其执行路线、不修改其任务状态。

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

### 5. 预览由正式Scene Play拥有

场景、Session、启动、停止、重建、暂停和单步都由唯一Scene Play协调器负责；本文只接技能观察和显示。构建显式触发，stale产物不冒充新内容；一步是完整安全更新单位，不在半次事务暂停。

子图缺少角色或参数上下文时从父技能运行并定位调用，不填假数据。协调器未完成则保留依赖缺口，不建临时播放器。原生协程断点不用于编译执行，本批不展示不支持的指令断点。

### 6. Document及迁移

v6增加技能Macro接口、owner和调用闭包，保持业务kind、typed字段、逻辑端口、整包hash和五生命周期。直接读写正式技能图，不暴露第三方私有字段、委托或诊断状态。旧包拒绝并重新checkout。

协议升级不等于迁移其他领域：非技能分片保持现有业务模型，共用Codec须证明其无业务变化往返，不能借版本切换强制转换Pose。

按精确技能闭包先生成迁移计划，核对身份、布局和资源，真实冲突由作者决定。根、私有Macro及实际修改共享owner进入完整事务；失败完整恢复。旧入口仅在确有未迁移消费者时保留并列明。

## Risks / Trade-offs

- [旧节点类型耦合编译] → 先拆正式输入，再接新图，不用旧对象树作为转换中间层。
- [原生编辑绕过Mutation] → 审计所有创建、字段、连线、接口、clipboard及Undo入口，必要扩展只做一次。
- [共享定义与执行实例混淆] → 区分资产owner、节点identity与调用执行identity。
- [观察影响执行] → 只消费完成快照，采集沿既有有界通道。
- [预览依赖未闭合] → 记录具体缺口，不增加第二预览路径。
- [共用修改影响Pose] → 审计消费者，本文不得替代Pose文档的决定。

## Migration Plan

1. 固定技能基线、能力清单及原BTSMTL／Scene Play任务归属。
2. 完成原生作者、能力与事务入口，迁Macro及局部状态／规则页面。
3. 直接编译新技能图，保持Numeric Target、ActionInstance及恢复合同。
4. 完成v6与技能资产事务迁移，显式构建产物。
5. 接入来源映射、实例观察、子图导航及正式Scene Play控制。
6. 用现有CLI完成编译、Document往返、技能回放及诊断；不新增测试代码，不把手工验收写入tasks。
7. 删除无消费者旧技能路径，安装规范和说明；不迁移Pose资产。

失败回退整个技能源码、资产和产物批次，不保留运行fallback。涉及用户改动的回退需作者决策。

## 规范与文档归属对账

| 文档 | 本变更关系 |
|---|---|
| refactor-btsmtl-authoring-architecture | 原变更拥有C#控制、技能业务和状态拆分；本文拥有技能FlowCanvas作者、直接编译入口及观察 |
| rebuild-btsmtl-preview-with-scene-play | 拥有预览生命周期；本文消费正式接口 |
| refactor-character-pose-graph-architecture | 独立管理Pose，不由本文替代其22.x、23.x或24.x |
| btsmtl-graph-core | 技能退出旧BaseGraph和inline图要求；其他领域原约束保留 |
| Domain Framework／Editor Shell | 只迁技能，不强制其他领域切换 |
| Document与project旧版本描述 | 安装时统一v6协议，保持非技能业务语义 |

原混合变更已更名为本变更，Pose规范增量及任务移出本目录。已有原型或未完成资产清理不算技能迁移完成证据。
