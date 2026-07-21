# btsmtl-compiled-simulation-program Specification

## Purpose
定义 Character authoring 经 validated Semantic IR artifact 和显式 Numeric Target 生成不可变 portable Simulation Program、ProgramCatalog 与 Presentation Projection 的正式编译和发布边界。
## Requirements
### Requirement: Projection Foot Analysis必须拥有独立规范身份

Projection Foot Analysis identity MUST包含其所消费artifact的canonical content hash，以及AnimationClip、Analysis Source、Sampling Rig、Calibration、采样参数和算法版本的规范identity。Library路径与Editor-only Source/Sampling Rig对象 MUST不进入Runtime Projection payload。Projection stale detector MUST按expected artifact identity与content hash判断，不得按名称、path、duration或文件时间匹配。

#### Scenario: Artifact内容变化

- **WHEN** AnimationClip或Analysis输入变化并生成新artifact
- **THEN** ProjectionRevision MUST变化且旧Projection MUST变为Stale
- **AND** Gameplay ProgramHash MUST保持不变

### Requirement: Projection不得保存原始动画采样快照

Foot Analysis MUST以经过确定性key reduction的有限curve set保存。Projection MUST不保存每帧骨骼Transform快照、Sampling Rig Prefab实例、Playable状态或可编辑左右脚Window资产。相同输入重复Build MUST生成相同canonical feature payload和ProjectionRevision。

#### Scenario: 多个producer复用同一AnimationClip

- **WHEN** Build在同一事务内分析相同clip与Source组合
- **THEN** Editor MAY复用采样工作结果
- **AND** 发布结果 MUST仍按每个stable clip binding精确引用而不产生运行时字符串字典

### Requirement: Character authoring 必须按显式 Numeric Target 生成 Simulation Program

系统 MUST先以 CharacterPipelineDefinition 为唯一编译根运行 Character Semantic Frontend，生成经过 canonical 校验的 Gameplay Semantic IR artifact，再由显式 Numeric Target 生成 CharacterSimulationProgram。Target Compiler 的正式输入 MUST是 validated artifact，MUST不接收 CharacterPipelineDefinition、Graph、Node、Timeline、Unity object 或 Frontend 私有 discovered model。每个 target artifact MUST只包含一个 NumericProfile；同一 source MAY为不同 target 生成不同 Program，但 MUST不重新实现或改变 Semantic IR operation。Runtime MUST不直接从 authoring object 或 Semantic IR 创建 gameplay runtime clone。

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

### Requirement: Authoring type 必须通过唯一 Emitter 生成 Operation

每个可执行 Node、Module、Track 和 Clip authoring type MUST在 Compiler Frontend registry 中对应唯一 emitter。一个 Emitter MAY生成多个 Semantic IR operation 或引用共享 catalog entry，但每个 operation MUST声明 source map、state declaration、input、world request 和 output。Emitter MUST不按 Local、ServerAuthoritative、Rollback 或 Numeric Target 生成不同业务规则；Target Compiler 只负责 lowering 与 capability validation。

#### Scenario: 缺少 Emitter

- **WHEN** 可达 authoring source 包含没有 Emitter 的可执行类型
- **THEN** Program build MUST失败并报告精确 source identity
- **AND** MUST不回退到 authoring node 虚方法执行

### Requirement: Program 必须声明完整 Character State Layout

Program MUST为Runnable、StateMachine、Timeline、Blackboard、Input request、Action、GameplayEffect、RNG、counter和sequence中会影响当前Commit后或未来SimulationTick的数据分配明确、类型化的Character State Layout。每个StateSlot MUST声明稳定index、owner、semantic、typed value kind与default；Layout MUST不允许opaque Bytes kind。只服务同一Step的MotionContribution、MotionAccumulator、PendingWorldRequest、输出staging和State Transaction MUST由Target Evaluation/Pending产品拥有，不得进入committed Character State Layout。任何影响未来SimulationTick的Actor Gameplay数据 MUST不留在authoring object、operation、emitter或领域runtime隐藏字段内。Body/world/solver state MUST由独立WorldSimulationState layout拥有。

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

### Requirement: Program 必须声明唯一 Numeric Target ABI

