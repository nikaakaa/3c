# Change: 收口动作切换准入与时间窗口作者链路

## Why

当前 Corin 已经把攻击主段、后摇、闪避恢复和连段时间写进 Timeline，但“什么时候能切换”仍通过 `Attack1Cancel`、`Attack1MoveCancel`、`DodgeRecoveryCancel` 等业务变量名直接接到 Transition。与此同时，目标动作能否替换当前 Action 的粗粒度策略只在 `ActivateActionInstance` 内验证，ConditionRuleGraph 无法在选择边之前复用同一判断；激活提交还会在内部隐式取消旧 Action。

这造成四个问题：

- 每新增一种“来源动作 × 目标动作”组合，就倾向新增专用 Bool、专用条件节点或硬编码分支。
- Timeline 已经生成正式 ActionWindow projection，Transition 却只读裸 Bool，窗口的 `WindowType`、`ActionInstanceId` 和来源身份在逻辑选择点丢失。
- Transition 可能先选中一条目标动作实际不能激活的边，或者纯查询与最终激活使用不同规则。
- source Action 的 Cancel 与 target Action 的 Activate 混在激活实现中，Graph 看不清完整生命周期，也容易与 OnExit 重复提交。

需要把动作切换统一为：Timeline 只定义时间，ActionProfile 只定义动作间的粗粒度许可，State Transition 明确表达目标与优先级，Action lifecycle 明确关闭 source 后再启动 target。作者随后只需微调 Timeline TreeClip 的窗口范围，不必继续扩展专用运行时代码。

## What Changes

- 新增通用纯条件节点 `ActionWindowActiveInfoNode`，按 `WindowType` 查询当前 active ActionInstance 在当前逻辑帧已经暂存的 ActionWindow projection，不消费输入、不写 Blackboard、不创建第二套窗口状态。
- 新增通用纯条件节点 `CanActivateActionInfoNode`，按目标 ActionProfile 预览动作准入；纯查询与 `ActivateActionInstance` 复用唯一的 numeric-neutral admission 规则。
- 将动作准入规则从 Float32/Fixed Action runtime 的重复实现中抽出，通过窄状态端口读取 catalog、统一 Gameplay Effect tags 和当前 ActionInstance。
- 移除 `ActivateActionInstance` 对旧 Action 的隐式自动 Cancel。State source OnExit 必须先提交一次明确 lifecycle terminal，replacement target 再创建新 ActionInstance；source 尚未关闭时激活必须明确失败。
- 保持 Decision TreeClip + local Bool Frame declaration 为 Timeline Window 唯一时间作者入口；ConditionRuleGraph 允许读取同一 projection stage 的 typed WindowType 查询，不新增 ActionWindow cache、registry 或 WindowTrack。
- 明确 State transition 条件的词法可见范围：条件图可读取普通祖先图和 source StateNode 的直接 body graph，不得读取 target body、兄弟 state body 或任意后代状态的局部窗口。
- 将 Corin 攻击窗口迁移为 `ComboAccept`、`RecoveryEarly`、`RecoveryLate`，将闪避恢复窗口迁移为 `RecoveryOpen`；窗口 declaration 下沉到对应 inline Timeline/TreeClip owner，不再把每段 Cancel 变量堆在 RootTree Blackboard。
- 将 ActionProfile granted tags 在 ActionInstance 激活时以稳定 instance source 写入唯一 Gameplay Effect Tag Container，并在 Complete、Cancel、Interrupt、Abort 时精确撤销；Action admission 与 BTSMTL Tag query 不再读取两份角色 Tag 真相。
- 将 Corin ActionProfile tag/query 配置为通用 Attack/Dodge 粗粒度替换许可；具体 Attack→Dodge、Attack→Combo、Attack→Move、Dodge→普通 Attack1、Dodge→Dodge、Dodge→Move 路线和优先级继续写在 StateMachine edge。
- Corin 攻击后摇使用 `Dodge > Combo > Move > Natural Complete` 的稳定边优先级；Attack1→2→3→4→5 是有限连段，Attack5 不得循环回 Attack1。闪避恢复使用 `Attack > Dodge > Move > Natural Complete`，其中 Attack 进入普通 Attack1。无输入时完整播放后摇并回 Idle，闪避自然结束且无移动输入时不得经过 RunEnd。
- 嵌套 Action StateMachine 采用单向完成路由：Attack/Dodge leaf edge 读取 leaf-local Timeline window 并退出内层状态机，外层 Attack/Dodge edge 只能在 `state_root_completed` 后结合仍未消费的 request、target admission 或 Move 输入选择目标，不得跨层重复读取 leaf window。
- 将 locomotion ownership 收口为唯一 `HasActionLocomotionOwnership` pipeline Blackboard 事实，删除 `IsDodging`、`ResumeLocomotionThroughRunEnd` 及同义恢复路由变量。
- 将 Gameplay Semantic IR operation set 升级到 `/5`，为两个纯查询 operation 建立唯一 Emitter、source map、Float32/Fixed lowering 和 target capability 校验，不保留 `/4` reader 或 fallback。
- 将 BTSMTL Agent Snapshot/Patch schema 升级到 v9，增加 typed ActionWindow/ActionAdmission 条件项和通用 Blackboard、TreeClip、Transition、ActionProfile/tag mutation；dry-run/apply 继续消费同一 typed command plan。
- 使用正式 Agent v9 export → dry-run → apply → export → validate 链迁移 Corin 资产，并重新生成唯一 Semantic IR、Float32/Fixed Program 与 Presentation Projection。
- 删除旧 per-state Cancel/MoveCancel declaration、旧 Transition 条件、隐式 activation cancel、Agent v8 schema、legacy RushAttack state/route、Corin 固定默认 macro 和精确快照硬编码，不保留兼容或双写路径。

