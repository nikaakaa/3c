## Purpose

定义BTSMTL技能统一作者图的可见行为、合法节点与端口、参数化子图所有权、直接编译和资产迁移合同，使作者只编辑一份正式图，运行始终消费经校验的编译产物。

## ADDED Requirements

### Requirement: BTSMTL技能必须只有一份正式作者拓扑

系统 MUST以统一原生图编辑能力管理技能的正式节点、端口及连接。编辑、保存、显式C#导出和编译 MUST读取当前正式资产拓扑；已导出C# MUST能够通过同一领域API重建其明确范围，不建立另一份可写图模型或通过旧作者图转换后执行。共享编辑基础 MUST不混淆不同领域的业务语义。

#### Scenario: 修改并构建技能图
- **WHEN** 作者修改一个技能节点及其连接并显式构建
- **THEN** 编译 MUST直接消费该正式图的业务含义并生成唯一运行产物
- **AND** MUST不创建另一份旧作者图参与编译

### Requirement: Skill Graph作者交互必须使用FlowCanvas原生表面

Skill Graph MUST以FlowCanvas原生`GraphEditor`作为唯一作者UI宿主。画布、Toolbar、Blackboard、节点与连线Inspector、创建菜单、变量拖拽、selection、clipboard、Undo和Graph下钻 MUST继续使用原生交互；Skill domain adapter只提供provider目录、typed payload、业务命令和只读诊断，不得创建第二个Skill画布、Blackboard、Inspector或旁路编辑面板。

#### Scenario: 在原生Blackboard中使用Skill provider
- **WHEN** 作者打开Skill Graph并查看Skill Local或外部provider
- **THEN** 原生GraphEditor的Blackboard/Inspector表面 MUST显示当前Skill Local声明及可引用的Character State、Ability Attribute、GameplayTag、Input/TargetData和Frame Fact
- **AND** provider引用、变量创建、Get/Set和外部节点创建 MUST进入同一Capability、Mutation和Undo

#### Scenario: Skill Graph不替换原生面板
- **WHEN** Skill Graph绑定Skill domain adapter
- **THEN** adapter MUST扩展原生GraphEditor的面板和菜单能力
- **AND** MUST不通过自定义UI Toolkit右栏、`SetDomainPanel`或等价旁路使原生Blackboard/Inspector失效

#### Scenario: Timeline保持Montage式动作边界
- **WHEN** 作者从Skill Graph打开Skill Timeline
- **THEN** Timeline MUST由原生Timeline编辑器维护Track、Clip、Action Slot和动作窗口
- **AND** Timeline MUST只向Program/Presentation发出播放与窗口合同，不得直接混合Locomotion、IK或最终Pose

### Requirement: Skill状态机必须以原生FSM作为唯一作者资产

Skill的StateMachine子图 MUST由NodeCanvas原生FSM及正式FSMState/FSMConnection适配保存，普通Skill根、StateBody、ConditionRule、Macro和TimelineBody MUST保持FlowCanvas执行图职责。各图 MUST通过同一原生GraphEditor编辑和导航；同一个状态机 MUST不同时保存自定义FlowGraph、图外转移表或另一份可写投影。StateBody MUST由State唯一拥有，私有ConditionRule MUST由转移Edge唯一拥有，FSM MUST归属其正式调用owner和实际资产文件。

#### Scenario: 编辑攻击连段状态机

- **WHEN** 作者从Attack入口打开Attack Combo StateMachine并修改一条转移
- **THEN** 修改 MUST作用于原生FSM连接的唯一业务参数，C#导出和编译读取同一目标
- **AND** StateBody与Timeline MUST保持原调用关系，不重新生成旧状态图参与保存或编译

#### Scenario: 复制包含状态机的技能

- **WHEN** 作者复制含私有FSM、StateBody和Edge条件图的Skill
- **THEN** 副本 MUST拥有独立私有闭包并重映射稳定引用，共享资产仍显式引用原目标
- **AND** 全部owner MUST进入同一复制与Undo事务

### Requirement: 原生FSM能力必须遵守BTSMTL生命周期和转移语义

原生Entry/Prime、Any、Exit与整体OnFSMEnter/Exit MUST分别映射正式入口路由、任意状态路由、退出调用与FSM整体钩子，MUST不混同StateBody的OnEnter/Root/OnExit。条件、priority、abortPolicy和同来源唯一order MUST只存于转移连接。原生无condition的OnFinish行为 MUST不得代替BTSMTL无条件边或state-root-completed条件；未登记的原生任务、栈调用和执行方式 MUST拒绝。ActionList/ConditionTask只可通过正式typed定义与compiler binding表达业务，不能交给插件运行。

