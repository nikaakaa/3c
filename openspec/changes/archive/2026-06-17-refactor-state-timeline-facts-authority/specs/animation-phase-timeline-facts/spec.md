## ADDED Requirements
### Requirement: 播放进度只作为 Timeline Facts 采样输入
动画播放进度事实 MUST 只作为 timeline facts 采样输入。Action request submission / interrupt arbitration、transition evaluator 和 output resolver MUST 消费采样后的 timeline facts，而不得分别读取播放进度并重算窗口。

#### Scenario: 播放进度集中采样
- **GIVEN** 动画外观层已经写入 Locomotion 或 Action 播放进度事实
- **WHEN** Character frame context 准备 current timeline facts
- **THEN** timeline sampler MUST 读取播放进度事实并产出 current timeline facts
- **AND** 后续请求准入和 transition 判断 MUST NOT 再自行读取播放进度重算同一窗口

#### Scenario: 无播放进度不猜测窗口
- **GIVEN** 当前状态 timeline policy 需要 normalized time
- **AND** 播放进度事实无效
- **WHEN** sampler 生成 current timeline facts
- **THEN** normalized-time 窗口 MUST 不活跃
- **AND** sampler MUST NOT 猜测 clip length、fade duration 或默认 normalized time
