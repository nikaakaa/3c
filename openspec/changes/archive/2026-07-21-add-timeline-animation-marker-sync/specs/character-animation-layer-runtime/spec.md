## ADDED Requirements

### Requirement: 同组 producer handoff 必须按 Marker Segment 映射

当同一Layer从outgoing Current切换到incoming target，且两者Projection binding均为MarkerGroup并拥有相同canonical SyncGroupId时，Animation Runtime MUST只从AnimationPlaybackLifecycle取得这两个真实playback，并按两侧SyncRole解析唯一leader与follower。默认由outgoing领导；incoming为AlwaysLeader或outgoing为AlwaysFollower时 MUST反向由incoming领导。Runtime MUST按leader effective time所在的有向MarkerId pair与segment fraction映射follower Timeline time，不得使用StateMachine上一状态、Graph edge、producer显示名、clip名称、Action名称、逻辑priority或Animancer weight推导方向。

#### Scenario: WalkLoop切换RunLoop

- **WHEN** Base层实际Current为WalkLoop且incoming target为同组RunLoop
- **THEN** Runtime MUST读取WalkLoop当前effective marker segment与fraction
- **AND** MUST在RunLoop的相同有向marker pair occurrence中计算target effective time

#### Scenario: source或target显式None

- **WHEN** source或target AnimationTrack的Projection binding为None
- **THEN** Runtime MUST明确记录NotApplicable
- **AND** target effective time MUST等于其raw visual time

#### Scenario: 同组数据损坏

- **WHEN** source与target声明同组但Projection缺少segment、duration或sampling state
- **THEN** Animation Runtime MUST进入明确Invalid并报告稳定diagnostic code
- **AND** MUST不静默退回normalized time、隐藏Idle或Animancer自动同步

#### Scenario: incoming finite producer要求领导

- **WHEN** outgoing Current为CanBeLeader且incoming target为AlwaysLeader
- **THEN** Runtime MUST建立`incoming -> outgoing` relation
- **AND** incoming MUST继续使用自己的raw表现节奏

#### Scenario: handoff角色冲突

- **WHEN** 两侧同时为AlwaysLeader或同时为AlwaysFollower
- **THEN** Runtime MUST以typed invalid reason失败
- **AND** 不得静默选择outgoing或incoming

### Requirement: Marker Sync 必须在共同可见期间持续求值

Marker Sync MUST在source与target共同可见的每个PresentationFrame重新使用source effective marker segment求target effective time，不得仅在target首样本时保存固定phase offset。target MUST以mapped time重新采样整个producer，包括所有AnimationClip membership、ClipIn、ease和内部weight。Gameplay提交的raw Timeline sample、cycle和logic completion MUST保持不变。

#### Scenario: 不同时长循环动画fade

- **WHEN** 1.0秒WalkLoop与0.6秒RunLoop在Animancer fade期间共同可见
- **THEN** RunLoop MUST在每个PresentationFrame持续对齐WalkLoop marker fraction
- **AND** MUST不因两个producer各自速度不同而在fade后半段重新漂移

#### Scenario: 多clip target producer

- **WHEN** target AnimationTrack在mapped time采样到两个重叠AnimationClip
- **THEN** 两个clip的membership、time与内部weight MUST由同一个effective Timeline time重新采样
- **AND** Marker Sync MUST不选择某一个clip作为第二phase authority

#### Scenario: Gameplay状态立即切换

- **WHEN** Program在logic tick将状态从Walk切换到Run
- **THEN** Gameplay state、Motion与World request MUST在该tick按原规则推进
- **AND** Presentation MUST不等待marker边界后才提交状态切换

### Requirement: Finite 与 Cyclic producer 必须使用明确拓扑映射

Runtime MUST支持`Cyclic -> Cyclic`、`Cyclic -> Finite`、`Finite -> Cyclic`和`Finite -> Finite`同组映射。Cyclic source/target MAY按duration回绕并保持展开cycle；Finite source/target MUST不回绕，target occurrence MUST单调前进。target首次存在多个相同有向pair occurrence时，Runtime MUST按与raw target time最小距离选择，并以frame和MarkerAuthoringId稳定破同；relation存活期间 MUST保持该occurrence连续性。

#### Scenario: RunLoop进入RunEnd

- **WHEN** Cyclic RunLoop切换到同组Finite RunEnd
- **THEN** Runtime MUST选择RunEnd中与当前raw time最近的兼容marker pair occurrence
- **AND** 后续共同可见帧 MUST沿RunEnd有限序列向前推进

#### Scenario: Finite source返回循环移动

- **WHEN** 同组Finite Turn或End producer切换到Cyclic locomotion producer
- **THEN** Runtime MUST从Finite source当前非回绕segment映射target最近展开cycle
- **AND** target成为独立Current后 MUST继续该展开cycle而不跳回cycle 0

#### Scenario: Finite覆盖耗尽

- **WHEN** relation要求Finite target前进到其marker coverage之外
- **THEN** Runtime MUST报告FiniteCoverageExceeded
- **AND** MUST不回绕Finite producer或静默解除同步

### Requirement: Sync relation 必须服从播放生命周期并连续脱离

Sync relation MUST以完整AnimationPlaybackId为key，并且只依赖AnimationPlaybackLifecycle的Current、PendingFirstSample、Outgoing与Retired事实。快速连续切换 MUST按实际relation依赖形成无环effective-time图并拓扑求值，不得假设leader generation一定早于follower。source正式Retired时，Runtime MUST以target最后effective time和raw time建立continuation anchor，删除relation，并让target按后续raw delta连续推进。Reset、target Retired和Dispose MUST清除对应relation与anchor。

#### Scenario: 连续A到B到C

- **WHEN** B仍跟随Outgoing A时Current B又切换到C
- **THEN** 当帧求值顺序 MUST为`A effective -> B effective -> C effective`
- **AND** C MUST读取B的effective time而不是B未经映射的raw time

#### Scenario: source淡出完成

- **WHEN** Animancer与lifecycle将source正式标记Retired
- **THEN** target MUST从最后mapped effective time建立continuation anchor
- **AND** 下一帧 MUST按target raw delta连续推进而不跳回原始Timeline time

#### Scenario: relation拓扑非法

- **WHEN** Runtime检测到relation环、同一target拥有两个source或跨Layer依赖
- **THEN** 对应Layer MUST进入明确Invalid
- **AND** MUST不依赖集合遍历顺序选择任意relation

### Requirement: Animancer 必须继续独占 Fade 与最终 Pose

MarkerSyncRuntime MUST只提供producer effective sample time。Animancer MUST继续独占state/mixer、TransitionLibrary、FadeMode、duration modifier、easing、layer weight、outgoing retirement与最终pose；AnimancerPlaybackAdapter MUST继续对Timeline控制的child使用`DontSynchronize`。项目 MUST不新增自定义crossfade weight、Animancer automatic synchronization或第二动画时钟。

#### Scenario: 同步target开始播放

- **WHEN** matched target首份合法sample进入lifecycle
- **THEN** Adapter MUST通过正式Animancer transition播放target
- **AND** MarkerSyncRuntime MUST不写入fade progress或state weight

#### Scenario: source retirement由Animancer确认

- **WHEN** source逻辑ownership已释放但Animancer仍在淡出
- **THEN** source MUST继续通过PresentationRetention提供animation-only sample与effective time
- **AND** relation MUST只在正式Retired后脱离
