# taco-sm-node-authoring Specification

## Purpose
定义 Taco 状态机创作链路：普通行为图通过 `StateMachineNode` 进入 `StateMachineGraph`；`StateMachineGraph` 只表达状态关系；`StateNode` 表达同层普通状态；状态具体行为在 `StateNode` 引用的 `SubTree` 或 `StateBehaviorSubTree` 中编辑。

## Requirements
### Requirement: 状态机层级角色分离
系统 MUST 使用 `StateMachineNode` 表达父级行为图进入状态机图的入口，使用 `StateMachineGraph : BaseTree` 表达状态结构，使用 `StateNode` 表达状态机图内普通状态。状态行为 MUST 位于 `StateNode` 引用的 `SubTree` 或 `StateBehaviorSubTree` 中。

#### Scenario: 父级行为图创建入口
- **WHEN** 用户在普通行为图中创建状态机入口
- **THEN** 创建结果 MUST 是 `StateMachineNode`
- **AND** 普通行为图 MUST NOT 创建 `StateNode`、`Enter`、`AnyState` 或 `Exit`

#### Scenario: 状态机图创建状态
- **WHEN** 用户在 `StateMachineGraph` 中创建 Idle、Walk 或 Attack
- **THEN** 创建结果 MUST 是 `StateNode`
- **AND** 系统 MUST NOT 创建业务特化状态节点

### Requirement: StateMachineGraph 只表达同层状态结构
系统 MUST 让 `StateMachineGraph` 只包含 `Enter`、`AnyState`、`Exit`、`StateNode` 和条件用 `ValueNode`。它 MUST NOT 创建 `RootNode`、`OnEnter`、`OnExit`、`StateMachineNode`、普通 `RunnableNode`、Timeline 行为节点或 Tree 行为节点。

#### Scenario: 创建规则统一
- **WHEN** 编辑器、拖拽、粘贴或脚本路径向 `StateMachineGraph` 创建节点
- **THEN** 系统 MUST 统一通过 `CanCreateNodeType()` 判定
- **AND** 非法节点 MUST NOT 进入正式节点集合

#### Scenario: 控制节点唯一
- **WHEN** 用户打开或校验 `StateMachineGraph`
- **THEN** 每层 MUST 且只能包含一个 `Enter`、一个 `AnyState` 和一个 `Exit`
- **AND** 每层 MUST 至少包含一个 `StateNode`

### Requirement: StateNode 下钻状态行为 SubTree
系统 MUST 允许 `StateNode` 通过正式状态行为引用模块引用普通 `SubTree` 或 `StateBehaviorSubTree`。`StateNode` MUST NOT 在 `StateMachineGraph` 本层暴露 `Behavior` flow port，也 MUST NOT 直接引用子 `StateMachineGraph`。

#### Scenario: State 下钻到行为图
- **WHEN** 用户打开 Idle `StateNode` 的状态行为引用
- **THEN** 编辑器 MUST 打开该 `SubTree`
- **AND** 用户 MUST 能在该 SubTree 中创建 Timeline、Action、Composite、Decorator、Tree 引用或嵌套 `StateMachineNode`

#### Scenario: 没有状态行为
- **WHEN** active `StateNode` 没有配置状态行为 `SubTree`
- **THEN** 该状态 MUST 保持 `Running`
- **AND** 状态切换 MUST 继续由同层 Transition 决定

### Requirement: StateBehaviorSubTree 提供状态生命周期入口
系统 MUST 让普通 `SubTree` 只表达 `RootNode` 行为入口。`StateBehaviorSubTree` MUST 固定拥有 `OnEnter`、`RootNode` 和 `OnExit` 生命周期入口。`OnEnter` 和 `OnExit` MUST 使用普通 `RunnableNode` flow 链路，MUST NOT 成为 `StateMachineGraph` Transition 端点。

#### Scenario: 普通 SubTree
- **WHEN** 用户创建普通 `SubTree`
- **THEN** 新图 MUST 默认包含一个 `RootNode`
- **AND** 新图 MUST NOT 默认包含 `OnEnter` 或 `OnExit`

#### Scenario: StateBehaviorSubTree
- **WHEN** 用户创建 `StateBehaviorSubTree`
- **THEN** 新图 MUST 默认包含一个 `OnEnter`、一个 `RootNode` 和一个 `OnExit`
- **AND** 缺失或重复生命周期入口 MUST 被校验报告为非法结构

