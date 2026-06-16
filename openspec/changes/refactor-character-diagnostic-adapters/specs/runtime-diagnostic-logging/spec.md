## ADDED Requirements
### Requirement: 角色诊断 Adapter 边界
系统 MUST 将角色运行时核心产生的诊断事实和日志提交分离。状态机 runner、transition evaluator、timeline sampler、character frame pipeline 和 output runtime SHOULD 产出纯数据 trace 或调用窄 diagnostic port；实际 `RuntimeDiagnosticLogEvent` 格式化和 `RuntimeDiagnosticLog.Submit` MUST 由 diagnostic adapter 或等价外围模块承担。

#### Scenario: Core 只产出 trace
- **WHEN** 状态机 runner、condition evaluator 或 timeline sampler 需要诊断信息
- **THEN** 它 MUST 产出纯数据 trace 或填充 frame result trace
- **AND** MUST NOT 直接提交 `RuntimeDiagnosticLog`
- **AND** trace MUST NOT 包含 MonoBehaviour、Transform、CharacterController、Animancer state 或 InputAction

#### Scenario: Adapter 提交统一日志
- **WHEN** diagnostic adapter 接收到 frame、timeline、condition 或 snapshot trace
- **THEN** 它 MUST 格式化为稳定 `RuntimeDiagnosticLogEvent`
- **AND** MUST 通过统一 `RuntimeDiagnosticLog` 出口提交
- **AND** MUST 保留已有 event id 和 channel key 语义

#### Scenario: 日志不改变玩法
- **GIVEN** runtime diagnostic filter 关闭某个分类或通道
- **WHEN** 角色 frame pipeline 处理同一输入序列
- **THEN** active path、owner、input consume、motion execution 和 animation presentation MUST 与开启日志时一致
- **AND** diagnostics adapter MUST NOT 成为状态权威或控制流条件

#### Scenario: 测试可替换 sink
- **WHEN** EditMode 测试验证诊断链路
- **THEN** 测试 MUST 能使用 fake diagnostic sink 观察 trace/event
- **AND** MUST 不依赖 Unity Console 文本作为唯一断言来源

### Requirement: 诊断 trace 必须是纯观测数据
系统 MUST 将 runtime core 产生的诊断数据建模为纯观测 trace。Trace MUST 描述已经发生或已经计算出的事实，不得持有 Unity runtime object，不得拥有状态权威，也不得影响下一帧控制流。

#### Scenario: Trace 不保存 Unity 对象
- **WHEN** runner、pipeline、timeline sampler 或 evaluator 产出 diagnostic trace
- **THEN** trace MUST NOT 保存 `MonoBehaviour`
- **AND** MUST NOT 保存 `Transform`
- **AND** MUST NOT 保存 `CharacterController`
- **AND** MUST NOT 保存 Animancer runtime state 或 InputAction

#### Scenario: Trace 不反向驱动玩法
- **WHEN** diagnostic trace 被生成、过滤、丢弃或提交失败
- **THEN** 状态机 active path MUST 不受影响
- **AND** input consume MUST 不受影响
- **AND** motion execution 和 animation presentation MUST 不受影响

### Requirement: 诊断事件所有权必须唯一
系统 MUST 为每个角色 runtime diagnostic event family 指定唯一 adapter/formatter owner。迁移后同一个 event family MUST NOT 同时从 runtime core 和 diagnostic adapter 两处提交，避免重复日志和顺序歧义。

#### Scenario: Event family 只有一个 submit owner
- **WHEN** FullBody path、action accepted、timeline facts、condition probe 或 Locomotion phase event 被提交
- **THEN** 该 event family MUST 有唯一 adapter/formatter owner
- **AND** runtime core MUST NOT 提交同名 event
- **AND** tests MUST 能通过 fake sink 观察该 event family

#### Scenario: 旧 key 保持可搜索
- **WHEN** diagnostic adapter 格式化迁移后的 event
- **THEN** 旧 event id 和 channel key MUST 保持可搜索
- **AND** 若 payload shape 有必要调整，MUST 在 proposal 或 spec 中记录兼容映射