Program manifest MUST声明 NumericProfile、scalar/vector ABI、operation-set version、rounding/overflow policy和 serialization version。所有 Gameplay constant MUST由 Target Compiler从 Semantic IR source literal转为该 target格式。Program MUST不保存 float/fixed双值，也 MUST不允许 Session Source、Pipeline或 Network Model在运行时切换 target。当前正式安装 Float32 与 FixedQ32.32 两个 Numeric Target；二者 MUST生成独立 artifact、Program/State ABI 与 Snapshot codec。

#### Scenario: Authoring 数值无法表达

- **WHEN** GameplayEffect magnitude 或 MotionCurve key 无法由当前 Numeric Target 合法表达
- **THEN** target lowering MUST失败并报告 source identity、原值、NumericProfile 和原因

### Requirement: Program bytes 与 ProgramHash 必须稳定

相同 SemanticHash、compiler version、operation-set version、NumericProfile、required world capability 和 TickRate MUST产生相同 canonical bytes 与 ProgramHash。Traversal、operation、constant、scope 和 catalog MUST使用稳定 identity/order，MUST不依赖 Unity instance id、display name 或无序集合迭代。不同 NumericProfile MUST产生不同 ProgramHash。

#### Scenario: 重复编译未修改资产

- **WHEN** 相同 source revision 被重复编译
- **THEN** ProgramHash MUST保持不变

### Requirement: Program Artifact 必须与 Source Revision 严格对齐

正式 Target Program artifact MUST记录 compiler version、operation-set version、source revision、SemanticHash、TickRate、NumericProfile、Target ABI、ProgramId、ProgramHash、LayoutHash与 capability manifest。Unity ProgramAsset MUST只包装经过正式 store重读校验的 exact `.csim` bytes与轻量 metadata。Host MUST在 artifact stale、Program缺失、Target ABI不匹配、ProgramAsset metadata不匹配或 required capability不满足时创建失败，MUST不在运行时重新编译、读取 `.csir`、重新编码 Program或使用旧 interpreter。

#### Scenario: Authoring 已修改但 Program 未重建

- **WHEN** Host检测到 source revision与 Program artifact不同
- **THEN** Host MUST拒绝创建 Session并报告 stale source
- **AND** MUST不从 ProgramAsset metadata、旧 `.csim`或 `.csir`选择近似匹配结果

### Requirement: Presentation Projection 必须与 Gameplay Numeric Target 分离

Compiler MUST从validated Gameplay Semantic IR artifact建立唯一target-neutral `CharacterPresentationSemanticContract`，并与同一authoring root的Presentation inventory及正式Animation Analysis artifacts生成`CharacterPresentationProjection`。Contract MUST规范保存ProgramId、Gameplay SourceRevision、SemanticHash、按index排序的producer contract与ContractHash。Projection MUST保存该ContractHash与独立ProjectionRevision，MUST不保存或接收Float32/Fixed ProgramHash、LayoutHash、NumericProfile、Target ABI、State codec或target-specific constant。Projection Compiler的正式输入 MUST不包含任何Numeric Target Program；Float32与Fixed Program只能在各自严格加载后通过Target Adapter生成同一Presentation contract。

Projection用于映射producer identity到AnimationClip、Animancer、Camera、Cue、Equipment Visual资源，以及由显式Presentation Analysis Source生成的每脚动画特征。Projection MAY保存Calibration/Analysis identity和压缩后的表现feature curve，但 MUST不保存Graph flow、State transition、Timeline Window、MotionCurve、GameplayEffect真值、Gameplay contact、Sampling Rig实例或Editor采样状态。生成特征 MUST不进入Semantic IR、Numeric Program、Character State、Snapshot、Gameplay Hash或Network协议。

#### Scenario: 客户端定位攻击动画

- **WHEN** 任一Numeric Target Program输出Attack producer command
- **THEN** Presentation MUST通过Projection定位Unity动画资源并采样匹配的Foot Analysis
- **AND** Projection MUST不决定Attack状态、Window或Gameplay命中

#### Scenario: 同一语义生成Float32与Fixed Program

