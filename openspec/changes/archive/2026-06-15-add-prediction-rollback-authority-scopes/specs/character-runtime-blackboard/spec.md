## ADDED Requirements

### Requirement: 黑板 facts 的回滚权威分类
角色运行时黑板 MUST 支持或可被外部 resolver 映射到回滚权威分类。Locomotion facts、Action facts 和 Animation facts MUST 能被区分为 strict gameplay、presentation drift、predictive gameplay 或 ignored。黑板自身 MUST 继续只保存 facts，不得成为第二状态机或 comparer 策略实现。

#### Scenario: Locomotion facts 默认为 strict
- **WHEN** comparer 比较 locomotion phase、gait、world direction 或 move intent facts
- **THEN** 这些 facts MUST 默认属于 strict gameplay
- **AND** 差异 MUST 导致 strict mismatch

#### Scenario: Action facts 默认为 strict
- **WHEN** comparer 比较 action active、state、completed 或 movement facts
- **THEN** 这些 facts MUST 默认属于 strict gameplay
- **AND** 差异 MUST 导致 strict mismatch

#### Scenario: Animation facts 可分层
- **WHEN** comparer 比较 animation facts
- **THEN** profile-driven playback facts MUST 能标记为 strict gameplay
- **AND** visual-only playback facts MUST 能标记为 presentation drift

### Requirement: 黑板不决定比较策略
黑板 MUST NOT 自行决定 F6/F8 是否失败。比较策略 MUST 由 rollback authority/scope resolver、state policy 或等价外部纯数据规则处理。黑板 MAY 提供 phase、alias、action key、normalized time 等事实供 resolver 判断。

#### Scenario: 黑板只提供事实
- **WHEN** comparer 需要判断某 animation fact 的 compare scope
- **THEN** 黑板 snapshot MAY 提供 phase、alias 和 action key
- **AND** 黑板 MUST NOT 持有 comparer 或 runner 实例

#### Scenario: Restore 不触发 scope 副作用
- **WHEN** 系统恢复黑板 snapshot
- **THEN** restore MUST 只恢复事实值
- **AND** MUST NOT 因 scope 分类播放动画、移动角色或切换状态
