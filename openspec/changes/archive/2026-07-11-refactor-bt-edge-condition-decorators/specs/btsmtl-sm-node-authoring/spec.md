# btsmtl-sm-node-authoring Specification

## REMOVED Requirements

### Requirement: TransitionRuleGraph 表达纯条件求值
`TransitionRuleGraph` 的状态机专属命名被 `ConditionRuleGraph` 取代。状态机 Transition 仍然使用 edge 内纯 Bool 条件图，但该条件图类型必须与 BT edge decorator 共用，不再保留第二套 `TransitionRuleGraph` 类型。

#### Scenario: 移除状态机专属条件图类型
- **WHEN** 本变更实现完成
- **THEN** 状态机 Transition MUST 使用 `ConditionRuleGraph`
- **AND** 系统 MUST NOT 保留 `TransitionRuleGraph` 作为独立运行或 authoring 类型

### Requirement: TransitionRuleGraph 必须拥有唯一结果节点
该要求被通用 `ConditionRuleGraph` 的唯一 result node 要求取代。结果节点语义保持不变，但节点名称和适用范围必须迁移到通用条件图。

#### Scenario: 移除状态机专属结果节点
- **WHEN** 本变更实现完成
- **THEN** 状态机 Transition 条件图 MUST 使用 `ConditionRuleResultNode`
- **AND** 系统 MUST NOT 保留 `TransitionRuleResultNode` 作为第二套结果节点

## MODIFIED Requirements

### Requirement: StateMachineGraph 只表达同层状态结构
系统 MUST 让 `StateMachineGraph` 只包含 `Enter`、`AnyState`、`Exit` 和 `StateNode`。它 MUST NOT 创建 `RootNode`、`OnEnter`、`OnExit`、`StateMachineNode`、普通 `RunnableNode`、Timeline 行为节点、Tree 行为节点或条件用 `ValueNode`。Transition 条件 MUST 下钻到 Transition 边 resolved `ConditionRuleGraph`。

#### Scenario: 创建规则统一
- **WHEN** 编辑器、拖拽、粘贴或脚本路径向 `StateMachineGraph` 创建节点
- **THEN** 系统 MUST 统一通过 `CanCreateNodeType()` 判定
- **AND** 非法节点 MUST NOT 进入正式节点集合

#### Scenario: 控制节点唯一
- **WHEN** 用户打开或校验 `StateMachineGraph`
- **THEN** 每层 MUST 且只能包含一个 `Enter`、一个 `AnyState` 和一个 `Exit`
- **AND** 每层 MUST 至少包含一个 `StateNode`

#### Scenario: 条件节点不在状态机本层
- **WHEN** 用户需要配置 Idle 到 Attack 的 Transition 条件
- **THEN** 用户 MUST 打开该 Transition 的 `ConditionRuleGraph`
- **AND** 系统 MUST NOT 在 `StateMachineGraph` 本层创建条件 `ValueNode`

### Requirement: Transition 是同层 BaseEdge 语义
系统 MUST 将状态转换表达为 `StateMachineGraph` 内联保存的 `BaseEdge`，MUST NOT 新增 `TransitionNode`，也 MUST NOT 为 Transition 本体创建 asset。Transition MUST 只连接同层 `Enter`、`AnyState`、`StateNode` 和 `Exit`。Transition 条件默认 MUST 是该 edge 内部的 inline `ConditionRuleGraph` 数据；需要复用时才显式绑定 shared `ConditionRuleGraph` asset。

#### Scenario: 合法端点
- **WHEN** 用户连接 Transition flow
- **THEN** 合法连接 MUST 是 `Enter -> StateNode`、`AnyState -> StateNode|Exit`、`StateNode -> StateNode|Exit`
- **AND** `RootNode`、`StateMachineNode`、普通 `RunnableNode`、`TimelineNode`、Tree 节点和 `ValueNode` MUST NOT 成为 Transition 端点