- **WHEN** 同一validated Semantic IR生成Float32 Program与Fixed Program
- **THEN** 两个Target Adapter MUST生成相同Presentation ContractHash并加载同一Projection
- **AND** 两个Program MUST继续拥有各自不同的ProgramHash、LayoutHash、NumericProfile与ABI

#### Scenario: 纯表现分析变化

- **WHEN** AnimationClip内容、Analysis Source内容或Rig Calibration改变但Gameplay authoring语义不变
- **THEN** ProjectionRevision MUST改变
- **AND** Gameplay SourceRevision、Semantic operation、State layout、Numeric Program payload与各Target ProgramHash MUST保持不变

#### Scenario: Graph Camera producer编译

- **WHEN** Projection Compiler处理Graph来源的Camera producer
- **THEN** MUST从validated Semantic IR operation、reference、source map与numeric-neutral literal生成Camera binding
- **AND** MUST不先生成Float32或Fixed Program再反读target constant

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

### Requirement: Program 与 Projection 必须在同一 Build Transaction 中发布

Character Simulation Build MUST按`Frontend artifact -> Presentation contract -> resolve exact Animation Analysis artifacts -> independently compile Presentation Projection and requested Numeric Target Programs -> cross-artifact identity validation -> atomic publish`执行。ProjectionRevision MUST由Projection schema、Presentation ContractHash、Presentation authoring dependency与Analysis artifact identity/content hash规范计算，MUST不包含任一Target ProgramHash、NumericProfile或ABI。单clip artifact MAY在该事务之前独立生成，但Build MUST重新校验其完整identity和payload hash。Semantic IR cache、Projection、全部请求Target canonical artifact、Unity wrapper与generated reference MUST先完成stage和exact重读，再作为一个发布组提交；任一artifact、Target或Projection阶段失败 MUST恢复完整旧发布组，不得更新一半generated reference。

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

### Requirement: Compiler Diagnostics 与 Agent 必须复用正式 Frontend 和 Target 阶段

Definition diagnostics、Agent validator 和其它 Editor caller MAY执行不发布 Program/Projection 的 dry-run，但 MUST复用正式 Authoring Discovery、Semantic Emission、artifact codec 和 Target Compiler。Dry-run result MUST以 artifact descriptor/identity 和分阶段 report 表达 Semantic 成功，不得依赖旧 `CharacterSimulationCompileResult.SemanticIr` 直通对象，也不得维护第二个 validator operation table。

#### Scenario: Agent 校验 Corin Patch

- **WHEN** Agent validator 对修改后的 Corin authoring 执行正式编译校验
- **THEN** MUST通过同一 Frontend 生成并校验 Semantic artifact payload
- **AND** MUST不自行发射 Semantic operations 或直接调用 raw Float32 lowerer

### Requirement: Target Program 必须作为正式独立 Artifact 原子发布

Editor build MUST将每个 Numeric Target的 canonical Program写入 `Library/CharacterSimulation/Programs/<definition-guid>/<numeric-profile>-abi<version>.csim`，并使用同目录临时文件、完整 flush、重新读取、header/ProgramHash/LayoutHash校验和原子替换。路径 MUST只来自合法 Definition GUID、NumericProfileId与 ABI version，不得按 Definition名称、asset path、ProgramId显示字符串或 fallback名称生成。

#### Scenario: 生成 Corin Float32 Program

- **WHEN** Float32 Target成功降低 Corin validated `.csir`
- **THEN** build MUST发布一份可由普通 .NET Reader读取的正式 Float32 `.csim`
- **AND** Corin ProgramAsset MUST包装从该 store重读的同一 bytes

#### Scenario: Program Artifact 写入中断

- **WHEN** `.csim` 临时写入、重读校验、Unity Asset publish或 Definition reference更新失败
- **THEN** build transaction MUST恢复旧 `.csim`、ProgramAsset、Projection与 Definition references
- **AND** MUST不留下新 Program与旧 Projection或旧 ProgramAsset的混合组合

### Requirement: Program Identity 与 Session Pipeline Identity 必须分离

