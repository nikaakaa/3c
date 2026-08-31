# character-foot-placement-presentation Specification

## Purpose

定义Corin Landing Prediction、Ground Path、Foot Lifecycle、Support、Pelvis、Goal Contribution与唯一FinalIK FBBIK之间的正式表现边界，不固定Foot模块内部类名和聚合方式。

## Requirements

### Requirement: Foot Placement必须是唯一Goal事务

唯一`CharacterPoseConstraintRuntime` MUST为每个Actor和表现帧建立匹配Frame、Completion与Rig lineage的Pending根Bank。根Runtime MUST只管理阶段顺序、lineage、页所有权、Seal、Discard、Invalidate和失败传播，不得实现Foot、Pelvis、Goal或Solver数学。

Foot Placement MUST作为一个深模块接收同帧不可变Frame Input并发布一个`CharacterFootPlacementResult`。调用方 MUST不编排Landing Observation、Ground Path、Foot Lifecycle、Support、Pelvis或Goal编码，也不得取得它们的可变状态。模块内部 MAY按职责拆分实现，但全部职责 MUST共享同一根事务、只发布一个Resolved Foot Pair和一组三个typed Goal Contribution。

#### Scenario: 正常生成Foot Placement结果

- **WHEN** 同一表现帧具有合法Component Pose、Foot输入、Body、World Query、Profile和根Pending Bank
- **THEN** Foot Placement MUST生成同Frame、Completion与Rig lineage的Resolved Foot Pair、Pelvis Result和三个Goal Contribution
- **AND** 调用方 MUST不取得或逐个提交Foot内部状态、Ground Path、Pelvis或Solver状态

#### Scenario: 重复执行Foot Placement

- **WHEN** 同一Frame与Completion第二次请求Foot Placement Prepare
- **THEN** 根Runtime MUST报告非法调用顺序并阻止整帧发布
- **AND** MUST不建立第二Foot Placement事务

### Requirement: Landing Prediction必须形成独立世界事实

每只脚 MUST从typed Step Event、committed Body Target世界速度、Timeline段边界与Continuation、KCC Future Body Translation和本帧可见姿态生成Raw Landing。Raw Landing MUST从本帧输入重新投影，不得旋转或平移上一帧查询结果，也不得外推没有正式Plan的Future Body Yaw。

Runtime MUST从Side、Landing Event、量化Raw Landing、量化Component Up、Profile Revision与World Revision构造canonical Landing Observation Key。相同Key MUST复用根事务已提交的不可变Accepted或Rejected Observation；新Key MUST恰好查询一次，并在固定容量合法候选中按距离与稳定identity选择canonical最近命中。上一Committed Surface和其它历史状态 MUST不进入Key或候选选择。

查询 MUST过滤自身Collider、初始重叠、非法点、非法法线与超坡度命中。容量溢出或没有合法命中 MUST发布typed拒绝，不得沿用另一Key、旧Landing或默认Surface。

#### Scenario: 相同Landing Observation Key

- **WHEN** 当前帧生成的canonical Landing Observation Key与上一Committed Page相同
- **THEN** Runtime MUST复用相同Observation identity、Surface、点、法线或Reject结果
- **AND** MUST不执行SphereCast或读取上一Surface重新选择候选

#### Scenario: 新Landing Observation Key

- **WHEN** canonical Raw Landing、Component Up、Event、Profile Revision或World Revision产生新Key
- **THEN** Runtime MUST执行一次SphereCast并产生canonical最近合法候选或typed拒绝
- **AND** Pending事务失败时 MUST丢弃该Observation，不得污染上一Committed Page

### Requirement: Foot Lifecycle必须生成唯一权威结果

每只脚 MUST在同一根事务内只有一份权威离散State、一份权威连续Correction和一个最终Resolved Foot。Foot模块内部 MAY把typed持久状态、Transition判定、State Target、时间连续化、Anchor和Hard Constraint拆成独立组件；spec不固定类名、对象数量或Context聚合形态。

拆分后的每项持久字段和每项Decision MUST只有一个写入Owner。Transition判定 MUST不推进Residual或Interpolation时间；时间连续化 MUST不选择离散State；Hard Constraint MUST不反向改写Transition。所有内部组件 MUST共享根Bank的Prepare、Seal和Discard，不得形成第二生命周期、第二输出路径或图外Goal后处理。

