## MODIFIED Requirements

### Requirement: Operation Evaluate 必须只有一个事务入口

每个 Numeric Target 的 Kernel MUST通过唯一 Operation Evaluator 完成一次 Actor/Tick Evaluate。Evaluator MUST按正式顺序处理 ingress、GE 推进、输入请求、活动 Timeline Decision、C# 角色控制决策、Skill 内容、Motion 合成、GE 保存、Blackboard 清理及输出收集，返回唯一 staged state 和正式结果。Skill 中的 Timeline/Tree MUST接入共用执行模块，但状态访问与输出仍属于当前角色事务；不得使用非 Skill owner 的独立帧循环。领域模块不得隐藏跨 Tick Gameplay 状态。

#### Scenario: Float32 Local Tick

- **WHEN** Kernel 对 Corin 执行一个 Float32 Evaluate
- **THEN** MUST只创建一个 evaluation transaction，代码控制、技能 Timeline/Tree、Action、Blackboard 和 GE 在其中推进
- **AND** 任一模块失败时 MUST不返回部分状态或外部输出

### Requirement: Operation 领域模块必须拥有明确输出权限

Operation runtime MUST将输入/值、Blackboard、Action、Timeline 调用接入、GE 与 Motion 分配给明确领域模块，通过窄 state/query/sink 合同协作，不得取得万能可变上下文或另一个模块的具体实现。通用 Tree 控制与 Timeline 时间执行 MUST不直接产生 Animation、Camera、Cue、GE、Motion 或 Network 输出；相应领域片段执行模块 MUST经当前角色正式端口提交结果。Timeline 和 Locomotion MUST不生成最终 WorldSolver result。

#### Scenario: Timeline 采样攻击动画和位移

- **WHEN** 技能片段在当前 Tick 采样 Animation producer 和 MotionCurve
- **THEN** 对应领域模块 MUST分别提交 Presentation command 与 Motion contribution
- **AND** 时间核心 MUST不直接修改子树游标、BodyState 或调用 WorldSolver

## ADDED Requirements

### Requirement: 共用 Timeline 状态必须由角色事务完整拥有

Skill Timeline 的播放时间、cycle、activation、片段/TreeClip/子图状态、参数、等待及停止进度 MUST全部映射到当前 ActionInstance 的 SkillExecutionState，并受同一角色事务、Snapshot、codec 和 hash 管理。共用执行只使用本 Tick 有效的状态视图，MUST不创建独立提交、捕获长期事务对象或保存镜像 Action Context。

#### Scenario: Timeline 已推进但世界结果失败

- **WHEN** 同一角色 Evaluate 已推进 Timeline，而 Finalize 因世界结果不匹配失败
- **THEN** Timeline 与子树状态 MUST随整个角色事务丢弃，原 committed 状态不变
