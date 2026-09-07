## MODIFIED Requirements

### Requirement: Character authoring 必须按显式 Numeric Target 生成 Simulation Program

Character Build MUST按显式Numeric Target生成角色静态运行包，包含控制模块binding、技能Program目录、ActionProfile、GE／Equipment／Body Motion描述和组合状态布局。角色级控制流程 MUST由匹配的代码模块执行；Tree／Timeline／局部状态机只作为技能内容编译。不同Target MUST生成独立Program／State ABI，不得通过网络模型推断或在运行中切换。

#### Scenario: 构建Fixed技能组合

- **WHEN** 角色配置显式选择Fixed Target
- **THEN** MUST生成包含完整控制合同和Fixed技能目录的角色Program
- **AND** MUST不生成可执行角色RootTree或加载Float32运行数据

#### Scenario: 编译 Corin Float32 Program

- **WHEN** 作者编译 Corin CharacterPipelineDefinition
- **THEN** Frontend MUST先发布一份 validated Semantic IR artifact，Float32 Target MUST只从该 artifact 生成 Program
- **AND** Runtime MUST不递归 clone RootTree、StateMachine 或 Timeline graph

#### Scenario: Target 收到未校验的内存 IR

- **WHEN** 调用方尝试绕过 artifact codec，把任意 `CharacterGameplaySemanticIr` 对象直接交给正式 Target 入口
- **THEN** 编译 API MUST不提供该公共路径
- **AND** MUST不因对象来自当前 Editor 进程就视为合法 build input


### Requirement: Program 必须是不可变 portable 数据

CharacterSimulationProgram MUST只包含稳定 identity、SemanticHash、typed operation/data table、Character state layout、portable catalog、source map、required world capability manifest、NumericProfile、operation-set version、Target ABI、ProgramHash与 LayoutHash。Target Compiler MUST将 Program编码为独立 canonical `.csim` artifact；该 artifact MUST不包含 UnityEngine.Object、GameObject、AnimationClip、Animancer state、Pipeline Definition、Pass、Execution Backend、Session Source、Endpoint、Transport、Network Model或 mutable World state，并 MUST可由 Unity与普通 .NET Host使用同一 canonical codec读取。

#### Scenario: 纯 CSharp 加载 Program

- **WHEN** 普通 .NET Host加载 Float32 `.csim` bytes
- **THEN** MUST不需要 UnityEngine、ScriptableObject、CharacterPipelineDefinition或 Pipeline asset才可解析 Program
- **AND** MUST得到与 Unity ProgramAsset相同的 ProgramHash与 LayoutHash
#### Scenario: Program引用代码模块

- **WHEN** 角色运行包声明C#控制实现
- **THEN** Program MUST只保存稳定binding、版本与数据合同
- **AND** MUST不序列化Type、delegate、模块对象或可变技能实例


### Requirement: Authoring type 必须通过唯一 Emitter 生成 Operation

每个可执行 Node、Module、Track 和 Clip authoring type MUST在 Compiler Frontend registry 中对应唯一 emitter。一个 Emitter MAY生成多个 Semantic IR operation 或引用共享 catalog entry，但每个 operation MUST声明 source map、state declaration、input、world request 和 output。Emitter MUST不按 Local、ServerAuthoritative、Rollback 或 Numeric Target 生成不同业务规则；Target Compiler 只负责 lowering 与 capability validation。

#### Scenario: 缺少 Emitter

- **WHEN** 可达 authoring source 包含没有 Emitter 的可执行类型
- **THEN** Program build MUST失败并报告精确 source identity
- **AND** MUST不回退到 authoring node 虚方法执行
#### Scenario: 控制配置进入构建

- **WHEN** 角色Definition引用代码控制模块
- **THEN** 构建 MUST记录模块合同并只对技能作者节点发射operation
- **AND** MUST不为代码控制流程建立假RootTree节点


### Requirement: Program 必须声明完整 Character State Layout

角色运行包 MUST声明控制模块状态、ActionInstance与其技能执行状态、技能调用作用域、GE和Equipment的完整typed布局、默认值及schema identity。每个Skill Program MUST声明本模板节点／Timeline／子图所需状态，实例地址 MUST结合ActionInstance、调用点和generation确定。跨Tick状态不得以Unity对象、任意object字典或未声明私有字段保存；transient请求、计算scratch与活跃transaction不得进入committed snapshot。目标不支持的字段类型 MUST在构建时失败，不得降为任意Bytes槽。

