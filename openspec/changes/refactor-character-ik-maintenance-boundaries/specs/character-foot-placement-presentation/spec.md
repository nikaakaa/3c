本差量的源码与行为对照固定为用户指定提交`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`；233436仅是对应回放证据，不能用当前HEAD或采样目录替代源码基线。

## MODIFIED Requirements

### Requirement: Resolved Foot必须形成紧凑下游合同

`CharacterResolvedFootResult` MUST只表示当前Foot流程完成既有Landing资格判断后的最终Goal输入。它 MUST发布下游实际消费的Frame、Completion、Rig、Side、Final Sole/Ankle/Rotation、有效Sole/Ankle/Rotation、Correction、作者位置/旋转权重、Contact Reference与Ownership、Support Eligibility、Support Intent与Weight、Support Error、Event lineage、所需typed Reach观察和Outcome。提供给Pelvis的初步需求 MUST使用不同的内部类型，不得把初步Resolved当作最终结果；迁移 MUST不复制两套同义字段或为已删除的夹脚建立受限输出合同。

最终Resolved Pair MUST只组合同Frame、Completion与Rig的两脚结果，不重新选择State、Support、Reach或Goal。内部State、Transition Decision、Path、Anchor历史与Interpolation过程 MUST不进入最终下游合同。Primary Support与Pelvis MUST只消费本模块内部的初步请求视图；Goal编码 MUST只读取最终Resolved与Pelvis Result，不得新增业务层Reach夹紧。必要的身份和数值检查 MUST复用现有生产/消费边界，不在每个内部阶段重复验证相同字段。

最终Sole、Ankle、Rotation、有效目标与Correction MUST保持当前Foot/Heel/Toe几何和权重规则。未加权Goal、加权目标与实际Solved/Physical Pose MUST保持不同含义，不得把最终Goal输入称为已写入的物理脚底或保证它必然可达。原目标不可达时 MUST保留真实观察和原Landing资格结果，不硬改目标、权重或骨盆来制造成功。

#### Scenario: 初步脚结果尚未完成Landing判断

- **WHEN** Foot已完成本帧目标与Interpolation但Pelvis响应及其后的原Landing完成判断尚未结束
- **THEN** Foot MUST只产生内部typed脚需求和完成凭据，不发布最终Resolved
- **AND** 根Runtime与Goal消费者 MUST不能取得这份未完成结果作为正式输出

#### Scenario: 原Landing资格不满足但目标保持

- **WHEN** Foot进入原Landing完成检查且本腿在当前加权Pelvis位移下不满足可达资格
- **THEN** 现有Transition MUST保留原未完成结果，不因此允许Full Lock
- **AND** Foot目标、作者权重和Pelvis响应 MUST保持原行为，不补回末端夹脚或硬压骨盆

#### Scenario: 正常输出保持

- **WHEN** 相同输入进入基于233436保留行为整理后的内部阶段
- **THEN** 分型迁移 MUST保持Goal的位置、旋转、权重和原连续性处理
- **AND** MUST不新增一次Interpolation、Pelvis响应或FBBIK

### Requirement: Pelvis必须只消费typed脚需求并保留可达观察

Primary Support MUST只读取同Frame、Completion、Rig与Side的typed请求中正式Support Eligibility、Support Intent、Support Error、Event lineage与Pelvis Reach Reference。正式Support为零或Reference无效时 MUST按现有业务发布不可用，不得按相对权重归一制造支撑。Contact Reference、Pelvis Reach Reference和Landing Reach Request MUST保持独立含义。

Pelvis MUST只消费请求中所需的目标与Reach视图、Primary Support Result、同帧动画/Body输入和显式设置，不得读取Foot State、Lock Mode、Anchor历史、Path Residual、Interpolation内部状态或Diagnostics。请求的未加权与有效目标 MUST明确分型，权重不得重复应用。