## Non-Goals

- 不新增全局 cancel priority manager、通用 Gameplay Ability System、输入连招脚本或动画状态机。
- 不改变输入 request buffer、输入有效期、消费序列或网络模型协议。
- 不把 Animation、Animancer CrossFade、MotionCurve 或 Presentation layer 作为动作打断裁决者。
- 不新增 Attack/Dodge 专用 runtime opcode、专用 Transition 节点或一次性 SubTree/Timeline asset。
- 不实现成功闪避、成功格挡、反击资格或 `Corin_Attack_Counter_WithWeaponRootmotion` 路由；这些能力必须由未来正式 Combat Resolution 产生 Gameplay Effect Tag 后另行闭环，不得用“上一状态是 Dodge”冒充成功判定。
- 不替作者决定最终手感帧数；迁移只建立明确的初始窗口语义，后续仍由作者在 Timeline 中微调。
- 不新增测试或手动验证任务，不运行 Unity batchmode。

## Dependencies

- 依赖 `refactor-corin-action-combo-authoring` 已生成的五段攻击、后摇动画、嵌套 Action StateMachine、Dodge 内层状态机与现有窗口范围，作为本 change 的唯一保留输入；其中错误创建的 RushAttack state/route 作为 legacy 数据删除。
- 依赖 `refactor-gameplay-runtime-and-tooling-modules` 已建立的 numeric-neutral portable runtime 边界；本 change 不重新复制 Float32/Fixed 业务控制流。
- 依赖现有 TreeClip scope projection、Action Context、Gameplay Effect tag state、State source-exit barrier 与 Agent typed command plan。

## Current Spec Comparison

- 当前 `character-state-interruption-authoring` 明确禁止 ConditionRuleGraph 使用 ActionWindow reader，并强制 Transition 读取 Blackboard Bool。本 change 修改为：TreeClip/Scope Variable 仍是唯一作者入口，但 Transition 可通过纯 typed reader 读取同一暂存 projection。
- 当前 `character-action-authoring-closure` 使用 `IsDodging` 和 `CanDodgeMoveCancel`。本 change 用通用 ownership fact 与 `RecoveryOpen` 取代它们，不保留别名。
- 当前 `character-state-timeline-authoring-loop` 规定 ActionOverride 无输入离开时进入 RunEnd，且只描述 Attack1/Attack2。本 change 改为无输入回 Idle，并把五段有限攻击、分级后摇窗口和 Attack→Dodge 纳入正式闭环。
- 已完成但未归档的 `refactor-corin-action-combo-authoring` 规定 per-state `AttackNCancel/AttackNMoveCancel`、`DodgeRecoveryCancel`、`ResumeLocomotionThroughRunEnd`，并把普通 Attack→Dodge 作为非目标。本 change 明确取代这些局部决策；旧数据必须迁移并删除，两个方案不得共存。
- 当前 `agent-character-controller-synthesis` 仍以 schema v8 和 Corin 业务样例为边界。本 change 升级到 v9，并把通用 authoring 验证与具体 Corin 迁移完全分开。

## Impact

- Affected specs:
  - `character-state-interruption-authoring`
  - `character-action-activation-flow`
  - `character-action-authoring-closure`
  - `character-state-timeline-authoring-loop`
  - `btsmtl-gameplay-semantic-ir`
  - `agent-character-controller-synthesis`
  - `btsmtl-graph-data-catalog-authoring`
  - `character-pipeline-blackboard`
  - `gameplay-tag-runtime`
- Affected code:
  - BTSMTL Character condition nodes、node registry 与 ConditionRule editor
  - Graph Data Catalog、Pipeline Blackboard projection editor 与 owner 导航
  - Action admission/lifecycle portable runtime、Gameplay Effect Tag Container、Float32/Fixed target ports
  - Semantic IR emitter、operation catalog、compiler/lowering、codec/source map
  - Agent Snapshot/Patch DTO、lowerer、handler、emitter、validator、macro/evaluator 与 MCP bridge
- Affected assets:
  - Corin RootTree、inline Action/Attack/Dodge StateMachine、inline Timeline TreeClip/local declarations；legacy RushAttack state/route 被删除
  - State transition ConditionRuleGraph 的 source-body lexical scope 与嵌套状态机完成路由
  - Corin ActionProfile、Gameplay tag catalog、generated Semantic IR/Programs/Projection
- Breaking changes:
  - Agent schema v8 与 operation set `/4` 不再读取。
  - 旧 Cancel/MoveCancel Blackboard key 与隐式 activation cancel 行为被删除。
  - 无法安全通过 Agent v9 保留 TreeClip、declaration、edge 和 ActionProfile identity 时，apply 必须停止并说明缺口，不能直接改 YAML 或建立临时迁移器。