#### Scenario: 状态尚未完成但取消条件成立

- **WHEN** 恢复窗口内闪避输入、动作准入等正式取消条件成立
- **THEN** Program MUST按既有priority/order与中止合同选择转移，不等待插件状态完成
- **AND** MUST先停止源主体并完成其OnExit及Action/Timeline清理，再依既有合同进入目标或退出调用

#### Scenario: 原生任务没有编译映射

- **WHEN** 创建、粘贴、代码生成或导出目标包含未登记ActionTask/ConditionTask
- **THEN** 正式能力校验 MUST拒绝该目标并定位实体
- **AND** MUST不通过插件Execute、Condition.Check、协程或GraphOwner补足执行

#### Scenario: FSM进入钩子与默认状态并存

- **WHEN** FSM具有整体进入业务与带条件的入口路由
- **THEN** 两者 MUST使用各自唯一正式引用和编译语义，OnFSMEnter不得作为状态转移端点
- **AND** Prime或入口路由 MUST不同时保存两份默认目标，也不得丢弃入口条件

### Requirement: Corin清理必须按真实消费者保留动作业务

Corin迁移 MUST删除Attack无消费Startup Branches、单步OnEnter Action Setup、Clear Directional Dodge Run Intent及对应图内意图声明和失去引用的私有空图。清理 MUST先核对全部Skill、Timeline、typed目标、provider和编译消费者，不按显示名或缺少Get节点判定无消费。Dodge有效Body/Exit Monitor并行、动作窗口和目标快照 MUST保留；正式ControlModule跑步意图的设置/清理时机 MUST不因删除副本改变。StopThreshold消费者未完成正式归属裁决及迁移前 MUST不删除仍被引用的声明，也不新增临时共享provider。

#### Scenario: 多条转移指向同一出口

- **WHEN** Attack状态的自然完成、恢复前段闪避取消、恢复后段移动取消均指向同一Exit
- **THEN** 系统 MUST保留每条转移identity、condition、priority、abortPolicy和order，只改善条件与目标摘要
- **AND** MUST不合并条件、复制Exit状态或移除动作准入及连段边

#### Scenario: Dodge同时播放和监控退出

- **WHEN** 清理Dodge中的无消费Setup写入
- **THEN** Timeline Body和Exit Monitor MUST保持原并行运行与停止语义
- **AND** MUST不将整体替换成等待Timeline结束才检查退出的Sequence

### Requirement: 所有作者入口必须遵守同一能力与事务合同

节点创建、字段修改、连线、改接、粘贴、删除、子图接口修改和撤销 MUST采用同一领域能力和端口规则。合法操作 MUST进入真实资产owner的一次事务；非法操作 MUST在正式数据被部分写入前拒绝。跨领域粘贴、未经声明的类型转换及未提供编译合同的节点 MUST拒绝。

#### Scenario: 多个输入均与源类型相同
- **WHEN** 作者从输出端口创建具有多个同类型输入的节点
- **THEN** 编辑器 MUST要求明确目标输入
- **AND** MUST不默选第一个输入或隐藏插入转换

#### Scenario: 一次粘贴含非法节点
- **WHEN** 粘贴集合包含未登记能力或错误领域节点
- **THEN** 整次操作 MUST拒绝且不残留部分节点

### Requirement: 参数化子图必须复用原生接口和调用表达

技能参数化子图 MUST提供稳定输入输出身份、显式类型和正式调用节点，复用原生子图编辑与导航。显示名或排列变化 MUST不改变端口身份。只有具有完整领域语义及编译合同的嵌套能力可进入创建目录；本批 MUST拒绝递归调用、闭包环和跨领域调用。

#### Scenario: 重命名共享子图输入
- **WHEN** 作者只修改输入显示名称
- **THEN** 已有调用连接 MUST保持同一稳定端口

#### Scenario: 按单入口等待技能子图
- **WHEN** 父节点调用技能Macro
- **THEN** Macro MUST只有一个执行入口，可以声明多个值输入与输出
- **AND** 父节点 MUST等待子图主体完成后读取返回值及完成状态，停止请求沿现有技能中断协议传播
- **AND** 本批 MUST拒绝多执行入口、控制出口及由条件值读取启动执行Macro

