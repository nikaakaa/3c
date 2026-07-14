# btsmtl-sm-node-authoring Specification

## MODIFIED Requirements

### Requirement: StateMachineGraph 只表达同层状态结构
系统 MUST 让 `StateMachineGraph` 只包含 `Enter`、`AnyState`、`Exit` 和 `StateNode`。它 MUST NOT 创建 `RootNode`、`OnEnter`、`OnExit`、`StateMachineNode`、普通 `RunnableNode`、Timeline 行为节点、Tree 行为节点或条件用 `ValueNode`。Transition 条件 MUST 下钻到 Transition 边 resolved `TransitionRuleGraph`。

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
- **THEN** 用户 MUST 打开该 Transition 的 `TransitionRuleGraph`
- **AND** 系统 MUST NOT 在 `StateMachineGraph` 本层创建条件 `ValueNode`

### Requirement: Transition 是同层 BaseEdge 语义
系统 MUST 将状态转换表达为 `StateMachineGraph` 内的 `BaseEdge`，MUST NOT 新增 `TransitionNode`。Transition MUST 只连接同层 `Enter`、`AnyState`、`StateNode` 和 `Exit`。合法 Transition MUST 拥有 edge 内部 inline `TransitionRuleGraph` 或显式 shared `TransitionRuleGraph`，条件 MUST 由该 resolved 规则图表达。

#### Scenario: 合法端点
- **WHEN** 用户连接 Transition flow
- **THEN** 合法连接 MUST 是 `Enter -> StateNode`、`AnyState -> StateNode|Exit`、`StateNode -> StateNode|Exit`
- **AND** `RootNode`、`StateMachineNode`、普通 `RunnableNode`、`TimelineNode`、Tree 节点、`ValueNode` 和规则图节点 MUST NOT 成为 Transition 端点

#### Scenario: 条件和优先级
- **WHEN** Transition 配置条件
- **THEN** 条件 MUST 由该 Transition resolved `TransitionRuleGraph` 输出 Bool
- **AND** `AnyState` Transition MUST 在规则图内配置有效条件
- **AND** runtime MUST 按 Transition 优先级选择可通过的边

#### Scenario: 默认普通转换
- **WHEN** `Enter -> StateNode` 或 `StateNode -> StateNode|Exit` 使用默认 inline 规则图且未连接条件
- **THEN** runtime MUST 通过该规则图得到 true
- **AND** runtime MUST NOT 因缺失规则图走无条件 fallback

### Requirement: 状态机运行时解释
系统 MUST 让 `StateMachineNode` 驱动自己引用的 `StateMachineGraph`，并让 `StateMachineGraphRuntime` 以 `StateNode` 作为 active state。解释器 MUST 从 `Enter` 读取初始 Transition，在每帧写入当前 `BaseGraph.DeltaTime`，并通过 Transition 边 resolved `TransitionRuleGraph` 判断条件。

#### Scenario: 父级 tick 状态机入口
- **WHEN** 父级行为图 tick 到 `StateMachineNode`
- **THEN** `StateMachineNode` MUST 进入引用的 `StateMachineGraph`
- **AND** 父级行为图 MUST 只看到该节点的 `Running/Success/Failure`

#### Scenario: active state tick
- **WHEN** 状态机已有 active state
- **THEN** 本帧 MUST 只 tick active `StateNode` 的状态行为 `SubTree`
- **AND** 其它状态 MUST NOT 被 tick，除非 Transition 切换 active state

#### Scenario: Transition 条件求值
- **WHEN** runtime 枚举 active state 的 outgoing Transition
- **THEN** runtime MUST 按优先级求值对应规则图
- **AND** runtime MUST NOT 因规则图缺失走无条件 fallback
- **AND** runtime MUST NOT 读取旧 BoolPort 条件字段作为 fallback

#### Scenario: AnyState 和 Exit
- **WHEN** 状态机已有 active state
- **THEN** runtime MUST 在 tick active state 前检查 `AnyState` Transition
- **AND** 命中 `Exit` MUST 让本层状态机返回 `Success`

#### Scenario: Stop 和 Reset 传播
- **WHEN** 父级 Graph stop 或 reset `StateMachineNode`
- **THEN** 当前 active `StateNode` MUST stop 或 reset 自己的状态行为 `SubTree`

### Requirement: Timeline 和输入通过正式状态行为链路接入
系统 MUST NOT 将 `TimelineNode` 或 InputAction 节点作为 `StateMachineGraph` 同层状态或 Transition flow 端点。Timeline MUST 通过 `StateNode` 下钻的状态行为 `SubTree` 接入；InputAction 值 MUST 在 `TransitionRuleGraph` 中作为条件输入接入。

#### Scenario: Idle 播放 Timeline
- **WHEN** Idle 状态需要播放 Timeline
- **THEN** 用户 MUST 在 Idle 引用的状态行为 `SubTree` 中创建 `TimelineNode`
- **AND** `TimelineNode` MUST 从所在运行 Graph 获得 `Owner.DeltaTime`

