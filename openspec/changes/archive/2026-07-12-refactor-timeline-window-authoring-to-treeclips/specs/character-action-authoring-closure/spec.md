## REMOVED Requirements

### Requirement: Timeline window Inspector 必须只编辑窗口输出

Timeline window Inspector MUST 只编辑 WindowType、WindowId、时间范围和窗口业务参数。Timeline window MUST NOT 保存完整 authority、history、replication、correction 或 cue playback 策略。

#### Scenario: 编辑 HitWindow

- **WHEN** 作者选中 HitWindow clip
- **THEN** Inspector MUST 允许设置 `WindowType = Hit` 和稳定 `WindowId`
- **AND** 是否进入 hit/result history、是否 server authoritative、是否 digest only MUST 从 ActionProfile 解析

#### Scenario: 没有静态 ActionProfile

- **WHEN** Timeline asset 被多个 action 复用
- **THEN** window clip MUST 保持只描述窗口输出
- **AND** 策略预览 MAY 依赖 editor 当前选择的 preview profile 或 runtime debug context，但不得写入 clip 作为正式配置

## ADDED Requirements

### Requirement: TreeClip 与 Scope Variable 必须是 Timeline Window 唯一作者入口

作者 MUST 使用 Decision TreeClip 表达 Timeline Window 时间范围，并使用 Bool Frame/Frame Pipeline Blackboard declaration 表达当前 Tick active 状态。需要 ActionWindowSample 的 declaration MUST 通过显式 ActionWindow projection 保存 WindowType、WindowId 和 Digest；完整网络 policy MUST 继续来自 ActionProfile。系统 MUST NOT 提供 ActionWindowTrack、ActionWindowClip、专用 Window reader 或直接 SubmitActionWindowSample 作者节点。

#### Scenario: 编辑 HitWindow

- **WHEN** 作者配置 `Attack1Hit`
- **THEN** 作者 MUST 在攻击 Timeline 中创建对应 Decision TreeClip
- **AND** inline Tree MUST 写入 `Attack1Hit=true`
- **AND** declaration MUST 显式配置 ActionWindow projection

#### Scenario: 编辑本地恢复门

- **WHEN** 作者配置 `CanDodgeMoveCancel`
- **THEN** 作者 MUST 使用 Decision TreeClip 写入 Bool Frame variable
- **AND** declaration MUST 保持 Projection=None

#### Scenario: Timeline asset 被多个动作复用

- **WHEN** 同一个带 projected variable 的 Timeline 被不同 Action Context 播放
- **THEN** Window fact MUST 使用每次 playback 的显式 Action Context
- **AND** TreeClip、declaration 或 Timeline asset MUST NOT 静态保存 ActionInstanceId

## MODIFIED Requirements

### Requirement: UI 闭环必须支持 Timeline 和非 Timeline 动作

作者 MUST 能使用同一套 ActionProfile、Graph request submit UI、scope variable fact projection 和 Runtime Debug 配置 Timeline 动作与非 Timeline 动作。Timeline 时间窗口 MUST 使用 Decision TreeClip 写 scope variable；非 Timeline 持续窗口 MUST 使用具有显式 Action Context provenance 的 scope variable 写入。系统 MUST NOT 要求非 Timeline 动作创建虚假 Timeline，也 MUST NOT 保留 SubmitActionWindowSampleNode 作为第二输出路径。

#### Scenario: Timeline 攻击

- **WHEN** 作者配置轻攻击
- **THEN** Graph request submit UI MUST 提交 `Attack.Light.01`
- **AND** Hit 和 Cancel 时间范围 MUST 由 Decision TreeClip 配置
- **AND** 对应 scope variable MUST 通过 projection 生成 Window facts

#### Scenario: 非 Timeline 格挡

- **WHEN** 作者配置持续格挡
- **THEN** Graph MUST 能在持有显式 Action Context 时写入 Guard window scope variable
- **AND** 相同 projection stage MUST 生成 Guard ActionWindowSample

### Requirement: 作者 UI 必须能从 ActionProfile 追到输出预览

系统 MUST 让作者从 Graph action request、TreeClip scope output、非 Timeline projected variable 和 Runtime Debug 追溯到同一个 ActionProfile 的策略预览。Graph Data Catalog 或 TreeClip Inspector MAY 显示只读 preview profile 或 runtime context，但 declaration projection MUST NOT 保存完整网络策略。

#### Scenario: 从 Graph request 查看策略

- **WHEN** 作者选中提交 `Guard.ParryCounter` 的 Graph 节点
- **THEN** UI MUST 显示它引用的 ActionProfile
- **AND** MAY 提供跳转或只读预览，展示该 profile 的 window、motion、cue 和 result 策略

#### Scenario: 从 projected variable 查看策略

