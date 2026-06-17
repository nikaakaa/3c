## ADDED Requirements

### Requirement: Dodge 旧路径可映射为 Behavior Submission
系统 MUST 提供测试或 adapter 证明当前正式 Dodge 路径的 request、claim、motion、animation、input consume、window facts、cue、diagnostics 和 Run latch 语义可以映射为 typed behavior submissions。该映射 MUST NOT 替换正式 runtime entry。

#### Scenario: Directional Dodge 映射
- **GIVEN** 当前正式路径 accepted Directional Dodge
- **WHEN** golden line mapping 运行
- **THEN** 输出 behavior submission MUST 表达相同 request、FullBody claim、motion spec、animation key 和 input consume candidate
- **AND** MUST 表达 Directional completion 后的 Run latch candidate

#### Scenario: Backstep Dodge 映射
- **GIVEN** 当前正式路径 accepted Backstep Dodge
- **WHEN** golden line mapping 运行
- **THEN** 输出 behavior submission MUST 表达相同 request、FullBody claim、motion spec、animation key 和 input consume candidate
- **AND** MUST NOT 表达 Run latch candidate

### Requirement: Rejected Dodge 不产生 Output Submission
当当前正式路径拒绝 Dodge request 时，golden line mapping MUST 保留 rejected / diagnostics 信息，但 MUST NOT 产生 motion、animation、input consume 或 body claim output submission。

#### Scenario: Rejected request 保留输入
- **GIVEN** Dodge request 被当前 action interrupt 规则拒绝
- **WHEN** golden line mapping 运行
- **THEN** request submission MAY 表达 rejected diagnostics
- **AND** output submission MUST 为空
- **AND** input consume candidate MUST 不存在

### Requirement: Golden Line 覆盖 Action Lifecycle
Dodge golden line MUST 覆盖动作启动、持续、完成、animation-end 等待、动作结束后再次触发和 restore 后 timing 一致性。

#### Scenario: Restore 后一致
- **GIVEN** baseline 路径在 Dodge 中间帧 capture restore state
- **WHEN** restore 后继续 tick
- **THEN** baseline output 与 mapped behavior submission MUST 表达同一 current frame、motion 和 animation intent

#### Scenario: 再次触发一致
- **GIVEN** Dodge 已完成并退出
- **WHEN** 玩家再次按下 Dodge 输入
- **THEN** baseline 路径 MUST 可再次 accepted
- **AND** mapped behavior submission MUST 表达新的 request 和新的 playback intent identity

### Requirement: Golden Line 不成为第二生产路径
Golden line helper、mapping 或测试 fixture MUST NOT 注册为正式 runtime host、submitter、motion executor、animation presenter 或 blackboard writer。正式 gameplay 仍 MUST 走当前生产路径，直到后续 entry proposal 被实施。

#### Scenario: 不替换默认入口
- **WHEN** 检查 `CharacterRuntimeCore` 默认 runtime host 创建逻辑
- **THEN** golden line helper MUST NOT 被创建为默认 submitter 或 runner
- **AND** MUST NOT 改变 production frame pipeline phase 顺序

#### Scenario: 不执行副作用
- **WHEN** golden line mapping 生成 behavior submission
- **THEN** mapping MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 写 runtime blackboard

### Requirement: Golden Line 可测试和可验证
系统 MUST 提供自动测试和静态边界验证，证明 Dodge golden line 能发现 behavior submission 合同缺口，而不会改变生产行为。

#### Scenario: 自动测试覆盖两个变体
- **WHEN** 运行 Dodge golden line EditMode 测试
- **THEN** 测试 MUST 覆盖 Directional、Backstep、rejected、retry、animation-end waiting 和 restore

#### Scenario: 静态边界验证
- **WHEN** 检查 golden line helper 源码
- **THEN** 静态测试 MUST 确认它不注册 production runtime host
- **AND** MUST 确认它不调用 motion executor、animation presenter 或 blackboard writer