#### Scenario: 条件和优先级
- **WHEN** Transition 配置条件
- **THEN** 条件 MUST 通过该 Transition resolved `ConditionRuleGraph` 表达
- **AND** 创建合法 Transition edge 时 MUST 立即创建该 edge 内部的 inline `ConditionRuleGraph`
- **AND** 默认规则图 MUST 是该 edge 内部的 inline graph data
- **AND** 默认规则图在未连接条件时 MUST 允许 Transition 通过
- **AND** `AnyState` Transition MUST 配置规则图条件
- **AND** runtime MUST 按 Transition 优先级选择可通过的边

#### Scenario: Transition 显式复用规则图
- **WHEN** 多条 Transition 需要复用同一套规则
- **THEN** 用户 MUST 显式抽取或分配 shared `ConditionRuleGraph` asset
- **AND** 删除 Transition 时 MUST 只断开 shared 引用，不删除 shared asset
- **AND** 切换到 shared asset 后 MUST 清理该 Transition 的 inline rule graph 真数据

#### Scenario: Shared ConditionRuleGraph asset 被删除
- **WHEN** Transition 引用的 shared `ConditionRuleGraph` asset 已经被删除或不再能解析为 `ConditionRuleGraph`
- **THEN** 编辑器刷新或校验该 `StateMachineGraph` 时 MUST 自动清理该 shared 引用
- **AND** 该 Transition MUST 回到 owner 内部 inline `ConditionRuleGraph`
- **AND** 系统 MUST NOT 保留 Missing asset 引用

### Requirement: 状态机运行时解释
系统 MUST 让 `StateMachineNode` 驱动自己 resolved `StateMachineGraph` 数据，并让 `StateMachineGraphRuntime` 以 `StateNode` 作为 active state。解释器 MUST 从 `Enter` 读取初始 Transition，并在每帧写入当前运行工作副本的 `BaseGraph.DeltaTime`。运行时 MUST 从 inline 或 shared authoring graph data 创建隔离工作副本。

#### Scenario: 父级 tick 状态机入口
- **WHEN** 父级行为图 tick 到 `StateMachineNode`
- **THEN** `StateMachineNode` MUST 进入 resolved `StateMachineGraph` 运行工作副本
- **AND** 父级行为图 MUST 只看到该节点的 `Running/Success/Failure`

#### Scenario: active state tick
- **WHEN** 状态机已有 active state
- **THEN** 本帧 MUST 只 tick active `StateNode` resolved 状态行为图的运行工作副本
- **AND** 其它状态 MUST NOT 被 tick，除非 Transition 切换 active state

#### Scenario: AnyState 和 Exit
- **WHEN** 状态机已有 active state
- **THEN** runtime MUST 在 tick active state 前检查 `AnyState` Transition
- **AND** 命中 `Exit` MUST 让本层状态机返回 `Success`

#### Scenario: Stop 和 Reset 传播
- **WHEN** 父级 Graph stop 或 reset `StateMachineNode`
- **THEN** 当前 active `StateNode` MUST stop 或 reset 自己的状态行为图运行工作副本

### Requirement: Timeline 和输入通过正式状态行为链路接入
系统 MUST NOT 将 `TimelineNode` 或 InputAction 节点作为 `StateMachineGraph` 同层状态或 Transition flow 端点。Timeline MUST 通过 `StateNode` 下钻的状态行为 `SubTree` 接入；InputAction 值 MUST 在 `ConditionRuleGraph` 中作为条件输入接入。

#### Scenario: Idle 播放 Timeline
- **WHEN** Idle 状态需要播放 Timeline
- **THEN** 用户 MUST 在 Idle 引用的状态行为 `SubTree` 中创建 `TimelineNode`
- **AND** `TimelineNode` MUST 从所在运行 Graph 获得 `Owner.DeltaTime`

#### Scenario: 输入驱动 Transition
- **WHEN** 用户需要用 InputAction Bool 驱动 Transition
- **THEN** 用户 MUST 在该 Transition 的 `ConditionRuleGraph` 中创建 InputAction Bool 输入节点
- **AND** 该输入节点 MUST NOT 出现在 `StateMachineGraph` 本层合法 Transition flow 端点候选中