#### Scenario: 输入驱动 Transition
- **WHEN** 用户需要用 InputAction Bool 驱动 Transition
- **THEN** 用户 MUST 在该 Transition 的 `TransitionRuleGraph` 中创建 InputAction Bool 输入节点
- **AND** 该输入节点 MUST NOT 出现在 `StateMachineGraph` 本层合法 Transition flow 端点候选中

## ADDED Requirements

### Requirement: TransitionRuleGraph 表达纯条件求值
系统 MUST 使用 `TransitionRuleGraph` 表达状态机 Transition 的纯 Bool 条件。`TransitionRuleGraph` MUST 使用现有 `BaseGraph` 数据、字段访问器、属性边集合和 `PropertyPort`，MUST NOT 继承或模拟 `RunnableTree` 的执行生命周期。

#### Scenario: 创建规则图数据
- **WHEN** 系统为一条 Transition 创建规则图
- **THEN** 系统 MUST 创建 `TransitionRuleGraph`
- **AND** 该图 MUST 使用现有节点集合、属性边集合、字段访问器和 `PropertyPort`
- **AND** 系统 MUST NOT 创建 Workbench 图或并行端口协议

#### Scenario: 规则图求值
- **WHEN** 状态机 runtime 求值 Transition 条件
- **THEN** runtime MUST 将规则图当作纯 Bool 求值图
- **AND** 规则图 MUST NOT tick Timeline、Action、RunnableNode 或状态行为 `SubTree`

### Requirement: TransitionRuleGraph 必须拥有唯一结果节点
系统 MUST 通过唯一 `TransitionRuleResultNode` 表达规则图的最终 Bool 输出。规则图缺失结果节点或存在多个结果节点时 MUST 被校验为非法。

#### Scenario: 新建规则图
- **WHEN** 系统新建 `TransitionRuleGraph`
- **THEN** 新图 MUST 默认包含一个 `TransitionRuleResultNode`
- **AND** 该节点 MUST 暴露一个 Bool 输入作为最终条件

#### Scenario: 结果节点非法
- **WHEN** 规则图没有结果节点或拥有多个结果节点
- **THEN** 校验 MUST 报告规则图非法
- **AND** runtime MUST NOT 将该规则图当作 true 通过

### Requirement: Transition 调度元数据留在边上
Transition 的优先级和同优先级稳定排序 MUST 属于边调度数据。规则图 MUST NOT 负责选择其它 Transition，也 MUST NOT 保存 priority、tag 或 trigger 的调度排序逻辑。

#### Scenario: 多条 Transition 同时成立
- **WHEN** 同一来源节点存在多条规则图返回 true 的 Transition
- **THEN** runtime MUST 先按 Transition 优先级选择
- **AND** 优先级相同 MUST 再按 flow order 保持稳定顺序

#### Scenario: tag 或 fact 条件
- **WHEN** Transition 需要 tag、fact、输入或黑板变量参与判断
- **THEN** 这些数据 MUST 通过规则图内的读取或谓词节点表达
- **AND** Transition 边 MUST NOT 为每类业务数据新增专用条件字段

### Requirement: Transition Rule 编辑入口属于 Transition 边
编辑器 MUST 允许用户从 Transition 边打开和查看规则图。边视图 MUST 显示优先级、ownership 和规则摘要。默认私有规则图 MUST 作为 Transition edge 内部 inline graph data 保存，需要复用时才显式抽取或分配 shared `TransitionRuleGraph` asset。

#### Scenario: 打开规则图
- **WHEN** 用户双击有规则图的 Transition 边或点击边 Inspector 的 `Open Rule`
- **THEN** 编辑器 MUST 打开该边 resolved `TransitionRuleGraph`
- **AND** 页面栈 MAY 记录来源边，但 MUST NOT 将页面栈写入图数据

#### Scenario: 缺失规则图修复
- **WHEN** 编辑器刷新或校验发现合法 Transition 缺失规则图
- **THEN** 系统 MUST 为该 Transition 补齐 edge 内部 inline `TransitionRuleGraph`
- **AND** 补齐流程 MUST NOT fallback 到外部临时资产路径

#### Scenario: 删除带规则图的 Transition
- **WHEN** 用户删除拥有 inline 规则图的 Transition 边
- **THEN** inline 规则图 MUST 随 Transition 边序列化数据一起删除
- **AND** 系统 MUST NOT 执行 subasset 删除

#### Scenario: 删除引用 shared 规则图的 Transition
- **WHEN** 用户删除引用 shared asset 规则图的 Transition 边
- **THEN** 系统 MUST 只删除 Transition 边并断开引用
- **AND** 系统 MUST NOT 删除 shared asset

### Requirement: 旧 BoolPort 条件链路必须删除
系统 MUST 删除旧 `TransitionConditionNodeGuid/PortId` 条件链路和同图 Bool port 条件菜单。可迁移的旧条件 MUST 迁移为规则图；不可迁移的旧条件 MUST 被报告为非法结构，不得 fallback。

#### Scenario: 旧条件字段存在
- **WHEN** 旧资产或代码路径仍保存 Transition BoolPort 条件引用
- **THEN** 迁移或清理路径 MUST 将其移除
- **AND** runtime MUST NOT 再读取该旧字段决定 Transition
