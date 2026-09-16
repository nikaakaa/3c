## MODIFIED Requirements

### Requirement: Projection Foot Analysis必须拥有独立规范身份

Foot Analysis 资源 MUST保存所消费内容的 canonical Hash，以及动画、分析源、Rig、采样参数和算法版本身份。Editor-only 对象和缓存路径 MUST不进入运行资源。资源绑定 MUST按精确身份检查；其变化 MUST不改变无关技能数据 Hash，不再要求生成角色 Projection 总包。

#### Scenario: Artifact内容变化

- **WHEN** AnimationClip或Analysis输入变化并生成新artifact
- **THEN** 表现资源修订 MUST变化且对应表现资源绑定 MUST变为Stale
- **AND** Gameplay AbilityDataHash MUST保持不变


### Requirement: Projection不得保存原始动画采样快照

正式 Foot Analysis 资源 MUST继续保存经过确定性 key reduction 的有限 curve set，不得保存每帧骨骼快照、采样 Prefab 实例或 Playable 状态。同源资源构建 MUST保持内容一致；此要求独立于原生 Pose 图运行，不重新建立全角色 Projection。

#### Scenario: 多个producer复用同一AnimationClip

- **WHEN** Build在同一事务内分析相同clip与Source组合
- **THEN** Editor MAY复用采样工作结果
- **AND** 发布结果 MUST仍按每个stable clip binding精确引用而不产生运行时字符串字典


### Requirement: Graph authoring 必须按显式 Numeric Target 准备图运行数据

系统 MUST 把已校验的 Graph artifact 准备为明确 Numeric Target 所需的图运行数据。Graph 准备 MUST 不以角色为必要根、不先构建 Pose，也不生成 Ability Program 或完整角色 Program。Target MUST 不接收 Unity 作者对象或 Frontend 私有 model；Float32/Fixed MUST 保持同一图语义，运行时 MUST 不编译或解释 IR。Ability 与角色模块通过正式 binding 执行图。

#### Scenario: 准备 Corin Float32 图运行数据

- **WHEN** 作者为 Corin 的 Gameplay Graph 请求 Float32 准备
- **THEN** Graph owner MUST 先发布 validated Graph artifact，Float32 Target MUST 只从该 artifact 准备图运行数据
- **AND** Runtime MUST不递归 clone RootTree、StateMachine 或 Timeline graph

#### Scenario: Target 收到未校验的内存 IR

- **WHEN** 调用方尝试绕过 artifact codec，把任意 `CharacterGameplaySemanticIr` 对象直接交给正式 Target 入口
- **THEN** 编译 API MUST不提供该公共路径
- **AND** MUST不因对象来自当前 Editor 进程就视为合法 build input


### Requirement: 图运行数据必须是不可变 portable 数据

图运行数据 MUST 只包含 Graph identity、语义/数值版本、节点与连接、图局部状态布局、常量与引用、来源及能力要求，并保存自己的 canonical 数据 Hash。数据 MUST 可由 Unity 与普通 .NET 读取，MUST 不包含 Unity object、角色全部状态、控制代码、网络模型、世界对象或表现图。共享 Graph 在不同 Ability/角色实例中 MUST 保持只读。

#### Scenario: 纯 CSharp 加载图运行数据

- **WHEN** 普通 .NET Host加载 Float32 `.csim` bytes
- **THEN** MUST 不需要 UnityEngine、ScriptableObject、GameplayAbilityDefinition 或 Pipeline asset 才可解析图运行数据
- **AND** MUST 得到与 Unity 图 artifact 相同的 GraphDataHash 与 GraphLayoutHash


### Requirement: Graph authoring type 必须通过唯一 Emitter 生成图数据

每个可执行 Graph 节点 MUST 由唯一 Graph emitter 生成调用/逻辑数据并保存来源、状态与接口要求，不按网络模型改变规则。Timeline 轨道、Clip、MotionCurve 和 MotionWarp MUST 不经过该 emitter；它们直接作为正式内容由唯一 Timeline Runtime 调度。TreeClip 引用的图仍通过同一 Graph owner 准备。

#### Scenario: 缺少 Emitter

- **WHEN** 可达 authoring source 包含没有 Emitter 的可执行类型
- **THEN** Graph artifact build MUST 失败并报告精确 source identity
- **AND** MUST不回退到 authoring node 虚方法执行


### Requirement: Program 必须声明唯一 Numeric Target ABI

Program manifest MUST声明 NumericProfile、scalar/vector ABI、operation-set version、rounding/overflow policy和 serialization version。所有 Gameplay constant MUST由 Target Compiler从 Semantic IR source literal转为该 target格式。Program MUST不保存 float/fixed双值，也 MUST不允许 Session Source、Pipeline或 Network Model在运行时切换 target。当前正式安装 Float32 与 FixedQ32.32 两个 Numeric Target；二者 MUST生成独立 artifact、Program/State ABI 与 Snapshot codec。