#### Scenario: 构建并发技能状态

- **WHEN** 同一Skill Program允许多个释放实例
- **THEN** 布局 MUST共享只读模板并支持独立实例和子图frame

#### Scenario: 控制模块遗漏状态声明

- **WHEN** 控制模块请求存储未声明或Target不支持的数据
- **THEN** 构建或正式状态访问 MUST失败并定位模块／字段
- **AND** MUST不保存对象镜像或任意payload绕过布局

#### Scenario: 构建MotionWarp技能

- **WHEN** 技能包含跨Tick累计Warp状态
- **THEN** 布局 MUST保留恢复继续执行需要的完整typed状态
- **AND** 当前Tick运动请求与求解临时数据 MUST保持transient

#### Scenario: 检查有状态 Operation

- **WHEN** Wait、StateMachine、Timeline、Input request、Action或GameplayEffect operation影响后续Tick
- **THEN** 其可变数据 MUST存入已声明typed Character state address
- **AND** operation object MUST保持不可变

#### Scenario: 检查同Step Motion transient

- **WHEN** Motion operation只为当前WorldSolve产生contribution与WorldRequest
- **THEN** Program State Layout MUST不声明MotionAccumulator或PendingWorldRequest committed slot
- **AND** Snapshot与StateHash MUST不包含该transient

#### Scenario: 编译未知复杂状态

- **WHEN** Target lowering需要保存没有正式typed value kind与canonical codec的领域状态
- **THEN** Target build MUST失败并指出source operation/state declaration
- **AND** MUST不回退为Bytes StateSlot


### Requirement: Program bytes 与 ProgramHash 必须稳定

相同控制模块语义／数据合同、技能依赖闭包、SemanticHash、compiler version、operation-set version、NumericProfile、required world capability 和 TickRate MUST产生相同 canonical bytes 与 ProgramHash。Traversal、operation、constant、scope 和 catalog MUST使用稳定 identity/order，MUST不依赖 Unity instance id、display name 或无序集合迭代。不同 NumericProfile MUST产生不同 ProgramHash。

#### Scenario: 重复编译未修改资产

- **WHEN** 相同 source revision 被重复编译
- **THEN** ProgramHash MUST保持不变
#### Scenario: 仅角色规则版本变化

- **WHEN** 技能作者内容不变但控制模块语义版本改变
- **THEN** 角色运行包identity MUST改变，旧组合不得被当作同一版本


### Requirement: Program Artifact 必须与 Source Revision 严格对齐

正式 Target Program artifact MUST记录 compiler version、operation-set version、source revision、SemanticHash、TickRate、NumericProfile、Target ABI、ProgramId、ProgramHash、LayoutHash、控制模块binding／状态schema、技能目录身份与 capability manifest。Unity ProgramAsset MUST只包装经过正式 store重读校验的 exact `.csim` bytes与轻量 metadata。Host MUST在 artifact stale、Program缺失、Target ABI不匹配、ProgramAsset metadata不匹配或 required capability不满足时创建失败，MUST不在运行时重新编译、读取 `.csir`、重新编码 Program或使用旧 interpreter。

#### Scenario: Authoring 已修改但 Program 未重建

- **WHEN** Host检测到 source revision与 Program artifact不同
- **THEN** Host MUST拒绝创建 Session并报告 stale source
- **AND** MUST不从 ProgramAsset metadata、旧 `.csim`或 `.csir`选择近似匹配结果


### Requirement: Session ProgramCatalog 必须不可变且支持每 Actor 显式绑定

系统 MUST以 `SimulationProgramCatalog`保存一个 Session可执行的有序 Program集，并以 ProgramId、ProgramHash、LayoutHash、SemanticHash、NumericProfile、operation-set version与 capability manifest计算稳定 CatalogHash。Catalog内全部 Program MUST使用相同 TickRate、NumericProfile与 ABI version，并将 required world capabilities合并为 Session requirement union。每个 Actor roster entry MUST显式绑定 Catalog中唯一 ProgramId；compiled Pipeline runtime MUST不假定全部 Actor使用同一角色 Program，也 MUST不在运行中热替换 Program、切换 NumericProfile或迁移 layout。

