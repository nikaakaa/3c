## ADDED Requirements

### Requirement: 动画调参字段必须具有唯一交互策略

系统 MUST通过统一Capability与typed Profile tuning catalog把每个动画作者字段分类为`Structural`、`TunableDefault`、`RuntimeInput`或`DerivedReadOnly`。`TunableDefault` MUST声明稳定owner/field identity、typed value kind、单位、范围、有限值规则、`NextFrame | NextActivation`、`PreserveState | ResetOwnerState`和consumer identity。需要改变Projection、Pose Plan topology、workspace容量、Rig/source binding、solver对象、clip/database集合或PhysicsScene的字段 MUST为`Structural`；未分类字段 MUST阻断发布和Live Tuning。人工UI MUST显示业务字段名和应用状态，内部owner identity默认隐藏。

#### Scenario: 作者查看Foot Placement字段

- **WHEN** Workspace选择Foot Placement节点并投影其Profile字段
- **THEN** 每个字段 MUST从统一descriptor取得`Live Now`、`Next Activation`、`Build Required`或`Read Only`策略
- **AND** UI MUST不依据窗口私有字段名猜测策略或显示完整Tuning Layout

#### Scenario: 字段需要重建solver

- **WHEN** 某字段只能通过重新创建solver或改变Native workspace应用
- **THEN** Capability MUST把该字段声明为`Structural`
- **AND** MUST不提供隐式运行时重建或调试旁路

### Requirement: Character Build必须发布固定Tuning Layout与默认参数块

Character Presentation Compiler MUST从唯一正式作者owner闭包生成稳定排序的`CharacterPoseTuningLayout`、layout hash、typed entry table、consumer identity与默认`CharacterPoseTuningParameterBlock`。Layout MUST与Program、Projection、Pose Plan、Rig和workspace容量共同校验。Parameter block MUST只包含有限primitive、enum和已编译dense数据，不得包含Unity Object、ScriptableObject、Transform、反射路径、字符串查找或任意字典。

#### Scenario: 共享Profile被多个节点消费

- **WHEN** 多个Pose operation引用同一个Profile字段
- **THEN** Layout MUST只发布一个稳定owner field entry和确定性consumer集合
- **AND** Runtime MUST不为每个节点复制一份mutable Profile配置

#### Scenario: 显式Character Build完成

- **WHEN** 作者显式执行Character Build
- **THEN** Projection MUST发布当前作者数据生成的Layout、默认block与parameter revision
- **AND** MUST不携带上一Play session的Editor candidate

### Requirement: Editor必须提交完整有界candidate而不是任意patch

一次成功的typed Authoring Mutation后，Editor Live Tuning Compiler MUST从当前完整owner闭包生成一个candidate block。Candidate MUST包含target、Program、Projection、Pose Plan、Rig、TuningLayout identity、source authoring revision与candidate revision，并在发送前完成全部字段和组合约束校验。Runtime每个目标 MUST至多保留一个Pending candidate和一个Active block；新candidate只替换尚未应用的Pending，不得对Active block逐字段写入。

#### Scenario: 作者连续拖动数值

- **WHEN** UI合并同一字段的连续ChangeEvent
- **THEN** 每次发送到目标的candidate仍 MUST是完整typed block
- **AND** Runtime MUST不观察到组合约束的半更新状态

#### Scenario: Candidate编译失败

- **WHEN** 当前作者闭包无法生成有效candidate
- **THEN** 作者数据 MUST保留为Invalid或Unpublished并显示精确字段错误
- **AND** 当前目标 MUST继续使用旧Active block

### Requirement: 调参参数块必须在表现帧开始处原子交换

Runtime MUST在PresentationFrame读取Fact并调度任何Pose operation之前验证并原子交换Pending parameter block。Animancer Evaluate Barrier之后 MUST禁止应用candidate。`NextFrame`字段 MUST从下一帧开始被consumer读取；`NextActivation`字段 MUST只被下一次transition、BlendStack entry或Inertialization generation捕获。`ResetOwnerState` MUST只重置声明的node-local有限状态，不得重建Rig、source、PlayableGraph、solver或整个Animation Runtime。

#### Scenario: 有效NextFrame candidate到达

- **WHEN** target identity、全部compiled identity与layout hash精确匹配且本帧尚未开始Prepare
- **THEN** Runtime MUST在本帧Pose operation前交换完整page
- **AND** 全部相关consumer MUST在同一帧看到同一candidate revision

#### Scenario: Candidate在Evaluate后到达

- **WHEN** candidate在当前帧Animancer Evaluate Barrier后到达
- **THEN** Runtime MUST保留它供下一PresentationFrame处理
- **AND** MUST不修改当前帧job、Full Body IK solve或FinalPublication使用的值

#### Scenario: 交换前验证失败

