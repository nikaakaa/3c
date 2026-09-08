## RENAMED Requirements

- FROM: `### Requirement: State-local source必须由Profile binding和provider解析`
- TO: `### Requirement: State-local source必须由作者资源编译并经provider供样`

## MODIFIED Requirements

### Requirement: Pose Graph必须唯一表达完整表现拓扑

正式Pose作者体系 MUST通过AnimGraph、Animation Layer、状态／规则图和Control Rig唯一表达动画组合与身体修正。根图 MUST引用相应作者功能，不要求逐个连接内部Action读取、参数汇总、Goal打包或Assembler。完整执行拓扑 MUST由同一Compiler从作者节点、接口和配置展开，继续形成既有Source、Pose、Constraint及Final Publication链。

编译后所有依赖与空间转换 MUST显式存在于唯一计划；Runtime MUST不补建Foot、Goal Set、FBBIK、惯性化或第二输出。每个角色最终计划的身体求解位置 MUST唯一，不得通过多个Rig调用重复求解或写骨骼。

#### Scenario: 查看身体修正
- **WHEN** 作者在AnimGraph查看身体修正节点
- **THEN** 作者 MUST能进入Control Rig编辑目标及求解设置，而无需在根图接Goal Assembler
- **AND** 内部步骤 MUST仍可通过同一来源映射按需观察

#### Scenario: 查看完整Foot Placement拓扑
- **WHEN** 作者查看包含Foot Placement与其它目标的身体修正
- **THEN** 作者 MUST在Control Rig编辑目标与FBIK，编译计划保留Contribution、唯一Assembler与Goal Set依赖
- **AND** 内部组装步骤仅按需诊断，不恢复为根图必接节点

### Requirement: Pose端口必须显式区分空间并允许typed控制目标

作者端口 MUST区分Local Pose、Component Pose、参数与Rig控制目标，目标 MUST具有骨骼、空间、权重和可用性。Control Rig调用的输入输出空间是接口合同；Compiler只在明确的接口边界展开必要转换，任意不同空间连接不得隐式cast。

内部Goal Contribution与Goal Set MUST继续使用现有不同typed合同，不作为普通作者必须组装的数据。编译展开 MUST拒绝未经明确目标混合／选择的重复Effector来源、错误Application、Rig不匹配和容量超限；Runtime不能按连线顺序覆盖或通过可写IK骨伪装目标。

#### Scenario: 双来源控制同一手部目标
- **WHEN** 两个控制来源直接竞争同一Effector且没有显式混合或选择
- **THEN** 编译 MUST同时定位两个来源与目标并拒绝发布
- **AND** MUST不生成两个Solver或临时覆盖顺序

#### Scenario: FootPlacement与PoseBone贡献同一Slot
- **WHEN** Foot Placement与其它来源竞争同一IK Effector而没有显式混合或选择
- **THEN** 编译 MUST定位两个来源与目标并拒绝发布
- **AND** 不得按接线顺序覆盖

### Requirement: Pose Plan必须按拓扑编译为有序执行阶段

唯一Pose Compiler MUST通过既有Closure、typed语义展开、Topology、Family Lowering、Stage、Value Lifetime、Workspace与Seal链生成不可变Program Image。作者一个Slot、层或Rig节点可以对应多条内部operation；内部步骤 MUST具有稳定作者owner、步骤种类、端口和完整call-site来源，不形成第二份可编辑图。

Source capture、Action读取、Slot、曲线传播、空间转换、Goal组装与FBBIK MUST进入显式有序依赖。没有作者Rig／FBIK请求时不得为了满足旧固定拓扑而补建求解；存在唯一身体求解请求且有效贡献为零时，保持现有固定容量零Goal语义。每个source usage每帧最多capture一次，每个有效operation仅按计划执行，PlayableGraph最多Evaluate一次，Physical Bones只由唯一Final Publication写入。Runtime不重新编译、重排或解释作者图。

#### Scenario: 一个Slot展开多个步骤
- **WHEN** Slot需要读取动作播放、混合Source Pose并传播曲线
- **THEN** Compiler MUST生成相应typed operation并归属该Slot作者节点
- **AND** 作者图 MUST不增加系统生成的可编辑输入节点

#### Scenario: 同一Pose供两个混合分支使用
- **WHEN** 作者明确复用同帧缓存Pose
- **THEN** 计划 MUST复用同一producer结果并保持固定值寿命
- **AND** MUST不重复推进状态机或采样该usage

