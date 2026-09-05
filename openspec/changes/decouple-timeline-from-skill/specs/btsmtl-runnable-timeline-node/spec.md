## MODIFIED Requirements

### Requirement: TimelineNode 是普通可执行节点

系统 MUST提供 TimelineNode 作为 Graph 中请求执行 Timeline 的普通节点，默认通过正式 ownership 持有可立即编辑的 inline 内容，并允许显式使用 shared 资产。节点 MUST只暴露输入控制流 port，不得暴露、保存或解析输出控制流 port。节点 MUST只表达内容引用、声明输入的绑定和播放选择，复用共用编译执行；MUST不成为播放器、状态机同层 State 或独立 Timeline 构建必需的假根。

#### Scenario: 创建 TimelineNode

- **WHEN** 用户在技能 Tree、技能局部状态行为或其他正式允许的编译树中创建 TimelineNode
- **THEN** 节点 MUST自动拥有 inline 内容，作者能立即下钻编辑轨道、片段和 TreeClip
- **AND** MUST不要求先创建 TimelineAsset

#### Scenario: 显式复用 Timeline

- **WHEN** 作者提取 shared 内容或选择已有 shared Timeline
- **THEN** 节点 MUST切换为 Shared Asset ownership 并清理原 inline 真数据
- **AND** UI MUST明确显示 Shared Asset

#### Scenario: Timeline 播放完成

- **WHEN** 节点调用的 Timeline 返回成功
- **THEN** 节点 MUST返回 Success，MUST不执行输出控制流目标

### Requirement: TimelineNode 生命周期映射 Timeline 播放

TimelineNode MUST编译为调用 operation 和对共用 Timeline 内容的引用。进入、推进、循环、完成、停止和释放 MUST映射到所属调用方的 typed 实例状态；Skill 调用 MUST归属当前 ActionInstance 的 SkillExecutionState，非 Skill 编译树调用 MUST归属其正式播放 owner。状态 MUST不由作者节点、Timeline 数据或树 clone 持有。

#### Scenario: TimelineNode 完成

- **WHEN** 内容到达调用终点且必要退出完成
- **THEN** 执行层 MUST从本次调用状态产生完成结果，而不是从共享资产读取运行状态

### Requirement: Timeline 请求入口来自正式执行上下文

Compiled TimelineNode MUST通过其当前正式执行范围创建、查询、停止和释放本次 Timeline 调用。该范围 MUST显式提供内容所需的绑定和状态；Skill 自动沿当前角色/释放获得既有信息。节点 MUST不访问旧播放器、作者对象 scheduler、具体场景组件、Presentation 对象或全局服务，也不得另建调用循环。

#### Scenario: Operation 创建 Timeline request

- **WHEN** 节点首次进入
- **THEN** MUST在当前 owner、调用点和 activation 的状态范围中建立请求
- **AND** Skill 请求 MUST进入所属 ActionInstance 的技能状态

#### Scenario: Program 缺失 Timeline 数据

- **WHEN** 请求引用的编译内容不存在
- **THEN** 构建或开始 MUST明确失败，MUST不寻找 TimelineAsset 作为运行替代

### Requirement: TimelineNode 播放状态隔离

调用方、播放实例、调用点、activation generation 和 cycle MUST共同区分 Timeline 状态。Skill 还 MUST关联 Actor 和 ActionInstance。共享作者内容或编译定义 MUST不导致任何两个独立调用共用进度、变量或停止状态。

#### Scenario: 两个角色播放同一 Timeline

- **WHEN** 两个 Actor 使用相同技能 Timeline 内容
- **THEN** 播放状态 MUST分别位于各自 ActionInstance 的技能执行范围

#### Scenario: 两个非 Skill 调用引用同一 Timeline

- **WHEN** 两个正式非 Skill owner 调用相同内容
- **THEN** 两次播放 MUST可独立推进和停止，不创建 ActionInstance 来区分身份

### Requirement: 保留 Timeline 驱动 Tree 链路

TreeTrack/TreeClip MUST编译为共用 Timeline 的 Decision/Commit 内容。Skill 的 Decision MUST在角色代码控制决策前仅产生当前释放/调用的 Frame 候选，Commit MUST在正式技能阶段执行完整进入、更新、退出和销毁。非 Skill MUST按正式调用方帧的 Decision/Commit 顺序使用相同树控制；Decision 不允许 Running 或副作用。系统 MUST不恢复作者对象 Bind/Evaluate/Unbind 自主 Gameplay 路径。

#### Scenario: Decision TreeClip 穿过 Loop 边界

- **WHEN** 一个 SimulationTick 穿过技能 Timeline 循环边界
- **THEN** MUST按尾段、完整 cycle、头段顺序处理
- **AND** 当前释放的 Frame 候选 MUST保持唯一并可被同 Tick 角色控制读取

#### Scenario: 非 Skill 使用 TreeClip

- **WHEN** 独立调用满足编译子树的全部依赖
- **THEN** 子树 MUST使用本次播放的绑定与状态，并随父播放停止

### Requirement: Timeline 动作事实必须来自 Timeline 轨道采样

用于技能的 Timeline gameplay 片段 MUST通过其正式领域采样产生 ActionWindow、MotionContribution 和 typed facts。Animation/Cue 资源 MUST通过已提交 Presentation command 与 Projection 定位，不得进入 gameplay state。通用时间执行 MUST不强制产生这些角色结果；非 Skill 缺少角色领域能力时不得生成等价伪造事实。

#### Scenario: Attack Cancel Window

- **WHEN** 技能 Decision TreeClip 命中 Cancel Window
- **THEN** MUST写入正式当前 Frame declaration 并投影关联本次 ActionInstance 的窗口候选

### Requirement: TimelineNode 完成状态必须保持请求语义

Compiled TimelineNode MUST根据本次请求状态更新其执行结果，MUST不直接驱动控制状态转换或解释 Action 生命周期。自然完成、graceful stop 与 force stop MUST使用统一停止语义。技能 Action 的有效性检查 MUST由 Skill 接入执行；非 Skill MUST使用其真正 owner 的有效性和停止来源，通用时间模块不得要求 Action 字段。

#### Scenario: Once Timeline 完成

- **WHEN** 请求到达成功终态
- **THEN** 节点 MUST返回 Success；后续技能内容或局部状态转换由其原控制流程决定
- **AND** MUST不直接结束整个 ActionInstance

#### Scenario: 播放中的 Action Context 终止

- **WHEN** 活动 Skill Timeline 对应的 ActionInstance 终止或失效
- **THEN** Skill 接入 MUST在继续正常 Decision/Commit 采样前启动原 ActionContextEnded 停止
- **AND** MUST经共用 stop barrier 释放该实例的 producer 与 camera 来源
- **AND** MUST不再产生旧实例的正常窗口、运动、Cue 或结果
- **AND** stop barrier 完成后节点 MUST进入完成终态，MUST不推导或补写 Action 生命周期

#### Scenario: ForceStop

- **WHEN** 所属 Session/Actor 或正式非 Skill owner 销毁并强制停止
- **THEN** 本次调用状态与子内容 MUST完成强制清理，MUST不等待动画 fade 或网络确认
