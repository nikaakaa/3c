# Design: 动作切换准入与时间窗口作者链路

## Context

Corin 当前已经具备嵌套 Action StateMachine、五段普通攻击、闪避、攻击后摇动画和 Timeline Decision TreeClip，并遗留了没有真实成功闪避判定支撑的 RushAttack state。真正没有收口的是“切换资格”的表达：

```text
Timeline TreeClip
    -> 写 Attack1Cancel / Attack1MoveCancel / DodgeRecoveryCancel Bool
    -> ConditionRuleGraph 直接读具体 key
    -> State edge 选 target
    -> ActivateActionInstance 再做一次 ActionProfile 验证
    -> 激活内部可能自动取消旧 Action
```

这条链把时间语义变成了 RootTree 上的业务变量名，把目标动作准入推迟到选边之后，又把 source lifecycle 藏进 target activation。业务表现出来的问题就是：攻击、移动、闪避需要不同后摇取消时点时，作者不知道应该加窗口、加变量、加边还是改代码。

## Goals

- 作者在 Timeline 上只调整窗口范围，就能改变攻击/闪避前后摇手感。
- ConditionRuleGraph 使用普通 And/Or/Not 组合输入请求、typed window 和 target admission，不新增目标专用节点。
- ActionProfile 只回答“目标动作原则上能否替换当前动作”，不回答具体时间与边优先级。
- StateMachine edge 继续拥有明确 target、路由和稳定 priority。
- source Action lifecycle 在 source OnExit 中明确结束，target activation 不再偷偷修改 source。
- Float32 与 Fixed Program 从同一 Semantic IR operation 获得相同业务语义。
- Agent 工具能正式完成全部 BTSMTL、Timeline、Blackboard 与 ActionProfile 迁移，并在以后继续修改同一资产。
- ActionProfile granted tags 与 Gameplay Effect granted tags 进入同一个按来源管理的 Tag Container，BTSMTL 与 Action admission 读取同一角色 Tag 真相。

## Non-Goals

- 不实现 UE Gameplay Ability System、Montage section、Ability Task 或 Gameplay Event 总线。
- 不实现全局“任何动作都能按矩阵互相取消”的中央裁决器。
- 不让网络层、动画层或 Motion solver 选择 gameplay transition。
- 不重做输入 request buffer、StateMachine stop barrier 或 Timeline projection。
- 不用一个通用字符串字典替代 typed authoring contract。
- 不实现 PerfectDodge、PerfectGuard、CounterReady 或反击动画；没有 Combat Resolution 成功事实时必须删除 RushAttack 假路由。

## Decision 1: 四类作者事实分开拥有

动作切换由四类信息共同决定：

```text
时间资格      Timeline TreeClip + local projected declaration
粗粒度许可    Target ActionProfile tag/query
具体路由      StateMachine transition edge
冲突优先级    同 source edge priority
```

以 Attack→Dodge 为例：

```text
Dodge request
AND ActionWindowActive(RecoveryEarly)
AND CanActivateAction(DodgeProfile)
    -> Attack leaf source-exit
    -> Cancel(RecoveryCancel)
    -> Dodge target activation
```

这里没有一个组件单独决定全部结果。Timeline 不知道目标是 Dodge，ActionProfile 不知道当前是第几帧，edge 不复制 tag policy，动画只消费已提交 producer。

### Tradeoff

- 好处：新增技能时通常只增加 ActionProfile、Timeline window 和普通 edge，不修改通用 runtime。
- 代价：作者需要理解三个可见配置位置，但每个位置只回答一个问题，Runtime Debug 可以沿同一决策链展示结果。
- 不采用全局 cancel matrix：它会把具体状态路线和优先级从图里搬到第二份资产，导致 StateMachine 与矩阵双真相。

## Decision 2: 只新增两个通用纯条件节点

### ActionWindowActiveInfoNode

配置输入只有 `WindowType`。节点读取当前 active ActionInstance 在当前逻辑帧已经暂存的 ActionWindow projection candidate，并返回 Bool。

约束：

- 只读，不消费 input request，不写 Blackboard，不提交 lifecycle。
- 必须匹配当前 active ActionInstanceId，不能误读上一动作、另一 actor 或历史帧窗口。
- 读取现有 projection candidate collection；不得创建 active-window registry、timeline decision cache 或第二条 fact 路径。
- 当前 runtime 只允许一个 active transaction Action，因此节点无需作者再连接一个 ambient ActionContext；如果以后允许并行动作，必须通过新的显式 capability 重新设计，不能暗中猜 action。
- declaration 的 `WindowId`、`Digest`、projection policy 和 debug identity 继续保留；条件只按业务 `WindowType` 匹配。