- **WHEN** runtime adapter、layout或identity校验失败
- **THEN** Runtime MUST保持旧Active block且不Fault当前Animation Runtime
- **AND** MUST发布typed拒绝原因

### Requirement: Preview与Live调参必须绑定一个精确目标

PoseGraph target选择 MUST只提供当前精确`Preview Instance`和RuntimeDebugSession已经解析为匹配的`Live Actors`。Live tuning MUST只作用于当前选中的一个目标，并复用RuntimeDebugSession target identity；不得扫描Scene、按名称或顺序查找Actor、记忆已销毁Host或向全部Profile消费者广播。Live Gameplay Fact与Runtime Input MUST保持只读。

#### Scenario: 场景中有多个匹配Actor

- **WHEN** RuntimeDebugSession报告多个精确匹配Actor
- **THEN** 作者 MUST显式选择一个Live Actor
- **AND** 系统 MUST显示当前目标并只向该target提交candidate

#### Scenario: Live target revision失配

- **WHEN** target的Program、Projection、Pose Plan、Rig或Layout identity发生变化
- **THEN** Pending、Applied与Graph overlay绑定 MUST失效并显示Stale或Rejected
- **AND** MUST不自动附着到其它Actor

### Requirement: Tunable修改必须保存正式owner并只在当前selection显示

`TunableDefault`修改 MUST通过其唯一typed Mutation直接保存正式owner并进入Undo。PoseGraph Details MUST只为当前Node、State或Transition显示其Authoring值、当前target Applied值、应用状态和精确错误；Workspace MUST不增加全局Tuning Layout表、Apply、Reset、Debug Profile、临时Override Asset或Profile专用Live窗口。纯Tunable修改 MUST保留当前Projection与Pose Plan可执行，只把发布参数状态标记为Unpublished，并允许identity相同的Preview或Live target执行candidate；Structural修改 MUST继续使Projection或Pose Plan Stale并要求显式Build。

#### Scenario: 作者修改并撤销Tunable字段

- **WHEN** 作者修改当前节点Tunable字段后执行Undo
- **THEN** 正式owner MUST恢复并重新生成完整candidate
- **AND** 当前selection Details MUST更新作者值、Applied值与应用状态

#### Scenario: 当前target结束

- **WHEN** Play退出、target销毁或Projection替换
- **THEN** Workspace MUST清空旧Applied值和candidate绑定
- **AND** 正式作者值 MUST继续保存在唯一owner中

#### Scenario: Layout已经变化

- **WHEN** Tunable candidate的LayoutHash与目标已发布Layout不同
- **THEN** 系统 MUST拒绝candidate并显示Build Required
- **AND** MUST不把字段patch映射到近似offset

### Requirement: 首批现有Pose能力必须使用共享调参合同

Foot Placement中不改变hit/path workspace容量的Grounding与Predictive数值、Full Body IK的iterations/FABRIK/spine/body/chain/limb/bend/node weight、Blend类节点默认Weight和Sequence Play Rate MUST使用`NextFrame`。Transition duration与内置blend数学、固定容量的BlendStack和Inertialization数值 MUST使用`NextActivation`。Rig、骨骼、Calibration、Source Slot、Profile引用、clip/database/source set、sample集合、solver binding、query capacity与team topology MUST保持`Structural`。已连接的正式Runtime Input MUST优先于default并在Live中只读显示。

#### Scenario: 调整Foot Placement pelvis参数

- **WHEN** 作者在精确Preview或Live target上修改当前Foot Placement节点可调pelvis数值
- **THEN** 正式Foot Placement consumer MUST从下一帧Active block读取该值
- **AND** MUST不直接修改FinalIK组件或创建第二solver

#### Scenario: 调整Transition duration

- **WHEN** 当前transition已经激活且作者修改其默认duration
- **THEN** 当前generation MUST保留激活时捕获的duration
- **AND** 下一次transition activation MUST使用新值

### Requirement: 调参失败必须保留正式单链路

Authoring Mutation失败 MUST不保存数据或生成candidate；candidate编译失败 MUST保留旧Active block；target失配 MUST拒绝且不选择fallback；candidate应用后发生的Pose stage失败 MUST继续遵循现有PresentationFrame事务，在Barrier前Discard Pending、Barrier后阻断FinalPublication并进入Faulted。退出Play、target结束、Projection替换或domain reload MUST清除Editor candidate绑定，新实例 MUST从正式published block开始。

#### Scenario: Candidate应用后Foot Placement stage失败

- **WHEN** 本帧已经采用candidate且Foot Placement在Animancer Evaluate Barrier后失败
- **THEN** FinalPublication MUST被阻断且Animation Runtime MUST进入Faulted
- **AND** Live Tuning MUST不恢复状态或Physical Bone快照
