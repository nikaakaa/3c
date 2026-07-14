## MODIFIED Requirements

### Requirement: Timeline 必须只是可选动作输出来源

Timeline MAY 在播放请求中携带显式 Action Context，使 Decision TreeClip 写入的 projected scope variable 生成带 ActionInstanceId 的 Window sample，并使其它正式 Track 生成 motion sample 或 cue event。Timeline MUST NOT 自动创建 ActionInstance，也 MUST NOT 通过 ambient current action、Timeline asset membership、TreeClip membership 或 declaration owner 自动继承动作归属。Timeline 与 ActionProfile MUST NOT 保存 WindowType 对应的网络策略；当前 Network Model adapter MUST 使用 Action Context 对应的稳定 ActionId 从 model profile 解析 effective policy。

#### Scenario: Timeline 攻击

- **WHEN** Graph 激活 `Attack.Light.01` 后播放 `LightAttack01Timeline`
- **THEN** Timeline playback request MUST 携带该 Action Context
- **AND** Hit/Cancel Decision TreeClip 的 projected variable MUST 使用该 context 生成 ActionWindowSample
- **AND** RootMotion 和 Cue 输出 MAY 使用相同 context 写入 ActionInstanceId
- **AND** 后续网络策略解析 MUST 由当前 Network Model adapter 完成

#### Scenario: 普通 Timeline 表现

- **WHEN** Graph 播放不属于动作事务的普通表现 Timeline
- **THEN** Timeline MUST 继续正常播放
- **AND** Projection=None 的 TreeClip variable MAY 作为本地条件
- **AND** ActionWindow-bound variable MUST 因缺少 Action Context 而拒绝事实投影