#### Scenario: 重复调度
- **WHEN** 计划重复调度、遗漏可达operation或违反World-aware依赖
- **THEN** Seal MUST拒绝发布，不由Runtime补执行或跳过掩盖

#### Scenario: Foot Placement后执行FullBodyIK

- **WHEN** Foot Placement与其它Goal Source完成同Frame Goal Contribution
- **THEN** Stage Schedule MUST在其后执行唯一Goal Assembler和唯一FullBodyIK
- **AND** 后续节点 MUST消费FBBIK输出而不是输入Pose或外层Runtime预先生成的副本

#### Scenario: 编译无Goal贡献的角色
- **WHEN** 图没有身体求解请求，或显式求解的当前有效贡献为零
- **THEN** 前者 MUST不生成求解链；后者 MUST保持既有固定零Goal合同
- **AND** 不补Empty Goal fallback或第二输出

#### Scenario: Operation被重复调度

- **WHEN** Stage Schedule包含重复Operation index、遗漏可达Operation或不匹配Execution Domain
- **THEN** Program Image Seal MUST拒绝Projection发布
- **AND** Runtime MUST不通过completion检查掩盖非法Schedule

### Requirement: State inline graph必须存入root-owned flat graph catalog

State、Subgraph、Animation Layer及Control Rig的作者嵌套 MUST通过稳定GraphId、EntryId和typed接口引用表达；相应owner使用flat catalog保存唯一图记录，库Implementation保留其正式owner。State MUST不重新内嵌旧图对象，Runtime MUST只消费静态展开的计划。

GraphId、接口与输出必须合法，递归或悬空调用必须在编译前定位完整链。Layer调用状态按Actor／Implementation generation／call-site隔离，只有明确共享合同允许共享状态；窗口导航必须保留调用来源。

#### Scenario: 打开状态Pose图
- **WHEN** 作者进入一个State
- **THEN** Editor MUST根据稳定引用打开唯一State Pose图，并显示完整父路径

#### Scenario: 层形成递归
- **WHEN** 两个Layer／Subgraph互相调用形成环
- **THEN** 编译 MUST报告完整调用链并停止，不提供动态递归fallback

#### Scenario: 打开Locomotion State图

- **WHEN** 作者进入Locomotion State
- **THEN** Editor MUST按State的PoseGraphId导航到catalog记录
- **AND** State serialized data MUST不再嵌套旧`m_InlinePoseGraph`

#### Scenario: Subgraph形成递归

- **WHEN** Graph A和Graph B通过GraphId相互调用
- **THEN** Build MUST失败并报告完整依赖链
- **AND** Runtime MUST不尝试动态递归

### Requirement: State-local source必须由作者资源编译并经provider供样

State-local Player MUST直接拥有精确原生AnimationClip、Blend Space或Motion Matching资源引用，或使用明确typed资源参数；面向作者的Source Slot与Profile重复资源binding必须删除。Profile MUST继续提供角色Rig及分析／资源约束，Compiler生成唯一dense binding和source usage，由既有provider供样。

Player MUST显式表达速率、起始位置、Loop与clock policy；资源引用不得复制曲线或恢复Sequence包装资产。有效Finite／Cyclic语义由该usage播放策略编译，并参与Phase／同步约束。provider MUST保持dense source index、generation、Projection revision、readiness和frame lease，不在Runtime查询AssetDatabase或显示名。

#### Scenario: 直接选择Idle动画
- **WHEN** 作者给Sequence Player选择原生Idle Clip
- **THEN** Compiler MUST从该唯一引用与Rig合同建立source binding
- **AND** 作者 MUST不再建立另一个Source Slot才能使用资源

#### Scenario: Player覆盖循环方式
- **WHEN** 同一Clip在不同Player中采用不同Loop设置
- **THEN** 编译 MUST按各usage生成合法时间策略，保持同一素材引用
- **AND** 同步关系不能使用不相容的循环假设

#### Scenario: ClipPlayer首次采样Idle
- **WHEN** Idle Player获得entry relevance
- **THEN** provider MUST从Player资源编译的唯一binding提供Ready sample
- **AND** 不查询旧Source Slot、Sequence资产或AssetDatabase

#### Scenario: ClipPlayer提交Loop字段
- **WHEN** 作者声明合法Loop Animation策略
- **THEN** 新合同 MUST接受并编译该usage的Finite／Cyclic行为
- **AND** 素材曲线不复制，旧的禁止Player Loop字段限制被替换

### Requirement: AnimationSlot必须是有限Action的唯一Pose插入口

