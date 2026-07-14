## ADDED Requirements

### Requirement: 角色管线必须保留跨 logic tick 的动画生命周期命令

系统 MUST 使用 presentation-owned 的持久命令队列或等价正式结构保存尚未被 PresentationFrame 消费的 terminal sampling metadata、Complete、Release、owner transition 和 `OwnerReady` events。该结构 MUST 独立于单个 `CharacterPipelineFrame.Output` 的 transient 生命周期。每个 command MUST 能按 local logic tick、同 tick 内 lifecycle phase 和稳定 sequence 保序。系统 MUST NOT 通过只保留最后一个 catch-up logic tick 的 presentation output 来表达完整动画生命周期。

#### Scenario: 单个 render frame 执行多个 logic tick

- **WHEN** `GameplayTickSystem` 在一个 render frame 内连续执行多个 catch-up logic tick
- **AND** 较早 tick 完成 Timeline
- **AND** 后续 tick 发生 state transition
- **THEN** Complete 和 owner transition events MUST 都保留到本 render frame 的 PresentationFrame
- **AND** 后一个 `CharacterPipelineFrame.Begin()` MUST NOT 覆盖较早 tick 尚未消费的命令

#### Scenario: 清理 transient output

- **WHEN** pipeline 调用 `Output.Clear()` 或 `ClearTransient()`
- **THEN** strict gameplay、普通 presentation output 和 sync facts MAY 按现有帧语义清理
- **AND** 尚未被 PresentationFrame 确认消费的动画 lifecycle commands MUST 保留

#### Scenario: Registry 应用命令失败

- **WHEN** PresentationFrame 已复制 pending lifecycle commands
- **AND** Registry 尚未成功应用完整批次
- **THEN** command queue MUST NOT 提前 acknowledge 或删除这些 commands
- **AND** 只有完整应用返回后才可移除已确认批次

#### Scenario: Pipeline 释放

- **WHEN** pipeline deactivate 或 dispose
- **THEN** pending lifecycle commands、Registry entries 和 terminal presentation records MUST 全部释放
- **AND** 系统 MUST NOT 使用超时等待隐藏未释放 owner

### Requirement: PresentationFrame 必须完成统一动画 lifecycle handoff

系统 MUST 在每个 PresentationFrame 中按正式顺序处理 Timeline animation sampling、lifecycle command consumption、Registry snapshot、LayerRuntime arbitration、Transition visual plan、Animancer application 和 retirement acknowledgement。该顺序 MUST 保证 producer completion 与 owner transition 不产生中间空 snapshot，同时 MUST 保持 gameplay facts 只在 logic tick 产生。

#### Scenario: Timeline 完成后下一 logic tick 切换状态

- **WHEN** Once Timeline 在一个 logic tick 完成
- **AND** StateMachine 在后续 logic tick 进入 target state
- **THEN** PresentationFrame MUST 能同时消费旧 playback 的 terminal/CompletedHeld 状态和新 owner 的 Sample
- **AND** 非零 transition MUST 生成 outgoing/incoming visual plans
- **AND** 零 transition MUST 原子替换

#### Scenario: Catch-up 中 target 首次执行

- **WHEN** 较早 logic tick 提交 owner transition
- **AND** 后续 catch-up logic tick 提交 target `OwnerReady`
- **THEN** 两个事件 MUST 按 tick 与 lifecycle phase 保留到同一个 PresentationFrame
- **AND** source MUST 在 ready 前保持最后合法输出

#### Scenario: 当前帧没有 transition

- **WHEN** Registry 中只有 Active 或 CompletedHeld contributions
- **AND** 本帧没有 owner transition event
- **THEN** LayerRuntime MUST 从 Registry 当前 snapshot 生成计划
- **AND** Presenter MUST NOT 依据 transient producer list 缺席提前 Stop

#### Scenario: 表现帧不产生 gameplay facts

- **WHEN** PresentationFrame 采样 active 或 terminal-pending Timeline 动画并完成 handoff
- **THEN** pipeline MUST NOT 再次 tick BTSMTL RootTree
- **AND** pipeline MUST NOT 再次产生 action window、cue、motion、ClientCommand 或 SyncFacts
