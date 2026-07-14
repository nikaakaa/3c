## MODIFIED Requirements

### Requirement: Transition 动画混合元数据必须属于 Transition 边

状态切换的动画策略、时长和曲线 MUST 作为 `AnimationTransitionDefinition` 内联保存于 `StateMachineGraph` Transition edge。Definition MUST 显式选择 `Immediate`、`ContributionCrossFade` 或 `Inertialization`，MUST NOT 根据时长、动画片段、source/target 类型或缺失配置隐式推断策略。Condition rule graph MUST 继续只表达纯 Bool 条件，MUST NOT 保存动画 Transition 字段，也 MUST NOT 创建 `TransitionNode`、Timeline 行为节点、ExposedProperty 或旧动画策略引用来表达切换表现。

#### Scenario: 配置 Immediate Transition

- **WHEN** 作者选中一条需要硬切的 Transition edge
- **THEN** Inspector MUST 显示 `Immediate` strategy
- **AND** duration MUST 固定为 0
- **AND** curve 编辑 MUST 不作为该策略的有效配置

#### Scenario: 配置 ContributionCrossFade Transition

- **WHEN** 作者为普通 locomotion edge 选择 `ContributionCrossFade`
- **THEN** Inspector MUST 显示 duration 和 curve
- **AND** duration MUST 大于 0
- **AND** definition MUST 随 edge 内联序列化

#### Scenario: 配置 Inertialization Transition

- **WHEN** 作者为攻击、闪避或高频抢占 edge 选择 `Inertialization`
- **THEN** Inspector MUST 显示 duration、curve 和正式 rig binding 摘要
- **AND** duration MUST 大于 0
- **AND** 系统 MUST NOT 为缺失 rig 自动改用 CrossFade

#### Scenario: Condition rule 求值

- **WHEN** runtime 求值 Condition rule graph
- **THEN** rule graph MUST 只返回该 Transition 是否可通过
- **AND** rule graph MUST NOT 决定 strategy、duration、curve、outgoing contribution 或 outgoing pose

### Requirement: StateMachine runtime 必须发布切换混合事实且不双 tick 状态

StateMachine runtime 命中 Transition 后 MUST 创建稳定的 animation transition instance identity，并发布包含 runtime activation scope、source owner、target owner 或 Empty、`AnimationTransitionDefinition` 和 stop/release cause 的正式 request。StateMachine MUST 在同一逻辑 barrier 内完成 source State 退出并激活 target State，MUST NOT 等待表现 Transition，MUST NOT 为获得 outgoing 动画继续 tick source State body。

#### Scenario: 命中指向 State 的 Transition

- **WHEN** active State 命中带 AnimationTransitionDefinition 的 Transition
- **THEN** runtime MUST 发布 source -> target animation transition request
- **AND** source State MUST 在逻辑层完成退出
- **AND** target State MUST 进入 active lifecycle
- **AND** source 与 target State body MUST NOT 因动画混合被同 Tick双重推进

#### Scenario: Transition 指向 Exit

- **WHEN** active State 命中指向 Exit 的 Transition
- **THEN** runtime MUST 发布 source -> Empty request
- **AND** request MUST 使用该 Exit edge 的显式 definition
- **AND** runtime MUST NOT 将 Exit 退化为无策略 `ReleaseOwner`

#### Scenario: target 首次执行

- **WHEN** target State 的 OnEnter 或 Root producer 首次获得正式 tick 机会
- **THEN** runtime MUST 为该 transition target 提交 TargetReady
- **AND** TargetReady MUST NOT 以 target 是否提交 animation contribution 为条件

## ADDED Requirements

### Requirement: StateMachine 上层停止必须携带明确动画 release 语义

父 Tree 对 StateMachineNode 的 graceful stop 或 replacement MUST 通过正式 stop context 提供 source -> Empty 的 `AnimationTransitionDefinition`。ForceStop、pipeline deactivate 和 dispose MUST 显式使用 `Immediate` release。系统 MUST NOT 用隐藏默认时长、默认 CrossFade 或保留旧 source 来补齐缺失配置。

#### Scenario: 父 Tree graceful replacement

- **WHEN** 上层 Selector 用 replacement child graceful 抢占 StateMachineNode
- **THEN** stop context MUST 携带明确 AnimationTransitionDefinition
- **AND** StateMachine runtime MUST 发布 source -> Empty request
- **AND** source State 逻辑 MUST 在 stop barrier 内关闭

#### Scenario: ForceStop

- **WHEN** StateMachineNode 收到 ForceStop、deactivate 或 dispose
- **THEN** runtime MUST 发布 Immediate source -> Empty request
- **AND** 表现层 MUST 原子释放对应 transition 与 contribution 资源

#### Scenario: graceful stop 缺少 definition

- **WHEN** 上层要求 graceful stop
- **AND** stop context 没有可追溯 AnimationTransitionDefinition
- **THEN** validator 或 runtime MUST 报告配置错误
- **AND** 系统 MUST NOT 自动选择动画策略