ProgramHash MUST只覆盖 Numeric Target Program语义和 ABI，MUST不包含 PipelineId、PipelineHash、BackendId、Session Source、Solver或 Network Model。同一 Program MAY进入多个合法 Session Pipeline；Session composition、Snapshot、diagnostics与后续 handshake MUST另外锁定 Pipeline/Backend/Source/Solver identity。Pipeline不同 MUST不要求重新编译 BTSMTL Program，也 MUST不允许两个不同 Pipeline snapshot互换。

#### Scenario: Corin 复用在 Local 与 Prediction Pipeline

- **WHEN** 两个 Session使用同一 Corin Float32 `.csim`但选择不同 Pipeline
- **THEN** 两者 ProgramHash MUST保持相同
- **AND** 两者 PipelineHash与 Session composition identity MUST不同

### Requirement: Target Program必须以结构化Binding保存Constant Value输入

每个Numeric Target Program MUST从validated Semantic IR降低结构化constant input binding table。每条binding MUST保存target operation、target port、target-specific constant index与resolved value kind，并 MUST进入Program canonical bytes与ProgramHash。Linked input MUST继续只来自`ProgramControlFlowEdge(kind=Value)`。Program constructor、codec、artifact store与composition MUST拒绝重复target port、linked/constant双source、非法constant index、kind不兼容和不支持该table的旧ABI；Runtime MUST不解析`/constant/port:`或其它constant identity约定。

#### Scenario: Compare同时读取连线与常量

- **WHEN** Compare的Left来自Value edge而Right来自authoring constant
- **THEN** Target Program MUST分别保存linked edge和Right constant binding
- **AND** ProgramExecutionLayout MUST能在不解析字符串的情况下合并两者

#### Scenario: Float32与Fixed来自同一Semantic binding

- **WHEN** 同一validated Semantic IR分别生成Float32与Fixed Program
- **THEN** 两个Program MUST保存相同target operation/port语义与resolved value kind
- **AND** constant index和值 MUST按各自Target ABI降低，ProgramHash MUST彼此不同

#### Scenario: 同一端口存在多个source

- **WHEN** Target Program table为同一operation/port包含重复binding或与Value edge冲突
- **THEN** Target build或Program load MUST在composition前失败
- **AND** Runtime MUST不选择第一个、最后一个或任意source继续执行

#### Scenario: Host读取旧字符串端口Program

- **WHEN** Host读取缺少结构化binding table的旧`.csim`、`.fixed-program`或ProgramAsset metadata
- **THEN** artifact/ABI validation MUST明确拒绝
- **AND** MUST不启用legacy parser、migrator、fallback artifact或双版本runtime

### Requirement: Program 必须声明 Motion Modifier descriptor 与固定顺序

Target Program MUST保存按channel索引的canonical Motion Modifier descriptor，包含operation、source Motion operation、Timeline owner、Action Context owner和state slot range。ProgramHash与LayoutHash MUST覆盖descriptor内容和顺序。Runtime MUST不扫描authoring asset、按字符串发现handler或根据Network Model改变顺序。

#### Scenario: 同一 Authoring 编译两个 Target

- **WHEN** 同一Semantic IR分别降低为Float32与Fixed Program
- **THEN** 两个Program MUST包含同语义modifier descriptor和source关系
- **AND** 数值表示差异 MUST不改变modifier eligibility与顺序

### Requirement: MotionWarp 跨 Tick 数据必须进入 Character State Layout

Program MUST为每个MotionWarp operation声明恢复后继续执行所需的typed state，包括active/initialized、generation、ActionInstance、窗口开始Body pose、源窗口起始pose、有效Target Pose、Limit结果、previous Warped Cumulative Pose、上一position/yaw progress与source operation identity。同Step raw contribution、resolved channel、current modifier output与CharacterMotionRequest MUST保持transient且不得进入committed state。Program MUST不再保存冻结total position/yaw correction或nominal curve-end residual。

#### Scenario: 检查 MotionWarp state layout

- **WHEN** Compiler生成包含MotionWarp的Program
- **THEN** Program MUST声明完整Warp state slots和默认值

### Requirement: Program 必须声明Body Motion descriptor与能力身份

