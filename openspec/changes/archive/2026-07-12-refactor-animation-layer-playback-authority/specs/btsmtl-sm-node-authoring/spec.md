## MODIFIED Requirements

### Requirement: Transition 动画混合元数据必须属于 Transition 边

`AnimationTransitionDefinition` MUST 内联保存于 StateMachine Transition edge，并显式配置 `AnimationHandoffRole`。Role=None 表示该逻辑 edge 不提供视觉交接策略，但其 runtime fact MUST 保留为有序因果 topology；Role=Driver 表示该 edge 可以为最终 HandoffPlan 提供 Immediate、ContributionCrossFade 或 Inertialization strategy、duration 与 curve。Definition MUST NOT保存 OwnerToOwner、LayerToOwner、OwnerToResolved 等视觉 endpoint mode。ConditionRuleGraph MUST 继续只表达 Bool 条件。

#### Scenario: 配置 None

- **WHEN** 作者选中 ActionOverride、inner Exit 或其它结构 edge
- **THEN** Inspector MUST 允许选择 None
- **AND** strategy、duration 与 curve MUST 不作为有效数据

#### Scenario: 配置 Driver

- **WHEN** 作者选中具有动画交接业务语义的 edge
- **THEN** Inspector MUST 允许选择 Driver 与显式 strategy
- **AND** Immediate duration MUST 为 0
- **AND** CrossFade/Inertialization duration MUST 大于 0

#### Scenario: 新建 Transition

- **WHEN** 作者创建非默认 Enter 的新 Transition
- **THEN** HandoffRole MUST 初始为 Unspecified
- **AND** Validator MUST 在显式配置前拒绝正式运行

#### Scenario: Condition Rule

- **WHEN** runtime 求值 ConditionRuleGraph
- **THEN** rule graph MUST 只决定 edge 是否通过
- **AND** rule graph MUST NOT决定 Role、strategy、causal disposition 或 layer endpoint

### Requirement: StateMachine runtime 必须发布切换混合事实且不双 tick 状态

StateMachine runtime 命中 edge 后 MUST 发布 source/target owner、resolved leaf owner、HandoffRole、strategy definition 与 cause，并让唯一 lifecycle command envelope 保存 tick、phase 与 sequence。它 MUST 在逻辑 barrier 内完成 source 退出和 target 激活，MUST NOT等待视觉 handoff，也 MUST NOT为 outgoing 继续 tick source body。StateMachine MUST 发布 None 与 Driver 两类有序事实，MUST NOT只把 Driver 发送给动画播放层。

#### Scenario: Driver edge

- **WHEN** active State 命中 Driver edge
- **THEN** runtime MUST 发布 ordered Driver fact
- **AND** source State MUST 完成逻辑退出
- **AND** target State MUST 正常激活

#### Scenario: None edge

- **WHEN** active State 命中 None edge
- **THEN** runtime MUST 发布 ordered None fact保留 source-to-target topology
- **AND** 它 MUST NOT创建独立视觉 session

#### Scenario: target 首次执行

- **WHEN** target State 首次获得正式 tick
- **THEN** runtime MUST 提交 AnimationOwnerReady
- **AND** ready MUST NOT以 target 是否产出 contribution 为条件

### Requirement: StateMachine 上层停止必须携带明确动画 release 语义

StateMachineNode external graceful stop definition MUST 显式配置 HandoffRole。需要提供交接策略时使用 Driver；只表达结构停止时使用 None。ForceStop、deactivate 与 dispose MUST 由 pipeline teardown 立即清理，不依赖隐藏 edge default。系统 MUST NOT根据 stop duration 或 replacement 类型推断 Role。

#### Scenario: Parent graceful replacement

- **WHEN** parent Tree graceful replacement 停止 StateMachineNode
- **THEN** stop context MUST 携带完整 external exit definition
- **AND** source State 逻辑 MUST 在 barrier 内关闭
- **AND** Arbitrator MUST 决定该 fact 最终是 Selected、Coalesced 或 Retired

