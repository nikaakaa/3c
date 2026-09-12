## Context

范围按用户澄清收窄：原业务节点已经存在，接入FlowCanvas后又出现一套同义定义。本change把业务定义保留一份，FlowCanvas使用它，不自行迁移图拓扑或Document版本。动机见[proposal.md](proposal.md)。

并行规划对账：integrate-native-fsm-skill-authoring负责原生NodeCanvas FSM与最终v8；该目标尚非已发布事实。本项只提供共同业务定义，图后端和版本选择归该change；其先落地时按正式新基线接入，不能把本项“保持当前合同”解释为恢复v7或强制状态机继续使用FlowGraph。

### 已核对的重复实例

| 业务 | 原节点 | FlowCanvas节点 | 当前差异 |
|---|---|---|---|
| 移动输入运动 | LocomotionInputMotionNode | BtsmtlSkillLocomotionFlowNode | 两侧重复保存七项参数；原ConfigureAuthoring检查模式、数值、曲线和时长，新Configure仅赋值 |
| 动作窗口条件 | ActionWindowActiveInfoNode | BtsmtlSkillActionWindowActiveFlowNode | 两侧声明窗口类型和条件输出 |
| 动作准入 | CanActivateActionInfoNode | BtsmtlSkillCanActivateActionFlowNode | 都表达ActionProfile与目标快照，引用外壳和owner表达不同，需要明确映射 |
| 输入与动作请求 | CharacterInput系列、CharacterActionRequestInfoNode | 对应Skill输入节点 | 输入身份、值类型、provider归属和操作语义需要逐项对照 |
| 状态与组合 | 原State、Sequence、Selector等节点 | Skill结构节点 | 名称相同不保证生命周期和端口表达相同，先核对业务等价范围 |

移动节点已共用ILocomotionInputMotionAuthoring和一段Locomotion编译逻辑，但字段、默认值和作者写入校验仍有两份。共享接口不等于业务定义已经统一。静态盘点曾找到43个Skill kind声明和15个逻辑wrapper登记；实施以当时实际注册集合为准，Macro、variant和未在Corin出现的能力不能遗漏。

## Goals / Non-Goals

**Goals:**

- 同义原节点和FlowCanvas节点只有一份业务参数类型、默认值、校验、逻辑端口与引用定义。
- 原有正确业务规则保留；发现两侧不同则列明差异，不能默认以新节点的较弱规则覆盖。
- 每个节点实例分别拥有自己的参数值；共同定义不意味着共享可变实例。
- 画布、现有Document和编译消费同一定义，迁移后删除重复声明与无消费者旧代码。

**Non-Goals:**

- 不迁出FlowCanvas图拓扑，不重做GraphEditor、Macro接口、保存/Undo系统或运行执行器。
- 不在本change迁移状态机Step/Edge数据、固定转移端口、条件图owner或并列转移顺序。
- 不自行升级Document版本或改动实施基线的公开kind、字段、端点和owner形状，不重做Agent工具；当前已发布基线为v7。
- 不重做作者语义hash/布局hash、网络、运行观察、Pose、Foot、AI或TrainingEnemy。
- 不新增测试代码，不把手动端到端验证写入tasks。

## Decisions

### D1 业务定义归原业务模块

共同参数与规则放在对应Motion、Action、Input或结构业务模块，不能归Agent或FlowCanvas专属模块。定义表达字段身份、参数类型、默认值、合法值、业务逻辑端口和引用；编译实现继续独立，只通过正式业务接口使用这些数据。

原框架节点和FlowCanvas节点是适配器：持有本实例的一份参数，负责本框架节点基类、端口物化和事件接入。不得把整个旧BaseNode嵌入FlowNode，也不得额外复制一份可写图。仍有正式消费者的原节点接入共同定义；没有消费者的原适配迁移后删除。

业务取舍：直接复制原节点到FlowCanvas实现更快，但每次业务变更仍要维护两遍；抽共同定义有一次迁移成本，之后两侧使用同一规则。保留真实用途的适配器是框架接入，不是保留废弃业务实现。

### D2 先区分同义、独有和废弃能力

逐对比较字段及默认值、单位/值类型、约束、输入输出、引用、执行和停止行为，不能仅按类名或kind相同合并。

- 同义部分：抽一份共同定义，两侧接入。
- 只有一侧存在且仍属正式能力：仍按该业务模块定义，不为凑对称制造另一个节点。
- 已无正式消费者：删除旧节点、目录登记及仅服务它的适配代码。
- 有已确认业务冲突：列出原值、当前新行为、影响节点和调用者，由用户决定；独立的无冲突节点可以继续。

