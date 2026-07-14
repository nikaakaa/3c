# Design: Corin Attack 嵌套状态机与层级运行时

## Context

当前 Corin Action 图的正式结构为：

```text
Action StateMachine
  None
  Attack1
  Attack2
  DodgeBack
  DodgeForward
```

`Attack1/Attack2` 各自拥有 StateBehaviorSubTree：OnEnter 激活 ActionProfile 并写 Action Context，Root 播放 inline Timeline，OnExit 根据 StateExitContext 提交 Complete、Cancel 或 Abort。两段攻击之间通过 Cancel TreeClip 写入的 Frame Blackboard variable 与 Attack request 组成 transition rule。

该业务链路本身应保留，问题是它与 Dodge 平铺在同一状态层。目标结构为：

```text
Action StateMachine
  None
  Attack
    StateBehaviorSubTree.Root
      Attack Combo StateMachineNode
        Attack1
        Attack2
        Exit
  DodgeBack
  DodgeForward
```

现有 runtime 已使用 `Stack<StateMachineExecutionScope>` 包住状态 body tick，因此嵌套执行期间能够看到 outer -> inner scope 顺序。但 `TimelinePlaybackRequest` 和 Blackboard access 只读取栈顶，`AnimationTransitionRequest` 也只保存单个 StateMachine runtime id。它们尚未把层级关系变成正式合同。

## Goals

- 外层 Action 图只表达动作大类，内层 Attack 图只表达攻击段位与连段。
- 具体攻击状态继续拥有自己的 Action Context、Timeline、窗口和 lifecycle。
- 外层 Attack state 可以拥有跨整个连段存活的局部变量，内层 Attack1/Attack2 可以拥有逐段清理的局部变量。
- 父层 transition、内层 combo transition 和 Tree abort 在同一 Action 表现域中只形成一个有效 animation transition 生命周期。
- 所有迁移使用 inline graph data，不创建一次性 SubTree、StateMachineGraph 或 Timeline asset。

## Non-Goals

- 不把 StateMachineGraph 改成可直接包含 StateMachineNode；嵌套入口仍位于 StateNode 的行为图。
- 不让外层 Attack state 复制内层 Action lifecycle 或 Action Context。
- 不让动画层根据节点名、Graph path 或 contribution 缺席猜测嵌套关系。
- 不增加 Corin 专用 Combo runtime、owner alias 表或停止旁路。

## Decisions

### Decision: Action 大类和 Attack 段位分成两层 StateMachine

外层 Action StateMachine 只保留 `None`、`Attack`、`DodgeBack`、`DodgeForward`。`Attack` 是结构状态，它的 Root 只运行一个普通 `StateMachineNode`。内层 Attack StateMachine 使用 `Attack1`、`Attack2` 和控制节点 `Enter/AnyState/Exit`。

外层 `None -> Attack` 只查询 Attack request，不消费。内层初始 `Attack1.OnEnter` 继续通过正式 activation 节点消费 request、激活 ActionProfile 并输出 Action Context。`Attack1 -> Attack2` 与 `Attack2 -> Attack1` 继续使用 Cancel window AND Attack request；target activation 消费 request。正常完成进入内层 Exit，随后外层 Attack root completed 边回到 None。

业务取舍：外层图不再直接显示每一击，但作者下钻 Attack 后能看到完整连段；新增攻击段只改内层图，不会让 Dodge 与其它 Action category 的边数量指数增长。

### Decision: 具体攻击 leaf 独占 Action lifecycle

`Attack1/Attack2` 保留各自 OnEnter、Root 和 OnExit。外层 `Attack.OnEnter/OnExit` 不激活 ActionProfile、不保存 Action Context，也不提交 Complete/Cancel/Abort。外层只负责嵌套 StateMachineNode 的启动、等待与停止。

业务取舍：每一击仍是一笔独立可预测、可拒绝、可取消的 ActionInstance，网络和调试身份不因结构分组而模糊；代价是作者需要下钻一层查看具体 Action Context，但不会遇到父子两层重复 terminal transition。

