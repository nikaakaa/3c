## ADDED Requirements

### Requirement: Runtime Dump和Replay证据必须绑定同一运行身份

Corin Runtime Dump MUST 记录RunId、Definition/Program/Layout/Projection hash、Session、Actor、ActionInstance、generation、Timeline/SourceMap路径、State/Snapshot hash和输入Trace identity。Runtime Dump是证据，不得被重新解释为作者配置。

#### Scenario: Corin运行证据可追溯

- **WHEN** Skill启动、Timeline播放、正常结束或中断事件被记录
- **THEN** 诊断Dump MUST 能定位到同一Actor、ActionInstance、generation和Replay Request
- **AND** 缺少版本或来源绑定的Dump MUST 不能用于A/B比较
