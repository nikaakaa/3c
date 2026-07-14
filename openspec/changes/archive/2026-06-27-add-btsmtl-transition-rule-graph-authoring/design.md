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

TransitionRuleGraph 默认作为 StateMachineGraph 的内嵌 sub-asset
  InputAction / Blackboard / Predicate / Logic / Compare 节点
  TransitionRuleResultNode
    Bool input -> 最终结果

Shared TransitionRuleGraph asset
  只有用户显式抽取或手动分配时才作为多条边复用的独立资产
```

`StateMachineGraph` 只负责同层状态关系。`TransitionRuleGraph` 只负责条件求值。`StateNode` 的状态行为仍然下钻到 `SubTree`，状态里要跑 Timeline、Action 或嵌套状态机，都在状态行为 `SubTree` 里做。

## 数据和运行链路

1. 用户在 `StateMachineGraph` 中连接 `StateNode -> StateNode`、`StateNode -> Exit`、`AnyState -> StateNode` 或 `Enter -> StateNode`。
2. 编辑器创建或选中这条 Transition 边。
3. 边 Inspector 显示 `priorityOrder`、规则图引用和规则图归属；双击边或点击命令只打开已有 `TransitionRuleGraph`，不得隐式创建。
4. 如果 Transition 没有规则图：
   - `Enter -> StateNode` 表示默认入口。
   - `StateNode -> StateNode|Exit` 表示无条件转换。
   - `AnyState -> ...` 非法。
5. 用户点击 `Create Rule` 时，编辑器在所属 `StateMachineGraph` asset 内创建内嵌 `TransitionRuleGraph` sub-asset，并绑定到该边。
6. 如果 Transition 有规则图，runtime 创建轻量求值器，把父 `StateMachineGraph` 的正式上下文传给规则图。
7. 求值器从 `TransitionRuleResultNode` 的 Bool 输入读取最终结果。
8. `StateMachineGraphRuntime` 按 `priorityOrder` 排序，命中第一个为 true 的 Transition。

## 规则图归属模型

默认规则图是边私有数据，保存为所属 `StateMachineGraph` asset 的 sub-asset：

```text
CombatSM.asset
  - Idle_To_Move_Rule
  - AnyState_To_HitReact_Rule
```

这样双击打开规则图时不会污染项目目录，也不需要用户选择路径。`BaseEdge` 仍然只保存一个 `TransitionRuleGraph` 引用，引用目标可以是：

- 同一个 `StateMachineGraph` asset 内的 embedded rule graph。
- 用户显式创建或抽取出来的独立 shared `TransitionRuleGraph` asset。

系统 MUST NOT 允许默认创建路径把规则图散落到 `Assets` 根目录或和状态机同目录的一堆独立 asset。真的需要复用时，用户通过 `Extract Shared Rule` 把 embedded rule graph 复制成独立 asset，并让当前边改引用 shared asset。

## 打开、创建、清理语义

- `Open Rule`：只打开已有规则图；没有规则图时切到边 Inspector 显示缺失状态，不创建。
- `Create Rule`：创建 embedded rule graph sub-asset，并绑定到当前边。
- `Assign Shared Rule`：通过边 Inspector 的对象引用字段分配已有独立 `TransitionRuleGraph` asset。
- `Extract Shared Rule`：把当前 embedded rule graph 复制为独立 asset，当前边改引用 shared asset，并删除原 embedded rule graph。
- `Delete Embedded Rule`：如果当前规则图是 embedded，则清引用并删除 embedded sub-asset。
- `Remove Reference`：如果当前规则图是 shared，则只清引用，不删除 shared asset。
- 删除 Transition 边：如果边拥有 embedded rule graph，则跟随删除；如果该规则图除了唯一默认 `TransitionRuleResultNode` 外还有节点、连接或 exposed property，删除前必须确认。
- 通过 Inspector 对象字段清空或替换规则图时，必须复用同一套 ownership 删除语义，不能绕过 embedded sub-asset 删除。

边 Inspector MUST 显示规则图归属：

- `None`
- `Embedded`
- `Shared Asset`
- `Invalid External Embedded`，表示引用了其它 asset 内部的 sub-asset，这种结构需要校验报错。

## 为什么不是直接 BoolPort

当前 `BaseEdge.TransitionConditionNodeGuid/PortId` 的 interface 太浅：调用方必须知道条件节点在哪一层、哪个 port 是最终条件、何时 `OutputValueImperatively()`、缺失端口怎么处理。它省了一个规则图资产，但复杂度会扩散到边菜单、runtime、校验和后续 pipeline facts 对接。

规则图把这些知识收进一个更深的 Module：外部只需要知道“这条边有没有规则图，规则图是否返回 true”。实现细节留在规则图内部，locality 更好。

## 为什么不是普通 SubTree

普通 `SubTree` 的 interface 是行为生命周期：Root、Running、Success、Failure、Stop、Reset。Transition rule 的 interface 是纯 Bool 求值。用 `SubTree` 会把两个语义混在一起，后续很容易在条件里 tick Timeline、Action 或改变状态。

规则图可以继续复用 BTSMTL 的图数据、节点、字段访问器和 `PropertyPort`，但不继承 `RunnableTree` 语义。

## 为什么不是 TransitionNode

Transition 是两个状态之间的关系。把它变成节点会让状态机拓扑从“边表达跳转”变成“节点表达跳转”，图会更绕，也会和现有 BTSMTL flow edge 的保存方式分裂。边上放调度元数据，条件下钻到规则图，心智更直接。

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

- 每条边一个规则图会增加 sub-asset 数量，需要编辑器在边上显示规则摘要、缺失状态、归属和打开入口。
- Shared rule 会被多条边引用，编辑器必须明确显示 shared 归属，避免用户误以为修改只影响当前边。
- 规则图必须强约束为纯求值图，否则会重新引入行为生命周期混乱。
- 需要统一校验：状态机图不能再创建条件 `ValueNode`，规则图不能创建状态、Timeline、Action 或普通 Runnable 节点。