#### Scenario: 进入和离开状态
- **WHEN** active `StateNode` 引用 `StateBehaviorSubTree`
- **THEN** runtime MUST 进入时先 tick `OnEnter`，active 时 tick `RootNode`
- **AND** Transition 离开该状态前 MUST tick `OnExit`

### Requirement: Transition 是同层 BaseEdge 语义
系统 MUST 将状态转换表达为 `StateMachineGraph` 内的 `BaseEdge`，MUST NOT 新增 `TransitionNode`。Transition MUST 只连接同层 `Enter`、`AnyState`、`StateNode` 和 `Exit`。

#### Scenario: 合法端点
- **WHEN** 用户连接 Transition flow
- **THEN** 合法连接 MUST 是 `Enter -> StateNode`、`AnyState -> StateNode|Exit`、`StateNode -> StateNode|Exit`
- **AND** `RootNode`、`StateMachineNode`、普通 `RunnableNode`、`TimelineNode`、Tree 节点和 `ValueNode` MUST NOT 成为 Transition 端点

#### Scenario: 条件和优先级
- **WHEN** Transition 配置条件
- **THEN** 条件 MUST 引用同层 `ValueNode` 的 Bool `PropertyPort`
- **AND** `AnyState` Transition MUST 配置 Bool 条件
- **AND** runtime MUST 按 Transition 优先级选择可通过的边

### Requirement: 状态机运行时解释
系统 MUST 让 `StateMachineNode` 驱动自己引用的 `StateMachineGraph`，并让 `StateMachineGraphRuntime` 以 `StateNode` 作为 active state。解释器 MUST 从 `Enter` 读取初始 Transition，并在每帧写入当前 `BaseGraph.DeltaTime`。

#### Scenario: 父级 tick 状态机入口
- **WHEN** 父级行为图 tick 到 `StateMachineNode`
- **THEN** `StateMachineNode` MUST 进入引用的 `StateMachineGraph`
- **AND** 父级行为图 MUST 只看到该节点的 `Running/Success/Failure`

#### Scenario: active state tick
- **WHEN** 状态机已有 active state
- **THEN** 本帧 MUST 只 tick active `StateNode` 的状态行为 `SubTree`
- **AND** 其它状态 MUST NOT 被 tick，除非 Transition 切换 active state

#### Scenario: AnyState 和 Exit
- **WHEN** 状态机已有 active state
- **THEN** runtime MUST 在 tick active state 前检查 `AnyState` Transition
- **AND** 命中 `Exit` MUST 让本层状态机返回 `Success`

#### Scenario: Stop 和 Reset 传播
- **WHEN** 父级 Graph stop 或 reset `StateMachineNode`
- **THEN** 当前 active `StateNode` MUST stop 或 reset 自己的状态行为 `SubTree`

### Requirement: Timeline 和输入通过正式状态行为链路接入
系统 MUST NOT 将 `TimelineNode` 或 InputAction 节点作为 `StateMachineGraph` 同层状态或 Transition flow 端点。Timeline MUST 通过 `StateNode` 下钻的状态行为 `SubTree` 接入；InputAction Bool 节点 MAY 作为同层 Transition 条件来源。

#### Scenario: Idle 播放 Timeline
- **WHEN** Idle 状态需要播放 Timeline
- **THEN** 用户 MUST 在 Idle 引用的状态行为 `SubTree` 中创建 `TimelineNode`
- **AND** `TimelineNode` MUST 从所在运行 Graph 获得 `Owner.DeltaTime`

#### Scenario: 输入驱动 Transition
- **WHEN** 用户在 `StateMachineGraph` 中创建 InputAction Bool 输入节点
- **THEN** 该节点 MAY 作为 Transition 条件来源
- **AND** 该节点 MUST NOT 出现在合法 Transition flow 端点候选中

### Requirement: 不保留旧特化数据路径
系统 MUST 不依赖旧 Locomotion、Action、FootPhase、BodyClaim 或 AnimationPresentationPolicy SO/config 数据来表达状态机创作语义。

#### Scenario: 旧数据存在
- **WHEN** 项目中发现旧状态、动作、FootPhase 或表现配置数据
- **THEN** 当前状态机能力 MUST NOT 读取该数据
- **AND** 该数据 MUST 迁移到 Graph、NodeModule 或 Timeline 轨道后删除