#### Scenario: Authoring 数值无法表达

- **WHEN** GameplayEffect magnitude 或 MotionCurve key 无法由当前 Numeric Target 合法表达
- **THEN** target lowering MUST失败并报告 source identity、原值、NumericProfile 和原因


### Requirement: Program bytes 与 ProgramHash 必须稳定

相同 SemanticHash、compiler version、operation-set version、NumericProfile、required world capability 和 TickRate MUST产生相同 canonical bytes 与 AbilityDataHash。Traversal、operation、constant、scope 和 catalog MUST使用稳定 identity/order，MUST不依赖 Unity instance id、display name 或无序集合迭代。不同 NumericProfile MUST产生不同 AbilityDataHash。

#### Scenario: 重复编译未修改资产

- **WHEN** 相同 source revision 被重复编译
- **THEN** AbilityDataHash MUST保持不变


### Requirement: Program Artifact 必须与 Source Revision 严格对齐

正式 Target 技能执行产物 MUST记录 compiler version、operation-set version、source revision、SemanticHash、TickRate、NumericProfile、Target ABI、AbilityId、AbilityDataHash、AbilityLayoutHash与 capability manifest。Unity AbilityDataAsset MUST只包装经过正式 store重读校验的 exact `.csim` bytes与轻量 metadata。Host MUST在 artifact stale、技能数据缺失、Target ABI不匹配、AbilityDataAsset metadata不匹配或 required capability不满足时创建失败，MUST不在运行时重新编译、读取 `.csir`、重新编码技能数据或使用旧 interpreter。

#### Scenario: Authoring 已修改但 Program 未重建

- **WHEN** Host检测到 source revision与 技能执行产物不同
- **THEN** Host MUST拒绝创建 Session并报告 stale source
- **AND** MUST不从 AbilityDataAsset metadata、旧 `.csim`或 `.csir`选择近似匹配结果


### Requirement: Compiler Diagnostics 与 Editor 作者入口必须复用正式 Frontend 和 Target 阶段

技能完整构建与 dry-run MUST复用唯一技能发现、语义、codec 和 Target 入口并返回分阶段结果；轻量作者校验 MUST不被强制升级为完整构建。Pose 校验与观察 MUST使用原生图路径，角色配置检查 MUST由各模块负责，不为诊断生成隐藏角色 Program 或 Pose Image。

#### Scenario: C#作者入口校验Corin authoring

- **WHEN** C#作者入口对修改后的 Corin authoring 执行正式编译校验
- **THEN** MUST通过同一 Frontend 生成并校验 Semantic artifact payload
- **AND** MUST不自行发射 Semantic operations 或直接调用 raw Float32 lowerer


### Requirement: Target Program 必须作为正式独立 Artifact 原子发布

每个技能 Numeric Target 产物 MUST以合法 Ability identity、数值目标和版本保存，完成 flush、重读和 Hash／布局检查后原子替换。Unity wrapper MUST只指向本次完整产物，不依赖角色 Definition GUID 或其它表现资源。文件路径和名称 MUST不参与业务 identity。

#### Scenario: 生成 Corin Float32 Program

- **WHEN** Float32 Target成功降低 Corin validated `.csir`
- **THEN** build MUST发布一份可由普通 .NET Reader读取的正式 Float32 `.csim`
- **AND** Corin AbilityDataAsset MUST包装从该 store重读的同一 bytes

#### Scenario: Program Artifact 写入中断

- **WHEN** `.csim` 临时写入、重读校验、Unity Asset publish或 Definition reference更新失败
- **THEN** 技能发布事务 MUST恢复旧技能bytes、AbilityDataAsset与该Ability的正式引用
- **AND** MUST不留下新技能bytes与旧技能metadata的混合组合


### Requirement: Program Identity 与 Session Pipeline Identity 必须分离

AbilityDataHash MUST只覆盖 Numeric 目标技能数据语义和 ABI，MUST不包含 PipelineId、PipelineHash、BackendId、Session Source、Solver或 Network Model。同一 Program MAY进入多个合法 Session Pipeline；Session composition、Snapshot、diagnostics与后续 handshake MUST另外锁定 Pipeline/Backend/Source/Solver identity。Pipeline不同 MUST不要求重新编译 BTSMTL Program，也 MUST不允许两个不同 Pipeline snapshot互换。

#### Scenario: Corin 复用在 Local 与 Prediction Pipeline

- **WHEN** 两个 Session使用同一 Corin Float32 `.csim`但选择不同 Pipeline
- **THEN** 两者 AbilityDataHash MUST保持相同
- **AND** 两者 PipelineHash与 Session composition identity MUST不同


