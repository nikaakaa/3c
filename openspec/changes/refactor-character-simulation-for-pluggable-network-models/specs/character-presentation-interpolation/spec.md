# character-presentation-interpolation Specification

## MODIFIED Requirements

### Requirement: 角色表现插值必须基于 logic sample 历史

角色表现 MUST基于 Simulation Driver/Committer 提供的 accepted 或当前 predicted simulation samples 插值。Sample MUST携带 ActorId、SimulationTick、pass/confirmation 状态、body pose、Timeline visual state 和 stable EventId ledger。Presentation MUST不读取 Kernel mutable state、model packet 或 World Solver object，也 MUST不把 visual result写回 SimulationState。

#### Scenario: Local 单机 sample

- **WHEN** Local Driver 完成一个 Tick
- **THEN** Committer MUST立即发布新 logic sample
- **AND** Presentation MUST按 RenderFrame interpolation alpha 平滑 visual root

#### Scenario: rollback 替换预测 sample

- **WHEN** Replay 产生同 Tick 的修正 state
- **THEN** Committer MUST替换对应 predicted sample并保持 EventId 账本一致
- **AND** Presentation MUST不把旧 visual pose 当作 gameplay truth

### Requirement: 表现插值不得产生同步事实

PresentationFrame MAY消费 logic pose、Timeline visual time、animation selection、camera/cue command 和 model-owned remote sample，但 MUST不写 SimulationState、Program input、gameplay facts、model history、state hash 或 network packet。Replay pass MUST不直接调用 PresentationFrame；只有 Driver/Committer 选择的最新输出可见。

#### Scenario: 一个 RenderFrame 内发生 replay

- **WHEN** Driver 在表现帧前恢复并重演多个 Tick
- **THEN** Presentation MUST只消费 replay 后的最终 sample/command ledger
- **AND** MUST不为每个 replay Tick 重复生成 Cue 或同步事实

### Requirement: Timeline pose time 与 Animancer fade time 必须独立连续推进

Timeline gameplay time MUST来自 SimulationState；Presentation projection MUST根据相邻 accepted/predicted samples 计算 visual Timeline time；Animancer fade MUST继续使用真实 presentation delta。Rollback 改变 Timeline gameplay state时，Presentation MUST从最新 sample重新定位 pose，但 MUST不回滚 Animancer 内部状态作为 gameplay state。

#### Scenario: Attack Timeline 被 replay 修正

- **WHEN** replay 后 selected playback 或 Timeline time 改变
- **THEN** Presentation MUST从新 command/sample 更新 Animancer target
- **AND** fade MUST继续由 Animancer 在 RenderFrame推进