例如Float/Int比较、不同作用域黑板及结构节点不能只因显示名接近而合成同一种执行语义。输入或目标引用的旧上下文只有能从正式owner准确解析时才映射，不得按显示名、selection或目录猜测。

业务取舍：强制所有同名节点共用一种实现会压掉真实业务差异；逐对归属需要完整清单，但能保留原系统已经正确的行为。

### D3 字段和逻辑端口只定义一次

每个业务定义集中提供字段读取/修改、默认值、约束和必要的条件可见性。两个框架的本地端口ID、基类和UI控件可以不同，但必须显式映射到同一业务字段/逻辑端口；这些映射不重新声明默认值或校验。

复用现有Capability、typed field和Port Shape基础。原目录与FlowCanvas目录只投影共同定义；删除构造默认FlowNode反推业务形状以及重复动态规则。原生逻辑wrapper的业务映射、Macro参数和Blackboard访问从各自正式定义/接口提供，不能通过运行getter求值。

本次保持已发布端口和字段身份。共同定义中的逻辑名称不要求改掉各宿主已有公开端点；映射只能按明确登记处理，不接受兼容别名或隐式类型转换。

未接线输入字面量继续使用本宿主唯一正式存储，定义提供默认值和类型，不能再拷进另一份参数对象。Macro接口和原生Variable保持各自唯一存储。

业务取舍：显式宿主映射能保持现有图连线和包格式，代价是保留少量框架接入代码；统一业务规则不需要把两种框架改成同一个节点类。

### D4 原生编辑与引用操作使用共同定义

原生Inspector通过共同字段访问和现有typed Mutation修改参数，复杂字段使用业务控件。条件显示和合法性不能再在Inspector中独立决定。将正式Ability节点从Inspector文件中移回业务定义/适配位置，但不因文件拆分重做整个编辑器。

受参数抽取影响的资源/子图引用通过共同定义读取和重映射，复用现有复制、owner与事务链。复制私有引用、保留共享引用、删除回收和保存重载必须保留既有正确行为。本次不另建通用引用引擎，不顺带改变Edge条件的存储归属。

业务取舍：同步必要消费者才能让参数抽取真正可用；重做整套引用/Undo系统会扩大改造面，因此只接入因字段迁移而受影响的部分。

### D5 编译业务逻辑独立于两种节点宿主

原节点编译登记与Skill编译适配读取同一业务参数接口，并调用适用的共同lowering。现有ILocomotionInputMotionAuthoring及Locomotion逻辑继续复用；已正确的Program Builder、Numeric Target与运行执行链保持原职责。

本地UID/端口和调用路径通过宿主适配进入现有SourceMap。不得为接入共同定义而构造旧节点执行，也不能把编辑器字段反射当运行语义。不同宿主的源码位置和完整产物hash可能不同，一致性比较针对业务参数、操作、连接和执行语义，不能要求所有序列化字节完全相同。

业务取舍：让两个宿主共用业务编译规则可减少维护成本；不同宿主仍需来源/拓扑接入，不能以追求单一类文件抹掉这些职责。

### D6 Document仅消费定义，本次不决定版本迁移

只修改受共同参数/字段访问影响的现有SkillDocument适配；保持实施基线的kind、typed properties、values、逻辑端点、owner和完整包生命周期。当前基线为v7；原生FSM change正式切换后的唯一协议若成为新基线，则按该合同消费，不能恢复旧版。Parser、Exporter与Apply使用共同定义，不增加Agent业务模型、节点MCP工具或第二事务。

如果某个字段无法在既有公开形状下无损映射，报告精确冲突，不能在本change自动新增字段、owner或升级版本。原生FSM的公开合同迁移由integrate-native-fsm-skill-authoring负责；本change不预定迁移实现，也不让两种协议并行读取。

业务取舍：保持包形状让这次改造聚焦业务定义去重；不能通过放宽parser或双读旧字段掩盖无法映射的问题。

### D7 参数存储迁移与完成标准

若字段从节点壳移动到共同参数对象，需要迁移的是同一节点实例的存储位置。旧字段仍可读取时按精确资产封存参数、UID、端点和引用，确认差异后通过既有正式资产Mutation迁移。保留原值、连接与owner，成功后删除重复字段和一次性转换代码；失败恢复原资产，不覆盖用户未提交修改。