#### Scenario: Corin 与另一角色共享 Session

- **WHEN** Session roster 的 ActorA 与 ActorB 绑定不同 ProgramId
- **THEN** compiled Pipeline runtime MUST按各自 binding选择 Program执行
- **AND** 两者 MUST 仍进入同一个 WorldSolver batch

#### Scenario: Actor 引用未知 Program

- **WHEN** roster entry 的 ProgramId 不存在于启动时 Catalog
- **THEN** Session 创建 MUST 失败
- **AND** MUST 不回退默认 Corin Program 或按 Program 名称查找

#### Scenario: Catalog 混入不同 Numeric Target

- **WHEN** 两个 Program 的 TickRate、NumericProfile 或 target ABI version 不一致
- **THEN** Catalog 创建 MUST失败并报告两个 Program identity
- **AND** WorldSolver MUST不接收混合格式 batch

#### Scenario: 相同 Authoring 生成 Float 与 Fixed Program

- **WHEN** 同一 Corin Semantic IR 生成 Float32 Program 与 FixedQ32.32 Program
- **THEN** 两者 MUST共享 SemanticHash 与 source identity
- **AND** 两者 MUST具有不同 ProgramHash，且 Snapshot MUST不可互换

#### Scenario: Solver 不满足某个 Program

- **WHEN** Catalog capability union 包含当前 WorldSolver 未声明的能力
- **THEN** Session composition MUST失败
- **AND** MUST不只按第一个 Actor 的 Program 检查能力

#### Scenario: 运行中 authoring 重新编译

- **WHEN** 已运行 Session 对应 authoring 生成了新 ProgramHash
- **THEN** 当前 Session MUST 继续保持原 Catalog 或明确停止并重建
- **AND** MUST 不热替换 Program bytes 或迁移现有 Character state
#### Scenario: 模块与技能目录混版

- **WHEN** Actor绑定的控制实现不匹配其Program中模块合同或技能目录
- **THEN** Session MUST在Active前失败
- **AND** MUST不只核对角色外壳ProgramId


### Requirement: Program 与 Projection 必须在同一 Build Transaction 中发布

Character Simulation Build MUST按`control-module contract and skill closure -> Frontend artifact -> Presentation contract -> resolve exact Animation Analysis artifacts -> independently compile Presentation Projection and requested Numeric Target Programs -> cross-artifact identity validation -> atomic publish`执行。ProjectionRevision MUST由Projection schema、Presentation ContractHash、Presentation authoring dependency与Analysis artifact identity/content hash规范计算，MUST不包含任一Target ProgramHash、NumericProfile或ABI。单clip artifact MAY在该事务之前独立生成，但Build MUST重新校验其完整identity和payload hash。控制模块发布身份、Semantic IR cache、Projection、全部请求Target的技能目录与canonical artifact、Unity wrapper与generated reference MUST先完成stage和exact重读，再作为一个发布组提交；任一artifact、Target或Projection阶段失败 MUST恢复完整旧发布组，不得更新一半generated reference。

#### Scenario: Ready artifact被复用

- **WHEN** Build发现全部artifact Ready且精确匹配
- **THEN** Build MAY跳过AnimationClip重新采样
- **AND** Projection、请求Target Program发布事务和最终contract校验 MUST仍完整执行

#### Scenario: Artifact损坏

- **WHEN** 任一artifact存在但codec或hash校验失败
- **THEN** Build MUST失败并定位对应stable clip binding
- **AND** MUST不使用旧Projection或默认feature继续发布

#### Scenario: Fixed-only产品构建

- **WHEN** Product Build显式只请求Fixed Numeric Target
- **THEN** Build MUST从同一Frontend artifact生成唯一Projection与Fixed Program并验证相同Presentation contract
- **AND** MUST不生成Float32 Program作为Projection编译的隐藏前置产物


### Requirement: Program 必须声明Body Motion descriptor与能力身份

