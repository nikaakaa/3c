## 1. 基线与依赖收口

- [x] 1.1 读取并记录 `refactor-corin-action-combo-authoring` 的最终 Corin state、Timeline、window、ActionProfile 与 ownership 资产身份
- [x] 1.2 读取并记录 `refactor-gameplay-runtime-and-tooling-modules` 的 portable runtime、Float32/Fixed target port 与程序集边界
- [x] 1.3 盘点所有 `AttackNCancel`、`AttackNMoveCancel`、`DodgeRecoveryCancel`、`CanDodgeMoveCancel`、`IsDodging` 与 `ResumeLocomotionThroughRunEnd` 声明和引用
- [x] 1.4 盘点 `ActivateActionInstance` 在 Float32/Fixed 中的准入判断、隐式 source cancel 与拒绝原因
- [x] 1.5 盘点 Blackboard projection candidate 从 TreeClip write 到 EndFrame fact flush 的正式阶段顺序
- [x] 1.6 盘点 Agent v8 Snapshot/Patch、condition term、typed command、handler、validator、macro、evaluator 与 MCP bridge 入口
- [x] 1.7 固化 Corin 现有 TreeClip、declaration、edge、ActionProfile 与 generated artifact identity 清单
- [x] 1.8 确认本 change 不依赖 network model、animation arbitration、motion solver 或 input buffer 修改

## 2. Portable Action admission 合同

- [x] 2.1 定义 numeric-neutral Action admission request、decision 与稳定 reject reason
- [x] 2.2 定义只暴露 catalog/profile、统一 Gameplay Effect state 与 active ActionInstance 的窄读取端口
- [x] 2.3 将目标 ActionProfile block 条件迁入唯一 portable admission evaluator
- [x] 2.4 将 active source granted tags 与 target cancel query 匹配迁入唯一 portable admission evaluator
- [x] 2.5 将现有资源或状态准入条件迁入同一 evaluator，不复制 Gameplay Effect 状态
- [x] 2.6 为无 active source 的普通激活定义明确准入语义
- [x] 2.7 为 active source 尚未关闭的 activation commit 定义 `SourceActionStillActive` 拒绝
- [x] 2.8 让 Float32 Action runtime 通过窄端口调用 portable evaluator
- [x] 2.9 让 Fixed Action runtime 通过窄端口调用同一 portable evaluator
- [x] 2.10 删除 Float32/Fixed 中重复的 admission 分支与字符串拒绝规则
- [x] 2.11 删除 `ActivateActionInstance` 内部隐式 source Cancel
- [x] 2.12 保持 target ActionInstance 创建、source input identity 与 Action Context 输出合同不变
- [x] 2.13 为唯一 Gameplay Effect Tag Container 增加按 ActionInstance source 授予与撤销 profile tags 的窄端口
- [x] 2.14 让 Float32 Action activation 成功后以 `action:<ActionInstanceId>` source 写入 ActionProfile tags
- [x] 2.15 让 Fixed Action activation 成功后以相同 source 语义写入 ActionProfile tags
- [x] 2.16 让 Complete、Cancel、Interrupt、Abort 与 teardown 精确撤销对应 ActionInstance tag source
- [x] 2.17 删除 Action admission 对 active Action tags 的私有 owned-tag 合并，target block query、BTSMTL Tag query 与 Gameplay Effect requirement 读取唯一 Container

## 3. 当前帧 ActionWindow 纯查询

- [x] 3.1 定义 numeric-neutral staged ActionWindow query port
- [x] 3.2 让查询按 actor、current active ActionInstanceId、current logic tick 与 WindowType 精确匹配
- [x] 3.3 明确 WindowId、Digest 与 projection policy 只保留 provenance，不参与 WindowType Bool 聚合
- [x] 3.4 让 Float32 Blackboard runtime 暴露现有 projection candidate 的只读窄视图
- [x] 3.5 让 Fixed Blackboard runtime 暴露同一语义的只读窄视图
- [x] 3.6 保证查询发生后仍由原 EndFrame 路径生成唯一 ActionWindowFact
- [x] 3.7 保证查询不消费 request、不写 Blackboard、不延长 candidate 生命周期
- [x] 3.8 对 phase 顺序不允许同帧读取的 authoring 产生明确 compiler error
- [x] 3.9 删除任何为条件查询新增 active-window registry、cache 或历史副本的实现

## 4. BTSMTL 通用纯条件节点