Target Program MUST保存由Definition正式Profile降低得到的Body Motion descriptor，包括GravityAcceleration、MaximumFallSpeed与semantic version，并 MUST将其纳入canonical bytes、ProgramHash、source revision和required world capabilities。Float32与Fixed Program MUST从同一numeric-neutral descriptor产生各自Target数值payload。Runtime MUST不读取authoring Profile、Scene默认或Network Model配置补齐descriptor；旧ABI或缺失descriptor的artifact MUST被拒绝。当前Float32 Program ABI MUST为7，Fixed Q32.32 Program ABI MUST为6。

#### Scenario: Fixed Program降低Body Motion配置

- **WHEN** Compiler从同一Semantic IR生成Fixed Program
- **THEN** GravityAcceleration与MaximumFallSpeed MUST按Fixed Target规则降低
- **AND** Program MUST要求AirborneVerticalMotion
- **AND** descriptor或semantic version变化 MUST形成新的Program identity
- **AND** Program MUST不为resolved channel或最终request分配跨Tickslot

### Requirement: MotionWarp 版本变化必须拒绝旧 Artifact

增加MotionWarp operation、descriptor、ActionTargetRequirement或Warp state schema时，Frontend、Operation Set、Target ABI、Program artifact与State codec identity MUST按实际payload变化提升。旧reader、旧state payload、兼容operation分派和字段猜测 MUST删除。

#### Scenario: Session 加载旧 Program

- **WHEN** composition读取MotionWarp版本升级前的Program或State payload
- **THEN** composition MUST在Session启动前明确失败
- **AND** MUST不把缺失descriptor解释为无Modifier

### Requirement: Compiled Program必须包含不可变Equipment catalog和layout

Target Program MUST包含canonical Equipment Slot、Route、Equipment、Feature、Parameter constant、Action binding、graph entry、Tag/Effect contribution、Presentation requirement与Initial Loadout catalog，并为Equipment aggregate、Feature local state、pending change和Action Equipment Context分配类型化state layout。Catalog和layout MUST参与Program/Layout identity，Runtime MUST不从Unity authoring资产补建。

#### Scenario: Program加载装备catalog

- **WHEN** Runtime创建Corin ProgramCatalog
- **THEN** MUST一次构建Equipment lookup和entry layout
- **AND** 每Tick MUST不重扫Feature、Graph或Action列表

#### Scenario: Equipment catalog bytes被修改

- **WHEN** canonical equipment bytes与ProgramHash不匹配
- **THEN** Program load MUST拒绝
- **AND** MUST不只重算Equipment子表继续运行

### Requirement: Program identity必须覆盖Equipment authoring真相

SourceRevision、SemanticHash、ProgramHash与LayoutHash MUST按各自现行职责覆盖Equipment Profile及全部引用Feature/Graph/Timeline/parameter/Tag/Effect/Presentation requirement。相同source在Float32与Fixed MAY具有不同ProgramHash/LayoutHash，但 MUST具有可核对的同一SemanticHash；不同Equipment catalog的Program snapshot MUST不可交换。

#### Scenario: 只修改武器参数

- **WHEN** 作者修改Sawblade MotionScale
- **THEN** SourceRevision、SemanticHash与目标ProgramHash MUST改变
- **AND** 旧generated Program MUST被判定过期

#### Scenario: 只修改Unity Prefab视觉资源

- **WHEN** SpawnedVisualAsset引用或binding pose改变
- **THEN** Presentation Projection identity MUST改变
- **AND** 若Program只保存稳定VisualBindingId，Gameplay SemanticHash MUST不因Unity表现内容无意义改变

### Requirement: Program Execution Layout必须预构建Equipment索引

Program runtime initialization MUST按Program一次构建Slot/Route/Equipment/Feature/Parameter/entry和state address索引，并验证引用闭包。Actor/Tick热路径 MUST使用稳定index或typed handle，不得执行LINQ catalog重建、字符串查找、AssetDatabase访问或Feature list排序。

#### Scenario: 每Tick解析PrimaryAction

- **WHEN** Route Host解析MainWeapon PrimaryAction
- **THEN** MUST通过预构建Slot/Route/Feature index定位entry
- **AND** MUST不分配临时集合或按字符串扫描

#### Scenario: 初始化发现悬空entry

- **WHEN** Route catalog引用不存在的operation entry
- **THEN** Program execution layout build MUST失败
- **AND** Session MUST不进入Active