### Requirement: Transition Rule 编辑入口属于 Transition 边
编辑器 MUST 允许用户从 Transition 边打开和查看条件图。边视图 MUST 显示优先级、ownership 和规则摘要。默认私有规则图 MUST 作为 Transition edge 内部 inline graph data 保存，需要复用时才显式抽取或分配 shared `ConditionRuleGraph` asset。

#### Scenario: 打开规则图
- **WHEN** 用户双击 Transition 边或点击边 Inspector 的 `Open Rule`
- **AND** 该 Transition 边已有 resolved `ConditionRuleGraph`
- **THEN** 编辑器 MUST 打开该边 resolved `ConditionRuleGraph`
- **AND** 页面栈 MAY 记录来源边，但 MUST NOT 将页面栈写入图数据

#### Scenario: 从 Transition 边补齐并打开规则图
- **WHEN** 用户双击合法 Transition 边或点击边 Inspector 的 `Open Rule`
- **AND** 该 Transition 边没有 inline 条件图，也没有 shared 条件图 asset
- **THEN** 编辑器 MUST 为该 Transition 创建 edge 内部 inline `ConditionRuleGraph`
- **AND** 编辑器 MUST 立即打开该 `ConditionRuleGraph`
- **AND** 默认规则图在未连接条件时 MUST 允许 Transition 通过
- **AND** 补齐流程 MUST NOT 创建 subasset、一次性外部 asset 或旧 `TransitionRuleGraph`

#### Scenario: 缺失规则图修复
- **WHEN** 编辑器刷新或校验发现合法 Transition 缺失规则图
- **THEN** 系统 MUST 为该 Transition 补齐 edge 内部 inline `ConditionRuleGraph`
- **AND** 补齐流程 MUST NOT fallback 到外部临时资产路径

#### Scenario: 删除带规则图的 Transition
- **WHEN** 用户删除拥有 inline 规则图的 Transition 边
- **THEN** inline 规则图 MUST 随 Transition 边序列化数据一起删除
- **AND** 系统 MUST NOT 执行 subasset 删除

#### Scenario: 删除引用 shared 规则图的 Transition
- **WHEN** 用户删除引用 shared asset 规则图的 Transition 边
- **THEN** 系统 MUST 只删除 Transition 边并断开引用
- **AND** 系统 MUST NOT 删除 shared asset

### Requirement: Transition Rule 默认图数据初始化
系统 MUST 在创建合法 Transition 或编辑器修复缺失规则图的 Transition 时初始化一份可立即下钻编辑的 inline `ConditionRuleGraph` 数据。规则图是条件求值图，不是普通行为树。

#### Scenario: 创建默认 Transition rule
- **WHEN** 用户连接合法 Transition 或编辑器修复缺失规则图的 Transition
- **THEN** 系统 MUST 创建 inline `ConditionRuleGraph` 数据
- **AND** 默认规则图 MUST 包含规则输出入口
- **AND** 默认规则图在未连接条件时 MUST 允许 Transition 通过
- **AND** 用户 MUST 能立即下钻编辑条件节点和属性连线
- **AND** 创建流程 MUST NOT 创建 subasset

### Requirement: TransitionRuleGraph 必须能读取当前状态运行事实
系统 MUST 提供正式 value node 或等价只读接口，让状态机 Transition 使用的 `ConditionRuleGraph` 能读取当前 `StateMachineGraphRuntime` 的状态运行事实。该能力 MUST 保持条件图的纯条件求值语义，MUST NOT tick 状态行为 SubTree、Timeline 或 Action 节点。

#### Scenario: Start 状态完成后切换 Loop
- **WHEN** 作者配置 `RunStart -> RunLoop`
- **THEN** `ConditionRuleGraph` MUST 能读取 `StateRootCompleted`
- **AND** `ConditionRuleGraph` MUST 能组合 `MoveMagnitude >= RunThreshold`
- **AND** runtime MUST 只在两者都成立时允许 transition

#### Scenario: End 状态完成后回 Idle
- **WHEN** 作者配置 `RunEnd -> Idle`
- **THEN** `ConditionRuleGraph` MUST 能读取 `StateRootCompleted`
- **AND** transition rule MUST NOT 通过 Timeline asset membership 或节点路径判断完成
