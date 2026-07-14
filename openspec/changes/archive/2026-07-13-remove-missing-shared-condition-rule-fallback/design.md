# Design: ConditionRuleGraph 引用 ownership 与错误闭合

## Context

`BaseEdge` 目前只通过 inline graph 与 shared asset 是否为空推断 ConditionRuleGraph 来源。Unity 删除 shared asset 后，该引用在布尔语义上会表现为 null；`StateMachineGraph.CheckInit()` 随后把 edge 视为“没有条件图”，自动创建默认 inline `ConditionRuleGraph`。

这会抹掉作者原本选择 shared 复用的事实。BT edge 的 spec 已要求保留错误并等待显式作者操作，StateMachine edge 却没有同样的边界，导致同一 `BaseEdge` 模型在不同运行入口出现分裂语义。

## Decisions

### Decision: ownership 是持久数据，不从对象 null 推断

`BaseEdge` 保存显式 ConditionRuleGraph ownership。合法值是 Inline 与 Shared；Unspecified 表示旧数据尚未迁移或结构非法。resolved graph 只在 ownership 与实际数据匹配时存在。

业务取舍：序列化字段增加，但“作者选择 shared”在 asset 删除后仍可被诊断，不会被 Unity 的 fake-null 语义吞掉。

### Decision: 新建自动 inline，损坏引用不自动修复

合法新 Transition/BT child edge 创建时，系统仍自动建立空的 inline `ConditionRuleGraph`，这是默认 private-first authoring，不是 fallback。已经标记 Shared 的 edge 缺失 asset、类型不匹配或双持有数据时必须保持 invalid，只有作者显式 Replace Shared 或 Use Inline 才能改变 ownership。

业务取舍：新建流程依旧轻量；已配置 edge 出错时会阻断流转而不是悄悄改变玩法条件。

### Decision: runtime fail closed

StateMachine 与 BT runtime 遇到 invalid ConditionRuleGraph ownership 时必须报告错误且让该 edge 条件失败。它们不得把错误 edge 当作无条件通过，也不得从同层其它规则图、旧 BoolPort 或默认 true 获取替代结果。

业务取舍：坏配置可能使某个状态无法切换，但不会让角色在错误条件下自动攻击、闪避或抢占。

### Decision: 迁移只写有效事实，不恢复 legacy 语义

editor-only migration 扫描所有 `BaseEdge`：有效 inline 数据标为 Inline，有效 shared `ConditionRuleGraph` asset 标为 Shared。双持有、缺失、错误类型或无法判断来源的 edge 报错并保持 invalid；迁移不生成 inline 图、不猜测 source、不复制 shared 数据。

业务取舍：部分旧资产需要作者处理，但迁移后的数据有唯一 ownership，不会长期保留按空引用推断的兼容代码。

## Alternatives

### 方案一：继续在 CheckInit 自动创建 inline

优点是编辑器打开旧资产时不容易报错。缺点是 shared 规则被替换成空规则，直接改变行为；不采用。

### 方案二：仅在 validator 报错，但 runtime 继续当无条件 edge

优点是改动小。缺点是未运行 validator 的路径仍可能通过错误 Transition；不采用。

### 方案三：保留旧空引用推断并增加 warning

优点是兼容旧资产。缺点是 warning 不会恢复作者意图，且保留隐式第二条数据解析路径；不采用。

## Risks

- 所有现有 edge 必须在一次迁移中写入明确 ownership；遗漏 edge 必须报错，不得运行时默认 Inline。
- BT 无条件 child edge 与 Shared 缺失 edge 必须区分：前者是未配置，后者是断裂配置。
- 编辑器修改 UI 只能在作者显式命令下切换 ownership，不能因刷新、打开或校验改写资产。