Pelvis MUST继续使用233436组合中用户已接受的共同目标、软姿态偏好、一次Spring及Handoff/背向速度规则，并保留逐腿和交集的typed Reach观察。Reach MUST不夹取骨盆目标或输出、不清边界速度、不阻止Release回零、不强开骨盆权重；Primary Support不得作为例外。末端Foot径向夹脚和公共硬执行边界 MUST保持删除，不以重构之名恢复。

原Landing完成可达资格 MUST继续使用本腿请求与当前实际加权Pelvis位移判断。该结果只作为现有Transition Resolver的准入输入，State仍由唯一Transition Runtime更新；Pelvis和Ground Constraint MUST不能直接反写离散State。删除硬Reach MUST不被扩大为删除原完成资格、改变作者权重或新增一个状态选择器。

#### Scenario: 下游选择Support

- **WHEN** Primary Support收到合法的两脚请求
- **THEN** 它 MUST仅按请求的正式Support与Event字段执行原有获取/保留选择
- **AND** MUST不读取Foot State、Lock Mode或Interpolation历史

#### Scenario: 可达观察参与原Landing完成

- **WHEN** 唯一Pelvis响应已产生本帧实际加权位移，原Foot流程请求检查Landing完成
- **THEN** 本腿typed观察 MUST按当前位移计算原可达资格
- **AND** Foot Lifecycle MUST按原政策消费该结果完成准入，不修改骨盆响应或脚目标

#### Scenario: 主支撑观察不可达

- **WHEN** Primary Support腿的几何观察范围不包含当前Pelvis输出
- **THEN** 系统 MUST保留真实不可达事实，不以Primary身份强制夹取骨盆或脚目标
- **AND** MUST不新增公共硬区间、边界清速度或权重补偿

#### Scenario: 请求身份混杂

- **WHEN** 请求与其对应观察或结果的Frame、Completion、Rig、Side或Event不匹配
- **THEN** 现有唯一交接校验入口 MUST在正式发布前拒绝，不将同一检查复制到每个内部方法
- **AND** MUST不借用上一帧结果、默认脚需求或另一只脚的裁决补全

### Requirement: Foot Placement诊断必须只显示正式结果

Runtime运行历史、内部typed请求、最终结果与只读过程证据 MUST严格分型。运行Owner MUST在计算时捕获本帧实际发生的证据；Diagnostics MUST在唯一根事务内从同一Pending的Observation、请求、最终Foot/Pelvis结果与后续阶段结果完成固定容量冻结和验证。Writer成功时 MUST仅补入同Completion的实际写入事实，Seal后消费者 MUST只读取Committed页，不延迟重算Foot业务。

响应、Contact、Support与Reach过程证据 MAY按业务分组保存，但 MUST不在多份记录中维护同义平铺真相。Gizmo、CSV、Trace与Pose Watch MUST不得查询世界、选择Support、生成Goal、执行FBBIK或改写运行历史。Diagnostics布局与显示兴趣 MUST不改变Runtime输出，公开Diagnostics不得被读取为下一帧状态。

已经完成的紧凑发布 MUST保留单次解析、Analyzer到Publisher的内存事实交接、`analysis.json`小清单、`details.jsonl`唯一明细、`details-index.json`及原始帧字节索引查询。记录分组 MUST不恢复展开facts.json、全量报告复制、磁盘全文重读或第二条Reader链。

#### Scenario: 捕获正式Foot事实

- **WHEN** Foot、Pelvis、Goal、FBBIK和Pending Pose准备进入正式Writer
- **THEN** 已完成阶段的输入、请求、Transition、连续Correction、Reach、最终Resolved与Solved证据 MUST属于同一Pending帧并已完成冻结校验
- **AND** Writer完成后Physical事实 MUST补入同一页，Seal后才作为Committed结果发布

#### Scenario: 增加响应解释字段

