## Purpose

定义BTSMTL技能统一作者图的可见行为、合法节点与端口、参数化子图所有权、直接编译和资产迁移合同，使作者只编辑一份正式图，运行始终消费经校验的编译产物。

## ADDED Requirements

### Requirement: BTSMTL技能必须只有一份正式作者拓扑

系统 MUST以统一原生图编辑能力管理技能的正式节点、端口及连接。编辑、保存、文档导出和编译 MUST读取同一份拓扑；MUST不建立可写镜像图或通过旧作者图转换后执行。共享编辑基础 MUST不混淆不同领域的业务语义。

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

每个技能根 MUST保存为独立FlowGraph主资产，CharacterPipelineDefinition MUST只引用该技能根，MUST不把技能根保存为Definition子资产。技能根拥有的私有页面、Macro与Timeline MUST保存为同一技能文件内的子资产；共享Macro所拥有的私有内容 MUST归属该共享Macro文件。私有内容的保存、复制与回收 MUST依据实际文件owner，不得回退使用Definition作为私有内容容器。

新建私有子图 MUST由当前根自动拥有，作者无需先创建独立资产。共享子图 MUST显式引用并显示共享属性。删除调用 MUST不删除仍被引用的共享定义；私有闭包回收、复制和保存 MUST归属根事务。MUST不同时保存私有子图的内联副本与资产副本。

#### Scenario: 新建技能根与私有内容
- **WHEN** 作者创建技能根及其私有Macro或Timeline
- **THEN** 技能根 MUST成为独立主资产，私有内容 MUST保存在该技能文件中
- **AND** Definition MUST只保存根引用，不持有这些私有子资产

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

### Requirement: 迁移必须原子替换正式闭包并清理旧入口

迁移 MUST按精确根输出实体及依赖变更计划，保留可保留的稳定身份，明确报告冲突；应用 MUST覆盖真实资产、私有子图和引用的完整事务。成功后仅新作者模型可从正式入口编辑；无消费者旧代码和转换路径 MUST删除。失败 MUST完整回滚，不得发布半迁移角色或增加兼容开关。

#### Scenario: 子图迁移保存失败
- **WHEN** 根及部分子图迁移后任一保存失败
- **THEN** 事务 MUST恢复迁移前完整资产闭包，正式产物 MUST不采用半迁移内容

### Requirement: 每个GA式技能必须只有一个正式入口图

每个正式Skill MUST由一个稳定Skill identity和一个Entry Graph组成。Skill外壳 MUST保存激活、目标、Action Context、替换和后续关系；执行Graph MUST承载节点、Macro、State、Condition和Timeline闭包。系统 MUST拒绝同一Skill的多个执行根、隐藏入口或从旧Character RootTree推断入口。

#### Scenario: 激活技能
- **WHEN** 输入或正式控制规则请求一个Skill
- **THEN** 系统 MUST通过该Skill唯一Entry Graph创建一次正式激活
- **AND** 编译、网络、回滚和观察 MUST使用同一Skill root identity

### Requirement: Skill变量必须按正式provider和生命周期访问

技能作者面板 MUST区分Character State、Ability Attribute、GameplayTag、Input/TargetData、Skill Local Blackboard、State、ActionInstance和Frame Fact。每个变量引用 MUST包含owner、稳定声明ID、类型、读写权限和生命周期。Skill MUST不通过名字、反射、路径扫描或某个Skill Graph的隐式共享变量访问其他provider。

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