迁移不允许正常入口长期双读或双写。迁移前后通过正式结构校验与编译检查核对业务一致性。所有公开同义节点都必须交付“共同定义位置、原适配、FlowCanvas适配、删除项”的源码路径，Agent往返正常不能代替该证据。

### D8 现有文档职责重新归位

| 文档 | 本change承接 | 原文档继续负责 |
|---|---|---|
| integrate-native-fsm-skill-authoring（接收旧FlowCanvas作者任务） | 原节点/FlowCanvas重复定义、字段与端口投影、必要消费代码；对应本tasks 1—6 | 原生FSM、最终v8/闭包迁移和作者事务；通用观察归finish-skill-runtime-observation，网络/正式运行归integrate-corin-dump-authoring-replay |
| refactor-agent-authoring-attribute-driven | Skill参数定义变化引起的Document消费适配；对应本tasks 5 | 已完成的Agent清理、非Skill领域、现行v7与通用事务证据 |
| add-skill-transfer-connections | 只消费最终共同定义，双方接口变更须对账 | 保留既有Step/Edge阶段成果；4.4、5.1、5.3、6.1、6.3对接integrate-native-fsm-skill-authoring的最终FSM验证/清理，不再转交本change或重复实现旧目标 |
| refactor-btsmtl-authoring-architecture | 同义节点业务编译输入和支持集对照；对应本tasks 1.1/4.1/4.2 | 其他领域编译、runtime、产品装配与总重构 |
| Pose、Scene Play、Foot、AI等 | 不转入新任务 | 各自业务职责 |

原已完成记录按当时版本保留。撤销的是此前扩大范围的任务交接，不是撤销已正确的代码。转移专项原来的错误顺序/owner或迁移缺口继续明确登记在原专项中，不因本次收窄被标成完成。

### D9 与现行规范的对账

| 规范或决定 | 本次处理 |
|---|---|
| graph-authoring-domain-framework的唯一metadata要求 | 补充原业务节点和FlowCanvas必须共用业务定义；完整保留原Scenario |
| btsmtl-agent-authoring-document-sync的完整闭包和版本规则 | 仅补共同定义消费要求，不自行升级版本；删除上一稿v8/edge owner/order delta，由integrate-native-fsm-skill-authoring负责最终协议迁移 |
| btsmtl-graph-core与原FlowCanvas change的Skill适用范围 | 保持现有图存储选择，不复制或提前安装原change的拓扑范围delta |
| 原转移专项的“条件在Edge、普通步骤保留” | 不改变其业务决定；本次不迁移步骤、端口或顺序，其自身仍需完成公开合同对账 |
| 内部实现变化不得自动升级schema | 本次严格遵守；不能把Agent工作包版本升级当作参数抽取前置条件 |
| 原节点与FlowCanvas现有规则差异 | 共同定义承接已正确的业务要求；无法确认的冲突逐项报告，不能用文档默选一侧 |

本提案只修改delta，现行spec正文不提前宣称已完成。实施后按最新current逐条合并，保留其它change新增的场景。

## Risks / Trade-offs

- [字段移动造成资产反序列化丢值] → 先封存精确实例并验证映射，再迁移和删除旧字段。
- [两个同名节点实际上行为不同] → 逐对列出单位、作用域、生命周期和结果差异，只合并已确认同义的部分。
- [只共用接口却仍有两份规则] → 验收共同定义及两侧调用路径，扫描重复字段、默认值和校验，不能只看类继承。
- [宿主映射变成另一份业务定义] → 映射只处理明确身份/端口对象与事件，业务默认值和约束只在业务模块。
- [已有转移中间态阻塞相关编译] → 记录精确阻塞并交由转移专项处理，无关节点继续；不得绕过当前系统或宣称验证完成。

## Migration Plan

1. 完成原节点与FlowCanvas逐对清单，固定正式消费者和资产基线。
2. 从原有效规则抽共同定义，接入对应原节点与FlowCanvas节点，逐业务族小步提交。
3. 接通受影响的Inspector、目录、引用读取与编译消费，保持外部身份和图存储。
4. 通过既有正式资产入口迁移参数存储，失败回滚；重新读取并核对值、UID、连接和引用。
5. 删除重复声明、无消费者旧适配及一次性迁移代码，完成正式编译/结构检查和当前Console核对。
6. 同步本change的delta及文档交接，不把转移、版本升级或其它运行专项完成度算入本次。
