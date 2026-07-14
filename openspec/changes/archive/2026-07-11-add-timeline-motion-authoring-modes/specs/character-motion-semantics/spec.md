# character-motion-semantics Specification Delta

## ADDED Requirements

### Requirement: Timeline 必须支持直接 MotionCurve 位移轨
系统 MUST 支持 Timeline 通过正式 MotionCurve 轨道直接输出 motion contribution。该轨道 MUST 表达位移曲线、yaw 曲线、空间、channel、blend mode、priority、weight 和是否消费低层 channel。轨道 MUST NOT 直接调用 `CharacterController.Move`、修改 Transform、驱动 Animator root motion 或绕过 MotionResolver。

#### Scenario: 攻击前踏使用手画曲线
- **WHEN** 攻击 Timeline 的 MotionCurve clip 覆盖当前播放时间
- **THEN** TimelinePlaybackScheduler MUST 采样该 clip 的位移和 yaw 曲线
- **AND** Scheduler MUST 提交正式 `MotionContribution`
- **AND** `CharacterMotionStage` MUST 通过 MotionResolver 仲裁后应用最终移动

#### Scenario: 本地空间曲线
- **WHEN** MotionCurve clip 配置为 Local space
- **THEN** MotionResolver MUST 按角色当前 rotation 把 displacement 转为世界位移
- **AND** 该行为 MUST 与其它 local motion contribution 使用同一解释规则

#### Scenario: 世界空间曲线
- **WHEN** MotionCurve clip 配置为 World space
- **THEN** MotionResolver MUST 直接使用该 displacement
- **AND** Timeline 轨道 MUST NOT 自己读取场景对象或 camera 作为方向 fallback

### Requirement: Timeline 位移来源必须可追踪
系统 MUST 让 Timeline 产生的直接 motion curve 和 motion warp 在 debug 数据中保持可区分来源。debug source identity MUST 至少能表达 Timeline source、track、clip 或曲线模式，以及关联的 ActionInstance（如果存在）。动画派生位移若进入 Timeline 运行时，MUST 以 MotionCurveTrack 或等价正式 motion fact 来源被追踪，而不是隐藏在 AnimationClip 字段中。

#### Scenario: 同帧存在多个 Timeline 位移来源
- **WHEN** 同一帧存在 MotionCurveTrack 和 MotionWarp window
- **THEN** motion debug MUST 能显示 motion curve contribution
- **AND** motion debug MUST 能显示 MotionWarp modifier delta
- **AND** 作者 MUST 能判断最终 motion intent 由哪个 channel 和 priority 获胜

### Requirement: MotionWarp 必须保持为目标对齐 modifier
系统 MUST 保持 MotionWarp 为 Move 前 modifier。MotionWarpTrack MUST 只表达时间窗口、目标 key、权重和限制参数；目标位置和目标 yaw MUST 来自正式 runtime context。MotionWarpTrack MUST NOT 直接保存场景对象引用、输出固定 displacement，或伪装成普通 motion contribution。

#### Scenario: 目标 key 有效
- **WHEN** Timeline 采样到 MotionWarp window 且 runtime context 提供目标 key
- **THEN** MotionWarpModifier MUST 基于 raw MotionIntent 计算 position/yaw 修正
- **AND** 修正后的 intent MUST 继续通过 CharacterMotionStage 应用

#### Scenario: 目标 key 缺失
- **WHEN** Timeline 采样到 MotionWarp window 但 runtime context 不提供目标 key
- **THEN** MotionWarpModifier MUST 跳过该 window 或按正式错误策略报告
- **AND** 系统 MUST NOT 使用场景搜索、默认目标、Camera.main 或隐藏 fallback 补齐目标

## MODIFIED Requirements

### Requirement: MotionContribution 必须携带仲裁语义
系统 MUST 让 `MotionContribution` 携带正式仲裁语义，包括 motion channel、blend mode、priority、weight、source type、source identity 和是否消费低层 channel。输入移动、Timeline motion curve、gameplay result 和 correction 等来源都 MUST 使用同一贡献合同或正式 modifier 合同。系统 MUST NOT 只依赖字段存在却不参与 resolver 的无效 priority。

#### Scenario: 输入移动提交运动来源
- **WHEN** 输入节点根据移动输入产生本帧位移
- **THEN** 它 MUST 提交 `Locomotion` channel 的 `MotionContribution`
- **AND** 它 MUST NOT 直接把输入移动写成最终 `MotionIntent`

#### Scenario: Timeline 动画轨不提交运动来源
- **WHEN** Timeline 采样到 `AnimationTrack`
- **THEN** 它 MUST 只提交动画表现贡献
- **AND** 它 MUST NOT 从 `AnimationClip` 字段提交 root motion contribution

#### Scenario: Timeline motion curve 提交运动来源
- **WHEN** Timeline 采样到 MotionCurve clip
- **THEN** 它 MUST 按 clip 配置提交正式 `MotionContribution`
- **AND** contribution MUST 携带 channel、blend mode、priority、weight、space 和可追踪 source identity
- **AND** contribution MUST NOT 绕过 MotionResolver 直接覆盖最终位移