#### Scenario: 编辑节点与Timeline组成的技能
- **WHEN** 作者编辑技能执行流程
- **THEN** 系统 MUST支持节点组织、Timeline等待及结束／中断的主链，不要求作者把技能改写为状态机

#### Scenario: 修改已使用的接口类型
- **WHEN** 子图接口变化导致发布闭包中的调用连接失效
- **THEN** 校验 MUST指出子图及具体调用位置，并拒绝不完整发布

### Requirement: 子图私有与共享所有权必须明确

每个完整技能 MUST以独立GameplayAbilityDefinition主资产作为作者入口，唯一AbilityGraph及其私有FSM、条件、Macro与Timeline MUST由该Ability拥有并保存于对应资产文件。CharacterPipelineDefinition MUST通过AbilityGrant精确引用Ability，MUST不再维护独立SkillDefinitions/SkillGraphs双重登记，也不得保存这些私有内容。共享Macro所拥有的私有内容 MUST归属该共享Macro文件；私有内容保存、复制与回收 MUST依据真实owner，不得通过第二根或镜像图绕过。

新建私有子图 MUST由当前根自动拥有，作者无需先创建独立资产。共享子图 MUST显式引用并显示共享属性。删除调用 MUST不删除仍被引用的共享定义；私有闭包回收、复制和保存 MUST归属根事务。MUST不同时保存私有子图的内联副本与资产副本。

#### Scenario: 新建技能根与私有内容
- **WHEN** 作者创建技能根及其私有Macro或Timeline
- **THEN** GameplayAbilityDefinition MUST成为独立主资产，执行图和私有内容 MUST由该Ability拥有
- **AND** 角色Definition MUST只保存授予引用，不持有技能内部子资产

#### Scenario: 私有内容指向错误文件
- **WHEN** 私有页面或Timeline的实际文件与其声明调用方的所属技能文件不一致
- **THEN** 所有权预检 MUST拒绝该修改并指出调用方与资产位置
- **AND** MUST不通过复制到Definition或建立第二份内容规避错误

#### Scenario: 复制包含私有子图的技能
- **WHEN** 作者复制完整技能定义
- **THEN** 复制结果 MUST拥有独立私有子图闭包，并保持显式共享引用

### Requirement: 编译运行必须保留领域执行语义

技能顺序、选择、并行、循环、局部状态机、Timeline完成与中断 MUST保持正式业务合同，不得按第三方同名节点推定等价。技能执行状态 MUST由动作实例及调用执行身份隔离；正式运行 MUST不解释作者图或启动作者框架的委托、协程和更新循环。

#### Scenario: 技能等待子图完成
- **WHEN** 顺序流程中的子图尚在执行Timeline
- **THEN** 后续步骤 MUST按照既有完成合同等待，不因调用端口返回就提前执行

#### Scenario: 同一子图被不同技能释放调用
- **WHEN** 两次技能释放使用相同共享子图
- **THEN** 时间、等待、循环及中断状态 MUST隔离，一次取消不得停止另一实例

### Requirement: FSM生成必须限定范围并复用原领域资产生命周期

迁移/生成 MUST明确根与拥有范围，保留业务identity、配置和顺序，显式恢复指定Definition/Skill根引用。资产替换、保存、清理与失败处理 MUST复用现有领域资产模块，MUST不新建整包同步事务、中央Validator或持久化对账计划。成功后仅正式原生作者模型参与编辑与编译；有效资产能力和调用者迁出后才删除无消费者旧协议。失败 MUST报告实际结果，不得宣称完整成功或发布半迁移产物，也不得增加兼容执行链。

#### Scenario: 子图迁移保存失败
- **WHEN** 根及部分子图迁移后任一保存失败
- **THEN** 原领域模块 MUST按其失败处理合同报告并处理本次明确输出，不误删范围外资源，正式产物 MUST不采用半迁移内容

### Requirement: FSM完整C#输出必须保留配置闭包与明确根绑定

公共显式export_code MUST从当前FSM与其正式拥有的StateBody、Condition、Macro等闭包读取全部节点类型、业务identity、参数、生命周期、条件、priority、abortPolicy、order、动态接口和布局。输出 MUST使用正式领域API，按创建、配置、引用、连接和根挂接组织；范围内共享对象只创建一次，范围外资源保持精确外部输入。MUST不经过旧Agent JSON/DTO或读取旧源码做增量合并。

