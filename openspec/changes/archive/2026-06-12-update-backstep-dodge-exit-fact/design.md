## Context
当前统一状态机已经把 Locomotion 四阶段和 `Action.Dodge` 放在同一张状态图里。基础移动的 `MoveStop -> Idle` 已经可以通过 `PhaseCanExit` 等待动画结束事实；但 `Dodge -> Idle` 仍然使用固定状态时间。Backstep 的动画资源时长约 2.35 秒，而动作位移配置为 0.35 秒，因此不能通过拉长位移 duration 修复。

## Goals / Non-Goals
- Goals:
  - Backstep Dodge 无输入时等待动作恢复事实后再回 `Idle`。
  - Backstep Dodge 恢复段收到移动输入时可提前回移动阶段。
  - 动作位移时长和动作动画恢复时机分离。
  - 状态机只读取纯数据 facts，不直接读取 Animancer runtime。
  - Directional Dodge 现有手感保持不变。
- Non-Goals:
  - 不实现完整动作 Timeline 编辑器。
  - 不实现 cancel window、hitbox window、IK window、VFX/SFX event 或 camera event。
  - 不使用 Animancer `OnEnd` 回调直接切状态。
  - 不让完整 Animator Root Motion 或动画 Presenter 成为位移权威。
  - 不新增独立 Dodge runtime、独立 Locomotion 图或第二角色控制器路径。

## Decisions
- Decision: 引入动作播放结束/恢复事实，而不是直接把 Backstep duration 改成长动画长度。
  - Reason: `duration` 目前驱动动作位移采样；直接拉长会让 3m 后闪位移分摊到 2.35 秒，破坏手感。
- Decision: 复用黑板和 transition evaluator 的纯数据事实边界。
  - Reason: 基础移动已经通过播放进度快照和 sampler 产出 `CanExit`，Action 应沿用同一方向，不让 Presenter 拥有状态权威。
- Decision: 第一版只提供最小 `ActionCanExit` 或等价恢复事实。
  - Reason: 用户当前只要求修正 Backstep 过早回 Idle；完整 Timeline/window 能力后续再审批。
- Decision: Backstep 的移动输入打断恢复段继续由统一状态机 transition 表达。
  - Reason: 重新输入移动是玩家主动恢复控制的规则，不应被“无输入等待恢复结束”误伤；本次不新增第二套打断系统。
- Decision: 未来 Timeline 应配置动作窗口事实，而不是配置运行时代码分支。
  - Reason: “哪些动作在哪些时间段能被移动、Dodge、Attack 或其它请求打断”应沉淀为 action timeline/window 数据，再由 sampler 输出纯数据 facts 给统一状态机和仲裁器。

## Risks / Trade-offs
- Risk: 如果动作播放进度事实在状态机 tick 后才写入，退出会晚一帧。
  - Mitigation: 接受一帧延迟，保持 Presenter 不直接切状态；测试使用事实输入覆盖状态机行为。
- Risk: Backstep 恢复段重新输入移动的切换点如果过早，可能截断核心后闪位移。
  - Mitigation: 本次只允许动作位移窗口完成后的恢复段回移动；更细的早取消窗口留给后续 Timeline 配置。
- Risk: 当前状态机资产中动画绑定、位移和退出条件仍混在同一配置里，可读性有限。
  - Mitigation: 本次只做窄修复；后续配置边界重构另开 OpenSpec，不在本变更中迁移配置资产结构。

## Migration Plan
1. 扩展动作动画播放进度事实。
2. 将 `ActionAnimationAnimancerPresenter` 的播放进度转换为只读事实写入黑板。
3. 给 transition condition 增加动作恢复退出条件。
4. 更新默认状态机代码 fallback 和 `DefaultCharacterStateMachine.asset` 的 Backstep 无输入退出条件。
5. 更新测试，证明 Backstep 未恢复时保持 Dodge，恢复后回 Idle。

## Open Questions
- 本变更已确认：Backstep 播放未结束但动作位移窗口完成后，如果玩家重新输入移动，允许提前打断恢复段并回到移动阶段。
- 后续 Timeline 需要单独定义移动取消、Dodge 取消、攻击取消等窗口类型和优先级；本变更不实现这些窗口编辑器或通用打断表。
