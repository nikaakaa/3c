## MODIFIED Requirements

### Requirement: Timeline 必须只是可选动作输出来源

Timeline MAY 在播放请求中携带显式 Action Context，使 Decision TreeClip 写入的 projected scope variable 生成带 ActionInstanceId 的 Window sample，并使其它正式 Track 生成 motion sample 或 cue event。Timeline MUST NOT自动创建 ActionInstance，也 MUST NOT通过 ambient current action、Timeline asset membership、TreeClip membership 或 declaration owner 自动继承动作归属。WindowType 对应的网络策略 MUST 继续通过 ActionProfile 解析。

#### Scenario: Timeline 攻击

- **WHEN** Graph 激活 `Attack.Light.01` 后播放 `LightAttack01Timeline`
- **THEN** Timeline playback request MUST 携带该 Action Context
- **AND** Hit/Cancel Decision TreeClip 的 projected variable MUST 使用该 context 生成 ActionWindowSample
- **AND** RootMotion 和 Cue 输出 MAY 使用相同 context 写入 ActionInstanceId

#### Scenario: 普通 Timeline 表现

- **WHEN** Graph 播放不属于动作事务的普通表现 Timeline
- **THEN** Timeline MUST 继续正常播放
- **AND** Projection=None 的 TreeClip variable MAY 作为本地条件
- **AND** ActionWindow-bound variable MUST 因缺少 Action Context 而拒绝事实投影

### Requirement: 非 Timeline 动作必须能使用同一 ActionInstance

系统 MUST 支持没有 Timeline 的动作事务通过 Graph 写入有 scope 的 Blackboard variable，并通过相同显式 fact projection 产出动作输出。需要 ActionWindow projection 的写入 MUST 携带显式 Action Context；系统 MUST NOT保留 SubmitActionWindowSampleNode，也 MUST NOT默认读取 ambient current active action。

#### Scenario: 持续格挡

- **WHEN** Graph 激活 `Guard.Hold` 后没有播放 Timeline
- **THEN** Graph MAY 在持有显式 Action Context 时每 Tick写入 Guard window Frame variable
- **AND** 相同 projection stage MUST 生成携带 `Guard.Hold` ActionInstanceId 的 sample

#### Scenario: 输出缺少动作上下文

- **WHEN** Graph 或 Timeline 写入 ActionWindow-bound variable
- **AND** 没有提供有效 Action Context
- **THEN** 系统 MUST 拒绝该 action-scoped projection
- **AND** 系统 MUST NOT自动使用当前 active action 补齐归属