#### Scenario: FSM包含环与共享条件引用

- **WHEN** 导出范围包含循环转移和多个引用同一内部对象的节点
- **THEN** 输出 MUST先建立所需对象再连接，内部对象只创建一次，各引用指向同一生成对象
- **AND** 输出排序 MUST不改变显式order、优先级、生命周期或取消行为

#### Scenario: 正式字段无法输出

- **WHEN** 某个原生配置、生命周期引用、参数或边没有完整读取及恢复方式
- **THEN** 完整导出 MUST明确失败并指出对象、字段和原因
- **AND** MUST不静默省略、调用插件runtime补足或报告部分源码为完整输出

### Requirement: FSM删除输出后重建必须保持业务身份和挂接

显式generate_assets MUST执行指定已编译正式入口，按明确范围创建/替换、挂接与保存，不依赖同批旧生成子资产GUID/local file ID读取内容。物理对象可以变化，状态/边/系统入口业务identity、条件、order、owner关系和指定Definition/Skill入口 MUST保持等价。源码、原始资源、范围外共享资产与未指定消费者 MUST不被自动清理或扫描修改。

#### Scenario: 删除生成资产后执行同一代码

- **WHEN** 在相同外部资源与正式API版本下删除已声明生成输出，再执行其完整创建代码
- **THEN** MUST重建等价FSM结构、配置、顺序、内部共享和布局，并恢复指定根挂接
- **AND** MUST不需要旧子资产GUID、协议local表或旧Graph镜像，也不能只创建孤立FSM

### Requirement: FSM人工编辑与代码生成必须显式分离

人工修改/保存FSM MUST不自动输出代码；只有显式export_code更新指定代码输出。generate_assets MUST不合并未导出的人工修改，不建立DocumentDirty/Conflict、rebase或源码同步生命周期。现有领域Undo/保存和失败报告 MUST保留，两个公共作者工具均不得自动Build；FSM不新增专属工具、中央Validator或整包事务。

#### Scenario: 人工编辑后重新生成

- **WHEN** 作者修改当前FSM但未显式导出，随后明确执行指定生成代码
- **THEN** 生成范围 MUST以该代码为准，不把未导出修改自动合并进源码或重建结果
- **AND** 人工保存本身 MUST不触发导出、生成或Build

### Requirement: GameplayAbilityDefinition必须是完整技能作者入口

每个GameplayAbilityDefinition MUST拥有稳定Ability identity、技能激活/阻断/取消规则、目标要求、已有消耗/冷却/效果引用、后续能力关系和唯一AbilityGraph。作者 MUST能从该入口编辑规则、执行图和资源；专用规则归Ability拥有，显式共享规则只有一个正式来源，不同时保存本地副本和外部覆盖。AbilityGraph MUST保留原生FSM及组合/Timeline业务，系统 MUST拒绝第二能力外壳、多个执行根或旧Character RootTree入口。此作者合同不要求新增尚不存在的效果或等级系统。

#### Scenario: 激活技能
- **WHEN** 输入或正式控制规则请求一个已授予Ability
- **THEN** 系统 MUST使用该Ability的正式激活规则和唯一AbilityGraph进入现有Program执行
- **AND** 编译、网络、回滚和观察 MUST保留对应稳定业务identity，不新增第二套运行实例

#### Scenario: 两个闪避能力共享规则
- **WHEN** DodgeBack与DodgeForward迁移为独立Ability并继续共享原激活策略
- **THEN** 规则 MUST只有一个明确来源，作者入口 MUST显示共享关系
- **AND** 原准入分组、并发上限、互斥和取消语义 MUST保持，规则identity不得误当Ability identity

### Requirement: Ability授予与一次执行必须各自归属

AbilityGrant MUST表示明确角色/装备授予的Ability引用、输入映射和已有授予参数，不复制技能规则或执行图。AbilityExecution MUST表示一次释放，AbilityExecutionContext MUST承载当前目标、来源和执行关联；二者 MUST复用现有对应状态，不另造Skill/Action并行状态。目标要求属于定义，实际目标属于执行。普通Ability节点及Timeline MUST绑定当前执行上下文，不要求作者创建空ActionContextSlot。合法非Skill上下文 MUST保留其业务边界。