### Decision: execution path 保存 outer 到 inner 的 activation frame

运行时引入只读 `StateMachineExecutionPath` 或等价值，按 outer -> inner 保存当前 activation frame。每个 frame 至少包含 StateMachine runtime identity、State identity、activation generation，以及能定位对应 State body Graph runtime/owner 的 identity。

Blackboard resolver 不直接使用“最内层 scope”处理所有 State declaration。它根据 declaration owner 与 Graph ownership 选择 path 中对应 frame：

- 声明在外层 `Attack State Body` 的 State variable 绑定外层 Attack activation，在 Attack1 -> Attack2 期间保持。
- 声明在 `Attack1 State Body` 的 State variable 绑定 Attack1 activation，离开 Attack1 时清理。
- 内层图引用 Character 或外层 Graph declaration 时继续使用显式 declaration reference，不复制、不按 key shadow。
- 找不到唯一 owner frame 时直接报告配置/runtime 错误，不降级到栈顶或 Character scope。

业务取舍：多传递一个结构化 path，但局部变量的业务生命周期与作者看到的 Graph ownership 一致，也为后续更深层状态机保留统一模型。

### Decision: 嵌套 StateMachine 共享根 animation transition domain

每个顶层并行 StateMachineNode 建立独立 presentation transition domain，例如 Locomotion domain 与 Action domain。状态 body 中嵌套的 StateMachineNode 继承当前 domain，不按内层 runtime id 再开一个并行 domain。

每个逻辑 State activation 仍拥有稳定 owner；动画 contribution 归属当前 active presentation leaf，例如 Attack1 或 Attack2。运行时维护祖先结构 owner 到当前 presentation leaf owner 的显式绑定：

- 父层 `None -> Attack` 的 target 在 Attack body 首次执行后解析为内层 Attack1 leaf owner。
- 内层 `Attack1 -> Attack2` 直接以两个 leaf owner 发布 handoff。
- 父层 `Attack -> DodgeForward` 的 source 解析为当前 Attack leaf，target 解析为 DodgeForward owner。
- 父层 `Attack -> None` 的 source 解析为最后 active Attack leaf，target 是显式空表现 owner。

同一 domain 同时最多一个 active transition。父子 runtime 在同一 logic tick 连续提交 transition 时，后提交的祖先 transition 必须从当前最终视觉结果 supersede 前一个 transition，不能并行叠加两套权重。不同 Locomotion/Action domain 仍可并行。

业务取舍：保留 leaf 级调试和 contribution ownership，同时让父层大类 transition 得到真正的源/目标动画。代价是 lifecycle command 需要携带 domain 和逻辑 owner/leaf owner 关系，但不需要复制 Registry 或新增 Action 专用混合器。

### Decision: 父层停止逐层关闭逻辑，表现 handoff 由 domain 收敛

当外层 Attack 被 State transition、Self/LowerPriority abort、Parent stop 或 ForceStop 关闭时：

1. 外层 State root 对嵌套 StateMachineNode 发出原始 stop context。
2. 内层 active Attack state 停止 Timeline gameplay 采样并执行 State.OnExit。
3. 内层 Action lifecycle 根据 StateExitContext 提交一次明确 terminal transition。
4. 内层 StateMachineNode terminal 后，外层 Attack State.OnExit 完成但不再提交 Action lifecycle。
5. transition domain 将同 tick 的内层 leaf release 与外层 replacement/Empty handoff 收敛为一个最终表现 transition。

`OriginCause`、replacement edge/node、logic tick 和 animation definition authority 必须沿调用链保持。ForceStop 仍不伪造 gameplay Cancel/Abort，但必须立即释放内层 Timeline、Action Context 本地句柄、Blackboard State bucket 和 animation membership。

业务取舍：逻辑收口仍是逐层、可解释的，表现切换则是单域单 authority；避免为了动画淡出继续 tick 攻击逻辑，也避免父子各自淡出一次。

