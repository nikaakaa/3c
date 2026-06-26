# taco-componentized-node-authoring Specification

## MODIFIED Requirements

### Requirement: 统一嵌套节点创作
Taco SHALL（必须）允许 Timeline 引用节点、StateMachine 入口节点、State 节点、Tree 引用节点、值节点和普通逻辑节点通过 Taco 原生节点、端口和模块模型组合。不同节点能否出现在同一张图中 SHALL（必须）由当前 Graph 类型的创建规则决定，而不是由 Workbench 或并行注册表决定。状态机结构图和状态行为 `SubTree` MUST 保持分层。

#### Scenario: 普通行为图创建 StateMachine 入口
- **当** 普通行为图被打开
- **并且** 用户调用节点搜索
- **则** StateMachine 入口节点必须可以在该创作图中创建
- **并且** StateNode 不得作为普通行为图节点创建

#### Scenario: 状态机图创建 State
- **当** `StateMachineGraph` 被打开
- **并且** 用户调用节点搜索
- **则** StateNode 必须可以在该图中创建
- **并且** Timeline 引用节点和普通 runnable 节点不得在该图中创建
- **并且** StateMachine 入口节点不得在该图中创建

#### Scenario: 状态行为 SubTree 创建行为节点
- **当** 用户打开 `StateNode` 引用的状态行为 `SubTree`
- **并且** 用户调用节点搜索
- **则** 普通 `SubTree` 必须固定包含 `RootNode`
- **并且** 普通 `SubTree` 不得强制包含 `OnEnter` 或 `OnExit`
- **并且** `StateBehaviorSubTree` 必须固定包含 `OnEnter`、`RootNode` 和 `OnExit`
- **并且** Timeline 引用节点和普通 runnable 节点必须可以在这些图中创建
- **并且** StateMachine 入口节点也可以作为嵌套状态机入口创建
- **并且** StateNode 不得作为普通行为图节点创建

### Requirement: 嵌套 Graph 语义不是端口语义
Taco SHALL（必须）把 Graph 嵌套、下钻命令、Graph 作用域和循环验证视为节点/模块创作语义，而不是 `PropertyPort` 的职责。`StateNode` 可以通过正式状态行为引用模块表达状态行为 `SubTree`，下钻语义不得通过属性端口连接推导。

#### Scenario: StateNode 打开 SubTree
- **当** StateNode 拥有 `SubTree` 引用字段
- **并且** 用户调用打开子 Graph 命令
- **则** Taco 必须打开该 StateNode 引用的 `SubTree`
- **并且** 该下钻不得依赖 Workbench 或并行 Graph 引用字段