Slot MUST提供Source Pose、稳定Slot引用和源更新策略，并可存在于AnimGraph、Animation Layer或State Pose图。它消费既有有限Action Timeline发布的播放结果，不要求作者连接Action Playback Input；读取步骤由Compiler展开。

Slot MUST不拥有Bone Mask、不判断动作准入、不推进Timeline。没有播放时透传合法Source Pose，播放及退出按该Action Timeline的动画Blend设置与正式Routing处理。Group、Slot、内部AnimationChannel分别绑定，多个消费位置不能创建多个播放实例或重复sample。层内Slot不自动私有化。

#### Scenario: FullBody动作为空
- **WHEN** Slot没有活动playback
- **THEN** 输出 MUST为其当前合法Source Pose，不生成默认Idle或旧Stored Pose

#### Scenario: 攻击结束时基础状态已变化
- **WHEN** Corin Slot持续更新Source Pose，攻击期间基础状态从Move切到Idle
- **THEN** 动作退出 MUST回到当前Idle，Gameplay无需指定恢复状态

#### Scenario: 每个动作有不同混合设置
- **WHEN** 攻击与换弹共用Slot但其Timeline动画Blend设置不同
- **THEN** 各playback MUST采用自己的设置，不被Slot级重复Policy覆盖

#### Scenario: FullBodyAction为空

- **WHEN** Slot没有活动Action playback
- **THEN** 输出 MUST与当前PoseState Source Pose一致
- **AND** MUST不创建默认Idle或常驻Stored Pose

#### Scenario: Attack结束时角色已经停下

- **WHEN** Attack期间PoseStateMachine已经从Move切到Idle
- **THEN** Slot release MUST回到当前Idle Source Pose
- **AND** Gameplay MUST不指定恢复到Run或Idle

### Requirement: Pose节点必须显式处理可用性和局部连续性

Player与组合节点 MUST声明Required／Optional Pose语义，NoPose与Invalid不得由bind pose、旧帧或零矩阵伪装。Blend Stack只拥有自己声明的历史与release；显式Inertialization只拥有其输入分支的完成输出history、residual与rebase。

惯性请求 MUST来自状态转换、Slot／Timeline动画Blend或显式Player不连续事件，各请求有唯一时间设置owner。下游处理节点可以接收有界的多个请求，按最短duration与稳定owner顺序采用对应设置；未连接分支不共享history。首帧、Reset、Invalid和NoPose保持明确处理，Output前不能自动生成全局惯性化。

#### Scenario: 局部动作惯性化
- **WHEN** 一个动作分支连接Inertialization后再参与骨骼混合
- **THEN** residual MUST只作用于该分支，不接管基础分支history

#### Scenario: Required Pose缺失
- **WHEN** 最终依赖路径上的Required Pose无效
- **THEN** 当前帧 MUST失败，不能发布旧Final Pose

#### Scenario: 局部Action分支惯性化

- **WHEN** Action Player连接Inertialization后进入LayeredBoneBlend
- **THEN** residual MUST只影响该Action分支
- **AND** Base Pose分支 MUST不共享其history

### Requirement: Pose参数必须通过typed页面和显式解析传播

Pose参数 MUST使用稳定typed声明、明确默认值和合法来源；变量输入读取committed页面，素材Curve随Pose Value传播。普通混合的Curve策略 MUST配置在实际组合节点，内部参数解析由Compiler展开，不强制作者接Pose Parameter Resolve。确有独立Curve修改意图时提供明确作者能力，不恢复通用内部数据汇总节点。

节点不得按显示名、任意字符串或Gameplay对象查值。脚部权重等参数必须保持其正式Curve／Fact来源，不因隐藏内部operation而补造默认结果。

#### Scenario: 权重连接参数
- **WHEN** 作者把正式参数接到Alpha
- **THEN** 编译 MUST确定类型与页面来源，Runtime直接读取该typed值

#### Scenario: 两份Pose需要不同Curve组合
- **WHEN** 作者在组合节点选择Curve混合策略
- **THEN** 编译 MUST按该策略传播参数并将内部步骤映射回该组合节点

#### Scenario: Blend权重读取Program参数

- **WHEN** BlendPose权重连接ProgramParameterInput
- **THEN** Compiler MUST校验ParameterId、类型和page layout
- **AND** Runtime MUST不读取Gameplay对象

### Requirement: Pose Graph工作区必须准确映射Authoring、Live与References

