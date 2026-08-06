## ADDED Requirements

### Requirement: CharacterSimulationPresentationRuntime 必须在唯一 Pose Plan 内 dispatch Linked fragment

`CharacterSimulationPresentationRuntime` MUST 接收 selector adapter 产生的通用 `CharacterLinkedPoseSelectionFrame`，并按 Projection dispatch descriptor 执行当前 Group Implementation 的预编译 Entry fragments。Root 与 Entry fragment MUST 共享同一只读 Fact frame、source backend、workspace transaction、Animancer Evaluate Barrier、Seal 与 final writer。Runtime MUST 不读取 Linked authoring asset、不解释 Graph、不建立第二 Pose Plan，也 MUST 不把旧 Implementation 输出作为缺失实现 fallback。

#### Scenario: 同帧执行 Locomotion、装备 Pose 与 Action Slot

- **WHEN** root 基础 Locomotion 经过 Equipment Pose Linked Call 后进入活动 Action Slot
- **THEN** Runtime MUST 按同一 ordered staged Pose Plan 执行三者
- **AND** 全部 source MUST 在同一次 PlayableGraph Evaluate 中采样

#### Scenario: Linked 选择在 Barrier 前失败

- **WHEN** incoming Implementation 的 signature、Fact contract 或 source readiness 在 Prepare 阶段失效
- **THEN** Runtime MUST 阻止该事务跨越 Barrier 并走正式 Discard 或 Session admission 失败
- **AND** MUST 不发布混合 generation Pose

### Requirement: 业务 selector 与 Linked 状态提交必须保持单向边界

业务 selector MUST 只把 committed 业务状态映射成通用 Group、Interface、Implementation 与 SelectionRevision。Linked Pose Runtime MUST 不向 Equipment Host、Gameplay Program、Action admission、Vehicle runtime 或 Visual binding 反写动画选择、节点状态或失败恢复。Implementation generation、node state、source demand 与 Pose 输出 MUST 只属于 Presentation Runtime，MUST 不进入 Gameplay snapshot 或 Network packet。

#### Scenario: Equipment selector 选择失败

- **WHEN** Presentation Runtime 因 Implementation ABI 不匹配拒绝切换
- **THEN** Gameplay Equipment committed 状态 MUST 保持原样
- **AND** 失败 MUST 通过 Presentation diagnostics 暴露而不是反向卸载装备

#### Scenario: 新业务状态接入 Linked Pose

- **WHEN** 后续业务提供实现统一合同的 selector adapter
- **THEN** Presentation Runtime MUST 只接收其通用 selection frame
- **AND** MUST 不新增业务专用 runtime dispatch 路径
