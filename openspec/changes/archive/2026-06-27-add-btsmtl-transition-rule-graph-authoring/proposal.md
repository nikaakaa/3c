# Proposal

## Why

当前 `StateMachineGraph` 的 Transition 条件还是薄原型：`BaseEdge` 直接保存一个同图 Bool `PropertyPort` 引用，`StateMachineGraphRuntime` 读取该端口决定是否跳转。这个做法能临时跑通输入驱动切换，但它把状态拓扑、条件求值节点和边调度数据揉在同一层，后续会很难表达复杂条件、组合条件、调试摘要和下钻编辑。

可以借鉴 Unreal Animation Blueprint 的 transition rule：状态机边仍然是边，优先级仍然是边的调度属性；每条需要条件的边拥有一个纯求值规则图，规则图最后输出一个 Bool。规则图不是行为树，不 tick Timeline，不运行 Action，只回答“这条边现在能不能过”。

## What Changes

- 新增 `TransitionRuleGraph` 创作能力，用于表达 Transition 的纯 Bool 条件。
- Transition 仍然是 `StateMachineGraph` 内的 `BaseEdge` 语义，不新增 `TransitionNode`。
- Transition 边保存优先级和可选 `TransitionRuleGraph` 引用；无规则图表示无条件 Transition。
- 默认创建的 `TransitionRuleGraph` MUST 作为所属 `StateMachineGraph` asset 的内嵌 sub-asset，避免规则图在文件系统散落。
- 需要复用时，用户 MUST 通过显式命令将内嵌规则图抽取为独立 shared asset，或在边 Inspector 中分配已有独立 `TransitionRuleGraph` asset。
- 删除 Transition 或清理规则图引用时 MUST 按 ownership 处理：embedded 跟随 Transition 删除，shared asset 只断开引用；非空 embedded 规则图删除前必须确认。
- `AnyState` Transition MUST 配置规则图，避免每帧无条件抢占 active state。
- `TransitionRuleGraph` 使用现有 `BaseTree/BaseGraph` 资产入口、节点集合、属性端口和字段访问器，不新增 Workbench、并行端口协议或 fallback 条件字段。
- `TransitionRuleGraph` 默认包含一个规则结果节点，规则图通过该结果节点的 Bool 输入决定输出。
- InputAction、黑板变量、后续 pipeline facts/tags 只作为规则图里的输入来源或谓词节点，不再由 Transition 边直接引用某个 Bool port。
- Runtime 从 “读取 edge 上的 Bool port 引用” 改为 “按优先级枚举 edge，再求值 edge 的规则图”。

## Non-Goals

- 不引入 `TransitionNode`。
- 不把 Transition 条件做成普通 `SubTree` 或 `RunnableTree`。
- 不做新的 Workbench 图、并行端口注册表或独立边集合。
- 不把 tag、priority、trigger 全部塞进规则图：priority 仍是边调度属性；tag/fact 是规则图读取的数据来源。
- 不在本变更里实现完整 gameplay pipeline facts；规则图只预留正式接入位置。

## Spec Conflicts To Resolve

- `btsmtl-sm-node-authoring` 当前写着 Transition 条件直接引用同层 `ValueNode` 的 Bool `PropertyPort`；本变更将它改为引用 `TransitionRuleGraph`。
- `btsmtl-sm-node-authoring` 当前写着 `StateMachineGraph` 可以包含条件用 `ValueNode`；本变更将条件节点移出状态机图本层，只允许它们存在于 `TransitionRuleGraph`。
- `btsmtl-input-action-node-authoring` 当前写着 InputAction Bool 节点可直接作为 Transition 条件来源；本变更将它改为规则图内的输入来源。

## Business Tradeoffs

- 规则图下钻比边 Inspector 表单更重，但更适合动作状态机的真实条件：输入、动画时间、窗口、tag、资源、命中反馈会组合增长，表单会很快变成另一套小语言。
- 规则图不是普通行为树，因为 Transition 需要的是纯判断，不是执行生命周期。把它做成 `RunnableTree` 会让条件求值带副作用，后续调试和确定性都会变差。
- 每条 Transition 默认私有内嵌规则图最接近 UE 的 transition rule 心智：规则图属于边，不污染项目目录。后续真的需要复用完整规则图时，再显式抽取为 shared asset；这比默认外部资产更干净，但需要编辑器明确显示规则图归属，避免误改 shared rule 影响多条边。
- Embedded rule 跟随边生死能避免孤儿 sub-asset，但非空规则图删除有误触风险；因此 UI 用明确文案和确认弹窗解决误触，而不是保留“删边但留下 embedded rule”的垃圾路径。
