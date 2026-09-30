## Why

此前让 TurnBack 根据 RunLoop 相位选择非零入口，会跳过转身的位移和旋转。随后只恢复 yaw、仍偏移平移的修补又让同一动作使用两套源时间。2026-09-30 用户明确要求保持 TurnBack 入口和出口，由淡入 RunLoop 匹配入口。本变更据此替换原来的 Control 源入口方案。

## What Changes

- TurnBack 的 Body 位移与 yaw 恢复原有 SourceCurve 区间差，完整执行原有 28 / 60 秒；表现层不再为它重选入口。
- RunLoop 接入有限动作时，只在关联建立时匹配一次 outgoing 的脚步相位，此后按自己的时钟连续推进。
- RunLoop 淡出到 TurnBack 时保持自然播放和既有混合，不等待新的脚步出口，也不跳转 RunLoop 的采样时间。
- 删除本次引入的 SourceEntryTicks、入口表、重复求值函数、表现事实及相关校验；同时删除原来未消费的 request Phase 参数。
- 普通循环到循环的持续相位同步保持现有行为。
- **BREAKING**：Control contract 与相关内容版本更新，旧绑定重新准备，不增加兼容入口。
- 保留现有运动曲线。Distance Matching 属于未来按距离反求时间的独立需求，本变更不提前保留入口框架。

## Capabilities

### New Capabilities

- `character-control-motion-entry`：约束本次转身修复的固定源入口与表现边界，不新增运行时入口选择模块。

### Modified Capabilities

- `character-animation-layer-runtime`：有限动作不作为相位跟随者；淡入循环动作从有限动作匹配一次入口，随后独立推进。

## Impact

涉及 Corin Control 请求、Fixed/Float32 SourceCurve 消费、运动绑定版本、Presentation fact 与 Pose 相位关联。原始动画、Root Motion 曲线和转向输出保持原有内容。原计划中的 ControlPhasePlan、authoring 入口表和 SourceEntryTicks 传播链全部取消。
