## MODIFIED Requirements
### Requirement: Dodge 作为 FullBody Action 管线实例
系统 MUST 将 Dodge 作为统一 request submission 与 FullBody Action 状态输出的一个动作实例处理。Dodge 可以拥有自己的实例配置、请求参数、方向/后撤变体、动作位移配置、转向配置、run latch 和返回 Locomotion 规则，但这些差异 MUST 通过统一请求/打断仲裁、统一状态机和 `CharacterFrameSubmission` 输出提交表达，不得形成 Dodge 专用准入管线或输出管线。

#### Scenario: Dodge 实例行为仍走同一准入
- **GIVEN** 输入缓冲中存在 Dodge 请求
- **WHEN** 系统处理该请求
- **THEN** 系统 MAY 使用 Dodge 实例逻辑解析 Directional 或 Backstep
- **AND** MAY 使用 Dodge 实例配置决定位移、转向和 resistance
- **BUT** 请求进入统一状态机前 MUST 作为 request submission 进入统一请求/打断仲裁

#### Scenario: Dodge 输出仍由统一状态机和角色提交负责
- **GIVEN** Dodge 请求已被仲裁接受
- **WHEN** 统一状态机进入 `FullBody/Action/Dodge`
- **THEN** Dodge 的动作位移、动画请求、输入消费和返回 Locomotion MUST 仍由统一状态机输出和 `CharacterFrameSubmission` 或等价角色级输出提交表达
- **AND** 仲裁器 MUST NOT 直接播放 Dodge 动画或执行 Dodge 位移