- **WHEN** 作者查看 `Attack1Hit` declaration 的 ActionWindow projection
- **THEN** UI MUST 显示 WindowType、WindowId、Digest 和可解析的 preview ActionProfile policy
- **AND** declaration MUST NOT复制 authority、history、replication 或 correction policy

### Requirement: Timeline 攻击闭环不得依赖 RootTree 平铺测试输出

作者配置 Timeline 攻击时，攻击时间事实 MUST 由 Timeline 内的 Decision TreeClip 写入 scope variable；Cue 仍由其正式 Timeline/Graph 输出模型表达。RootTree 主流程 MUST NOT 平铺 `SubmitActionWindowSample`、`SubmitActionCueEvent` 或测试 GameplayResult 节点补充动作 body 事实，系统也 MUST NOT保留 ActionWindowTrack 作为另一条 Timeline Window 作者路径。

#### Scenario: Corin Attack1

- **WHEN** 作者配置 `Attack1` 为 Timeline 攻击
- **THEN** Hit/Cancel 时间范围 MUST 位于 `Attack1` Timeline 的 Decision TreeClip
- **AND** TreeClip MUST 写入对应 Bool Frame variables
- **AND** Gameplay/VFX/Camera cue MUST 继续位于其正式 Timeline 输出
- **AND** RootTree 主流程 MUST NOT平铺窗口、Cue 或结果测试节点

#### Scenario: 非 Timeline 动作

- **WHEN** 作者配置不播放 Timeline 的持续格挡或其它动作
- **THEN** Graph MAY 写入具有显式 projection 的 scope variable
- **AND** 输出仍 MUST 使用 Action Context 和 ActionProfile 策略解析

### Requirement: 非 Timeline 输出必须共享同一套策略解析

系统 MUST 允许 Timeline 和非 Timeline action window 使用同一套 Action Context、scope variable fact projection 与 policy resolver。非 Timeline Graph MUST 通过显式 Action Context provenance 写入 projected variable；系统 MUST NOT 保留直接 SubmitActionWindowSample 作者节点，也 MUST NOT引入第二套 action identity、ActionModule 或 per-node 网络策略。Motion、Cue 和 Result 的非 Timeline 输出仍 MUST 使用各自正式输出合同。

#### Scenario: 持续格挡窗口

- **WHEN** Guard 状态在没有 Timeline 的情况下持续写入 Guard window Frame variable=true
- **AND** 写入携带显式 Guard Action Context
- **THEN** 统一 projection MUST 生成归属该 ActionInstance 的 Guard ActionWindowSample
- **AND** GuardWindow 的 prediction、history 和 replication policy MUST 从 ActionProfile 解析

#### Scenario: 非 Timeline 运动输出

- **WHEN** 某个 dodge 或 knockback 逻辑直接产出 motion sample
- **THEN** 输出 MUST 使用 MotionSourceType 和 Action Context 描述事实
- **AND** Window 作者路径重构 MUST NOT改变 Motion policy resolver

### Requirement: Dodge Action 必须通过 pipeline blackboard 公布 locomotion ownership

Corin DodgeForward 和 DodgeBack MUST 保持为 Action StateMachine 中唯一 Dodge 业务状态。Dodge OnEnter MUST 在 ActionInstance 成功激活后写入 pipeline blackboard `IsDodging=true`；所有 source-exit 的 OnExit MUST 写入 `IsDodging=false`。Dodge Timeline 的移动恢复门和 IFrame 时间范围 MUST 都由 Decision TreeClip 写入 Bool Frame variables：`CanDodgeMoveCancel` 保持 Projection=None，Dodge IFrame declaration 使用显式 ActionWindow projection。Locomotion MUST 只读取 ownership fact，不得复制 Dodge request、ActionProfile、Timeline、motion curve、IFrame 或恢复门。

#### Scenario: Dodge 激活后让渡 locomotion 所有权

- **WHEN** DodgeForward 或 DodgeBack 成功激活 ActionInstance
- **THEN** 对应 OnEnter MUST 写入 `IsDodging=true`
- **AND** Locomotion StateMachine MUST 能读取该值进入 ActionOverride

#### Scenario: Dodge 正常完成或被打断

- **WHEN** Dodge state 正常完成、被 State transition 抢占或被上层 tree stop
- **THEN** source OnExit MUST 写入 `IsDodging=false`
- **AND** Locomotion MUST 能按当前 MoveAxis 收回所有权

#### Scenario: 单一 Dodge 动作真相

- **WHEN** Locomotion 处理 Dodge 活跃期间的所有权
- **THEN** Locomotion MUST NOT创建第二个 Dodge action state 或引用 Dodge Timeline
- **AND** Dodge request MUST 继续只由 Action 激活接受点消费
- **AND** Dodge IFrame MUST 由 Decision TreeClip scope variable projection 产生，不得保留 ActionWindowTrack