- **WHEN** 仅增加本帧响应原因或前后数值的诊断记录
- **THEN** 运行状态、脚目标、Pelvis、Bend与最终骨骼 MUST保持不变
- **AND** MUST不要求修改Goal Assembler、Solver算法或质量评分政策

## ADDED Requirements

### Requirement: Foot阶段数据必须具有唯一权威来源和消费权限

正式Pose/Foot Motion/Body输入、世界Observation、选中Target、Interpolation输出、Ground Constraint输出、初步脚请求、Pelvis结果、Landing完成后的Resolved、Goal、Solved Pose和Physical写入 MUST按阶段分型，明确空间、权重是否已应用、生产Owner及合法消费者。中间事实可以是本阶段权威，但 MUST不冒充后续已完成结果，不得从同名诊断值、临时编码或另一阶段的近似值补齐。

唯一Foot请求生产者 MUST按基线实际规则发布Support事实、Landing Reach观察准入、正式权重及加权脚几何。Module与Pelvis消费者 MUST不混读过程Motion.State、Step和Resolved再次决定同一准入，不通过临时Goal编码后反算另一份Pelvis脚底输入。Stride/Pelvis所需的步态、落点与可用性 MUST通过最小typed请求视图进入唯一准备阶段，不直接取得Foot Context、完整Path页或原始Landing历史。

迁移 MUST以用户指定源码基线中实际被Pelvis、Goal和Writer消费的条件与数值为依据，保持空间换算、权重及数值顺序。同义字段不一致时 MUST明确唯一生产者并报告差异，不凭名字认定权威，不保留两份可选择的正式真相。

实际消费链 MUST只作为回归对照，不能替代正式业务Owner定义。纠正非权威来源若产生行为变化，MUST独立记录来源、数值和业务影响并交由用户决策；不得为保留旧结果建立双读，也不得将行为修正伪装成机械迁移。

#### Scenario: 下游需要判断本腿是否进入可达观察

- **WHEN** Foot请求已发布正式Reach观察准入
- **THEN** Module与Pelvis准备 MUST只消费该决定，不读取过程Motion.State或原始Step重判
- **AND** 请求生产者 MUST保留基线的全部实际权重、事件和可用性条件

#### Scenario: Pelvis需要有效脚底

- **WHEN** 请求已包含按正式权重和空间规则得到的有效Sole
- **THEN** Pelvis MUST直接读取，不建立临时Goal再反解另一份脚底
- **AND** 迁移 MUST保持原来真正被消费的求值顺序，不直接采用未经对账的同义字段

### Requirement: Foot业务控制权必须由唯一Owner执行

Transition Resolver MUST拥有离散变化判定，Transition Runtime MUST唯一应用State、Contact边沿与Anchor命令，Landing Runtime MUST拥有正式Landing记录。State Target MUST只选择请求目标，Interpolation MUST唯一推进残差、响应和Applied Direction历史，Ground Constraint MUST只发布原阶段输出，不倒写Interpolation历史。

Foot输出Owner MUST按作者输入和既有Ready/Suppress/Contact规则解析权重；Primary Selector MUST唯一选择主支撑并写入其历史；Pelvis Owner MUST唯一产生目标、响应和Pelvis权重。本腿可达观察到原Landing完成资格 MUST是唯一反馈，不允许反复重算Foot/Pelvis、反写Spring或建立另一份权重控制器。

Root MUST只调度和提交，不执行业务数学；Encoder、Assembler、Solver和Diagnostics MUST不反写Foot状态、请求、权重或已发布Goal数据。权限 MUST通过收窄输入视图、职责和可变状态可见性落实，不能以多层重复检查代替所有权分离。

#### Scenario: 当前Pelvis位移使Landing不能完成

- **WHEN** 本腿可达观察不满足原Landing完成条件
- **THEN** 现有Foot Transition MUST决定并应用未完成状态
- **AND** Pelvis、Module、Goal层 MUST不另写State、不夹脚、不再次积分或修改原权重

#### Scenario: 诊断读取过程事实

