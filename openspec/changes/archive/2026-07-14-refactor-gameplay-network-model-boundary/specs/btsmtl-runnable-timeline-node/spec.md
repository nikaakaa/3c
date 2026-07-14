## MODIFIED Requirements

### Requirement: Timeline 动作事实必须来自 Timeline 轨道采样

系统 MUST 让 Timeline 中具有时间范围的动作事实通过 Decision `TreeClip` 写入显式 scope variable。Timeline 时间范围 MUST 来自 TreeClip，区间逻辑 MUST 来自 inline/shared TimelineRunningTree，当前 Tick active 真值 MUST 来自 Bool Frame/Frame Pipeline Blackboard declaration。系统 MUST NOT 保留 `ActionWindowTrack`、`ActionWindowClip` 或其它与 TreeClip 并行的 Window Track。需要 ActionInstance 身份的 Window variable MUST 通过显式 ActionWindow fact projection 使用 Timeline playback request 携带的 Action Context 生成 `ActionWindowSample`；Timeline asset membership、clip membership 或 ambient active action MUST NOT 自动补齐动作归属。Timeline、TreeClip、Blackboard declaration 与 ActionProfile MUST NOT 保存完整网络策略；当前 Network Model adapter MUST 使用 ActionInstance 对应的稳定 ActionId 从 model profile 解析 effective policy。

#### Scenario: Attack1 产出 HitWindow

- **WHEN** `Attack1` 状态播放带 Action Context 的攻击 Timeline
- **AND** `Attack1Hit` Decision TreeClip 在当前目标时间 active
- **THEN** TreeClip MUST 写入 `Attack1Hit=true` 的 Bool Frame variable
- **AND** 该 declaration 的显式 ActionWindow projection MUST 生成带 ActionInstanceId 的 `ActionWindowSample`
- **AND** Window authority、history 和 replication policy MUST 从当前 Network Model profile 解析

#### Scenario: 普通 locomotion Timeline

- **WHEN** `RunLoop` 状态播放不带 Action Context 的 locomotion Timeline
- **THEN** Timeline MAY 产出 animation contribution 或 motion contribution
- **AND** Timeline MUST NOT 自动创建 ActionInstance
- **AND** ActionWindow-bound variable 缺少 Action Context 时 MUST 报告配置或运行错误，不得伪造动作归属

#### Scenario: Timeline 创建时间窗口

- **WHEN** 作者需要在 Timeline 某个帧范围发布可读条件
- **THEN** 作者 MUST 创建 Decision TreeClip 并写入显式 Blackboard declaration
- **AND** Timeline Editor MUST NOT 提供 ActionWindowTrack 或 ActionWindowClip
