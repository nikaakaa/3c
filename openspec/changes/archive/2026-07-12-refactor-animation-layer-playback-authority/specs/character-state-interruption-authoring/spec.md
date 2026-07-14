## MODIFIED Requirements

### Requirement: 状态抢占必须复用分层停止协议

状态抢占 MUST 继续复用 TreeNode stop、StateMachine transition、State OnExit 与 producer release。逻辑层 MUST 在 stop barrier 内关闭 source State、Action、Timeline 与 gameplay output；表现层 MUST 由 Arbitrator 将完整有序 None/Driver facts提交为 LayerPlan，再由 LayerRuntime 独立完成动画收尾。系统 MUST NOT让 source 逻辑为淡出继续 Running。

#### Scenario: RunEnd 被输入抢占

- **WHEN** RunEnd 命中更高优先级 Driver edge
- **THEN** StateMachine MUST 完成 source exit 与 target activation
- **AND** Arbitrator MUST 为实际 layer output变化生成 LayerPlan
- **AND** LayerRuntime MUST 在表现域执行该 plan

#### Scenario: Parent 抢占 StateMachineNode

- **WHEN** parent graceful replacement 停止 StateMachineNode
- **THEN** stop context MUST 携带明确 None/Driver definition
- **AND** source gameplay lifecycle MUST 在 barrier 内关闭
- **AND** layer handoff MAY 在逻辑 terminal 后继续

#### Scenario: ForceStop

- **WHEN** pipeline/host ForceStop、deactivate 或 dispose
- **THEN** Pipeline MUST 立即清理 owner membership、Arbitrator ledger 与 layer resources
- **AND** 它 MUST NOT等待 blend duration

### Requirement: 状态退出逻辑屏障与表现收尾必须分离

source State root、Action lifecycle、Timeline gameplay output 与 owner membership MUST 在逻辑 barrier 内关闭。Arbitrator MAY 继续持有已发布的 ordered transition records；LayerRuntime MAY 使用当前 FinalOutput 或最终 pose/velocity 完成已提交 plan，但两者 MUST NOT继续 tick source producer。Registry membership release MUST NOT等同于 causal record disposition 或 visual retirement。

#### Scenario: CrossFade 收尾

- **WHEN** source 已逻辑退出且 HandoffPlan 使用 ContributionCrossFade
- **THEN** LayerRuntime MAY 使用冻结 FinalOutput 淡出
- **AND** source MUST NOT再产生 gameplay facts

#### Scenario: Inertialization 收尾

- **WHEN** source 已逻辑退出且 HandoffPlan 使用 Inertialization
- **THEN** output job MAY 使用最终 pose/velocity 衰减
- **AND** source playback MUST 已停止

#### Scenario: 结构 target

- **WHEN** logical target Ready 但本身不产 animation contribution
- **THEN** Arbitrator MUST 使用完整批次后的 DesiredCandidate规划 incoming
- **AND** RequireOutput MUST 在 incoming 缺失时生成 Hold plan
- **AND** logical target MUST NOT自动等于 Empty

### Requirement: 动画 Transition 的完成不得反向阻塞 Tree terminal

Tree/StateMachine terminal MUST 只由逻辑停止协议决定，MUST NOT等待 layer handoff duration。Ordered record 的 Pending/Selected/Coalesced/Retired/Conflict MUST 由 Arbitrator 在表现 commit 推进；Capturing、Running、Completed 与 Superseded MUST 由 LayerRuntime 使用 presentation delta推进。teardown MUST 确定性清理两类生命周期。

#### Scenario: 长淡出与新 child

- **WHEN** source SMNode 已 terminal 但 layer handoff 仍 Running
- **THEN** parent Tree MUST 能推进 replacement child
- **AND** replacement contribution 与 transition facts MUST 能进入下一次 commit

#### Scenario: Host 销毁

- **WHEN** host 在 handoff Running 时 dispose
- **THEN** ledger、layer session、held output 与 native data MUST 释放

## ADDED Requirements

### Requirement: 并行与嵌套停止必须先区分连续链与独立竞争

父子或并行 StateMachine MAY 在同一或连续 logic tick 发布多个 intents。Pipeline MUST 先按精确 activation owner 与 command order 构造因果组件：同一连通路径上的多个 Driver MUST 归并为一个 HandoffPlan，互不连通组件才执行 authority 仲裁。结构 edge MUST 配置 None并保留为 topology fact。Pipeline MUST NOT通过 animation domain、全局 sequence 胜负或继续 source playback 解决冲突。

#### Scenario: Inner Exit 与 Outer Driver

- **WHEN** inner Attack leaf -> Exit 为 None
- **AND** outer Attack -> None 为 Driver
- **THEN** inner None MUST 作为连续 topology保留
- **AND** outer Driver MUST 为最终 Attack -> Locomotion HandoffPlan提供策略
- **AND** inner Exit MUST 不产生 Empty 中间帧

#### Scenario: 快速连续抢占

- **WHEN** RunLoop -> RunEnd -> MovingTurn -> RunEnd 在一个表现 commit 前连续成立
- **THEN** Pipeline MUST 将它们归并为一个因果组件
- **AND** 同一组件内更早 Driver MUST Coalesced
- **AND** LayerRuntime MUST 只执行一次最终交接

#### Scenario: Dodge 与 Locomotion 恢复

- **WHEN** Dodge -> None Driver 与 Locomotion 恢复 facts 同批到达
- **THEN** Arbitrator MUST 使用可见 Action authority生成一个 Dodge -> 最终 Locomotion plan
- **AND** 较低 authority underlay component MUST Retired
- **AND** Base MUST 不先变 Empty

#### Scenario: 真正的 Driver 冲突

- **WHEN** 同一 layer 存在多个互不连通且相同最高 authority 的 components
- **THEN** Arbitrator MUST 生成 Invalid plan并保持最后合法 output
- **AND** Pipeline MUST NOT按命令顺序选择

## REMOVED Requirements

### Requirement: 嵌套停止的动画表现必须收敛到单一 transition domain

**Reason**：根 animation domain 仍以逻辑 StateMachine 为边界，无法表达多个 StateMachine 写入同一 Base layer 的视觉事实。

**Migration**：删除 domain，以 ordered causal components、每层唯一 LayerPlan 与持久 playback Runtime 作为收敛合同。

#### Scenario: Inner 与 Outer 同 Tick

- **WHEN** inner/outer intents 同 Tick 到达
- **THEN** intents MUST 不携带 animation domain
- **AND** Pipeline MUST 按 owner topology、command order 与 layer authority 收敛
