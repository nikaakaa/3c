## ADDED Requirements

### Requirement: Timeline 攻击闭环不得依赖 RootTree 平铺测试输出

作者配置 Timeline 攻击时，攻击窗口和 cue 的时间事实 MUST 优先由 Timeline 轨道表达。RootTree MUST NOT 为某个 Timeline 攻击平铺同一组 `SubmitActionWindowSample`、`SubmitActionCueEvent` 或测试 `GameplayResult` 节点来补充本应属于动作 body 的时间事实。

#### Scenario: Corin Attack1

- **WHEN** 作者配置 `Attack1` 为 Timeline 攻击
- **THEN** Hit/Cancel window MUST 位于 `Attack1` Timeline 的 ActionWindowTrack
- **AND** Gameplay/VFX/Camera cue MUST 位于 `Attack1` Timeline 的 ActionCueTrack
- **AND** RootTree MUST NOT 再平铺 `Submit Attack Window`、`Submit Attack Cue` 或 `Submit Loopback Result`

#### Scenario: 非 Timeline 动作

- **WHEN** 作者配置不播放 Timeline 的持续格挡或其它动作
- **THEN** Graph 或后续 stage MAY 使用非 Timeline 输出节点提交 window、motion 或 cue
- **AND** 这些输出仍 MUST 使用 Action Context 和 ActionProfile 策略解析