Target Program MUST保存由Definition正式Profile降低得到的Body Motion descriptor，包括GravityAcceleration、MaximumFallSpeed与semantic version，并 MUST将其纳入canonical bytes、ProgramHash、source revision和required world capabilities。Float32与Fixed Program MUST从同一numeric-neutral descriptor产生各自Target数值payload。Runtime MUST不读取authoring Profile、Scene默认或Network Model配置补齐descriptor；旧ABI或缺失descriptor的artifact MUST被拒绝。本次控制与技能布局变化 MUST升级对应Program／State ABI；发布清单 MUST明确记录实际版本，并拒绝迁移前的版本。

#### Scenario: Fixed Program降低Body Motion配置

- **WHEN** Compiler从同一Semantic IR生成Fixed Program
- **THEN** GravityAcceleration与MaximumFallSpeed MUST按Fixed Target规则降低
- **AND** Program MUST要求AirborneVerticalMotion
- **AND** descriptor或semantic version变化 MUST形成新的Program identity
- **AND** Program MUST不为resolved channel或最终request分配跨Tickslot


### Requirement: Compiled Program必须包含不可变Equipment catalog和layout

角色运行包 MUST包含canonical Slot、Route、Equipment、Feature、typed参数、Action／Skill binding、代码控制binding、Tag／Effect贡献、Presentation requirement与Initial Loadout。装备聚合、pending change、Feature控制状态和Action Equipment Context MUST进入typed布局及identity。旧Persistent／Route graph entry和Host控制opcode MUST删除，Runtime不得读取Unity作者资产补建。

#### Scenario: 初始化装备技能目录

- **WHEN** 角色运行包包含多个Feature与技能binding
- **THEN** 初始化 MUST一次解析Slot／Route／Feature／Skill及代码模块索引
- **AND** Tick MUST不重新扫描或加载Feature资产

#### Scenario: 装备内容hash不匹配

- **WHEN** catalog bytes与完整Program identity不一致
- **THEN** 加载 MUST拒绝，不得只重算子表继续运行

#### Scenario: Program加载装备catalog

- **WHEN** Runtime创建Corin ProgramCatalog
- **THEN** MUST一次构建Equipment lookup和entry layout
- **AND** 每Tick MUST不重扫Feature、Graph或Action列表

#### Scenario: Equipment catalog bytes被修改

- **WHEN** canonical equipment bytes与ProgramHash不匹配
- **THEN** Program load MUST拒绝
- **AND** MUST不只重算Equipment子表继续运行


### Requirement: Program identity必须覆盖Equipment authoring真相

SourceRevision、SemanticHash、ProgramHash与LayoutHash MUST按各自现行职责覆盖Equipment Profile及全部引用Feature控制合同、Skill定义／子图／Timeline、parameter、Tag／Effect和Presentation requirement。相同source在Float32与Fixed MAY具有不同ProgramHash/LayoutHash，但 MUST具有可核对的同一SemanticHash；不同Equipment catalog的Program snapshot MUST不可交换。

#### Scenario: 只修改武器参数

- **WHEN** 作者修改Sawblade MotionScale
- **THEN** SourceRevision、SemanticHash与目标ProgramHash MUST改变
- **AND** 旧generated Program MUST被判定过期

#### Scenario: 只修改Unity Prefab视觉资源

- **WHEN** SpawnedVisualAsset引用或binding pose改变
- **THEN** Presentation Projection identity MUST改变
- **AND** 若Program只保存稳定VisualBindingId，Gameplay SemanticHash MUST不因Unity表现内容无意义改变


### Requirement: Program Execution Layout必须预构建Equipment索引

Program runtime initialization MUST按Program一次构建Slot/Route/Equipment/Feature/Parameter/代码binding/Skill entry和state address索引，并验证引用闭包。Actor/Tick热路径 MUST使用稳定index或typed handle，不得执行LINQ catalog重建、字符串查找、AssetDatabase访问或Feature list排序。

#### Scenario: 每Tick解析PrimaryAction

- **WHEN** 代码控制模块解析MainWeapon PrimaryAction
- **THEN** MUST通过预构建Slot/Route/Feature index定位entry
- **AND** MUST不分配临时集合或按字符串扫描

#### Scenario: 初始化发现悬空entry

- **WHEN** Route catalog引用不存在的Skill entry或代码binding
- **THEN** Program execution layout build MUST失败
- **AND** Session MUST不进入Active
