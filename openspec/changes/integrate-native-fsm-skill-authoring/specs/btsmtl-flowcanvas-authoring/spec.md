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

原生Entry/Prime、Any、Exit MUST分别表达入口路由、任意状态路由和退出当前FSM调用，MUST不被当作Ability终态。状态退出阶段及内部回调由运行代码拥有，不要求作者配置OnExit清理或终态图。条件、priority、abortPolicy和同来源唯一order MUST只存于转移连接。原生无condition的OnFinish行为 MUST不得代替BTSMTL无条件边或state-root-completed条件；未登记的插件任务/栈调用拒绝，正式业务仍编译进Program，不启动插件runtime。

#### Scenario: 状态尚未完成但取消条件成立

- **WHEN** 恢复窗口内闪避输入、动作准入等正式取消条件成立
- **THEN** Ability规则与生命周期 MUST接受相应结束请求并停止执行内容，不等待插件状态完成
- **AND** 状态与Timeline模块 MUST自动处理内部退出和资源释放，不由作者OnExit决定结束结果

#### Scenario: 原生任务没有编译映射

- **WHEN** 创建、粘贴、代码生成或导出目标包含未登记ActionTask/ConditionTask
- **THEN** 正式能力校验 MUST拒绝该目标并定位实体
- **AND** MUST不通过插件Execute、Condition.Check、协程或GraphOwner补足执行

#### Scenario: FSM进入钩子与默认状态并存

- **WHEN** FSM具有整体进入业务与带条件的入口路由
- **THEN** 两者 MUST使用各自唯一正式引用和编译语义，OnFSMEnter不得作为状态转移端点
- **AND** Prime或入口路由 MUST不同时保存两份默认目标，也不得丢弃入口条件

### Requirement: Corin清理必须按真实消费者保留动作业务

Corin迁移 MUST删除Attack无消费Startup Branches、单步OnEnter Action Setup、Clear Directional Dodge Run Intent及对应图内意图声明和失去引用的私有空图。清理 MUST按全部Skill、Timeline、typed目标、provider和编译消费者处理，不按显示名或缺少Get节点判定无消费。Dodge已迁入Native FSM的根图 MUST不恢复Startup Branches或Setup/Clear；有效结束条件迁回Ability规则/生命周期，StateBody清理由代码承担。原窗口取消、目标及ControlModule跑步意图语义保持；仍被引用且未裁决归属的StopThreshold声明不得删除或用临时provider替代。

#### Scenario: 多条转移指向同一出口

- **WHEN** Attack状态的自然完成、恢复前段闪避取消、恢复后段移动取消均指向同一Exit
- **THEN** 系统 MUST保留每条转移identity、condition、priority、abortPolicy和order，只改善条件与目标摘要
- **AND** MUST不合并条件、复制Exit状态或移除动作准入及连段边

#### Scenario: Dodge迁移后保留及时退出语义

- **WHEN** Dodge根图已迁移为Native FSM并删除旧Setup/Parallel外壳
- **THEN** Timeline执行期间的原取消/中断/中止条件与停止语义 MUST保留
- **AND** MUST不恢复旧包装，也不得变成等待Timeline结束才响应取消

### Requirement: Ability生命周期必须统一决定执行结束

执行根完成、Ability取消规则、外部中断/替换和强制终止 MUST进入同一AbilityExecution生命周期处理，保留Complete/Cancel/Interrupt/Abort及原有效原因与请求竞争语义。接受的结束结果 MUST在停止尚运行内容前明确，退出阶段不能重写或反推结果；状态内部转换或单个Timeline完成不得自动等同整个Ability结束。终态与逻辑停止过程 MUST复用同一执行状态，不能增加第二运行链。

#### Scenario: 闪避替换攻击
- **WHEN** 闪避替换满足正式规则并中断攻击
- **THEN** 生命周期 MUST先接受攻击的Interrupt，再停止其逻辑执行；必要停止完成后允许新能力激活
- **AND** 状态退出回调不得重新选择Cancel/Complete，也不得只因旧动画仍淡出阻止新能力

#### Scenario: 连段切换内部状态
- **WHEN** Attack1正常转移到Attack2
- **THEN** 同一AbilityExecution MUST继续执行，状态代码处理原状态退出
- **AND** 不得因State.OnExit或Timeline局部结束就结束整个Ability

### Requirement: 状态退出清理必须由代码自动完成

状态、执行子图、Timeline及各模块拥有的窗口和运行资源 MUST由对应代码按生命周期自动停止/回收，不要求作者连接OnExit清理、动画释放或结束分派节点。内部OnExit与正式强制停止仍由代码处理，必要释放不能依赖普通回调一定执行。Dodge旧ActionExit的有效结束条件迁回Ability后，Selector、空规则、被替代Submit分支及空作者OnExit页 MUST删除，不能隐藏进新节点或子图。

#### Scenario: Ability没有作者OnExit图
- **WHEN** 正常完成、取消、中断或强制终止没有配置作者OnExit图的Ability
- **THEN** 对应运行模块 MUST完成其拥有内容的停止和释放，缺少作者清理图不能成为资源残留原因
- **AND** 不得自动生成空OnExit页补齐旧结构约束

