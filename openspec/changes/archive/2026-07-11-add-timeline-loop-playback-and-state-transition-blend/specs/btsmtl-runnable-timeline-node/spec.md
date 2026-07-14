## ADDED Requirements

### Requirement: TimelineNode 播放模式必须属于请求语义

`TimelineNode` MUST 拥有正式播放模式 authoring 数据，并在提交 Timeline playback request 时携带该模式。默认模式 MUST 是 `Once`，保持现有一次性播放完成语义。循环模式 MUST 是 `Loop`，表示同一个 Timeline playback request 在 duration 边界回绕并继续运行。系统 MUST NOT 要求作者用普通 `LoopNode` 包住 `TimelineNode` 来表达 Timeline 动画循环。

#### Scenario: 一次性 Timeline 保持现有完成语义

- **WHEN** `TimelineNode` 播放模式是 `Once`
- **AND** Timeline playback request 返回 `Succeeded`
- **THEN** `TimelineNode` MUST 返回 `Success`
- **AND** 状态是否离开 MUST 继续由 StateMachine condition rule 决定

#### Scenario: 循环 Timeline 保持 Running

- **WHEN** `TimelineNode` 播放模式是 `Loop`
- **AND** Timeline playback request 到达 Timeline duration
- **THEN** request MUST 在 Timeline runtime owner 内回绕并保持 `Running`
- **AND** `TimelineNode` MUST 继续返回 `Running`
- **AND** 节点 MUST NOT 通过自身重启或普通 `LoopNode` 重启来获得下一轮播放

#### Scenario: 循环 Timeline 被状态离开取消

- **WHEN** `Loop` 模式的 `TimelineNode` 因状态离开、父级 stop 或 reset 被停止
- **THEN** 节点 MUST 通过正式播放请求入口取消对应 request
- **AND** 节点 MUST 清理自己的 request handle
- **AND** request MUST NOT 自然返回 `Succeeded`

#### Scenario: 循环 Timeline duration 非法

- **WHEN** `TimelineNode` 播放模式是 `Loop`
- **AND** 引用 Timeline 的 duration 小于等于 0
- **THEN** 系统 MUST 报告配置错误或让该播放请求失败
- **AND** 系统 MUST NOT 自动改为 `Once`、注入默认时长或创建 fallback Timeline
