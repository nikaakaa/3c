## ADDED Requirements

### Requirement: 动画仲裁内部必须按状态所有权分层并保持单一权威

系统 MUST 保持 `CharacterAnimationLayerArbitrator` 为每层 `AnimationLayerPlan` 的唯一正式 commit 入口。Arbitrator MUST 私有拥有 contribution candidate allocation、ordered handoff ledger、causal graph construction 与 layer handoff resolution 的单一 concrete implementation；这些内部实现 MUST NOT作为独立播放权威被 Stage、Graph、LayerRuntime、Presenter 或 Preview 直接调用。

Candidate allocation、causal graph construction 与 handoff resolution MUST 是无跨帧持久状态的转换；ordered records、Ready/release retention 与 per-layer disposition MUST 只由 Arbitrator 私有拥有的 HandoffLedger 持久保存。系统 MUST NOT复制 Ledger、缓存第二份 Resolution，或从内部模块直接提交 LayerPlan。

#### Scenario: 单帧仲裁使用同一 Ledger Snapshot

- **WHEN** Arbitrator 收到一个包含多个 logic tick records 的表现批次
- **THEN** Ledger MUST 先完整 ingest 该批次
- **AND** Arbitrator MUST 为本帧捕获一份只读 Ledger Snapshot
- **AND** 所有正式 LayerId MUST 基于同一份 Ledger Snapshot 与 playback snapshot batch完成 resolution
- **AND** Arbitrator MUST 在全部 layer resolve 后一次提交全部 dispositions并执行 prune

#### Scenario: Candidate allocation 不解释 handoff

- **WHEN** Registry snapshot 包含多个 layer、priority、override 与 additive contributions
- **THEN** CandidateAllocator MUST 只生成完整 DesiredCandidates与非法 layer errors
- **AND** CandidateAllocator MUST NOT读取 Ledger、选择 Driver或生成 HandoffPlan

#### Scenario: 因果建图不决定 authority

- **WHEN** ordered records 包含连续链与互不连通组件
- **THEN** CausalGraphBuilder MUST 只依据 command order、activation owner 与正式 resolved owner构造有向组件和路径
- **AND** CausalGraphBuilder MUST NOT依据 contribution priority选择组件
- **AND** HandoffResolver MUST 使用正式 candidate 与 playback snapshot完成 authority 和 disposition

#### Scenario: 内部模块不能形成第二个入口

- **WHEN** Character runtime 或 Timeline Preview 需要动画仲裁
- **THEN** 调用方 MUST 只调用 CharacterAnimationLayerArbitrator 的正式 Build/Reset 边界
- **AND** 调用方 MUST NOT直接调用 Ledger、CausalGraphBuilder 或 HandoffResolver生成计划

#### Scenario: Arbitrator Reset

- **WHEN** Pipeline deactivate、dispose、Preview seek 或 target switch触发 Arbitrator Reset
- **THEN** Arbitrator MUST 清理其私有 Ledger、Ready/release facts与单帧中间集合
- **AND** 任意内部模块 MUST NOT在 Arbitrator 外保留跨会话状态

### Requirement: 动画播放执行必须区分多层协调与单层播放状态

`CharacterAnimationLayerRuntime` MUST 只协调正式 layer catalog、逐层 `AnimationLayerPlan`、playback snapshots、final outputs 与 frame diagnostics。每个 LayerId 的 FinalOutput、HeldOutput、DesiredCandidate、唯一 ActiveHandoff、blend elapsed 与 inertialization session MUST 由该层唯一 `AnimationLayerPlaybackState` 持有。Runtime 与 PlaybackState MUST NOT读取 lifecycle commands、HandoffLedger、causal graph 或重新执行 authority 仲裁。

#### Scenario: 每层应用一个计划

- **WHEN** Arbitrator 为本帧输出全部 LayerPlans
- **THEN** CharacterAnimationLayerRuntime MUST 为每个正式 LayerId 找到且只应用一个 plan
- **AND** 对应 AnimationLayerPlaybackState MUST 生成该层唯一 final output

#### Scenario: ActiveHandoff 重入

- **WHEN** 某层 PlaybackState 正在执行 handoff并收到 supersede HandoffPlan
- **THEN** 该 PlaybackState MUST 从当前 FinalOutput capture新 handoff
- **AND** 旧 ActiveHandoff MUST 在同一 PlaybackState 内退休
- **AND** 多层 Runtime MUST NOT建立跨 layer handoff stack

#### Scenario: 缺失正式计划

- **WHEN** 某个正式 LayerId 没有收到 committed LayerPlan
- **THEN** CharacterAnimationLayerRuntime MUST 生成明确 Invalid 结果并保持最后合法输出
- **AND** Runtime MUST NOT调用内部 Resolver、选择默认 clip或构造 fallback plan