### Decision: Agent authoring 显式表达 nested_state_machine

Snapshot 必须递归输出 State body 内的 StateMachineNode、resolved graph path、states、transitions 和 ownership。Macro/Patch IR 使用普通 `StateMachineNode + StateMachineGraph + StateNode` 指令表达嵌套，不新增 Attack 专用 opcode。Validator 检查：

- Attack1/Attack2 不再位于外层 Action StateMachine。
- Attack state body 恰有一个承担 Root 的 Attack Combo StateMachineNode。
- 内层 Attack states 保持 inline body、Action Context、Timeline ownership 和 lifecycle。
- 父子 graph owner/path、execution path 可解析，且 transition domain 不分裂。

业务取舍：Agent 输出更深一层，但仍使用同一 BTSMTL authoring API；人类和 Agent 看到同一结构，不维护第二套简化 schema 真相。

## Migration

1. 记录 Corin 当前外层 Attack1/Attack2 GUID、body、TimelineData、TreeClip、declaration reference、transition definition 和 ConditionRuleGraph ownership。
2. 在外层 Action StateMachine 创建 `Attack` StateNode 和 inline StateBehaviorSubTree。
3. 在 Attack Root 创建普通 StateMachineNode，并使用 inline StateMachineGraph。
4. 将 Attack1/Attack2 StateNode 与 body 数据迁移到内层 graph，重绑 owner/path，不克隆业务真相。
5. 将 combo edge 和 rule graph 迁移到内层；正常完成边改连内层 Exit。
6. 将外层 None->Attack1 改为 None->Attack，并让外层 Attack->None 只读 nested root completed。
7. 删除外层旧 Attack1/Attack2、旧 edge、旧 orphan rule graph 和旧 path identity。
8. 重导出 Snapshot 并使用正式 validator 确认单一结构。

迁移必须原子完成。若 inline graph 序列化不能安全重绑 owner/path，实施必须停止并报告缺口，不创建 shared 临时资产或兼容镜像。

## Alternatives

### 方案一：继续平铺 Attack1/Attack2

优点是 runtime 无需变化。缺点是每增加攻击段、派生或武器动作都会扩大外层 Action 图，Dodge/Guard/HitReact 与 combo 边混杂。业务上无法形成稳定动作大类边界，不采用。

### 方案二：只迁移 authoring，不修改 scope 和 animation handoff

优点是资产改动快。缺点是外层 Attack owner 与内层 Timeline owner 分离，父子 transition 可能并行，Blackboard 也只能读取最内层 scope。运行看似能动但生命周期与表现不可信，违反单一正式链路，不采用。

### 方案三：新增 AttackComboNode 或专用 ComboRuntime

优点是可以针对攻击快速编码。缺点是复制 StateMachine、Transition、ConditionRuleGraph、stop 和 animation lifecycle，后续 Guard、技能或受击分组仍要再造一套。业务能力被锁进 Corin 特化路径，不采用。

### 方案四：使用通用嵌套 StateMachine execution path 与 transition domain

优点是 Attack、Guard、HitReact 和后续其它分组都能复用同一机制，Graph authoring 与 runtime 语义一致。缺点是需要修改 scope、handoff 和 validator 多个模块。本 change 采用该方案，因为它是唯一不产生分裂路径且能完整支持用户要求层级的方案。

## Risks

- 父子 runtime command 顺序错误可能让 parent handoff 捕获不到 leaf source；必须在同一 logic tick 保留稳定命令顺序和 source snapshot。
- declaration owner 到 execution frame 的映射错误可能让外层 Attack 变量被 Attack1 exit 提前清理；必须以 owner identity 显式解析。
- domain 继承错误可能把并行 Locomotion 与 Action transition 合并；顶层 StateMachineNode 必须各自创建 domain，只有嵌套节点继承。
- 资产迁移若复制而不是移动 managed-reference 数据，会留下两个 Attack1/Attack2 真相；validator 必须拒绝外层残留和 orphan owner/path。