### Requirement: Target Program必须以结构化Binding保存Constant Value输入

每个Numeric 目标技能数据 MUST从validated Semantic IR降低结构化constant input binding table。每条binding MUST保存target operation、target port、target-specific constant index与resolved value kind，并 MUST进入Program canonical bytes与AbilityDataHash。Linked input MUST继续只来自`ProgramControlFlowEdge(kind=Value)`。Program constructor、codec、artifact store与composition MUST拒绝重复target port、linked/constant双source、非法constant index、kind不兼容和不支持该table的旧ABI；Runtime MUST不解析`/constant/port:`或其它constant identity约定。

#### Scenario: Compare同时读取连线与常量

- **WHEN** Compare的Left来自Value edge而Right来自authoring constant
- **THEN** 目标技能数据 MUST分别保存linked edge和Right constant binding
- **AND** AbilityExecutionLayout MUST能在不解析字符串的情况下合并两者

#### Scenario: Float32与Fixed来自同一Semantic binding

- **WHEN** 同一validated Semantic IR分别生成Float32与Fixed Program
- **THEN** 两个Program MUST保存相同target operation/port语义与resolved value kind
- **AND** constant index和值 MUST按各自Target ABI降低，AbilityDataHash MUST彼此不同

#### Scenario: 同一端口存在多个source

- **WHEN** 目标技能数据 table为同一operation/port包含重复binding或与Value edge冲突
- **THEN** Target build或Program load MUST在composition前失败
- **AND** Runtime MUST不选择第一个、最后一个或任意source继续执行

#### Scenario: Host读取旧字符串端口Program

- **WHEN** Host读取缺少结构化binding table的旧`.csim`、`.fixed-program`或AbilityDataAsset metadata
- **THEN** artifact/ABI validation MUST明确拒绝
- **AND** MUST不启用legacy parser、migrator、fallback artifact或双版本runtime


### Requirement: Program 必须声明 Motion Modifier descriptor 与固定顺序

Motion Modifier 的类型、参数、源Clip、Timeline／动作调用和顺序 MUST由正式 Timeline 内容与运动绑定提供，不再编码为技能Program中的 operation descriptor。运行 MUST保留原 channel 仲裁、固定处理顺序与MotionWarp累计差值规则，不能扫描Unity作者资产或根据网络模型改变行为。

#### Scenario: 同一 Authoring 编译两个 Target

- **WHEN** 同一正式Timeline内容分别准备Float32与Fixed运行绑定
- **THEN** 两个绑定 MUST包含同语义modifier数据和source Clip关系
- **AND** 数值表示差异 MUST不改变modifier eligibility与顺序


### Requirement: MotionWarp 跨 Tick 数据必须进入 Character State Layout

MotionWarp 的当前实例状态 MUST保存 active／initialized、generation、动作实例、窗口起点 Body／source pose、有效目标、限制结果、前一累计位姿和进度，并通过 Timeline／Motion 的正式领域状态接入完整角色快照。技能图只保存 Timeline 调用及返回状态，不为每个 WarpClip 生成操作状态槽。同 Step 运动贡献 MUST保持 transient，不恢复冻结 total correction 或 nominal residual。

#### Scenario: 检查 MotionWarp state layout

- **WHEN** Timeline Runtime准备一个包含MotionWarp的播放实例
- **THEN** Timeline／Motion领域 MUST提供完整Warp实例状态和初始值


### Requirement: MotionWarp 版本变化必须拒绝旧 Artifact

Timeline内容字段、Motion源／映射、Target数值绑定或MotionWarp状态格式改变时，负责该内容的模块 MUST升级实际受影响的版本并拒绝旧数据，不再要求为TimelineClip增加operation-set opcode或角色Program ABI。旧Clip-operation reader、字段猜测和兼容分派 MUST删除。

#### Scenario: Session 加载旧 Program

- **WHEN** composition读取MotionWarp版本升级前的Timeline内容、运动绑定或State payload
- **THEN** composition MUST在Session启动前明确失败
- **AND** MUST不把缺失descriptor解释为无Modifier


## REMOVED Requirements

### Requirement: Program 必须声明完整 Character State Layout

**Reason**: 该条款要求角色总 Program 或整包编译目录，与本次领域运行和技能独立范围冲突；撤销其载体要求，不撤销状态恢复、资源有效性或业务行为。

**Migration**: 由 character-domain-runtime 的角色装配、完整领域状态与身份要求，以及本能力新增的独立绑定要求接管；原独立 Timeline 产品不受影响。

### Requirement: Presentation Projection 必须与 Gameplay Numeric Target 分离

**Reason**: 该条款要求角色总 Program 或整包编译目录，与本次领域运行和技能独立范围冲突；撤销其载体要求，不撤销状态恢复、资源有效性或业务行为。