- [x] 4.1 新增 `ActionWindowActiveInfoNode` authoring 类型与 WindowType 配置
- [x] 4.2 为 `ActionWindowActiveInfoNode` 接入 ConditionRuleGraph 可创建白名单
- [x] 4.3 为 `ActionWindowActiveInfoNode` 接入节点 Inspector、显示名与数据目录
- [x] 4.4 新增 `CanActivateActionInfoNode` authoring 类型与 ActionProfile 引用配置
- [x] 4.5 为 `CanActivateActionInfoNode` 接入 ConditionRuleGraph 可创建白名单
- [x] 4.6 为 `CanActivateActionInfoNode` 接入节点 Inspector、显示名与角色 authoring context 资产选择
- [x] 4.7 保证两个节点都只能输出纯 Bool 且不能进入执行流端口
- [x] 4.8 保证两个节点可与现有 Equal/And/Or/Not 和 input request query 组合
- [x] 4.9 删除任何 Attack、Dodge 或 target-specific cancel 条件节点
- [x] 4.10 更新 Graph Data Catalog 显示 owner-local ActionWindow projection、provenance 与 WindowType query 定位
- [x] 4.11 保留 Projection=None 普通局部变量的编辑与 ValueNode 读取，并与 typed ActionWindow query 明确区分
- [x] 4.12 让 State transition ConditionRuleGraph 的 topology projection 包含普通祖先图与 source StateNode 的直接 body graph
- [x] 4.13 拒绝 ConditionRuleGraph 读取 target State body、兄弟 State body 或任意后代 leaf 的局部 declaration
- [x] 4.14 让 Semantic IR compiler、Agent Snapshot、Inspector 与 Validator 复用同一 source-body lexical scope 规则
- [x] 4.15 在 source-local TreeClip 写入晚于 condition 查询或 owner scope 不可见时输出精确编译错误，不读取上一帧值

## 5. Semantic IR operation set `/5`

- [x] 5.1 为 `ActionWindowActive` 分配稳定 numeric-neutral operation code
- [x] 5.2 为 `CanActivateAction` 分配稳定 numeric-neutral operation code
- [x] 5.3 为两个节点登记唯一 Authoring Discovery 与 Semantic Emitter
- [x] 5.4 在 IR payload 中保存 typed WindowType 与稳定 ActionProfile identity
- [x] 5.5 为两个 operation 生成精确 source map 与 capability manifest
- [x] 5.6 在 Float32 target lowering 中登记两个 operation
- [x] 5.7 在 Fixed target lowering 中登记两个 operation
- [x] 5.8 在 Float32 operation machine 中调用 staged window port 与 portable admission evaluator
- [x] 5.9 在 Fixed operation machine 中调用同一语义端口与 evaluator
- [x] 5.10 将 OperationSetVersion 从 `/4` 升级为 `/5`
- [x] 5.11 删除 `/4` Program reader、旧 operation fallback 与兼容分派
- [x] 5.12 更新 Program codec、canonical hash、source map reader 与 diagnostics label
- [x] 5.13 更新普通 DotNet artifact reader 对 `/5` header 与 operation payload 的读取

## 6. Source lifecycle 与 replacement 顺序

- [x] 6.1 核对 State transition replacement 的 source stop barrier 必须先于 target activation
- [x] 6.2 让攻击恢复期 replacement 在 leaf OnExit 提交唯一 `Cancel(RecoveryCancel)`
- [x] 6.3 让闪避恢复期 replacement 在 leaf OnExit 提交唯一 `Cancel(RecoveryCancel)`
- [x] 6.4 保持自然 Timeline terminal 只提交一次 Complete
- [x] 6.5 保持 parent LowerPriority、external correction 与 ForceStop 使用现有 Interrupt/Abort/teardown 语义
- [x] 6.6 保证外层 Action State 与内层 leaf 不重复提交 terminal lifecycle
- [x] 6.7 保证 target activation 在 source ActionContext 关闭后创建新的 ActionInstance
- [x] 6.8 为 source 未关闭、重复 terminal 与 target admission 漂移输出精确 runtime diagnostics
- [x] 6.9 删除 target-specific lifecycle reason 与 activation 隐式 cancel 兼容路径

## 7. Agent Snapshot/Patch schema v9