#### Scenario: 内部责任拆分

- **WHEN** Foot实现把Transition、Target和时间连续化拆成独立组件
- **THEN** 相同Frame Input和上一Committed状态 MUST只产生一份离散State、一份Effective Correction和一个Resolved Foot
- **AND** 任一内部组件 MUST不能独立提交或绕过根事务

#### Scenario: 整帧Discard

- **WHEN** Foot内部Pending状态已经更新但后续Goal或Solver阶段失败
- **THEN** 上一Committed Foot状态、Correction、Anchor和Path MUST保持不变
- **AND** 下一帧 MUST不读取被丢弃的内部结果

### Requirement: Foot Placement配置与Rig必须显式

FootPlacement节点 MUST显式引用唯一Profile与Calibration。Projection、Profile、Calibration、Rig v4和Animation Rig Binding的identity与revision MUST精确匹配；PhysicsScene、World-Aware Binding或正式Future Body Translation source缺失时 MUST报告不可用，不得从Transform名称、Animator Avatar、旧Prefab组件或默认配置补全。

#### Scenario: Projection与Calibration不匹配

- **WHEN** Projection保存的Calibration identity或revision与Runtime资产不同
- **THEN** Runtime创建 MUST失败并报告stale identity
- **AND** MUST不继续使用旧Goal

### Requirement: Foot Placement必须与Gameplay和Network隔离

Landing、Goal、查询命中和diagnostics只属于Presentation。它们 MUST不进入Character State、World State、Gameplay Fact、Blackboard、Snapshot、Hash或网络packet，也 MUST不写VisualRoot或Gameplay Body。

#### Scenario: 两端显示同一角色

- **WHEN** 两个客户端以不同Presentation时刻显示同一committed Body
- **THEN** 两端 MAY独立计算Landing diagnostics
- **AND** 结果 MUST不改变Gameplay或网络确认

### Requirement: Foot Placement诊断必须只显示正式结果

Runtime Result MUST与Diagnostics严格分型。Diagnostics MUST从同一Frame、Completion、Rig和Bank lineage的Committed Observation、Foot状态、Resolved Result和后续阶段Result单向深冻结正式事实。Gizmo、CSV、Trace与Pose Watch MUST只读取这些Committed页，不得查询世界、修改Foot状态、选择Support、生成Goal或执行FBBIK。

#### Scenario: 捕获正式Foot事实

- **WHEN** Foot、Pelvis、Goal、FBBIK和Pending Pose完成验证并Seal
- **THEN** Diagnostics MUST发布同lineage的输入、Transition、Target、连续Correction、Hard Constraint、Resolved、Solved和Physical事实
- **AND** Diagnostics命名或布局变化 MUST不改变Runtime Result

### Requirement: Ground Path必须使用上一已提交落点与下一事件落点

每只脚 MUST按Landing Event identity维护LastLanding与NextSwingLanding。PreSwing或Swing阶段每帧 MUST重新投影Raw Landing并构造canonical Observation Key；只有新Key执行一次SphereCast，相同Key复用Committed Observation。新Observation低于正式更新死区时 MUST保留NextSwingLanding与Ground Path；达到死区时 MUST提交新落点并重建Path。事件完成后最新NextSwingLanding MUST晋级为LastLanding。

Ground Path MUST只使用LastLanding与NextSwingLanding构造查询输入。没有LastLanding时 MUST发布`CurrentLandingUnavailable`，不得用Animated Sole、Transform、固定高度或默认地面补起点。

#### Scenario: 相同Observation持续多个表现帧

- **WHEN** PreSwing或Swing连续帧产生相同canonical Observation Key
- **THEN** Runtime MUST复用Committed Observation、NextSwingLanding与Committed Ground Path
- **AND** MUST不执行新的SphereCast或Capsule Ground Detection

#### Scenario: 新Observation达到更新死区

- **WHEN** 新Key产生的Accepted Observation与同Event NextSwingLanding距离达到正式更新死区
- **THEN** Runtime MUST提交新NextSwingLanding并重建同一Foot事务中的Ground Path
- **AND** Ground Path MUST消费该Observation，不得执行第二次Landing查询