**Migration**: 由 character-domain-runtime 的角色装配、完整领域状态与身份要求，以及本能力新增的独立绑定要求接管；原独立 Timeline 产品不受影响。

### Requirement: Session ProgramCatalog 必须不可变且支持每 Actor 显式绑定

**Reason**: 该条款要求角色总 Program 或整包编译目录，与本次领域运行和技能独立范围冲突；撤销其载体要求，不撤销状态恢复、资源有效性或业务行为。

**Migration**: 由 character-domain-runtime 的角色装配、完整领域状态与身份要求，以及本能力新增的独立绑定要求接管；原独立 Timeline 产品不受影响。

### Requirement: Program 与 Projection 必须在同一 Build Transaction 中发布

**Reason**: 该条款要求角色总 Program 或整包编译目录，与本次领域运行和技能独立范围冲突；撤销其载体要求，不撤销状态恢复、资源有效性或业务行为。

**Migration**: 由 character-domain-runtime 的角色装配、完整领域状态与身份要求，以及本能力新增的独立绑定要求接管；原独立 Timeline 产品不受影响。

### Requirement: Program 必须声明Body Motion descriptor与能力身份

**Reason**: 该条款要求角色总 Program 或整包编译目录，与本次领域运行和技能独立范围冲突；撤销其载体要求，不撤销状态恢复、资源有效性或业务行为。

**Migration**: 由 character-domain-runtime 的角色装配、完整领域状态与身份要求，以及本能力新增的独立绑定要求接管；原独立 Timeline 产品不受影响。

### Requirement: Compiled Program必须包含不可变Equipment catalog和layout

**Reason**: 该条款要求角色总 Program 或整包编译目录，与本次领域运行和技能独立范围冲突；撤销其载体要求，不撤销状态恢复、资源有效性或业务行为。

**Migration**: 由 character-domain-runtime 的角色装配、完整领域状态与身份要求，以及本能力新增的独立绑定要求接管；原独立 Timeline 产品不受影响。

### Requirement: Program identity必须覆盖Equipment authoring真相

**Reason**: 该条款要求角色总 Program 或整包编译目录，与本次领域运行和技能独立范围冲突；撤销其载体要求，不撤销状态恢复、资源有效性或业务行为。

**Migration**: 由 character-domain-runtime 的角色装配、完整领域状态与身份要求，以及本能力新增的独立绑定要求接管；原独立 Timeline 产品不受影响。

### Requirement: Program Execution Layout必须预构建Equipment索引

**Reason**: 该条款要求角色总 Program 或整包编译目录，与本次领域运行和技能独立范围冲突；撤销其载体要求，不撤销状态恢复、资源有效性或业务行为。

**Migration**: 由 character-domain-runtime 的角色装配、完整领域状态与身份要求，以及本能力新增的独立绑定要求接管；原独立 Timeline 产品不受影响。

## ADDED Requirements

### Requirement: 技能产物与角色领域状态必须独立发布和绑定

技能 MUST独立发布；Control、BodyMotion、Input、Effect、Equipment 配置与状态 MUST由各自模块拥有，角色绑定在 Session Active 前检查完整引用和能力。技能局部状态 MUST包含图流程、Blackboard、调用帧与计时；Timeline播放状态由正式Timeline Runtime独立拥有并参与同一角色恢复；角色快照 MUST组合所有领域的同次提交状态。纯 Pose 或资源问题 MUST不阻止合法技能构建，但非法角色资源绑定 MUST阻止对应角色运行。

#### Scenario: Pose配置缺失但技能合法
- **WHEN** 作者构建一个依赖完整的技能，而某角色的动画配置无效
- **THEN** 技能构建 MUST可以独立完成，角色实例准备仍 MUST报告动画绑定错误

#### Scenario: 多角色使用不同装备配置
- **WHEN** 相同技能分别绑定两个合法角色装备目录
- **THEN** 技能数据 MUST复用，装备状态和整体玩法一致性身份 MUST分别归各角色正式模块

### Requirement: Timeline内容必须作为独立直接数据交付

Timeline发布 MUST只保存正式轨道／片段字段、时间区间、稳定身份和资源／TreeClip图引用；内容校验、portable导出和目标数值准备 MUST不生成时间轴IR或操作表。技能发布只记录实际内容依赖，独立Timeline调用使用同一数据与运行入口。旧独立Timeline IR／Program构建入口 MUST迁移为内容发布，不能因此删除独立调用能力。

#### Scenario: 普通DotNet加载独立Timeline
- **WHEN** 调用方提供合法portable时间轴内容、资源和上下文
- **THEN** 正式Timeline Runtime MUST直接调度该内容，不加载Unity对象或生成临时Program