- **WHEN** CSV、Trace或Watch读取目标、权重、Primary或可达性证据
- **THEN** 它们 MUST只展示生产Owner已经作出的决定
- **AND** 运行Owner MUST不反读这些证据控制下一帧


### Requirement: Foot运行历史不得借用过程证据保存

下一帧必须读取的方向、响应、残差和有效性 MUST保存在固定布局typed运行状态中，每项字段具有唯一写入Owner及明确初始化/Reset语义。过程Fact MUST只表达本帧前值、采用值、结果与理由，不能成为隐藏的跨帧状态容器。

全部状态和证据 MUST仍属于同一根Bank；拆分不得创建独立Committed/Pending生命周期、全局缓存、字符串状态Key或新的外部可变Context。Pending事务开放与Committed结果可读 MUST分别判断，不得以已经关闭的Pending标志否定正式历史。其它active拥有的连续性参考不得由本change新增旁路消费。

#### Scenario: 从过程记录移出上一帧方向

- **WHEN** Interpolation需要上一帧实际应用方向限制本帧方向变化
- **THEN** 它 MUST读取唯一正式方向历史并由同一Owner写入Pending新值
- **AND** 诊断Fact只能单向记录该变化，删除诊断投影不得改变方向计算

#### Scenario: 后续阶段丢弃本帧

- **WHEN** Pending Foot历史已更新但完整帧未成功Seal
- **THEN** 新历史与过程证据 MUST共同丢弃，Committed历史保持上一成功帧
- **AND** 任一内部记录 MUST不能单独提交或从未提交Fact恢复状态

### Requirement: Foot采样读写必须由唯一typed列绑定描述

当前Foot采样格式 MUST由Editor唯一有序typed列绑定声明名称、类型、单位、业务分组、有效性和读写映射；Header、写行、Analyzer读取和必需列校验 MUST使用同一绑定。相同列名不得重复声明，位置写入与列名解释不得分别维护互不关联的清单。格式identity MUST来自唯一正式定义。

绑定及索引 MUST在明确初始化入口验证和缓存；原始文件校验 MUST沿现有唯一解析入口执行，不在每次字段搬运或记录转交时重复重检，也不在OnInspectorGUI进行重操作。Runtime MUST不读取列名、反射或采样Dictionary。原始主行与大几何表 MUST继续沿唯一采样链分别发布，保留紧凑明细和随机查询；搬运映射不得执行第二份Foot数学或生成评分。

字段布局或含义变化 MUST显式升级版本，缺列、重复列、非法类型或不匹配版本 MUST拒绝，不建立旧reader、别名或默认值补全。历史原包及其旧结果 MUST保留为证据，不自动覆盖或用新语义重新解释。现有评分维度、权重、分母和Unavailable规则 MUST保持原Owner。

仅修改内部记录组织或采样映射且列名、顺序、类型和含义均不变时，版本 MUST保持；不得为已经删除的Reach夹紧虚构一组新旧字段或强制ABI迁移。

#### Scenario: 新增普通证据列

- **WHEN** 当前版本新增一个正式响应证据字段
- **THEN** 列名、写值位置、typed读取和必需列校验 MUST由同一绑定得到
- **AND** 不改变质量规则时 MUST不新增评分Target或修改Publisher业务规则

#### Scenario: 列绑定不完整或重复

- **WHEN** 当前格式存在重名、缺失typed读写绑定或类型不一致
- **THEN** 初始化 MUST明确失败，不开始生成看似合法的采样文件
- **AND** MUST不靠空值、零值或忽略该列继续运行

#### Scenario: 仅统一读写映射

- **WHEN** 内部多处手工映射改为同一typed列绑定，但原采样列含义未变
- **THEN** Header、逐字段值、有效性和格式版本 MUST保持不变
- **AND** 原有紧凑分析存储和只读查询 MUST继续工作，不重新生成另一种存储格式
