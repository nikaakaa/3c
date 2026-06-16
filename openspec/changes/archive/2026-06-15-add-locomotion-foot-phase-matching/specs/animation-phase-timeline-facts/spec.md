## ADDED Requirements
### Requirement: Locomotion 脚相位 Timeline Fact
系统 MUST 将 Locomotion 脚相位作为动画 timeline facts 的扩展项。脚相位 fact MUST 从播放进度快照和脚相位 Profile 采样得到，并保持纯数据边界。

#### Scenario: 播放进度采样脚相位
- **GIVEN** 播放进度快照有效
- **AND** 当前 phase/gait/alias 存在有效脚相位 Profile
- **WHEN** timeline facts sampler 采样当前 normalized time
- **THEN** 输出 MUST 包含当前 locomotion foot phase fact

#### Scenario: 缺少 Profile 不猜测
- **GIVEN** 播放进度快照有效
- **AND** 当前 phase/gait/alias 没有有效脚相位 Profile
- **WHEN** timeline facts sampler 尝试采样脚相位
- **THEN** 输出 MUST 标记脚相位无效
- **AND** MUST NOT 根据 alias 名称或 normalized time 猜测左右脚

#### Scenario: Timeline fact 保持纯数据
- **WHEN** 逻辑层或黑板读取脚相位 timeline fact
- **THEN** 它们 MUST NOT 能通过该 fact 访问 Animancer state
- **AND** MUST NOT 能访问 AnimationClip、TransitionAsset 或 Unity 场景实例

### Requirement: TurnBack 退出脚相位 Fact
系统 MUST 能在 TurnBack 退出窗口采样并保留退出脚相位 fact，使下一段 RunLoop 可以进行相位匹配。该 fact MUST 不改变 TurnBack 的进入条件、退出条件或运动权威。

#### Scenario: TurnBack 可退出时采样退出脚相位
- **GIVEN** 当前 phase 为 `TurnBack`
- **AND** 当前播放进度达到 TurnBack 可退出窗口
- **AND** 当前脚相位 sample 有效
- **WHEN** 系统准备进入 `MoveLoop + Run`
- **THEN** timeline facts MUST 提供 TurnBack exit foot phase fact

#### Scenario: TurnBack 退出条件不由脚相位决定
- **GIVEN** 当前 phase 为 `TurnBack`
- **AND** 当前脚相位 sample 为 `LeftPlant` 或 `RightPlant`
- **WHEN** 状态机评估 TurnBack 是否可退出
- **THEN** 是否可退出 MUST 仍由现有 TurnBack exit policy、timeline window 或 StateCanExit 事实决定
- **AND** 脚相位 fact MUST NOT 单独允许或拒绝 TurnBack 退出

#### Scenario: 非 TurnBack 不覆盖退出脚相位
- **GIVEN** 当前 phase 不是 `TurnBack`
- **WHEN** timeline facts sampler 采样当前脚相位
- **THEN** sampler MUST NOT 把该 sample 作为 TurnBack exit foot phase 写入

### Requirement: 脚相位 Timeline Fact 测试
系统 MUST 为脚相位 timeline facts 提供自动测试，证明采样、无效输入和 TurnBack 退出 fact 行为确定。

#### Scenario: 自动测试覆盖有效采样
- **WHEN** 运行 animation timeline facts EditMode 测试
- **THEN** 测试 MUST 覆盖有效 profile 和播放进度产出当前 foot phase

#### Scenario: 自动测试覆盖无效输入
- **WHEN** 运行 animation timeline facts EditMode 测试
- **THEN** 测试 MUST 覆盖缺少 profile 时不猜测脚相位

#### Scenario: 自动测试覆盖 TurnBack exit fact
- **WHEN** 运行 animation timeline facts EditMode 测试
- **THEN** 测试 MUST 覆盖 TurnBack 可退出时产出 exit foot phase
- **AND** MUST 覆盖非 TurnBack 不覆盖 exit foot phase