### Requirement: Ground Detection必须发布原始Capsule接触集合

Ground Detection MUST沿LastLanding到NextSwingLanding构造唯一Capsule请求。请求 MUST显式携带轴端点、Component Down、半径、查询距离、最大轴段长度、Ground Layer、分段命中容量与整条路径Contact容量。Backend MUST按最大轴段长度确定性切分轴并执行真实Capsule Cast，过滤自身Collider、初始重叠、非法几何和同分段重复命中，并发布原始位置、法线、Surface、分段索引、查询距离和稳定candidate identity。

Backend MUST不把接触集合预先压成单个落点，不得改用Raycast、SphereCast或第二种查询算法。没有合法接触或固定容量溢出 MUST发布typed rejection，不得生成默认地面。

#### Scenario: Capsule命中多个表面

- **WHEN** 分段Capsule Cast命中多个合法表面
- **THEN** Backend MUST在固定容量页中保留各接触的位置、法线和identity
- **AND** MUST不先压成中心线或单一Surface

### Requirement: Ground Envelope必须来自可达Edge与上侧凸包

Ground Envelope Builder MUST把Raw Contacts投影到脚步纵向与Component Up组成的二维平面，稳定生成Edge候选并在同一路径距离保留最高候选。Path Start与Target Landing MUST作为首尾端点保留；`CastAbove`和`CastBelow`只属于查询范围，不得成为Reachability限值。

正式Profile MUST提供米制`MaximumReachableVerticalEdge`。任一Edge超过限值时 MUST发布`UnreachableEdge`与首个Invalid Segment，不得删除障碍后继续构造Hull、沿用旧Envelope或借用KCC Step高度和腿长替代。全部Edge合法时，Builder MUST输出位于所有保留候选上侧或与其重合的连续上侧Convex Hull。Envelope只表达feet-only地面下界，不改变Foot XZ或驱动Pelvis。

#### Scenario: 路径经过不可达垂直面

- **WHEN** 任一Edge沿Component Up的高度超过`MaximumReachableVerticalEdge`
- **THEN** Ground Path MUST发布`UnreachableEdge`且Accepted Envelope为空
- **AND** Raw Contacts与Edge事实 MUST保留在同一成功Seal的只读诊断页

### Requirement: Ground Path与Foot持续状态必须保持抽象和实现分离

Foot核心 MUST只依赖World Query合同、Ground Envelope Builder和预分配Observation页。Unity Adapter只执行查询与固定容量写入，不得选择Foot State、保存持续状态、推进Interpolation、创建Anchor、构造Pelvis或写Goal。Foot业务只消费不可变Observation，不得直接访问Unity查询对象。

跨帧状态 MUST保存在固定布局typed记录中，并 MAY按Transition、Interpolation、Anchor和Path责任分区；每个字段 MUST只有一个写入Owner。系统 MUST不使用字符串Key、共享Dictionary、Gameplay Blackboard、动态字段或可变Diagnostics保存Foot状态。全部状态页 MUST由根Bank统一提交或丢弃，任一内部组件不得拥有独立Committed/Pending生命周期。

#### Scenario: 分型状态共同提交

- **WHEN** Transition、Interpolation、Anchor与Path分别产生Pending typed状态
- **THEN** 根Bank MUST在完整Foot、Goal和Solver闭包合法后一次Seal
- **AND** 任一分区 MUST不能单独提交、回退到旧Context或从Diagnostics恢复状态

### Requirement: Future Body Translation必须写入固定Workspace

Foot Placement MUST为每个根Bank预分配固定容量Future Body Translation Workspace，并把它交给正式Translation Source写入。Translation Source MUST只更新有效Sample数量和内容，不得为每次活跃预测新建Trajectory对象、临时Sample数组或复制Sample集合。

#### Scenario: 同一帧左右脚请求未来Body平移

- **WHEN** 左右脚需要同一Body、Timeline与Duration范围的未来平移
- **THEN** Foot模块 MUST在本帧只填充一次Pending Workspace并让两脚读取同一只读结果
- **AND** 预测不得产生托管堆分配

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
