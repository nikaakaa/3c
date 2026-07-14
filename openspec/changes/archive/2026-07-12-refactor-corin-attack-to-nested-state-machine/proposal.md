# Change: 将 Corin Attack 重构为嵌套状态机

## Why

Corin 当前 `Action StateMachine` 将 `None`、`Attack1`、`Attack2`、`DodgeBack` 和 `DodgeForward` 平铺在同一层。这个结构把“动作大类切换”和“攻击连段阶段切换”混成了一套状态关系：增加 Attack3、蓄力、派生技或空中攻击时，外层 Action 图会持续膨胀；攻击内部的 CancelWindow、Action Context 和 lifecycle 也会泄漏到与 Dodge 同级的结构层。

现有 BTSMTL 已允许在 `StateBehaviorSubTree` 中放置 `StateMachineNode`，但 runtime 只把 scope 栈顶作为当前状态 owner，动画 transition 又按每个 `StateMachineGraphRuntime` 分别建域。若只迁移资产，外层 `Attack` state owner 不产动画，真正的 Timeline contribution 却归属内层 `Attack1/Attack2` owner，父子状态机可能分别发布 handoff，造成 target owner 空输出、重复淡出或不同 transition session 同时作用于同一 Action layer。

因此本 change 必须同时闭环 Attack 两层 authoring、嵌套状态执行路径、父子停止传播和单一动画 handoff 域，不能只做 YAML 搬运，也不能为 Corin 增加 Action 专用 runtime 旁路。

## What Changes

- 将 Corin 外层 `Action StateMachine` 收敛为 `None`、`Attack`、`DodgeBack` 和 `DodgeForward`。
- 让外层 `Attack` 的 inline `StateBehaviorSubTree.Root` 运行一个 inline-first `Attack Combo StateMachineNode`。
- 将现有 `Attack1`、`Attack2` StateNode、状态 body、ActionProfile 激活、Action Context、Timeline、Hit/Cancel TreeClip、OnExit lifecycle 和连段条件原子迁移到内层 Attack StateMachine。
- 将 `Attack1/Attack2` 正常完成边改为进入内层 `Exit`；外层 `Attack` 在嵌套 StateMachineNode 成功后通过 `StateRootCompleted` 回到 `None`。
- 保持初次攻击 request 在外层只查询不消费，由内层具体攻击 state 的 activation 节点消费并创建新的 Action Context。
- 为嵌套状态机建立有序 execution path，使局部 Graph、State Blackboard、ConditionRuleGraph、Timeline request 和 Action lifecycle 能解析到正确的外层或内层 activation frame。
- 为嵌套状态机建立继承的 animation transition domain，并把父层逻辑 state owner 解析到当前 active presentation leaf owner；同一 Action 域同时最多推进一个正式 transition。
- 让父状态 transition、Tree graceful abort 和 ForceStop 通过同一嵌套 stop 链逐层关闭内层 Timeline、Action lifecycle 和 State.OnExit，再由拥有该表现域的 transition authority 发布最终 handoff。
- 更新 Agent Snapshot、Macro、Patch Compiler 和 Validator，使其能表达、生成和校验嵌套 Attack StateMachine，而不是重新生成平铺 Attack1/Attack2。
- 删除 Corin 外层旧 `Attack1/Attack2` StateNode、旧同层 combo edge 和迁移后失去 owner 的 ConditionRuleGraph 数据，不保留兼容读取或双结构。

## Impact

- 受影响 current specs：
  - `btsmtl-sm-node-authoring`
  - `character-state-timeline-authoring-loop`
  - `character-state-interruption-authoring`
  - `character-animation-pipeline`
  - `character-pipeline-blackboard`
  - `agent-character-controller-synthesis`
- 受影响 active changes：
  - `refactor-pipeline-blackboard-owned-scopes` 已完成但尚未归档，本 change 必须以其 declaration owner 与 runtime address 结果为基线扩展嵌套 path 解析。
  - `refactor-animation-transition-lifecycle` 已完成但尚未归档，本 change 必须扩展其 transition domain 与 owner handoff，不恢复旧 Registry transition 路径。
  - `refactor-timeline-window-authoring-to-treeclips` 已完成但尚未归档，本 change 必须以其 Decision TreeClip + scope variable + ActionWindow projection 为攻击窗口真相；current `character-action-authoring-closure` 中仍残留的 ActionWindowTrack 描述已过期，不能在本 change 中恢复。
  - `refactor-timeline-node-inline-shared-authoring` 已完成但尚未归档，Attack Timeline 继续保持 TimelineNode inline data，不恢复独立一次性 Timeline asset。
- 主要代码影响：`StateMachineGraphRuntime`、`StateMachineNode`、execution scope/context、Pipeline Blackboard scope resolver、动画 lifecycle command/transition runtime、Agent snapshot/compiler/validator。
- 主要资产影响：`CorinPlayableRootTree.asset` 中 Action StateMachine 与 Attack1/Attack2 inline graph ownership。
- 不新增测试；实施时使用编译、Agent snapshot/validator、静态资产结构检查和 OpenSpec strict validate 作为自动验证。

本 change 明确替换 current `character-state-timeline-authoring-loop` 与 `agent-character-controller-synthesis` 中把 Attack1/Attack2 放在外层 Action StateMachine 的平铺口径。Timeline window 的 current spec 冲突不属于本 change 新设计，而是上述已完成 TreeClip change 尚未归档造成的基线时序；apply 前必须以较新的 TreeClip 结果为准。

## Out of Scope

- 不新增 Attack3、重攻击、蓄力、空中攻击、受击或武器切换内容。
- 不新增或替换动画资源，不调整具体动画帧和 MotionCurve 数值。
- 不改变 Hit/Cancel Window 的 TreeClip 帧范围、WindowId、Digest 或 ActionProfile 网络策略。
- 不实现服务端命中、伤害或真实 Fantasy backend。
- 不创建 `AttackStateMachineNode`、`ActionStateNode`、`ComboRuntime` 等业务特化节点或运行时。
