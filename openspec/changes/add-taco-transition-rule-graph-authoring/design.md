# Design

## 目标模型

```text
普通行为图 / SubTree
  StateMachineNode
    -> StateMachineGraph

StateMachineGraph
  Enter
  AnyState
  Exit
  StateNode: Idle
  StateNode: Attack
  TransitionEdge: Idle -> Attack
    priorityOrder
    transitionRuleGraph

TransitionRuleGraph
  InputAction / Blackboard / Predicate / Logic / Compare 节点
  TransitionRuleResultNode
    Bool input -> 最终结果
```

`StateMachineGraph` 只负责同层状态关系。`TransitionRuleGraph` 只负责条件求值。`StateNode` 的状态行为仍然下钻到 `SubTree`，状态里要跑 Timeline、Action 或嵌套状态机，都在状态行为 `SubTree` 里做。

## 数据和运行链路

1. 用户在 `StateMachineGraph` 中连接 `StateNode -> StateNode`、`StateNode -> Exit`、`AnyState -> StateNode` 或 `Enter -> StateNode`。
2. 编辑器创建或选中这条 Transition 边。
3. 边 Inspector 显示 `priorityOrder` 和规则图引用；双击边或点击命令打开该边的 `TransitionRuleGraph`。
4. 如果 Transition 没有规则图：
   - `Enter -> StateNode` 表示默认入口。
   - `StateNode -> StateNode|Exit` 表示无条件转换。
   - `AnyState -> ...` 非法。
5. 如果 Transition 有规则图，runtime 创建轻量求值器，把父 `StateMachineGraph` 的正式上下文传给规则图。
6. 求值器从 `TransitionRuleResultNode` 的 Bool 输入读取最终结果。
7. `StateMachineGraphRuntime` 按 `priorityOrder` 排序，命中第一个为 true 的 Transition。

## 为什么不是直接 BoolPort

当前 `BaseEdge.TransitionConditionNodeGuid/PortId` 的 interface 太浅：调用方必须知道条件节点在哪一层、哪个 port 是最终条件、何时 `OutputValueImperatively()`、缺失端口怎么处理。它省了一个规则图资产，但复杂度会扩散到边菜单、runtime、校验和后续 pipeline facts 对接。

规则图把这些知识收进一个更深的 Module：外部只需要知道“这条边有没有规则图，规则图是否返回 true”。实现细节留在规则图内部，locality 更好。

## 为什么不是普通 SubTree

普通 `SubTree` 的 interface 是行为生命周期：Root、Running、Success、Failure、Stop、Reset。Transition rule 的 interface 是纯 Bool 求值。用 `SubTree` 会把两个语义混在一起，后续很容易在条件里 tick Timeline、Action 或改变状态。

规则图可以继续复用 Taco 的图数据、节点、字段访问器和 `PropertyPort`，但不继承 `RunnableTree` 语义。

## 为什么不是 TransitionNode

Transition 是两个状态之间的关系。把它变成节点会让状态机拓扑从“边表达跳转”变成“节点表达跳转”，图会更绕，也会和现有 Taco flow edge 的保存方式分裂。边上放调度元数据，条件下钻到规则图，心智更直接。

## Inspector 快捷条件怎么处理

可以做，但只能作为同一个 `TransitionRuleGraph` 的快捷外观：

- 用户在 Inspector 里选 “InputAction IsPressed”。
- 系统在该 Transition 的规则图里生成或更新对应输入节点和结果连接。
- 边数据不再保存第二套 `conditionNodeGuid/portId`。

这样不会出现“Inspector 看起来一个条件，规则图里又是另一个条件”的双事实。

## tags、facts 和 pipeline 对接

本变更不实现完整 pipeline facts。规则图先读取现有图上下文、黑板变量和输入值来源。后续 pipeline 建起来后，新增 facts/tags 读取节点即可：

- `HasTag(tagId)`
- `CompareFact(factId, op, value)`
- `InActionWindow(windowId)`
- `TimelineMarkerPassed(markerId)`

这些节点是规则图的输入/谓词实现，不改变 Transition edge 的 interface。

## 迁移策略

旧 `TransitionConditionNodeGuid/PortId` 是原型字段。实现阶段需要将已有边条件一次性迁移为对应规则图，然后删除旧字段和旧菜单入口。无法迁移的旧条件报告为非法结构，不做 fallback。

## 风险

- 每条边一个规则图会增加资产数量，需要编辑器在边上显示规则摘要、缺失状态和打开入口。
- 规则图必须强约束为纯求值图，否则会重新引入行为生命周期混乱。
- 需要统一校验：状态机图不能再创建条件 `ValueNode`，规则图不能创建状态、Timeline、Action 或普通 Runnable 节点。