正式工作区 MUST用唯一原生图区域组织各角色图、目录、Details、必要资产工具与运行观察。Authoring只写当前真实owner；Live只显示精确Actor／generation／图与产物版本／call-site的已完成结果；References只按需提供资源、Rig、Policy、Timeline与调用来源。GUID、hash、compiled index和空运行栏目默认隐藏。

Player直接显示资源选择，状态、Alias和转换显示可编辑业务字段；转换双击进入条件。字段提交失败必须恢复正式值并就地解释，不能以“已处理”吞掉失败。Source Map内部步骤只属于按需诊断，不成为作者节点。

#### Scenario: 查看State的Player
- **WHEN** 作者选中Sequence或Blend Space Player
- **THEN** Details MUST显示实际资源、速率和播放策略，并提供打开资源的入口
- **AND** MUST不要求在Source Slot与Profile Binding两个页面重复配置

#### Scenario: 作者修改导致运行版本失配
- **WHEN** 图、Rig或产物不再匹配观察结果
- **THEN** Live MUST清空旧叠加并解释Stale，不伪造作者默认值为运行值

#### Scenario: 查看Locomotion State
- **WHEN** 作者选中状态的Sequence或Blend Space Player
- **THEN** Details MUST显示实际资源、速率和播放策略并可打开资源
- **AND** 不要求Source Slot／Profile两次选择或显示内部provider id

#### Scenario: Runtime revision不匹配

- **WHEN** snapshot revision与当前文档或Projection不一致
- **THEN** Live MUST显示Stale
- **AND** MUST不从authoring默认值或Animancer state伪造结果

### Requirement: Preview、Runtime与Live Debug必须复用同一固定Pose Plan

Pose MUST继续编译为唯一不可变Program Image，Actor复用既有Execution View、Source、Constraint、根Frame事务和Final Publication。窗口Preview／Live只表示普通Unity Play真实角色观察，不创建场景、角色、独立播放器、作者图执行器或私有时钟。

Layer、Slot和Rig调用全部进入同一计划；观察不得增加sample、Foot、FBBIK或Writer执行次数。更改作者数据只使产物Stale，必须由显式Build发布新版本，不热换运行实例。

#### Scenario: 普通Play中打开控制图
- **WHEN** 已有精确匹配的真实Actor
- **THEN** 工作区 MUST观察该调用完成结果，不启动一份Control Rig runtime

#### Scenario: World Context缺失
- **WHEN** 实际Foot Placement没有合法World Context
- **THEN** 正式Runtime MUST按原合同发布Unavailable，窗口不能补造地面或简化求解

#### Scenario: Graph修改后继续Preview
- **WHEN** 作者改变状态、层、Slot、Rig或Foot设置导致版本失配
- **THEN** 窗口 MUST清空旧叠加并提示显式Build，普通角色继续原运行
- **AND** 不热换产物、建临时Program或使用旧reader

#### Scenario: Preview缺少world context
- **WHEN** 真实角色执行Foot Placement时缺少World Context
- **THEN** 正式Runtime MUST发布Unavailable并阻断依赖输出
- **AND** 窗口不得补地面或进行简化求解

### Requirement: Pose authoring必须使用共享Capability与类型化Presentation Mutation

AnimGraph、Layer、State、Rule、Rig与节点字段／端口 MUST由唯一Capability及接口合同提供，供Editor、Document、Clipboard、Reconciler、Mutation和Compiler共用。作者节点和运行operation不必一一对应，语义展开只能由对应Definition提供；Topology和Stage保持全局唯一职责。

创建、连接、改接、粘贴、删除、资源、字段、接口和布局变更 MUST进入同一typed Mutation与实际owner事务；失败不得部分写入。内部Generated binding和operation不能进入editable，Compiler不能通过作者getter求值或旧图镜像获取输入。

#### Scenario: 添加新的Rig目标能力
- **WHEN** 正式目录注册新的typed目标节点
- **THEN** 创建菜单、端口、Document、编译展开及约束 MUST识别同一合同
- **AND** MUST不在Editor和Compiler分别维护目标字段清单

#### Scenario: 新增Pose节点能力

- **WHEN** 新Pose节点注册唯一Definition Adapter
- **THEN** 人工创建菜单、Document v7、Clipboard、统一Port Shape、Validator、Graph Closure和Compiler MUST识别同一Capability与Payload合同
- **AND** MUST不要求在多个Catalog、Handler或NodeKind switch中重复声明同一字段和端口

#### Scenario: Node Definition缺少Document投影

