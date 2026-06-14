## ADDED Requirements
### Requirement: 通用状态 Timeline Window Facts
系统 MUST 将状态 timeline policy 和当前状态播放/计时进度采样为通用 window facts。facts MUST 表达 state id、normalized time、elapsed seconds、活跃窗口、稳定 fact id 和窗口携带的 priority/resistance 信息，并 MUST 保持纯数据边界。

#### Scenario: TurnBack motion window facts
- **GIVEN** 当前状态为 `FullBody/Locomotion/TurnBack`
- **AND** 当前 normalized time 位于 motion window 内
- **WHEN** timeline sampler 采样
- **THEN** window facts MUST 标记 motion window active
- **AND** 输出 MUST 能被 TurnBack baked motion 采样器消费

#### Scenario: 输入锁窗口 facts
- **GIVEN** 当前状态位于 input lock window 内
- **WHEN** timeline sampler 采样
- **THEN** window facts MUST 标记 input lock active
- **AND** 运动输出层 MUST 能据此抑制普通输入旋转和平面位移

#### Scenario: 取消窗口 facts
- **GIVEN** 当前状态位于 interrupt/cancel window 内
- **WHEN** timeline sampler 采样
- **THEN** window facts MUST 标记对应 request kind 在当前窗口可被仲裁
- **AND** MUST 输出 `CancelableToDodge`、`ComboInputOpen` 或等价 typed fact id
- **AND** 仲裁入口 MUST 能读取该事实，而不是直接读取播放层对象

#### Scenario: typed facts 可枚举
- **WHEN** 诊断、测试或未来编辑器读取 active facts
- **THEN** 系统 MUST 能枚举当前 active fact id
- **AND** MUST 能区分 input lock、motion、natural exit、cancel、combo 等事实语义

### Requirement: Timeline Facts 不拥有业务裁决
timeline sampler MUST 只负责把配置和进度转换为 facts，不得直接切换状态、接受请求、播放动画或提交位移。priority、resistance、force 和 request 选择 MUST 由状态请求仲裁入口处理。

#### Scenario: sampler 不接受 Dodge 请求
- **GIVEN** 当前 window facts 表示 Dodge cancel window active
- **WHEN** sampler 输出 facts
- **THEN** sampler MUST NOT 生成 accepted Dodge request
- **AND** MUST NOT 调用状态机切换 API

#### Scenario: sampler 不提交 TurnBack 位移
- **GIVEN** 当前 TurnBack motion window active
- **WHEN** sampler 输出 facts
- **THEN** sampler MUST NOT 调用 motion executor
- **AND** MUST NOT 写 Transform

### Requirement: Timeline Facts 可测试和可诊断
系统 MUST 为通用 timeline window facts 提供自动测试和诊断输出，证明窗口命中、边界值、播放进度缺失和非法配置都可追踪。

#### Scenario: 自动测试覆盖窗口边界
- **WHEN** 运行 timeline facts EditMode 测试
- **THEN** 测试 MUST 覆盖窗口前、窗口起点、窗口中、窗口终点和窗口后

#### Scenario: 诊断日志显示窗口状态
- **WHEN** 诊断开关启用且当前状态存在 timeline policy
- **THEN** 日志 MUST 能显示 state id、normalized time、elapsed seconds 和当前 active windows