#### Scenario: 相同Ability授予不同角色
- **WHEN** 两个角色引用同一Ability但使用不同输入映射
- **THEN** 输入绑定 MUST分别属于各自AbilityGrant，技能规则和执行图保持同一正式来源
- **AND** 两次实际释放 MUST各自拥有隔离的运行身份，授予记录不得被当作执行实例

### Requirement: 完整Ability代码生成必须包含外壳与执行内容

完整Ability导出 MUST以GameplayAbilityDefinition为根，覆盖定义字段、自有规则、唯一执行图及私有闭包，显式引用外部共享规则/效果/素材；生成 MUST恢复完整定义和声明范围内的角色授予/输入绑定。MUST不以只登记Graph、只恢复FSM或依赖旧SkillDefinition残留作为完整能力生成。子图范围 MUST只处理指定子图和已有owner，不克隆或覆盖能力外壳。公共工具继续为export_code/generate_assets，不新增Ability专用MCP。

#### Scenario: 角色尚无该Ability记录
- **WHEN** 完整生成目标明确包含Ability资产及对指定角色的授予，而角色尚未登记该能力
- **THEN** 生成 MUST创建完整Ability及内部内容，并恢复明确AbilityGrant和输入映射
- **AND** MUST不要求预先存在旧SkillDefinition/ActionProfile外壳或旧生成子资产GUID

#### Scenario: 只重建连段FSM
- **WHEN** 明确生成范围仅是某个Ability的连段状态机
- **THEN** 生成 MUST保持该Ability规则、授予和其它内容不变，恢复原子图owner引用
- **AND** MUST不新增第二Ability外壳，也不能将子图输出报告为完整Ability输出

### Requirement: Skill变量必须按正式provider和生命周期访问

技能作者面板 MUST区分Character State、Ability Attribute、GameplayTag、Input/TargetData、Ability Local Blackboard、State、AbilityExecution和Frame Fact。每个变量引用 MUST包含owner、稳定声明ID、类型、读写权限和生命周期。Ability MUST不通过名字、反射、路径扫描或某个图的隐式共享变量访问其他provider；正式Skill专属命名迁移后不得保留并行别名或第二份运行数据。

#### Scenario: 读取角色移动事实
- **WHEN** Skill读取速度、朝向或移动模式
- **THEN** 系统 MUST提供只读typed fact
- **AND** Skill MUST不直接写入Movement Runtime字段

#### Scenario: 修改能力数值
- **WHEN** Skill需要修改资源、层数或冷却等会受GameplayEffect影响的数值
- **THEN** 系统 MUST通过Ability Attribute/GameplayEffect合同写入
- **AND** MUST不把该数值复制成一个Skill Local Blackboard变量

#### Scenario: 创建技能私有变量
- **WHEN** 作者在当前Skill Graph创建变量
- **THEN** 变量 MUST属于当前Graph的Local Blackboard并拥有稳定声明ID
- **AND** 从其他provider拖入的变量 MUST只保存显式引用，不复制其正式值

### Requirement: Skill Timeline必须表达有限动作并输出表现合同

Skill Timeline MUST能够表达动作AnimationTrack、AnimationClip、Action Slot、进入/退出混合请求、命中窗口、取消和完成时序。Timeline MUST输出稳定的Animation Producer/Playback合同；最终Locomotion混合、Layer、IK和Output Pose MUST由Presentation/PoseGraph完成，Skill MUST不直接写最终Pose。

#### Scenario: 技能播放动作
- **WHEN** GA式Skill进入动作Timeline
- **THEN** Program MUST按Timeline发出动作表现请求并等待完成、混出或中断
- **AND** Presentation MUST依据该请求完成最终动画组合

### Requirement: Skill Program必须进入可替换的Simulation Pipeline

正式Skill Program MUST通过Session Composition进入可校验的Simulation Pipeline。Pipeline MUST区分Ingress、Schedule、Step和Egress，并验证Program、Backend、World Solver、Snapshot和Pass Contract兼容性。网络实现 MAY替换Session Source和Pipeline Adapter，但 MUST不改变Skill的执行语义。

#### Scenario: 使用Rollback或Server Authority运行同一Skill
- **WHEN** 同一Character Program被不同正式Session Pipeline加载
- **THEN** Skill语义、ActionInstance身份和状态恢复合同 MUST保持一致
- **AND** 网络层 MUST同步输入、权威状态、Hash或Snapshot，不得复制作者Graph或最终Pose
