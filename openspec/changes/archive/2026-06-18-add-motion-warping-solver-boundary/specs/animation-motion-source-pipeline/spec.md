## ADDED Requirements

### Requirement: Motion Warping Solver 边界
系统 MUST 提供 Motion Warping solver 或批准的等价纯数据求解边界，用于把 motion intent、动画运动源采样、warp target snapshot、root pose snapshot 和 motion window 解析为本 tick 的运动贡献。该 solver MUST 位于 motion intent / animation motion source 之后、`MovementCommand` 或 `ActionMovementCommand` 生成之前，并 MUST NOT 执行运动副作用。

#### Scenario: Solver 位于命令生成前
- **GIVEN** Action 或 Locomotion 已经产出 motion intent
- **AND** 当前 motion intent 声明需要 Motion Warping
- **WHEN** 本 tick 构建 movement command 或 action movement command
- **THEN** 系统 MUST 先通过 Motion Warping solver 生成纯数据 delta、yaw 或等价 motion result
- **AND** 生成结果 MUST 再进入 `MovementCommand`、`ActionMovementCommand` 或批准的等价 command payload

#### Scenario: Solver 不执行副作用
- **WHEN** Motion Warping solver 处理任意输入
- **THEN** solver MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 写 `Transform`
- **AND** MUST NOT 调用 `CharacterController.Move`
- **AND** MUST NOT 写 runtime blackboard

#### Scenario: Solver 不读取表现层 runtime
- **WHEN** Motion Warping solver 需要动画播放进度或动画运动数据
- **THEN** 它 MUST 使用纯数据 playback window、motion profile sample 或批准的等价 tick sampled data
- **AND** MUST NOT 直接读取 Animancer runtime state、Animator runtime state、AnimationClip 或 TransitionAsset

### Requirement: Warp Target Snapshot 纯数据
系统 MUST 将 Motion Warping 目标解析为当前 tick 的纯数据 target pose snapshot 后再交给 solver。Warp target snapshot MUST 至少能表达目标位置、目标朝向、有效性、source id 和 source step，且 MUST NOT 保存 Unity scene object、表现层 runtime object、目标历史或预测状态。

#### Scenario: Target provider 输出 snapshot
- **GIVEN** 当前动作需要对齐锁定目标、交互点或设计点
- **WHEN** runtime adapter 或 target provider 解析该目标
- **THEN** 输出 MUST 是纯数据 warp target snapshot
- **AND** solver MUST 只消费该 snapshot
- **AND** solver MUST NOT 保存或读取目标 `Transform`

#### Scenario: 缺失 target 不 fallback
- **GIVEN** motion intent 声明 target 是必需的
- **AND** target provider 未能解析有效 target snapshot
- **WHEN** Motion Warping solver 运行
- **THEN** solver MUST 输出正式无效结果或配置错误
- **AND** MUST NOT 自动改用角色前方默认点、零点、上一次目标或场景查找结果

#### Scenario: Moving target 由每 tick snapshot 表达
- **GIVEN** 近战攻击目标在多个 tick 之间移动
- **WHEN** target provider 每 tick 解析目标
- **THEN** provider MAY 输出不同的当前 tick target pose snapshot
- **AND** Motion Warping solver MUST 只消费本 tick snapshot
- **AND** solver MUST NOT 持有目标 `Transform`
- **AND** MUST NOT 根据上一 tick target 自行预测或追踪目标轨迹

### Requirement: MotionWarpInput 与 MotionWarpResult 共享合同
系统 MUST 为 Action 与 Locomotion 提供共享的 MotionWarpInput / MotionWarpResult 或批准的等价纯数据合同。该共享合同 MUST 位于领域 command 之前；Action 与 Locomotion MAY 分别把 result 适配到现有 `ActionMovementCommand`、`MovementCommand` 或 movement facts。