- [x] 7.1 将 Agent Snapshot schema 常量升级到 v9
- [x] 7.2 将 Agent Patch schema 常量升级到 v9
- [x] 7.3 删除 v8 Snapshot writer、Patch reader 与兼容分支
- [x] 7.4 为 Snapshot 输出 `action_window_active` condition term
- [x] 7.5 为 Snapshot 输出 `action_can_activate` condition term 与 ActionProfile identity
- [x] 7.6 为 Snapshot 输出 local Blackboard declaration、projection metadata 与 owner identity
- [x] 7.7 为 Snapshot 输出 Timeline TreeClip range、write target 与 stable identity
- [x] 7.8 为 Snapshot 输出 State transition priority 与 condition tree
- [x] 7.9 为 Snapshot 输出 ActionProfile granted tags、cancel query 与 block query
- [x] 7.10 为 Patch DTO 增加两个 typed condition term
- [x] 7.11 增加 `ensure_blackboard_declaration` typed command、lowerer 与 handler
- [x] 7.12 增加 `delete_blackboard_declaration` typed command、lowerer 与 handler
- [x] 7.12a 增加 `move_blackboard_declaration` typed command、lowerer 与 handler，支持同事务保留 identity 的 owner 迁移
- [x] 7.13 增加 `ensure_timeline_tree_clip` typed command、lowerer 与 handler
- [x] 7.14 增加 `delete_timeline_clip` typed command、lowerer 与 handler
- [x] 7.15 增加 `ensure_tree_clip_blackboard_write` typed command、lowerer 与 handler
- [x] 7.16 增加 `delete_transition` typed command、lowerer 与 handler
- [x] 7.16a 增加 `delete_flow_edge` typed command、lowerer 与 handler，支持保留节点 identity 的状态 body flow 重接
- [x] 7.16b 增加 `delete_state` typed command、lowerer 与 handler，按 stable State identity 删除 legacy state 及其正式 transition/inline body owner
- [x] 7.17 增加 `ensure_gameplay_tag` typed command、lowerer 与 handler
- [x] 7.18 增加 `set_action_profile_granted_tags` typed command、lowerer 与 handler
- [x] 7.19 增加 `set_action_profile_cancel_query` typed command、lowerer 与 handler
- [x] 7.20 让所有新 command 使用 stable identity 或前序 output symbol 定位 owner
- [x] 7.20a 让 transition command 以显式 stable edge identity 区分同端点的不同业务边，删除按端点合并行为
- [x] 7.21 让 dry-run/apply 消费同一 immutable typed command plan
- [x] 7.22 让 authoring transaction owner collector 覆盖 Graph、Timeline、Blackboard、ActionProfile 与 tag catalog
- [x] 7.23 让 rollback/dirty/save 继续只属于唯一 Agent authoring service
- [x] 7.24 更新 Node Emitter registry 对两个通用条件节点的白名单与参数校验
- [x] 7.25 更新 Validator 对 WindowType、ActionProfile、owner、phase 与 condition purity 的通用校验
- [x] 7.26 删除 Validator 对 Corin 名称、连段数量、cancel key 与精确 transition 集合的硬编码
- [x] 7.27 删除 Macro 中业务状态名、DodgeRecoveryCancel 与 Corin priority 固定默认值
- [x] 7.28 保留 Macro 时要求状态、窗口、profile、edge priority 全部显式输入
- [x] 7.29 保持 MCP bridge 只透传 `manage_btsmtl_agent_authoring` 正式服务
- [x] 7.30 更新 `btsmtl-agent-authoring` skill 的 v9 export、dry-run、apply、validate 工作流

## 8. Corin Timeline Window 迁移

- [x] 8.1 通过 Agent v9 导出 Corin migration baseline snapshot
- [x] 8.2 为 Gameplay tag catalog 正式增加 `Attack` tag
- [x] 8.3 为 Attack ActionProfile 配置 granted `Attack` tag
- [x] 8.4 为 Dodge ActionProfile 保持 granted `Dodge` tag
- [x] 8.5 为 Attack target 配置可匹配 active `Attack` 或 `Dodge` 的 cancel query
- [x] 8.6 为 Dodge target 配置可匹配 active `Attack` 或 `Dodge` 的 cancel query
- [x] 8.7 为 Attack1 inline Timeline 创建 local `ComboAccept` declaration 与 TreeClip write
- [x] 8.8 为 Attack2 inline Timeline 创建 local `ComboAccept` declaration 与 TreeClip write
- [x] 8.9 为 Attack3 inline Timeline 创建 local `ComboAccept` declaration 与 TreeClip write
- [x] 8.10 为 Attack4 inline Timeline 创建 local `ComboAccept` declaration 与 TreeClip write
- [x] 8.11 删除 Attack5 的旧循环 ComboAccept TreeClip、declaration、reader 与 transition 引用
- [x] 8.12 使用 `delete_state` 删除 legacy RushAttack state、inline body、Timeline 与关联 transition
- [x] 8.13 为 Attack1..5 创建从 End clip 起始到自然结束的 local `RecoveryEarly` TreeClip
- [x] 8.14 为 Attack1..5 迁移现有 MoveCancel range 为 local `RecoveryLate` TreeClip
- [x] 8.15 为 DodgeBack 迁移现有恢复范围为 local `RecoveryOpen` TreeClip
- [x] 8.16 为 DodgeForward 迁移现有恢复范围为 local `RecoveryOpen` TreeClip
- [x] 8.17 保留所有迁移窗口的稳定 WindowId、Digest、ActionWindow projection 与 owner identity
- [x] 8.18 删除 RootTree 上旧 `AttackNCancel` declarations
- [x] 8.19 删除 RootTree 上旧 `AttackNMoveCancel` declarations
- [x] 8.20 删除 RootTree 上旧 RushAttack Cancel/MoveCancel declarations，并确认删除 state 后没有不可达 inline owner
- [x] 8.21 删除旧 `DodgeRecoveryCancel` 与 `CanDodgeMoveCancel` declarations
- [x] 8.22 删除全部旧 key reader 与旧 TreeClip write，不保留别名

