## Context

TurnBack 是有限动作，RunLoop 是循环动作。把 Run 的脚步相位映射到 TurnBack 中间会裁掉一次完整转身；只给 yaw 恢复零入口不能解决位移和姿态不同步。用户确认新的职责是保留 TurnBack，调整淡入 RunLoop 的入口。

## Goals / Non-Goals

- 保留 TurnBack 原始源入口、源出口、位移、yaw 与 28 / 60 秒 Control 状态时长。
- 淡入 RunLoop 的入口使用当前 outgoing 有限动作的有效采样相位，之后自然循环。
- RunLoop 淡出时不瞬间换相位，不增加 Control 等待窗口。
- 不实现 Distance Matching、动画变形、新的脚锁或支撑脚变体。

## Decisions

### 1. Control 只执行完整源曲线

Corin 不再保存或选择 SourceEntryTicks。Fixed 与 Float32 使用原来的 EvaluateDelta，以 motion elapsed 对同一区间的位移曲线和 yaw 曲线求差。删除入口计划、入口专用求值、无消费者的 request Phase 参数和传播字段，不保留新旧双路径。

### 2. 有限动作不被相位同步重定时

Pose 状态机只为 incoming 循环 Player 建立跟随关系。RunLoop -> TurnBack 保持 outgoing Run 的自然时间和 Standard Blend；TurnBack 保持原入口播放。本次不将淡出 Run 强制映射到另一姿态，也不新增延后转向的逻辑。

该规则按有限动作与循环动作的职责生效，不按 Corin 字符串特判；因此同类有限 incoming 动作也不会再被循环相位改写起点。

### 3. 有限动作接循环只匹配一次

TurnBack -> RunLoop 建立关系时，使用 TurnBack 当前 SampleTime 的相位，反求 RunLoop 的对应入口并建立 continuation anchor。后续可见混合帧继续维护该关联，但不再次覆盖 RunLoop 时间。否则当有限动作已钳制在末帧时，持续匹配会冻结循环动作。

循环到循环仍按既有相位算法持续同步。正常提交、丢弃与关系释放沿用 Player 现有帧事务和 continuation，不新增缓存标志或实时分配。

### 4. 删除不再成立的提前设计

源曲线采样继续服务现有动作。未来技能 Distance Matching 应根据明确的距离目标、距离曲线单调区间、播放速率与运动权威设计，不能复用未经校准的 Run 相位到 TurnBack 入口表。

### 5. 内容版本与范围

Corin Control semantic version 升为 5，Presentation fact 升为 v6，运动绑定 codec 升为 v4，相关 hash 标识更新。旧 binding 按现有版本边界重新准备。本次没有生成新的动画或修改曲线资产。

## Risks / Trade-offs

- RunLoop -> TurnBack 的自然淡出保留响应与连续性，但不承诺任意脚步位置都无滑步；实际视觉效果需要在 Editor 中观察。
- 淡入 RunLoop 只匹配入口，不保证整个混合窗口脚步始终严格同相；这是保留它自然播放速度的取舍。
- 代码编译、离线曲线检查与 Unity 实际表现是不同证据，不把前两者写成视觉验收完成。