#### Scenario: Action result 适配到 ActionMovementCommand
- **GIVEN** Action motion intent 使用 Motion Warping
- **WHEN** solver 输出 MotionWarpResult
- **THEN** Action motion resolve MUST 将该 result 转换为 `ActionMovementCommand` 或批准的等价 action motion payload
- **AND** MUST NOT 要求 Locomotion command contract 同步变化

#### Scenario: Locomotion result 适配到 MovementCommand
- **GIVEN** Locomotion motion source 使用同一类 MotionWarpInput
- **WHEN** solver 输出 MotionWarpResult
- **THEN** Locomotion runtime MUST 将该 result 转换为 `MovementCommand`、movement facts 或批准的等价 locomotion motion payload
- **AND** MUST NOT 强行合并 `MovementCommand` 与 `ActionMovementCommand`

### Requirement: 第一版 Motion Warping 支持攻击吸附和转向修正
系统第一版 Motion Warping MUST 支持基于当前 tick target pose snapshot 的攻击吸附和转向修正。攻击吸附 MUST 输出受策略限制的 planar delta；转向修正 MUST 输出受策略限制的 yaw delta；两者 MUST 保持纯数据结果并由现有 command adapter 进入角色帧计划。

#### Scenario: 攻击吸附输出受限 planar delta
- **GIVEN** Action motion intent 在有效 motion window 内声明攻击吸附
- **AND** 当前 tick target pose snapshot 有效
- **WHEN** Motion Warping solver 运行
- **THEN** solver MUST 输出受 warp policy 限制的 planar delta
- **AND** MUST NOT 直接移动角色根

#### Scenario: 转向修正输出受限 yaw delta
- **GIVEN** Action motion intent 在有效 motion window 内声明 facing correction
- **AND** 当前 tick target pose snapshot 有效
- **WHEN** Motion Warping solver 运行
- **THEN** solver MUST 输出受 warp policy 限制的 yaw delta
- **AND** MUST NOT 直接写角色 Transform rotation

### Requirement: Warped TickSampledMotion 保持确定性
系统 MUST 在需要预测、回滚或预测矫正的 Motion Warping 状态中使用 tick 对齐播放窗口和纯数据 target snapshot。Warped motion 的采样和修正 MUST 对同一输入序列确定，并 MUST NOT 依赖 Unity frame 回调次数。

#### Scenario: 同一 tick 输入产生同一 warp result
- **GIVEN** 相同的 playback window、motion profile sample、root pose snapshot、target snapshot 和 warp policy
- **WHEN** Motion Warping solver 重复运行
- **THEN** 输出的 planar delta、yaw delta、有效性和失败原因 MUST 保持一致

#### Scenario: 不依赖 OnAnimatorMove
- **GIVEN** 当前 motion intent 使用 Motion Warping
- **WHEN** pipeline 需要本 tick 的 warped motion 贡献
- **THEN** 它 MUST 使用 tick 对齐输入数据
- **AND** MUST NOT 依赖 `OnAnimatorMove` pending delta、Animator runtime root delta 或 Unity frame callback 次数

### Requirement: Motion Warping 不产生第二运动路径
系统 MUST 保持最终位移和旋转应用权威在正式 motion executor。Motion Warping solver 只能产生候选 motion result，最终是否执行 MUST 由 `CharacterFramePlan` 或批准的等价角色级计划决定。

#### Scenario: Plan 未选择时不执行
- **GIVEN** Motion Warping solver 已生成 motion result
- **AND** `CharacterFramePlan` 未选择该 source 的 motion candidate
- **WHEN** output applier 执行本帧
- **THEN** 该 warped motion MUST NOT 被提交给 motion executor
- **AND** solver MUST NOT 通过其它路径补交该运动

#### Scenario: 执行仍经正式 executor
- **GIVEN** `CharacterFramePlan` 选择了包含 warped motion 的 motion candidate
- **WHEN** output applier 执行本帧
- **THEN** warped motion MUST 经现有 motion executor 或批准的等价统一运动端口应用
- **AND** 系统 MUST NOT 新增 Motion Warping 专用 `CharacterController.Move` 路径
