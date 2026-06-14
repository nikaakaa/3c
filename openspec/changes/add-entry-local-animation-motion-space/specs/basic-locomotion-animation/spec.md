## ADDED Requirements

### Requirement: 烘焙运动 Profile 平面空间声明
基础移动烘焙运动 Profile 的 runtime 采样结果 MUST 能在 movement facts 中声明平面 delta 的坐标空间。Profile sampler MUST 继续输出纯 local delta 和 yaw delta；空间解释 MUST 发生在 movement facts/command 到 motion executor 的边界。

#### Scenario: Sampler 只输出资源 local delta
- **GIVEN** 存在匹配的 `LocomotionMotionProfileSO`
- **WHEN** runtime sampler 采样 normalized playback window
- **THEN** sampler MUST 通过累计曲线差分输出本 tick local planar delta
- **AND** MUST 通过累计 yaw 曲线差分输出本 tick yaw delta
- **AND** sampler MUST NOT 读取 Transform、Animator、AnimancerState 或 CharacterController

#### Scenario: TurnBack 使用 EntryLocal translation
- **GIVEN** 当前基础移动 phase 为 `TurnBack`
- **AND** TurnBack translation source 为 `BakedMotionProfile`
- **WHEN** pipeline 将 sampled profile 转换为 movement facts
- **THEN** planar delta space MUST 为 `EntryLocal`
- **AND** movement facts MUST 携带 TurnBack 进入时捕获的 entry planar basis
- **AND** yaw delta MUST 继续来自 sampled profile yaw

#### Scenario: RunEnd 等现有 profile 不强制迁移
- **GIVEN** 当前基础移动 phase 为非 TurnBack 的既有烘焙运动状态
- **WHEN** pipeline 将 sampled profile 转换为 movement facts
- **THEN** 系统 MUST 保持该状态当前声明的 planar delta space 语义
- **AND** 本变更 MUST NOT 强制所有已有 profile 改为 `EntryLocal`

### Requirement: EntryLocal 诊断可见
系统 MUST 在动画运动诊断中暴露 `EntryLocal` 解析所需的关键字段，使用户能从日志确认 profile local delta 如何映射为 world delta。

#### Scenario: TurnBack 方向日志包含 basis
- **WHEN** TurnBack sampled profile 贡献平面位移
- **THEN** 诊断日志 MUST 包含 delta space、entry basis forward、sampled local delta、resolved world delta 和 actual root delta
- **AND** 日志 MUST 能证明位移不是来自 `OnAnimatorMove` pending buffer