- **WHEN** 一个Definition无法为正式Document/Mutation合同提供完整typed字段、条件端口或Graph dependency
- **THEN** Definition目录或Character Build MUST失败并定位Node Kind
- **AND** Agent authoring MUST不使用通用SerializedProperty或自由文本绕过

### Requirement: Pose Graph UI必须保留准确术语和serialized identity

作者术语 MUST对应AnimGraph、Animation Layer、State Machine、Transition Rule、Sequence Player、Blend Space Player、Slot、Layered Blend Per Bone、Inertialization、Control Rig和Output Pose。Sequence Player使用原生AnimationClip，不引入旧Sequence资产；现有有限Action Timeline承担Montage职责，通用Timeline名称保留。Source binding、Animation Slot与IK Effector不得混称。

显示名称、颜色和布局不得替代稳定identity或改变空间。新组织迁移应保留可保留的Node／State／Graph identity，退役旧内部作者节点及无消费者配置，不保留兼容作者kind或第二画布。

#### Scenario: 创建播放器
- **WHEN** 作者从目录或资源拖拽创建Sequence Player
- **THEN** 系统 MUST建立对原生AnimationClip的唯一正式引用，并使用统一能力与稳定身份
- **AND** MUST不新建Animation Sequence包装资产

#### Scenario: 作者添加单Clip播放器
- **WHEN** 作者添加单原生Clip播放器
- **THEN** 作者名称 MUST对齐Sequence Player，底层使用原生AnimationClip及统一identity
- **AND** 不创建Animation Sequence包装资产

### Requirement: Goal Sources与FullBodyIK必须使用统一typed目标合同

编译产物中的全部Goal Source MUST发布`CharacterFullBodyIkGoalContribution`，至少携带Frame、Completion、Rig、Producer、Slot、Application、Component空间目标与权重。Foot Placement内部 MUST在既有Pelvis响应、可达观察与原Landing完成判断后才发布最终Resolved Pair；Foot Goal Encoder MUST只读取最终Resolved的目标和权重，Pelvis Goal Encoder MUST只读取唯一Pelvis Result。两者 MUST不读取Foot State、Lock Response、Context、Path、Residual或Diagnostics，不得恢复业务层Reach夹紧或末端夹脚。

唯一Goal Assembler MUST把合法Contribution规范化为一个`CharacterFullBodyIkGoalSet`，继续由现有入口拥有身份、容量、合法性和重复Slot校验，不接管Foot或Pelvis数学，也不为内部请求分型逐层复制相同检查。FBBIK MUST不理解Foot State Context、Contact Patch、Constraint State、Pelvis选择或Diagnostics。

FBBIK腿Effector跨帧稳定策略 MUST由正式FullBodyIK Profile、Rig准备结果和Pending BendHistory决定。Solver MUST不通过搜索FootPlacement SourceKind启用隐藏状态规则；Vendor FinalIK对象内部可变字段不得成为跨帧真相。正式初始化方向只允许来自同一参考姿态准备结果，不增加第二种默认策略。

233436组合中已保留的可靠动画有符号膝向运输 MUST保持，Stable继续保存运输前动画方向，Applied继续保存实际请求，退化分支维持既有政策。结构迁移 MUST不恢复可靠动画半球强翻、SmoothKnee尾段或改变Bend权重来掩盖已知深折叠。

Goal对求解要求权威，Solved Pose对本次Solver输出权威，Physical Result对本次实际写入权威；三者 MUST保留同Completion阶段区别，不得互相代替或反推覆盖源Pose。Solver MUST只写Pending Pose及正式BendHistory，Writer MUST唯一写骨骼；Encoder、Assembler、Root调度和诊断不得取得额外Pose写入权。已有特殊Goal应用数学保持本change范围约定。

#### Scenario: FBBIK消费Foot Placement贡献

- **WHEN** FootPlacement已完成初步请求、Pelvis响应、可达观察和原Landing完成判断，并由Assembler完成唯一Goal Set
- **THEN** FBBIK MUST只按Goal Application、Slot、Profile、正式Rig准备结果与Pending BendHistory执行求解
- **AND** MUST不回调FootPlacement、读取Ground Path或修改Contact ownership

#### Scenario: 编码几何不可达但保持原值的目标

- **WHEN** Foot记录了本腿不可达观察且原流程保持该脚目标与作者权重
- **THEN** Encoder MUST只完成原空间和权重编码，保持最终Resolved与Contribution一致
- **AND** Assembler MUST不新增Reach拦截或夹脚，FBBIK继续原求解数学并保留真实误差