## 9. Corin Action StateMachine 路由

- [x] 9.1 将 Attack1→Attack2 条件改为 Attack request AND `ComboAccept` AND target admission
- [x] 9.2 将 Attack2→Attack3 条件改为 Attack request AND `ComboAccept` AND target admission
- [x] 9.3 将 Attack3→Attack4 条件改为 Attack request AND `ComboAccept` AND target admission
- [x] 9.4 将 Attack4→Attack5 条件改为 Attack request AND `ComboAccept` AND target admission
- [x] 9.5 删除 Attack5→Attack1 循环边，保证 Attack5 只允许 Dodge、Move 或 natural complete
- [x] 9.6 删除全部 RushAttack incoming/outgoing transition 与 condition rule
- [x] 9.7 为每个 Attack leaf 增加 Dodge request AND `RecoveryEarly` AND Dodge admission 的内层 Exit edge
- [x] 9.8 为每个 Attack leaf 增加 Move input AND `RecoveryLate` 的内层 Exit edge
- [x] 9.9 将 Attack edge priority 固定为 Dodge 高于 Combo、Combo 高于 Move、Move 高于 natural complete
- [x] 9.10 为 DodgeBack 恢复期增加 Attack request AND `RecoveryOpen` AND Attack admission 的内层 Exit edge
- [x] 9.11 为 DodgeForward 恢复期增加 Attack request AND `RecoveryOpen` AND Attack admission 的内层 Exit edge
- [x] 9.12 为 DodgeBack/DodgeForward 恢复期增加 Dodge request AND Dodge admission 的方向重入边
- [x] 9.13 为 DodgeBack/DodgeForward 恢复期增加 Move input AND `RecoveryOpen` 的内层 Exit edge
- [x] 9.14 将 Dodge edge priority 固定为 Attack 高于 Dodge、Dodge 高于 Move、Move 高于 natural complete
- [x] 9.15 保证所有 condition query 不消费 request
- [x] 9.16 保证只有 target ActivateActionInstance 接受并消费对应 request
- [x] 9.17 保证 Attack 无输入自然完成后退出 Action 并进入 Idle
- [x] 9.18 保证 Dodge 无输入自然完成后退出 Action 并进入 Idle
- [x] 9.19 删除普通 Action 完成后强制经过 RunEnd 的 transition
- [x] 9.20 删除条件和业务语义完全重复的旧边与旧优先级配置，保留同端点但业务条件不同的正式 priority 边
- [x] 9.21 将外层 Attack→Dodge 配置为 `state_root_completed` AND Dodge request AND Dodge admission，不读取任意 Attack leaf window
- [x] 9.22 将外层 Attack→None Move 路由配置为 `state_root_completed` AND Move input，并保留独立 natural-complete `state_root_completed` 边
- [x] 9.23 将外层 Dodge→Attack 配置为 `state_root_completed` AND Attack request AND Attack admission，不读取任意 Dodge leaf window
- [x] 9.24 将外层 Dodge→None Move 路由配置为 `state_root_completed` AND Move input，并保留独立 natural-complete `state_root_completed` 边
- [x] 9.25 保证内层 leaf edge 只决定离开时机，外层 category edge 只决定完成后的目标，request 只由最终 target activation 消费

## 10. Locomotion ownership 收口