### CanActivateActionInfoNode

配置输入是目标 ActionProfile。节点调用共享 Action admission evaluator，在不修改状态的情况下返回目标当前能否激活。

约束：

- 不消费 request，不创建 ActionInstance，不取消 source。
- 读取与实际 activation 相同的 catalog/profile、统一 Gameplay Effect state、active ActionInstance 和 target cancel/block query。
- 输出只是当前 Tick 的预览，不是 reservation token；最终 activation 必须再次调用同一 evaluator。

### Tradeoff

- 好处：ConditionRuleGraph 用普通 `Request AND Window AND Admission` 即可组合攻击、闪避、技能和 AI 行为。
- 代价：两个节点会进入 Semantic IR operation set，Float32/Fixed 都必须正式实现。
- 不采用 `AttackDodgeCancelNode`：它把角色业务、输入、时间和目标策略写死在一个节点，下一种取消关系还要继续加节点。
- 不让作者直接读 `Attack1Cancel` Bool：裸 Bool 能决定 true/false，但丢失 ActionInstance 与 WindowType 身份，也迫使 RootTree 暴露大量一次性变量。

## Decision 3: Window 仍只有一条作者与运行链

正式链路为：

```text
Decision TreeClip active range
    -> 写 inline Timeline owner 下的 Bool Frame declaration
    -> Blackboard runtime 依据 declaration projection metadata
       暂存 SimulationActionWindowProjectionCandidate
    -> ActionWindowActiveInfo operation 在同帧只读 candidate
    -> EndFrame 将同一 candidate 刷成 ActionWindowFact
    -> Model policy 决定 history / replication
```

逻辑条件读取与网络/诊断输出共享同一个 candidate，但处于不同阶段。条件查询不会自己生成 fact，也不会延长窗口寿命。

local declaration 只属于对应 inline Timeline/TreeClip owner；RootTree Blackboard 不再公开 `Attack1Cancel` 等每段窗口 key。作者在 Timeline 中看到的是 `WindowType` 和帧范围，而不是跨图复制的路由变量。

State transition 的 ConditionRuleGraph 使用明确的词法可见范围：

```text
transition 所在 StateMachine graph
    + 普通祖先 graph
    + source StateNode 的直接 body graph
    -> condition 可见数据
```

source body 中的 inline Timeline 可以把 local declaration 投影为同一帧 ActionWindow candidate，因此从该 source State 离开的 edge 可以读取对应 `WindowType`。target State body、兄弟 State body和任意后代 leaf 的局部 declaration 不在可见范围内。编译器必须按同一规则建立引用并在越界时失败，Agent Snapshot、Inspector 与 Runtime 不得各自猜测另一套 scope。

Agent authoring service 在 export、dry-run、apply 和 validate 之前统一重绑 Graph 的只读节点与边引用。冷域重载后的第一次调用必须与后续调用得到相同的 source-body 词法可见范围，不能依赖先打开图窗口或先失败一次来触发 `CheckInit`。

### Tradeoff

- 好处：Timeline 仍是唯一时间真相，逻辑、同步与 Debug 保留同一 WindowId/Digest。
- 代价：Topology projection 与编译器都必须知道 source State body 的词法边界，并证明 TreeClip 写入发生在 Transition 查询之前；范围或顺序不满足时应编译失败，不能读取 target、兄弟状态或上一帧值。
- 不采用独立 ActionWindow runtime registry：现有 projection candidate 已经持有完整身份，重复注册只会形成分裂路径。

## Decision 4: Action admission 使用唯一 portable evaluator

新增 numeric-neutral `ActionAdmissionControl`，只依赖窄端口：

```text
TargetActionProfile
Current GameplayEffect Tag Container/attributes
Optional ActiveActionInstance + its granted tags
    -> Allowed / Rejected(reason)
```

它负责现有 ActionProfile admission 语义：目标 block 条件、资源/状态条件、active source 与 target cancel query 的关系，以及稳定拒绝原因。Float32/Fixed 只负责各自状态数据的 port 实现，不复制判断流程。

调用点只有：

- `CanActivateActionInfo`：纯预览。
- `ActivateActionInstance`：提交前复核并创建 target。

`ActivateActionInstance` 不再自动 Cancel active source。如果 active source 仍存在，它必须返回明确的 `SourceActionStillActive` 拒绝；StateMachine replacement 必须先完成 source stop barrier 和显式 OnExit lifecycle，再执行 target activation。

