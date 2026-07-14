# btsmtl-compiled-simulation-program Specification

## ADDED Requirements

### Requirement: BTSMTL gameplay authoring 必须编译为唯一正式 Program

系统 MUST 将 Graph、Node、Edge、StateMachine、ConditionRuleGraph、Timeline、TreeClip、Blackboard declaration 和 Character gameplay catalog 编译为唯一不可变 `CharacterSimulationProgram`。正式 gameplay runtime MUST 只执行 Program operation，MUST NOT clone 或 tick authoring Graph、Node、Timeline、Track、Clip 或 ScriptableObject。单机、ServerAuthoritative 和 DeterministicRollback MUST 共用同一 Program 和 operation 语义。

#### Scenario: 编译 Corin Definition

- **WHEN** 作者编译 Corin `CharacterPipelineDefinition`
- **THEN** Compiler MUST 递归解析全部 inline/shared gameplay source
- **AND** MUST 生成一个 canonical Program、一个 ProgramHash 和一个 Debug Source Map
- **AND** MUST NOT 为不同 Network Model 生成不同业务 Program

#### Scenario: 运行单机角色

- **WHEN** Local Driver 创建 Corin actor
- **THEN** actor MUST 从 compiled Program 和全新 SimulationState 启动
- **AND** MUST NOT创建 RootTree 或 Timeline authoring runtime clone

### Requirement: Program 必须可移植且不包含 Unity runtime 对象

Program bytes MUST 只包含 portable identity、数值、operation、table、state layout、runtime definition 和 source map data。Program MUST NOT保存 `UnityEngine.Object`、AnimationClip、Animancer transition、InputAction、Camera、Transform、Graph runtime object、Network Model、Endpoint、Transport 或 concrete World Solver。Unity client 与纯 .NET server MUST 能读取同一 canonical bytes。

#### Scenario: 纯 CSharp 服务端加载 Program

- **WHEN** .NET authoritative host 加载 Corin Program bytes
- **THEN** loader MUST 不依赖 UnityEngine 或 Unity asset database
- **AND** MUST 得到与客户端相同的 ProgramId、Revision、TickRate 和 ProgramHash

#### Scenario: Timeline 包含动画轨道

- **WHEN** Timeline 同时包含 animation track 与 gameplay TreeClip/motion data
- **THEN** Program MUST只保存 gameplay producer identity、逻辑时间和 portable gameplay data
- **AND** AnimationClip 与 Animancer binding MUST只进入客户端 Presentation projection

### Requirement: Compiler 必须建立显式 operation 与 state layout

Compiler MUST 为每个可执行 authoring source 解析唯一 operation emitter，并为每个影响未来 Tick 的可变值分配显式 state slot。Runtime operation MUST 不在 operation object、authoring node、闭包、static field 或外部 service 中保存隐藏 gameplay state。

#### Scenario: 编译 WaitNode

- **WHEN** authoring source 表达等待 0.5 秒且 Program TickRate 为 60Hz
- **THEN** Compiler MUST 生成 WaitTicks operation 和明确 duration tick
- **AND** elapsed tick MUST 保存于 SimulationState slot
- **AND** runtime MUST 不读取 `Time.deltaTime`

#### Scenario: 编译有状态自定义节点

- **WHEN** node emitter 没有为其可变状态声明 state layout
- **THEN** Compiler MUST 报告精确 source identity 并失败
- **AND** MUST 不通过反射复制私有字段作为 fallback

### Requirement: Compiler 必须保证稳定顺序、身份与内容 Hash

Compiler MUST 使用稳定 authoring identity、containment route、显式 flow order 和 ordinal ordering 生成 operation handle、reference index、state slot 与 bytes。相同 source content、compiler version 和 TickRate MUST 产生相同 ProgramHash；显示名、asset path、dictionary iteration 或编译机器 MUST NOT改变 ProgramHash。

#### Scenario: 客户端与服务端比较 Program

- **WHEN** 两端加载相同 authoring revision 的 Program
- **THEN** ProgramHash MUST相同
- **AND** model session MAY据此允许 actor binding

#### Scenario: source 已修改但 Program 未重新编译

- **WHEN** authoring source hash 与 Program manifest 不一致
- **THEN** Editor 和 runtime MUST报告 stale Program
- **AND** MUST不运行旧 Program 或现场重新解释 authoring source

### Requirement: Compiler 必须声明 Program capability 并拒绝不支持的 operation

Program manifest MUST 声明 Portable、Snapshotable、Deterministic 和 required World Solver capabilities。Compiler MUST拒绝缺失 emitter、断裂引用、非 canonical runtime state、系统时间、Unity Random、直接 Unity Physics、直接 InputAction/Camera/Transform 读取或其它不满足目标 capability 的 gameplay operation。系统 MUST NOT提供 interpreted fallback 或忽略 operation。

#### Scenario: Deterministic Program 使用 Unity Random

- **WHEN** 可达 gameplay node 只能调用 Unity Random 且没有 SimulationState RNG operation
- **THEN** deterministic compile MUST失败并定位该 node
- **AND** Model Inspector MUST不允许 DeterministicRollback 使用该 Program

#### Scenario: Local Program 包含未安装 operation

- **WHEN** Local model 编译遇到没有正式 emitter 的 node
- **THEN** compile MUST同样失败
- **AND** MUST不因 Local model 较宽松而回退旧 node runtime

### Requirement: Authoring 与 Presentation projection 必须保持单一数据来源

Compiler MAY从同一 authoring source 生成 gameplay Program 与客户端 Presentation projection，但两者 MUST通过稳定 source/producer identity 关联，MUST NOT复制 Window、Motion、Action、Transition 或 Blackboard gameplay 语义。Presentation projection MUST只包含客户端表现所需资源定位与采样数据。

#### Scenario: 攻击 Timeline 同时包含 HitWindow 与动画

- **WHEN** Attack1 Timeline 被编译
- **THEN** HitWindow、MotionCurve 和 Action lifecycle MUST只进入 gameplay Program
- **AND** AnimationClip binding MUST只进入 Presentation projection
- **AND** 两侧 MUST通过同一 Timeline/Track/Clip source identity 调试映射