- [x] 10.1 创建唯一 pipeline Blackboard `HasActionLocomotionOwnership` declaration
- [x] 10.2 让所有 Corin full-body Action 在成功 activation 后设置 ownership true
- [x] 10.3 让所有 Corin full-body Action source exit 对称设置 ownership false
- [x] 10.4 让普通 locomotion state 高优先级读取 ownership 进入 ActionOverride
- [x] 10.5 让 ActionOverride 在 ownership false 且有 Move 时直接进入 RunLoop
- [x] 10.6 让 ActionOverride 在 ownership false 且无 Move 时直接进入 Idle
- [x] 10.7 删除 `IsDodging` declaration、write、reader 与 diagnostics label
- [x] 10.8 删除 `ResumeLocomotionThroughRunEnd` declaration、write、reader 与 edge
- [x] 10.9 保证 ActionOverride 不引用 ActionProfile、Timeline、request、motion 或 animation
- [x] 10.10 更新通用 Validator 检查 full-body Action ownership 的 OnEnter/OnExit 对称性

## 11. Generated artifacts 与诊断

- [x] 11.1 使用正式 compiler 重新生成 Corin Gameplay Semantic IR artifact
- [x] 11.2 使用 `/5` IR 重新生成 Corin Float32 Program
- [x] 11.3 使用 `/5` IR 重新生成 Corin Fixed Program
- [x] 11.4 重新生成 Corin Presentation Projection
- [x] 11.5 更新 Definition 对唯一 generated artifacts 的正式引用
- [x] 11.6 用唯一 `/5` payload 替换 Corin generated artifact，并删除 stale `/4` cache identity
- [x] 11.7 在 Runtime Debug 中展示 request、WindowType、admission decision、selected edge 与 lifecycle result
- [x] 11.8 在编译诊断中展示 declaration owner、phase、WindowId 与 ActionProfile source identity
- [x] 11.9 通过 Agent v9 重新导出 Corin snapshot 并确认没有旧 key、旧 schema 或旧 transition
- [x] 11.10 运行 Agent 通用 validate 并修复全部 Graph/Timeline/ActionProfile 语义错误

## 12. 清理、文档与静态校验

- [x] 12.1 删除旧 Agent schema v8 DTO、reader、writer、condition term 与文档
- [x] 12.2 删除旧 implicit activation cancel helper、重复 Float32/Fixed admission helper 与废弃 diagnostics
- [x] 12.3 删除旧 Cancel/MoveCancel root Blackboard data、旧 exact Corin evaluator 与固定业务 macro defaults
- [x] 12.4 使用 `rg` 与 Agent Snapshot 确认不存在以 `Attack1Cancel`..`Attack5Cancel`、`Attack1MoveCancel`..`Attack5MoveCancel`、`DodgeRecoveryCancel`、`CanDodgeMoveCancel`、`IsDodging`、`ResumeLocomotionThroughRunEnd` 命名的 Blackboard key、reader、condition 或 route，并确认不存在 legacy RushAttack state/route；迁移窗口允许按 8.17 保留相同文本的稳定 WindowId provenance
- [x] 12.5 使用 `rg` 确认不存在 Agent schema v8 reader/writer 与 OperationSet `/4` runtime reader
- [x] 12.6 使用 `rg` 确认不存在 ActionWindow registry/cache、Attack/Dodge 专用 cancel node 或 activation cancel fallback
- [x] 12.6a 使用 `rg` 确认 ActionProfile granted tags 只以 Gameplay Effect Tag Container 的 ActionInstance source 形成角色运行时状态，不存在 Action runtime 私有持久 Tag 集合
- [x] 12.7 更新 `openspec/project.md` 的 Action、Timeline、ownership、Agent schema 与 operation-set 架构真相
- [x] 12.8 核对全部 spec delta 已完整替换 current specs 中冲突的旧窗口、ownership、Agent schema 与恢复口径
- [x] 12.9 更新 Agent authoring 与 Corin authoring 文档中的输入、处理、输出和 owner 边界
- [x] 12.10 编译相关普通 DotNet reader 工程，命令携带 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`
- [x] 12.11 编译 `ThirdPersonSimulation.Core`、Float32、Fixed 与相关 Unity C# 项目，命令携带 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`
- [x] 12.12 编译 Agent Editor 与 Assembly-CSharp-Editor 项目，命令携带 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`
- [x] 12.13 每次编译后立即执行 `dotnet build-server shutdown`
- [x] 12.14 运行 `openspec validate refactor-action-transition-eligibility-authoring --strict --no-interactive`
- [x] 12.15 核对全部任务真实完成后再统一标记 `[x]`
- [x] 12.16 让 Agent authoring service 在所有 action 前统一重绑只读 Graph 引用，保证冷域重载后的首次 export、dry-run、apply 与 validate 不依赖图窗口或 `CheckInit` 调用顺序