#### Scenario: ForceStop

- **WHEN** StateMachineNode 收到 ForceStop/deactivate/dispose
- **THEN** pipeline MUST 确定性释放 owner membership 与 layer resources
- **AND** 系统 MUST NOT等待 blend duration

#### Scenario: 缺失 external definition

- **WHEN** graceful stop 需要正式 handoff 但 external definition 为 Unspecified
- **THEN** validator/runtime MUST 报告配置错误
- **AND** 系统 MUST NOT选择默认 CrossFade 或 Empty

## ADDED Requirements

### Requirement: 嵌套 StateMachine 动画事实必须按 Layer Output 收敛

父子 StateMachine MAY 在同一或连续 logic tick 发布多个 None/Driver facts。Pipeline MUST 保留它们的 command order，并由 Arbitrator 先按 activation owner归并连续因果链，再为同一 LayerId 提交唯一 LayerPlan。父子 Graph MUST 分别维护瞬时 execution path、state owner parent relation 与每个逻辑 owner 的最后 presentation leaf，MUST NOT创建或继承 animation transition domain。execution scope 重入 MUST NOT覆盖最后 presentation leaf；只有新 descendant activation 或正式 animation contribution producer 才能替换 leaf。

#### Scenario: Inner combo

- **WHEN** Attack1 -> Attack2 Driver 命中
- **THEN** source/target leaf MUST 解析为 Attack1/Attack2
- **AND** Arbitrator MUST 将该 fact纳入 Base causal component

#### Scenario: Inner Exit 与 Outer Exit

- **WHEN** Attack leaf -> inner Exit 为 None
- **AND** outer Attack -> None 为 Driver
- **THEN** inner None MUST 保留 topology但不提供 strategy
- **AND** outer Driver MUST 能通过最后 presentation leaf 连接当前 Base output

#### Scenario: Outer OnExit 重新进入 execution scope

- **WHEN** inner Attack2 已完成且 outer Attack 在 update、OnExit 或 force-stop 中重新 Push 自身 scope
- **THEN** execution path MUST 允许只包含 outer Attack
- **AND** outer Attack 的最后 presentation leaf MUST 继续为 Attack2
- **AND** scope 重入 MUST NOT把 leaf 回退为无动画的 outer Attack owner

#### Scenario: 连续 Locomotion activation

- **WHEN** RunLoop#4 -> RunEnd#5 -> MovingTurn#6 -> RunEnd#7 在表现 commit 前连续发生
- **THEN** 每条 fact MUST 保留各自 activation generation 与顺序
- **AND** Pipeline MUST 将连通 facts归并为一个 Base LayerPlan
- **AND** StateMachine MUST NOT为此减少或跳过合法逻辑 transition

#### Scenario: Parallel Locomotion 与 Action

- **WHEN** Locomotion 与 Action 发布互不连通的 transition components
- **THEN** Arbitrator MUST 按 layer 可见 authority 仲裁组件
- **AND** 相同最高 authority 的独立组件 MUST 报告冲突而不是按 StateMachine 顺序选择

## REMOVED Requirements

### Requirement: 嵌套 StateMachine 必须继承根动画 transition domain

**Reason**：animation domain 按逻辑 StateMachine 分组，但玩家实际看到的是统一 LayerId；它无法正确表示 Action 与 Locomotion 共同写入 Base。

**Migration**：删除 AnimationTransitionDomainId 和继承逻辑，保留 execution path、leaf owner 与完整有序 None/Driver facts，由 Arbitrator 生成每层唯一 LayerPlan。

#### Scenario: Nested transition

- **WHEN** inner 或 outer StateMachine 发布 handoff fact
- **THEN** fact MUST 不再携带 animation domain
- **AND** Pipeline MUST 使用 ordered causal component 与实际 layer output 处理
