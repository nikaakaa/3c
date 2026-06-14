## ADDED Requirements

### Requirement: EntryLocal 基准 Tick 可恢复
系统 MUST 在 simulation tick 推进和回滚 restore 中保持 `EntryLocal` entry basis 稳定，使同一状态、同一输入和同一 profile window 在重放时产生相同 world delta。

#### Scenario: Tick 内使用已捕获 basis
- **GIVEN** 当前 Locomotion 状态声明 `EntryLocal` planar delta
- **AND** 该状态已经在进入时捕获 entry basis
- **WHEN** simulation tick N 采样并执行动画运动
- **THEN** pipeline MUST 使用已捕获 basis 解析本 tick world delta
- **AND** MUST NOT 在 tick N 根据当前 Transform 重新推导 entry basis

#### Scenario: 回滚恢复 basis 后重放
- **GIVEN** 客户端在 tick M 捕获 TurnBack entry basis
- **AND** 系统在 tick N 之后回滚到 tick M 或更早快照
- **WHEN** 同一输入序列重新推进到 tick N
- **THEN** TurnBack `EntryLocal` delta MUST 使用恢复后的同一 entry basis
- **AND** root pose MUST 在自动测试容差内收敛

#### Scenario: 多 tick 同帧不改变 basis
- **GIVEN** 一个 Unity frame 内产生多个 simulation tick
- **WHEN** TurnBack 在这些 tick 内连续采样 profile translation
- **THEN** 每个 tick MUST 使用同一 entry basis
- **AND** MUST 只让 sampled yaw 改变当前 root 朝向，不让当前 root 朝向反向改变 translation basis
