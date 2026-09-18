## ADDED Requirements

### Requirement: 开放时长 TreeClip 必须由树退出事件定型

标记为树决定退出的 TreeClip MUST 在 StartFrame 边界产生 Enter，其 Exit MUST 由树内执行到显式“结束片段”节点并回传 Timeline runtime 后产生，MUST NOT 由 EndFrame 边界触发。此类 clip 的 EndFrame MUST 降级为最短持有时长：游标未达 EndFrame 前 runtime MUST NOT 提前退出，超过 EndFrame 后 clip 保持活跃直至树退出事件到达。未标记的 TreeClip MUST 保持现有帧边界语义不变。

#### Scenario: 格挡等待输入

- **WHEN** 标记为树决定退出的格挡 TreeClip 进入且格挡条件持续满足
- **THEN** clip MUST 保持活跃，超过 EndFrame 后 runtime MUST NOT 触发 Exit
- **AND** 树退出事件到达时 runtime MUST 通过既有 Advance/Commit 协议产生真实 Exit 边界并定型该 clip
- **AND** 事件若发生在当前 Advance 内，MUST 在当前 Advance discard 时丢弃，commit 后由下一个 Advance 注入

#### Scenario: 未标记 clip 行为不变

- **WHEN** 未标记退出来源的 TreeClip 播放到 EndFrame
- **THEN** runtime MUST 按现有帧边界语义触发 Exit

### Requirement: 预览观察必须如实显示生长区间

ScenePlay 预览观察中，开放时长 TreeClip 的可视 End MUST 跟随 Runtime 游标实时增长，树退出事件定型后 MUST 固定于实际退出位置。循环等待类内容（无 clip 级退出判定、活跃至 playback 停止）的可视 End MUST 同样跟随 Runtime 游标，playback 停止时定型。显示 MUST 只消费正式 Runtime 发布的游标、活跃 clip 与退出事实，MUST NOT 由 UI 估算时长或补长。

#### Scenario: 预览中观察生长

- **WHEN** 预览游标推进且开放时长 TreeClip 仍活跃
- **THEN** 该 clip 的可视 End MUST 随游标增长
- **AND** 树退出事件到达后可视区间 MUST 定型不再变化

#### Scenario: 循环等待内容随停止定型

- **WHEN** 循环等待类 clip 活跃期间 playback 被外部停止或转场
- **THEN** 该 clip 可视区间 MUST 定型于停止时刻

### Requirement: 循环等待内容不得新增 clip 级退出判定

循环等待输入/转场的内容 MUST 活跃至所属 playback 被外部停止或转场，系统 MUST NOT 为其新增 clip 级独立退出判定或退出事件——转场判定职责 MUST 保持在 ability program 播放级。

#### Scenario: 蓄力循环等待

- **WHEN** 蓄力循环 clip 播放中且无外部停止
- **THEN** 系统 MUST NOT 产生 clip 级退出事件
- **AND** clip MUST 保持活跃直至 playback 停止或转场
