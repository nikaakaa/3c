## Context
当前项目已经有三层事实：

- `BasicLocomotionStateMachine` 使用 UnityHFSM 管理 `Idle / MoveStart / MoveLoop / MoveStop`。
- `add-fullbody-action-framework` 已规划并大部分实现 FullBody coordinator，让 Locomotion 和 Dodge 每帧只有一个 owner 提交平面位移和 base layer 动画。
- `DodgeFullBodyActionModule`、`ActionInterruptArbiter` 和 `ActionRuntimeStateTracker` 已经形成 Action 侧的请求、仲裁和生命周期事实。

现在的问题不是缺一个新的 Dodge 或新的移动状态机，而是 FullBody 主行为域还没有显式状态树。`PlayerFullBodyActionController` 当前以 `CurrentOwner` 和 `dodgeModule.IsActive` 选择 Locomotion 或 Dodge；这能保证权威，但设计者看不到统一状态路径，也不利于后续 Roll、Jump、Attack、Hit、Death 继续接入同一主树。

## Goals / Non-Goals
- Goals:
  - 用现有 UnityHFSM 能力表达 FullBody 主行为域层级。
  - 对外暴露统一状态路径和快照。
  - 保留现有 Locomotion 四阶段语义。
  - 保留现有 FullBody Action module、Action 仲裁和 tracker 事实边界。
  - 让后续动作只能通过 FullBody/Action 子树扩展，避免 per-action controller 分裂。
- Non-Goals:
  - 不新增动作内容。
  - 不重写 Locomotion pipeline。
  - 不把动画 Presenter 或 motion executor 塞进状态机。
  - 不做可视化编辑器。
  - 不把 BBB 运行时作为依赖。

## Decisions
- Decision: 新增 FullBody HFSM driver/builder，而不是重写 `PlayerFullBodyActionController` 成大型主控。
  - Reason: 现有 coordinator 已负责 Unity 组件引用、端口连接和命令提交；HFSM 只应接管状态路径和 transition 权威。

- Decision: `FullBody/Locomotion` 子树复用现有 Locomotion phase 结果。
  - Reason: `BasicLocomotionStateMachine` 已有完整四阶段测试和配置图；本变更只把它作为 FullBody 状态路径的一部分呈现，不复制规则。

- Decision: `FullBody/Action/Dodge` 第一版只包住现有 `DodgeFullBodyActionModule`。
  - Reason: Dodge 已经完成请求、仲裁、动作位移、动作动画命令和退出事实；HFSM 不应重新实现这些业务。

- Decision: `FullBody/Action` 是 FullBody 主树内的动作子域，不是独立 Action 状态机。
  - Reason: Action 仲裁器和 tracker 只是请求裁决与事实记录模块；它们不能成为与 Locomotion 并列、能自行提交 base layer 或位移的第二状态权威。

- Decision: Action 进入许可仍由 `ActionInterruptArbiter` 决定。
  - Reason: 不把优先级藏进多个 `AddTransitionFromAny` 注册顺序，保持已有纯数据打断策略权威。

- Decision: 统一状态快照使用稳定 ID 和字符串路径。
  - Reason: 设计调试需要可读路径；后续网络同步可再把稳定 ID 映射为协议字段，不直接同步 UnityHFSM 内部对象。

## Risks / Trade-offs
- Risk: HFSM 和现有 owner 选择重复，造成两个状态权威。
  - Mitigation: 任务要求迁移后 owner 必须来自 HFSM snapshot，旧 if/else 只保留为过渡实现并最终移除。

- Risk: 为了状态树把 Locomotion phase 规则复制一份。
  - Mitigation: 明确 FullBody/Locomotion 子树读取或包装现有 `BasicLocomotionStateMachine` 输出，不复制 transition 条件。

- Risk: Action module active 状态和 HFSM active state 不一致。
  - Mitigation: 自动测试覆盖 Dodge accepted、active、completed 后状态路径和 tracker 的一致性。

## Migration Plan
1. 定义 FullBody 状态 ID、层级路径和快照模型。
2. 新建 FullBody HFSM builder/driver，第一版只包含 `Locomotion` 和 `Action.Dodge`。
3. 将现有 `PlayerFullBodyActionController.CurrentOwner` 来源切到 HFSM snapshot。
4. 让 Locomotion owner 时的输出仍走现有 `ExecuteLocomotionMotion` 和 `PresentLocomotionAnimation`。
5. 让 Action.Dodge owner 时的输出仍走现有 `DodgeFullBodyActionModule`。
6. 增加诊断属性和测试，确认路径、owner、phase、tracker 一致。
7. 更新路线文档，明确后续 Roll/Jump/Attack 只能作为 `FullBody/Action/*` 状态接入。

## Open Questions
- 第一版 FullBody 状态 ID 使用 enum 还是现有 `ActionStateId`/`BasicMovementPhase` 组合需要在实现时按代码最小改动确定。
- `FullBody/Locomotion` 是否直接嵌套现有 `StateMachine<BasicMovementPhase>`，还是先用 wrapper state 映射 `ActivePath`，实现时以不复制 Locomotion 规则为准；无论实现形式如何，Locomotion 都只是 FullBody 下的逻辑子状态机。