ActionProfile granted tags 不再只由 admission 临时拼接为一份私有角色 Tag 集合。ActionInstance 创建成功时，runtime 必须以 `action:<ActionInstanceId>` source 把 profile tags 写入 Gameplay Effect Tag Container；ActionInstance 进入任一 terminal lifecycle 时必须精确撤销该 source。Target block query、BTSMTL `HasGameplayTagNode` 与 Gameplay Effect requirement 读取同一 Container。Active source cancel query 可以读取 active ActionProfile 的不可变 tag 定义来描述来源动作类别，但不得形成第二份持久状态。

### Tradeoff

- 好处：边选择与最终提交不会使用两份规则，Action lifecycle 在图上可见且只提交一次。
- 代价：所有现有依赖“激活新动作顺便取消旧动作”的 Graph 都必须迁移；漏配会明确报错，而不是继续运行。
- 不保留隐式 cancel fallback：它会继续掩盖 source OnExit 缺失，并可能产生重复 terminal transition。

## Decision 5: Corin 使用语义窗口，不使用目标专用窗口名

窗口类型定义为：

| WindowType | 业务含义 | 初始迁移来源 |
|---|---|---|
| `ComboAccept` | 接受下一段攻击输入 | 保留现有 Attack1..4 combo range；Attack5 不创建循环窗口路由 |
| `RecoveryEarly` | 后摇较早阶段，允许高机动替换 | 从每段 End clip 开始到自然结束 |
| `RecoveryLate` | 后摇较晚阶段，允许普通移动恢复 | 保留现有 MoveCancel range |
| `RecoveryOpen` | 闪避承诺段结束后的开放恢复期 | 保留 DodgeRecoveryCancel range |

它们描述“当前动作处于什么阶段”，不描述“具体要去哪个 target”。因此 `RecoveryEarly` 可被 Dodge edge 使用，未来也可被具有相同业务语义的技能 edge 使用；是否允许仍需 `CanActivateAction(target)`。

Corin ActionProfile policy：

- Attack profile granted tag 增加 `Attack`。
- Dodge profile 保留 `Dodge`。
- Attack target cancel query 允许匹配 active `Attack` 或 `Dodge`。
- Dodge target cancel query 允许匹配 active `Attack` 或 `Dodge`。
- 时间资格和目标边不进入 profile。

## Decision 6: Corin 路由与优先级留在层级状态机

嵌套状态机只允许一条向外完成链：leaf edge 决定“当前具体动作何时可以离开”，category edge 决定“内层已经离开后进入哪个外层动作大类”。外层 edge 不得跨层读取任意 leaf 的 `ComboAccept`、`RecoveryEarly`、`RecoveryLate` 或 `RecoveryOpen`。

```text
leaf-local Timeline window
    -> inner leaf transition
    -> inner StateMachine Exit / state_root_completed
    -> outer category transition
    -> target category Enter
```

### Attack leaf

同一 source 的稳定优先级：

```text
1. Dodge request + RecoveryEarly + CanActivate(Dodge)
2. Attack request + ComboAccept + CanActivate(next Attack)
3. Move input + RecoveryLate
4. Timeline natural complete
```

- Attack1→2→3→4→5；Attack5 是终段，不得以重复 Attack 输入回到 Attack1。
- Move 只能取消后半段恢复，Dodge 可更早取消。
- 无有效输入时完整播放 End clip，Complete 后退出 Action 并回 Idle。
- Dodge 或 Move replacement 命中时，leaf edge 只退出 Attack 内层状态机；外层 Attack→Dodge 使用 `state_root_completed + Dodge request + CanActivate(Dodge)`，Attack→None 的 Move 路由使用 `state_root_completed + Move input`，自然完成使用单独的 `state_root_completed` 边。

### Dodge leaf

`RecoveryOpen` 前不响应普通 Attack、再次 Dodge 或移动恢复；窗口打开后：

```text
1. Attack request + CanActivate(Attack)
2. Dodge request + CanActivate(Dodge)
3. Move input
4. Timeline natural complete
```

- Attack 退出 Dodge 并进入普通 Attack1，不根据上一状态选择特殊攻击。
- Dodge 可按输入方向重入对应 Dodge leaf。
- Move 进入 locomotion RunLoop。
- 无输入自然结束时进入 Idle，不播放 RunEnd。
- Attack 或 Move replacement 命中时，leaf edge 只退出 Dodge 内层状态机；外层 Dodge→Attack 使用 `state_root_completed + Attack request + CanActivate(Attack)`，Dodge→None 的 Move 路由使用 `state_root_completed + Move input`，自然完成使用单独的 `state_root_completed` 边。