#### Scenario: 退出时Ability结果已经确定
- **WHEN** Ability已接受Cancel或Interrupt，状态层随后收到统一停止上下文
- **THEN** 原终态 MUST保持；内部回调需要原因时读取既定执行上下文
- **AND** 不得从TreeParentStop等状态退出原因重新猜测或提交另一个终态

### Requirement: Timeline片段混合与跨动作过渡必须各有唯一owner

Timeline动画轨道/片段 MUST拥有素材、时间区间、片段重叠及WeightCurve/EaseIn/EaseOut等局部混合。不同动作播放之间的接替、动作退出到基础姿态 MUST由对应动作Slot的正式转移规则处理；基础姿态之间的切换属于Pose动画状态机。各处 MUST不重复保存或执行同一次交接的时长/曲线。中途打断 MUST从当前采样和混合结果接替，不跳到旧片段末尾冒充EaseOut。

#### Scenario: 同一Timeline内两个片段重叠
- **WHEN** 作者在Timeline内安排片段A/B的重叠区间和局部权重
- **THEN** 播放请求 MUST保留该时间与局部混合含义，不把内部片段衔接转为跨Ability的Slot规则
- **AND** 完整C#输出/生成 MUST恢复同一片段配置

#### Scenario: 攻击动画中途被闪避接替
- **WHEN** 攻击逻辑已中断而旧播放按配置淡出
- **THEN** 动作Slot MUST按正式源/目标转移规则接入新播放，旧播放仅保留表现所需状态
- **AND** 旧Timeline不得继续产生命中、效果或技能Motion，不以淡出时长代替玩法恢复时间

#### Scenario: 退出后没有接替动画
- **WHEN** 动作贡献淡出且没有新动作播放
- **THEN** PoseGraph MUST继续输出基础姿态并在贡献消失后自然显露
- **AND** 不要求作者在OnExit硬播Idle或显式释放播放

#### Scenario: 完整导出与生成Ability
- **WHEN** C#作者工具输出并生成含动画播放的Ability
- **THEN** 自有Timeline的局部混合和明确Slot/表现引用 MUST完整保留，Slot过渡与Pose配置保留其原owner，停止与释放由原模块自动执行
- **AND** 输出不得重新创建Action Exit Selector、空规则或作者OnExit清理链

### Requirement: 动作混合历史必须由对应动画模块维护

连续接替时必要的旧来源、权重历史、容量和释放 MUST由动作Slot自身的混合实现管理，不能由Ability、State.OnExit或EventGraph维护第二份栈。Pose分支确有多来源历史时可使用其显式BlendStack，但不得因存在动作Slot自动新增全局BlendStack或重复管理同一动作历史。源/目标过渡 MUST使用该节点正式规则。

#### Scenario: A到B尚未混完又接入C
- **WHEN** 动作A/B正在混合时收到C的正式播放请求
- **THEN** 混合模块 MUST保留当前输出所需历史并接入C，不清空后从完整B姿态重新开始
- **AND** 旧来源无贡献后由动画代码回收，不继续执行已停止的Ability逻辑

### Requirement: 动画EventGraph与动作播放请求必须保持不同的数据路径

动画EventGraph MUST从Gameplay原始事实计算动画专用变量，供PoseGraph/基础动画状态机消费。Ability Timeline的正式播放、时间、片段权重和停止请求 MUST沿既有Presentation接口直接进入动作Slot，不强制先经EventGraph再次仲裁或转发。EventGraph不得因此获得结束Ability、反写Gameplay或手工推进混合栈的职责；BTSMTL也不得接管最终骨骼采样和Pose组合。

#### Scenario: 技能播放和基础姿态同时工作
- **WHEN** 动画EventGraph更新走跑变量，同时Ability请求播放攻击动画
- **THEN** 基础姿态 MUST由动画状态机按变量决定，攻击播放请求由Slot接入并组合
- **AND** 不需要EventGraph另接开始/停止事件才能释放该技能播放，也不创建第二播放器

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

Skill Timeline MUST能够表达动作AnimationTrack、AnimationClip、指定动作Slot、片段重叠/局部权重以及命中窗口、取消和完成时序。Timeline MUST输出稳定Animation Producer/Playback、时间、局部权重和自动停止通知；跨动作接替由Slot正式规则处理，基础Locomotion转移、Layer、IK和Output Pose由Presentation/PoseGraph完成。Skill不得直接写最终Pose，也不得通过作者OnExit补齐播放释放。

#### Scenario: 技能播放动作
- **WHEN** GA式Skill进入动作Timeline
- **THEN** Program MUST按Timeline发出动作表现请求并推进其Gameplay完成/取消流程，不隐含等待纯表现混出结束
- **AND** Presentation MUST依据该请求完成最终动画组合

### Requirement: Skill Program必须进入可替换的Simulation Pipeline

正式Skill Program MUST通过Session Composition进入可校验的Simulation Pipeline。Pipeline MUST区分Ingress、Schedule、Step和Egress，并验证Program、Backend、World Solver、Snapshot和Pass Contract兼容性。网络实现 MAY替换Session Source和Pipeline Adapter，但 MUST不改变Skill的执行语义。

#### Scenario: 使用Rollback或Server Authority运行同一Skill
- **WHEN** 同一Character Program被不同正式Session Pipeline加载
- **THEN** Skill语义、ActionInstance身份和状态恢复合同 MUST保持一致
- **AND** 网络层 MUST同步输入、权威状态、Hash或Snapshot，不得复制作者Graph或最终Pose
