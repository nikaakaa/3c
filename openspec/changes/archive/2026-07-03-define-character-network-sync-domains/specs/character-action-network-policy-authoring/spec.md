## MODIFIED Requirements

### Requirement: Timeline window 必须只标输出类型和窗口参数
系统 MUST 让 Timeline window track/clip 只表达窗口输出类型、窗口 id、时间和业务参数。窗口的 authority、history、replication、digest 策略 MUST 通过 `ActionProfile + WindowType` 解析。命中窗口可以进入 hit/result history rewind，但该策略 MUST 属于输出策略解析结果，不属于 Timeline clip 自身。

#### Scenario: HitWindow
- **WHEN** 作者在 Timeline 中编辑攻击窗口
- **THEN** clip MUST 至少能表达 `WindowType = Hit` 和稳定 `WindowId`
- **AND** 是否进入 hit/result rewind、是否服务端权威、是否只同步 digest MUST 来自 ActionProfile

#### Scenario: CancelWindow
- **WHEN** 作者在 Timeline 中编辑取消窗口
- **THEN** clip MUST 表达 `WindowType = Cancel` 和窗口参数
- **AND** 本地预测或服务器修正策略 MUST 来自 ActionProfile

### Requirement: Runtime Debug 必须按 ActionInstance 展示链路
系统 MUST 提供或预留 Runtime Debug 视图按 `ActionInstance` 展示预测、窗口、motion、gameplay result、网络确认和校正链路。调试视图 MUST 帮助面试官看到本地表现和服务端结果如何对应。

#### Scenario: 查看本地预测攻击
- **WHEN** 本地玩家预测启动攻击
- **THEN** Debug MUST 能显示 action instance id、action id、prediction key、input sequence、state 和 phase
- **AND** MUST 能关联显示该实例产生的 window、motion、cue、gameplay result 和网络发送状态

#### Scenario: 查看服务端拒绝
- **WHEN** 服务端拒绝某次预测动作
- **THEN** Debug MUST 能显示被拒绝的 instance id 和拒绝原因
- **AND** MUST 能显示后续 correction 或表现修正状态