request query 在内外两层都只读。只有最终 target leaf 的 `ActivateActionInstance` 消费 request；因此外层路由可以在 inner source 已完成后继续看到同一个请求，同时不会重复激活动作。

边 priority 是当前业务图的明确选择，不提升成 runtime 全局 action priority。以后技能对同一请求有不同优先级时，作者直接在 source state 的边上看到并调整。

## Decision 7: Locomotion 只消费一个动作所有权事实

`IsDodging` 改为 `HasActionLocomotionOwnership`。任何 full-body Action 在成功激活后设置 true，所有 source exit 设置 false。Locomotion 只据此进入/离开 `ActionOverride`：

```text
ownership=true                  -> ActionOverride
ownership=false + Move present  -> RunLoop
ownership=false + no Move       -> Idle
```

删除 `ResumeLocomotionThroughRunEnd`。RunEnd 只表示从正在跑动的 locomotion 自然减速停止，不再被当成任何 Action 结束后的通用恢复动画。

### Tradeoff

- 好处：攻击、闪避和未来 full-body 技能共用同一所有权合同，Locomotion 不知道动作种类。
- 代价：所有 full-body Action leaf 都必须对称维护 ownership；Validator 要检查 OnEnter/OnExit 闭环。
- 不按 ActionId 在 Locomotion 中分支：那会把 Action Graph 的业务复制到移动图。

## Decision 8: Lifecycle reason 保持通用，业务原因由 source-exit 提供

正常恢复期被另一状态替换时，source leaf 统一提交一次 `Cancel(RecoveryCancel)`；parent LowerPriority、外部 correction、ForceStop 继续使用现有 `Interrupt`、`Abort` 或 teardown 语义。Runtime 不新增 `AttackToDodge`、`DodgeToAttack` 等 target-specific reason。

Agent v9 的 `ensure_action_exit_lifecycle` 必须以 `cancelConditionGroups` 保存 replacement 条件的析取范式：组内条件使用 AND，组间使用 OR。它不得把 request、window 与 admission 三个条件摊平成 OR，也不得仅凭 `StateTransition` 把自然完成误判为 Cancel。

流程：

```text
Transition edge committed
    -> source descendant stop
    -> Timeline gameplay sampling stops
    -> source OnExit submits one terminal lifecycle
    -> ownership released
    -> stop barrier completes
    -> target State activation
    -> target ActivateActionInstance creates new context
```

动画层只看到 source producer release 和 target producer selection，不解释 Cancel reason。

## Decision 9: Agent schema v9 是唯一正式迁移入口

Snapshot/Patch schema v9 增加：

- condition terms:
  - `action_window_active(windowType)`
  - `action_can_activate(actionProfileRef)`
- generic mutation operations:
  - `ensure_blackboard_declaration`
  - `delete_blackboard_declaration`
  - `ensure_timeline_tree_clip`
  - `delete_timeline_clip`
  - `ensure_tree_clip_blackboard_write`
  - `ensure_blackboard_write`
  - `delete_transition`
  - `ensure_gameplay_tag`
  - `set_action_profile_granted_tags`
  - `set_action_profile_cancel_query`

每个 operation 必须降低为 immutable typed command，并由同一 handler catalog 服务 dry-run/apply。`ensure_tree_clip_blackboard_write` 只负责 Timeline TreeClip projection，`ensure_blackboard_write` 负责状态 body 的普通 typed setter，二者不得互相代替。Snapshot 输出 local declaration、projection identity、TreeClip range、condition term、ActionProfile tag/query、edge priority，以及状态 body activation、lifecycle transition 和 setter 的 stable node identity；setter 还必须输出 typed value 与 OnEnter/OnExit phase。

迁移已有状态 body flow 时使用通用 `delete_flow_edge` 按 stable graph/edge identity 精确断开旧连接，再以 `link_flow` 连接原节点或前序 operation output。不得通过删除并重建 activation、Timeline 或 lifecycle 节点来规避 flow 重接。

`ensure_transition` 与 `ensure_condition_rule` 必须显式携带 stable edge identity，并按该 identity 更新或创建具体边。同一 source/target MAY存在语义不同的 Dodge、Move 与 natural-complete 边；Runtime 继续按 edge priority 与 flow order 选择，Agent 不得按端点合并或删除其它边。

