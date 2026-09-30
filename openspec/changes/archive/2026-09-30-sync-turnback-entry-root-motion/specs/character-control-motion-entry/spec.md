## Purpose

约束 Corin 转身的固定源入口与完整运动输出，明确循环相位匹配不进入 Control 运动求值。

## ADDED Requirements

### Requirement: TurnBack 必须保持完整源运动
TurnBack MUST 使用原有源入口、源出口与完整 Control 状态时长。位移与 yaw MUST 使用同一 motion elapsed 对原始曲线求区间差，MUST NOT 因 RunLoop 脚步相位选择非零入口或拆分两者的采样时间。

#### Scenario: 不同跑步相位触发转身
- **WHEN** 玩家在不同 RunLoop 脚步相位触发同一 TurnBack
- **THEN** TurnBack 的局部源曲线位移与 yaw 序列保持原样
- **AND** 60Hz 下完整执行 28 ticks，不通过入口偏移裁剪运动

### Requirement: 表现相位不得反写控制运动
RunLoop 入口匹配 MUST 由 Presentation 消费动画相位计划完成，MUST NOT 写入 Control request、rollback 状态或 Body 位移。系统 MUST 删除本次源入口方案中不再消费的字段、映射表和专用求值路径。

#### Scenario: RunLoop 选择表现入口
- **WHEN** TurnBack 淡出并进入 RunLoop
- **THEN** 仅 RunLoop 的表现入口和 continuation 被调整
- **AND** TurnBack 的 Control 状态出口与 Body 输出不变

### Requirement: 后续距离匹配不得依赖废弃入口表
现有运动曲线 MUST 保持可用。本变更 MUST NOT 为假设的 Distance Matching 保留 Run 相位到 TurnBack 入口的表或空实现。

#### Scenario: 当前转身不需要距离目标
- **WHEN** 运行当前 Corin 转身链路
- **THEN** 系统使用原有运动曲线与表现相位计划
- **AND** 不安装额外的 ControlPhasePlan 或 SourceEntryTicks 协议
