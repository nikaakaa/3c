## ADDED Requirements
### Requirement: 仲裁入口只消费预采样 Timeline Facts
动作打断仲裁入口 MUST 将 timeline facts 视为外部输入事实。仲裁入口 MUST NOT 自行根据状态机 definition、snapshot、动画播放进度或 timeline policy 采样窗口。

#### Scenario: 仲裁入口不采样窗口
- **GIVEN** 当前帧已经提供 current `StateTimelineWindowFacts`
- **WHEN** 仲裁入口处理 Dodge、TurnBack、Attack 或等价请求
- **THEN** 仲裁入口 MUST 只读取传入 facts
- **AND** MUST NOT 调用状态机 runner 或 timeline sampler 来生成 current facts

#### Scenario: 缺少 facts 不使用 fallback
- **GIVEN** 某个请求策略要求 timeline fact
- **AND** 当前帧未提供有效 current timeline facts
- **WHEN** 仲裁入口处理该请求
- **THEN** 请求 MUST 被拒绝或配置校验 MUST 报错
- **AND** 系统 MUST NOT 使用 elapsed time fallback 伪造窗口事实
