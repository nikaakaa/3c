## ADDED Requirements

### Requirement: Timeline preview session 必须隔离动画生命周期状态

系统 MUST 让每个 `TimelinePreviewSession` 拥有独立的动画贡献生命周期 Registry 或等价 session state。Preview session MUST NOT 读取角色 runtime Registry，MUST NOT 与其它预览窗口共享 active playback entries，也 MUST NOT 把 lifecycle state 写入 Timeline asset。

#### Scenario: 两个窗口预览同一 Timeline

- **WHEN** 两个 Timeline 编辑器窗口预览同一个 Timeline asset
- **THEN** 每个窗口 MUST 拥有独立 playback、contribution 和 owner identities
- **AND** 一个窗口的播放、停止、seek 或 target 切换 MUST NOT 释放另一个窗口的 entries

#### Scenario: 切换预览目标

- **WHEN** preview session 从一个 `TimelinePreviewTarget` 切换到另一个 target
- **THEN** 旧 target 的 preview owner 和 Animancer plans MUST 正式释放
- **AND** 新 target MUST 使用新的 session owner 和独立 Registry

#### Scenario: 关闭预览窗口

- **WHEN** Timeline 编辑器窗口关闭或 preview session dispose
- **THEN** session Registry MUST 释放全部 entries
- **AND** Timeline asset MUST 不保存任何 runtime lifecycle 状态

## MODIFIED Requirements

### Requirement: 预览采样复用正式动画贡献链路

系统 MUST 让 Timeline 编辑器预览复用正式 Timeline 采样、统一动画贡献 lifecycle、动画层和 adapter 链路。动画预览 MUST 从 `AnimationTrack.Sample(...)` 产生带 preview playback/contribution/owner identity 的动画提交，进入 preview session 私有 Registry，再由 `CharacterAnimationLayerRuntime` 生成播放计划，并由 `AnimancerAnimationPresenter` 应用。系统 MUST NOT 通过 Timeline track 直接播放 AnimationClip，也 MUST NOT 在预览中实现第二套 lifecycle 或混合规则。

#### Scenario: 动画轨道处于当前时间

- **WHEN** preview session 时间落在某个 AnimationTrack clip 范围内
- **THEN** 正式管线预览目标 MUST 采样该 clip 并提交 Sample
- **AND** Sample MUST 进入 preview session 私有 Registry
- **AND** Registry snapshot MUST 经过角色动画层运行时仲裁
- **AND** Animancer adapter MUST 只消费仲裁后的播放计划

#### Scenario: 多轨道贡献同一 layer

- **WHEN** 同一预览时间存在多个有效动画 contributions 指向同一 layer
- **THEN** 正式管线预览目标 MUST 使用与角色管线相同的 priority、weight 和 blend mode 规则生成播放计划
- **AND** 系统 MUST NOT 在 Timeline 编辑器里实现第二套混合规则

#### Scenario: 非连续拖拽时间游标

- **WHEN** 用户将 preview time 从一个位置非连续 seek 到另一个位置
- **THEN** preview session MUST 重置旧 playback lifecycle state
- **AND** session MUST 从目标时间重新提交当前有效 contributions
- **AND** 已离开范围的 None clip MUST 不得因为历史 Registry entry 被隐式 Hold

#### Scenario: 连续播放经过 clip 结尾

- **WHEN** preview session 连续播放经过 `ExtraPolationMode=None` clip 的 EndTime
- **THEN** preview producer MUST 提交对应 contribution Release
- **AND** preview 结果 MUST 与正式角色管线的 clip membership 语义一致
