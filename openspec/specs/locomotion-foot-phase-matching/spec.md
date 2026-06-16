# locomotion-foot-phase-matching Specification

## Purpose
TBD - created by archiving change add-locomotion-foot-phase-matching. Update Purpose after archive.
## Requirements
### Requirement: Locomotion 脚相位 Profile
系统 MUST 提供运行时可读的 Locomotion 脚相位 Profile，用于描述某个 `BasicMovementPhase + BasicMovementGait + alias key` 的脚相位 marker。Profile MUST 是纯数据配置，不得保存 Animancer runtime、AnimationClip、Transform、CharacterController、InputAction 或场景实例引用。

#### Scenario: Profile 描述 RunLoop 左右脚 marker
- **GIVEN** 设计者为 `MoveLoop + Run + RunLoop` 配置脚相位 Profile
- **WHEN** profile 被运行时读取
- **THEN** profile MUST 暴露 alias key、phase、gait 和 marker 列表
- **AND** marker MUST 至少能表达 `LeftPlant` 和 `RightPlant`
- **AND** marker MUST 使用 normalized time 表达位置

#### Scenario: Profile 描述 TurnBack 退出相位
- **GIVEN** 设计者为 `TurnBack + Run + Locomotion.Turn.Back` 配置脚相位 Profile
- **WHEN** TurnBack 播放进度达到退出窗口
- **THEN** sampler MUST 能基于该 profile 采样当前退出脚相位

#### Scenario: Profile 保持纯数据边界
- **WHEN** 运行时读取脚相位 Profile
- **THEN** profile MUST NOT 暴露 Animancer runtime 对象
- **AND** MUST NOT 暴露 AnimationClip
- **AND** MUST NOT 暴露 Transform、CharacterController 或 InputAction

### Requirement: 脚相位 Profile 校验
系统 MUST 校验参与 phase matching 的脚相位 Profile。启用 phase matching 的 profile 缺少 alias、marker、有效 normalized time 或目标 phase/gait 时 MUST 报告错误。系统 MUST NOT 静默使用 `0`、`0.5`、TransitionAsset start time 或其它隐式 fallback 作为脚相位配置。

#### Scenario: 缺少 marker 报错
- **GIVEN** 某脚相位 Profile 启用 phase matching
- **AND** marker 列表为空
- **WHEN** 运行配置校验
- **THEN** 校验结果 MUST 报告缺少脚相位 marker

#### Scenario: marker 时间非法报错
- **GIVEN** 某脚相位 Profile 存在 normalized time 小于 0 或大于 1 的 marker
- **WHEN** 运行配置校验
- **THEN** 校验结果 MUST 报告 marker 时间非法

#### Scenario: 缺失配置不产生 fallback
- **GIVEN** TurnBack 需要 phase matching
- **AND** TurnBack 或 RunLoop 缺少有效脚相位 Profile
- **WHEN** 系统解析 phase matching
- **THEN** 匹配结果 MUST 标记为无效
- **AND** MUST 输出校验或诊断原因
- **AND** MUST NOT 自动改用硬编码 normalized time

### Requirement: 脚相位采样
系统 MUST 提供纯数据脚相位 sampler，根据脚相位 Profile 和播放 normalized time 产出当前脚相位 sample。Sampler MUST 不读取动画播放层对象，不决定逻辑状态，不执行位移。

#### Scenario: 采样当前支撑脚
- **GIVEN** profile 中 `LeftPlant` marker 位于 `0.0`
- **AND** `RightPlant` marker 位于 `0.5`
- **WHEN** sampler 使用 normalized time `0.5` 采样
- **THEN** 输出 MUST 表示当前脚相位为 `RightPlant`

#### Scenario: 无效输入输出 invalid sample
- **GIVEN** profile 缺失或禁用 phase matching
- **WHEN** sampler 尝试采样任意 normalized time
- **THEN** 输出 MUST 标记为无有效脚相位
- **AND** sampler MUST NOT 猜测左右脚

#### Scenario: Sampler 纯数据边界
- **WHEN** 检查 sampler 运行时代码
- **THEN** sampler MUST NOT 引用 Animancer
- **AND** MUST NOT 引用 AnimationClip
- **AND** MUST NOT 引用 Transform、CharacterController 或 InputAction

### Requirement: TurnBack 到 RunLoop 相位匹配
系统 MUST 能将 TurnBack 退出脚相位匹配到 RunLoop 的入场 normalized time。第一版 MUST 只对 `TurnBack -> MoveLoop + RunLoop` 生效，并且只在 RunLoop 新进入时应用一次。

#### Scenario: RightPlant 退出匹配 RightPlant RunLoop
- **GIVEN** TurnBack 退出脚相位为 `RightPlant`
- **AND** RunLoop profile 中存在 `RightPlant` marker
- **WHEN** 系统解析 RunLoop 入场起播点
- **THEN** 匹配结果 MUST 使用 RunLoop 的 `RightPlant` marker normalized time
- **AND** Presenter MUST 在新播放 RunLoop 时设置一次该 normalized time

#### Scenario: LeftPlant 退出匹配 LeftPlant RunLoop
- **GIVEN** TurnBack 退出脚相位为 `LeftPlant`
- **AND** RunLoop profile 中存在 `LeftPlant` marker
- **WHEN** 系统解析 RunLoop 入场起播点
- **THEN** 匹配结果 MUST 使用 RunLoop 的 `LeftPlant` marker normalized time

#### Scenario: 非 TurnBack 入场不应用匹配
- **GIVEN** 当前角色从 Idle、MoveStart、Dodge 或 MoveStop 进入 `MoveLoop + RunLoop`
- **WHEN** 动画上下文没有有效 TurnBack exit foot phase
- **THEN** Presenter MUST NOT 应用脚相位匹配起播 override

#### Scenario: 连续 RunLoop 不重复重设时间
- **GIVEN** Presenter 已经按脚相位匹配播放 RunLoop
- **WHEN** 下一帧仍然提交相同 phase、gait 和 alias key
- **THEN** Presenter MUST NOT 再次设置 `NormalizedTime`

### Requirement: 脚相位诊断
系统 MUST 提供脚相位匹配诊断，使用户能确认 TurnBack 退出相位、RunLoop 目标 marker 和实际起播 normalized time。诊断 MUST 不删除或替代现有 TurnBack、animation motion 和 playback 日志。

#### Scenario: 成功匹配输出诊断
- **GIVEN** TurnBack exit foot phase 成功匹配到 RunLoop marker
- **WHEN** Presenter 新播放 RunLoop
- **THEN** 诊断 MUST 包含 exit foot phase、target alias、target normalized time 和 applied 状态

#### Scenario: 匹配失败输出原因
- **GIVEN** phase matching 请求存在
- **AND** 目标 profile 缺失或没有同脚 marker
- **WHEN** 系统解析匹配结果
- **THEN** 诊断 MUST 包含失败原因
- **AND** MUST NOT 删除现有 locomotion animation playback 日志

