## MODIFIED Requirements

### Requirement: Locomotion Phase relation必须服从Transition generation与Player continuation

普通循环到循环的 Phase 关联 MUST 保持 outgoing 为 leader，incoming 按既有算法持续同步。有限 incoming 动作 MUST 保持原始入口和自然播放时间，MUST NOT 被循环相位重定时；outgoing 循环动作 MUST 连续淡出，不跳转自身相位或延迟 Control 状态切换。有限 outgoing 动作进入循环 incoming 时，incoming MUST 在新关联建立时匹配一次 outgoing 当前有效相位并建立 continuation，后续 MUST 按自己的时钟自然推进，MUST NOT 持续锁定到有限动作末帧。两侧 MUST 继续使用既有 Standard Blend；表现相位 MUST NOT 写回 Gameplay。转换替换、反向 edge、正常 release、AlwaysResetOnEntry、图替换、Reset 与 Dispose MUST 清理旧关联；Commit／Discard MUST 保持帧事务一致。

#### Scenario: 同authority的Turn进入RunLoop
- **WHEN** 有限 TurnBack 与循环 RunLoop 建立过渡
- **THEN** RunLoop 选择与 TurnBack 当前出口采样相位对应的入口
- **AND** 后续混合帧按 RunLoop 原有时钟推进，即使 TurnBack 已停在末帧也不冻结

#### Scenario: RunLoop 进入 TurnBack
- **WHEN** RunLoop 与有限 TurnBack 建立过渡
- **THEN** TurnBack 从原入口播放，RunLoop 保持自然时间淡出
- **AND** 不裁剪 TurnBack、不跳转 RunLoop，也不增加 Control 等待

#### Scenario: 普通循环切换
- **WHEN** WalkLoop 与 RunLoop 都具有合法循环相位计划
- **THEN** incoming 继续按现有 Phase 反求与关联规则同步
- **AND** Control 不读取 Presentation 的同步结果

#### Scenario: Transition在Blend中被替换
- **WHEN** 有限动作到循环的混合结束或被替换
- **THEN** 旧关联按现有生命周期释放
- **AND** 循环继续保留有效采样锚点，新的关联不复用旧 leader 身份
