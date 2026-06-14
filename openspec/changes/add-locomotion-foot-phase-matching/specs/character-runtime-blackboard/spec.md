## ADDED Requirements
### Requirement: 黑板保存 Locomotion 脚相位事实
系统 SHALL 扩展角色运行时黑板的 Animation facts，使其可以保存当前 locomotion 脚相位和最近一次 locomotion 退出脚相位。脚相位 facts SHALL 是纯数据，可 snapshot/restore，不得保存 Unity 场景实例或 Animancer runtime 对象。

#### Scenario: 当前脚相位写入黑板
- **WHEN** animation facts adapter 从当前 locomotion 播放进度采样到有效脚相位
- **THEN** 黑板 Animation facts MUST 保存当前 alias key、normalized time、foot phase、是否有效和 source step

#### Scenario: TurnBack 退出脚相位写入黑板
- **GIVEN** 当前 locomotion phase 为 `TurnBack`
- **AND** 当前脚相位 sample 有效
- **WHEN** 系统确认 TurnBack 将退出到 `MoveLoop + Run`
- **THEN** 黑板 Animation facts MUST 保存最近一次 locomotion exit foot phase

#### Scenario: 无效脚相位不伪造事实
- **WHEN** 当前播放进度无效或缺少有效 foot phase profile
- **THEN** 黑板 Animation facts MUST 标记当前脚相位无效
- **AND** MUST NOT 用 `Unknown` 伪装成可匹配脚相位

### Requirement: 脚相位事实 Snapshot / Restore
系统 SHALL 将 locomotion 脚相位 facts 纳入黑板 snapshot/restore，使本地回放、预测恢复和同步测试能恢复相同的相位匹配输入。

#### Scenario: Snapshot 捕获脚相位
- **GIVEN** 黑板中存在有效当前脚相位和 exit foot phase
- **WHEN** 系统捕获黑板 snapshot
- **THEN** snapshot MUST 包含这些脚相位 facts
- **AND** snapshot MUST 不包含 Unity 对象引用

#### Scenario: Restore 恢复脚相位
- **GIVEN** 系统已经捕获包含脚相位 facts 的 snapshot
- **WHEN** 系统 restore 该 snapshot
- **THEN** 黑板 MUST 恢复相同的当前脚相位和 exit foot phase
- **AND** 重复 restore 同一 snapshot MUST 得到一致结果

#### Scenario: Restore 不触发表现副作用
- **WHEN** 系统 restore 包含脚相位 facts 的黑板 snapshot
- **THEN** restore MUST NOT 播放动画
- **AND** restore MUST NOT 调用 `CharacterController.Move`
- **AND** restore MUST NOT 写入角色 Transform

### Requirement: 脚相位写入权威
系统 SHALL 明确脚相位 facts 的写入权威。只有 animation facts adapter MAY 将播放进度和脚相位 profile 采样结果写入黑板；Presenter、状态机 runner 和 movement executor MUST NOT 直接改写脚相位 facts。

#### Scenario: Adapter 写入脚相位
- **WHEN** 动画播放进度需要转换为脚相位事实
- **THEN** animation facts adapter MAY 写入黑板 Animation facts
- **AND** 写入内容 MUST 是纯数据 sample

#### Scenario: Presenter 不写黑板
- **WHEN** `BasicLocomotionAnimancerPresenter` 播放 RunLoop 并应用 start override
- **THEN** Presenter MUST NOT 直接写入黑板
- **AND** Presenter MUST NOT 通过黑板请求状态切换

#### Scenario: 状态机不维护脚相位
- **WHEN** 统一状态机 runner 推进状态
- **THEN** runner MUST NOT 直接计算或改写脚相位 facts
- **AND** runner MAY 读取黑板 snapshot 中已有的脚相位 facts 作为条件或输出输入