已有 Root declaration 下沉到 inline Timeline owner 时使用 `move_blackboard_declaration` 在同一 authoring transaction 内迁移 stable identity 并更新 key/projection metadata。命令必须支持 source 上尚存在和 target 上已经迁移两种幂等状态；不得拆成会留下中间无效资产的两次 apply。

正式 MCP `export_snapshot` 必须输出 Full Snapshot，Snapshot 内的节点位置使用无 Unity 运行时属性的纯 `{x,y}` DTO。Compact Snapshot 只可用于编辑器内轻量浏览，不能作为 v9 Patch identity binding 或资产迁移输入。

`ensure_action_exit_lifecycle` 只拥有它生成的 `Action Exit` selector 子树。重复 apply 必须通过 Snapshot 绑定的 stable selector identity 精确替换该子树；首次迁移的旧 lifecycle 节点必须由显式 `delete_state_behavior_node` command 删除。宏不得按节点类型扫描并删除 state body 其它 OnEnter、OnExit 或未来 lifecycle slot 的提交节点。

旧 v8 reader/writer、Corin 精确状态名 evaluator、固定业务状态/DodgeRecoveryCancel macro 默认值全部删除。Macro 只可保留通用便利层，并要求调用方显式传入状态、窗口、profile 与 priority；普通 validator 不得检查 Corin 连段数量或业务名称。

MCP bridge 继续只有 `manage_btsmtl_agent_authoring`，只透传正式 authoring service，不形成另一套 mutation 语义。

## Decision 10: Semantic IR 与 Numeric Target 同步升级

新增两个 numeric-neutral operation：

- `ActionWindowActive`
- `CanActivateAction`

Frontend 为每个节点使用唯一 Emitter，IR 保存 typed WindowType/ActionProfile identity 和 source map。Float32/Fixed target 必须从同一 IR lowering，并调用各自窄状态 port 后面的共享 portable evaluator/query。operation set 从 `/4` 升级到 `/5`，旧 Program 明确 stale 并重新生成，不提供旧 reader。

## Migration Sequence

1. 完成 portable admission、ActionInstance Tag source 与 staged window query 合同，不改 Corin 资产。
2. 安装两个 authoring node、Emitter、IR operation、Float32/Fixed lowering 与 Editor catalog。
3. 升级 Agent v9 Snapshot/Patch/validator，并使 export→dry-run→apply 支持全部正式 mutation。
4. 导出 Corin v9 baseline，记录旧 declaration、TreeClip、edge、ActionProfile 与 owner identity。
5. 通过一个显式 v9 patch 下沉 declaration、替换窗口类型、条件项、边与 profile policy，并删除 legacy RushAttack state/route。
6. 删除旧 root declaration、旧条件、旧 transition、旧 ownership/recovery key 与隐式 cancel 依赖。
7. 再次导出并运行通用 validator，确认没有旧 key、旧 v8 或硬编码业务路径。
8. 重新生成唯一 Semantic IR、Float32/Fixed Program 和 Presentation Projection。
9. 更新 current project/spec/skill 文档，严格校验 change。

## Failure Boundaries

- Agent v9 无法安全保持 inline Timeline、TreeClip、declaration、edge 或 ActionProfile identity 时必须停止，不得直接编辑 YAML、克隆 Timeline 或创建一次性 migrator。
- 当前 projection candidate 在 Transition evaluation 前不可见时必须停止并调整正式 Program phase，不得读取上一帧 Bool 或新增 cache。
- Float32 与 Fixed 无法共享同一 admission/control 语义时必须停止并说明 target port 缺口，不得复制业务 evaluator。
- source stop barrier 不能保证 OnExit 先于 target activation 时必须停止并修正通用 StateMachine lifecycle，不得恢复 target activation 隐式 cancel。
- ActionProfile tags 无法按 ActionInstance source 写入并撤销唯一 Gameplay Effect Tag Container 时必须停止并修正 Tag state port，不得继续由 admission 保存私有 Tag 状态。
- Corin 缺失正式动画或 motion 资源不会阻止本 change 的逻辑迁移，但不得用 fallback clip；资源缺口应明确报告并保留未绑定错误。

## Resulting Chain

```text
Input request buffer
    + Timeline TreeClip local window projection
    + Target ActionProfile admission
    -> ConditionRuleGraph And/Or/Not
    -> StateMachine edge priority
    -> source stop / OnExit lifecycle
    -> target ActivateActionInstance
    -> Timeline gameplay outputs
    -> Motion / Gameplay / Presentation
```

处理前，Cancel key 同时承担时间与路由，激活还隐式修改 source。处理后，每一段数据只回答一个朴素问题，并沿唯一 Program/lifecycle 链提交。
